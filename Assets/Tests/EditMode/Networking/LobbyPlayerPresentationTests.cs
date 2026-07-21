using InterrogationRoom.Networking;
using InterrogationRoom.Debugging;
using NUnit.Framework;

namespace InterrogationRoom.Tests.EditMode.Networking
{
    public sealed class LobbyPlayerPresentationTests
    {
        [Test]
        public void NormalizeDisplayName_PreservesPolishCharactersAndCollapsesWhitespace()
        {
            string normalized = LobbyPlayerPresentation.NormalizeDisplayName(
                "  Łukasz\n\tŚledź  ",
                "Gracz");

            Assert.That(normalized, Is.EqualTo("Łukasz Śledź"));
        }

        [Test]
        public void NormalizeDisplayName_BoundsUntrustedLobbyText()
        {
            string normalized = LobbyPlayerPresentation.NormalizeDisplayName(
                new string('A', 80),
                "Gracz");

            Assert.That(normalized, Has.Length.EqualTo(LobbyPlayerPresentation.MaxDisplayNameLength));
        }

        [Test]
        public void CreateSimulatedPlayers_UsesPublicPresentationOnly()
        {
            var players = LobbyPlayerPresentation.CreateSimulatedPlayers(7);

            Assert.That(players.Count, Is.EqualTo(7));
            Assert.That(players[0].DisplayName, Is.EqualTo("Alicja Żur"));
            Assert.That(players[6].DisplayName, Is.EqualTo("Paweł Wróbel"));
            Assert.That(players, Has.All.Matches<LobbyPlayerInfo>(player =>
                player.IsSimulated &&
                !player.IsHost &&
                player.IsReady &&
                player.NetworkIdentityNetId == 0 &&
                player.PlayerId < 0));
        }

        [Test]
        public void CreateSimulatedPlayers_IsCappedAtSevenAdditionalSlots()
        {
            Assert.That(LobbyPlayerPresentation.CreateSimulatedPlayers(99).Count, Is.EqualTo(7));
            Assert.That(LobbyPlayerPresentation.CreateSimulatedPlayers(-1).Count, Is.Zero);
        }

        [TestCase(720, 2f / 3f)]
        [TestCase(1080, 1f)]
        [TestCase(1440, 4f / 3f)]
        [TestCase(2160, 2f)]
        public void DeveloperPanelScale_KeepsA1080PixelVirtualHeight(
            int screenHeight,
            float expectedScale)
        {
            Assert.That(
                RoundDeveloperPanel.CalculateGuiScale(screenHeight),
                Is.EqualTo(expectedScale).Within(0.001f));
        }
    }
}
