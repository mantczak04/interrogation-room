using NUnit.Framework;

namespace InterrogationRoom.Voice.Tests
{
    public sealed class VoiceChannelModeStateTests
    {
        [TestCase(false, true)]
        [TestCase(true, false)]
        public void JoiningModeIsEffectiveBeforeJoinCompletes(
            bool activeSpatial,
            bool targetSpatial)
        {
            VoiceChannelModeState state =
                new VoiceChannelModeState(activeSpatial).BeginJoin(targetSpatial);

            Assert.That(state.ActiveSpatial, Is.EqualTo(activeSpatial));
            Assert.That(state.EffectiveSpatial, Is.EqualTo(targetSpatial));
        }

        [Test]
        public void CommitMakesJoiningModeActive()
        {
            VoiceChannelModeState state =
                new VoiceChannelModeState(false).BeginJoin(true).CommitJoin();

            Assert.That(state.ActiveSpatial, Is.True);
            Assert.That(state.JoiningSpatial, Is.Null);
            Assert.That(state.EffectiveSpatial, Is.True);
        }

        [Test]
        public void RollbackRestoresPreviousActiveMode()
        {
            VoiceChannelModeState state =
                new VoiceChannelModeState(false).BeginJoin(true).RollbackJoin();

            Assert.That(state.ActiveSpatial, Is.False);
            Assert.That(state.JoiningSpatial, Is.Null);
            Assert.That(state.EffectiveSpatial, Is.False);
        }
    }
}
