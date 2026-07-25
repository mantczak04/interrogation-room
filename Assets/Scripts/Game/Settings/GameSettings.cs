using System;

namespace InterrogationRoom.Settings
{
    /// <summary>
    /// Player-facing settings with clamping, defaults, and persistence keys in
    /// one testable plain class. New settings extend this class with their own
    /// key, range, and accessor; storage stays behind <see cref="ISettingsStore"/>.
    /// </summary>
    public sealed class GameSettings
    {
        public const string MouseSensitivityKey = "settings.mouseSensitivity";
        public const string LanguageKey = "settings.language";
        public const string MicrophoneLevelKey = "settings.voice.microphoneLevel";
        public const string MicrophoneMutedKey = "settings.voice.microphoneMuted";
        public const float DefaultMouseSensitivity = 1f;
        public const float MinMouseSensitivity = 0.2f;
        public const float MaxMouseSensitivity = 8f;
        public const float DefaultVoicePercent = 100f;
        public const float MinVoicePercent = 0f;
        public const float MaxVoicePercent = 200f;

        private readonly ISettingsStore store;
        private float fallbackMouseSensitivity = DefaultMouseSensitivity;

        public event Action Changed;

        public GameSettings(ISettingsStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public float MouseSensitivity =>
            store.TryGetFloat(MouseSensitivityKey, out float stored)
                ? ClampMouseSensitivity(stored)
                : fallbackMouseSensitivity;

        public UiLanguage Language =>
            store.TryGetFloat(LanguageKey, out float stored)
                ? UiLanguageUtility.FromStoredValue(stored)
                : UiLanguage.Polish;

        public float MicrophoneLevelPercent =>
            store.TryGetFloat(MicrophoneLevelKey, out float stored)
                ? ClampVoicePercent(stored)
                : DefaultVoicePercent;

        public bool MicrophoneMuted =>
            store.TryGetFloat(MicrophoneMutedKey, out float stored) && stored >= 0.5f;

        public void SetMouseSensitivityFallback(float value)
        {
            fallbackMouseSensitivity = ClampMouseSensitivity(value);
        }

        public void SetMouseSensitivity(float value)
        {
            float clamped = ClampMouseSensitivity(value);
            if (store.TryGetFloat(MouseSensitivityKey, out float stored) &&
                ClampMouseSensitivity(stored) == clamped)
            {
                return;
            }

            store.SetFloat(MouseSensitivityKey, clamped);
            store.Save();
            Changed?.Invoke();
        }

        public void SetLanguage(UiLanguage language)
        {
            UiLanguage normalized = UiLanguageUtility.Normalize(language);
            if (store.TryGetFloat(LanguageKey, out float stored) &&
                UiLanguageUtility.FromStoredValue(stored) == normalized)
            {
                return;
            }

            store.SetFloat(LanguageKey, (float)normalized);
            store.Save();
            Changed?.Invoke();
        }

        public void SetMicrophoneLevelPercent(float value)
        {
            float clamped = ClampVoicePercent(value);
            if (store.TryGetFloat(MicrophoneLevelKey, out float stored) &&
                ClampVoicePercent(stored) == clamped)
            {
                return;
            }

            store.SetFloat(MicrophoneLevelKey, clamped);
            store.Save();
            Changed?.Invoke();
        }

        public void SetMicrophoneMuted(bool muted)
        {
            if (store.TryGetFloat(MicrophoneMutedKey, out float stored) &&
                (stored >= 0.5f) == muted)
            {
                return;
            }

            store.SetFloat(MicrophoneMutedKey, muted ? 1f : 0f);
            store.Save();
            Changed?.Invoke();
        }

        public static float ClampMouseSensitivity(float value)
        {
            if (float.IsNaN(value))
            {
                return DefaultMouseSensitivity;
            }

            if (value < MinMouseSensitivity)
            {
                return MinMouseSensitivity;
            }

            return value > MaxMouseSensitivity ? MaxMouseSensitivity : value;
        }

        public static float ClampVoicePercent(float value)
        {
            if (float.IsNaN(value))
                return DefaultVoicePercent;

            if (value < MinVoicePercent)
                return MinVoicePercent;

            return value > MaxVoicePercent ? MaxVoicePercent : value;
        }

        public static int VoicePercentToVivoxVolume(float percent)
        {
            float clamped = ClampVoicePercent(percent);
            if (clamped <= 0f)
                return -50;

            double decibels = 20d * Math.Log10(clamped / DefaultVoicePercent);
            return Math.Max(-50, Math.Min(50, (int)Math.Round(decibels)));
        }
    }
}
