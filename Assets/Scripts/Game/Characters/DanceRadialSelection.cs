using UnityEngine;

namespace InterrogationRoom.Gameplay.Characters
{
    public static class DanceRadialSelection
    {
        public const int DanceCount = 4;
        public const int NoSelection = -1;

        /// <summary>
        /// Player-facing labels, in the same order as the Dance blend tree thresholds. These are
        /// display names, not asset names: index 0 is "Dancing Twerk.fbx", 1 "Silly Dancing.fbx",
        /// 2 "Samba Dancing.fbx", 3 "Hokey Pokey.fbx".
        /// </summary>
        public static readonly string[] Names =
        {
            "TWERK",
            "WYGŁUPY",
            "SAMBA",
            "HOKEY POKEY"
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
