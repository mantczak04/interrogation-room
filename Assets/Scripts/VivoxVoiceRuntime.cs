using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
}

internal struct VivoxSpeakingStateMessage : NetworkMessage
{
    public uint NetworkIdentityNetId;
    public bool IsSpeaking;
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
    private const float MaxAudibleDistance = 18f;

    [Header("Session")]
    [SerializeField] private string channelPrefix = "interrogation-room";
    [SerializeField, Min(0.1f)] private float positionUpdateInterval = 0.3f;

    [Header("Playback")]
    [SerializeField, Min(1f)] private float audibleDistance = 15f;
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
    private readonly VoiceSpeakingState networkSpeakingState = new();

    private GameObject localPlayer;
    private string activeSessionId;
    private string activeChannelName;
    private string hostSessionId;
    private float nextPositionUpdate;
    private bool wasServerActive;
    private bool serverHandlerRegistered;
    private bool clientHandlerRegistered;
    private bool isMuted;
    private bool isReady;
    private bool isJoining;
    private bool isDisconnecting;
    private bool isShuttingDown;
    private bool localSpeechDetected;
    private bool localSpeakingStateSent;
    private TaskCompletionSource<string> pendingSessionId;

    public VoiceConnectionState ConnectionState { get; private set; } = VoiceConnectionState.WaitingForNetwork;

    private float EffectiveAudibleDistance => Mathf.Min(audibleDistance, MaxAudibleDistance);

    public int ActiveAttenuatedSpeakerCount => participantTaps.Values.Count(tap =>
        tap != null &&
        tap.TryGetComponent(out VivoxVoiceOcclusion occlusion) &&
        occlusion.IsActivelyAttenuated);

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

        if (Time.unscaledTime >= nextPositionUpdate)
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

            SetConnectionState(VoiceConnectionState.JoiningChannel);
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
            isReady = true;
            PublishLocalSpeakingState(false, force: true);
            SetConnectionState(
                VivoxService.Instance.AvailableInputDevices.Count == 0
                    ? VoiceConnectionState.NoInputDevice
                    : VoiceConnectionState.Ready);
            Debug.Log($"[Vivox] Joined positional channel '{activeChannelName}'.");
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
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Custom;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = maxDistance;
        audioSource.SetCustomCurve(
            AudioSourceCurveType.CustomRolloff,
            VoiceAudibilityModel.BuildDistanceRolloffCurve(conversationalDistance, maxDistance));

        VivoxVoiceOcclusion occlusion = tapObject.AddComponent<VivoxVoiceOcclusion>();
        occlusion.Configure(localPlayer.transform, identity.transform, audioSource, occlusionMask);

        participantTaps[participant.PlayerId] = tapObject;
        pendingParticipants.Remove(participant.PlayerId);
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

        isMuted = !isMuted;
        ApplyMuteState();
    }

    private void ApplyMuteState()
    {
        if (isMuted)
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
        if (isMuted || string.IsNullOrEmpty(activeChannelName))
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
        bool normalizedState = !isMuted && isSpeaking;
        if (!force && localSpeakingStateSent && localSpeechDetected == normalizedState)
            return;

        localSpeechDetected = normalizedState;
        if (!NetworkClient.active || NetworkClient.connection == null)
            return;

        NetworkClient.Send(new VivoxLocalSpeakingStateMessage
        {
            IsSpeaking = normalizedState
        });
        localSpeakingStateSent = true;
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
            participantTaps.Clear();
            pendingParticipants.Clear();
            pendingSessionId?.TrySetCanceled();
            pendingSessionId = null;
            activeSessionId = null;
            activeChannelName = null;
            localPlayer = null;
            localSpeechDetected = false;
            localSpeakingStateSent = false;
            networkSpeakingState.Clear();
            isDisconnecting = false;
            if (!isShuttingDown)
                SetConnectionState(VoiceConnectionState.Disconnected);
        }
    }

    private async void OnDestroy()
    {
        isShuttingDown = true;
        UnregisterNetworkMessageHandlers();
        await DisconnectAsync(logout: true);
    }

    private void RefreshNetworkMessageHandlers()
    {
        bool serverActive = NetworkServer.active;
        if (wasServerActive && !serverActive)
        {
            hostSessionId = null;
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

        NetworkServer.SendToAll(new VivoxSpeakingStateMessage
        {
            NetworkIdentityNetId = netId,
            IsSpeaking = message.IsSpeaking
        });
    }

    private void OnSpeakingStateReceived(VivoxSpeakingStateMessage message)
    {
        networkSpeakingState.Apply(message.NetworkIdentityNetId, message.IsSpeaking);
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
