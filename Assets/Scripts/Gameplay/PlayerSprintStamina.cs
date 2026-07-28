using UnityEngine;

namespace InterrogationRoom.Gameplay
{
    /// <summary>
    /// Pure stamina math for the limited sprint.
    ///
    /// A Round is meant to be walked, not raced: sprinting buys a short burst to
    /// close a corridor or break away from the Detective, then forces the player
    /// back to walking speed. The budget is expressed as a normalised charge so
    /// the tuning fields stay readable ("three seconds of sprint") and a HUD can
    /// render the value directly.
    ///
    /// Kept free of Unity components so it can be covered by Edit Mode tests.
    /// </summary>
    public static class PlayerSprintStamina
    {
        /// <summary>
        /// Advances the sprint budget by one frame and reports whether the player
        /// sprints during it.
        /// </summary>
        /// <param name="wantsSprint">Sprint key held with a valid movement intent.</param>
        /// <param name="wasSprinting">Sprint state produced by the previous frame.</param>
        /// <param name="charge01">Remaining budget, 0..1. Updated in place.</param>
        /// <param name="recoveryDelayRemaining">
        /// Seconds left before the budget starts refilling. Updated in place.
        /// </param>
        public static bool Advance(
            bool wantsSprint,
            bool wasSprinting,
            float deltaTime,
            float sprintDurationSeconds,
            float recoverySeconds,
            float recoveryDelaySeconds,
            float minimumChargeToStart,
            ref float charge01,
            ref float recoveryDelayRemaining)
        {
            charge01 = Mathf.Clamp01(charge01);
            recoveryDelayRemaining = Mathf.Max(0f, recoveryDelayRemaining);
            deltaTime = Mathf.Max(0f, deltaTime);

            // Restarting a sprint costs a minimum charge, so a drained player
            // cannot tap the key every frame and stutter between the two speeds.
            float restartThreshold = wasSprinting ? 0f : Mathf.Clamp01(minimumChargeToStart);
            bool isSprinting = wantsSprint && charge01 > 0f && charge01 >= restartThreshold;

            if (isSprinting)
            {
                charge01 = Mathf.Max(0f, charge01 - deltaTime / Mathf.Max(0.01f, sprintDurationSeconds));
                recoveryDelayRemaining = Mathf.Max(0f, recoveryDelaySeconds);
                return true;
            }

            if (recoveryDelayRemaining > 0f)
            {
                recoveryDelayRemaining = Mathf.Max(0f, recoveryDelayRemaining - deltaTime);
                return false;
            }

            charge01 = Mathf.Min(1f, charge01 + deltaTime / Mathf.Max(0.01f, recoverySeconds));
            return false;
        }
    }
}
