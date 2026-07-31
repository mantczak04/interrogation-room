namespace InterrogationRoom.Gameplay.Characters
{
    public static class CharacterActionRules
    {
        public static bool CanPunch(bool isDead, bool isSeated, bool hasWeapon) =>
            !isDead && !isSeated && !hasWeapon;

        public static bool CanDance(bool isDead, bool isSeated, bool _, bool supportsDance) =>
            !isDead && !isSeated && supportsDance;

        public static bool CanDie(bool isDead) => !isDead;
    }
}
