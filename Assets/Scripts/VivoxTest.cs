using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using System.Linq;

public class VivoxTest : MonoBehaviour
{
    [Header("UI Settings")]
    public Image micIcon;
    public Color micNormalColor = Color.white;   
    public Color micSpeakingColor = Color.green; 
    public Color micMutedColor = Color.red;      

    private bool isMuted = false;

    async void Start()
    {
        Debug.Log("[Vivox] 1. Connecting...");
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        await VivoxService.Instance.LoginAsync();
        await VivoxService.Instance.JoinEchoChannelAsync("TestChannel", ChatCapability.AudioOnly);

        Debug.Log("[Vivox] READY! Press 'V' to toggle mute.");

        if (micIcon != null)
        {
            micIcon.color = micNormalColor;
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            ToggleMute();
        }

        if (!isMuted && micIcon != null)
        {
            CheckVoiceActivity();
        }
    }

    private void ToggleMute()
    {
        isMuted = !isMuted;

        if (isMuted)
        {
            VivoxService.Instance.MuteInputDevice();
            if (micIcon != null) micIcon.color = micMutedColor;
            Debug.Log("[Vivox] Muted (Red)");
        }
        else
        {
            VivoxService.Instance.UnmuteInputDevice();
            if (micIcon != null) micIcon.color = micNormalColor;
            Debug.Log("[Vivox] Unmuted (White)");
        }
    }

    private void CheckVoiceActivity()
    {
        bool isSpeaking = false;

        if (VivoxService.Instance.ActiveChannels.Count > 0)
        {
            var channel = VivoxService.Instance.ActiveChannels.FirstOrDefault().Value;

            if (channel != null)
            {
                var me = channel.FirstOrDefault(p => p.PlayerId == AuthenticationService.Instance.PlayerId);

                if (me != null && me.SpeechDetected)
                {
                    isSpeaking = true;
                }
            }
        }

        micIcon.color = isSpeaking ? micSpeakingColor : micNormalColor;
    }
}