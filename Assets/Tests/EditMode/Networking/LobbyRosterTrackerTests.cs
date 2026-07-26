using System.Linq;
using NUnit.Framework;

namespace InterrogationRoom.Networking.Tests
{
    public sealed class LobbyRosterTrackerTests
    {
        [Test]
        public void ReadyStateTracksOnlyTheCurrentRoster()
        {
            var tracker = new LobbyRosterTracker();
            tracker.SetReady(10, true);
            tracker.SetReady(20, true);

            Assert.That(tracker.AreAllReady(new[] { 10, 20 }), Is.True);

            tracker.Disconnect(20, preserveDisplayName: false);

            Assert.That(tracker.AreAllReady(new[] { 10 }), Is.True);
            Assert.That(tracker.AreAllReady(new[] { 10, 20 }), Is.False);
        }

        [Test]
        public void VoiceRosterReconnectRebindsNetIdAndPreservesStablePlayerName()
        {
            var tracker = new LobbyRosterTracker();
            tracker.SetDisplayName(7, "Alicja");
            tracker.BindNetworkIdentity(7, 101u);

            tracker.Disconnect(7, preserveDisplayName: true);
            tracker.BindNetworkIdentity(7, 202u);

            VoiceRosterEntry[] roster = tracker.BuildVoiceRoster().ToArray();
            Assert.That(roster, Has.Length.EqualTo(1));
            Assert.That(roster[0].NetworkIdentityNetId, Is.EqualTo(202u));
            Assert.That(roster[0].DisplayName, Is.EqualTo("Alicja"));
        }

        [Test]
        public void DuplicateProfileAndReadyUpdatesAreIgnored()
        {
            var tracker = new LobbyRosterTracker();

            Assert.That(tracker.SetDisplayName(1, "Alicja"), Is.True);
            Assert.That(tracker.SetDisplayName(1, "Alicja"), Is.False);
            Assert.That(tracker.SetReady(1, true), Is.True);
            Assert.That(tracker.SetReady(1, true), Is.False);
        }

        [Test]
        public void VoiceRosterContainsOnlyConnectedRealPlayers()
        {
            var players = new[]
            {
                new LobbyPlayerInfo(1, 101u, "Alicja", false, false, true),
                new LobbyPlayerInfo(2, 202u, "Bartek", false, false, true),
                new LobbyPlayerInfo(-1, 0u, "Bot", false, true, true)
            };

            VoiceRosterEntry[] roster = VoiceRosterView.Build(
                    players,
                    netId => netId == 202u)
                .ToArray();

            Assert.That(roster, Has.Length.EqualTo(1));
            Assert.That(roster[0].NetworkIdentityNetId, Is.EqualTo(202u));
            Assert.That(roster[0].DisplayName, Is.EqualTo("Bartek"));
        }
    }
}
