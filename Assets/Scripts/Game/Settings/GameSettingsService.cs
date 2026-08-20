using System;
using System.Collections.Generic;
using InterrogationRoom.UI;
using UnityEngine;
using UnityEngine.InputSystem;

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

    public enum InputRebindOutcome
    {
        Applied,
        Cancelled,
        Reserved,
        Conflict,
        Invalid
    }

    public readonly struct InputRebindResult
    {
        public GameInputAction Action { get; }
        public InputRebindOutcome Outcome { get; }
        public string ControlPath { get; }
        public GameInputAction? ConflictingAction { get; }

        public InputRebindResult(
            GameInputAction action,
            InputRebindOutcome outcome,
            string controlPath = null,
            GameInputAction? conflictingAction = null)
        {
            Action = action;
            Outcome = outcome;
            ControlPath = controlPath;
            ConflictingAction = conflictingAction;
        }
    }

    /// <summary>
    /// The sole runtime Input System adapter for player-facing gameplay actions.
    /// Raw Esc, F8, and character keys 1-5 intentionally remain outside this
    /// catalog. Gameplay actions, including voice mute and unarmed primary
    /// action, go through these bindings.
    /// </summary>
    public static class GameInputBindings
    {
        private static readonly Dictionary<GameInputAction, InputAction> RuntimeActions = new();
        private static readonly object RebindInputOwner = new();

        private static InputActionRebindingExtensions.RebindingOperation rebindOperation;
        private static GameInputAction rebindAction;
        private static Action<InputRebindResult> rebindCompleted;
        private static GameSettings subscribedSettings;
        private static bool initialized;
        private static int suppressRawInputThroughFrame = -1;

        public static event Action BindingsChanged;
        public static event Action RebindStateChanged;

        public static bool IsRebinding => rebindOperation != null;
        public static bool RawInputSuppressed =>
            GameplaySuppressed ||
            Time.frameCount <= suppressRawInputThroughFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            CancelInteractiveRebind();
            if (subscribedSettings != null)
                subscribedSettings.Changed -= OnSettingsChanged;

            foreach (InputAction action in RuntimeActions.Values)
                action.Dispose();

            RuntimeActions.Clear();
            subscribedSettings = null;
            initialized = false;
            suppressRawInputThroughFrame = -1;
            BindingsChanged = null;
            RebindStateChanged = null;
        }

        public static Vector2 ReadMove()
        {
            if (GameplaySuppressed)
                return Vector2.zero;

            Vector2 value = Vector2.zero;
            if (IsPressed(GameInputAction.MoveLeft))
                value.x -= 1f;
            if (IsPressed(GameInputAction.MoveRight))
                value.x += 1f;
            if (IsPressed(GameInputAction.MoveBackward))
                value.y -= 1f;
            if (IsPressed(GameInputAction.MoveForward))
                value.y += 1f;
            return Vector2.ClampMagnitude(value, 1f);
        }

        public static bool IsPressed(GameInputAction action)
        {
            return !GameplaySuppressed && GetAction(action).IsPressed();
        }

        public static bool WasPressedThisFrame(GameInputAction action)
        {
            return !GameplaySuppressed && GetAction(action).WasPressedThisFrame();
        }

        public static bool WasPressedThisFrameForModal(GameInputAction action)
        {
            return !IsRebinding && GetAction(action).WasPressedThisFrame();
        }

        public static bool WasReleasedThisFrame(GameInputAction action)
        {
            return !GameplaySuppressed && GetAction(action).WasReleasedThisFrame();
        }

        public static bool WasReleasedThisFrameForModal(GameInputAction action)
        {
            return !IsRebinding && GetAction(action).WasReleasedThisFrame();
        }

        public static string GetBindingDisplayString(GameInputAction action)
        {
            return GetAction(action).GetBindingDisplayString(0);
        }

        public static void ResetBinding(GameInputAction action)
        {
            EnsureInitialized();
            GameSettingsService.Current.ResetInputBindingOverride(action);
            ApplyPersistedBinding(action);
            BindingsChanged?.Invoke();
        }

        public static void ResetAllBindings()
        {
            EnsureInitialized();
            foreach (GameInputAction action in GameInputBindingCatalog.Actions)
                GameSettingsService.Current.ResetInputBindingOverride(action);

            ApplyAllPersistedBindings();
            BindingsChanged?.Invoke();
        }

        public static bool BeginInteractiveRebind(
            GameInputAction action,
            Action<InputRebindResult> completed)
        {
            EnsureInitialized();
            if (IsRebinding || !GameInputBindingCatalog.IsRebindable(action))
                return false;

            InputAction inputAction = GetAction(action);
            inputAction.Disable();
            rebindAction = action;
            rebindCompleted = completed;
            suppressRawInputThroughFrame = Time.frameCount;
            PlayerInputGate.SetModalInputBlocked(RebindInputOwner, true);

            rebindOperation = inputAction.PerformInteractiveRebinding(0)
                // Esc is excluded here and owned by EscapeInputRouter. Letting
                // both Input System and the router cancel in one frame could
                // cancel the rebind and then also close Settings.
                .WithControlsExcluding("<Keyboard>/escape")
                .WithControlsExcluding("<Keyboard>/f8")
                .WithControlsExcluding("<Keyboard>/digit1")
                .WithControlsExcluding("<Keyboard>/digit2")
                .WithControlsExcluding("<Keyboard>/digit3")
                .WithControlsExcluding("<Keyboard>/digit4")
                .WithControlsExcluding("<Keyboard>/digit5")
                .WithControlsExcluding("<Keyboard>/1")
                .WithControlsExcluding("<Keyboard>/2")
                .WithControlsExcluding("<Keyboard>/3")
                .WithControlsExcluding("<Keyboard>/4")
                .WithControlsExcluding("<Keyboard>/5")
                .WithControlsExcluding("<Keyboard>/numpad1")
                .WithControlsExcluding("<Keyboard>/numpad2")
                .WithControlsExcluding("<Keyboard>/numpad3")
                .WithControlsExcluding("<Keyboard>/numpad4")
                .WithControlsExcluding("<Keyboard>/numpad5")
                .OnComplete(OnRebindCompleted)
                .OnCancel(OnRebindCancelled);
            RebindStateChanged?.Invoke();
            rebindOperation.Start();
            return true;
        }

        public static void CancelInteractiveRebind()
        {
            rebindOperation?.Cancel();
        }

        private static bool GameplaySuppressed =>
            IsRebinding || PlayerInputGate.GameplayInputBlocked;

        private static InputAction GetAction(GameInputAction action)
        {
            EnsureInitialized();
            if (!RuntimeActions.TryGetValue(action, out InputAction inputAction))
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
            return inputAction;
        }

        private static void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;
            foreach (GameInputAction action in GameInputBindingCatalog.Actions)
            {
                var runtimeAction = new InputAction(
                    action.ToString(),
                    InputActionType.Button);
                runtimeAction.AddBinding(GameInputBindingCatalog.GetDefaultPath(action));
                RuntimeActions.Add(action, runtimeAction);
            }

            ApplyAllPersistedBindings();
            foreach (InputAction action in RuntimeActions.Values)
                action.Enable();

            subscribedSettings = GameSettingsService.Current;
            subscribedSettings.Changed += OnSettingsChanged;
            if (Application.isPlaying)
            {
                EscapeInputRouter.EnsureInstance().Register(
                    RebindInputOwner,
                    EscapeHandlerPriority.Rebinding,
                    () => IsRebinding,
                    CancelInteractiveRebind);
            }
        }

        private static void OnSettingsChanged()
        {
            if (!initialized || IsRebinding)
                return;

            ApplyAllPersistedBindings();
            BindingsChanged?.Invoke();
        }

        private static void ApplyAllPersistedBindings()
        {
            foreach (GameInputAction action in GameInputBindingCatalog.Actions)
                ApplyPersistedBinding(action);
        }

        private static void ApplyPersistedBinding(GameInputAction action)
        {
            if (!RuntimeActions.TryGetValue(action, out InputAction inputAction))
                return;

            bool wasEnabled = inputAction.enabled;
            if (wasEnabled)
                inputAction.Disable();

            inputAction.RemoveBindingOverride(0);
            string overridePath =
                GameSettingsService.Current.GetInputBindingOverride(action);
            if (!string.IsNullOrWhiteSpace(overridePath))
                inputAction.ApplyBindingOverride(0, overridePath);

            if (wasEnabled)
                inputAction.Enable();
        }

        private static void OnRebindCompleted(
            InputActionRebindingExtensions.RebindingOperation operation)
        {
            string path = operation.action.bindings[0].effectivePath;
            InputBindingValidation validation =
                GameInputBindingCatalog.ValidateOverride(
                    rebindAction,
                    path,
                    GameSettingsService.Current);

            if (validation.Status == InputBindingValidationStatus.Valid)
            {
                GameSettingsService.Current.SetInputBindingOverride(rebindAction, path);
                FinishRebind(new InputRebindResult(
                    rebindAction,
                    InputRebindOutcome.Applied,
                    path));
                return;
            }

            InputRebindOutcome outcome = validation.Status switch
            {
                InputBindingValidationStatus.Reserved => InputRebindOutcome.Reserved,
                InputBindingValidationStatus.Conflict => InputRebindOutcome.Conflict,
                _ => InputRebindOutcome.Invalid
            };
            FinishRebind(new InputRebindResult(
                rebindAction,
                outcome,
                path,
                validation.ConflictingAction));
        }

        private static void OnRebindCancelled(
            InputActionRebindingExtensions.RebindingOperation operation)
        {
            FinishRebind(new InputRebindResult(
                rebindAction,
                InputRebindOutcome.Cancelled));
        }

        private static void FinishRebind(InputRebindResult result)
        {
            InputActionRebindingExtensions.RebindingOperation completedOperation =
                rebindOperation;
            Action<InputRebindResult> callback = rebindCompleted;
            rebindOperation = null;
            rebindCompleted = null;
            suppressRawInputThroughFrame = Time.frameCount;
            completedOperation?.Dispose();

            ApplyPersistedBinding(result.Action);
            if (RuntimeActions.TryGetValue(result.Action, out InputAction inputAction) &&
                !inputAction.enabled)
            {
                inputAction.Enable();
            }

            PlayerInputGate.SetModalInputBlocked(RebindInputOwner, false);
            RebindStateChanged?.Invoke();
            if (result.Outcome == InputRebindOutcome.Applied)
                BindingsChanged?.Invoke();
            callback?.Invoke(result);
        }
    }
}
