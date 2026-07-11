namespace InterrogationRoom.Gameplay.Characters
{
    public static class CharacterActionRules
    {
        public static bool CanPunch(bool isDead, bool isSeated, bool hasWeapon) =>
            !isDead && !isSeated && !hasWeapon;

        public static bool CanDie(bool isDead) => !isDead;
    }
}
