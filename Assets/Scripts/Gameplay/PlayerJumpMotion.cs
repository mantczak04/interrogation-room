namespace InterrogationRoom.Gameplay
{
    /// <summary>
    /// Pure vertical-motion math for the player capsule.
    ///
    /// Kept out of <see cref="PlayerController"/> so Edit Mode tests can cover it
    /// without pulling Mirror into the test assembly.
    /// </summary>
    public static class PlayerJumpMotion
    {
        /// <summary>
        /// Cancels upward velocity while the capsule touches a ceiling.
        ///
        /// A CharacterController stops the movement but not the velocity, so a jump
        /// into low headroom otherwise keeps pushing up and the player hangs under
        /// the ceiling until gravity has eaten the whole launch speed.
        /// </summary>
        public static float ClampVerticalVelocityAtCeiling(float verticalVelocity, bool touchingCeiling)
        {
            return touchingCeiling && verticalVelocity > 0f ? 0f : verticalVelocity;
        }
    }
}
