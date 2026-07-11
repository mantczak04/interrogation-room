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
public sealed class VivoxTest : MonoBehaviour
{
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

    private GameObject localPlayer;
    private string activeChannelName;
    private float nextPositionUpdate;
    private bool isMuted;
    private bool isReady;
    private bool isJoining;
    private bool isDisconnecting;
    private bool isShuttingDown;

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

            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            if (VivoxService.Instance.InitializationState == VivoxInitializationState.Uninitialized)
            {
                await VivoxService.Instance.InitializeAsync();
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

            await VivoxService.Instance.JoinPositionalChannelAsync(
                activeChannelName,
                ChatCapability.AudioOnly,
                new Channel3DProperties());

            VivoxService.Instance.Set3DPosition(localPlayer, activeChannelName);
            nextPositionUpdate = Time.unscaledTime + positionUpdateInterval;
            isReady = true;
            Debug.Log($"[Vivox] Joined positional channel '{activeChannelName}'.");
        }
        catch (Exception exception)
        {
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

        if (!TryGetNetworkIdentity(participant.PlayerId, out NetworkIdentity identity))
        {
            Debug.LogWarning($"[Vivox] Cannot map participant '{participant.PlayerId}' to a Mirror player.");
            return;
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
    }

    private void OnParticipantRemoved(VivoxParticipant participant)
    {
        if (!participantTaps.Remove(participant.PlayerId, out GameObject tapObject))
        {
            return;
        }

        if (tapObject != null)
        {
            Destroy(tapObject);
        }
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
            activeChannelName = null;
            localPlayer = null;
            isDisconnecting = false;
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
}
