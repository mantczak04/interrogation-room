using UnityEngine;

namespace InterrogationRoom.UI
{
    /// <summary>
    /// Shared cursor and local gameplay-input gate owned by the runtime UI assembly.
    /// PlayerController delegates to this seam so phase UI and FPP input cannot drift.
    /// </summary>
    public static class PlayerInputGate
    {
        private static bool uiBlocksGameplay = true;

        public static bool CursorReleased { get; private set; } = true;

        public static void SetUiInputBlocked(bool blocked)
        {
            if (uiBlocksGameplay == blocked)
            {
                if (blocked)
                    ApplyCursorState(true);
                return;
            }

            uiBlocksGameplay = blocked;
            ApplyCursorState(blocked);
        }

        public static void SetPlayerCursorReleased(bool released)
        {
            if (uiBlocksGameplay)
                return;

            ApplyCursorState(released);
        }

        private static void ApplyCursorState(bool released)
        {
            CursorReleased = released;
            Cursor.lockState = released ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = released;
        }
    }
}
