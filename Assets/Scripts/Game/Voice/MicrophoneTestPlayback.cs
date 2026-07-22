using System;
using UnityEngine;

namespace InterrogationRoom.Voice
{
    [DisallowMultipleComponent]
    public sealed class MicrophoneTestPlayback : MonoBehaviour
    {
        public enum TestState
        {
            Idle,
            Starting,
            Monitoring,
            NoInputDevice,
            Failed
        }

        private const int SampleRate = 44100;
        private const int BufferSeconds = 2;
        private const float StartupTimeoutSeconds = 2f;

        private AudioSource playbackSource;
        private AudioClip microphoneClip;
        private float startupStartedAt;
        private int targetLatencySamples;
        private int minimumSafeGapSamples;
        private volatile float monitorGain = 1f;
        private volatile bool monitorAudio;

        public event Action StateChanged;

        public TestState State { get; private set; }

        private void Awake()
        {
            playbackSource = gameObject.AddComponent<AudioSource>();
            playbackSource.playOnAwake = false;
            playbackSource.loop = true;
            playbackSource.spatialBlend = 0f;
        }

        private void Update()
        {
            if (State != TestState.Starting && State != TestState.Monitoring)
                return;

            int writePosition = Microphone.GetPosition(null);
            if (writePosition < 0)
            {
                StopMonitoring(TestState.Failed);
                return;
            }

            if (State == TestState.Starting)
            {
                if (writePosition >= targetLatencySamples)
                    BeginPlayback(writePosition);
                else if (
                Time.unscaledTime - startupStartedAt >= StartupTimeoutSeconds)
                {
                    StopMonitoring(TestState.Failed);
                }
                return;
            }

            if (microphoneClip != null &&
                playbackSource.isPlaying &&
                MicrophoneMonitorBuffer.RequiresResync(
                    writePosition,
                    playbackSource.timeSamples,
                    microphoneClip.samples,
                    minimumSafeGapSamples))
            {
                playbackSource.timeSamples = MicrophoneMonitorBuffer.CalculateReadPosition(
                    writePosition,
                    targetLatencySamples,
                    microphoneClip.samples);
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
            if (State == TestState.Starting || State == TestState.Monitoring)
            {
                StopMonitoring(TestState.Idle);
                return;
            }

            StartMonitoring();
        }

        public void Cancel() => StopMonitoring(TestState.Idle);

        public void SetLevelPercent(float percent)
        {
            monitorGain = Mathf.Clamp(percent, 0f, 200f) / 100f;
        }

        private void StartMonitoring()
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                SetState(TestState.NoInputDevice);
                return;
            }

            ReleaseMicrophoneClip();
            microphoneClip = Microphone.Start(null, true, BufferSeconds, SampleRate);
            if (microphoneClip == null)
            {
                SetState(TestState.Failed);
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
            monitorAudio = false;
            SetState(TestState.Starting);
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
            SetState(TestState.Monitoring);
        }

        private void StopMonitoring(TestState finalState)
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

        private void SetState(TestState state)
        {
            if (State == state)
                return;

            State = state;
            StateChanged?.Invoke();
        }
    }
}
