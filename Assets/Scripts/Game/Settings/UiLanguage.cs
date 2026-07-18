namespace InterrogationRoom.Settings
{
    public enum UiLanguage
    {
        Polish = 0,
        English = 1
    }

    public static class UiLanguageUtility
    {
        public static UiLanguage Normalize(UiLanguage language) =>
            language == UiLanguage.English ? UiLanguage.English : UiLanguage.Polish;

        public static UiLanguage FromStoredValue(float value) =>
            value >= 0.5f && value < 1.5f ? UiLanguage.English : UiLanguage.Polish;
    }
}
