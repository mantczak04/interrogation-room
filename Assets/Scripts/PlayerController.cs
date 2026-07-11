using System;
using System.Collections.Generic;
using InterrogationRoom.Gameplay.Characters;
using InterrogationRoom.Gameplay.Interaction;
using InterrogationRoom.Gameplay.Weapons;
using Mirror;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkIdentity))]
public class PlayerController : NetworkBehaviour
{
    [Serializable]
    private sealed class CharacterVisualDefinition
    {
        public CharacterId characterId;
        public GameObject modelRoot;
        public RuntimeAnimatorController animatorController;
        public Avatar avatar;
        public float hipsOffset;
    }

    [Header("Movement")]
    public float speed = 5f;
    public float mouseSensitivity = 2f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;
    public Camera playerCamera;

    [Header("Characters")]
    [SerializeField] private CharacterVisualDefinition[] characterVisuals = Array.Empty<CharacterVisualDefinition>();

    private CharacterController characterController;
    private Animator animator;
    private AudioListener audioListener;
    private Renderer[] playerRenderers;
    private PlayerInteractor playerInteractor;
    private PlayerWeaponController playerWeaponController;
    private ShotHitbox shotHitbox;
    private int allocationKey;
    private float verticalVelocity;
    private float cameraPitch;
    private float seatedCameraYaw;
    private NetworkChairSeat activeSeat;
    private Vector3 smoothedLookTarget;
    private bool hasSmoothedLookTarget;
    private Vector3 firstPersonCameraLocalPos;
    private bool isThirdPerson;

    [Header("Third Person Camera")]
    [SerializeField] private float minZoomDistance = 1.0f;
    [SerializeField] private float maxZoomDistance = 10.0f;
    [SerializeField] private float zoomSensitivity = 0.5f;
    private float thirdPersonDistance = 2.5f;

    [SyncVar(hook = nameof(OnSeatedChanged))]
    private bool isSeated;

    [SyncVar(hook = nameof(OnCharacterChanged))]
    private CharacterId characterId;

    [SyncVar(hook = nameof(OnDeadChanged))]
    private bool isDead;

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
    private static readonly int IsDeadParameter = Animator.StringToHash("IsDead");

    public static bool CursorReleased { get; private set; } = true;

    public bool IsSeated => isSeated;
    public bool IsDead => isDead;
    public CharacterId CharacterId => characterId;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerInteractor = GetComponent<PlayerInteractor>();
        playerWeaponController = GetComponent<PlayerWeaponController>();
        shotHitbox = GetComponent<ShotHitbox>();
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
        else
        {
            firstPersonCameraLocalPos = new Vector3(0f, 1.6f, 0f);
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

        if (shotHitbox != null)
        {
            shotHitbox.HitReceivedServer += OnShotHitServer;
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

        // Testing shortcuts to change character on the fly: keys 1, 2, 3, 4
        if (WasKeyJustPressed(KeyCode.Alpha1)) CmdChangeCharacter(CharacterId.Malpa);
        else if (WasKeyJustPressed(KeyCode.Alpha2)) CmdChangeCharacter(CharacterId.Wieprz);
        else if (WasKeyJustPressed(KeyCode.Alpha3)) CmdChangeCharacter(CharacterId.Jak);
        else if (WasKeyJustPressed(KeyCode.Alpha4)) CmdChangeCharacter(CharacterId.Karton);

        if (WasCursorTogglePressed())
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

        if (WasPunchPressed() && CharacterActionRules.CanPunch(
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
                thirdPersonDistance = Mathf.Clamp(thirdPersonDistance - scroll * zoomSensitivity, minZoomDistance, maxZoomDistance);
            }
        }

        Look();
        if (!isSeated)
        {
            Move();
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
        isSeated = true;
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

    [Command]
    private void CmdChangeCharacter(CharacterId selected)
    {
        characterId = selected;
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

        RpcPlayPunch();
    }

    [ClientRpc]
    private void RpcPlayPunch()
    {
        animator?.SetTrigger(PunchParameter);
    }

    [Server]
    private void StandServer()
    {
        if (!isSeated || activeSeat == null)
        {
            return;
        }

        Vector3 standPosition = activeSeat.StandPosition;
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

        if (characterController != null)
        {
            characterController.enabled = !seated && (isLocalPlayer || isServer);
        }

        if (!seated)
        {
            Transform visuals = transform.Find("Visuals");
            if (visuals != null)
            {
                visuals.localPosition = Vector3.zero;
            }
        }
    }

    private void LateUpdate()
    {
        if (isSeated && animator != null)
        {
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform visuals = transform.Find("Visuals");
            if (hips != null && visuals != null)
            {
                float targetHipsWorldY = transform.position.y + GetCharacterHipsOffset(characterId);
                float currentHipsWorldY = hips.position.y;
                float diff = targetHipsWorldY - currentHipsWorldY;
                
                Vector3 localPos = visuals.localPosition;
                localPos.y += diff;
                visuals.localPosition = localPos;
            }
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

        if (animator != null)
        {
            animator.runtimeAnimatorController = selected.animatorController;
            animator.avatar = selected.avatar;
            animator.Rebind();
            animator.Update(0f);
            SetMovementAnimationIdle();
            animator.SetBool(IsSeatedParameter, isSeated);
            animator.SetBool(IsDeadParameter, isDead);
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

    private void RefreshRendererVisibility()
    {
        bool visible = !isLocalPlayer || isThirdPerson;
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
    private void OnShotHitServer(ShotHitContext _)
    {
        if (!CharacterActionRules.CanDie(isDead))
        {
            return;
        }

        if (isSeated)
        {
            StandServer();
        }

        isDead = true;
        verticalVelocity = 0f;
    }

    private void Look()
    {
        Vector2 mouseDelta = GetMouseDelta();
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        if (playerCamera == null)
        {
            return;
        }

        if (isThirdPerson)
        {
            // In third person, we always orbit 360 degrees around the player (both seated and standing)
            // and the player transform does not rotate.
            seatedCameraYaw += mouseX;
        }
        else
        {
            // In first person, standard controls
            if (isSeated)
            {
                seatedCameraYaw = Mathf.Clamp(seatedCameraYaw + mouseX, -70f, 70f);
            }
            else
            {
                seatedCameraYaw = 0f;
                transform.Rotate(Vector3.up * mouseX);
            }
        }

        cameraPitch = Mathf.Clamp(cameraPitch - mouseY, -80f, 80f);
        
        Quaternion targetRotation = Quaternion.Euler(cameraPitch, seatedCameraYaw, 0f);
        playerCamera.transform.localRotation = targetRotation;

        if (isThirdPerson)
        {
            float maxDistance = thirdPersonDistance;
            Vector3 worldPivot = transform.TransformPoint(firstPersonCameraLocalPos);
            Vector3 targetLocalPos = firstPersonCameraLocalPos - targetRotation * Vector3.forward * maxDistance;
            Vector3 worldTarget = transform.TransformPoint(targetLocalPos);
            
            float obstacleDist = GetObstacleDistance(worldPivot, worldTarget, maxDistance);
            float finalDistance = Mathf.Max(0.2f, obstacleDist - 0.2f);
            
            playerCamera.transform.localPosition = firstPersonCameraLocalPos - targetRotation * Vector3.forward * finalDistance;
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

    private void Move()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        Vector2 moveInput = GetMoveInput();

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move = Vector3.ClampMagnitude(move, 1f);

        if (animator != null)
        {
            animator.SetFloat(SpeedParameter, move.magnitude, 0.1f, Time.deltaTime);
        }

        if (characterController.isGrounded && WasJumpPressed())
        {
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
        if (shotHitbox != null)
        {
            shotHitbox.HitReceivedServer -= OnShotHitServer;
        }

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
        if (Mouse.current != null)
        {
            return Mouse.current.delta.ReadValue() * InputSystemMouseScale;
        }
#endif
        try
        {
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }
        catch (System.Exception)
        {
            return Vector2.zero;
        }
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

    private bool WasPunchPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
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

    private float GetObstacleDistance(Vector3 worldPivot, Vector3 worldTarget, float maxDist)
    {
        Vector3 direction = worldTarget - worldPivot;
        RaycastHit[] hits = Physics.RaycastAll(worldPivot, direction.normalized, maxDist);
        float closestDist = maxDist;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.isTrigger || hit.transform.root == transform.root)
            {
                continue;
            }
            if (hit.distance < closestDist)
            {
                closestDist = hit.distance;
            }
        }
        return closestDist;
    }

    private float GetScrollInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            float val = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(val) > 0.0001f)
            {
                return Mathf.Sign(val);
            }
        }
#endif
        try
        {
            float oldVal = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(oldVal) > 0.0001f)
            {
                return Mathf.Sign(oldVal);
            }
        }
        catch (System.Exception) {}
        return 0f;
    }

    private float GetCharacterHipsOffset(CharacterId selectedCharacter)
    {
        float inspectorOffset = 0f;
        bool found = false;
        foreach (CharacterVisualDefinition visual in characterVisuals)
        {
            if (visual != null && visual.characterId == selectedCharacter)
            {
                inspectorOffset = visual.hipsOffset;
                found = true;
                break;
            }
        }

        if (found && Mathf.Abs(inspectorOffset) < 0.0001f)
        {
            switch (selectedCharacter)
            {
                case CharacterId.Wieprz:
                    return 0.22f;
                case CharacterId.Jak:
                    return 0.05f;
                case CharacterId.Malpa:
                    return 0.12f;
                case CharacterId.Karton:
                    return 0.12f;
                default:
                    return 0f;
            }
        }
        return inspectorOffset;
    }

    private bool WasKeyJustPressed(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return false;
        }
        switch (key)
        {
            case KeyCode.Alpha1:
                return Keyboard.current.digit1Key.wasPressedThisFrame;
            case KeyCode.Alpha2:
                return Keyboard.current.digit2Key.wasPressedThisFrame;
            case KeyCode.Alpha3:
                return Keyboard.current.digit3Key.wasPressedThisFrame;
            case KeyCode.Alpha4:
                return Keyboard.current.digit4Key.wasPressedThisFrame;
            default:
                return false;
        }
#else
        return Input.GetKeyDown(key);
#endif
    }
}
