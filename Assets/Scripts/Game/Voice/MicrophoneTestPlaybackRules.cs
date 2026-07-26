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
}
