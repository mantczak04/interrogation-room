using System;
using System.Collections.Generic;
using InterrogationRoom.Gameplay.Characters;
using InterrogationRoom.Gameplay.Interaction;
using InterrogationRoom.Gameplay.Weapons;
using InterrogationRoom.Networking;
using Mirror;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkIdentity))]
public class PlayerController : NetworkBehaviour, IRoundEliminationPort
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
    public float mouseSensitivity = 2f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;
    public Camera playerCamera;

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
    private int allocationKey;
    private float verticalVelocity;
    private float cameraPitch;
    private float seatedCameraYaw;
    private NetworkChairSeat activeSeat;
    private Vector3 smoothedLookTarget;
    private bool hasSmoothedLookTarget;
    private bool forceShowLocalModel;
    private bool isThirdPerson;
    private float thirdPersonDistance = 2.5f;
    private Vector3 firstPersonCameraLocalPos;
    private GameObject activeModelRoot;
    private Vector3 activeModelRootBaseLocalPos;
    private Mesh seatedPoseMesh;
    private float seatedButtToHipsHeight;
    private float seatedTorsoBackDepth;
    private bool hasSeatedButtHeight;
    private int seatedFrameCount;

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

    [SyncVar(hook = nameof(OnDancingChanged))]
    private bool isDancing;

    private const float InputSystemMouseScale = 0.1f;
    private const float LookAtDistance = 5f;
    private const float LookTargetSmoothSpeed = 25f;
    private const float MinLookAtHumanScale = 0.25f;
    private const float MaxLookAtHumanScale = 4f;
    private const float MaxVisualRootScaleDeviation = 0.05f;
    private const float MaxLookDownDegrees = 50f;
    private const float MaxLookUpDegrees = 60f;
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int LookPitchParameter = Animator.StringToHash("LookPitch");
    private static readonly int IsSeatedParameter = Animator.StringToHash("IsSeated");
    private static readonly int PunchParameter = Animator.StringToHash("Punch");
    private static readonly int PunchVariantParameter = Animator.StringToHash("PunchVariant");
    private static readonly int IsDeadParameter = Animator.StringToHash("IsDead");
    private static readonly int DanceParameter = Animator.StringToHash("Dance");
    private int nextPunchVariant;

    public static bool CursorReleased { get; private set; } = true;

    public bool IsSeated => isSeated;
    public bool IsDead => isDead;
    public bool IsEliminated => isDead;
    public CharacterId CharacterId => characterId;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerInteractor = GetComponent<PlayerInteractor>();
        playerWeaponController = GetComponent<PlayerWeaponController>();
        RefreshPlayerRenderers();
        ValidateCharacterVisuals();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
        }

        if (playerCamera != null)
        {
            audioListener = playerCamera.GetComponent<AudioListener>();
            firstPersonCameraLocalPos = playerCamera.transform.localPosition;
        }
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
        if (playerCamera != null)
        {
            cameraPitch = playerCamera.transform.localEulerAngles.x;
            if (cameraPitch > 180f)
            {
                cameraPitch -= 360f;
            }
        }

        SetCursorReleased(false);
    }

    public override void OnStopLocalPlayer()
    {
        SetCursorReleased(true);
    }

    private void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (WasCursorTogglePressed() && !CenteredNetworkManagerHUD.HandlesEscape)
        {
            SetCursorReleased(!CursorReleased);
        }

        if (CursorReleased)
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

        if (!interactionMovementLocked && WasDancePressed())
        {
            CmdToggleDance();
        }

        if (!interactionMovementLocked && WasPunchPressed() && CharacterActionRules.CanPunch(
                isDead,
                isSeated,
                playerWeaponController != null && playerWeaponController.HasWeapon))
        {
            CmdTryPunch();
        }

        if (WasCameraTogglePressed())
        {
            isThirdPerson = !isThirdPerson;
            RefreshRendererVisibility();
        }

        if (isThirdPerson)
        {
            float scroll = GetScrollInput();
            if (Mathf.Abs(scroll) > 0.001f)
            {
                thirdPersonDistance = Mathf.Clamp(
                    thirdPersonDistance - scroll * zoomSensitivity,
                    minZoomDistance,
                    maxZoomDistance);
            }
        }

        if (!interactionMovementLocked)
        {
            HandleCharacterHotkeys();
        }

        Look();
        if (!isSeated && !interactionMovementLocked)
        {
            Move();
        }
        else if (interactionMovementLocked)
        {
            SetMovementAnimationIdle();
        }
    }

    public static void SetCursorReleased(bool released)
    {
        CursorReleased = released;
        Cursor.lockState = released ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = released;
    }

    public bool TryRequestStand()
    {
        if (!isLocalPlayer || !isSeated || isDead)
        {
            return false;
        }

        CmdStand();
        return true;
    }

    [Server]
    public bool TrySitServer(NetworkChairSeat seat)
    {
        if (!NetworkServer.active || isDead || isSeated || seat == null || !seat.TryOccupyServer(netIdentity))
        {
            return false;
        }

        activeSeat = seat;
        isDancing = false;
        isSeated = true;
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
    private void CmdSelectCharacter(CharacterId selectedCharacter)
    {
        TrySwapCharacterServer(selectedCharacter, out _);
    }

    [Server]
    public bool TrySwapCharacterServer(CharacterId newCharacter, out CharacterId previousCharacter)
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

    public bool HasVisualFor(CharacterId candidate)
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
        if (animator == null)
        {
            return;
        }

        if (HasAnimatorParameter(PunchVariantParameter, AnimatorControllerParameterType.Int))
        {
            animator.SetInteger(PunchVariantParameter, variant);
        }

        animator.SetTrigger(PunchParameter);
    }

    [Command]
    private void CmdToggleDance()
    {
        bool hasWeapon = playerWeaponController != null && playerWeaponController.HasWeapon;
        if (isDancing)
        {
            isDancing = false;
            return;
        }

        if (CharacterActionRules.CanDance(isDead, isSeated, hasWeapon, SupportsDance(characterId)))
        {
            isDancing = true;
        }
    }

    [Command]
    private void CmdStopDance()
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
        if (animator != null)
        {
            animator.SetFloat(SpeedParameter, 0f);
            animator.SetBool(IsSeatedParameter, seated);
        }

        if (seated)
        {
            SetDancingLocally(false);
        }

        if (characterController != null)
        {
            characterController.enabled = !seated && (isLocalPlayer || isServer);
        }
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

        if (animator != null)
        {
            animator.runtimeAnimatorController = selected.animatorController;
            animator.avatar = selected.avatar;
            animator.Rebind();
            animator.Update(0f);
            SetMovementAnimationIdle();
            animator.SetBool(IsSeatedParameter, isSeated);
            animator.SetBool(IsDeadParameter, isDead);
            SetDancingLocally(isDancing && selected.supportsDance);
        }

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
    public void SetLocalModelVisible(bool visible)
    {
        forceShowLocalModel = visible;
        RefreshRendererVisibility();
    }

    private void RefreshRendererVisibility()
    {
        bool visible = !isLocalPlayer || forceShowLocalModel || isThirdPerson;
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
        animator?.SetBool(IsDeadParameter, dead);

        if (dead)
        {
            SetDancingLocally(false);
            ResetLookAtIkState();
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

    private void Look()
    {
        Vector2 mouseDelta = GetMouseDelta();
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        if (isThirdPerson)
        {
            // Third person orbits the full 360 degrees around the player, both
            // seated and standing; the player transform does not rotate.
            seatedCameraYaw += mouseX;
        }
        else if (isSeated)
        {
            seatedCameraYaw = Mathf.Clamp(seatedCameraYaw + mouseX, -70f, 70f);
        }
        else
        {
            seatedCameraYaw = 0f;
            transform.Rotate(Vector3.up * mouseX);
        }

        if (playerCamera == null)
        {
            return;
        }

        cameraPitch = Mathf.Clamp(cameraPitch - mouseY, -80f, 80f);
        Quaternion cameraRotation = Quaternion.Euler(cameraPitch, seatedCameraYaw, 0f);
        playerCamera.transform.localRotation = cameraRotation;

        if (isThirdPerson)
        {
            Vector3 worldPivot = transform.TransformPoint(firstPersonCameraLocalPos);
            Vector3 orbitOffset = cameraRotation * Vector3.forward * thirdPersonDistance;
            Vector3 worldTarget = transform.TransformPoint(firstPersonCameraLocalPos - orbitOffset);
            float obstacleDistance = GetCameraObstacleDistance(worldPivot, worldTarget, thirdPersonDistance);
            float finalDistance = Mathf.Max(0.2f, obstacleDistance - 0.2f);
            playerCamera.transform.localPosition =
                firstPersonCameraLocalPos - cameraRotation * Vector3.forward * finalDistance;
        }
        else
        {
            playerCamera.transform.localPosition = firstPersonCameraLocalPos;
        }

        if (animator != null)
        {
            animator.SetFloat(LookPitchParameter, cameraPitch);
        }
    }

    private float GetCameraObstacleDistance(Vector3 worldPivot, Vector3 worldTarget, float maxDistance)
    {
        Vector3 direction = worldTarget - worldPivot;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return maxDistance;
        }

        float closestDistance = maxDistance;
        foreach (RaycastHit hit in Physics.RaycastAll(worldPivot, direction.normalized, maxDistance))
        {
            if (hit.collider.isTrigger || hit.transform.root == transform.root)
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
            }
        }

        return closestDistance;
    }

    private void Move()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        Vector2 moveInput = GetMoveInput();

        if (isDancing && moveInput.sqrMagnitude > 0.01f)
        {
            SetDancingLocally(false);
            CmdStopDance();
        }

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move = Vector3.ClampMagnitude(move, 1f);

        if (animator != null)
        {
            animator.SetFloat(SpeedParameter, move.magnitude, 0.1f, Time.deltaTime);
        }

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

        Vector3 velocity = move * speed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private void SetMovementAnimationIdle()
    {
        animator?.SetFloat(SpeedParameter, 0f);
    }

    private void OnDancingChanged(bool _, bool dancing)
    {
        SetDancingLocally(dancing);
    }

    private void SetDancingLocally(bool dancing)
    {
        if (animator != null && HasAnimatorParameter(DanceParameter, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(DanceParameter, dancing);
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

    private bool HasAnimatorParameter(int parameterHash, AnimatorControllerParameterType parameterType)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == parameterHash && parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    private void LateUpdate()
    {
        AlignSeatedHipsToSeat();
    }

    /// <summary>
    /// Every character's sit clip carries a different hips offset from the
    /// animation root, so seating the root at the seat centre leaves some
    /// characters perched in front of, or hovering above, the chair. After
    /// the animator poses the skeleton, shift the visual model so the hips
    /// land over the seat and the measured bottom of the character rests on
    /// the seat surface — works for any character and any sit clip.
    /// </summary>
    private void AlignSeatedHipsToSeat()
    {
        if (activeModelRoot == null)
        {
            return;
        }

        if (!isSeated || isDead || animator == null || !animator.isHuman)
        {
            activeModelRoot.transform.localPosition = activeModelRootBaseLocalPos;
            seatedFrameCount = 0;
            hasSeatedButtHeight = false;
            return;
        }

        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hips == null)
        {
            return;
        }

        seatedFrameCount++;

        // Pull the hips toward the backrest, but not so far that the
        // character's measured back sinks deep into the backrest — a bulky
        // torso sits further forward on the seat. Up to 5 cm of overlap is
        // allowed (it reads as pressing into the backrest), so slim
        // characters keep their natural pose.
        float backOffset = seatedHipsBackOffset;
        if (hasSeatedButtHeight && seatedBackrestOffset > 0f)
        {
            float backrestLimit = seatedBackrestOffset - seatedTorsoBackDepth + 0.05f;
            backOffset = Mathf.Max(Mathf.Min(seatedHipsBackOffset, backrestLimit), -0.10f);
        }

        Vector3 desired = transform.position - transform.forward * backOffset;
        Vector3 delta = desired - hips.position;
        delta.y = 0f;

        // The sit-down transition takes a moment; measure once the pose has
        // settled and refine once more shortly after.
        if (seatedFrameCount == 30 || seatedFrameCount == 90)
        {
            MeasureSeatedButtHeight(hips);
        }

        if (hasSeatedButtHeight)
        {
            float seatTopY = transform.position.y +
                             (seatedSeatSurfaceHeight > 0f ? seatedSeatSurfaceHeight : 0.46f);
            float desiredHipsY = seatTopY - 0.015f + seatedButtToHipsHeight;
            delta.y = desiredHipsY - hips.position.y;
        }

        activeModelRoot.transform.position += delta;
    }

    /// <summary>
    /// Measures the seated body against the hips bone in the current pose:
    /// how far the lowest pelvis point sits below the hips (to rest it on the
    /// seat surface) and how far the back sticks out behind them (to keep it
    /// in front of the backrest).
    /// </summary>
    private void MeasureSeatedButtHeight(Transform hips)
    {
        SkinnedMeshRenderer skinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>(false);
        if (skinnedRenderer == null)
        {
            return;
        }

        seatedPoseMesh ??= new Mesh();
        skinnedRenderer.BakeMesh(seatedPoseMesh, true);

        Matrix4x4 localToWorld = skinnedRenderer.transform.localToWorldMatrix;
        Vector3 hipsPosition = hips.position;
        Vector3 back = -transform.forward;
        float lowestY = float.MaxValue;
        float backDepth = 0f;

        foreach (Vector3 vertex in seatedPoseMesh.vertices)
        {
            Vector3 world = localToWorld.MultiplyPoint3x4(vertex);

            Vector2 planar = new(world.x - hipsPosition.x, world.z - hipsPosition.z);
            if (planar.sqrMagnitude <= 0.17f * 0.17f &&
                world.y <= hipsPosition.y &&
                world.y >= hipsPosition.y - 0.3f && // pelvis area only, not calves/feet
                world.y < lowestY)
            {
                lowestY = world.y;
            }

            // Lower back / torso band that would meet a backrest.
            if (world.y >= hipsPosition.y &&
                world.y <= hipsPosition.y + 0.45f &&
                planar.sqrMagnitude <= 0.3f * 0.3f)
            {
                float depth = Vector3.Dot(world - hipsPosition, back);
                if (depth > backDepth)
                {
                    backDepth = depth;
                }
            }
        }

        if (lowestY < float.MaxValue)
        {
            seatedButtToHipsHeight = hipsPosition.y - lowestY;
            seatedTorsoBackDepth = backDepth;
            hasSeatedButtHeight = true;
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !animator.isHuman || isDead || (isLocalPlayer && isThirdPerson))
        {
            ResetLookAtIkState();
            return;
        }

        if (!TryGetActiveVisualRootScale(out Vector3 visualRootScale) ||
            !IsHumanoidIkScaleValid(animator.humanScale, visualRootScale))
        {
            animator.SetLookAtWeight(0f);
            ResetLookAtIkState();
            return;
        }

        Transform anchor = animator.GetBoneTransform(HumanBodyBones.Neck);
        if (anchor == null)
        {
            anchor = animator.GetBoneTransform(HumanBodyBones.Head);
        }

        if (anchor == null)
        {
            animator.SetLookAtWeight(0f);
            ResetLookAtIkState();
            return;
        }

        Vector3 lookDirection = GetClampedLookDirection();
        Vector3 desiredLookTarget = anchor.position + lookDirection * LookAtDistance;
        if (!hasSmoothedLookTarget)
        {
            smoothedLookTarget = desiredLookTarget;
            hasSmoothedLookTarget = true;
        }
        else
        {
            float smoothFactor = 1f - Mathf.Exp(-LookTargetSmoothSpeed * Time.deltaTime);
            smoothedLookTarget = Vector3.Lerp(smoothedLookTarget, desiredLookTarget, smoothFactor);
        }

        animator.SetLookAtWeight(1f, 0.2f, 0.85f, 0.35f, 0.5f);
        animator.SetLookAtPosition(smoothedLookTarget);
    }

    private Vector3 GetClampedLookDirection()
    {
        float pitch = Mathf.Clamp(
            GetBodyRelativeLookPitch(),
            -MaxLookDownDegrees,
            MaxLookUpDegrees);

        return (Quaternion.AngleAxis(pitch, transform.right) * transform.forward).normalized;
    }

    private float GetBodyRelativeLookPitch()
    {
        if (isLocalPlayer && playerCamera != null)
        {
            if (isSeated)
            {
                return GetPitchFromDirection(playerCamera.transform.forward);
            }

            return cameraPitch;
        }

        return animator.GetFloat(LookPitchParameter);
    }

    private float GetPitchFromDirection(Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return 0f;
        }

        Vector3 flatForward = Vector3.ProjectOnPlane(worldDirection, transform.right);
        if (flatForward.sqrMagnitude <= Mathf.Epsilon)
        {
            return worldDirection.y >= 0f ? MaxLookUpDegrees : -MaxLookDownDegrees;
        }

        flatForward.Normalize();
        return Vector3.SignedAngle(flatForward, worldDirection.normalized, transform.right);
    }

    private void ResetLookAtIkState()
    {
        hasSmoothedLookTarget = false;
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

    private static bool IsHumanoidIkScaleValid(float humanScale, Vector3 visualRootScale)
    {
        if (humanScale < MinLookAtHumanScale || humanScale > MaxLookAtHumanScale)
        {
            return false;
        }

        float maxAxisDeviation = Mathf.Max(
            Mathf.Abs(visualRootScale.x - 1f),
            Mathf.Abs(visualRootScale.y - 1f),
            Mathf.Abs(visualRootScale.z - 1f));

        return maxAxisDeviation <= MaxVisualRootScaleDeviation;
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

    private Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
        {
            return Vector2.zero;
        }

        return Mouse.current.delta.ReadValue() * InputSystemMouseScale;
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
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

    private bool WasCursorTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private bool WasCameraTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.C);
#endif
    }

    private static float GetScrollInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
        {
            return 0f;
        }

        float scroll = Mouse.current.scroll.ReadValue().y;
        return Mathf.Abs(scroll) > 0.0001f ? Mathf.Sign(scroll) : 0f;
#else
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        return Mathf.Abs(scroll) > 0.0001f ? Mathf.Sign(scroll) : 0f;
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
}
