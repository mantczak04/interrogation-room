using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuSetup
{
    [MenuItem("Tools/Setup Main Menu Scene")]
    public static void SetupScene()
    {
        string scenePath = "Assets/Scenes/MainMenu.unity";
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // Add Main Camera
        GameObject cameraObj = new GameObject("Main Camera");
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cameraObj.tag = "MainCamera";

        // Add EventSystem
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        // Using reflection to add InputSystemUIInputModule if available, else fallback to Standalone
        var inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null) {
            eventSystemObj.AddComponent(inputSystemModuleType);
        } else {
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }

        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Add MainMenuManager
        MainMenuManager manager = canvasObj.AddComponent<MainMenuManager>();

        // Background Image Setup (Fix Texture Type to Sprite)
        string texPath = "Assets/UI/Sprites/MainMenuBackground.jpg";
        TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }

        // Background Image
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.rectTransform.anchorMin = Vector2.zero;
        bgImage.rectTransform.anchorMax = Vector2.one;
        bgImage.rectTransform.sizeDelta = Vector2.zero;
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 1f); // Fallback color
        
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
        if (bgSprite != null)
        {
            bgImage.sprite = bgSprite;
            bgImage.color = Color.white;
        }
        else
        {
            Debug.LogWarning("Background sprite not found at " + texPath);
        }

        // Menu Container
        GameObject containerObj = new GameObject("MenuContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0.5f);
        containerRect.anchorMax = new Vector2(0, 0.5f);
        containerRect.pivot = new Vector2(0, 0.5f);
        containerRect.anchoredPosition = new Vector2(100, 0); // 100px from left
        containerRect.sizeDelta = new Vector2(500, 600);
        
        VerticalLayoutGroup vlg = containerObj.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.spacing = 30;
        vlg.childControlHeight = false; // Fix squishing
        vlg.childControlWidth = false;  // Fix text wrapping
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;

        // Create Buttons (Pass method directly, NOT lambda, to avoid serialization NullReferenceException)
        CreateButton(containerObj.transform, "Host Game", manager.HostGame);
        CreateButton(containerObj.transform, "Join Server", manager.JoinServer);
        CreateButton(containerObj.transform, "Settings", manager.OpenSettings);
        CreateButton(containerObj.transform, "Quit", manager.QuitGame);

        // Save Scene
        EditorSceneManager.SaveScene(newScene, scenePath);
        
        // Add to Build Settings at index 0
        EditorBuildSettingsScene[] original = EditorBuildSettings.scenes;
        bool exists = false;
        foreach (var s in original) {
            if (s.path == scenePath) { exists = true; break; }
        }
        if (!exists) {
            var newScenes = new EditorBuildSettingsScene[original.Length + 1];
            newScenes[0] = new EditorBuildSettingsScene(scenePath, true);
            for (int i = 0; i < original.Length; i++) {
                newScenes[i + 1] = original[i];
            }
            EditorBuildSettings.scenes = newScenes;
        }

        Debug.Log("MainMenu scene setup complete and saved to " + scenePath);
    }

    private static void CreateButton(Transform parent, string title, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject("Button_" + title);
        btnObj.transform.SetParent(parent, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(400, 60); // Fixed size for VLG

        Button button = btnObj.AddComponent<Button>();
        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0,0,0,0); // Transparent

        // Text Obj
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = title;
        tmp.fontSize = 42;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        tmp.enableWordWrapping = false; // Crucial fix for the wrapping issue!
        
        RectTransform txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        txtRect.anchoredPosition = new Vector2(20, 0); // Offset text slightly right from the dot

        // Dot Obj
        GameObject dotObj = new GameObject("Dot");
        dotObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI dotTmp = dotObj.AddComponent<TextMeshProUGUI>();
        dotTmp.text = "•";
        dotTmp.fontSize = 42;
        dotTmp.alignment = TextAlignmentOptions.Left;
        dotTmp.color = Color.white;
        dotTmp.enableWordWrapping = false;
        
        RectTransform dotRect = dotObj.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(0, 0.5f);
        dotRect.anchorMax = new Vector2(0, 0.5f);
        dotRect.sizeDelta = new Vector2(30, 60);
        dotRect.anchoredPosition = new Vector2(-10, 0); // Offset to the left

        // Hover script
        MenuButtonHover hover = btnObj.AddComponent<MenuButtonHover>();
        var hoverSer = new SerializedObject(hover);
        hoverSer.FindProperty("buttonText").objectReferenceValue = tmp;
        hoverSer.FindProperty("indicatorText").objectReferenceValue = dotTmp;
        hoverSer.ApplyModifiedProperties();

        // Hook up event
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, action);
        
        // Navigation (none)
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
    }
}
