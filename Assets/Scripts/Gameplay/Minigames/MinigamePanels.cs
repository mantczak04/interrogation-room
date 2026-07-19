using System;
using System.Collections;
using System.Collections.Generic;
using InterrogationRoom.Minigames;
using InterrogationRoom.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace InterrogationRoom.Gameplay.Minigames
{
    public static class MinigamePanelHost
    {
        private static MinigamePanelBase activePanel;

        public static bool IsOpen => activePanel != null;

        public static void Open(
            MinigameSpec spec,
            Action succeeded,
            Action failed,
            Action cancelled)
        {
            Close(notifyCancellation: false);
            if (spec == null)
                return;

            var panelObject = new GameObject($"MinigamePanel_{spec.Kind}");
            switch (spec.Kind)
            {
                case MinigameKind.CodeLock:
                    activePanel = panelObject.AddComponent<CodeLockMinigamePanel>();
                    break;
                case MinigameKind.RecordsTerminal:
                    activePanel = panelObject.AddComponent<RecordsTerminalMinigamePanel>();
                    break;
                default:
                    activePanel = panelObject.AddComponent<FileSearchMinigamePanel>();
                    break;
            }

            activePanel.Open(spec, succeeded, failed, cancelled);
        }

        public static void Close(bool notifyCancellation)
        {
            if (activePanel == null)
                return;

            MinigamePanelBase panel = activePanel;
            activePanel = null;
            panel.Close(notifyCancellation);
        }

        internal static void NotifyClosed(MinigamePanelBase panel)
        {
            if (ReferenceEquals(activePanel, panel))
                activePanel = null;
        }
    }

    public abstract class MinigamePanelBase : MonoBehaviour
    {
        protected static readonly Color ScrimColor = new Color32(0x14, 0x17, 0x15, 0xE6);
        protected static readonly Color PaperColor = new Color32(0xE8, 0xDC, 0xC5, 0xFF);
        protected static readonly Color InkColor = new Color32(0x2B, 0x2A, 0x24, 0xFF);
        protected static readonly Color MutedInkColor = new Color32(0x6E, 0x68, 0x57, 0xFF);
        protected static readonly Color AccentGreen = new Color32(0x5F, 0x6F, 0x52, 0xFF);
        protected static readonly Color AccentDark = new Color32(0x46, 0x53, 0x3D, 0xFF);
        protected static readonly Color ButtonPaper = new Color32(0xD9, 0xCB, 0xAF, 0xFF);
        protected static readonly Color WarningColor = new Color32(0x8C, 0x53, 0x2B, 0xFF);
        protected static readonly Color LightText = new Color32(0xF0, 0xE9, 0xD8, 0xFF);

        private Action succeeded;
        private Action failed;
        private Action cancelled;
        private bool cursorWasReleased;
        private bool closing;
        private Font font;
        private Canvas canvas;
        protected Text StatusLabel { get; private set; }
        protected Transform ContentRoot { get; private set; }
        protected MinigameSpec Spec { get; private set; }
        protected int LaunchSeed { get; private set; }
        private GameObject verificationRow;
        private CanvasGroup verificationGroup;
        private RectTransform verificationSpinner;
        private bool verifying;

        public void Open(
            MinigameSpec spec,
            Action succeededCallback,
            Action failedCallback,
            Action cancelledCallback)
        {
            Spec = spec;
            LaunchSeed = spec.NextLaunchSeed();
            succeeded = succeededCallback;
            failed = failedCallback;
            cancelled = cancelledCallback;
            cursorWasReleased = PlayerController.CursorReleased;
            PlayerController.SetCursorReleased(true);
            EnsureEventSystem();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildFrame(UiText.Get(Title), UiText.Get(spec.IntroText));
            BuildContent(ContentRoot);
            BuildVerificationIndicator(ContentRoot);
        }

        public void Close(bool notifyCancellation)
        {
            if (closing)
                return;

            closing = true;
            if (notifyCancellation)
                cancelled?.Invoke();
            RestoreCursor();
            MinigamePanelHost.NotifyClosed(this);
            Destroy(gameObject);
        }

        protected abstract string Title { get; }
        protected abstract void BuildContent(Transform parent);

        protected void Succeed()
        {
            if (closing)
                return;
            SetStatus(UiText.Get("Wynik przyjęty. Oczekiwanie na serwer…"), AccentDark);
            SetButtonsInteractable(false);
            succeeded?.Invoke();
        }

        protected void Fail(string message)
        {
            if (closing)
                return;
            SetStatus(message, WarningColor);
            SetButtonsInteractable(false);
            failed?.Invoke();
        }

        protected void SetStatus(string value, Color color)
        {
            StatusLabel.text = value;
            StatusLabel.color = color;
        }

        protected virtual void SetButtonsInteractable(bool interactable)
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button.name != "Cancel")
                    button.interactable = interactable;
            }
        }

        protected void BeginVerification(
            Func<MinigameAttemptResult> evaluate,
            Action<MinigameAttemptResult> resolved)
        {
            if (verifying || closing || evaluate == null || resolved == null)
                return;

            StartCoroutine(VerifyAnswer(evaluate, resolved));
        }

        private IEnumerator VerifyAnswer(
            Func<MinigameAttemptResult> evaluate,
            Action<MinigameAttemptResult> resolved)
        {
            const float verificationDurationSeconds = 5f;
            verifying = true;
            SetButtonsInteractable(false);
            SetStatus(string.Empty, MutedInkColor);
            verificationGroup.alpha = 1f;

            float elapsed = 0f;
            while (elapsed < verificationDurationSeconds)
            {
                float delta = Time.unscaledDeltaTime;
                elapsed += delta;
                verificationSpinner.Rotate(0f, 0f, -240f * delta);
                yield return null;
            }

            verificationGroup.alpha = 0f;
            verifying = false;
            SetButtonsInteractable(true);
            resolved(evaluate());
        }

        private void BuildVerificationIndicator(Transform parent)
        {
            verificationRow = new GameObject("Verification", typeof(RectTransform));
            verificationRow.transform.SetParent(parent, false);
            verificationRow.AddComponent<LayoutElement>().preferredHeight = 48f;
            verificationGroup = verificationRow.AddComponent<CanvasGroup>();
            verificationGroup.alpha = 0f;
            verificationGroup.blocksRaycasts = false;
            verificationGroup.interactable = false;
            var row = verificationRow.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 12f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var spinnerObject = new GameObject("Spinner", typeof(RectTransform));
            spinnerObject.transform.SetParent(verificationRow.transform, false);
            spinnerObject.AddComponent<LayoutElement>().preferredWidth = 40f;
            verificationSpinner = spinnerObject.GetComponent<RectTransform>();
            verificationSpinner.sizeDelta = new Vector2(40f, 40f);

            const int dotCount = 8;
            const float radius = 14f;
            for (int index = 0; index < dotCount; index++)
            {
                var dotObject = new GameObject($"Dot_{index}", typeof(RectTransform));
                dotObject.transform.SetParent(spinnerObject.transform, false);
                RectTransform dot = dotObject.GetComponent<RectTransform>();
                dot.anchorMin = new Vector2(0.5f, 0.5f);
                dot.anchorMax = new Vector2(0.5f, 0.5f);
                dot.sizeDelta = new Vector2(5f, 5f);
                float angle = index * Mathf.PI * 2f / dotCount;
                dot.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Image image = dotObject.AddComponent<Image>();
                Color color = AccentDark;
                color.a = Mathf.Lerp(0.2f, 1f, (index + 1f) / dotCount);
                image.color = color;
                image.raycastTarget = false;
            }

            CreateLabel(
                verificationRow.transform,
                "VerificationLabel",
                UiText.Get("Weryfikowanie odpowiedzi..."),
                18,
                MutedInkColor,
                TextAnchor.MiddleLeft,
                FontStyle.Bold,
                40f);
        }

        protected Text CreateLabel(
            Transform parent,
            string name,
            string value,
            int fontSize,
            Color color,
            TextAnchor alignment,
            FontStyle style = FontStyle.Normal,
            float preferredHeight = 32f)
        {
            var labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            Text label = labelObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.text = value;
            label.raycastTarget = false;
            LayoutElement element = labelObject.AddComponent<LayoutElement>();
            element.preferredHeight = preferredHeight;
            return label;
        }

        protected Button CreateButton(
            Transform parent,
            string name,
            string label,
            Action clicked,
            Color? background = null,
            Color? textColor = null,
            float height = 48f)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<LayoutElement>().preferredHeight = height;
            Image image = buttonObject.AddComponent<Image>();
            image.color = background ?? ButtonPaper;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            button.onClick.AddListener(() => clicked());

            Text buttonLabel = CreateLabel(
                buttonObject.transform,
                "Label",
                label,
                18,
                textColor ?? InkColor,
                TextAnchor.MiddleCenter,
                FontStyle.Bold,
                height);
            RectTransform labelRect = buttonLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return button;
        }

        private void Update()
        {
            if (!closing && WasCancelPressed())
                Close(notifyCancellation: true);
        }

        private void OnDestroy()
        {
            RestoreCursor();
            MinigamePanelHost.NotifyClosed(this);
        }

        private void BuildFrame(string title, string intro)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4500;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            Image scrim = CreateImage(transform, "Scrim", ScrimColor, true);
            Stretch(scrim.rectTransform);

            Image panel = CreateImage(transform, "PaperPanel", PaperColor, true);
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(860f, 0f);
            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 28, 28);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateLabel(panel.transform, "Title", title, 34, InkColor, TextAnchor.MiddleLeft, FontStyle.Bold, 46f);
            Image accent = CreateImage(panel.transform, "Accent", AccentGreen);
            accent.gameObject.AddComponent<LayoutElement>().preferredHeight = 3f;
            CreateLabel(panel.transform, "Intro", intro, 18, MutedInkColor, TextAnchor.MiddleLeft, FontStyle.Italic, 54f);

            var contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(panel.transform, false);
            ContentRoot = contentObject.transform;

            StatusLabel = CreateLabel(
                panel.transform,
                "Status",
                "",
                17,
                MutedInkColor,
                TextAnchor.MiddleCenter,
                FontStyle.Bold,
                30f);
            CreateButton(
                panel.transform,
                "Cancel",
                UiText.Get("Anuluj"),
                () => Close(notifyCancellation: true),
                ButtonPaper,
                InkColor,
                44f);
        }

        private void RestoreCursor()
        {
            if (!cursorWasReleased)
                PlayerController.SetCursorReleased(false);
            cursorWasReleased = true;
        }

        private static Image CreateImage(
            Transform parent,
            string name,
            Color color,
            bool raycastTarget = false)
        {
            var imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private static bool WasCancelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }

    public sealed class FileSearchMinigamePanel : MinigamePanelBase
    {
        private FileSearchSession session;
        private Transform gridRoot;
        private readonly List<Button> folderButtons = new List<Button>();
        private Text inspectedLabel;
        private Button confirmButton;

        protected override string Title => "Przeszukiwanie akt";

        protected override void BuildContent(Transform parent)
        {
            session = FileSearchSession.Create(LaunchSeed, Spec.FolderCount, Spec.TargetYear);
            var layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            CreateLabel(
                parent,
                "Criterion",
                UiText.Format(
                    "Notatka magazynowa: rocznik {0}, końcówka numeru {1:00}, suma cyfr {2}.",
                    session.TargetYear,
                    session.TargetNumberSuffix,
                    session.TargetDigitSum),
                20,
                InkColor,
                TextAnchor.MiddleCenter,
                FontStyle.Bold,
                52f);
            var gridObject = new GameObject("FolderGrid", typeof(RectTransform));
            gridObject.transform.SetParent(parent, false);
            gridObject.AddComponent<LayoutElement>().preferredHeight = 210f;
            GridLayoutGroup grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(185f, 56f);
            grid.spacing = new Vector2(10f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;
            gridRoot = gridObject.transform;
            RebuildFolders();

            inspectedLabel = CreateLabel(
                parent,
                "InspectedFolder",
                UiText.Get("Wybierz teczkę, aby sprawdzić jej etykietę."),
                18,
                MutedInkColor,
                TextAnchor.MiddleCenter,
                FontStyle.Normal,
                36f);
            confirmButton = CreateButton(
                parent,
                "ConfirmFolder",
                UiText.Get("Otwórz wybraną teczkę"),
                ConfirmFolder,
                AccentGreen,
                LightText,
                48f);
            confirmButton.interactable = false;
        }

        private void RebuildFolders()
        {
            foreach (Transform child in gridRoot)
                child.gameObject.SetActive(false);
            folderButtons.Clear();

            for (int index = 0; index < session.Folders.Count; index++)
            {
                int selectedIndex = index;
                FileFolderOption folder = session.Folders[index];
                folderButtons.Add(CreateButton(
                    gridRoot,
                    $"Folder_{index}",
                    folder.Label,
                    () => SelectFolder(selectedIndex),
                    ButtonPaper,
                    InkColor,
                    56f));
            }
        }

        private void SelectFolder(int index)
        {
            if (!session.Inspect(index))
                return;

            FileFolderOption folder = session.Folders[index];
            inspectedLabel.text = UiText.Format(
                "Wybrano: {0} — rocznik {1}.",
                folder.Signature,
                folder.Year);
            inspectedLabel.color = InkColor;
            confirmButton.interactable = true;
        }

        private void ConfirmFolder()
        {
            BeginVerification(session.ConfirmInspected, ResolveFolderResult);
        }

        private void ResolveFolderResult(MinigameAttemptResult result)
        {
            if (result == MinigameAttemptResult.Success)
            {
                Succeed();
                return;
            }

            RebuildFolders();
            inspectedLabel.text = UiText.Get("Teczki przełożono. Odczytaj wskazówki ponownie.");
            inspectedLabel.color = WarningColor;
            confirmButton.interactable = false;
            SetStatus(UiText.Format(
                "Nie te akta. Strata {0} s — teczki przełożono.",
                session.PenaltySeconds), WarningColor);
            StartCoroutine(UnlockAfterDelay());
        }

        private IEnumerator UnlockAfterDelay()
        {
            SetButtonsInteractable(false);
            yield return new WaitForSecondsRealtime(Spec.WrongChoiceDelay);
            SetButtonsInteractable(true);
            confirmButton.interactable = false;
            SetStatus(UiText.Get("Spróbuj ponownie."), MutedInkColor);
        }
    }

    public sealed class CodeLockMinigamePanel : MinigamePanelBase
    {
        private CodeLockSession session;
        private readonly int[] digits = new int[3];
        private readonly Text[] digitLabels = new Text[3];

        protected override string Title => "Zamek szyfrowy";

        protected override void BuildContent(Transform parent)
        {
            session = Spec.CreateCodeLockSession(LaunchSeed);
            var layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            CreateLabel(
                parent,
                "CodeBrief",
                UiText.Format(
                    "Notatka technika:\npierwsza + środkowa = {0},  środkowa + ostatnia = {1},  pierwsza + ostatnia = {2}",
                    session.FirstPairSum,
                    session.LastPairSum,
                    session.OuterPairSum),
                20,
                InkColor,
                TextAnchor.MiddleCenter,
                FontStyle.Bold,
                62f);

            var dialsObject = new GameObject("Dials", typeof(RectTransform));
            dialsObject.transform.SetParent(parent, false);
            HorizontalLayoutGroup dials = dialsObject.AddComponent<HorizontalLayoutGroup>();
            dials.spacing = 18f;
            dials.childControlWidth = true;
            dials.childControlHeight = true;
            dials.childForceExpandWidth = true;
            dials.childForceExpandHeight = false;

            for (int index = 0; index < digits.Length; index++)
                BuildDial(dialsObject.transform, index);

            CreateButton(parent, "Confirm", UiText.Get("Zatwierdź kod"), Confirm, AccentGreen, LightText, 52f);
        }

        private void BuildDial(Transform parent, int index)
        {
            var dialObject = new GameObject($"Dial_{index}", typeof(RectTransform));
            dialObject.transform.SetParent(parent, false);
            VerticalLayoutGroup layout = dialObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            CreateButton(dialObject.transform, "Plus", "+", () => ChangeDigit(index, 1), ButtonPaper, InkColor, 38f);
            digitLabels[index] = CreateLabel(
                dialObject.transform,
                "Digit",
                "0",
                42,
                AccentDark,
                TextAnchor.MiddleCenter,
                FontStyle.Bold,
                56f);
            CreateButton(dialObject.transform, "Minus", "−", () => ChangeDigit(index, -1), ButtonPaper, InkColor, 38f);
        }

        private void ChangeDigit(int index, int delta)
        {
            digits[index] = (digits[index] + delta + 10) % 10;
            digitLabels[index].text = digits[index].ToString();
        }

        private void Confirm()
        {
            int candidate = digits[0] * 100 + digits[1] * 10 + digits[2];
            BeginVerification(() => session.Enter(candidate), ResolveCodeResult);
        }

        private void ResolveCodeResult(MinigameAttemptResult result)
        {
            if (result == MinigameAttemptResult.Success)
            {
                Succeed();
                return;
            }

            if (result == MinigameAttemptResult.Restarted)
            {
                Array.Clear(digits, 0, digits.Length);
                foreach (Text label in digitLabels)
                    label.text = "0";
                SetStatus(UiText.Get(
                    "Limit prób. Zamek zresetował pokrętła — możesz spróbować ponownie."), WarningColor);
                return;
            }

            int remaining = session.MaximumAttempts - session.AttemptsInCurrentRun;
            SetStatus(UiText.Format("Błędny kod. Pozostało prób: {0}.", remaining), WarningColor);
        }
    }

    public sealed class RecordsTerminalMinigamePanel : MinigamePanelBase
    {
        private const float FilterLoadingDurationSeconds = 3f;

        private RecordsTerminalSession session;
        private Transform recordsRoot;
        private Text loadingRecordsLabel;
        private Text openedRecordLabel;
        private Button confirmButton;
        private bool loadingFilter;

        protected override string Title => "Terminal kartoteki";

        protected override void BuildContent(Transform parent)
        {
            session = RecordsTerminalSession.Create(LaunchSeed, Spec.RecordCount);
            var layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            CreateLabel(
                parent,
                "Criterion",
                UiText.Format(
                    "Notatka: nazwisko na „{0}”, jednostka „{1}”, okres {2}–{3}.",
                    session.TargetSurnameInitial,
                    session.TargetUnit,
                    session.TargetYearBandStart,
                    session.TargetYearBandStart + 4),
                19,
                InkColor,
                TextAnchor.MiddleCenter,
                FontStyle.Bold,
                52f);

            CreateFilterRow(parent, UiText.Get("Jednostka"), session.UnitOptions, ApplyUnitFilter);
            var yearLabels = new List<string>();
            for (int index = 0; index < session.YearBandOptions.Count; index++)
            {
                int start = session.YearBandOptions[index];
                yearLabels.Add($"{start}–{start + 4}");
            }
            CreateFilterRow(parent, UiText.Get("Okres"), yearLabels, ApplyYearFilter);

            var scrollObject = new GameObject("RecordsScroll", typeof(RectTransform));
            scrollObject.transform.SetParent(parent, false);
            scrollObject.AddComponent<LayoutElement>().preferredHeight = 180f;
            Image background = scrollObject.AddComponent<Image>();
            background.color = new Color32(0xCB, 0xBE, 0xA2, 0xFF);
            ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewportObject = new GameObject("Viewport", typeof(RectTransform));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(8f, 8f);
            viewport.offsetMax = new Vector2(-8f, -8f);
            viewportObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewportObject.AddComponent<Mask>().showMaskGraphic = false;

            var contentObject = new GameObject("Records", typeof(RectTransform));
            contentObject.transform.SetParent(viewportObject.transform, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup recordsLayout = contentObject.AddComponent<VerticalLayoutGroup>();
            recordsLayout.spacing = 7f;
            recordsLayout.childControlWidth = true;
            recordsLayout.childControlHeight = true;
            recordsLayout.childForceExpandHeight = false;
            contentObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            recordsRoot = content;
            loadingRecordsLabel = CreateLabel(
                recordsRoot,
                "LoadingRecords",
                UiText.Get("Ładowanie danych..."),
                20,
                MutedInkColor,
                TextAnchor.MiddleCenter,
                FontStyle.Bold,
                160f);
            loadingRecordsLabel.gameObject.SetActive(false);
            RebuildRecords();

            openedRecordLabel = CreateLabel(
                parent,
                "OpenedRecord",
                UiText.Get("Ustaw oba filtry, następnie otwórz rekord i sprawdź szczegóły."),
                17,
                MutedInkColor,
                TextAnchor.MiddleCenter,
                FontStyle.Normal,
                42f);
            confirmButton = CreateButton(
                parent,
                "ConfirmRecord",
                UiText.Get("Zatwierdź otwarty rekord"),
                ConfirmRecord,
                AccentGreen,
                LightText,
                46f);
            confirmButton.interactable = false;
        }

        private void CreateFilterRow(
            Transform parent,
            string label,
            IReadOnlyList<string> options,
            Action<int> selected)
        {
            var rowObject = new GameObject($"{label}Filters", typeof(RectTransform));
            rowObject.transform.SetParent(parent, false);
            rowObject.AddComponent<LayoutElement>().preferredHeight = 42f;
            var row = rowObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 7f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = false;

            for (int index = 0; index < options.Count; index++)
            {
                int selectedIndex = index;
                CreateButton(
                    rowObject.transform,
                    $"{label}_{index}",
                    options[index],
                    () => selected(selectedIndex),
                    ButtonPaper,
                    InkColor,
                    40f);
            }
        }

        private void ApplyUnitFilter(int index)
        {
            if (!loadingFilter)
                StartCoroutine(ApplyFilterAfterLoading(isUnitFilter: true, index));
        }

        private void ApplyYearFilter(int index)
        {
            if (!loadingFilter)
                StartCoroutine(ApplyFilterAfterLoading(isUnitFilter: false, index));
        }

        private IEnumerator ApplyFilterAfterLoading(bool isUnitFilter, int index)
        {
            loadingFilter = true;
            ResetOpenedRecord();
            HideRecordButtons();
            loadingRecordsLabel.gameObject.SetActive(true);
            SetButtonsInteractable(false);

            float elapsed = 0f;
            int previousDotCount = -1;
            while (elapsed < FilterLoadingDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                int dotCount = Mathf.FloorToInt(elapsed * 2f) % 4;
                if (dotCount != previousDotCount)
                {
                    previousDotCount = dotCount;
                    loadingRecordsLabel.text = UiText.Format(
                        "Ładowanie danych{0}",
                        new string('.', dotCount));
                }
                yield return null;
            }

            if (isUnitFilter)
                session.SetUnitFilter(session.UnitOptions[index]);
            else
                session.SetYearBandFilter(session.YearBandOptions[index]);

            loadingFilter = false;
            loadingRecordsLabel.gameObject.SetActive(false);
            SetButtonsInteractable(true);
            ResetOpenedRecord();
            RebuildRecords();
        }

        private void HideRecordButtons()
        {
            foreach (Transform child in recordsRoot)
                child.gameObject.SetActive(false);
        }

        private void RebuildRecords()
        {
            HideRecordButtons();

            for (int visibleIndex = 0; visibleIndex < session.VisibleRecordIndices.Count; visibleIndex++)
            {
                int recordIndex = session.VisibleRecordIndices[visibleIndex];
                CreateButton(
                    recordsRoot,
                    $"Record_{recordIndex}",
                    session.Records[recordIndex].Label,
                    () => SelectRecord(recordIndex),
                    ButtonPaper,
                    InkColor,
                    46f);
            }

            if (session.VisibleRecordIndices.Count == 0)
                SetStatus(UiText.Get("Ustaw właściwą jednostkę i okres."), MutedInkColor);
            else
                SetStatus(UiText.Format(
                    "Wyniki filtrowania: {0}. Otwórz właściwy rekord.",
                    session.VisibleRecordIndices.Count), MutedInkColor);
        }

        private void SelectRecord(int index)
        {
            if (!session.OpenRecord(index))
                return;

            RecordsTerminalOption record = session.Records[index];
            openedRecordLabel.text = UiText.Format(
                "OTWARTO: {0}\nRok: {1}   Jednostka: {2}",
                record.Surname,
                record.Year,
                record.Unit);
            openedRecordLabel.color = InkColor;
            confirmButton.interactable = true;
        }

        private void ConfirmRecord()
        {
            BeginVerification(session.ConfirmOpenedRecord, ResolveRecordResult);
        }

        private void ResolveRecordResult(MinigameAttemptResult result)
        {
            if (result == MinigameAttemptResult.Success)
            {
                Succeed();
                return;
            }

            ResetOpenedRecord();
            SetStatus(UiText.Get(
                "To nie ten rekord. Terminal pozostaje otwarty, ale tracisz czas."), WarningColor);
        }

        private void ResetOpenedRecord()
        {
            openedRecordLabel.text = UiText.Get("Otwórz rekord i porównaj jego szczegóły z notatką.");
            openedRecordLabel.color = MutedInkColor;
            confirmButton.interactable = false;
        }
    }
}
