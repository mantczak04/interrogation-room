using InterrogationRoom.Gameplay.Characters;
using InterrogationRoom.Networking;
using InterrogationRoom.UI;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
[RequireComponent(typeof(UIDocument))]
public sealed class LobbyCharacterPresenter : MonoBehaviour
{
    private const int PreviewLayer = 31;

    private NetworkRoundCoordinator coordinator;
    private SteamLobby steamLobby;
    private Label characterNameLabel;
    private Button previousCharacterButton;
    private Button nextCharacterButton;
    private Button inviteButton;
    private Image characterPreviewImage;
    private VisualElement lobbyPanel;
    private PlayerController previewPlayer;
    private CharacterId? selectedCharacter;
    private CharacterId? renderedCharacter;
    private GameObject previewRoot;
    private GameObject previewModel;
    private Camera previewCamera;
    private RenderTexture previewTexture;
    private bool bound;

    public void Configure(NetworkRoundCoordinator roundCoordinator, SteamLobby lobby)
    {
        coordinator = roundCoordinator;
        steamLobby = lobby;
    }

    private void Start()
    {
        BindVisualTree();
    }

    private void Update()
    {
        if (!bound)
            return;

        if (lobbyPanel.resolvedStyle.display == DisplayStyle.None)
        {
            DestroyPreview();
            return;
        }

        PlayerController localPlayer = NetworkClient.localPlayer != null
            ? NetworkClient.localPlayer.GetComponent<PlayerController>()
            : null;
        bool canSelectCharacter = localPlayer != null;
        SetVisible(previousCharacterButton, canSelectCharacter);
        SetVisible(nextCharacterButton, canSelectCharacter);
        SetVisible(characterNameLabel, canSelectCharacter);
        SetVisible(characterPreviewImage, canSelectCharacter);
        SetVisible(inviteButton, coordinator != null && coordinator.IsLocalHost && steamLobby != null && steamLobby.InLobby);

        if (canSelectCharacter)
            RefreshCharacterPreview(localPlayer);
        else
            DestroyPreview();
    }

    private void OnDisable()
    {
        if (bound)
        {
            previousCharacterButton.clicked -= OnPreviousCharacterClicked;
            nextCharacterButton.clicked -= OnNextCharacterClicked;
            inviteButton.clicked -= OnInviteClicked;
        }

        bound = false;
        DestroyPreview();
    }

    private void BindVisualTree()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        lobbyPanel = Required<VisualElement>(root, "lobby-panel");
        characterNameLabel = Required<Label>(root, "character-name-label");
        previousCharacterButton = Required<Button>(root, "previous-character-button");
        nextCharacterButton = Required<Button>(root, "next-character-button");
        inviteButton = Required<Button>(root, "invite-button");
        characterPreviewImage = Required<Image>(root, "character-preview");
        previousCharacterButton.clicked += OnPreviousCharacterClicked;
        nextCharacterButton.clicked += OnNextCharacterClicked;
        inviteButton.clicked += OnInviteClicked;
        bound = true;
    }

    private void OnPreviousCharacterClicked() => SelectCharacter(-1);

    private void OnNextCharacterClicked() => SelectCharacter(1);

    private void OnInviteClicked() => steamLobby?.OpenInviteDialog();

    private void SelectCharacter(int offset)
    {
        PlayerController localPlayer = NetworkClient.localPlayer != null
            ? NetworkClient.localPlayer.GetComponent<PlayerController>()
            : null;
        if (localPlayer == null)
            return;

        CharacterId current = selectedCharacter ?? localPlayer.CharacterId;
        CharacterId selected = CharacterSelectionCarousel.Step(current, offset);
        if (!localPlayer.isLocalPlayer || !localPlayer.HasVisualFor(selected))
            return;

        localPlayer.CmdSelectCharacter(selected);
        selectedCharacter = selected;
        renderedCharacter = null;
        RefreshCharacterPreview(localPlayer);
    }

    private void RefreshCharacterPreview(PlayerController localPlayer)
    {
        if (previewPlayer != localPlayer)
        {
            DestroyPreview();
            previewPlayer = localPlayer;
            selectedCharacter = localPlayer.CharacterId;
        }

        CharacterId selected = selectedCharacter ?? localPlayer.CharacterId;
        characterNameLabel.text = UiText.Get(CharacterSelectionCarousel.DisplayName(selected));
        EnsurePreviewRig();

        if (renderedCharacter == selected && previewModel != null)
            return;

        if (previewModel != null)
            Destroy(previewModel);

        previewModel = localPlayer.CreateCharacterPreview(selected, previewRoot.transform);
        renderedCharacter = selected;
        if (previewModel == null)
        {
            characterPreviewImage.image = null;
            return;
        }

        SetLayerRecursively(previewModel, PreviewLayer);
        FramePreviewModel(previewModel);
        characterPreviewImage.image = previewTexture;
    }

    private void EnsurePreviewRig()
    {
        if (previewRoot != null)
            return;

        previewRoot = new GameObject("Lobby Character Preview Rig");
        previewRoot.transform.position = new Vector3(10000f, 10000f, 10000f);
        previewRoot.hideFlags = HideFlags.DontSave;

        GameObject cameraObject = new GameObject("Preview Camera");
        cameraObject.transform.SetParent(previewRoot.transform, false);
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color32(0x16, 0x1B, 0x1E, 0xFF);
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.fieldOfView = 27f;
        previewCamera.nearClipPlane = 0.05f;
        previewCamera.allowHDR = true;
        previewCamera.allowMSAA = true;

        previewTexture = new RenderTexture(768, 900, 24, RenderTextureFormat.ARGB32)
        {
            name = "Lobby Character Preview",
            antiAliasing = 4,
            filterMode = FilterMode.Bilinear
        };
        previewTexture.Create();
        previewCamera.targetTexture = previewTexture;

        CreatePreviewLight("Preview Key Light", new Color(1f, 0.9f, 0.76f), 1.75f, new Vector3(32f, -32f, 0f));
        CreatePreviewLight("Preview Fill Light", new Color(0.62f, 0.76f, 1f), 1.05f, new Vector3(18f, 138f, 0f));
        CreatePreviewLight("Preview Rim Light", new Color(1f, 0.72f, 0.42f), 1.35f, new Vector3(12f, 205f, 0f));
    }

    private void CreatePreviewLight(string lightName, Color color, float intensity, Vector3 rotation)
    {
        GameObject lightObject = new GameObject(lightName);
        lightObject.transform.SetParent(previewRoot.transform, false);
        lightObject.transform.localRotation = Quaternion.Euler(rotation);
        Light previewLight = lightObject.AddComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.intensity = intensity;
        previewLight.color = color;
        previewLight.cullingMask = 1 << PreviewLayer;
    }

    private void FramePreviewModel(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);

        float height = Mathf.Max(1f, bounds.size.y);
        Vector3 focus = bounds.center + Vector3.up * height * 0.04f;
        float distance = Mathf.Max(3.2f, height * 2.35f);
        previewCamera.transform.position = focus + new Vector3(0f, height * 0.02f, distance);
        previewCamera.transform.LookAt(focus);
    }

    private void DestroyPreview()
    {
        if (previewRoot != null)
            Destroy(previewRoot);
        if (previewTexture != null)
        {
            previewTexture.Release();
            Destroy(previewTexture);
        }

        previewRoot = null;
        previewModel = null;
        previewCamera = null;
        previewTexture = null;
        previewPlayer = null;
        selectedCharacter = null;
        renderedCharacter = null;
        if (characterPreviewImage != null)
            characterPreviewImage.image = null;
    }

    private static T Required<T>(VisualElement root, string name) where T : VisualElement
    {
        T element = root.Q<T>(name);
        if (element == null)
            throw new MissingReferenceException($"Lobby UI requires '{name}'.");
        return element;
    }

    private static void SetVisible(VisualElement element, bool visible)
    {
        if (element != null)
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
