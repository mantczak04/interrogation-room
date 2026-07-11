using InterrogationRoom.Gameplay.Interaction;
using Mirror;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkIdentity))]
public class PlayerController : NetworkBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 2f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;
    public Camera playerCamera;

    private CharacterController characterController;
    private Animator animator;
    private AudioListener audioListener;
    private Renderer[] playerRenderers;
    private float verticalVelocity;
    private float cameraPitch;
    private float seatedCameraYaw;
    private NetworkChairSeat activeSeat;

    [SyncVar(hook = nameof(OnSeatedChanged))]
    private bool isSeated;

    private const float InputSystemMouseScale = 0.1f;
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int LookPitchParameter = Animator.StringToHash("LookPitch");

    public static bool CursorReleased { get; private set; } = true;

    public bool IsSeated => isSeated;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerRenderers = GetComponentsInChildren<Renderer>(true);

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
        }

        if (playerCamera != null)
        {
            audioListener = playerCamera.GetComponent<AudioListener>();
        }
    }

    public override void OnStartClient()
    {
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

        foreach (Renderer playerRenderer in playerRenderers)
        {
            if (playerRenderer != null)
            {
                playerRenderer.enabled = !local;
            }
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

        if (WasCursorTogglePressed())
        {
            SetCursorReleased(!CursorReleased);
        }

        if (CursorReleased)
        {
            return;
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
        if (!isLocalPlayer || !isSeated)
        {
            return false;
        }

        CmdStand();
        return true;
    }

    [Server]
    public bool TrySitServer(NetworkChairSeat seat)
    {
        if (!NetworkServer.active || isSeated || seat == null || !seat.TryOccupyServer(netIdentity))
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

    private void Look()
    {
        Vector2 mouseDelta = GetMouseDelta();
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        if (isSeated)
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
        playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, seatedCameraYaw, 0f);

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

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !animator.isHuman)
        {
            return;
        }

        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (head == null)
        {
            return;
        }

        ApplySeatedIk();

        float lookPitch = animator.GetFloat(LookPitchParameter);
        Vector3 lookDirection = Quaternion.AngleAxis(lookPitch, transform.right) * transform.forward;

        animator.SetLookAtWeight(1f, 0.2f, 0.85f, 0.35f, 0.5f);
        animator.SetLookAtPosition(head.position + lookDirection * 10f);
    }

    private void ApplySeatedIk()
    {
        float weight = isSeated ? 1f : 0f;
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, weight);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, weight);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, weight);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, weight);

        if (!isSeated)
        {
            return;
        }

        animator.bodyPosition = transform.TransformPoint(new Vector3(0f, 0.58f, 0f));
        Quaternion footRotation = transform.rotation;
        animator.SetIKPosition(
            AvatarIKGoal.LeftFoot,
            transform.TransformPoint(new Vector3(-0.16f, 0.05f, 0.42f)));
        animator.SetIKRotation(AvatarIKGoal.LeftFoot, footRotation);
        animator.SetIKPosition(
            AvatarIKGoal.RightFoot,
            transform.TransformPoint(new Vector3(0.16f, 0.05f, 0.42f)));
        animator.SetIKRotation(AvatarIKGoal.RightFoot, footRotation);
    }

    public override void OnStopServer()
    {
        if (activeSeat != null)
        {
            activeSeat.ReleaseServer(netIdentity);
            activeSeat = null;
        }

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
}
