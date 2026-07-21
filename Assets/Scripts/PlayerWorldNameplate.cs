using InterrogationRoom.Networking;
using InterrogationRoom.UI;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerWorldNameplate : MonoBehaviour
{
    private const string NameplateObjectName = "Player Nameplate";
    private const float DefaultHeadHeight = 1.45f;
    private const float MaximumModelHeight = 2.5f;
    private const float HeadClearance = 0.12f;
    private const float CanvasScale = 0.005f;
    private const float CoordinatorRetryInterval = 0.5f;

    private PlayerController owner;
    private NetworkRoundCoordinator coordinator;
    private Canvas canvas;
    private TextMeshProUGUI label;
    private Camera viewingCamera;
    private bool hasDisplayName;
    private float nextCoordinatorLookup;

    public static PlayerWorldNameplate Attach(PlayerController player)
    {
        if (player == null)
            return null;

        PlayerWorldNameplate nameplate = player.GetComponent<PlayerWorldNameplate>();
        if (nameplate == null)
            nameplate = player.gameObject.AddComponent<PlayerWorldNameplate>();

        nameplate.Initialize(player);
        return nameplate;
    }

    private void Initialize(PlayerController player)
    {
        owner = player;
        EnsureVisuals();
        ResolveCoordinator(force: true);
        RefreshDisplayName();
        RefreshVisibility();
    }

    private void LateUpdate()
    {
        if (owner == null)
            return;

        ResolveCoordinator(force: false);
        RefreshViewingCamera();
        RefreshVisibility();
        if (canvas == null || !canvas.enabled || viewingCamera == null)
            return;

        canvas.transform.position = ResolveWorldAnchor();
        canvas.transform.rotation = viewingCamera.transform.rotation;
        canvas.worldCamera = viewingCamera;
    }

    private void OnDestroy()
    {
        UnsubscribeCoordinator();
    }

    private void EnsureVisuals()
    {
        if (canvas != null && label != null)
            return;

        GameObject visual = new GameObject(
            NameplateObjectName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        visual.transform.SetParent(owner.transform, false);
        visual.transform.localScale = Vector3.one * CanvasScale;

        var rect = (RectTransform)visual.transform;
        rect.sizeDelta = new Vector2(260f, 54f);

        canvas = visual.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        var scaler = visual.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        GameObject textObject = new GameObject(
            "Name",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(visual.transform, false);
        var textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        label = textObject.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = 32f;
        label.fontStyle = FontStyles.Bold;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.richText = false;
        label.raycastTarget = false;
        label.outlineColor = new Color32(8, 10, 11, 245);
        label.outlineWidth = 0.2f;
    }

    private void ResolveCoordinator(bool force)
    {
        if (coordinator != null || !force && Time.unscaledTime < nextCoordinatorLookup)
            return;

        nextCoordinatorLookup = Time.unscaledTime + CoordinatorRetryInterval;
        NetworkRoundCoordinator found = FindFirstObjectByType<NetworkRoundCoordinator>();
        if (found == coordinator)
            return;

        UnsubscribeCoordinator();
        coordinator = found;
        if (coordinator != null)
            coordinator.LobbyStateChanged += RefreshDisplayName;
    }

    private void UnsubscribeCoordinator()
    {
        if (coordinator != null)
            coordinator.LobbyStateChanged -= RefreshDisplayName;
        coordinator = null;
    }

    private void RefreshDisplayName()
    {
        string displayName = string.Empty;
        hasDisplayName = owner != null &&
            coordinator != null &&
            PlayerWorldNameplatePresentation.TryResolveDisplayName(
                coordinator.PublicLobbyPlayers,
                owner.netId,
                out displayName);

        if (label != null)
            label.text = hasDisplayName ? displayName : string.Empty;
    }

    private void RefreshViewingCamera()
    {
        PlayerController localPlayer = NetworkClient.localPlayer != null
            ? NetworkClient.localPlayer.GetComponent<PlayerController>()
            : null;
        Camera candidate = localPlayer != null ? localPlayer.playerCamera : null;
        if (candidate != null && candidate.isActiveAndEnabled)
            viewingCamera = candidate;
        else if (viewingCamera == null || !viewingCamera.isActiveAndEnabled)
            viewingCamera = Camera.main;
    }

    private void RefreshVisibility()
    {
        if (canvas == null || owner == null)
            return;

        canvas.enabled = PlayerWorldNameplatePresentation.ShouldShow(
            hasDisplayName,
            owner.isLocalPlayer,
            owner.IsThirdPerson);
    }

    private Vector3 ResolveWorldAnchor()
    {
        float top = owner.transform.position.y + DefaultHeadHeight;
        Renderer[] renderers = owner.GetComponentsInChildren<Renderer>(false);
        for (int index = 0; index < renderers.Length; index++)
        {
            Renderer playerRenderer = renderers[index];
            if (playerRenderer == null ||
                !playerRenderer.enabled ||
                !(playerRenderer is SkinnedMeshRenderer) &&
                !(playerRenderer is MeshRenderer))
            {
                continue;
            }

            float rendererTop = playerRenderer.bounds.max.y;
            if (rendererTop <= owner.transform.position.y + MaximumModelHeight)
                top = Mathf.Max(top, rendererTop);
        }

        Vector3 position = owner.transform.position;
        position.y = top + HeadClearance;
        return position;
    }
}
