using System.Collections.Generic;
using InterrogationRoom.Gameplay.Characters;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InterrogationRoom.UI
{
    [DisallowMultipleComponent]
    public sealed class DanceRadialMenu : MonoBehaviour, IDanceRadialMenu
    {
        private const string PanelSettingsResource = "UI/UIPanelSettings";
        private const string StyleSheetResource = "UI/DanceRadialMenu";
        private const float WheelSize = 600f;
        private const float DeadZoneRadius = 96f;

        private static readonly Vector2[] OptionPositions =
        {
            new(200f, 72f),
            new(372f, 254f),
            new(200f, 436f),
            new(28f, 254f)
        };

        private static readonly Color AccentColor = new(0.88f, 0.71f, 0.41f, 1f);
        private static readonly Color TextColor = new(0.91f, 0.89f, 0.84f, 1f);

        private UIDocument document;
        private VisualElement overlay;
        private DanceRadialWheelElement wheel;
        private VisualElement center;
        private Label centerLabel;
        private readonly VisualElement[] options =
            new VisualElement[DanceRadialSelection.DanceCount];
        private readonly Label[] optionNumbers =
            new Label[DanceRadialSelection.DanceCount];
        private bool openedWhileDancing;

        public bool IsOpen { get; private set; }
        public int SelectedDance { get; private set; } = DanceRadialSelection.NoSelection;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterFactory()
        {
            DanceRadialMenuHost.Factory =
                owner => owner.AddComponent<DanceRadialMenu>();
        }

        private void Awake()
        {
            BuildUi();
            EscapeInputRouter.EnsureInstance().Register(
                this,
                EscapeHandlerPriority.Modal,
                () => IsOpen,
                Cancel);
        }

        private void OnDestroy()
        {
            EscapeInputRouter.UnregisterOwner(this);
            PlayerInputGate.SetModalInputBlocked(this, false);
        }

        public void Open(bool currentlyDancing)
        {
            if (IsOpen || overlay == null)
                return;

            IsOpen = true;
            openedWhileDancing = currentlyDancing;
            SelectedDance = DanceRadialSelection.NoSelection;
            overlay.style.display = DisplayStyle.Flex;
            PlayerInputGate.SetModalInputBlocked(this, true);
            WarpPointerToScreenCenter();

            // The warp only lands on the next input update, so reading the pointer now would
            // still return the pre-open position and pre-select a sector. Paint the neutral
            // state instead and let the per-frame RefreshSelection take over.
            RefreshHighlight();
            PlayOpenTransition();
        }

        public int Close()
        {
            if (!IsOpen)
                return DanceRadialSelection.NoSelection;

            int selection = SelectedDance;
            IsOpen = false;
            SelectedDance = DanceRadialSelection.NoSelection;
            overlay.style.display = DisplayStyle.None;
            PlayerInputGate.SetModalInputBlocked(this, false);
            return selection;
        }

        public void Cancel()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            SelectedDance = DanceRadialSelection.NoSelection;
            overlay.style.display = DisplayStyle.None;
            PlayerInputGate.SetModalInputBlocked(this, false);
        }

        public void RefreshSelection()
        {
            if (!IsOpen)
                return;

            Vector2 offset = ReadPointerPosition() -
                new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            int nextSelection = DanceRadialSelection.FromPointerOffset(
                offset,
                DeadZoneRadius * ResolvePanelScale());
            if (nextSelection == SelectedDance)
                return;

            SelectedDance = nextSelection;
            RefreshHighlight();
        }

        private void BuildUi()
        {
            PanelSettings panelSettings = Resources.Load<PanelSettings>(PanelSettingsResource);
            if (panelSettings == null)
            {
                Debug.LogError(
                    $"[{nameof(DanceRadialMenu)}] Missing Resources/{PanelSettingsResource}.",
                    this);
                return;
            }

            var uiObject = new GameObject("Dance Radial Menu UI");
            uiObject.transform.SetParent(transform, false);
            document = uiObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.sortingOrder = 200;

            overlay = document.rootVisualElement;
            overlay.name = "dance-radial-overlay";
            overlay.pickingMode = PickingMode.Ignore;
            StyleSheet styleSheet = Resources.Load<StyleSheet>(StyleSheetResource);
            if (styleSheet != null)
                overlay.styleSheets.Add(styleSheet);
            overlay.AddToClassList("ui-font");
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0f;
            overlay.style.right = 0f;
            overlay.style.top = 0f;
            overlay.style.bottom = 0f;
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.38f);
            SetTransition(overlay, 0.1f, "opacity");

            wheel = new DanceRadialWheelElement
            {
                name = "dance-radial-wheel",
                pickingMode = PickingMode.Ignore
            };
            wheel.style.width = WheelSize;
            wheel.style.height = WheelSize;
            wheel.style.position = Position.Relative;
            wheel.style.borderTopLeftRadius = WheelSize * 0.5f;
            wheel.style.borderTopRightRadius = WheelSize * 0.5f;
            wheel.style.borderBottomLeftRadius = WheelSize * 0.5f;
            wheel.style.borderBottomRightRadius = WheelSize * 0.5f;
            wheel.style.backgroundColor = new Color(0.078f, 0.094f, 0.106f, 0.98f);
            wheel.style.overflow = Overflow.Hidden;
            wheel.style.transformOrigin = new TransformOrigin(
                Length.Percent(50f),
                Length.Percent(50f));
            SetTransition(wheel, 0.12f, "scale");
            SetBorder(wheel, new Color(0.5f, 0.41f, 0.28f, 1f), 4f);
            overlay.Add(wheel);

            for (int index = 0; index < options.Length; index++)
            {
                VisualElement option = CreateOption(index);
                options[index] = option;
                wheel.Add(option);
            }

            center = new VisualElement { pickingMode = PickingMode.Ignore };
            center.style.position = Position.Absolute;
            center.style.left = (WheelSize - 176f) * 0.5f;
            center.style.top = (WheelSize - 176f) * 0.5f;
            center.style.width = 176f;
            center.style.height = 176f;
            center.style.alignItems = Align.Center;
            center.style.justifyContent = Justify.Center;
            center.style.borderTopLeftRadius = 88f;
            center.style.borderTopRightRadius = 88f;
            center.style.borderBottomLeftRadius = 88f;
            center.style.borderBottomRightRadius = 88f;
            center.style.backgroundColor = new Color(0.078f, 0.094f, 0.106f, 1f);
            SetBorder(center, new Color(0.5f, 0.41f, 0.28f, 1f), 3f);

            centerLabel = new Label("ANULUJ") { pickingMode = PickingMode.Ignore };
            centerLabel.AddToClassList("stamp-font");
            centerLabel.style.width = 148f;
            centerLabel.style.whiteSpace = WhiteSpace.Normal;
            centerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            centerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            centerLabel.style.fontSize = 16f;
            centerLabel.style.letterSpacing = 1.5f;
            centerLabel.style.color = TextColor;
            center.Add(centerLabel);
            wheel.Add(center);

            UiControlStates.Normalize(overlay);
            overlay.style.display = DisplayStyle.None;
            RefreshHighlight();
        }

        private VisualElement CreateOption(int index)
        {
            var option = new VisualElement { pickingMode = PickingMode.Ignore };
            option.style.position = Position.Absolute;
            option.style.left = OptionPositions[index].x;
            option.style.top = OptionPositions[index].y;
            option.style.width = 200f;
            option.style.height = 92f;
            option.style.alignItems = Align.Center;
            option.style.justifyContent = Justify.Center;
            SetTransition(option, 0.09f, "scale", "opacity");

            var number = new Label($"0{index + 1}") { pickingMode = PickingMode.Ignore };
            number.style.fontSize = 12f;
            number.style.unityFontStyleAndWeight = FontStyle.Bold;
            number.style.letterSpacing = 2f;
            number.style.color = AccentColor;
            number.style.marginBottom = 4f;
            option.Add(number);
            optionNumbers[index] = number;

            var name = new Label(DanceRadialSelection.Names[index])
            {
                pickingMode = PickingMode.Ignore
            };
            name.AddToClassList("stamp-font");
            name.style.fontSize = 20f;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.unityTextAlign = TextAnchor.MiddleCenter;
            name.style.letterSpacing = 1f;
            name.style.color = TextColor;
            option.Add(name);
            return option;
        }

        private void RefreshHighlight()
        {
            for (int index = 0; index < options.Length; index++)
            {
                bool selected = index == SelectedDance;
                options[index].style.scale = selected
                    ? new Scale(new Vector3(1.06f, 1.06f, 1f))
                    : new Scale(Vector3.one);
                options[index].style.opacity = selected ? 1f : 0.72f;
                optionNumbers[index].style.opacity = selected ? 1f : 0.5f;
            }

            wheel.SelectedDance = SelectedDance;
            bool centerSelected = SelectedDance == DanceRadialSelection.NoSelection;
            center.style.backgroundColor = centerSelected
                ? new Color(0.2f, 0.23f, 0.25f, 1f)
                : new Color(0.078f, 0.094f, 0.106f, 1f);
            RefreshCenterLabel(centerSelected);
        }

        private void RefreshCenterLabel(bool nothingSelected)
        {
            if (nothingSelected)
            {
                centerLabel.text = openedWhileDancing ? "PUŚĆ, ABY ZATRZYMAĆ" : "ANULUJ";
                centerLabel.style.color = TextColor;
                centerLabel.style.fontSize = 16f;
                return;
            }

            centerLabel.text = DanceRadialSelection.Names[SelectedDance];
            centerLabel.style.color = AccentColor;
            centerLabel.style.fontSize = 18f;
        }

        private void PlayOpenTransition()
        {
            wheel.style.scale = new Scale(new Vector3(0.94f, 0.94f, 1f));
            overlay.style.opacity = 0f;
            overlay.schedule.Execute(() =>
            {
                wheel.style.scale = new Scale(Vector3.one);
                overlay.style.opacity = 1f;
            }).ExecuteLater(0);
        }

        /// <summary>
        /// Screen pixels per panel unit. The panel scales with screen size, so a dead zone
        /// expressed in panel units has to be converted before it is compared against a raw
        /// pointer offset; otherwise the hit test only matches the artwork at the reference width.
        /// </summary>
        private float ResolvePanelScale()
        {
            if (overlay == null)
                return 1f;

            float panelWidth = overlay.resolvedStyle.width;
            if (float.IsNaN(panelWidth) || panelWidth < 1f)
                return 1f;

            return Screen.width / panelWidth;
        }

        private static void SetTransition(
            VisualElement element,
            float seconds,
            params string[] properties)
        {
            var names = new List<StylePropertyName>(properties.Length);
            var durations = new List<TimeValue>(properties.Length);
            var easings = new List<EasingFunction>(properties.Length);
            foreach (string property in properties)
            {
                names.Add(new StylePropertyName(property));
                durations.Add(new TimeValue(seconds, TimeUnit.Second));
                easings.Add(new EasingFunction(EasingMode.EaseOutCubic));
            }

            element.style.transitionProperty = new StyleList<StylePropertyName>(names);
            element.style.transitionDuration = new StyleList<TimeValue>(durations);
            element.style.transitionTimingFunction = new StyleList<EasingFunction>(easings);
        }

        private static void SetBorder(VisualElement element, Color color, float width)
        {
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
        }

        private static Vector2 ReadPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current == null
                ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
                : Mouse.current.position.ReadValue();
#else
            return Input.mousePosition;
#endif
        }

        private static void WarpPointerToScreenCenter()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse.current?.WarpCursorPosition(
                new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
#endif
        }
    }

    internal sealed class DanceRadialWheelElement : VisualElement
    {
        private const int SectorCount = DanceRadialSelection.DanceCount;
        private const int SegmentCount = 24;
        private const float InnerRadius = 91f;
        private const float OuterPadding = 8f;
        private const float SectorHalfAngle = 43.5f;

        private static readonly Color32[] SectorColors =
        {
            new(39, 45, 49, 255),
            new(32, 38, 42, 255),
            new(39, 45, 49, 255),
            new(32, 38, 42, 255)
        };

        private static readonly Color32 SelectedColor = new(78, 110, 91, 255);

        private int selectedDance = DanceRadialSelection.NoSelection;

        public int SelectedDance
        {
            get => selectedDance;
            set
            {
                if (selectedDance == value)
                    return;

                selectedDance = value;
                MarkDirtyRepaint();
            }
        }

        public DanceRadialWheelElement()
        {
            generateVisualContent += DrawSectors;
        }

        private void DrawSectors(MeshGenerationContext context)
        {
            Rect bounds = contentRect;
            Vector2 center = bounds.center;
            float outerRadius = Mathf.Min(bounds.width, bounds.height) * 0.5f -
                OuterPadding;

            for (int sector = 0; sector < SectorCount; sector++)
            {
                float centerAngle = -90f + sector * 90f;
                DrawSector(
                    context,
                    center,
                    InnerRadius,
                    outerRadius,
                    centerAngle - SectorHalfAngle,
                    centerAngle + SectorHalfAngle,
                    sector == selectedDance ? SelectedColor : SectorColors[sector]);
            }
        }

        private static void DrawSector(
            MeshGenerationContext context,
            Vector2 center,
            float innerRadius,
            float outerRadius,
            float startAngle,
            float endAngle,
            Color32 color)
        {
            const int verticesPerStep = 2;
            MeshWriteData mesh = context.Allocate(
                (SegmentCount + 1) * verticesPerStep,
                SegmentCount * 6);

            for (int step = 0; step <= SegmentCount; step++)
            {
                float angle = Mathf.Lerp(startAngle, endAngle, step / (float)SegmentCount) *
                    Mathf.Deg2Rad;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                mesh.SetNextVertex(CreateVertex(center + direction * outerRadius, color));
                mesh.SetNextVertex(CreateVertex(center + direction * innerRadius, color));
            }

            for (int step = 0; step < SegmentCount; step++)
            {
                ushort outerCurrent = (ushort)(step * verticesPerStep);
                ushort innerCurrent = (ushort)(outerCurrent + 1);
                ushort outerNext = (ushort)(outerCurrent + verticesPerStep);
                ushort innerNext = (ushort)(outerNext + 1);

                mesh.SetNextIndex(outerCurrent);
                mesh.SetNextIndex(outerNext);
                mesh.SetNextIndex(innerCurrent);
                mesh.SetNextIndex(outerNext);
                mesh.SetNextIndex(innerNext);
                mesh.SetNextIndex(innerCurrent);
            }
        }

        private static Vertex CreateVertex(Vector2 position, Color32 color) =>
            new()
            {
                position = new Vector3(position.x, position.y, Vertex.nearZ),
                tint = color
            };
    }
}
