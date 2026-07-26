using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Networking;
using NUnit.Framework;

namespace InterrogationRoom.UI.Tests
{
    public sealed class VoiceRosterDiffTests
    {
        [Test]
        public void DiffReportsAddRemoveAndRenameWithoutStringSignatures()
        {
            var previous = new Dictionary<uint, string>
            {
                [10u] = "Alicja",
                [20u] = "Bartek"
            };
            var current = new[]
            {
                new VoiceRosterEntry(10u, "Ala"),
                new VoiceRosterEntry(30u, "Celina"),
                new VoiceRosterEntry(99u, "Local")
            };

            VoiceRosterChange[] changes =
                VoiceRosterDiff.Calculate(previous, current, localNetId: 99u).ToArray();

            Assert.That(changes.Count(change =>
                change.Kind == VoiceRosterChangeKind.Remove &&
                change.NetworkIdentityNetId == 20u), Is.EqualTo(1));
            Assert.That(changes.Count(change =>
                change.Kind == VoiceRosterChangeKind.Update &&
                change.NetworkIdentityNetId == 10u &&
                change.DisplayName == "Ala"), Is.EqualTo(1));
            Assert.That(changes.Count(change =>
                change.Kind == VoiceRosterChangeKind.Add &&
                change.NetworkIdentityNetId == 30u), Is.EqualTo(1));
            Assert.That(changes.Any(change => change.NetworkIdentityNetId == 99u), Is.False);
        }

        [Test]
        public void StableRosterProducesNoOperations()
        {
            var previous = new Dictionary<uint, string> { [10u] = "Alicja" };
            var current = new[] { new VoiceRosterEntry(10u, "Alicja") };

            Assert.That(
                VoiceRosterDiff.Calculate(previous, current, localNetId: 99u),
                Is.Empty);
        }
    }
}
