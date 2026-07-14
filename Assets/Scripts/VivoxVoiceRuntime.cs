using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mirror;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

    [Header("Session")]
    [SerializeField] private string channelPrefix = "interrogation-room";
    [SerializeField, Min(0.1f)] private float positionUpdateInterval = 0.3f;

    [Header("Playback")]
    [SerializeField, Min(1f)] private float audibleDistance = 32f;
    [SerializeField] private LayerMask occlusionMask = ~0;

    [Header("UI")]
    [SerializeField] private Image micIcon;
    [SerializeField] private Color micNormalColor = Color.white;
    [SerializeField] private Color micSpeakingColor = Color.green;
    [SerializeField] private Color micMutedColor = Color.red;

    private readonly Dictionary<string, GameObject> participantTaps = new();
    private readonly Dictionary<string, VivoxParticipant> pendingParticipants = new();

    private GameObject localPlayer;
    private string activeChannelName;
    private float nextPositionUpdate;
    private bool isMuted;
    private bool isReady;
    private bool isJoining;
    private bool isDisconnecting;
    private bool isShuttingDown;

    public VoiceConnectionState ConnectionState { get; private set; } = VoiceConnectionState.WaitingForNetwork;

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
        HandleMuteInput();

        if (isReady && (localPlayer == null || !NetworkClient.active))
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
            activeChannelName = BuildChannelName();

            SetConnectionState(VoiceConnectionState.InitializingServices);
            var initializationOptions = new InitializationOptions()
                .SetProfile(BuildPlayerId(NetworkClient.localPlayer.netId));
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

            if (!VivoxService.Instance.IsLoggedIn)
            {
                var loginOptions = new LoginOptions
                {
                    PlayerId = BuildPlayerId(NetworkClient.localPlayer.netId),
                    DisplayName = $"Player {NetworkClient.localPlayer.netId}"
                };

                await VivoxService.Instance.LoginAsync(loginOptions);
            }

            VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAdded;
            VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantRemoved;

            SetConnectionState(VoiceConnectionState.JoiningChannel);
            await VivoxService.Instance.JoinPositionalChannelAsync(
                activeChannelName,
                ChatCapability.AudioOnly,
                new Channel3DProperties());

            VivoxService.Instance.Set3DPosition(localPlayer, activeChannelName);
            nextPositionUpdate = Time.unscaledTime + positionUpdateInterval;
            isReady = true;
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

    private string BuildChannelName()
    {
        SteamLobby steamLobby = FindFirstObjectByType<SteamLobby>();
        string sessionId = steamLobby != null ? steamLobby.VoiceSessionId : "local";
        return $"{channelPrefix}-{sessionId}";
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
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = audibleDistance;

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

    private static bool TryGetNetworkIdentity(string playerId, out NetworkIdentity identity)
    {
        identity = null;

        if (string.IsNullOrEmpty(playerId) ||
            !playerId.StartsWith(PlayerIdPrefix, StringComparison.Ordinal) ||
            !uint.TryParse(playerId[PlayerIdPrefix.Length..], out uint netId))
        {
            return false;
        }

        return NetworkClient.spawned.TryGetValue(netId, out identity);
    }

    private static string BuildPlayerId(uint netId)
    {
        return $"{PlayerIdPrefix}{netId}";
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

        if (isMuted)
        {
            VivoxService.Instance.MuteInputDevice();
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
        if (isMuted || micIcon == null || string.IsNullOrEmpty(activeChannelName))
        {
            return;
        }

        bool isSpeaking = VivoxService.Instance.ActiveChannels.TryGetValue(
                activeChannelName,
                out var participants) &&
            participants.FirstOrDefault(participant => participant.IsSelf)?.SpeechDetected == true;

        SetMicColor(isSpeaking ? micSpeakingColor : micNormalColor);
    }

    private async Task DisconnectAndWaitForReconnectAsync()
    {
        await DisconnectAsync(logout: false);

        if (!isShuttingDown)
        {
            await WaitForLocalPlayerAndConnectAsync();
        }
    }

    private async Task DisconnectAsync(bool logout)
    {
        if (isDisconnecting ||
            VivoxService.Instance.InitializationState == VivoxInitializationState.Uninitialized)
        {
            return;
        }

        isDisconnecting = true;
        isReady = false;
        VivoxService.Instance.ParticipantAddedToChannel -= OnParticipantAdded;
        VivoxService.Instance.ParticipantRemovedFromChannel -= OnParticipantRemoved;
        UnsubscribeConnectionEvents();

        try
        {
            if (!string.IsNullOrEmpty(activeChannelName) &&
                VivoxService.Instance.ActiveChannels.ContainsKey(activeChannelName))
            {
                await VivoxService.Instance.LeaveChannelAsync(activeChannelName);
            }

            if (logout && VivoxService.Instance.IsLoggedIn)
            {
                await VivoxService.Instance.LogoutAsync();
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
            activeChannelName = null;
            localPlayer = null;
            isDisconnecting = false;
            if (!isShuttingDown)
                SetConnectionState(VoiceConnectionState.Disconnected);
        }
    }

    private async void OnDestroy()
    {
        isShuttingDown = true;
        await DisconnectAsync(logout: true);
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
