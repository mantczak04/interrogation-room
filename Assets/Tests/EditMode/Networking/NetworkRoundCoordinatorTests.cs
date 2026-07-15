using InterrogationRoom.Domain;
using NUnit.Framework;

namespace InterrogationRoom.Networking.Tests
{
    public sealed class NetworkRoundCoordinatorTests
    {
        [TestCase(RoundPhase.Round, false, 10d, 10d, true)]
        [TestCase(RoundPhase.Round, false, 9d, 10d, false)]
        [TestCase(RoundPhase.Round, true, 1000d, 10d, false)]
        [TestCase(RoundPhase.Preparation, false, 1000d, 10d, false)]
        public void ShouldExpireRound_SkipsAutomaticTimeoutForDeveloperScenario(
            RoundPhase phase,
            bool developerRoundUnlimited,
            double now,
            double deadline,
            bool expected)
        {
            Assert.That(
                NetworkRoundCoordinator.ShouldExpireRound(
                    phase,
                    developerRoundUnlimited,
                    now,
                    deadline),
                Is.EqualTo(expected));
        }
    }
}
