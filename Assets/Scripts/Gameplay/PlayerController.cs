using System;
using System.Collections.Generic;
using InterrogationRoom.Gameplay.Characters;
using InterrogationRoom.Gameplay.Interaction;
using InterrogationRoom.Gameplay.Weapons;
using InterrogationRoom.Networking;
using InterrogationRoom.Settings;
using InterrogationRoom.UI;
using Mirror;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InterrogationRoom.Gameplay
{
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkIdentity))]
public class PlayerController : PlayerGameplayController, IRoundEliminationPort, IRoundRelocationPort
{
    [Serializable]
    private sealed class CharacterVisualDefinition
    {
        public CharacterId characterId;
        public GameObject modelRoot;
        public RuntimeAnimatorController animatorController;
        public Avatar avatar;
        public bool supportsDance;
    }

    [Header("Movement")]
    public float speed = 5f;
    public float mouseSensitivity = 1f;

    // The station has 1.18 m of headroom above a standing player, so a jump that
    // clears more than that ends with the capsule pinned against the ceiling.
    // The stronger gravity keeps the arc short instead of floaty.
    public float jumpHeight = 0.75f;
    public float gravity = -20f;
    public Camera playerCamera;

    [Header("Sprint")]
    [SerializeField, Min(1f)]
    [Tooltip("Multiplies the walking speed while sprinting.")]
    private float sprintSpeedMultiplier = 1.55f;

    [SerializeField, Min(0.5f)]
    [Tooltip("Seconds of continuous sprinting available from a full budget.")]
    private float sprintDurationSeconds = 3f;

    [SerializeField, Min(0.5f)]
    [Tooltip("Seconds needed to refill an empty budget once recovery starts.")]
    private float sprintRecoverySeconds = 6f;

    [SerializeField, Min(0f)]
    [Tooltip("Pause between releasing sprint and the budget starting to refill.")]
    private float sprintRecoveryDelaySeconds = 0.6f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("Budget required to start a new sprint, so a drained player cannot stutter-tap it.")]
    private float sprintMinimumChargeToStart = 0.25f;

    [Header("Characters")]
    [SerializeField] private CharacterVisualDefinition[] characterVisuals = Array.Empty<CharacterVisualDefinition>();

    [Header("Seated Pose")]
    [SerializeField, Range(-0.2f, 0.2f)] private float seatedHipsBackOffset = 0.06f;

    [Header("Third Person Camera")]
    [SerializeField, Min(0.5f)] private float minZoomDistance = 1f;
    [SerializeField, Min(1f)] private float maxZoomDistance = 6f;
    [SerializeField, Min(0.05f)] private float zoomSensitivity = 0.5f;

    private CharacterController characterController;
    private Animator animator;
    private AudioListener audioListener;
    private Renderer[] playerRenderers;
    private PlayerInteractor playerInteractor;
    private PlayerWeaponController playerWeaponController;
    private PlayerCameraRig cameraRig;
    private PlayerSeating seating;
    private PlayerAnimationDriver animationDriver;
    private int allocationKey;
    private float verticalVelocity;
    private float sprintCharge01 = 1f;
    private float sprintRecoveryDelayRemaining;
    private bool isSprinting;
    private NetworkChairSeat activeSeat;
    private bool forceShowLocalModel;
    private GameObject activeModelRoot;
    private Vector3 activeModelRootBaseLocalPos;

    [SyncVar]
    private float seatedSeatSurfaceHeight;

    [SyncVar]
    private float seatedBackrestOffset;

    [SyncVar(hook = nameof(OnSeatedChanged))]
    private bool isSeated;

    [SyncVar(hook = nameof(OnCharacterChanged))]
    private CharacterId characterId;

    [SyncVar(hook = nameof(OnDeadChanged))]
    private bool isDead;

    [SyncVar(hook = nameof(OnDanceIndexChanged))]
    private int danceIndex;

    [SyncVar(hook = nameof(OnDancingChanged))]
    private bool isDancing;

    private int nextPunchVariant;
    private IDanceRadialMenu danceRadialMenu;

    public override bool IsSeated => isSeated;
    public override bool IsDead => isDead;
    public bool IsEliminated => isDead;
    public override CharacterId CharacterId => characterId;
    public override Camera PlayerCamera => playerCamera;
    public override bool IsThirdPerson => cameraRig != null && cameraRig.IsThirdPerson;

    /// <summary>Remaining sprint budget, 0..1, for a HUD to render.</summary>
    public float SprintCharge01 => sprintCharge01;

    public bool IsSprinting => isSprinting;

    public GameObject CreateCharacterPreview(CharacterId selectedCharacter, Transform parent)
    {
        CharacterVisualDefinition selected = null;
        foreach (CharacterVisualDefinition visual in characterVisuals)
        {
            if (visual != null && visual.characterId == selectedCharacter)
            {
                selected = visual;
                break;
            }
        }

        if (selected?.modelRoot == null || selected.animatorController == null || selected.avatar == null)
            return null;

        GameObject preview = Instantiate(selected.modelRoot, parent, false);
        preview.name = $"{selectedCharacter} Lobby Preview";
        preview.SetActive(true);

        Animator previewAnimator = preview.GetComponent<Animator>();
        if (previewAnimator == null)
            previewAnimator = preview.AddComponent<Animator>();
        previewAnimator.runtimeAnimatorController = selected.animatorController;
        previewAnimator.avatar = selected.avatar;
        previewAnimator.applyRootMotion = false;
        previewAnimator.Rebind();
        previewAnimator.Update(0f);
        foreach (Renderer previewRenderer in preview.GetComponentsInChildren<Renderer>(true))
            previewRenderer.enabled = true;
        return preview;
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerInteractor = GetComponent<PlayerInteractor>();
        playerWeaponController = GetComponent<PlayerWeaponController>();
        animationDriver = GetComponent<PlayerAnimationDriver>() ??
                          gameObject.AddComponent<PlayerAnimationDriver>();
        cameraRig = GetComponent<PlayerCameraRig>() ??
                    gameObject.AddComponent<PlayerCameraRig>();
        seating = GetComponent<PlayerSeating>() ??
                  gameObject.AddComponent<PlayerSeating>();
        RefreshPlayerRenderers();
        ValidateCharacterVisuals();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
        }

        if (playerCamera != null)
            audioListener = playerCamera.GetComponent<AudioListener>();

        animationDriver.Configure(animator);
        cameraRig.Configure(
            playerCamera,
            animationDriver,
            minZoomDistance,
            maxZoomDistance,
            zoomSensitivity);
        seating.Configure(characterController, animator, seatedHipsBackOffset);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyCharacter(characterId);

        bool local = isLocalPlayer;

        if (playerCamera != null)
        {
            playerCamera.enabled = local;
        }

        if (audioListener != null)
        {
            audioListener.enabled = local;
        }

        characterController.enabled = local || isServer;
        RefreshSeatedState();
        RefreshRendererVisibility();
        SetDeadLocally(isDead);
        NotifyClientStarted(this);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        allocationKey = connectionToClient != null ? connectionToClient.connectionId : netId.GetHashCode();
        if (NetworkCharacterAllocator.Instance == null)
        {
            Debug.LogError(
                $"The scene requires an active {nameof(NetworkCharacterAllocator)} on its NetworkManager.",
                this);
        }
        else
        {
            characterId = NetworkCharacterAllocator.Instance.Acquire(allocationKey);
        }

    }

    public override void OnStartLocalPlayer()
    {
        GameSettings settings = GameSettingsService.Current;
        settings.SetMouseSensitivityFallback(mouseSensitivity);
        mouseSensitivity = settings.MouseSensitivity;
        settings.Changed += ApplyGameSettings;

        danceRadialMenu = DanceRadialMenuHost.Create(gameObject);
        PlayerInputGate.SetPlayerCursorReleased(false);
    }

    public override void OnStopLocalPlayer()
    {
        GameSettingsService.Current.Changed -= ApplyGameSettings;
        danceRadialMenu?.Cancel();
        PlayerInputGate.SetPlayerCursorReleased(true);
    }

    private void ApplyGameSettings()
    {
        mouseSensitivity = GameSettingsService.Current.MouseSensitivity;
    }

    private void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (danceRadialMenu != null && danceRadialMenu.IsOpen)
        {
            HandleDanceRadialMenu();
            SetMovementAnimationIdle();
            return;
        }

        if (PlayerInputGate.CursorReleased)
        {
            SetMovementAnimationIdle();
            return;
        }

        if (isDead)
        {
            SetMovementAnimationIdle();
            return;
        }

        bool interactionMovementLocked = playerInteractor != null && playerInteractor.IsMovementLocked;

        bool hasWeapon = playerWeaponController != null && playerWeaponController.HasWeapon;
        if (isDancing && hasWeapon)
        {
            SetDancingLocally(false);
            CmdStopDance();
        }

        if (!interactionMovementLocked &&
            WasDancePressed() &&
            (isDancing || CharacterActionRules.CanDance(
                isDead,
                isSeated,
                hasWeapon,
                SupportsDance(characterId))))
        {
            danceRadialMenu?.Open(isDancing);
            SetMovementAnimationIdle();
            return;
        }

        if (!interactionMovementLocked && WasPunchPressed() && CharacterActionRules.CanPunch(
                isDead,
                isSeated,
                playerWeaponController != null && playerWeaponController.HasWeapon))
        {
            CmdTryPunch();
        }

        if (cameraRig.Tick(isSeated, mouseSensitivity))
            RefreshRendererVisibility();

        if (!interactionMovementLocked)
        {
            HandleCharacterHotkeys();
        }

        if (!isSeated && !interactionMovementLocked)
        {
            Move();
        }
        else if (interactionMovementLocked)
        {
            SetMovementAnimationIdle();
        }
    }

    public override bool TryRequestStand()
    {
        if (!isLocalPlayer || !isSeated || isDead)
        {
            return false;
        }

        CmdStand();
        return true;
    }

    [Server]
    public override bool TrySitServer(NetworkChairSeat seat)
    {
        if (!NetworkServer.active || isDead || isSeated || seat == null || !seat.TryOccupyServer(netIdentity))
        {
            return false;
        }

        activeSeat = seat;
        isDancing = false;
        isSeated = true;
        ResetSprint();
        seatedSeatSurfaceHeight = seat.SeatSurfaceHeight;
        seatedBackrestOffset = seat.BackrestOffset;
        verticalVelocity = 0f;
        ApplySeatPose(seat.SeatPosition, seat.SeatRotation);
        TargetApplyPose(connectionToClient, seat.SeatPosition, seat.SeatRotation, true);
        return true;
    }

    [Command]
    private void CmdStand()
    {
        StandServer();
    }

    private void HandleCharacterHotkeys()
    {
        CharacterId? requested = GetPressedCharacterHotkey();
        if (requested.HasValue)
        {
            CmdSelectCharacter(requested.Value);
        }
    }

    /// <summary>
    /// Testing shortcut: keys 1-5 switch the character directly, bypassing the
    /// swap stations' uniqueness guarantee.
    /// </summary>
    [Command]
    public void CmdSelectCharacter(CharacterId selectedCharacter)
    {
        TrySwapCharacterServer(selectedCharacter, out _);
    }

    [Server]
    public override bool TrySwapCharacterServer(CharacterId newCharacter, out CharacterId previousCharacter)
    {
        previousCharacter = characterId;
        if (isDead || isSeated || newCharacter == characterId || !HasVisualFor(newCharacter))
        {
            return false;
        }

        isDancing = false;
        characterId = newCharacter;
        return true;
    }

    public override bool HasVisualFor(CharacterId candidate)
    {
        foreach (CharacterVisualDefinition visual in characterVisuals)
        {
            if (visual != null && visual.characterId == candidate && visual.modelRoot != null)
            {
                return true;
            }
        }

        return false;
    }

    [Command]
    private void CmdTryPunch()
    {
        if (!CharacterActionRules.CanPunch(
                isDead,
                isSeated,
                playerWeaponController != null && playerWeaponController.HasWeapon))
        {
            return;
        }

        isDancing = false;
        int variant = nextPunchVariant;
        nextPunchVariant = 1 - nextPunchVariant;
        RpcPlayPunch(variant);
    }

    [ClientRpc]
    private void RpcPlayPunch(int variant)
    {
        animationDriver.PlayPunch(variant);
    }

    [Command]
    private void CmdSelectDance(int selectedDance)
    {
        if (!DanceRadialSelection.IsValid(selectedDance))
            return;

        bool hasWeapon = playerWeaponController != null && playerWeaponController.HasWeapon;
        if (CharacterActionRules.CanDance(isDead, isSeated, hasWeapon, SupportsDance(characterId)))
        {
            danceIndex = selectedDance;
            isDancing = true;
        }
    }

    [Command]
    private void CmdStopDance()
    {
        StopDanceServer();
    }

    [Server]
    public void StopDanceServer()
    {
        isDancing = false;
    }

    [Server]
    private void StandServer()
    {
        if (!isSeated || activeSeat == null)
        {
            return;
        }

        Vector3 standPosition = activeSeat.GetStandPositionServer();
        Quaternion standRotation = activeSeat.SeatRotation;
        activeSeat.ReleaseServer(netIdentity);
        activeSeat = null;
        isSeated = false;
        verticalVelocity = -2f;
        ApplySeatPose(standPosition, standRotation);
        TargetApplyPose(connectionToClient, standPosition, standRotation, false);
    }

    [TargetRpc]
    private void TargetApplyPose(
        NetworkConnectionToClient _,
        Vector3 position,
        Quaternion rotation,
        bool seated)
    {
        isSeated = seated;
        ApplySeatPose(position, rotation);
        SetSeatedLocally(seated);
    }

    private void ApplySeatPose(Vector3 position, Quaternion rotation)
    {
        bool wasEnabled = characterController != null && characterController.enabled;
        if (wasEnabled)
        {
            characterController.enabled = false;
        }

        transform.SetPositionAndRotation(position, rotation);

        if (characterController != null)
        {
            characterController.enabled = !isSeated && (isLocalPlayer || isServer);
        }
    }

    private void OnSeatedChanged(bool _, bool seated)
    {
        SetSeatedLocally(seated);
    }

    private void SetSeatedLocally(bool seated)
    {
        animationDriver.SetSeated(seated);
        seating.SetSeatGeometry(seatedSeatSurfaceHeight, seatedBackrestOffset);

        if (seated)
            SetDancingLocally(false);

        seating.SetLocalState(seated, !seated && (isLocalPlayer || isServer));
    }

    private void RefreshSeatedState()
    {
        SetSeatedLocally(isSeated);
    }

    private void OnCharacterChanged(CharacterId _, CharacterId selectedCharacter)
    {
        ApplyCharacter(selectedCharacter);
    }

    private void ApplyCharacter(CharacterId selectedCharacter)
    {
        CharacterVisualDefinition selected = null;
        foreach (CharacterVisualDefinition visual in characterVisuals)
        {
            if (visual == null)
            {
                continue;
            }

            bool active = visual.characterId == selectedCharacter;
            if (visual.modelRoot != null)
            {
                visual.modelRoot.SetActive(active);
            }

            if (active)
            {
                selected = visual;
            }
        }

        if (selected == null)
        {
            Debug.LogError($"No visual is configured for character '{selectedCharacter}'.", this);
            return;
        }

        activeModelRoot = selected.modelRoot;
        activeModelRootBaseLocalPos = selected.modelRoot.transform.localPosition;
        seating.SetVisualRoot(activeModelRoot, activeModelRootBaseLocalPos);

        animationDriver.Rebind(
            selected.animatorController,
            selected.avatar,
            isSeated,
            isDead);
        SetMovementAnimationIdle();
        SetDanceIndexLocally(danceIndex);
        SetDancingLocally(isDancing && selected.supportsDance);

        RefreshPlayerRenderers();
        RefreshRendererVisibility();
    }

    private void RefreshPlayerRenderers()
    {
        playerRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void ValidateCharacterVisuals()
    {
        var configuredCharacters = new HashSet<CharacterId>();
        foreach (CharacterVisualDefinition visual in characterVisuals)
        {
            if (visual == null ||
                visual.modelRoot == null ||
                visual.animatorController == null ||
                visual.avatar == null ||
                !visual.avatar.isHuman ||
                !configuredCharacters.Add(visual.characterId))
            {
                Debug.LogError(
                    "Character visuals must contain one complete, unique Humanoid definition per character.",
                    this);
                return;
            }
        }

        if (configuredCharacters.Count != CharacterAssignmentRoster.DefaultCharacters.Count)
        {
            Debug.LogError(
                $"Expected {CharacterAssignmentRoster.DefaultCharacters.Count} character visuals, " +
                $"but found {configuredCharacters.Count}.",
                this);
        }
    }

    /// <summary>
    /// Local-only debug aid: shows the normally hidden local player model so
    /// animations can be inspected from an external camera.
    /// </summary>
    public override void SetLocalModelVisible(bool visible)
    {
        forceShowLocalModel = visible;
        RefreshRendererVisibility();
    }

    private void RefreshRendererVisibility()
    {
        bool visible = !isLocalPlayer || forceShowLocalModel || IsThirdPerson;
        foreach (Renderer playerRenderer in playerRenderers)
        {
            if (playerRenderer != null)
            {
                playerRenderer.enabled = visible;
            }
        }
    }

    private void OnDeadChanged(bool _, bool dead)
    {
        SetDeadLocally(dead);
    }

    private void SetDeadLocally(bool dead)
    {
        animationDriver.SetDead(dead);

        if (dead)
        {
            SetDancingLocally(false);
        }

        if (characterController != null)
        {
            characterController.enabled = !dead && !isSeated && (isLocalPlayer || isServer);
        }

        if (playerInteractor != null)
        {
            playerInteractor.enabled = !dead;
        }

        if (playerWeaponController != null)
        {
            playerWeaponController.enabled = !dead;
        }
    }

    [Server]
    public bool TryEliminateServer()
    {
        if (!NetworkServer.active || !CharacterActionRules.CanDie(isDead))
        {
            return false;
        }

        if (isSeated)
        {
            StandServer();
        }

        isDancing = false;
        isDead = true;
        verticalVelocity = 0f;
        ResetSprint();
        return true;
    }

    [Server]
    public bool ResetEliminationServer()
    {
        if (!NetworkServer.active || !isDead)
        {
            return false;
        }

        isDead = false;
        verticalVelocity = 0f;
        return true;
    }

    [Server]
    public bool RelocateToStartRoomServer(Vector3 position, Quaternion rotation)
    {
        if (!NetworkServer.active)
        {
            return false;
        }

        if (activeSeat != null)
        {
            activeSeat.ReleaseServer(netIdentity);
            activeSeat = null;
        }

        isSeated = false;
        isDancing = false;
        verticalVelocity = 0f;
        ResetSprint();
        SetSeatedLocally(false);

        NetworkTransformBase networkTransform = GetComponent<NetworkTransformBase>();
        if (networkTransform == null)
        {
            return false;
        }

        networkTransform.ServerTeleport(position, rotation);
        return true;
    }

    private void Move()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        // A rising capsule that meets the ceiling keeps its upward velocity, so it
        // stays pinned there until gravity cancels it out. Dropping the velocity on
        // contact makes the player fall away immediately instead.
        verticalVelocity = PlayerJumpMotion.ClampVerticalVelocityAtCeiling(
            verticalVelocity,
            (characterController.collisionFlags & CollisionFlags.Above) != 0);

        Vector2 moveInput = GetMoveInput();

        if (isDancing && moveInput.sqrMagnitude > 0.01f)
        {
            SetDancingLocally(false);
            CmdStopDance();
        }

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move = Vector3.ClampMagnitude(move, 1f);

        // Sprinting is a forward burst; it does not turn strafing into a getaway.
        bool wantsSprint = IsSprintHeld() && moveInput.y > 0.1f && !isDancing;
        isSprinting = PlayerSprintStamina.Advance(
            wantsSprint,
            isSprinting,
            Time.deltaTime,
            sprintDurationSeconds,
            sprintRecoverySeconds,
            sprintRecoveryDelaySeconds,
            sprintMinimumChargeToStart,
            ref sprintCharge01,
            ref sprintRecoveryDelayRemaining);

        // The locomotion blend tree only spans idle..walk, so the animator keeps
        // receiving a normalised speed while the body travels faster.
        animationDriver.SetMovementSpeed(move.magnitude, true);

        if (characterController.isGrounded && WasJumpPressed())
        {
            if (isDancing)
            {
                SetDancingLocally(false);
                CmdStopDance();
            }

            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * (speed * (isSprinting ? sprintSpeedMultiplier : 1f));
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private void ResetSprint()
    {
        isSprinting = false;
        sprintCharge01 = 1f;
        sprintRecoveryDelayRemaining = 0f;
    }

    private void SetMovementAnimationIdle()
    {
        animationDriver.SetMovementSpeed(0f, false);
    }

    private void OnDancingChanged(bool _, bool dancing)
    {
        SetDancingLocally(dancing);
    }

    private void OnDanceIndexChanged(int _, int selectedDance)
    {
        SetDanceIndexLocally(selectedDance);
    }

    private void SetDancingLocally(bool dancing)
    {
        SetDanceIndexLocally(danceIndex);
        animationDriver.SetDancing(dancing);
    }

    private void SetDanceIndexLocally(int selectedDance)
    {
        animationDriver.SetDanceIndex(selectedDance);
    }

    private void HandleDanceRadialMenu()
    {
        bool hasWeapon = playerWeaponController != null && playerWeaponController.HasWeapon;
        bool movementLocked = playerInteractor != null && playerInteractor.IsMovementLocked;
        if (isDead || isSeated || hasWeapon || movementLocked)
        {
            danceRadialMenu.Cancel();
            if (isDancing)
                CmdStopDance();
            return;
        }

        danceRadialMenu.RefreshSelection();
        if (!WasDanceReleased())
            return;

        int selectedDance = danceRadialMenu.Close();
        if (DanceRadialSelection.IsValid(selectedDance))
        {
            CmdSelectDance(selectedDance);
        }
        else if (isDancing)
        {
            CmdStopDance();
        }
    }

    private bool SupportsDance(CharacterId candidate)
    {
        foreach (CharacterVisualDefinition visual in characterVisuals)
        {
            if (visual != null && visual.characterId == candidate)
            {
                return visual.supportsDance;
            }
        }

        return false;
    }

    private void LateUpdate()
    {
        seating?.Tick(isSeated, isDead);
    }


    private void OnAnimatorIK(int layerIndex)
    {
        float bodyRelativePitch = isLocalPlayer
            ? cameraRig.GetBodyRelativePitch(isSeated)
            : animationDriver.GetRemoteLookPitch();
        bool hasVisualRootScale =
            TryGetActiveVisualRootScale(out Vector3 visualRootScale);
        animationDriver.ApplyLookAtIk(
            isDead,
            isLocalPlayer,
            IsThirdPerson,
            isDancing,
            bodyRelativePitch,
            hasVisualRootScale,
            visualRootScale);
    }


    private bool TryGetActiveVisualRootScale(out Vector3 visualRootScale)
    {
        visualRootScale = Vector3.one;

        foreach (CharacterVisualDefinition visual in characterVisuals)
        {
            if (visual?.modelRoot == null || !visual.modelRoot.activeInHierarchy)
            {
                continue;
            }

            visualRootScale = visual.modelRoot.transform.lossyScale;
            return true;
        }

        return false;
    }

    public override void OnStopServer()
    {
        if (activeSeat != null)
        {
            activeSeat.ReleaseServer(netIdentity);
            activeSeat = null;
        }

        NetworkCharacterAllocator.Instance?.Release(allocationKey);

        base.OnStopServer();
    }

    private Vector2 GetMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        Vector2 input = Vector2.zero;

        if (Keyboard.current.aKey.isPressed)
        {
            input.x -= 1f;
        }

        if (Keyboard.current.dKey.isPressed)
        {
            input.x += 1f;
        }

        if (Keyboard.current.sKey.isPressed)
        {
            input.y -= 1f;
        }

        if (Keyboard.current.wKey.isPressed)
        {
            input.y += 1f;
        }

        return Vector2.ClampMagnitude(input, 1f);
#else
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
    }

    private bool IsSprintHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }

    private bool WasJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private static CharacterId? GetPressedCharacterHotkey()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return null;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame) return CharacterId.Malpa;
        if (Keyboard.current.digit2Key.wasPressedThisFrame) return CharacterId.Wieprz;
        if (Keyboard.current.digit3Key.wasPressedThisFrame) return CharacterId.Jak;
        if (Keyboard.current.digit4Key.wasPressedThisFrame) return CharacterId.Karton;
        if (Keyboard.current.digit5Key.wasPressedThisFrame) return CharacterId.Ptaku;
#else
        if (Input.GetKeyDown(KeyCode.Alpha1)) return CharacterId.Malpa;
        if (Input.GetKeyDown(KeyCode.Alpha2)) return CharacterId.Wieprz;
        if (Input.GetKeyDown(KeyCode.Alpha3)) return CharacterId.Jak;
        if (Input.GetKeyDown(KeyCode.Alpha4)) return CharacterId.Karton;
        if (Input.GetKeyDown(KeyCode.Alpha5)) return CharacterId.Ptaku;
#endif
        return null;
    }

    private bool WasPunchPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private bool WasDancePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.T);
#endif
    }

    private bool WasDanceReleased()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.tKey.wasReleasedThisFrame;
#else
        return Input.GetKeyUp(KeyCode.T);
#endif
    }
}
}
