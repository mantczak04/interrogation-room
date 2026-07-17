using UnityEngine;

namespace InterrogationRoom.Settings
{
    public static class GameSettingsService
    {
        private static GameSettings current;

        public static GameSettings Current => current ??= new GameSettings(new PlayerPrefsSettingsStore());

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            current = null;
        }
    }
}
