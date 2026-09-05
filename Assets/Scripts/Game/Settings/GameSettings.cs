using System;
using System.Collections.Generic;

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
        public const string GraphicsQualityKey = "settings.graphics.quality";
        public const string MicrophoneLevelKey = "settings.voice.microphoneLevel";
        public const string MicrophoneMutedKey = "settings.voice.microphoneMuted";
        public const string VoiceInputDeviceKey = "settings.voice.inputDevice";
        private const string InputBindingKeyPrefix = "settings.input.";
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

        public string PreferredVoiceInputDeviceId =>
            GetOptionalString(VoiceInputDeviceKey);

        public int GraphicsQuality => store.TryGetFloat(GraphicsQualityKey, out float stored)
            ? NormalizeGraphicsQuality(stored) : 3;

        public void SetGraphicsQuality(int quality)
        {
            int normalized = NormalizeGraphicsQuality(quality);
            if (store.TryGetFloat(GraphicsQualityKey, out float stored) && stored == normalized) return;
            store.SetFloat(GraphicsQualityKey, normalized);
            store.Save();
            Changed?.Invoke();
        }

        private static int NormalizeGraphicsQuality(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? 3 : (int)Math.Max(0, Math.Min(3, value));

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

        public void SetPreferredVoiceInputDevice(string deviceId) =>
            SetOptionalString(VoiceInputDeviceKey, deviceId);

        public string GetInputBindingOverride(GameInputAction action)
        {
            EnsureRebindableAction(action);
            return GetOptionalString(GetInputBindingKey(action));
        }

        public void SetInputBindingOverride(GameInputAction action, string controlPath)
        {
            EnsureRebindableAction(action);
            SetOptionalString(GetInputBindingKey(action), controlPath);
        }

        public void ResetInputBindingOverride(GameInputAction action)
        {
            EnsureRebindableAction(action);
            SetOptionalString(GetInputBindingKey(action), null);
        }

        public static string GetInputBindingKey(GameInputAction action)
        {
            EnsureRebindableAction(action);
            return $"{InputBindingKeyPrefix}{action}";
        }

        private string GetOptionalString(string key)
        {
            return store.TryGetString(key, out string stored) &&
                   !string.IsNullOrWhiteSpace(stored)
                ? stored.Trim()
                : null;
        }

        private void SetOptionalString(string key, string value)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            string current = GetOptionalString(key);
            if (string.Equals(current, normalized, StringComparison.Ordinal))
                return;

            if (normalized == null)
                store.DeleteKey(key);
            else
                store.SetString(key, normalized);

            store.Save();
            Changed?.Invoke();
        }

        private static void EnsureRebindableAction(GameInputAction action)
        {
            if (!GameInputBindingCatalog.IsRebindable(action))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(action),
                    action,
                    "Only approved player-facing gameplay actions can be rebound.");
            }
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

    public enum GameInputAction
    {
        MoveForward,
        MoveBackward,
        MoveLeft,
        MoveRight,
        Sprint,
        Jump,
        Interact,
        Drop,
        Dance,
        View,
        PrivateObjective,
        Fire,
        VoiceMute
    }

    public enum InputBindingValidationStatus
    {
        Valid,
        Invalid,
        Reserved,
        Conflict
    }

    public readonly struct InputBindingValidation
    {
        public InputBindingValidationStatus Status { get; }
        public GameInputAction? ConflictingAction { get; }

        private InputBindingValidation(
            InputBindingValidationStatus status,
            GameInputAction? conflictingAction = null)
        {
            Status = status;
            ConflictingAction = conflictingAction;
        }

        public static InputBindingValidation Valid() =>
            new InputBindingValidation(InputBindingValidationStatus.Valid);

        public static InputBindingValidation Invalid() =>
            new InputBindingValidation(InputBindingValidationStatus.Invalid);

        public static InputBindingValidation Reserved() =>
            new InputBindingValidation(InputBindingValidationStatus.Reserved);

        public static InputBindingValidation Conflict(GameInputAction action) =>
            new InputBindingValidation(InputBindingValidationStatus.Conflict, action);
    }

    /// <summary>
    /// Product-approved rebind catalog. Esc, F8, and character hotkeys 1-5 are
    /// deliberately absent and additionally rejected as target controls.
    /// </summary>
    public static class GameInputBindingCatalog
    {
        private static readonly GameInputAction[] RebindableActions =
        {
            GameInputAction.MoveForward,
            GameInputAction.MoveBackward,
            GameInputAction.MoveLeft,
            GameInputAction.MoveRight,
            GameInputAction.Sprint,
            GameInputAction.Jump,
            GameInputAction.Interact,
            GameInputAction.Drop,
            GameInputAction.Dance,
            GameInputAction.View,
            GameInputAction.PrivateObjective,
            GameInputAction.Fire,
            GameInputAction.VoiceMute
        };

        private static readonly IReadOnlyDictionary<GameInputAction, string> DefaultPaths =
            new Dictionary<GameInputAction, string>
            {
                [GameInputAction.MoveForward] = "<Keyboard>/w",
                [GameInputAction.MoveBackward] = "<Keyboard>/s",
                [GameInputAction.MoveLeft] = "<Keyboard>/a",
                [GameInputAction.MoveRight] = "<Keyboard>/d",
                [GameInputAction.Sprint] = "<Keyboard>/leftShift",
                [GameInputAction.Jump] = "<Keyboard>/space",
                [GameInputAction.Interact] = "<Keyboard>/e",
                [GameInputAction.Drop] = "<Keyboard>/g",
                [GameInputAction.Dance] = "<Keyboard>/t",
                [GameInputAction.View] = "<Keyboard>/c",
                [GameInputAction.PrivateObjective] = "<Keyboard>/i",
                [GameInputAction.Fire] = "<Mouse>/leftButton",
                [GameInputAction.VoiceMute] = "<Keyboard>/v"
            };

        private static readonly HashSet<string> ReservedPaths =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "<Keyboard>/escape",
                "<Keyboard>/f8",
                "<Keyboard>/digit1",
                "<Keyboard>/digit2",
                "<Keyboard>/digit3",
                "<Keyboard>/digit4",
                "<Keyboard>/digit5",
                "<Keyboard>/1",
                "<Keyboard>/2",
                "<Keyboard>/3",
                "<Keyboard>/4",
                "<Keyboard>/5",
                "<Keyboard>/numpad1",
                "<Keyboard>/numpad2",
                "<Keyboard>/numpad3",
                "<Keyboard>/numpad4",
                "<Keyboard>/numpad5"
            };

        public static IReadOnlyList<GameInputAction> Actions => RebindableActions;

        public static bool IsRebindable(GameInputAction action) =>
            DefaultPaths.ContainsKey(action);

        public static string GetDefaultPath(GameInputAction action)
        {
            if (!DefaultPaths.TryGetValue(action, out string path))
                throw new ArgumentOutOfRangeException(nameof(action), action, null);

            return path;
        }

        public static string GetEffectivePath(GameInputAction action, GameSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            return settings.GetInputBindingOverride(action) ?? GetDefaultPath(action);
        }

        public static InputBindingValidation ValidateOverride(
            GameInputAction action,
            string controlPath,
            GameSettings settings)
        {
            if (!IsRebindable(action) ||
                string.IsNullOrWhiteSpace(controlPath) ||
                settings == null)
            {
                return InputBindingValidation.Invalid();
            }

            string normalized = controlPath.Trim();
            if (ReservedPaths.Contains(normalized))
                return InputBindingValidation.Reserved();

            foreach (GameInputAction candidate in RebindableActions)
            {
                if (candidate == action)
                    continue;

                string candidatePath = GetEffectivePath(candidate, settings);
                if (string.Equals(
                        candidatePath,
                        normalized,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return InputBindingValidation.Conflict(candidate);
                }
            }

            return InputBindingValidation.Valid();
        }
    }
}
