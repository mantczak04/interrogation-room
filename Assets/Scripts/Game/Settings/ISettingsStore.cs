namespace InterrogationRoom.Settings
{
    public interface ISettingsStore
    {
        bool TryGetFloat(string key, out float value);
        void SetFloat(string key, float value);
        bool TryGetString(string key, out string value);
        void SetString(string key, string value);
        void DeleteKey(string key);
        void Save();
    }
}
