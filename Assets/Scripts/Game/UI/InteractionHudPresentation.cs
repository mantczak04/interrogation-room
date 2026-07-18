using InterrogationRoom.Settings;

namespace InterrogationRoom.UI
{
    public enum InteractionHudMode
    {
        Hidden,
        Available,
        HoldAvailable,
        Active,
        Success,
        Warning,
        Cancelled,
        Seated
    }

    public readonly struct InteractionHudCopy
    {
        public InteractionHudCopy(
            string key,
            string title,
            string instruction,
            float progress)
        {
            Key = key;
            Title = title;
            Instruction = instruction;
            Progress = progress;
        }

        public string Key { get; }
        public string Title { get; }
        public string Instruction { get; }
        public float Progress { get; }
    }

    /// <summary>
    /// Keeps interaction copy deterministic and independently testable. The
    /// runtime HUD owns motion and colour; this class owns meaning and language.
    /// </summary>
    public static class InteractionHudPresentation
    {
        public static InteractionHudCopy Build(
            InteractionHudMode mode,
            string action,
            float progress,
            UiLanguage language)
        {
            string localizedAction = UiText.Get(action, language);

            switch (mode)
            {
                case InteractionHudMode.Available:
                    return new InteractionHudCopy(
                        "E",
                        localizedAction,
                        UiText.Get("NACIŚNIJ, ABY WYKONAĆ", language),
                        0f);

                case InteractionHudMode.HoldAvailable:
                    return new InteractionHudCopy(
                        "E",
                        localizedAction,
                        UiText.Get("PRZYTRZYMAJ, ABY ROZPOCZĄĆ", language),
                        0f);

                case InteractionHudMode.Active:
                    return new InteractionHudCopy(
                        "E",
                        localizedAction,
                        UiText.Get("TRZYMAJ • PUŚĆ, ABY PRZERWAĆ", language),
                        UnityEngine.Mathf.Clamp01(progress));

                case InteractionHudMode.Success:
                    return new InteractionHudCopy(
                        "OK",
                        localizedAction,
                        UiText.Get("CZYNNOŚĆ ZAKOŃCZONA", language),
                        1f);

                case InteractionHudMode.Warning:
                    return new InteractionHudCopy(
                        "!",
                        localizedAction,
                        UiText.Get("SPRAWDŹ SWÓJ AKTUALNY CEL", language),
                        0f);

                case InteractionHudMode.Cancelled:
                    return new InteractionHudCopy(
                        "×",
                        localizedAction,
                        UiText.Get("MOŻESZ SPRÓBOWAĆ PONOWNIE", language),
                        0f);

                case InteractionHudMode.Seated:
                    return new InteractionHudCopy(
                        "E",
                        localizedAction,
                        UiText.Get("NACIŚNIJ, ABY WSTAĆ", language),
                        0f);

                default:
                    return new InteractionHudCopy(null, null, null, 0f);
            }
        }
    }
}
