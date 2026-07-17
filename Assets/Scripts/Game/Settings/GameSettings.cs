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
        public const float DefaultMouseSensitivity = 2f;
        public const float MinMouseSensitivity = 0.2f;
        public const float MaxMouseSensitivity = 8f;

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
    }
}
