using NUnit.Framework;

namespace InterrogationRoom.Voice.Tests
{
    public sealed class VoiceSpeakingStateTests
    {
        [Test]
        public void Apply_TracksSpeakingStateByNetworkIdentity()
        {
            var state = new VoiceSpeakingState();

            Assert.That(state.Apply(17u, true), Is.True);
            Assert.That(state.IsSpeaking(17u), Is.True);
            Assert.That(state.IsSpeaking(18u), Is.False);

            Assert.That(state.Apply(17u, false), Is.True);
            Assert.That(state.IsSpeaking(17u), Is.False);
        }

        [Test]
        public void Apply_IgnoresMissingNetworkIdentity()
        {
            var state = new VoiceSpeakingState();

            Assert.That(state.Apply(0u, true), Is.False);
            Assert.That(state.IsSpeaking(0u), Is.False);
        }

        [Test]
        public void Clear_RemovesAllSpeakingPlayers()
        {
            var state = new VoiceSpeakingState();
            state.Apply(2u, true);
            state.Apply(3u, true);

            state.Clear();

            Assert.That(state.IsSpeaking(2u), Is.False);
            Assert.That(state.IsSpeaking(3u), Is.False);
        }
    }
}
