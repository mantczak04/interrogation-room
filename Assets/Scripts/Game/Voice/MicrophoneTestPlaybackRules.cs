using System;
using System.Collections.Generic;

namespace InterrogationRoom.Voice
{
    public enum MicrophoneTestState
    {
        Idle,
        Starting,
        Monitoring,
        NoInputDevice,
        Failed
    }

    public enum MicrophoneTestAction
    {
        None,
        BeginPlayback,
        ResyncPlayback,
        StopCapture
    }

    public readonly struct MicrophoneTestTransition
    {
        public MicrophoneTestState State { get; }
        public MicrophoneTestAction Action { get; }

        public MicrophoneTestTransition(
            MicrophoneTestState state,
            MicrophoneTestAction action = MicrophoneTestAction.None)
        {
            State = state;
            Action = action;
        }
    }

    public static class MicrophoneTestPlaybackRules
    {
        public static bool ShouldMuteCapture(
            bool userMuted,
            bool microphoneTestActive) =>
            userMuted && !microphoneTestActive;

        public static MicrophoneTestTransition Start(
            bool hasInputDevice,
            bool captureStarted)
        {
            if (!hasInputDevice)
                return new MicrophoneTestTransition(MicrophoneTestState.NoInputDevice);

            return captureStarted
                ? new MicrophoneTestTransition(MicrophoneTestState.Starting)
                : new MicrophoneTestTransition(MicrophoneTestState.Failed);
        }

        public static MicrophoneTestTransition Update(
            MicrophoneTestState state,
            bool captureIsRecording,
            int writePosition,
            int targetLatencySamples,
            float startupElapsedSeconds,
            float startupTimeoutSeconds,
            bool playbackIsRunning,
            float writePositionStalledSeconds,
            float writePositionStallTimeoutSeconds,
            int readPosition,
            int capacity,
            int minimumSafeGapSamples)
        {
            if (state != MicrophoneTestState.Starting &&
                state != MicrophoneTestState.Monitoring)
            {
                return new MicrophoneTestTransition(state);
            }

            if (!captureIsRecording ||
                writePosition < 0 ||
                writePositionStalledSeconds >= writePositionStallTimeoutSeconds)
            {
                return new MicrophoneTestTransition(
                    MicrophoneTestState.Failed,
                    MicrophoneTestAction.StopCapture);
            }

            if (state == MicrophoneTestState.Starting)
            {
                if (writePosition >= targetLatencySamples)
                {
                    return new MicrophoneTestTransition(
                        MicrophoneTestState.Monitoring,
                        MicrophoneTestAction.BeginPlayback);
                }

                if (startupElapsedSeconds >= startupTimeoutSeconds)
                {
                    return new MicrophoneTestTransition(
                        MicrophoneTestState.Failed,
                        MicrophoneTestAction.StopCapture);
                }

                return new MicrophoneTestTransition(MicrophoneTestState.Starting);
            }

            if (!playbackIsRunning)
            {
                return new MicrophoneTestTransition(
                    MicrophoneTestState.Failed,
                    MicrophoneTestAction.StopCapture);
            }

            bool requiresResync = MicrophoneMonitorBuffer.RequiresResync(
                    writePosition,
                    readPosition,
                    capacity,
                    minimumSafeGapSamples);
            return new MicrophoneTestTransition(
                MicrophoneTestState.Monitoring,
                requiresResync
                    ? MicrophoneTestAction.ResyncPlayback
                    : MicrophoneTestAction.None);
        }
    }

    public readonly struct VoiceAudioDevice
    {
        public string Id { get; }
        public string Name { get; }

        public VoiceAudioDevice(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public static class VoiceDeviceSelection
    {
        public static bool IsUsableInputDevice(
            string deviceId,
            string deviceName) =>
            !string.Equals(
                deviceName,
                "No Device",
                StringComparison.OrdinalIgnoreCase) &&
            (!string.IsNullOrWhiteSpace(deviceId) ||
             !string.IsNullOrWhiteSpace(deviceName));

        public static string ResolveDeviceId(
            string preferredDeviceId,
            string activeDeviceId,
            IReadOnlyList<VoiceAudioDevice> availableDevices)
        {
            if (availableDevices == null || availableDevices.Count == 0)
                return null;

            string preferred = FindDeviceId(preferredDeviceId, availableDevices);
            if (preferred != null)
                return preferred;

            string active = FindDeviceId(activeDeviceId, availableDevices);
            if (active != null)
                return active;

            for (int index = 0; index < availableDevices.Count; index++)
            {
                if (!string.IsNullOrWhiteSpace(availableDevices[index].Id))
                    return availableDevices[index].Id;
            }

            return null;
        }

        private static string FindDeviceId(
            string requestedId,
            IReadOnlyList<VoiceAudioDevice> availableDevices)
        {
            if (string.IsNullOrWhiteSpace(requestedId))
                return null;

            for (int index = 0; index < availableDevices.Count; index++)
            {
                string candidate = availableDevices[index].Id;
                if (string.Equals(candidate, requestedId, StringComparison.Ordinal))
                    return candidate;
            }

            return null;
        }
    }
}
