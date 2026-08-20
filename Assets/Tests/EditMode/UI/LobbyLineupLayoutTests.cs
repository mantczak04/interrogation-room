using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace InterrogationRoom.UI.Tests
{
    public sealed class LobbyLineupLayoutTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void UpToFivePlayersUseOneRowWithLocalPlayerNearCenter(int playerCount)
        {
            int localIndex = playerCount - 1;

            LobbyLineupPlan plan = LobbyLineupLayout.Create(playerCount, localIndex);

            Assert.That(plan.BackRow, Is.Empty);
            Assert.That(plan.FrontRow.Count, Is.EqualTo(playerCount));
            Assert.That(plan.FrontRow[(playerCount - 1) / 2], Is.EqualTo(localIndex));
            AssertContainsEveryPlayerExactlyOnce(plan, playerCount);
        }

        [TestCase(6, 3, 3)]
        [TestCase(7, 4, 3)]
        [TestCase(8, 4, 4)]
        public void LargerLobbiesUseBalancedRowsWithLocalPlayerInFront(
            int playerCount,
            int expectedBackCount,
            int expectedFrontCount)
        {
            int localIndex = playerCount - 1;

            LobbyLineupPlan plan = LobbyLineupLayout.Create(playerCount, localIndex);

            Assert.That(plan.BackRow, Has.Count.EqualTo(expectedBackCount));
            Assert.That(plan.FrontRow, Has.Count.EqualTo(expectedFrontCount));
            Assert.That(plan.FrontRow[(expectedFrontCount - 1) / 2], Is.EqualTo(localIndex));
            AssertContainsEveryPlayerExactlyOnce(plan, playerCount);
        }

        [Test]
        public void MissingLocalPlayerPreservesRosterOrder()
        {
            LobbyLineupPlan plan = LobbyLineupLayout.Create(8, -1);

            Assert.That(plan.FrontRow, Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(plan.BackRow, Is.EqualTo(new[] { 4, 5, 6, 7 }));
        }

        [TestCase(-1, -1)]
        [TestCase(9, -1)]
        [TestCase(2, 2)]
        public void InvalidInputIsRejected(int playerCount, int localIndex)
        {
            Assert.That(
                () => LobbyLineupLayout.Create(playerCount, localIndex),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        private static void AssertContainsEveryPlayerExactlyOnce(LobbyLineupPlan plan, int playerCount)
        {
            IEnumerable<int> allPlayers = plan.BackRow.Concat(plan.FrontRow);
            Assert.That(allPlayers.OrderBy(index => index), Is.EqualTo(Enumerable.Range(0, playerCount)));
        }
    }
}
