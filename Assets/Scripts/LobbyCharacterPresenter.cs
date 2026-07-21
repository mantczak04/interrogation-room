using System.Collections.Generic;
using System.Text;
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
    private ScrollView playerList;
    private Label playerListEmptyLabel;
    private Label rosterCountLabel;
    private VisualElement lobbyPanel;
    private VivoxVoiceRuntime voiceRuntime;
    private PlayerController previewPlayer;
    private CharacterId? selectedCharacter;
    private CharacterId? renderedCharacter;
    private GameObject previewRoot;
    private GameObject previewModel;
    private Camera previewCamera;
    private RenderTexture previewTexture;
    private bool bound;
    private float nextRosterRefresh;
    private string renderedRosterSignature;
    private uint renderedLocalNetId;
    private readonly Dictionary<int, VisualElement> speakerIndicators = new();
    private readonly Dictionary<int, uint> speakerNetIds = new();

    public void Configure(NetworkRoundCoordinator roundCoordinator, SteamLobby lobby)
    {
        coordinator = roundCoordinator;
        steamLobby = lobby;
        coordinator?.SetLocalLobbyDisplayName(LobbyDisplayNameProvider.Resolve("Gracz lokalny"));
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

        if (Time.unscaledTime >= nextRosterRefresh)
        {
            RefreshPlayerRoster();
            nextRosterRefresh = Time.unscaledTime + 0.1f;
        }
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
        speakerIndicators.Clear();
        speakerNetIds.Clear();
        renderedRosterSignature = null;
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
        playerList = Required<ScrollView>(root, "lobby-player-list");
        playerListEmptyLabel = Required<Label>(root, "lobby-player-list-empty");
        rosterCountLabel = Required<Label>(root, "lobby-roster-count");
        previousCharacterButton.clicked += OnPreviousCharacterClicked;
        nextCharacterButton.clicked += OnNextCharacterClicked;
        inviteButton.clicked += OnInviteClicked;
        bound = true;
        RefreshPlayerRoster(force: true);
    }

    private void RefreshPlayerRoster(bool force = false)
    {
        if (coordinator == null || playerList == null)
            return;

        IReadOnlyList<LobbyPlayerInfo> players = coordinator.PublicLobbyPlayers;
        string signature = BuildRosterSignature(players);
        uint localNetId = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId : 0u;
        if (force || signature != renderedRosterSignature || localNetId != renderedLocalNetId)
        {
            renderedRosterSignature = signature;
            renderedLocalNetId = localNetId;
            RebuildPlayerRoster(players);
        }

        if (voiceRuntime == null)
            voiceRuntime = FindFirstObjectByType<VivoxVoiceRuntime>();

        foreach (KeyValuePair<int, VisualElement> entry in speakerIndicators)
        {
            bool speaking = speakerNetIds.TryGetValue(entry.Key, out uint netId) &&
                voiceRuntime != null &&
                voiceRuntime.IsNetworkPlayerSpeaking(netId);
            SetVisible(entry.Value, speaking);
        }
    }

    private void RebuildPlayerRoster(IReadOnlyList<LobbyPlayerInfo> players)
    {
        playerList.Clear();
        speakerIndicators.Clear();
        speakerNetIds.Clear();
        SetVisible(playerListEmptyLabel, players == null || players.Count == 0);
        rosterCountLabel.text = $"{UiText.Get("Gracze w lobby")}: {players?.Count ?? 0}/8";
        if (players == null)
            return;

        uint localNetId = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId : 0u;
        for (int index = 0; index < players.Count; index++)
        {
            LobbyPlayerInfo player = players[index];
            var row = new VisualElement();
            row.AddToClassList("lobby-player-row");
            row.EnableInClassList("lobby-player-row--local", localNetId != 0 && player.NetworkIdentityNetId == localNetId);
            row.EnableInClassList("lobby-player-row--simulated", player.IsSimulated);

            var number = new Label((index + 1).ToString("00"));
            number.AddToClassList("lobby-player-number");
            row.Add(number);

            var identity = new VisualElement();
            identity.AddToClassList("lobby-player-identity");
            var name = new Label(player.DisplayName);
            name.AddToClassList("lobby-player-name");
            identity.Add(name);

            var hostLabel = new Label(player.IsHost ? $"({UiText.Get("Host")})" : string.Empty);
            hostLabel.AddToClassList("lobby-player-host");
            identity.Add(hostLabel);

            var readyLabel = new Label(player.IsReady ? UiText.Get("GOTOWY") : string.Empty);
            readyLabel.AddToClassList("lobby-player-ready");
            identity.Add(readyLabel);
            row.Add(identity);

            VisualElement speaker = CreateSpeakerIndicator();
            row.Add(speaker);
            speakerIndicators[player.PlayerId] = speaker;
            speakerNetIds[player.PlayerId] = player.NetworkIdentityNetId;
            SetVisible(speaker, false);
            playerList.Add(row);
        }
    }

    private static VisualElement CreateSpeakerIndicator()
    {
        var indicator = new VisualElement();
        indicator.AddToClassList("lobby-speaker-indicator");
        return indicator;
    }

    private static string BuildRosterSignature(IReadOnlyList<LobbyPlayerInfo> players)
    {
        if (players == null || players.Count == 0)
            return string.Empty;

        var signature = new StringBuilder(players.Count * 32);
        foreach (LobbyPlayerInfo player in players)
        {
            signature.Append(player.PlayerId).Append('|')
                .Append(player.NetworkIdentityNetId).Append('|')
                .Append(player.DisplayName).Append('|')
                .Append(player.IsHost).Append('|')
                .Append(player.IsSimulated).Append('|')
                .Append(player.IsReady).Append(';');
        }
        return signature.ToString();
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
        // Transparent, so the character stands on the dossier itself rather than
        // inside a grey rectangle whose edges do not line up with anything.
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
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
