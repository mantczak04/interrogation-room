using UnityEngine;

namespace InterrogationRoom.Settings
{
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        public bool TryGetFloat(string key, out float value)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                value = 0f;
                return false;
            }

            value = PlayerPrefs.GetFloat(key);
            return true;
        }

        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
