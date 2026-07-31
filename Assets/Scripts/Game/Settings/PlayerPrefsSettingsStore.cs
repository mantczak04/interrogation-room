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

        public bool TryGetString(string key, out string value)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                value = null;
                return false;
            }

            value = PlayerPrefs.GetString(key);
            return true;
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
