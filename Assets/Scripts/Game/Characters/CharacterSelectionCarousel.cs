using System;

namespace InterrogationRoom.Gameplay.Characters
{
    public static class CharacterSelectionCarousel
    {
        public static CharacterId Step(CharacterId current, int offset)
        {
            var characters = CharacterAssignmentRoster.DefaultCharacters;
            int currentIndex = -1;
            for (int index = 0; index < characters.Count; index++)
            {
                if (characters[index] == current)
                {
                    currentIndex = index;
                    break;
                }
            }

            if (currentIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(current), current, "Unknown character.");

            int nextIndex = (currentIndex + offset) % characters.Count;
            if (nextIndex < 0)
                nextIndex += characters.Count;

            return characters[nextIndex];
        }

        public static string DisplayName(CharacterId character) => character switch
        {
            CharacterId.Malpa => "Małpa",
            CharacterId.Wieprz => "Wieprz",
            CharacterId.Jak => "Jak",
            CharacterId.Karton => "Karton",
            CharacterId.Ptaku => "Ptaku",
            _ => throw new ArgumentOutOfRangeException(nameof(character), character, "Unknown character.")
        };
    }
}
