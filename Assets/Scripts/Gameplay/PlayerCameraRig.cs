using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InterrogationRoom.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        private const float InputSystemMouseScale = 0.1f;
        private readonly RaycastHit[] obstacleHitBuffer = new RaycastHit[32];

        private Camera playerCamera;
        private PlayerAnimationDriver animationDriver;
        private Vector3 firstPersonLocalPosition;
        private float pitch;
        private float yaw;
        private float minZoomDistance;
        private float maxZoomDistance;
        private float zoomSensitivity;
        private float thirdPersonDistance = 2.5f;

        public bool IsThirdPerson { get; private set; }
        public float Pitch => pitch;
        public Camera Camera => playerCamera;

        public void Configure(
            Camera camera,
            PlayerAnimationDriver driver,
            float minimumZoomDistance,
            float maximumZoomDistance,
            float zoomStep)
        {
            playerCamera = camera;
            animationDriver = driver;
            minZoomDistance = minimumZoomDistance;
            maxZoomDistance = maximumZoomDistance;
            zoomSensitivity = zoomStep;

            if (playerCamera == null)
                return;

            firstPersonLocalPosition = playerCamera.transform.localPosition;
            pitch = playerCamera.transform.localEulerAngles.x;
            if (pitch > 180f)
                pitch -= 360f;
        }

        public bool Tick(bool isSeated, float mouseSensitivity)
        {
            bool previousThirdPerson = IsThirdPerson;
            if (WasCameraTogglePressed())
                IsThirdPerson = !IsThirdPerson;

            if (IsThirdPerson)
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

            ApplyLook(isSeated, mouseSensitivity);
            return previousThirdPerson != IsThirdPerson;
        }

        public float GetBodyRelativePitch(bool isSeated)
        {
            if (isSeated && playerCamera != null)
                return PlayerAnimationDriver.GetPitchFromDirection(
                    playerCamera.transform.forward,
                    transform.right);

            return pitch;
        }

        private void ApplyLook(bool isSeated, float mouseSensitivity)
        {
            Vector2 mouseDelta = GetMouseDelta();
            float mouseX = mouseDelta.x * mouseSensitivity;
            float mouseY = mouseDelta.y * mouseSensitivity;

            if (IsThirdPerson)
                yaw += mouseX;
            else if (isSeated)
                yaw = Mathf.Clamp(yaw + mouseX, -70f, 70f);
            else
            {
                yaw = 0f;
                transform.Rotate(Vector3.up * mouseX);
            }

            if (playerCamera == null)
                return;

            pitch = Mathf.Clamp(pitch - mouseY, -80f, 80f);
            Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
            playerCamera.transform.localRotation = cameraRotation;

            if (IsThirdPerson)
            {
                Vector3 worldPivot = transform.TransformPoint(firstPersonLocalPosition);
                Vector3 orbitOffset = cameraRotation * Vector3.forward * thirdPersonDistance;
                Vector3 worldTarget = transform.TransformPoint(firstPersonLocalPosition - orbitOffset);
                float obstacleDistance = GetCameraObstacleDistance(
                    worldPivot,
                    worldTarget,
                    thirdPersonDistance);
                float finalDistance = Mathf.Max(0.2f, obstacleDistance - 0.2f);
                playerCamera.transform.localPosition =
                    firstPersonLocalPosition - cameraRotation * Vector3.forward * finalDistance;
            }
            else
            {
                playerCamera.transform.localPosition = firstPersonLocalPosition;
            }

            animationDriver?.SetLookPitch(pitch);
        }

        private float GetCameraObstacleDistance(
            Vector3 worldPivot,
            Vector3 worldTarget,
            float maxDistance)
        {
            Vector3 direction = worldTarget - worldPivot;
            if (direction.sqrMagnitude < 0.0001f)
                return maxDistance;

            float closestDistance = maxDistance;
            int hitCount = Physics.RaycastNonAlloc(
                worldPivot,
                direction.normalized,
                obstacleHitBuffer,
                maxDistance);
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = obstacleHitBuffer[index];
                if (!hit.collider.isTrigger &&
                    hit.transform.root != transform.root &&
                    hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                }
            }

            return closestDistance;
        }

        public static float ResolveClosestObstacleDistance(
            float maxDistance,
            IReadOnlyList<float> validHitDistances)
        {
            float closestDistance = maxDistance;
            if (validHitDistances == null)
                return closestDistance;

            for (int index = 0; index < validHitDistances.Count; index++)
            {
                float distance = validHitDistances[index];
                if (distance >= 0f && distance < closestDistance)
                    closestDistance = distance;
            }

            return closestDistance;
        }

        private static Vector2 GetMouseDelta()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current == null
                ? Vector2.zero
                : Mouse.current.delta.ReadValue() * InputSystemMouseScale;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
        }

        private static bool WasCameraTogglePressed()
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
                return 0f;

            float scroll = Mouse.current.scroll.ReadValue().y;
            return Mathf.Abs(scroll) > 0.001f ? Mathf.Sign(scroll) : 0f;
#else
            return Input.GetAxis("Mouse ScrollWheel");
#endif
        }
    }
}
