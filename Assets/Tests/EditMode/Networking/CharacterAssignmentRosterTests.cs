using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Gameplay.Characters;
using NUnit.Framework;

namespace InterrogationRoom.Networking.Tests
{
    public sealed class CharacterAssignmentRosterTests
    {
        [Test]
        public void FirstFourAssignmentsAreUnique()
        {
            var roster = CreateRoster();

            CharacterId[] assignments = Enumerable.Range(1, 4)
                .Select(connectionId => roster.Acquire(connectionId))
                .ToArray();

            Assert.That(assignments.Distinct().Count(), Is.EqualTo(4));
        }

        [Test]
        public void FifthAndSixthAssignmentsRemainValidAfterUniquePoolIsExhausted()
        {
            var roster = CreateRoster();

            CharacterId[] assignments = Enumerable.Range(1, 6)
                .Select(connectionId => roster.Acquire(connectionId))
                .ToArray();

            Assert.That(
                assignments.Skip(4),
                Is.All.Matches<CharacterId>(CharacterAssignmentRoster.DefaultCharacters.Contains));
        }

        [Test]
        public void ReleasedCharacterIsPreferredWhenItBecomesUnused()
        {
            var roster = CreateRoster();
            var assignments = new Dictionary<int, CharacterId>();
            for (int connectionId = 1; connectionId <= 4; connectionId++)
            {
                assignments.Add(connectionId, roster.Acquire(connectionId));
            }

            CharacterId released = assignments[2];
            Assert.That(roster.Release(2), Is.True);

            Assert.That(roster.Acquire(5), Is.EqualTo(released));
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
