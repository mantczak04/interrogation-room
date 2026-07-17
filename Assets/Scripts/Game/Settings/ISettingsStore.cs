namespace InterrogationRoom.Settings
{
    public interface ISettingsStore
    {
        bool TryGetFloat(string key, out float value);
        void SetFloat(string key, float value);
        void Save();
    }
}
