using System;
using InterrogationRoom.Settings;
using UnityEngine;

namespace InterrogationRoom.Voice
{
    [DisallowMultipleComponent]
    public sealed class MicrophoneTestPlayback : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const int BufferSeconds = 2;
        private const float StartupTimeoutSeconds = 2f;
        private const float WritePositionStallTimeoutSeconds = 1f;

        private AudioSource playbackSource;
        private AudioClip microphoneClip;
        private float startupStartedAt;
        private float lastWritePositionProgressAt;
        private int lastWritePosition = -1;
        private int targetLatencySamples;
        private int minimumSafeGapSamples;
        private volatile float monitorGain = 1f;
        private volatile bool monitorAudio;

        public event Action StateChanged;

        public MicrophoneTestState State { get; private set; }

        private void Awake()
        {
            playbackSource = gameObject.AddComponent<AudioSource>();
            playbackSource.playOnAwake = false;
            playbackSource.loop = true;
            playbackSource.spatialBlend = 0f;
        }

        private void Update()
        {
            if (State != MicrophoneTestState.Starting &&
                State != MicrophoneTestState.Monitoring)
                return;

            int writePosition = Microphone.GetPosition(null);
            if (writePosition != lastWritePosition)
            {
                lastWritePosition = writePosition;
                lastWritePositionProgressAt = Time.unscaledTime;
            }

            MicrophoneTestTransition transition = MicrophoneTestPlaybackRules.Update(
                State,
                Microphone.IsRecording(null),
                writePosition,
                targetLatencySamples,
                Time.unscaledTime - startupStartedAt,
                StartupTimeoutSeconds,
                microphoneClip != null && playbackSource.isPlaying,
                Time.unscaledTime - lastWritePositionProgressAt,
                WritePositionStallTimeoutSeconds,
                playbackSource.timeSamples,
                microphoneClip != null ? microphoneClip.samples : 0,
                minimumSafeGapSamples);
            switch (transition.Action)
            {
                case MicrophoneTestAction.BeginPlayback:
                    BeginPlayback(writePosition);
                    break;
                case MicrophoneTestAction.ResyncPlayback:
                    playbackSource.timeSamples = MicrophoneMonitorBuffer.CalculateReadPosition(
                        writePosition,
                        targetLatencySamples,
                        microphoneClip.samples);
                    break;
                case MicrophoneTestAction.StopCapture:
                    StopMonitoring(transition.State);
                    break;
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!monitorAudio)
                return;

            float gain = monitorGain;
            for (int index = 0; index < data.Length; index++)
                data[index] = Mathf.Clamp(data[index] * gain, -1f, 1f);
        }

        private void OnDisable()
        {
            Cancel();
        }

        public void StartOrStop()
        {
            if (State == MicrophoneTestState.Starting ||
                State == MicrophoneTestState.Monitoring)
            {
                StopMonitoring(MicrophoneTestState.Idle);
                return;
            }

            StartMonitoring();
        }

        public void Cancel() => StopMonitoring(MicrophoneTestState.Idle);

        public void SetLevelPercent(float percent)
        {
            monitorGain = GameSettings.ClampVoicePercent(percent) / 100f;
        }

        private void StartMonitoring()
        {
            bool hasInputDevice = Microphone.devices != null &&
                Microphone.devices.Length > 0;
            if (!hasInputDevice)
            {
                SetState(MicrophoneTestPlaybackRules.Start(
                    hasInputDevice: false,
                    captureStarted: false).State);
                return;
            }

            ReleaseMicrophoneClip();
            microphoneClip = Microphone.Start(null, true, BufferSeconds, SampleRate);
            MicrophoneTestTransition start = MicrophoneTestPlaybackRules.Start(
                hasInputDevice: true,
                captureStarted: microphoneClip != null);
            if (start.State != MicrophoneTestState.Starting)
            {
                SetState(start.State);
                return;
            }

            AudioSettings.GetDSPBufferSize(out int dspBufferLength, out int dspBufferCount);
            targetLatencySamples = MicrophoneMonitorBuffer.CalculateTargetLatencySamples(
                microphoneClip.frequency,
                dspBufferLength,
                dspBufferCount);
            targetLatencySamples = Mathf.Min(
                targetLatencySamples,
                Mathf.Max(1, microphoneClip.samples / 2));
            minimumSafeGapSamples = Mathf.Max(1, targetLatencySamples / 2);
            startupStartedAt = Time.unscaledTime;
            lastWritePositionProgressAt = startupStartedAt;
            lastWritePosition = -1;
            monitorAudio = false;
            SetState(start.State);
        }

        private void BeginPlayback(int writePosition)
        {
            playbackSource.clip = microphoneClip;
            playbackSource.timeSamples = MicrophoneMonitorBuffer.CalculateReadPosition(
                writePosition,
                targetLatencySamples,
                microphoneClip.samples);
            monitorAudio = true;
            playbackSource.Play();
            SetState(MicrophoneTestState.Monitoring);
        }

        private void StopMonitoring(MicrophoneTestState finalState)
        {
            monitorAudio = false;
            if (playbackSource != null)
                playbackSource.Stop();
            if (Microphone.IsRecording(null))
                Microphone.End(null);
            ReleaseMicrophoneClip();
            SetState(finalState);
        }

        private void ReleaseMicrophoneClip()
        {
            if (microphoneClip != null)
                Destroy(microphoneClip);
            microphoneClip = null;
            if (playbackSource != null)
                playbackSource.clip = null;
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
