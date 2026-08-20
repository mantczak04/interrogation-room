using System.Collections.Generic;
using System.Text;
using InterrogationRoom.Gameplay;
using InterrogationRoom.Gameplay.Characters;
using InterrogationRoom.Networking;
using InterrogationRoom.Settings;
using InterrogationRoom.Steam;
using InterrogationRoom.Voice;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

namespace InterrogationRoom.UI
{
[DisallowMultipleComponent]
[RequireComponent(typeof(UIDocument))]
public sealed class LobbyCharacterPresenter : MonoBehaviour
{
    private const int PreviewLayer = 31;
    private const float FixedLineupCameraDistance = 6.6f;
    private const float SplitLineupSpacing = 1.08f;
    private const float LineupVisualCenterOffsetX = 0.18f;
    private const float SplitLineupFocusHeightOffset = 0.04f;
    private const float SingleRowFocusHeightOffset = 0.17f;
    private const float LineupLabelFloorOffset = 0.045f;

    private NetworkRoundCoordinator coordinator;
    private SteamLobby steamLobby;
    private Label characterNameLabel;
    private Button previousCharacterButton;
    private Button nextCharacterButton;
    private Button inviteButton;
    private VisualElement inviteFriendPanel;
    private ScrollView inviteFriendList;
    private Label inviteFriendEmptyLabel;
    private Label inviteFriendStatusLabel;
    private Image lineupPreviewImage;
    private VisualElement playerLineup;
    private Label playerListEmptyLabel;
    private Label rosterCountLabel;
    private VisualElement lobbyPanel;
    private VivoxVoiceRuntime voiceRuntime;
    private PlayerController previewPlayer;
    private CharacterId? selectedCharacter;
    private GameObject previewRoot;
    private Camera previewCamera;
    private RenderTexture previewTexture;
    private bool bound;
    private bool inviteFriendPanelOpen;
    private float nextRosterRefresh;
    private float nextInviteFriendRefresh;
    private string renderedRosterSignature;
    private string renderedInviteFriendSignature;
    private uint renderedLocalNetId;
    private readonly Dictionary<int, VisualElement> speakerIndicators = new();
    private readonly Dictionary<int, uint> speakerNetIds = new();
    private readonly Dictionary<int, Label> microphoneStateLabels = new();
    private readonly List<LineupLabelAnchor> lineupLabelAnchors = new();

    private sealed class LineupLabelAnchor
    {
        public LineupLabelAnchor(VisualElement label, Vector3 worldPosition)
        {
            Label = label;
            WorldPosition = worldPosition;
        }

        public VisualElement Label { get; }
        public Vector3 WorldPosition { get; }
    }

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
            ResetPreviewState();
            return;
        }

        PlayerController localPlayer = NetworkClient.localPlayer != null
            ? NetworkClient.localPlayer.GetComponent<PlayerController>()
            : null;
        bool canSelectCharacter = localPlayer != null;
        SetVisible(previousCharacterButton, canSelectCharacter);
        SetVisible(nextCharacterButton, canSelectCharacter);
        SetVisible(characterNameLabel, canSelectCharacter);
        bool canInviteFriends = coordinator != null &&
            coordinator.IsLocalHost &&
            steamLobby != null &&
            steamLobby.InLobby;
        SetVisible(inviteButton, canInviteFriends);
        SetVisible(
            inviteFriendPanel,
            canInviteFriends && inviteFriendPanelOpen && !steamLobby.OverlayEnabled);

        if (!canInviteFriends || steamLobby.OverlayEnabled)
            inviteFriendPanelOpen = false;

        if (previewPlayer != localPlayer)
        {
            DestroyPreviewRig();
            previewPlayer = localPlayer;
            selectedCharacter = localPlayer != null ? localPlayer.CharacterId : null;
            renderedRosterSignature = null;
        }

        if (canSelectCharacter)
        {
            CharacterId selected = selectedCharacter ?? localPlayer.CharacterId;
            characterNameLabel.text = UiText.Get(CharacterSelectionCarousel.DisplayName(selected));
        }

        if (Time.unscaledTime >= nextRosterRefresh)
        {
            RefreshPlayerRoster();
            nextRosterRefresh = Time.unscaledTime + 0.1f;
        }

        if (inviteFriendPanelOpen && Time.unscaledTime >= nextInviteFriendRefresh)
        {
            RefreshInviteFriends();
            nextInviteFriendRefresh = Time.unscaledTime + 1f;
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
        microphoneStateLabels.Clear();
        renderedRosterSignature = null;
        renderedInviteFriendSignature = null;
        inviteFriendPanelOpen = false;
        ResetPreviewState();
    }

    private void BindVisualTree()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        UiControlStates.Normalize(root);
        lobbyPanel = Required<VisualElement>(root, "lobby-panel");
        characterNameLabel = Required<Label>(root, "character-name-label");
        previousCharacterButton = Required<Button>(root, "previous-character-button");
        nextCharacterButton = Required<Button>(root, "next-character-button");
        inviteButton = Required<Button>(root, "invite-button");
        inviteFriendPanel = Required<VisualElement>(root, "invite-friend-panel");
        inviteFriendList = Required<ScrollView>(root, "invite-friend-list");
        inviteFriendEmptyLabel = Required<Label>(root, "invite-friend-empty");
        inviteFriendStatusLabel = Required<Label>(root, "invite-friend-status");
        lineupPreviewImage = Required<Image>(root, "lobby-lineup-preview");
        playerLineup = Required<VisualElement>(root, "lobby-player-lineup");
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
        if (coordinator == null || playerLineup == null)
            return;

        IReadOnlyList<LobbyPlayerInfo> players = coordinator.PublicLobbyPlayers;
        uint localNetId = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId : 0u;
        string signature = $"{GameSettingsService.Current.Language}|{BuildRosterSignature(players, localNetId)}";
        if (force || signature != renderedRosterSignature || localNetId != renderedLocalNetId)
        {
            renderedRosterSignature = signature;
            renderedLocalNetId = localNetId;
            RebuildPlayerLineup(players, localNetId);
        }

        if (voiceRuntime == null)
            voiceRuntime = FindFirstObjectByType<VivoxVoiceRuntime>();

        foreach (KeyValuePair<int, VisualElement> entry in speakerIndicators)
        {
            bool hasNetworkPlayer = speakerNetIds.TryGetValue(entry.Key, out uint netId) && netId != 0u;
            bool speaking = hasNetworkPlayer &&
                voiceRuntime != null &&
                voiceRuntime.IsNetworkPlayerSpeaking(netId);
            SetVisible(entry.Value, speaking);

            if (!microphoneStateLabels.TryGetValue(entry.Key, out Label stateLabel))
                continue;

            bool locallyMuted = hasNetworkPlayer &&
                voiceRuntime != null &&
                voiceRuntime.IsParticipantLocallyMuted(netId);
            bool microphoneMuted = hasNetworkPlayer &&
                voiceRuntime != null &&
                voiceRuntime.IsNetworkPlayerMicrophoneMuted(netId);
            stateLabel.text = locallyMuted
                ? UiText.Get("WYCISZONY LOKALNIE")
                : microphoneMuted
                    ? UiText.Get("MIKROFON WYCISZONY")
                    : string.Empty;
            SetVisible(stateLabel, !string.IsNullOrEmpty(stateLabel.text));
        }
    }

    private void RebuildPlayerLineup(IReadOnlyList<LobbyPlayerInfo> players, uint localNetId)
    {
        DestroyPreviewRig();
        playerLineup.Clear();
        speakerIndicators.Clear();
        speakerNetIds.Clear();
        microphoneStateLabels.Clear();
        SetVisible(playerListEmptyLabel, players == null || players.Count == 0);
        // The roster header is the single player counter in the lobby (the
        // settings panel used to repeat it). Developer fake players show the
        // preview variant so the real count stays readable.
        int presentedCount = players?.Count ?? 0;
        int realCount = coordinator.PublicLobbyPlayerCount;
        rosterCountLabel.text = presentedCount == realCount
            ? $"{UiText.Get("Gracze w lobby")}: {realCount}/8"
            : $"{UiText.Get("Podgląd lobby")}: {presentedCount}/8 • {UiText.Get("prawdziwi")}: {realCount}";
        if (players == null)
            return;

        int localIndex = -1;
        for (int index = 0; index < players.Count; index++)
        {
            if (localNetId != 0u && players[index].NetworkIdentityNetId == localNetId)
            {
                localIndex = index;
                break;
            }
        }

        LobbyLineupPlan layout = LobbyLineupLayout.Create(players.Count, localIndex);
        playerLineup.EnableInClassList("lobby-player-lineup--dense", players.Count >= 6);
        EnsurePreviewRig();
        if (previewRoot == null || previewCamera == null)
            return;

        var models = new List<GameObject>(players.Count);
        bool splitLineup = layout.BackRow.Count > 0;
        if (splitLineup)
            AddLineupModels(players, layout.BackRow, localNetId, true, layout.FrontRow.Count, players.Count, models);
        AddLineupModels(players, layout.FrontRow, localNetId, false, layout.BackRow.Count, players.Count, models);
        if (!TryGetCombinedRendererBounds(models, out Bounds lineupBounds))
            return;

        FrameLineup(lineupBounds, previewCamera, splitLineup);
        PositionLineupLabels();
    }

    private void AddLineupModels(
        IReadOnlyList<LobbyPlayerInfo> players,
        IReadOnlyList<int> playerIndexes,
        uint localNetId,
        bool backRow,
        int oppositeRowCount,
        int totalSlotCount,
        List<GameObject> models)
    {
        bool splitLineup = totalSlotCount >= 6;
        // The camera no longer zooms out as players join, so the horizontal
        // footprint must stay bounded. Staggered rows deliberately overlap
        // silhouettes a little while keeping faces on separate screen axes.
        float spacing = splitLineup
            ? SplitLineupSpacing
            : ResolveSingleRowSpacing(playerIndexes.Count);
        float rowCenter = (playerIndexes.Count - 1) * 0.5f;
        float horizontalOffset = splitLineup && playerIndexes.Count == oppositeRowCount
            ? spacing * (backRow ? -0.25f : 0.25f)
            : 0f;
        for (int rowIndex = 0; rowIndex < playerIndexes.Count; rowIndex++)
        {
            int playerIndex = playerIndexes[rowIndex];
            float depth = splitLineup ? (backRow ? -0.72f : 0.72f) : 0f;
            float elevation = splitLineup && backRow ? 0.38f : 0f;
            Vector3 position = new Vector3(
                (rowIndex - rowCenter) * spacing + horizontalOffset,
                elevation,
                depth);
            AddPlayerModel(players[playerIndex], playerIndex, localNetId, backRow, position, models);
        }
    }

    private static float ResolveSingleRowSpacing(int playerCount)
    {
        if (playerCount <= 1)
            return 0f;

        return Mathf.Min(1.55f, 4.25f / (playerCount - 1));
    }

    private void AddPlayerModel(
        LobbyPlayerInfo player,
        int rosterIndex,
        uint localNetId,
        bool backRow,
        Vector3 lineupPosition,
        List<GameObject> models)
    {
        bool isLocal = localNetId != 0u && player.NetworkIdentityNetId == localNetId;
        CharacterId character = ResolveCharacter(player, rosterIndex, localNetId);
        GameObject model = previewPlayer.CreateCharacterPreview(character, previewRoot.transform);
        if (model == null)
            return;

        model.transform.localPosition += lineupPosition;
        SetLayerRecursively(model, PreviewLayer);
        models.Add(model);

        var playerTag = new VisualElement();
        playerTag.AddToClassList("lobby-player-tag");
        playerTag.EnableInClassList("lobby-player-tag--local", isLocal);
        playerTag.EnableInClassList("lobby-player-tag--simulated", player.IsSimulated);
        playerTag.EnableInClassList("lobby-player-tag--back", backRow);
        var identityLine = new VisualElement();
        identityLine.AddToClassList("lobby-player-identity-line");
        if (player.IsHost)
        {
            var crown = new Label("♛") { tooltip = UiText.Get("Host") };
            crown.AddToClassList("lobby-player-crown");
            identityLine.Add(crown);
        }

        var name = new Label(player.DisplayName);
        name.AddToClassList("lobby-player-name");
        identityLine.Add(name);

        VisualElement speaker = CreateSpeakerIndicator();
        identityLine.Add(speaker);
        playerTag.Add(identityLine);

        var readyLabel = new Label(player.IsReady ? UiText.Get("GOTOWY") : string.Empty);
        readyLabel.AddToClassList("lobby-player-ready");
        playerTag.Add(readyLabel);

        var microphoneState = new Label();
        microphoneState.AddToClassList("lobby-player-voice-state");
        playerTag.Add(microphoneState);
        playerLineup.Add(playerTag);

        speakerIndicators[player.PlayerId] = speaker;
        speakerNetIds[player.PlayerId] = player.NetworkIdentityNetId;
        microphoneStateLabels[player.PlayerId] = microphoneState;
        SetVisible(speaker, false);
        SetVisible(microphoneState, false);
        UiControlStates.Normalize(playerTag);

        if (TryGetRendererBounds(model, out Bounds modelBounds))
        {
            // Labels follow the authored row floor, not each mesh's lowest
            // renderer point. Shoes, coats and idle poses otherwise put names
            // from the same row on visibly different heights.
            float rowFloorY = previewRoot.transform.TransformPoint(
                new Vector3(0f, lineupPosition.y, 0f)).y;
            Vector3 anchor = new Vector3(
                modelBounds.center.x,
                rowFloorY - LineupLabelFloorOffset,
                modelBounds.center.z);
            lineupLabelAnchors.Add(new LineupLabelAnchor(playerTag, anchor));
        }
    }

    private static VisualElement CreateSpeakerIndicator()
    {
        var indicator = new VisualElement();
        indicator.AddToClassList("lobby-speaker-indicator");
        return indicator;
    }

    private string BuildRosterSignature(IReadOnlyList<LobbyPlayerInfo> players, uint localNetId)
    {
        if (players == null || players.Count == 0)
            return string.Empty;

        var signature = new StringBuilder(players.Count * 32);
        for (int index = 0; index < players.Count; index++)
        {
            LobbyPlayerInfo player = players[index];
            signature.Append(player.PlayerId).Append('|')
                .Append(player.NetworkIdentityNetId).Append('|')
                .Append(player.DisplayName).Append('|')
                .Append(player.IsHost).Append('|')
                .Append(player.IsSimulated).Append('|')
                .Append(player.IsReady).Append('|')
                .Append((byte)ResolveCharacter(player, index, localNetId)).Append(';');
        }
        return signature.ToString();
    }

    private CharacterId ResolveCharacter(LobbyPlayerInfo player, int rosterIndex, uint localNetId)
    {
        if (localNetId != 0u && player.NetworkIdentityNetId == localNetId && selectedCharacter.HasValue)
            return selectedCharacter.Value;

        if (player.NetworkIdentityNetId != 0u &&
            NetworkClient.spawned.TryGetValue(player.NetworkIdentityNetId, out NetworkIdentity identity))
        {
            PlayerController networkPlayer = identity.GetComponent<PlayerController>();
            if (networkPlayer != null)
                return networkPlayer.CharacterId;
        }

        IReadOnlyList<CharacterId> characters = CharacterAssignmentRoster.DefaultCharacters;
        int stableIndex = player.IsSimulated
            ? (int)((uint)player.PlayerId % (uint)characters.Count)
            : rosterIndex % characters.Count;
        return characters[stableIndex];
    }

    private void OnPreviousCharacterClicked() => SelectCharacter(-1);

    private void OnNextCharacterClicked() => SelectCharacter(1);

    private void OnInviteClicked()
    {
        if (steamLobby == null || !steamLobby.InLobby)
            return;

        if (steamLobby.OverlayEnabled)
        {
            inviteFriendPanelOpen = false;
            SetVisible(inviteFriendPanel, false);
            steamLobby.OpenInviteDialog();
            return;
        }

        inviteFriendPanelOpen = !inviteFriendPanelOpen;
        inviteFriendStatusLabel.text = string.Empty;
        SetVisible(inviteFriendStatusLabel, false);
        SetVisible(inviteFriendPanel, inviteFriendPanelOpen);
        if (inviteFriendPanelOpen)
            RefreshInviteFriends(force: true);
    }

    private void RefreshInviteFriends(bool force = false)
    {
        if (steamLobby == null || !steamLobby.InLobby)
            return;

        int friendCount = steamLobby.DirectInviteFriendCount;
        var signature = new StringBuilder(friendCount * 24 + 16);
        signature.Append(GameSettingsService.Current.Language).Append('|').Append(friendCount);
        for (int index = 0; index < friendCount; index++)
            signature.Append('|').Append(steamLobby.GetDirectInviteFriendName(index));

        string currentSignature = signature.ToString();
        if (!force && currentSignature == renderedInviteFriendSignature)
            return;

        renderedInviteFriendSignature = currentSignature;
        inviteFriendList.Clear();
        SetVisible(inviteFriendEmptyLabel, friendCount == 0);

        for (int index = 0; index < friendCount; index++)
        {
            int friendIndex = index;
            string friendName = steamLobby.GetDirectInviteFriendName(friendIndex);
            var friendButton = new Button(() => InviteDirectFriend(friendIndex, friendName))
            {
                text = friendName
            };
            friendButton.AddToClassList("lobby-invite-friend");
            inviteFriendList.Add(friendButton);
        }
    }

    private void InviteDirectFriend(int friendIndex, string friendName)
    {
        bool sent = steamLobby != null && steamLobby.InviteDirectFriend(friendIndex);
        inviteFriendStatusLabel.text = sent
            ? UiText.Format("Zaproszenie wysłane do: {0}.", friendName)
            : UiText.Get("Nie udało się wysłać zaproszenia.");
        inviteFriendStatusLabel.EnableInClassList("lobby-invite-status--error", !sent);
        SetVisible(inviteFriendStatusLabel, true);
    }

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
        characterNameLabel.text = UiText.Get(CharacterSelectionCarousel.DisplayName(selected));
        renderedRosterSignature = null;
        RefreshPlayerRoster(force: true);
    }

    private void EnsurePreviewRig()
    {
        if (previewRoot != null || previewPlayer == null)
            return;

        previewRoot = new GameObject("Lobby Player Lineup Preview Rig");
        // Keep the isolated rig below the scene without sacrificing float
        // precision. At 10,000 units, small idle-animation motion visibly
        // quantized in the close-up preview.
        previewRoot.transform.position = new Vector3(0f, -100f, 0f);
        previewRoot.hideFlags = HideFlags.DontSave;

        CreatePreviewLight("Preview Key Light", new Color(1f, 0.9f, 0.76f), 1.75f, new Vector3(32f, -32f, 0f));
        CreatePreviewLight("Preview Fill Light", new Color(0.62f, 0.76f, 1f), 1.05f, new Vector3(18f, 138f, 0f));
        CreatePreviewLight("Preview Rim Light", new Color(1f, 0.72f, 0.42f), 1.35f, new Vector3(12f, 205f, 0f));

        GameObject cameraObject = new GameObject("Lobby Lineup Preview Camera");
        cameraObject.transform.SetParent(previewRoot.transform, false);
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.fieldOfView = 30f;
        previewCamera.nearClipPlane = 0.05f;
        previewCamera.allowHDR = true;
        previewCamera.allowMSAA = true;

        previewTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32)
        {
            name = "Lobby Shared Player Lineup",
            antiAliasing = 2,
            filterMode = FilterMode.Bilinear
        };
        previewTexture.Create();
        previewCamera.targetTexture = previewTexture;
        lineupPreviewImage.image = previewTexture;
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
        previewLight.shadows = LightShadows.Soft;
    }

    private static void FrameLineup(Bounds bounds, Camera camera, bool splitLineup)
    {
        float height = Mathf.Max(1f, bounds.size.y);
        // Keep the accepted six-player scale for every lobby size. Wider
        // rosters are composed more tightly instead of shrinking every model.
        // Centre the actually visible silhouettes instead of only their slot
        // origins. Wide ears, horns and coats otherwise shift the perceived
        // centre even when the authored formation is mathematically symmetric.
        // Looking slightly above the bounds centre moves the lineup lower.
        float focusHeightOffset = splitLineup
            ? SplitLineupFocusHeightOffset
            : SingleRowFocusHeightOffset;
        Vector3 focus = new Vector3(
            bounds.center.x + LineupVisualCenterOffsetX,
            bounds.center.y + height * focusHeightOffset,
            bounds.center.z);
        camera.transform.position = focus + new Vector3(0f, height * 0.025f, FixedLineupCameraDistance);
        camera.transform.LookAt(focus);
    }

    private void PositionLineupLabels()
    {
        if (previewCamera == null)
            return;

        foreach (LineupLabelAnchor anchor in lineupLabelAnchors)
        {
            Vector3 viewport = previewCamera.WorldToViewportPoint(anchor.WorldPosition);
            float x = Mathf.Clamp(viewport.x, 0.06f, 0.94f);
            float y = Mathf.Clamp(viewport.y, 0.06f, 0.9f);
            anchor.Label.style.left = Length.Percent(x * 100f);
            anchor.Label.style.top = Length.Percent((1f - y) * 100f);
        }
    }

    private static bool TryGetRendererBounds(GameObject model, out Bounds bounds)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);
        return true;
    }

    private static bool TryGetCombinedRendererBounds(IReadOnlyList<GameObject> models, out Bounds bounds)
    {
        bool hasBounds = false;
        bounds = default;
        foreach (GameObject model in models)
        {
            if (!TryGetRendererBounds(model, out Bounds modelBounds))
                continue;

            if (!hasBounds)
            {
                bounds = modelBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(modelBounds);
            }
        }

        return hasBounds;
    }

    private void DestroyPreviewRig()
    {
        lineupLabelAnchors.Clear();
        if (lineupPreviewImage != null)
            lineupPreviewImage.image = null;

        if (previewTexture != null)
        {
            previewTexture.Release();
            Destroy(previewTexture);
        }

        if (previewRoot != null)
            Destroy(previewRoot);

        previewRoot = null;
        previewCamera = null;
        previewTexture = null;
    }

    private void ResetPreviewState()
    {
        DestroyPreviewRig();
        previewPlayer = null;
        selectedCharacter = null;
        renderedRosterSignature = null;
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
}
