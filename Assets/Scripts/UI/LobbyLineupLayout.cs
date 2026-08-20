using System;
using System.Collections.Generic;

namespace InterrogationRoom.UI
{
    public sealed class LobbyLineupPlan
    {
        public LobbyLineupPlan(IReadOnlyList<int> backRow, IReadOnlyList<int> frontRow)
        {
            BackRow = backRow;
            FrontRow = frontRow;
        }

        public IReadOnlyList<int> BackRow { get; }
        public IReadOnlyList<int> FrontRow { get; }
    }

    public static class LobbyLineupLayout
    {
        public const int MaximumPlayers = 8;
        private const int MaximumSingleRowPlayers = 5;

        public static LobbyLineupPlan Create(int playerCount, int localPlayerIndex)
        {
            if (playerCount < 0 || playerCount > MaximumPlayers)
                throw new ArgumentOutOfRangeException(nameof(playerCount));
            if (localPlayerIndex < -1 || localPlayerIndex >= playerCount)
                throw new ArgumentOutOfRangeException(nameof(localPlayerIndex));

            var remaining = new List<int>(playerCount);
            for (int index = 0; index < playerCount; index++)
            {
                if (index != localPlayerIndex)
                    remaining.Add(index);
            }

            if (playerCount <= MaximumSingleRowPlayers)
            {
                InsertLocalPlayerAtCenter(remaining, localPlayerIndex);
                return new LobbyLineupPlan(Array.Empty<int>(), remaining.AsReadOnly());
            }

            // Two rows keep the compact 3D lobby composition. The presenter
            // interleaves their horizontal slots, so the rows never reuse the
            // same screen-space column and cannot hide one another.
            int frontRowCount = playerCount / 2;
            int otherFrontPlayers = localPlayerIndex >= 0
                ? frontRowCount - 1
                : frontRowCount;
            var frontRow = new List<int>(frontRowCount);
            for (int index = 0; index < otherFrontPlayers; index++)
                frontRow.Add(remaining[index]);

            remaining.RemoveRange(0, otherFrontPlayers);
            InsertLocalPlayerAtCenter(frontRow, localPlayerIndex);
            return new LobbyLineupPlan(remaining.AsReadOnly(), frontRow.AsReadOnly());
        }

        private static void InsertLocalPlayerAtCenter(List<int> row, int localPlayerIndex)
        {
            if (localPlayerIndex < 0)
                return;

            row.Insert(row.Count / 2, localPlayerIndex);
        }
    }
}
