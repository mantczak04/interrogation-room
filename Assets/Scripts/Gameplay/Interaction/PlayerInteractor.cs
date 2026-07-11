using Mirror;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InterrogationRoom.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class PlayerInteractor : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera interactionCamera;

        [Header("Interaction")]
        [SerializeField, Min(0.5f)] private float interactionRange = 2.5f;
        [SerializeField, Min(0f)] private float serverRangeTolerance = 0.25f;
        [SerializeField, Min(0f)] private float serverViewHeight = 1.6f;
        [SerializeField] private LayerMask interactionMask = ~0;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            if (interactionCamera == null)
            {
                Debug.LogError(
                    $"[{nameof(PlayerInteractor)}] Assign the local player's interaction Camera.",
                    this);
            }
        }

        private void Update()
        {
            if (!isLocalPlayer || interactionCamera == null || !WasInteractPressed())
            {
                return;
            }

            if (TryGetComponent(out PlayerController playerController) && playerController.TryRequestStand())
            {
                return;
            }

            TryRequestInteraction();
        }

        private void TryRequestInteraction()
        {
            float cameraOffset = Vector3.Distance(interactionCamera.transform.position, transform.position);
            float raycastRange = interactionRange + serverRangeTolerance + cameraOffset;
            if (!TryGetFirstNonSelfHit(
                    interactionCamera.transform.position,
                    interactionCamera.transform.forward,
                    raycastRange,
                    out RaycastHit hit))
            {
                return;
            }

            if (!TryGetInteractable(hit.collider.transform, out _))
            {
                return;
            }

            CmdTryInteract(interactionCamera.transform.forward);
        }

        [Command]
        private void CmdTryInteract(Vector3 requestedDirection)
        {
            if (!IsFinite(requestedDirection) || requestedDirection.sqrMagnitude < 0.5f)
            {
                return;
            }

            float allowedDistance = interactionRange + serverRangeTolerance;
            Vector3 origin = transform.TransformPoint(Vector3.up * serverViewHeight);
            if (!TryGetFirstNonSelfHit(
                    origin,
                    requestedDirection.normalized,
                    allowedDistance,
                    out RaycastHit hit) ||
                !TryGetInteractable(hit.collider.transform, out INetworkInteractable interactable))
            {
                return;
            }

            Vector3 interactionPosition = interactable.InteractionPosition;
            if ((interactionPosition - transform.position).sqrMagnitude > allowedDistance * allowedDistance)
            {
                return;
            }

            interactable.TryInteractServer(netIdentity);
        }

        private bool TryGetFirstNonSelfHit(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            out RaycastHit closestHit)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                direction,
                maxDistance,
                interactionMask,
                QueryTriggerInteraction.Collide);

            closestHit = default;
            float closestDistance = float.PositiveInfinity;
            bool foundHit = false;

            foreach (RaycastHit hit in hits)
            {
                Transform hitTransform = hit.collider.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform) ||
                    hit.distance >= closestDistance)
                {
                    continue;
                }

                closestHit = hit;
                closestDistance = hit.distance;
                foundHit = true;
            }

            return foundHit;
        }

        private static bool TryGetInteractable(
            Transform hitTransform,
            out INetworkInteractable interactable)
        {
            for (Transform current = hitTransform; current != null; current = current.parent)
            {
                foreach (MonoBehaviour component in current.GetComponents<MonoBehaviour>())
                {
                    if (component is INetworkInteractable candidate)
                    {
                        interactable = candidate;
                        return true;
                    }
                }
            }

            interactable = null;
            return false;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool WasInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }
    }
}
