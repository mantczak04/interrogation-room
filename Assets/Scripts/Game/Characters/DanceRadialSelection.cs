using UnityEngine;

namespace InterrogationRoom.Gameplay.Characters
{
    public static class DanceRadialSelection
    {
        public const int DanceCount = 4;
        public const int NoSelection = -1;

        public static readonly string[] Names =
        {
            "Dancing Twerk",
            "Silly Dancing",
            "Samba Dancing",
            "Hokey Pokey"
        };

        public static int FromPointerOffset(Vector2 offset, float deadZoneRadius)
        {
            if (offset.sqrMagnitude < deadZoneRadius * deadZoneRadius)
                return NoSelection;

            float clockwiseFromTop = Mathf.Atan2(offset.x, offset.y) * Mathf.Rad2Deg;
            if (clockwiseFromTop < 0f)
                clockwiseFromTop += 360f;

            float sectorSize = 360f / DanceCount;
            return Mathf.FloorToInt((clockwiseFromTop + sectorSize * 0.5f) / sectorSize) %
                DanceCount;
        }

        public static bool IsValid(int danceIndex) =>
            danceIndex >= 0 && danceIndex < DanceCount;
    }
}
