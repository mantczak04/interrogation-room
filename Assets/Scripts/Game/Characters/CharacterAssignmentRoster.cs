using System;
using System.Collections.Generic;
using System.Linq;

namespace InterrogationRoom.Gameplay.Characters
{
    public sealed class CharacterAssignmentRoster
    {
        private static readonly CharacterId[] Characters =
        {
            CharacterId.Malpa,
            CharacterId.Wieprz,
            CharacterId.Jak,
            CharacterId.Karton,
            CharacterId.Ptaku
        };

        private readonly Dictionary<int, CharacterId> assignments = new();
        private readonly Func<int, int> nextIndex;

        public CharacterAssignmentRoster(Func<int, int> nextIndex)
        {
            this.nextIndex = nextIndex ?? throw new ArgumentNullException(nameof(nextIndex));
        }

        public static IReadOnlyList<CharacterId> DefaultCharacters => Characters;

        public int Count => assignments.Count;

        public CharacterId Acquire(int connectionId)
        {
            if (assignments.TryGetValue(connectionId, out CharacterId existing))
            {
                return existing;
            }

            HashSet<CharacterId> used = assignments.Values.ToHashSet();
            CharacterId[] candidates = Characters.Where(character => !used.Contains(character)).ToArray();
            if (candidates.Length == 0)
            {
                candidates = Characters;
            }

            int index = nextIndex(candidates.Length);
            if (index < 0 || index >= candidates.Length)
            {
                throw new InvalidOperationException("The random index provider returned an invalid index.");
            }

            CharacterId assigned = candidates[index];
            assignments.Add(connectionId, assigned);
            return assigned;
        }

        public bool Release(int connectionId) => assignments.Remove(connectionId);
    }
}
