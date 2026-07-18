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
            if (IsAudibleControl(evt.target as VisualElement))
                PlayHover();
        }

        private static void OnClick(ClickEvent evt)
        {
            if (IsAudibleControl(evt.target as VisualElement))
                PlayClick();
        }

        /// <summary>
        /// Disabled controls stay silent: a click that does nothing should not
        /// sound like a click that did something.
        /// </summary>
        private static bool IsAudibleControl(VisualElement element)
        {
            return element is Button && element.enabledInHierarchy;
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
