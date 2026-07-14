using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Gameplay.Characters;
using NUnit.Framework;

namespace InterrogationRoom.Networking.Tests
{
    public sealed class CharacterAssignmentRosterTests
    {
        [Test]
        public void FirstFiveAssignmentsAreUnique()
        {
            var roster = CreateRoster();

            CharacterId[] assignments = Enumerable.Range(1, 5)
                .Select(connectionId => roster.Acquire(connectionId))
                .ToArray();

            Assert.That(assignments.Distinct().Count(), Is.EqualTo(5));
        }

        [Test]
        public void SixthAssignmentRemainsValidAfterUniquePoolIsExhausted()
        {
            var roster = CreateRoster();

            CharacterId[] assignments = Enumerable.Range(1, 6)
                .Select(connectionId => roster.Acquire(connectionId))
                .ToArray();

            Assert.That(
                assignments.Skip(5),
                Is.All.Matches<CharacterId>(CharacterAssignmentRoster.DefaultCharacters.Contains));
        }

        [Test]
        public void ReleasedCharacterIsPreferredWhenItBecomesUnused()
        {
            var roster = CreateRoster();
            var assignments = new Dictionary<int, CharacterId>();
            for (int connectionId = 1; connectionId <= 5; connectionId++)
            {
                assignments.Add(connectionId, roster.Acquire(connectionId));
            }

            CharacterId released = assignments[2];
            Assert.That(roster.Release(2), Is.True);

            Assert.That(roster.Acquire(6), Is.EqualTo(released));
        }

        [Test]
        public void RepeatedAcquireReturnsExistingAssignment()
        {
            var roster = CreateRoster();
            CharacterId original = roster.Acquire(42);

            Assert.That(roster.Acquire(42), Is.EqualTo(original));
            Assert.That(roster.Count, Is.EqualTo(1));
        }

        [TestCase(false, false, false, true)]
        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        public void PunchEligibilityRequiresAnAliveStandingUnarmedPlayer(
            bool isDead,
            bool isSeated,
            bool hasWeapon,
            bool expected)
        {
            Assert.That(
                CharacterActionRules.CanPunch(isDead, isSeated, hasWeapon),
                Is.EqualTo(expected));
        }

        [TestCase(false, false, false, true, true)]
        [TestCase(true, false, false, true, false)]
        [TestCase(false, true, false, true, false)]
        [TestCase(false, false, true, true, false)]
        [TestCase(false, false, false, false, false)]
        public void DanceEligibilityRequiresAnAliveStandingUnarmedSupportedCharacter(
            bool isDead,
            bool isSeated,
            bool hasWeapon,
            bool supportsDance,
            bool expected)
        {
            Assert.That(
                CharacterActionRules.CanDance(isDead, isSeated, hasWeapon, supportsDance),
                Is.EqualTo(expected));
        }

        [Test]
        public void OnlyLivingPlayerCanEnterDeathState()
        {
            Assert.That(CharacterActionRules.CanDie(false), Is.True);
            Assert.That(CharacterActionRules.CanDie(true), Is.False);
        }

        private static CharacterAssignmentRoster CreateRoster() =>
            new CharacterAssignmentRoster(index => 0);
    }
}
