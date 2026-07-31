using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace InterrogationRoom.UI
{
    /// <summary>
    /// Shared cursor and local gameplay-input gate owned by the runtime UI assembly.
    /// PlayerController delegates to this seam so phase UI and FPP input cannot drift.
    /// </summary>
    public static class PlayerInputGate
    {
        private static bool uiBlocksGameplay = true;
        private static bool playerCursorReleased = true;
        private static readonly HashSet<object> ModalOwners = new();

        public static bool CursorReleased { get; private set; } = true;
        public static bool GameplayInputBlocked =>
            uiBlocksGameplay || playerCursorReleased || ModalOwners.Count > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            uiBlocksGameplay = true;
            playerCursorReleased = true;
            ModalOwners.Clear();
            ApplyCursorState(true);
        }

        public static void SetUiInputBlocked(bool blocked)
        {
            if (uiBlocksGameplay == blocked)
            {
                ApplyCursorState(GameplayInputBlocked);
                return;
            }

            uiBlocksGameplay = blocked;
            ApplyCursorState(GameplayInputBlocked);
        }

        public static void SetPlayerCursorReleased(bool released)
        {
            playerCursorReleased = released;
            ApplyCursorState(GameplayInputBlocked);
        }

        public static void SetModalInputBlocked(object owner, bool blocked)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            if (blocked)
                ModalOwners.Add(owner);
            else
                ModalOwners.Remove(owner);

            ApplyCursorState(GameplayInputBlocked);
        }

        private static void ApplyCursorState(bool released)
        {
            CursorReleased = released;
            UnityEngine.Cursor.lockState =
                released ? CursorLockMode.None : CursorLockMode.Locked;
            UnityEngine.Cursor.visible = released;
        }
    }

    public enum EscapeHandlerPriority
    {
        Context = 0,
        Settings = 100,
        Modal = 200,
        Rebinding = 300
    }

    /// <summary>
    /// Reads Esc once per frame and dispatches it to the highest active owner.
    /// Individual screens register behavior; none of them poll Esc directly.
    /// </summary>
    public sealed class EscapeInputRouter : MonoBehaviour
    {
        private static EscapeInputRouter instance;
        private readonly EscapeHandlerStack handlers = new();
#if UNITY_EDITOR
        private bool cursorWasLockedLastFrame;
#endif

        public static EscapeInputRouter EnsureInstance()
        {
            if (instance != null)
                return instance;

            var host = new GameObject(nameof(EscapeInputRouter));
            DontDestroyOnLoad(host);
            instance = host.AddComponent<EscapeInputRouter>();
            return instance;
        }

        public static void UnregisterOwner(object owner)
        {
            if (instance != null)
                instance.Unregister(owner);
        }

        public void Register(
            object owner,
            EscapeHandlerPriority priority,
            Func<bool> isActive,
            Action handler)
        {
            handlers.Register(owner, priority, isActive, handler);
        }

        public void Unregister(object owner)
        {
            handlers.Unregister(owner);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
#if UNITY_EDITOR
            cursorWasLockedLastFrame =
                UnityEngine.Cursor.lockState == CursorLockMode.Locked;
#endif
        }

        private void Update()
        {
            bool pressed =
                Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame;
#if UNITY_EDITOR
            // Unity's Game view can consume Esc while unlocking the cursor.
            bool editorConsumedPress =
                cursorWasLockedLastFrame &&
                UnityEngine.Cursor.lockState == CursorLockMode.None &&
                !PlayerInputGate.CursorReleased;
            cursorWasLockedLastFrame =
                UnityEngine.Cursor.lockState == CursorLockMode.Locked;
            pressed |= editorConsumedPress;
#endif
            if (pressed)
                handlers.TryHandleEscape();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }

    public sealed class EscapeHandlerStack
    {
        private readonly Dictionary<object, Registration> registrations = new();
        private long nextOrder;

        public void Register(
            object owner,
            EscapeHandlerPriority priority,
            Func<bool> isActive,
            Action handler)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (isActive == null)
                throw new ArgumentNullException(nameof(isActive));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            registrations[owner] = new Registration(
                priority,
                isActive,
                handler,
                nextOrder++);
        }

        public void Unregister(object owner)
        {
            if (owner != null)
                registrations.Remove(owner);
        }

        public bool TryHandleEscape()
        {
            Registration selected = null;
            foreach (Registration registration in registrations.Values)
            {
                if (!registration.IsActive())
                    continue;

                if (selected == null ||
                    registration.Priority > selected.Priority ||
                    registration.Priority == selected.Priority &&
                    registration.Order > selected.Order)
                {
                    selected = registration;
                }
            }

            if (selected == null)
                return false;

            selected.Handler();
            return true;
        }

        private sealed class Registration
        {
            public EscapeHandlerPriority Priority { get; }
            public Func<bool> IsActive { get; }
            public Action Handler { get; }
            public long Order { get; }

            public Registration(
                EscapeHandlerPriority priority,
                Func<bool> isActive,
                Action handler,
                long order)
            {
                Priority = priority;
                IsActive = isActive;
                Handler = handler;
                Order = order;
            }
        }
    }

    public static class UiControlStates
    {
        public const string SelectedClass = "is-selected";
        public const string ActiveClass = "is-active";

        public static void Normalize(VisualElement root)
        {
            if (root == null)
                return;

            foreach (Label label in root.Query<Label>().ToList())
                label.pickingMode = PickingMode.Ignore;
        }

        public static void SetSelected(VisualElement control, bool selected)
        {
            control?.EnableInClassList(SelectedClass, selected);
        }

        public static void SetActive(VisualElement control, bool active)
        {
            control?.EnableInClassList(ActiveClass, active);
        }
    }
}
