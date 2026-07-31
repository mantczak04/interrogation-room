using NUnit.Framework;

namespace InterrogationRoom.Voice.Tests
{
    public sealed class MicrophoneTestPlaybackRulesTests
    {
        [TestCase(true, false, true)]
        [TestCase(true, true, false)]
        [TestCase(false, false, false)]
        [TestCase(false, true, false)]
        public void CaptureRemainsOpenForLocalMonitorWithoutChangingUserMute(
            bool userMuted,
            bool microphoneTestActive,
            bool expectedMute)
        {
            Assert.That(
                MicrophoneTestPlaybackRules.ShouldMuteCapture(
                    userMuted,
                    microphoneTestActive),
                Is.EqualTo(expectedMute));
        }

        [TestCase(false, false, MicrophoneTestState.NoInputDevice)]
        [TestCase(true, false, MicrophoneTestState.Failed)]
        [TestCase(true, true, MicrophoneTestState.Starting)]
        public void StartReportsCaptureAvailability(
            bool hasInputDevice,
            bool captureStarted,
            MicrophoneTestState expected)
        {
            Assert.That(
                MicrophoneTestPlaybackRules.Start(hasInputDevice, captureStarted).State,
                Is.EqualTo(expected));
        }

        [Test]
        public void StartupBeginsPlaybackAfterLatencyBufferFills()
        {
            MicrophoneTestTransition transition = Update(
                MicrophoneTestState.Starting,
                writePosition: 5292,
                elapsed: 0.5f);

            Assert.That(transition.State, Is.EqualTo(MicrophoneTestState.Monitoring));
            Assert.That(transition.Action, Is.EqualTo(MicrophoneTestAction.BeginPlayback));
        }

        [Test]
        public void StartupFailsWhenCaptureDoesNotAdvanceBeforeTimeout()
        {
            MicrophoneTestTransition transition = Update(
                MicrophoneTestState.Starting,
                writePosition: 0,
                elapsed: 2f);

            Assert.That(transition.State, Is.EqualTo(MicrophoneTestState.Failed));
            Assert.That(transition.Action, Is.EqualTo(MicrophoneTestAction.StopCapture));
        }

        [Test]
        public void InvalidCapturePositionFailsAnActiveTest()
        {
            MicrophoneTestTransition transition = Update(
                MicrophoneTestState.Monitoring,
                writePosition: -1,
                elapsed: 0f);

            Assert.That(transition.State, Is.EqualTo(MicrophoneTestState.Failed));
            Assert.That(transition.Action, Is.EqualTo(MicrophoneTestAction.StopCapture));
        }

        [Test]
        public void CaptureStoppingFailsAnActiveTest()
        {
            MicrophoneTestTransition transition = Update(
                MicrophoneTestState.Monitoring,
                writePosition: 6000,
                elapsed: 0f,
                captureIsRecording: false);

            Assert.That(transition.State, Is.EqualTo(MicrophoneTestState.Failed));
            Assert.That(transition.Action, Is.EqualTo(MicrophoneTestAction.StopCapture));
        }

        [Test]
        public void PlaybackStoppingAfterLaunchFailsAnActiveTest()
        {
            MicrophoneTestTransition transition = Update(
                MicrophoneTestState.Monitoring,
                writePosition: 6000,
                elapsed: 0f,
                playbackIsRunning: false);

            Assert.That(transition.State, Is.EqualTo(MicrophoneTestState.Failed));
            Assert.That(transition.Action, Is.EqualTo(MicrophoneTestAction.StopCapture));
        }

        [Test]
        public void StalledWritePositionFailsAnActiveTest()
        {
            MicrophoneTestTransition transition = Update(
                MicrophoneTestState.Monitoring,
                writePosition: 6000,
                elapsed: 0f,
                stalledSeconds: 1f);

            Assert.That(transition.State, Is.EqualTo(MicrophoneTestState.Failed));
            Assert.That(transition.Action, Is.EqualTo(MicrophoneTestAction.StopCapture));
        }

        [TestCase(708, MicrophoneTestAction.None)]
        [TestCase(5900, MicrophoneTestAction.ResyncPlayback)]
        public void MonitoringRequestsResyncOnlyOutsideTheSafeBufferWindow(
            int readPosition,
            MicrophoneTestAction expected)
        {
            MicrophoneTestTransition transition = MicrophoneTestPlaybackRules.Update(
                MicrophoneTestState.Monitoring,
                captureIsRecording: true,
                writePosition: 6000,
                targetLatencySamples: 5292,
                startupElapsedSeconds: 10f,
                startupTimeoutSeconds: 2f,
                playbackIsRunning: true,
                writePositionStalledSeconds: 0f,
                writePositionStallTimeoutSeconds: 1f,
                readPosition,
                capacity: 44100,
                minimumSafeGapSamples: 2646);

            Assert.That(transition.State, Is.EqualTo(MicrophoneTestState.Monitoring));
            Assert.That(transition.Action, Is.EqualTo(expected));
        }

        [TestCase(MicrophoneTestState.Idle)]
        [TestCase(MicrophoneTestState.NoInputDevice)]
        [TestCase(MicrophoneTestState.Failed)]
        public void InactiveStatesIgnoreCaptureFrames(MicrophoneTestState state)
        {
            MicrophoneTestTransition transition = Update(
                state,
                writePosition: -1,
                elapsed: 10f);

            Assert.That(transition.State, Is.EqualTo(state));
            Assert.That(transition.Action, Is.EqualTo(MicrophoneTestAction.None));
        }

        private static MicrophoneTestTransition Update(
            MicrophoneTestState state,
            int writePosition,
            float elapsed,
            bool captureIsRecording = true,
            bool playbackIsRunning = true,
            float stalledSeconds = 0f) =>
            MicrophoneTestPlaybackRules.Update(
                state,
                captureIsRecording,
                writePosition,
                targetLatencySamples: 5292,
                startupElapsedSeconds: elapsed,
                startupTimeoutSeconds: 2f,
                playbackIsRunning,
                writePositionStalledSeconds: stalledSeconds,
                writePositionStallTimeoutSeconds: 1f,
                readPosition: 708,
                capacity: 44100,
                minimumSafeGapSamples: 2646);
    }
}
