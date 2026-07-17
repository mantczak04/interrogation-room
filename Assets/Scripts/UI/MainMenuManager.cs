using InterrogationRoom.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MainMenuManager : MonoBehaviour
{
    private static readonly Color Graphite = new Color32(0x14, 0x18, 0x1B, 0xF4);
    private static readonly Color GraphiteRaised = new Color32(0x2F, 0x35, 0x3A, 0xFF);
    private static readonly Color Paper = new Color32(0xE8, 0xDC, 0xC5, 0xFF);
    private static readonly Color MutedPaper = new Color32(0xA6, 0xAB, 0xAA, 0xFF);
    private static readonly Color Amber = new Color32(0xE0, 0xB5, 0x68, 0xFF);
    private static readonly Color Brass = new Color32(0x80, 0x69, 0x48, 0xFF);
    private static readonly Color Green = new Color32(0x41, 0x5B, 0x4C, 0xFF);

    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Room"; // Zmienić w razie innej nazwy sceny

    private bool loadingGameScene;

    private void Start()
    {
        PlayerInputGate.SetUiInputBlocked(true);
        BuildPresentation();
        TryOpenPendingSteamLobby();
    }

    private void Update()
    {
        TryOpenPendingSteamLobby();

        if (WasEscapePressed() && !SettingsMenu.IsOpen && !SettingsMenu.EscapeConsumedThisFrame)
        {
            OpenSettings();
        }
    }

    private static bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private void TryOpenPendingSteamLobby()
    {
        if (loadingGameScene || !GameLaunchRequest.HasPendingSteamLobbyJoin)
            return;

        loadingGameScene = true;
        SceneManager.LoadScene(gameSceneName);
    }

    public void HostGame()
    {
        loadingGameScene = true;
        GameLaunchRequest.Set(GameLaunchMode.Host);
        SceneManager.LoadScene(gameSceneName);
    }

    public void JoinServer()
    {
        loadingGameScene = true;
        GameLaunchRequest.Set(GameLaunchMode.Join);
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        SettingsMenu.EnsureInstance().Open();
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game clicked! Quitting...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BuildPresentation()
    {
        Canvas canvas = GetComponent<Canvas>();
        RectTransform menu = transform.Find("MenuContainer") as RectTransform;
        if (canvas == null || menu == null)
        {
            Debug.LogWarning("[MainMenuManager] Expected Canvas/MenuContainer hierarchy is missing.", this);
            return;
        }

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        RectTransform dossier = EnsureDossierBackdrop(menu);
        dossier.SetSiblingIndex(Mathf.Max(1, menu.GetSiblingIndex() - 1));

        menu.anchorMin = menu.anchorMax = new Vector2(0f, 0.5f);
        menu.pivot = new Vector2(0f, 0.5f);
        menu.anchoredPosition = new Vector2(142f, -112f);
        menu.sizeDelta = new Vector2(476f, 360f);

        VerticalLayoutGroup layout = menu.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        string[] labels = { "UTWÓRZ LOBBY", "DOŁĄCZ DO GRY", "USTAWIENIA", "WYJDŹ" };
        Button[] buttons = menu.GetComponentsInChildren<Button>(true);
        for (int index = 0; index < buttons.Length && index < labels.Length; index++)
        {
            StyleButton(buttons[index], labels[index], index == 0);
        }
    }

    private RectTransform EnsureDossierBackdrop(RectTransform menu)
    {
        Transform existing = transform.Find("MenuDossier");
        if (existing != null)
        {
            return (RectTransform)existing;
        }

        var dossierObject = new GameObject("MenuDossier", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform dossier = dossierObject.GetComponent<RectTransform>();
        dossier.SetParent(transform, false);
        dossier.anchorMin = dossier.anchorMax = new Vector2(0f, 0.5f);
        dossier.pivot = new Vector2(0f, 0.5f);
        dossier.anchoredPosition = new Vector2(72f, 0f);
        dossier.sizeDelta = new Vector2(616f, 830f);

        Image image = dossierObject.GetComponent<Image>();
        image.color = Graphite;
        image.raycastTarget = false;
        Outline outline = dossierObject.AddComponent<Outline>();
        outline.effectColor = Brass;
        outline.effectDistance = new Vector2(2f, -2f);

        CreateText(dossier, "Kicker", "POSTERUNEK • AKTA 01/26", 17f, Amber, FontStyles.Bold,
            new Vector2(64f, -58f), new Vector2(490f, 32f));
        CreateText(dossier, "Title", "PRZESŁUCHANIE", 48f, Paper, FontStyles.Bold,
            new Vector2(64f, -98f), new Vector2(500f, 72f));
        CreateText(dossier, "Subtitle", "Wybierz sposób wejścia do lobby.", 19f, MutedPaper, FontStyles.Normal,
            new Vector2(64f, -176f), new Vector2(490f, 34f));

        Image rule = CreateImage(dossier, "Rule", Brass);
        RectTransform ruleRect = rule.rectTransform;
        ruleRect.anchorMin = ruleRect.anchorMax = new Vector2(0f, 1f);
        ruleRect.pivot = new Vector2(0f, 1f);
        ruleRect.anchoredPosition = new Vector2(64f, -226f);
        ruleRect.sizeDelta = new Vector2(488f, 2f);

        CreateText(dossier, "Footer", "Każde zeznanie zostawia ślad.", 15f, MutedPaper, FontStyles.Italic,
            new Vector2(64f, -762f), new Vector2(490f, 30f));
        return dossier;
    }

    private static void StyleButton(Button button, string label, bool primary)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 64f);
        LayoutElement element = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 64f;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = primary ? Green : GraphiteRaised;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.16f, 1.16f, 1.16f, 1f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text mainLabel = null;
        foreach (TMP_Text text in texts)
        {
            if (mainLabel == null || text.fontSize > mainLabel.fontSize)
            {
                mainLabel = text;
            }
        }

        if (mainLabel != null)
        {
            mainLabel.text = label;
            mainLabel.fontSize = 24f;
            mainLabel.fontStyle = FontStyles.Bold;
            mainLabel.alignment = TextAlignmentOptions.MidlineLeft;
            mainLabel.margin = new Vector4(24f, 0f, 20f, 0f);
            mainLabel.color = Paper;
            mainLabel.characterSpacing = 2f;
        }

        MenuButtonHover hover = button.GetComponent<MenuButtonHover>();
        if (hover != null)
        {
            hover.ConfigurePresentation(Paper, Color.white);
        }
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string value,
        float size,
        Color color,
        FontStyles style,
        Vector2 position,
        Vector2 dimensions)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        return text;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
