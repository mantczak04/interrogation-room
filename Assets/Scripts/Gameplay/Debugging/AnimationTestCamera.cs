using Mirror;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InterrogationRoom.Gameplay.Debugging
{
    /// <summary>
    /// Local-only animation inspection camera. F5 places a detached camera at the
    /// current view position and reveals the local player model; the player keeps
    /// full control of the character (WASD, mouse, E, punch) while the camera
    /// tracks it. Arrow keys move the camera, Page Up/Down change its height.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class AnimationTestCamera : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController player;

        [Header("Camera")]
        [SerializeField, Min(0.5f)] private float flySpeed = 3f;
        [SerializeField, Min(0f)] private float lookHeight = 1.1f;

        private Camera testCamera;

        public bool IsActive => testCamera != null;

        private void Awake()
        {
            if (player == null)
            {
                player = GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            if (!isLocalPlayer || player == null)
            {
                return;
            }

            if (WasTogglePressed())
            {
                if (IsActive)
                {
                    Deactivate();
                }
                else
                {
                    Activate();
                }
            }

            if (IsActive && !PlayerController.CursorReleased)
            {
                MoveCamera();
            }
        }

        private void LateUpdate()
        {
            if (!IsActive)
            {
                return;
            }

            Vector3 focus = player.transform.position + Vector3.up * lookHeight;
            Vector3 toFocus = focus - testCamera.transform.position;
            if (toFocus.sqrMagnitude > 0.0001f)
            {
                testCamera.transform.rotation = Quaternion.LookRotation(toFocus);
            }
        }

        public override void OnStopLocalPlayer()
        {
            Deactivate();
            base.OnStopLocalPlayer();
        }

        private void OnDestroy()
        {
            if (testCamera != null)
            {
                Destroy(testCamera.gameObject);
                testCamera = null;
            }
        }

        private void Activate()
        {
            Camera sourceCamera = player.playerCamera;
            if (sourceCamera == null)
            {
                return;
            }

            var cameraObject = new GameObject("AnimationTestCamera");
            cameraObject.transform.SetPositionAndRotation(
                sourceCamera.transform.position,
                sourceCamera.transform.rotation);

            testCamera = cameraObject.AddComponent<Camera>();
            testCamera.CopyFrom(sourceCamera);
            testCamera.transform.SetPositionAndRotation(
                sourceCamera.transform.position,
                sourceCamera.transform.rotation);
            testCamera.enabled = true;

            sourceCamera.enabled = false;
            player.SetLocalModelVisible(true);
        }

        private void Deactivate()
        {
            if (!IsActive)
            {
                return;
            }

            if (player != null)
            {
                if (player.playerCamera != null)
                {
                    player.playerCamera.enabled = true;
                }

                player.SetLocalModelVisible(false);
            }

            Destroy(testCamera.gameObject);
            testCamera = null;
        }

        private void MoveCamera()
        {
            Vector3 input = GetFlyInput();
            if (input.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Transform cameraTransform = testCamera.transform;
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.ProjectOnPlane(cameraTransform.up, Vector3.up).normalized;
            }

            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 move = right * input.x + forward * input.z + Vector3.up * input.y;
            cameraTransform.position += Vector3.ClampMagnitude(move, 1f) * (flySpeed * Time.deltaTime);
        }

        private static Vector3 GetFlyInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return Vector3.zero;
            }

            Vector3 input = Vector3.zero;
            if (Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
            if (Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
            if (Keyboard.current.downArrowKey.isPressed) input.z -= 1f;
            if (Keyboard.current.upArrowKey.isPressed) input.z += 1f;
            if (Keyboard.current.pageDownKey.isPressed) input.y -= 1f;
            if (Keyboard.current.pageUpKey.isPressed) input.y += 1f;
            return input;
#else
            Vector3 input = Vector3.zero;
            if (Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) input.x += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) input.z -= 1f;
            if (Input.GetKey(KeyCode.UpArrow)) input.z += 1f;
            if (Input.GetKey(KeyCode.PageDown)) input.y -= 1f;
            if (Input.GetKey(KeyCode.PageUp)) input.y += 1f;
            return input;
#endif
        }

        private static bool WasTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F5);
#endif
        }
    }
}
