using System;

namespace InterrogationRoom.Voice
{
    public static class MicrophoneMonitorBuffer
    {
        private const double MinimumLatencySeconds = 0.12d;

        public static int CalculateTargetLatencySamples(
            int sampleRate,
            int dspBufferLength,
            int dspBufferCount)
        {
            int safeSampleRate = Math.Max(1, sampleRate);
            int minimumLatency = Math.Max(
                1,
                (int)Math.Ceiling(safeSampleRate * MinimumLatencySeconds));
            long dspLatency = (long)Math.Max(1, dspBufferLength) *
                Math.Max(2, dspBufferCount + 1);
            return (int)Math.Min(int.MaxValue, Math.Max(minimumLatency, dspLatency));
        }

        public static int CalculateReadPosition(
            int writePosition,
            int targetLatencySamples,
            int capacity)
        {
            if (capacity <= 0)
                return 0;

            int normalizedWrite = PositiveModulo(writePosition, capacity);
            int normalizedLatency = Math.Min(
                Math.Max(0, targetLatencySamples),
                capacity - 1);
            return PositiveModulo(normalizedWrite - normalizedLatency, capacity);
        }

        public static bool RequiresResync(
            int writePosition,
            int readPosition,
            int capacity,
            int minimumGapSamples)
        {
            if (capacity <= 1)
                return true;

            int safeMinimum = Math.Min(
                Math.Max(1, minimumGapSamples),
                capacity / 2);
            int safeMaximum = (int)Math.Min(
                capacity - 1L,
                (long)safeMinimum * 4L);
            int gap = PositiveModulo(writePosition - readPosition, capacity);
            return gap < safeMinimum || gap > safeMaximum;
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
