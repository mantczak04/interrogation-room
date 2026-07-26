using NUnit.Framework;

namespace InterrogationRoom.Voice.Tests
{
    public sealed class MicrophoneMonitorBufferTests
    {
        [Test]
        public void TargetLatency_UsesAtLeastOneHundredTwentyMilliseconds()
        {
            Assert.That(
                MicrophoneMonitorBuffer.CalculateTargetLatencySamples(44100, 512, 4),
                Is.EqualTo(5292));
        }

        [Test]
        public void TargetLatency_ExpandsForLargeDspBuffers()
        {
            Assert.That(
                MicrophoneMonitorBuffer.CalculateTargetLatencySamples(44100, 1024, 8),
                Is.EqualTo(9216));
        }

        [TestCase(6000, 5292, 44100, 708)]
        [TestCase(2000, 5292, 44100, 40808)]
        public void ReadPosition_RemainsTargetLatencyBehindWriteHead(
            int writePosition,
            int targetLatency,
            int capacity,
            int expected)
        {
            Assert.That(
                MicrophoneMonitorBuffer.CalculateReadPosition(
                    writePosition,
                    targetLatency,
                    capacity),
                Is.EqualTo(expected));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void ReadPosition_DegenerateCapacityReturnsZero(int capacity)
        {
            Assert.That(
                MicrophoneMonitorBuffer.CalculateReadPosition(100, 20, capacity),
                Is.Zero);
        }

        [TestCase(6000, 5900, 44100, 2646, true)]
        [TestCase(6000, 708, 44100, 2646, false)]
        [TestCase(6000, 7000, 44100, 2646, true)]
        [TestCase(20000, 8299, 44100, 2646, true)]
        public void Resync_TriggersWhenGapLeavesSafeWindow(
            int writePosition,
            int readPosition,
            int capacity,
            int minimumGap,
            bool expected)
        {
            Assert.That(
                MicrophoneMonitorBuffer.RequiresResync(
                    writePosition,
                    readPosition,
                    capacity,
                    minimumGap),
                Is.EqualTo(expected));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        public void Resync_DegenerateCapacityAlwaysRequestsRecovery(int capacity)
        {
            Assert.That(
                MicrophoneMonitorBuffer.RequiresResync(10, 5, capacity, 2),
                Is.True);
        }
    }
}
