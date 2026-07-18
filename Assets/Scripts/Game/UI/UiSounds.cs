using UnityEngine;
using UnityEngine.UIElements;

namespace InterrogationRoom.UI
{
    /// <summary>
    /// Hover and click feedback for UI Toolkit screens. A screen calls
    /// <see cref="Bind"/> once with its root and every button underneath it —
    /// including ones added later — starts making noise; nothing else needs an
    /// AudioSource, a serialized clip, or a per-button component.
    /// </summary>
    public static class UiSounds
    {
        private const string ClipResource = "Audio/UiClick";

        /// <summary>
        /// Put this class on any element to silence hover for everything under
        /// it; clicks still sound. Hover feedback earns its place on a short
        /// list of large entries the player is choosing between — the main
        /// menu. On a dense panel the cursor crosses controls on its way
        /// somewhere else, and the sound stops being feedback and starts being
        /// noise. The rule lives in UXML next to the layout because that is
        /// where the density is visible.
        /// </summary>
        public const string NoHoverSoundClass = "no-hover-sound";

        /// <summary>
        /// The clip is RMS-normalized to -20 dBFS, so these land the click near
        /// -32 dBFS and the hover near -38 dBFS at the listener. Menu feedback
        /// should sit under the music, not on top of it.
        /// </summary>
        private const float HoverVolume = 0.125f;
        private const float ClickVolume = 0.25f;

        /// <summary>
        /// Hover is pitched <em>down</em>, not up. The clip is a bright tick
        /// centred around 8 kHz, so raising it for "lightness" only makes hover
        /// the harshest sound in the menu — and hover fires far more often than
        /// click.
        /// </summary>
        private const float HoverPitch = 0.92f;

        /// <summary>Stops a fast cursor sweep from turning the menu into a rattle.</summary>
        private const float HoverCooldown = 0.05f;

        private static AudioSource hoverSource;
        private static AudioSource clickSource;
        private static AudioClip clip;
        private static float lastHoverTime = float.NegativeInfinity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            hoverSource = null;
            clickSource = null;
            clip = null;
            lastHoverTime = float.NegativeInfinity;
        }

        /// <summary>
        /// Wires hover and click sounds for every button in <paramref name="root"/>.
        /// Callbacks sit on the root rather than on individual buttons so screens
        /// that build their controls lazily are covered too. Safe to call again
        /// after a rebuild: re-registering the same handlers is a no-op.
        /// </summary>
        public static void Bind(VisualElement root)
        {
            if (root == null)
                return;

            root.RegisterCallback<PointerEnterEvent>(OnPointerEnter, TrickleDown.TrickleDown);
            root.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);

            // Load and decompress now rather than on the first hover, so the
            // first sound of a screen is not the one that arrives late.
            EnsureSources();
        }

        public static void Unbind(VisualElement root)
        {
            if (root == null)
                return;

            root.UnregisterCallback<PointerEnterEvent>(OnPointerEnter, TrickleDown.TrickleDown);
            root.UnregisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
        }

        public static void PlayHover()
        {
            float now = Time.unscaledTime;
            if (now < lastHoverTime + HoverCooldown)
                return;

            lastHoverTime = now;
            Play(EnsureSources() ? hoverSource : null, HoverVolume);
        }

        public static void PlayClick()
        {
            Play(EnsureSources() ? clickSource : null, ClickVolume);
        }

        private static void OnPointerEnter(PointerEnterEvent evt)
        {
            VisualElement control = FindControl(evt.target as VisualElement);
            if (control != null && !IsHoverSilenced(control))
                PlayHover();
        }

        private static void OnClick(ClickEvent evt)
        {
            if (FindControl(evt.target as VisualElement) != null)
                PlayClick();
        }

        /// <summary>
        /// Returns the control the event really belongs to, or null if the
        /// element is not part of one. A Button is its own target, but a Toggle
        /// delivers events on its checkmark or label, so matching the target
        /// alone would leave checkboxes silent. Disabled controls return null:
        /// a click that does nothing should not sound like one that did.
        /// </summary>
        private static VisualElement FindControl(VisualElement element)
        {
            for (VisualElement node = element; node != null; node = node.parent)
            {
                if (node is Button || node is Toggle)
                    return node.enabledInHierarchy ? node : null;
            }

            return null;
        }

        /// <summary>
        /// Walks up to the panel root looking for <see cref="NoHoverSoundClass"/>.
        /// Hover fires rarely enough, and hierarchies are shallow enough, that
        /// the walk is cheaper than keeping a registry in sync with a tree the
        /// screens rebuild at runtime.
        /// </summary>
        private static bool IsHoverSilenced(VisualElement element)
        {
            for (VisualElement node = element; node != null; node = node.parent)
            {
                if (node.ClassListContains(NoHoverSoundClass))
                    return true;
            }

            return false;
        }

        private static void Play(AudioSource source, float volume)
        {
            if (source == null || clip == null)
                return;

            source.PlayOneShot(clip, volume);
        }

        private static bool EnsureSources()
        {
            if (clickSource != null)
                return true;

            clip = Resources.Load<AudioClip>(ClipResource);
            if (clip == null)
            {
                Debug.LogWarning($"UiSounds: missing clip at Resources/{ClipResource}.");
                return false;
            }

            var host = new GameObject("UiSounds") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(host);

            clickSource = CreateSource(host, 1f);
            hoverSource = CreateSource(host, HoverPitch);
            return true;
        }

        private static AudioSource CreateSource(GameObject host, float pitch)
        {
            AudioSource source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.bypassEffects = true;
            source.bypassReverbZones = true;
            source.ignoreListenerPause = true;
            source.ignoreListenerVolume = false;
            source.pitch = pitch;
            source.volume = 1f;
            return source;
        }
    }
}
