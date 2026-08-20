using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InterrogationRoom.Networking;
using InterrogationRoom.Settings;
using InterrogationRoom.Steam;
using Mirror;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using Unity.Services.Vivox.AudioTaps;
using UnityEngine;
using UnityEngine.UI;

namespace InterrogationRoom.Voice
{
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

internal struct VivoxVoiceIdentityRegistrationMessage : NetworkMessage
{
    public string VivoxPlayerId;
}

internal struct VivoxVoiceIdentityMessage : NetworkMessage
{
    public string VivoxPlayerId;
    public uint NetworkIdentityNetId;
}

/// <summary>
/// Process-wide Vivox bootstrap and input-device owner. Settings can initialize
/// and select capture hardware in MainMenu without logging in or joining a
/// channel; the network voice runtime later reuses the same initialized service.
/// </summary>
public static class VivoxInputDeviceSettings
{
    private static readonly List<VoiceAudioDevice> AvailableDevices = new();
    private static Task initializationTask;
    private static bool applyingSelection;
    private static int selectionRequestVersion;
    private static bool subscribed;

    public static event Action Changed;

    public static IReadOnlyList<VoiceAudioDevice> AvailableInputDevices =>
        AvailableDevices;
    public static string ActiveInputDeviceId { get; private set; }
    public static string ActiveInputDeviceName { get; private set; }
    public static bool HasEffectiveInputDevice { get; private set; }
    public static bool IsInitialized { get; private set; }
    public static bool IsInitializing =>
        initializationTask != null && !initializationTask.IsCompleted;
    public static string LastError { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        AvailableDevices.Clear();
        initializationTask = null;
        applyingSelection = false;
        selectionRequestVersion = 0;
        subscribed = false;
        ActiveInputDeviceId = null;
        ActiveInputDeviceName = null;
        HasEffectiveInputDevice = false;
        IsInitialized = false;
        LastError = null;
        Changed = null;
    }

    public static Task EnsureInitializedAsync()
    {
        if (initializationTask == null ||
            initializationTask.IsFaulted ||
            initializationTask.IsCanceled)
        {
            initializationTask = InitializeCoreAsync();
        }

        return initializationTask;
    }

    public static async void SetPreferredInputDevice(string deviceId)
    {
        GameSettingsService.Current.SetPreferredVoiceInputDevice(deviceId);
        await EnsureInitializedAsync();
        await ApplyPreferredInputDeviceAsync();
    }

    public static async void Refresh()
    {
        await EnsureInitializedAsync();
        RefreshSnapshot();
        await ApplyPreferredInputDeviceAsync();
    }

    private static async Task InitializeCoreAsync()
    {
        LastError = null;
        Changed?.Invoke();
        try
        {
            string profileId = $"voice-device-{Guid.NewGuid():N}"[..29];
            var options = new InitializationOptions().SetProfile(profileId);
            await UnityServices.InitializeAsync(options);
            if (VivoxService.Instance.InitializationState ==
                VivoxInitializationState.Uninitialized)
            {
                await VivoxService.Instance.InitializeAsync();
            }

            Subscribe();
            IsInitialized = true;
            RefreshSnapshot();
            await ApplyPreferredInputDeviceAsync();
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            IsInitialized = false;
            Changed?.Invoke();
            throw;
        }
    }

    private static void Subscribe()
    {
        if (subscribed)
            return;

        VivoxService.Instance.AvailableInputDevicesChanged -= OnDevicesChanged;
        VivoxService.Instance.AvailableInputDevicesChanged += OnDevicesChanged;
        VivoxService.Instance.EffectiveInputDeviceChanged -= OnDevicesChanged;
        VivoxService.Instance.EffectiveInputDeviceChanged += OnDevicesChanged;
        subscribed = true;
    }

    private static async void OnDevicesChanged()
    {
        RefreshSnapshot();
        await ApplyPreferredInputDeviceAsync();
    }

    private static void RefreshSnapshot()
    {
        AvailableDevices.Clear();
        if (!IsInitialized ||
            VivoxService.Instance == null ||
            VivoxService.Instance.InitializationState ==
            VivoxInitializationState.Uninitialized)
        {
            ActiveInputDeviceId = null;
            ActiveInputDeviceName = null;
            HasEffectiveInputDevice = false;
            Changed?.Invoke();
            return;
        }

        foreach (VivoxInputDevice device in VivoxService.Instance.AvailableInputDevices)
        {
            if (IsNoInputDevice(device))
                continue;

            AvailableDevices.Add(
                new VoiceAudioDevice(device.DeviceID, device.DeviceName));
        }

        VivoxInputDevice active = VivoxService.Instance.ActiveInputDevice;
        ActiveInputDeviceId = active?.DeviceID;
        ActiveInputDeviceName = active?.DeviceName;
        HasEffectiveInputDevice =
            !IsNoInputDevice(VivoxService.Instance.EffectiveInputDevice);
        Changed?.Invoke();
    }

    private static bool IsNoInputDevice(VivoxInputDevice device) =>
        device == null ||
        !VoiceDeviceSelection.IsUsableInputDevice(
            device.DeviceID,
            device.DeviceName);

    private static async Task ApplyPreferredInputDeviceAsync()
    {
        if (!IsInitialized)
            return;

        selectionRequestVersion++;
        if (applyingSelection)
            return;

        applyingSelection = true;
        try
        {
            while (true)
            {
                int applyingVersion = selectionRequestVersion;
                try
                {
                    RefreshSnapshot();
                    string targetId = VoiceDeviceSelection.ResolveDeviceId(
                        GameSettingsService.Current.PreferredVoiceInputDeviceId,
                        ActiveInputDeviceId,
                        AvailableDevices);
                    if (!string.IsNullOrEmpty(targetId))
                    {
                        VivoxInputDevice target =
                            VivoxService.Instance.AvailableInputDevices
                                .FirstOrDefault(device =>
                                    string.Equals(
                                        device.DeviceID,
                                        targetId,
                                        StringComparison.Ordinal));
                        if (target != null &&
                            !string.Equals(
                                ActiveInputDeviceId,
                                target.DeviceID,
                                StringComparison.Ordinal))
                        {
                            await VivoxService.Instance.SetActiveInputDeviceAsync(target);
                        }
                    }

                    LastError = null;
                }
                catch (Exception exception)
                {
                    if (applyingVersion != selectionRequestVersion)
                        continue;

                    LastError = exception.Message;
                    Debug.LogWarning(
                        $"[Vivox] Could not apply preferred input device: {exception.Message}");
                    break;
                }

                if (applyingVersion == selectionRequestVersion)
                    break;
            }
        }
        finally
        {
            applyingSelection = false;
            RefreshSnapshot();
        }
    }
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
    private const int MaxVivoxPlayerIdLength = 256;

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
    private readonly VoiceParticipantPreferences participantPreferences = new();
    private readonly LocalVoicePublicationState localSpeakingPublication = new();
    private readonly VoiceSpeakingState networkSpeakingState = new();
    private readonly Dictionary<uint, VivoxSpeakingStateMessage> serverSpeakingStates = new();
    private readonly Dictionary<string, uint> networkIdentityNetIdsByVivoxPlayerId = new();
    private readonly Dictionary<string, uint> serverNetworkIdentityNetIdsByVivoxPlayerId = new();
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
    private VoiceChannelModeState channelMode;
    private bool microphoneTestActive;
    private readonly SemaphoreSlim microphoneTestTransitionGate = new(1, 1);
    private int microphoneTestRequestVersion;
    private bool isDisconnecting;
    private bool isShuttingDown;
    private TaskCompletionSource<string> pendingSessionId;

    public static VivoxVoiceRuntime Instance { get; private set; }

    public event Action VoiceStateChanged;
    public event Action VoiceDevicesChanged;

    public VoiceConnectionState ConnectionState { get; private set; } = VoiceConnectionState.WaitingForNetwork;
    public bool IsReady => isReady;
    public bool IsSpatialVoice => channelMode.ActiveSpatial;
    public bool IsLocalMicrophoneMuted =>
        GameSettingsService.Current.MicrophoneMuted || microphoneTestActive;
    public float MicrophoneLevelPercent => GameSettingsService.Current.MicrophoneLevelPercent;
    public IReadOnlyList<VoiceAudioDevice> AvailableInputDevices =>
        VivoxInputDeviceSettings.AvailableInputDevices;
    public string ActiveInputDeviceId =>
        VivoxInputDeviceSettings.ActiveInputDeviceId;
    public string ActiveInputDeviceName =>
        VivoxInputDeviceSettings.ActiveInputDeviceName;

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
        if (!isSwitchingChannel && wantsSpatialVoice != channelMode.ActiveSpatial)
            _ = SwitchVoiceChannelAsync(wantsSpatialVoice);

        if (channelMode.ActiveSpatial && Time.unscaledTime >= nextPositionUpdate)
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

            SetConnectionState(VoiceConnectionState.InitializingServices);
            await VivoxInputDeviceSettings.EnsureInitializedAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            SubscribeConnectionEvents();
            if (!VivoxInputDeviceSettings.HasEffectiveInputDevice)
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
                DisplayName = LobbyDisplayNameProvider.Resolve($"Gracz {localNetId}")
            };

            await VivoxService.Instance.LoginAsync(loginOptions);
            RegisterLocalVoiceIdentity();
            VivoxInputDeviceSettings.Refresh();
            ApplyMuteState();

            VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAdded;
            VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantRemoved;

            VivoxService.Instance.EnableAcousticEchoCancellation();
            await JoinVoiceChannelAsync(ResolveWantsSpatialVoice());
            PublishLocalSpeakingState(false, force: true);
            SetConnectionState(
                !VivoxInputDeviceSettings.HasEffectiveInputDevice
                    ? VoiceConnectionState.NoInputDevice
                    : VoiceConnectionState.Ready);
            Debug.Log($"[Vivox] Joined {(channelMode.ActiveSpatial ? "spatial" : "global")} channel '{activeChannelName}'.");
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
        bool spatial = channelMode.EffectiveSpatial;
        audioSource.spatialBlend = spatial ? 1f : 0f;
        if (spatial)
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

        if (TryResolveNetworkIdentityNetId(
                activeSessionId,
                participant.PlayerId,
                networkIdentityNetIdsByVivoxPlayerId,
                out uint netId))
        {
            participantsByNetId.Remove(netId);
        }
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

        if (!TryResolveNetworkIdentityNetId(
                activeSessionId,
                playerId,
                networkIdentityNetIdsByVivoxPlayerId,
                out uint netId))
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

    internal static bool TryResolveNetworkIdentityNetId(
        string sessionId,
        string playerId,
        IReadOnlyDictionary<string, uint> networkIdentityNetIdsByVivoxPlayerId,
        out uint netId)
    {
        netId = 0;
        if (!string.IsNullOrEmpty(playerId) &&
            networkIdentityNetIdsByVivoxPlayerId != null &&
            networkIdentityNetIdsByVivoxPlayerId.TryGetValue(playerId, out uint mappedNetId) &&
            mappedNetId != 0u)
        {
            netId = mappedNetId;
            return true;
        }

        return TryParsePlayerId(sessionId, playerId, out netId);
    }

    internal static string BuildModeChannelName(string prefix, string sessionId, bool spatial) =>
        $"{BuildChannelName(prefix, sessionId)}-{(spatial ? "round" : "lobby")}";

    public float GetParticipantVolumePercent(uint networkIdentityNetId) =>
        participantPreferences.GetVolumePercent(networkIdentityNetId);

    public void SetParticipantVolumePercent(uint networkIdentityNetId, float volumePercent)
    {
        if (!participantPreferences.SetVolumePercent(networkIdentityNetId, volumePercent))
            return;

        if (participantsByNetId.TryGetValue(networkIdentityNetId, out VivoxParticipant participant))
        {
            participant.SetLocalVolume(GameSettings.VoicePercentToVivoxVolume(
                participantPreferences.GetVolumePercent(networkIdentityNetId)));
        }
        VoiceStateChanged?.Invoke();
    }

    public bool IsParticipantLocallyMuted(uint networkIdentityNetId) =>
        participantPreferences.IsMuted(networkIdentityNetId);

    public void SetParticipantLocallyMuted(uint networkIdentityNetId, bool muted)
    {
        if (!participantPreferences.SetMuted(networkIdentityNetId, muted))
            return;

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

    public void SetPreferredInputDevice(string deviceId) =>
        VivoxInputDeviceSettings.SetPreferredInputDevice(deviceId);

    public void RefreshAudioDevices() => VivoxInputDeviceSettings.Refresh();

    public async Task<bool> SetMicrophoneTestActiveAsync(bool active)
    {
        microphoneTestActive = active;
        int requestVersion = ++microphoneTestRequestVersion;
        await microphoneTestTransitionGate.WaitAsync();
        try
        {
            if (requestVersion != microphoneTestRequestVersion)
                return false;

            // The test may have temporarily opened a user-muted capture device.
            // Restore that mute before re-enabling channel transmission.
            if (!active)
                ApplyMuteState();

            if (VivoxService.Instance.IsLoggedIn &&
                !string.IsNullOrEmpty(activeChannelName))
            {
                await VivoxService.Instance.SetChannelTransmissionModeAsync(
                    active ? TransmissionMode.None : TransmissionMode.Single,
                    active ? null : activeChannelName);
            }

            if (requestVersion != microphoneTestRequestVersion)
                return false;

            ApplyMuteState();
            VoiceStateChanged?.Invoke();
            return microphoneTestActive == active;
        }
        catch (Exception exception)
        {
            if (requestVersion == microphoneTestRequestVersion)
            {
                microphoneTestActive = false;
                ApplyMuteState();
                VoiceStateChanged?.Invoke();
            }

            Debug.LogWarning(
                $"[Vivox] Could not change microphone-test transmission: {exception.Message}",
                this);
            return false;
        }
        finally
        {
            microphoneTestTransitionGate.Release();
        }
    }

    private void OnGameSettingsChanged()
    {
        ApplyMuteState();
        VoiceStateChanged?.Invoke();
    }

    private bool ResolveWantsSpatialVoice()
    {
        if (roundCoordinator == null)
        {
            roundCoordinator = FindFirstObjectByType<NetworkRoundCoordinator>();
        }

        return roundCoordinator != null && roundCoordinator.UsesSpatialVoice;
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
                !VivoxInputDeviceSettings.HasEffectiveInputDevice
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
        channelMode = channelMode.BeginJoin(spatial);
        SetConnectionState(VoiceConnectionState.JoiningChannel);
        string previousChannelName = activeChannelName;

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

            if (microphoneTestActive)
            {
                await VivoxService.Instance.SetChannelTransmissionModeAsync(
                    TransmissionMode.None);
            }

            channelMode = channelMode.CommitJoin();
            RebuildParticipantTapsForEffectiveMode();
            isReady = true;
            ApplyMuteState();
            VoiceStateChanged?.Invoke();
            Debug.Log($"[Vivox] Voice mode: {(spatial ? "spatial Runda" : "global lobby")}.");
        }
        catch
        {
            channelMode = channelMode.RollbackJoin();
            activeChannelName =
                !string.IsNullOrEmpty(previousChannelName) &&
                VivoxService.Instance.ActiveChannels.ContainsKey(previousChannelName)
                    ? previousChannelName
                    : null;
            DestroyParticipantTaps();
            throw;
        }
        finally
        {
            isSwitchingChannel = false;
        }
    }

    private void RebuildParticipantTapsForEffectiveMode()
    {
        VivoxParticipant[] participants = participantsByNetId.Values
            .Concat(pendingParticipants.Values)
            .GroupBy(participant => participant.PlayerId)
            .Select(group => group.First())
            .ToArray();
        DestroyParticipantTaps();
        foreach (VivoxParticipant participant in participants)
        {
            if (!TryCreateParticipantTap(participant))
                pendingParticipants[participant.PlayerId] = participant;
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
        bool togglePressed =
            GameInputBindings.WasPressedThisFrameForModal(GameInputAction.VoiceMute);
        if (!togglePressed)
        {
            return;
        }

        // Muting is a local persisted preference, so the key must still work
        // while Vivox is reconnecting or unavailable. ApplyMuteState safely
        // no-ops until the service is ready, while lobby UI updates at once.
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

        bool shouldMuteCapture = MicrophoneTestPlaybackRules.ShouldMuteCapture(
            GameSettingsService.Current.MicrophoneMuted,
            microphoneTestActive);
        if (shouldMuteCapture)
        {
            VivoxService.Instance.MuteInputDevice();
            PublishLocalSpeakingState(false);
            SetMicColor(micMutedColor);
        }
        else
        {
            VivoxService.Instance.UnmuteInputDevice();
            if (microphoneTestActive)
            {
                PublishLocalSpeakingState(false);
                SetMicColor(micMutedColor);
            }
            else
            {
                SetMicColor(micNormalColor);
            }
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
        LocalVoicePublication publication =
            localSpeakingPublication.Evaluate(isSpeaking, muted, force);
        if (!publication.ShouldPublish)
            return;

        if (!NetworkClient.active || NetworkClient.connection == null)
            return;

        NetworkClient.Send(new VivoxLocalSpeakingStateMessage
        {
            IsSpeaking = publication.IsSpeaking,
            IsMuted = publication.IsMuted
        });
        localSpeakingPublication.MarkPublished(publication);
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
            ? localSpeakingPublication.IsSpeaking
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
            microphoneTestActive = false;
            microphoneTestRequestVersion++;
            DestroyParticipantTaps();
            participantPreferences.Clear();
            pendingSessionId?.TrySetCanceled();
            pendingSessionId = null;
            activeSessionId = null;
            activeChannelName = null;
            localPlayer = null;
            networkIdentityNetIdsByVivoxPlayerId.Clear();
            localSpeakingPublication.Reset();
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
            serverNetworkIdentityNetIdsByVivoxPlayerId.Clear();
        }

        wasServerActive = serverActive;

        if (serverActive && !serverHandlerRegistered)
        {
            NetworkServer.ReplaceHandler<VivoxVoiceSessionRequestMessage>(OnSessionIdRequested);
            NetworkServer.ReplaceHandler<VivoxLocalSpeakingStateMessage>(OnLocalSpeakingStateReceived);
            NetworkServer.ReplaceHandler<VivoxVoiceIdentityRegistrationMessage>(
                OnVoiceIdentityRegistrationReceived);
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
            NetworkClient.ReplaceHandler<VivoxVoiceIdentityMessage>(OnVoiceIdentityReceived);
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
            NetworkServer.UnregisterHandler<VivoxVoiceIdentityRegistrationMessage>();
            serverHandlerRegistered = false;
        }

        if (clientHandlerRegistered)
        {
            NetworkClient.UnregisterHandler<VivoxVoiceSessionResponseMessage>();
            NetworkClient.UnregisterHandler<VivoxSpeakingStateMessage>();
            NetworkClient.UnregisterHandler<VivoxVoiceIdentityMessage>();
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

        PruneDisconnectedSpeakingStates();
        foreach (VivoxSpeakingStateMessage state in serverSpeakingStates.Values)
            connection.Send(state);
    }

    private void RegisterLocalVoiceIdentity()
    {
        string vivoxPlayerId = AuthenticationService.Instance.PlayerId;
        if (!NetworkClient.active || !IsValidVivoxPlayerId(vivoxPlayerId))
            return;

        NetworkClient.Send(new VivoxVoiceIdentityRegistrationMessage
        {
            VivoxPlayerId = vivoxPlayerId
        });
    }

    private void OnVoiceIdentityRegistrationReceived(
        NetworkConnectionToClient connection,
        VivoxVoiceIdentityRegistrationMessage message)
    {
        uint netId = connection?.identity != null ? connection.identity.netId : 0u;
        if (netId == 0u || !IsValidVivoxPlayerId(message.VivoxPlayerId))
            return;

        PruneDisconnectedVoiceIdentities();

        string stalePlayerId = serverNetworkIdentityNetIdsByVivoxPlayerId
            .FirstOrDefault(entry =>
                entry.Value == netId &&
                !string.Equals(
                    entry.Key,
                    message.VivoxPlayerId,
                    StringComparison.Ordinal))
            .Key;
        if (!string.IsNullOrEmpty(stalePlayerId))
            serverNetworkIdentityNetIdsByVivoxPlayerId.Remove(stalePlayerId);

        foreach (KeyValuePair<string, uint> entry in serverNetworkIdentityNetIdsByVivoxPlayerId)
        {
            connection.Send(new VivoxVoiceIdentityMessage
            {
                VivoxPlayerId = entry.Key,
                NetworkIdentityNetId = entry.Value
            });
        }

        serverNetworkIdentityNetIdsByVivoxPlayerId[message.VivoxPlayerId] = netId;
        NetworkServer.SendToAll(new VivoxVoiceIdentityMessage
        {
            VivoxPlayerId = message.VivoxPlayerId,
            NetworkIdentityNetId = netId
        });
    }

    private void OnVoiceIdentityReceived(VivoxVoiceIdentityMessage message)
    {
        if (message.NetworkIdentityNetId == 0u ||
            !IsValidVivoxPlayerId(message.VivoxPlayerId))
        {
            return;
        }

        string stalePlayerId = networkIdentityNetIdsByVivoxPlayerId
            .FirstOrDefault(entry =>
                entry.Value == message.NetworkIdentityNetId &&
                !string.Equals(
                    entry.Key,
                    message.VivoxPlayerId,
                    StringComparison.Ordinal))
            .Key;
        if (!string.IsNullOrEmpty(stalePlayerId))
            networkIdentityNetIdsByVivoxPlayerId.Remove(stalePlayerId);

        networkIdentityNetIdsByVivoxPlayerId[message.VivoxPlayerId] =
            message.NetworkIdentityNetId;
        RetryPendingParticipants();
    }

    private static bool IsValidVivoxPlayerId(string playerId) =>
        !string.IsNullOrWhiteSpace(playerId) &&
        playerId.Length <= MaxVivoxPlayerIdLength;

    private void PruneDisconnectedVoiceIdentities()
    {
        if (serverNetworkIdentityNetIdsByVivoxPlayerId.Count == 0)
            return;

        List<string> stalePlayerIds = null;
        foreach (KeyValuePair<string, uint> entry in serverNetworkIdentityNetIdsByVivoxPlayerId)
        {
            if (!NetworkServer.spawned.ContainsKey(entry.Value))
                (stalePlayerIds ??= new List<string>()).Add(entry.Key);
        }

        if (stalePlayerIds == null)
            return;

        foreach (string playerId in stalePlayerIds)
            serverNetworkIdentityNetIdsByVivoxPlayerId.Remove(playerId);
    }

    private void PruneDisconnectedSpeakingStates()
    {
        if (serverSpeakingStates.Count == 0)
            return;

        List<uint> stale = null;
        foreach (uint netId in serverSpeakingStates.Keys)
        {
            if (!NetworkServer.spawned.ContainsKey(netId))
                (stale ??= new List<uint>()).Add(netId);
        }

        if (stale == null)
            return;

        foreach (uint netId in stale)
            serverSpeakingStates.Remove(netId);
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
        VivoxInputDeviceSettings.Changed -= OnInputDeviceSettingsChanged;
        VivoxService.Instance.ConnectionRecovering += OnConnectionRecovering;
        VivoxService.Instance.ConnectionRecovered += OnConnectionRecovered;
        VivoxService.Instance.ConnectionFailedToRecover += OnConnectionFailedToRecover;
        VivoxInputDeviceSettings.Changed += OnInputDeviceSettingsChanged;
    }

    private void UnsubscribeConnectionEvents()
    {
        VivoxService.Instance.ConnectionRecovering -= OnConnectionRecovering;
        VivoxService.Instance.ConnectionRecovered -= OnConnectionRecovered;
        VivoxService.Instance.ConnectionFailedToRecover -= OnConnectionFailedToRecover;
        VivoxInputDeviceSettings.Changed -= OnInputDeviceSettingsChanged;
    }

    private void OnConnectionRecovering()
    {
        SetConnectionState(VoiceConnectionState.Recovering);
    }

    private void OnConnectionRecovered()
    {
        VivoxInputDeviceSettings.Refresh();
        if (VivoxService.Instance.IsLoggedIn)
            ApplyMuteState();

        SetConnectionState(
            !VivoxInputDeviceSettings.HasEffectiveInputDevice
                ? VoiceConnectionState.NoInputDevice
                : VoiceConnectionState.Ready);
    }

    private void OnConnectionFailedToRecover()
    {
        SetConnectionState(VoiceConnectionState.Faulted);
    }

    private void OnInputDeviceSettingsChanged()
    {
        VoiceDevicesChanged?.Invoke();
        if (!isReady)
            return;

        if (!VivoxInputDeviceSettings.HasEffectiveInputDevice)
        {
            SetConnectionState(VoiceConnectionState.NoInputDevice);
        }
        else if (ConnectionState == VoiceConnectionState.NoInputDevice)
        {
            SetConnectionState(VoiceConnectionState.Ready);
        }
    }

    private void SetConnectionState(VoiceConnectionState state)
    {
        if (ConnectionState == state)
            return;

        ConnectionState = state;
        Debug.Log($"[Vivox] State: {state}; attenuated speakers: {ActiveAttenuatedSpeakerCount}.", this);
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class VivoxMicrophoneTestPlayback : MonoBehaviour
{
    private const float StartupTimeoutSeconds = 2f;

    private VivoxCaptureSourceTap captureTap;
    private float startupStartedAt;
    private volatile float monitorGain = 1f;
    private volatile bool monitorAudio;
    private int operationRevision;

    public event Action StateChanged;

    public MicrophoneTestState State { get; private set; }

    private void Awake()
    {
        captureTap = gameObject.AddComponent<VivoxCaptureSourceTap>();
        captureTap.enabled = false;
    }

    private void Update()
    {
        if (State == MicrophoneTestState.Starting)
        {
            if (captureTap != null && captureTap.IsRunning)
            {
                monitorAudio = true;
                SetState(MicrophoneTestState.Monitoring);
            }
            else if (Time.unscaledTime - startupStartedAt >= StartupTimeoutSeconds)
            {
                StopMonitoring(MicrophoneTestState.Failed);
            }
        }
        else if (State == MicrophoneTestState.Monitoring &&
                 (captureTap == null || !captureTap.IsRunning))
        {
            StopMonitoring(MicrophoneTestState.Failed);
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!monitorAudio)
        {
            Array.Clear(data, 0, data.Length);
            return;
        }

        float gain = monitorGain;
        for (int index = 0; index < data.Length; index++)
            data[index] = Mathf.Clamp(data[index] * gain, -1f, 1f);
    }

    private void OnDisable()
    {
        Cancel();
    }

    public async void StartOrStop()
    {
        if (State == MicrophoneTestState.Starting ||
            State == MicrophoneTestState.Monitoring)
        {
            StopMonitoring(MicrophoneTestState.Idle);
            return;
        }

        VivoxVoiceRuntime runtime = VivoxVoiceRuntime.Instance;
        if (runtime == null ||
            !runtime.IsReady ||
            !VivoxInputDeviceSettings.HasEffectiveInputDevice)
        {
            SetState(MicrophoneTestState.NoInputDevice);
            return;
        }

        int revision = ++operationRevision;
        monitorAudio = false;
        SetState(MicrophoneTestState.Starting);

        bool transmissionPrepared =
            await runtime.SetMicrophoneTestActiveAsync(true);
        if (revision != operationRevision ||
            State != MicrophoneTestState.Starting)
        {
            return;
        }

        if (!transmissionPrepared)
        {
            StopMonitoring(MicrophoneTestState.Failed);
            return;
        }

        startupStartedAt = Time.unscaledTime;
        captureTap.enabled = true;
    }

    public void Cancel() => StopMonitoring(MicrophoneTestState.Idle);

    public void RefreshInputAvailability(bool hasInputDevice)
    {
        if (!hasInputDevice &&
            (State == MicrophoneTestState.Starting ||
             State == MicrophoneTestState.Monitoring))
        {
            StopMonitoring(MicrophoneTestState.NoInputDevice);
        }
        else if (hasInputDevice && State == MicrophoneTestState.NoInputDevice)
        {
            SetState(MicrophoneTestState.Idle);
        }
    }

    public void SetLevelPercent(float percent)
    {
        monitorGain = GameSettings.ClampVoicePercent(percent) / 100f;
    }

    private void StopMonitoring(MicrophoneTestState finalState)
    {
        operationRevision++;
        monitorAudio = false;
        if (captureTap != null)
            captureTap.enabled = false;
        if (VivoxVoiceRuntime.Instance != null)
            _ = VivoxVoiceRuntime.Instance.SetMicrophoneTestActiveAsync(false);
        SetState(finalState);
    }

    private void SetState(MicrophoneTestState state)
    {
        if (State == state)
            return;

        State = state;
        StateChanged?.Invoke();
    }
}
}
