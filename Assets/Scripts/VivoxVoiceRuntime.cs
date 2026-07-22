using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using InterrogationRoom.Domain;
using InterrogationRoom.Networking;
using InterrogationRoom.Settings;
using InterrogationRoom.Voice;
using Mirror;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

internal struct VivoxVoiceSessionRequestMessage : NetworkMessage
{
}

internal struct VivoxVoiceSessionResponseMessage : NetworkMessage
{
    public string SessionId;
}

internal struct VivoxLocalSpeakingStateMessage : NetworkMessage
{
    public bool IsSpeaking;
    public bool IsMuted;
}

internal struct VivoxSpeakingStateMessage : NetworkMessage
{
    public uint NetworkIdentityNetId;
    public bool IsSpeaking;
    public bool IsMuted;
}

[DisallowMultipleComponent]
public sealed class VivoxVoiceRuntime : MonoBehaviour
{
    public enum VoiceConnectionState
    {
        WaitingForNetwork,
        InitializingServices,
        NoInputDevice,
        JoiningChannel,
        Ready,
        Recovering,
        Disconnected,
        Faulted
    }

    private const string PlayerIdPrefix = "mirror-";
    private const float SessionRequestRetrySeconds = 1f;
    private const float SessionResolutionTimeoutSeconds = 10f;
    private const float MaxAudibleDistance = 10f;

    [Header("Session")]
    [SerializeField] private string channelPrefix = "interrogation-room";
    [SerializeField, Min(0.1f)] private float positionUpdateInterval = 0.3f;

    [Header("Playback")]
    [SerializeField, Range(4f, MaxAudibleDistance)] private float audibleDistance = MaxAudibleDistance;
    [SerializeField, Min(0.5f)] private float conversationalDistance = 2f;
    [SerializeField, Min(0.1f)] private float audioFadeIntensity = 1.5f;
    [SerializeField] private LayerMask occlusionMask = ~0;

    [Header("UI")]
    [SerializeField] private Image micIcon;
    [SerializeField] private Color micNormalColor = Color.white;
    [SerializeField] private Color micSpeakingColor = Color.green;
    [SerializeField] private Color micMutedColor = Color.red;

    private readonly Dictionary<string, GameObject> participantTaps = new();
    private readonly Dictionary<string, VivoxParticipant> pendingParticipants = new();
    private readonly Dictionary<uint, VivoxParticipant> participantsByNetId = new();
    private readonly Dictionary<uint, float> participantVolumePercent = new();
    private readonly HashSet<uint> locallyMutedParticipants = new();
    private readonly VoiceSpeakingState networkSpeakingState = new();
    private readonly Dictionary<uint, VivoxSpeakingStateMessage> serverSpeakingStates = new();

    private GameObject localPlayer;
    private NetworkRoundCoordinator roundCoordinator;
    private string activeSessionId;
    private string activeChannelName;
    private string hostSessionId;
    private float nextPositionUpdate;
    private bool wasServerActive;
    private bool serverHandlerRegistered;
    private bool clientHandlerRegistered;
    private bool isReady;
    private bool isJoining;
    private bool isSwitchingChannel;
    private bool isSpatialChannel;
    private bool microphoneTestActive;
    private bool isDisconnecting;
    private bool isShuttingDown;
    private bool localSpeechDetected;
    private bool localSpeakingStateSent;
    private bool localMutedStateSent;
    private TaskCompletionSource<string> pendingSessionId;

    public static VivoxVoiceRuntime Instance { get; private set; }

    public event Action VoiceStateChanged;

    public VoiceConnectionState ConnectionState { get; private set; } = VoiceConnectionState.WaitingForNetwork;
    public bool IsReady => isReady;
    public bool IsSpatialVoice => isSpatialChannel;
    public bool IsLocalMicrophoneMuted =>
        GameSettingsService.Current.MicrophoneMuted || microphoneTestActive;
    public float MicrophoneLevelPercent => GameSettingsService.Current.MicrophoneLevelPercent;

    private float EffectiveAudibleDistance => Mathf.Min(audibleDistance, MaxAudibleDistance);

    public int ActiveAttenuatedSpeakerCount => participantTaps.Values.Count(tap =>
        tap != null &&
        tap.TryGetComponent(out VivoxVoiceOcclusion occlusion) &&
        occlusion.IsActivelyAttenuated);

    private void Awake()
    {
        Instance = this;
        GameSettingsService.Current.Changed += OnGameSettingsChanged;
    }

    private async void Start()
    {
        SetMicColor(micNormalColor);
        await WaitForLocalPlayerAndConnectAsync();
    }

    private void Update()
    {
        RefreshNetworkMessageHandlers();
        HandleMuteInput();

        if (isReady &&
            (localPlayer == null ||
             !NetworkClient.active ||
             NetworkClient.localPlayer == null ||
             NetworkClient.localPlayer.gameObject != localPlayer))
        {
            _ = DisconnectAndWaitForReconnectAsync();
            return;
        }

        if (!isReady || localPlayer == null)
        {
            return;
        }

        RetryPendingParticipants();

        bool wantsSpatialVoice = ResolveWantsSpatialVoice();
        if (!isSwitchingChannel && wantsSpatialVoice != isSpatialChannel)
            _ = SwitchVoiceChannelAsync(wantsSpatialVoice);

        if (isSpatialChannel && Time.unscaledTime >= nextPositionUpdate)
        {
            VivoxService.Instance.Set3DPosition(localPlayer, activeChannelName);
            nextPositionUpdate = Time.unscaledTime + positionUpdateInterval;
        }

        UpdateMicActivity();
    }

    private async Task WaitForLocalPlayerAndConnectAsync()
    {
        if (isJoining)
        {
            return;
        }

        isJoining = true;
        SetConnectionState(VoiceConnectionState.WaitingForNetwork);

        try
        {
            while (!isShuttingDown && (NetworkClient.localPlayer == null || !NetworkClient.active))
            {
                await Task.Yield();
            }

            if (isShuttingDown)
            {
                return;
            }

            localPlayer = NetworkClient.localPlayer.gameObject;
            uint localNetId = NetworkClient.localPlayer.netId;
            activeSessionId = await ResolveSessionIdAsync();
            activeChannelName = BuildChannelName(channelPrefix, activeSessionId);
            string playerId = BuildPlayerId(activeSessionId, localNetId);

            SetConnectionState(VoiceConnectionState.InitializingServices);
            var initializationOptions = new InitializationOptions()
                .SetProfile(BuildServicesProfileId(activeSessionId, localNetId));
            await UnityServices.InitializeAsync(initializationOptions);

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            if (VivoxService.Instance.InitializationState == VivoxInitializationState.Uninitialized)
            {
                await VivoxService.Instance.InitializeAsync();
            }

            SubscribeConnectionEvents();
            if (VivoxService.Instance.AvailableInputDevices.Count == 0)
            {
                SetConnectionState(VoiceConnectionState.NoInputDevice);
                Debug.LogWarning("[Vivox] No microphone input device is available. Receiving voice remains enabled.", this);
            }

            if (VivoxService.Instance.IsLoggedIn)
            {
                await VivoxService.Instance.LogoutAsync();
            }

            var loginOptions = new LoginOptions
            {
                PlayerId = playerId,
                DisplayName = LobbyDisplayNameProvider.Resolve($"Gracz {localNetId}")
            };

            await VivoxService.Instance.LoginAsync(loginOptions);
            ApplyMuteState();

            VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAdded;
            VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantRemoved;

            VivoxService.Instance.EnableAcousticEchoCancellation();
            await JoinVoiceChannelAsync(ResolveWantsSpatialVoice());
            PublishLocalSpeakingState(false, force: true);
            SetConnectionState(
                VivoxService.Instance.AvailableInputDevices.Count == 0
                    ? VoiceConnectionState.NoInputDevice
                    : VoiceConnectionState.Ready);
            Debug.Log($"[Vivox] Joined {(isSpatialChannel ? "spatial" : "global")} channel '{activeChannelName}'.");
        }
        catch (Exception exception)
        {
            SetConnectionState(VoiceConnectionState.Faulted);
            Debug.LogException(exception, this);
            SetMicColor(micMutedColor);
        }
        finally
        {
            isJoining = false;
        }
    }

    private async Task<string> ResolveSessionIdAsync()
    {
        RefreshNetworkMessageHandlers();

        if (NetworkServer.active)
        {
            return EnsureHostSessionId();
        }

        if (!NetworkClient.active)
        {
            throw new InvalidOperationException("A connected Mirror client is required to resolve the Vivox session.");
        }

        var sessionIdCompletion =
            new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        pendingSessionId = sessionIdCompletion;
        float timeoutAt = Time.realtimeSinceStartup + SessionResolutionTimeoutSeconds;
        float nextRequestAt = float.NegativeInfinity;

        while (!isShuttingDown && NetworkClient.active && !sessionIdCompletion.Task.IsCompleted)
        {
            if (Time.realtimeSinceStartup >= nextRequestAt)
            {
                NetworkClient.Send(new VivoxVoiceSessionRequestMessage());
                nextRequestAt = Time.realtimeSinceStartup + SessionRequestRetrySeconds;
            }

            if (Time.realtimeSinceStartup >= timeoutAt)
            {
                sessionIdCompletion.TrySetException(
                    new TimeoutException("The host did not provide a Vivox session identifier."));
            }

            await Task.Yield();
        }

        if (!sessionIdCompletion.Task.IsCompleted)
        {
            sessionIdCompletion.TrySetCanceled();
        }

        return await sessionIdCompletion.Task;
    }

    private void OnParticipantAdded(VivoxParticipant participant)
    {
        if (participant.IsSelf || participant.ChannelName != activeChannelName)
        {
            return;
        }

        if (!TryCreateParticipantTap(participant))
            pendingParticipants[participant.PlayerId] = participant;
    }

    private bool TryCreateParticipantTap(VivoxParticipant participant)
    {
        if (participantTaps.ContainsKey(participant.PlayerId))
            return true;

        if (!TryGetNetworkIdentity(participant.PlayerId, out NetworkIdentity identity))
        {
            return false;
        }

        GameObject tapObject = participant.CreateVivoxParticipantTap(
            $"VivoxVoice-{identity.netId}",
            silenceInChannelAudioMix: true);

        tapObject.transform.SetParent(identity.transform, false);

        AudioSource audioSource = tapObject.GetComponent<AudioSource>();
        float maxDistance = EffectiveAudibleDistance;
        audioSource.spatialBlend = isSpatialChannel ? 1f : 0f;
        if (isSpatialChannel)
        {
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = maxDistance;
            audioSource.SetCustomCurve(
                AudioSourceCurveType.CustomRolloff,
                VoiceAudibilityModel.BuildDistanceRolloffCurve(conversationalDistance, maxDistance));

            VivoxVoiceOcclusion occlusion = tapObject.AddComponent<VivoxVoiceOcclusion>();
            occlusion.Configure(localPlayer.transform, identity.transform, audioSource, occlusionMask);
        }

        participantTaps[participant.PlayerId] = tapObject;
        participantsByNetId[identity.netId] = participant;
        ApplyParticipantSettings(identity.netId, participant);
        pendingParticipants.Remove(participant.PlayerId);
        VoiceStateChanged?.Invoke();
        return true;
    }

    private void OnParticipantRemoved(VivoxParticipant participant)
    {
        pendingParticipants.Remove(participant.PlayerId);
        if (!participantTaps.Remove(participant.PlayerId, out GameObject tapObject))
        {
            return;
        }

        if (tapObject != null)
        {
            Destroy(tapObject);
        }

        if (TryParsePlayerId(activeSessionId, participant.PlayerId, out uint netId))
            participantsByNetId.Remove(netId);
        VoiceStateChanged?.Invoke();
    }

    private void RetryPendingParticipants()
    {
        if (pendingParticipants.Count == 0)
            return;

        VivoxParticipant[] snapshot = pendingParticipants.Values.ToArray();
        for (int index = 0; index < snapshot.Length; index++)
            TryCreateParticipantTap(snapshot[index]);
    }

    private bool TryGetNetworkIdentity(string playerId, out NetworkIdentity identity)
    {
        identity = null;

        if (!TryParsePlayerId(activeSessionId, playerId, out uint netId))
        {
            return false;
        }

        return NetworkClient.spawned.TryGetValue(netId, out identity);
    }

    internal static string BuildChannelName(string prefix, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("A Vivox channel prefix is required.", nameof(prefix));
        }

        return $"{prefix}-{BuildSessionKey(sessionId)}";
    }

    internal static string BuildPlayerId(string sessionId, uint netId)
    {
        return $"{PlayerIdPrefix}{BuildSessionKey(sessionId)}-{netId.ToString(CultureInfo.InvariantCulture)}";
    }

    internal static bool TryParsePlayerId(string sessionId, string playerId, out uint netId)
    {
        netId = 0;
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrEmpty(playerId))
        {
            return false;
        }

        string expectedPrefix = $"{PlayerIdPrefix}{BuildSessionKey(sessionId)}-";
        return playerId.StartsWith(expectedPrefix, StringComparison.Ordinal) &&
            uint.TryParse(
                playerId[expectedPrefix.Length..],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
            out netId);
    }

    internal static string BuildModeChannelName(string prefix, string sessionId, bool spatial) =>
        $"{BuildChannelName(prefix, sessionId)}-{(spatial ? "round" : "lobby")}";

    public float GetParticipantVolumePercent(uint networkIdentityNetId) =>
        participantVolumePercent.TryGetValue(networkIdentityNetId, out float volume)
            ? volume
            : GameSettings.DefaultVoicePercent;

    public void SetParticipantVolumePercent(uint networkIdentityNetId, float volumePercent)
    {
        if (networkIdentityNetId == 0u)
            return;

        float clamped = GameSettings.ClampVoicePercent(volumePercent);
        participantVolumePercent[networkIdentityNetId] = clamped;
        if (participantsByNetId.TryGetValue(networkIdentityNetId, out VivoxParticipant participant))
            participant.SetLocalVolume(GameSettings.VoicePercentToVivoxVolume(clamped));
        VoiceStateChanged?.Invoke();
    }

    public bool IsParticipantLocallyMuted(uint networkIdentityNetId) =>
        networkIdentityNetId != 0u && locallyMutedParticipants.Contains(networkIdentityNetId);

    public void SetParticipantLocallyMuted(uint networkIdentityNetId, bool muted)
    {
        if (networkIdentityNetId == 0u)
            return;

        if (muted)
            locallyMutedParticipants.Add(networkIdentityNetId);
        else
            locallyMutedParticipants.Remove(networkIdentityNetId);

        if (participantsByNetId.TryGetValue(networkIdentityNetId, out VivoxParticipant participant))
        {
            if (muted)
                participant.MutePlayerLocally();
            else
                participant.UnmutePlayerLocally();
        }

        VoiceStateChanged?.Invoke();
    }

    public void SetLocalMicrophoneMuted(bool muted) =>
        GameSettingsService.Current.SetMicrophoneMuted(muted);

    public void SetLocalMicrophoneLevelPercent(float percent) =>
        GameSettingsService.Current.SetMicrophoneLevelPercent(percent);

    public void SetMicrophoneTestActive(bool active)
    {
        if (microphoneTestActive == active)
            return;

        microphoneTestActive = active;
        ApplyMuteState();
        VoiceStateChanged?.Invoke();
    }

    private void OnGameSettingsChanged()
    {
        ApplyMuteState();
        VoiceStateChanged?.Invoke();
    }

    private bool ResolveWantsSpatialVoice()
    {
        if (roundCoordinator == null)
            roundCoordinator = FindFirstObjectByType<NetworkRoundCoordinator>();

        PlayerRoundView view = roundCoordinator != null ? roundCoordinator.CurrentView : null;
        return view != null && view.Phase != RoundPhase.Lobby;
    }

    private async Task SwitchVoiceChannelAsync(bool spatial)
    {
        if (isSwitchingChannel || string.IsNullOrEmpty(activeSessionId))
            return;

        try
        {
            await JoinVoiceChannelAsync(spatial);
            PublishLocalSpeakingState(false, force: true);
            SetConnectionState(
                VivoxService.Instance.AvailableInputDevices.Count == 0
                    ? VoiceConnectionState.NoInputDevice
                    : VoiceConnectionState.Ready);
        }
        catch (Exception exception)
        {
            SetConnectionState(VoiceConnectionState.Faulted);
            Debug.LogException(exception, this);
        }
    }

    private async Task JoinVoiceChannelAsync(bool spatial)
    {
        isSwitchingChannel = true;
        isReady = false;
        SetConnectionState(VoiceConnectionState.JoiningChannel);

        try
        {
            if (!string.IsNullOrEmpty(activeChannelName) &&
                VivoxService.Instance.ActiveChannels.ContainsKey(activeChannelName))
            {
                await VivoxService.Instance.LeaveChannelAsync(activeChannelName);
            }

            DestroyParticipantTaps();
            activeChannelName = BuildModeChannelName(channelPrefix, activeSessionId, spatial);
            if (spatial)
            {
                int channelAudibleDistance = Mathf.Max(2, Mathf.RoundToInt(EffectiveAudibleDistance));
                int channelConversationalDistance = Mathf.Clamp(
                    Mathf.RoundToInt(conversationalDistance),
                    1,
                    channelAudibleDistance - 1);
                await VivoxService.Instance.JoinPositionalChannelAsync(
                    activeChannelName,
                    ChatCapability.AudioOnly,
                    new Channel3DProperties(
                        channelAudibleDistance,
                        channelConversationalDistance,
                        audioFadeIntensity,
                        AudioFadeModel.InverseByDistance));
                VivoxService.Instance.Set3DPosition(localPlayer, activeChannelName);
                nextPositionUpdate = Time.unscaledTime + positionUpdateInterval;
            }
            else
            {
                await VivoxService.Instance.JoinGroupChannelAsync(
                    activeChannelName,
                    ChatCapability.AudioOnly);
            }

            isSpatialChannel = spatial;
            isReady = true;
            ApplyMuteState();
            VoiceStateChanged?.Invoke();
            Debug.Log($"[Vivox] Voice mode: {(spatial ? "spatial Runda" : "global lobby")}.");
        }
        finally
        {
            isSwitchingChannel = false;
        }
    }

    private void DestroyParticipantTaps()
    {
        foreach (GameObject tapObject in participantTaps.Values)
        {
            if (tapObject != null)
                Destroy(tapObject);
        }

        participantTaps.Clear();
        pendingParticipants.Clear();
        participantsByNetId.Clear();
    }

    private void ApplyParticipantSettings(uint netId, VivoxParticipant participant)
    {
        participant.SetLocalVolume(
            GameSettings.VoicePercentToVivoxVolume(GetParticipantVolumePercent(netId)));
        if (IsParticipantLocallyMuted(netId))
            participant.MutePlayerLocally();
        else
            participant.UnmutePlayerLocally();
    }

    private static string BuildServicesProfileId(string sessionId, uint netId)
    {
        string sessionKey = BuildSessionKey(sessionId);
        return $"voice-{sessionKey[..12]}-{netId.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string BuildSessionKey(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("A Vivox session identifier is required.", nameof(sessionId));
        }

        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(sessionId));
        var builder = new StringBuilder(16);
        for (int index = 0; index < 8; index++)
        {
            builder.Append(hash[index].ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private void HandleMuteInput()
    {
#if ENABLE_INPUT_SYSTEM
        bool togglePressed = Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame;
#else
        bool togglePressed = Input.GetKeyDown(KeyCode.V);
#endif
        if (!togglePressed || !VivoxService.Instance.IsLoggedIn)
        {
            return;
        }

        GameSettingsService.Current.SetMicrophoneMuted(
            !GameSettingsService.Current.MicrophoneMuted);
    }

    private void ApplyMuteState()
    {
        if (VivoxService.Instance == null ||
            VivoxService.Instance.InitializationState == VivoxInitializationState.Uninitialized ||
            !VivoxService.Instance.IsLoggedIn)
        {
            return;
        }

        VivoxService.Instance.SetInputDeviceVolume(
            GameSettings.VoicePercentToVivoxVolume(
                GameSettingsService.Current.MicrophoneLevelPercent));

        if (IsLocalMicrophoneMuted)
        {
            VivoxService.Instance.MuteInputDevice();
            PublishLocalSpeakingState(false);
            SetMicColor(micMutedColor);
        }
        else
        {
            VivoxService.Instance.UnmuteInputDevice();
            SetMicColor(micNormalColor);
        }
    }

    private void UpdateMicActivity()
    {
        if (IsLocalMicrophoneMuted || string.IsNullOrEmpty(activeChannelName))
        {
            return;
        }

        bool isSpeaking = VivoxService.Instance.ActiveChannels.TryGetValue(
                activeChannelName,
                out var participants) &&
            participants.FirstOrDefault(participant => participant.IsSelf)?.SpeechDetected == true;

        PublishLocalSpeakingState(isSpeaking);
        SetMicColor(isSpeaking ? micSpeakingColor : micNormalColor);
    }

    private void PublishLocalSpeakingState(bool isSpeaking, bool force = false)
    {
        bool muted = IsLocalMicrophoneMuted;
        bool normalizedState = !muted && isSpeaking;
        if (!force &&
            localSpeakingStateSent &&
            localSpeechDetected == normalizedState &&
            localMutedStateSent == muted)
        {
            return;
        }

        localSpeechDetected = normalizedState;
        if (!NetworkClient.active || NetworkClient.connection == null)
            return;

        NetworkClient.Send(new VivoxLocalSpeakingStateMessage
        {
            IsSpeaking = normalizedState,
            IsMuted = muted
        });
        localSpeakingStateSent = true;
        localMutedStateSent = muted;
        VoiceStateChanged?.Invoke();
    }

    public bool IsNetworkPlayerSpeaking(uint networkIdentityNetId)
    {
        if (!isReady || networkIdentityNetId == 0)
            return false;

        uint localNetId = NetworkClient.localPlayer != null
            ? NetworkClient.localPlayer.netId
            : 0u;
        return localNetId == networkIdentityNetId
            ? localSpeechDetected
            : networkSpeakingState.IsSpeaking(networkIdentityNetId);
    }

    public bool IsNetworkPlayerMicrophoneMuted(uint networkIdentityNetId)
    {
        if (networkIdentityNetId == 0u)
            return false;

        uint localNetId = NetworkClient.localPlayer != null
            ? NetworkClient.localPlayer.netId
            : 0u;
        return localNetId == networkIdentityNetId
            ? IsLocalMicrophoneMuted
            : networkSpeakingState.IsMuted(networkIdentityNetId);
    }

    private async Task DisconnectAndWaitForReconnectAsync()
    {
        await DisconnectAsync(logout: true);

        if (!isShuttingDown)
        {
            await WaitForLocalPlayerAndConnectAsync();
        }
    }

    private async Task DisconnectAsync(bool logout)
    {
        var service = VivoxService.Instance;
        if (isDisconnecting ||
            service == null ||
            service.InitializationState == VivoxInitializationState.Uninitialized)
        {
            return;
        }

        isDisconnecting = true;
        PublishLocalSpeakingState(false, force: true);
        isReady = false;
        service.ParticipantAddedToChannel -= OnParticipantAdded;
        service.ParticipantRemovedFromChannel -= OnParticipantRemoved;
        UnsubscribeConnectionEvents();

        try
        {
            if (!string.IsNullOrEmpty(activeChannelName) &&
                service.ActiveChannels.ContainsKey(activeChannelName))
            {
                await service.LeaveChannelAsync(activeChannelName);
            }

            if (logout && service.IsLoggedIn)
            {
                await service.LogoutAsync();
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            DestroyParticipantTaps();
            participantVolumePercent.Clear();
            locallyMutedParticipants.Clear();
            pendingSessionId?.TrySetCanceled();
            pendingSessionId = null;
            activeSessionId = null;
            activeChannelName = null;
            localPlayer = null;
            localSpeechDetected = false;
            localSpeakingStateSent = false;
            localMutedStateSent = false;
            networkSpeakingState.Clear();
            isDisconnecting = false;
            if (!isShuttingDown)
                SetConnectionState(VoiceConnectionState.Disconnected);
        }
    }

    private async void OnDestroy()
    {
        isShuttingDown = true;
        GameSettingsService.Current.Changed -= OnGameSettingsChanged;
        if (Instance == this)
            Instance = null;
        UnregisterNetworkMessageHandlers();
        await DisconnectAsync(logout: true);
    }

    private void RefreshNetworkMessageHandlers()
    {
        bool serverActive = NetworkServer.active;
        if (wasServerActive && !serverActive)
        {
            hostSessionId = null;
            serverSpeakingStates.Clear();
        }

        wasServerActive = serverActive;

        if (serverActive && !serverHandlerRegistered)
        {
            NetworkServer.ReplaceHandler<VivoxVoiceSessionRequestMessage>(OnSessionIdRequested);
            NetworkServer.ReplaceHandler<VivoxLocalSpeakingStateMessage>(OnLocalSpeakingStateReceived);
            serverHandlerRegistered = true;
        }
        else if (!serverActive)
        {
            serverHandlerRegistered = false;
        }

        if (NetworkClient.active && !clientHandlerRegistered)
        {
            NetworkClient.ReplaceHandler<VivoxVoiceSessionResponseMessage>(OnSessionIdReceived);
            NetworkClient.ReplaceHandler<VivoxSpeakingStateMessage>(OnSpeakingStateReceived);
            clientHandlerRegistered = true;
        }
        else if (!NetworkClient.active)
        {
            clientHandlerRegistered = false;
        }
    }

    private void UnregisterNetworkMessageHandlers()
    {
        if (serverHandlerRegistered)
        {
            NetworkServer.UnregisterHandler<VivoxVoiceSessionRequestMessage>();
            NetworkServer.UnregisterHandler<VivoxLocalSpeakingStateMessage>();
            serverHandlerRegistered = false;
        }

        if (clientHandlerRegistered)
        {
            NetworkClient.UnregisterHandler<VivoxVoiceSessionResponseMessage>();
            NetworkClient.UnregisterHandler<VivoxSpeakingStateMessage>();
            clientHandlerRegistered = false;
        }
    }

    private void OnSessionIdRequested(
        NetworkConnectionToClient connection,
        VivoxVoiceSessionRequestMessage message)
    {
        connection.Send(new VivoxVoiceSessionResponseMessage
        {
            SessionId = EnsureHostSessionId()
        });

        foreach (VivoxSpeakingStateMessage state in serverSpeakingStates.Values)
            connection.Send(state);
    }

    private void OnSessionIdReceived(VivoxVoiceSessionResponseMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.SessionId))
        {
            pendingSessionId?.TrySetResult(message.SessionId);
        }
    }

    private void OnLocalSpeakingStateReceived(
        NetworkConnectionToClient connection,
        VivoxLocalSpeakingStateMessage message)
    {
        uint netId = connection?.identity != null ? connection.identity.netId : 0u;
        if (netId == 0u)
            return;

        var state = new VivoxSpeakingStateMessage
        {
            NetworkIdentityNetId = netId,
            IsSpeaking = message.IsSpeaking,
            IsMuted = message.IsMuted
        };
        serverSpeakingStates[netId] = state;
        NetworkServer.SendToAll(state);
    }

    private void OnSpeakingStateReceived(VivoxSpeakingStateMessage message)
    {
        networkSpeakingState.Apply(
            message.NetworkIdentityNetId,
            message.IsSpeaking,
            message.IsMuted);
        VoiceStateChanged?.Invoke();
    }

    private string EnsureHostSessionId()
    {
        if (!string.IsNullOrEmpty(hostSessionId))
        {
            return hostSessionId;
        }

        SteamLobby steamLobby = FindFirstObjectByType<SteamLobby>();
        hostSessionId = steamLobby != null && steamLobby.InLobby
            ? $"steam-{steamLobby.VoiceSessionId}"
            : $"kcp-{Guid.NewGuid():N}";
        return hostSessionId;
    }

    private void SetMicColor(Color color)
    {
        if (micIcon != null)
        {
            micIcon.color = color;
        }
    }

    private void SubscribeConnectionEvents()
    {
        VivoxService.Instance.ConnectionRecovering -= OnConnectionRecovering;
        VivoxService.Instance.ConnectionRecovered -= OnConnectionRecovered;
        VivoxService.Instance.ConnectionFailedToRecover -= OnConnectionFailedToRecover;
        VivoxService.Instance.ConnectionRecovering += OnConnectionRecovering;
        VivoxService.Instance.ConnectionRecovered += OnConnectionRecovered;
        VivoxService.Instance.ConnectionFailedToRecover += OnConnectionFailedToRecover;
    }

    private void UnsubscribeConnectionEvents()
    {
        VivoxService.Instance.ConnectionRecovering -= OnConnectionRecovering;
        VivoxService.Instance.ConnectionRecovered -= OnConnectionRecovered;
        VivoxService.Instance.ConnectionFailedToRecover -= OnConnectionFailedToRecover;
    }

    private void OnConnectionRecovering()
    {
        SetConnectionState(VoiceConnectionState.Recovering);
    }

    private void OnConnectionRecovered()
    {
        if (VivoxService.Instance.IsLoggedIn)
            ApplyMuteState();

        SetConnectionState(
            VivoxService.Instance.AvailableInputDevices.Count == 0
                ? VoiceConnectionState.NoInputDevice
                : VoiceConnectionState.Ready);
    }

    private void OnConnectionFailedToRecover()
    {
        SetConnectionState(VoiceConnectionState.Faulted);
    }

    private void SetConnectionState(VoiceConnectionState state)
    {
        if (ConnectionState == state)
            return;

        ConnectionState = state;
        Debug.Log($"[Vivox] State: {state}; attenuated speakers: {ActiveAttenuatedSpeakerCount}.", this);
    }
}
