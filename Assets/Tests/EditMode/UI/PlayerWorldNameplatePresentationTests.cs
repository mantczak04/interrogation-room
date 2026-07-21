using InterrogationRoom.Networking;
using NUnit.Framework;

namespace InterrogationRoom.UI.Tests
{
    public sealed class PlayerWorldNameplatePresentationTests
    {
        [Test]
        public void ResolvesRealPlayerByNetworkIdentityAndPreservesPolishCharacters()
        {
            var players = new[]
            {
                new LobbyPlayerInfo(0, 41u, "Łukasz Śledź", true, false, true),
                new LobbyPlayerInfo(-1, 0u, "Alicja Żur", false, true, true)
            };

            bool found = PlayerWorldNameplatePresentation.TryResolveDisplayName(
                players,
                41u,
                out string displayName);

            Assert.That(found, Is.True);
            Assert.That(displayName, Is.EqualTo("Łukasz Śledź"));
        }

        [Test]
        public void SimulatedLobbyEntryCannotCreateAWorldNameplate()
        {
            var players = new[]
            {
                new LobbyPlayerInfo(-1, 0u, "Gracz testowy", false, true, true)
            };

            Assert.That(
                PlayerWorldNameplatePresentation.TryResolveDisplayName(
                    players,
                    0u,
                    out _),
                Is.False);
        }

        [TestCase(false, false, false, false)]
        [TestCase(true, false, false, true)]
        [TestCase(true, true, false, false)]
        [TestCase(true, true, true, true)]
        public void VisibilityKeepsRemoteNamesVisibleAndShowsLocalNameOnlyInThirdPerson(
            bool hasDisplayName,
            bool isLocalPlayer,
            bool isThirdPerson,
            bool expected)
        {
            Assert.That(
                PlayerWorldNameplatePresentation.ShouldShow(
                    hasDisplayName,
                    isLocalPlayer,
                    isThirdPerson),
                Is.EqualTo(expected));
        }
    }
}
