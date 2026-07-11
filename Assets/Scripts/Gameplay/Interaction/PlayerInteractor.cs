using System;
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

        private NetworkIdentity hoveredIdentity;
        private INetworkInteractable hoveredInteractable;

        public NetworkIdentity HoveredIdentity => hoveredIdentity;

        public bool HasHoveredTarget => hoveredIdentity != null && hoveredInteractable != null;

        public event Action<NetworkIdentity> HoveredTargetChanged;

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

        private void OnDisable()
        {
            SetHoveredTarget(null, null);
        }

        private void Update()
        {
            if (!isLocalPlayer || interactionCamera == null)
            {
                return;
            }

            if (PlayerController.CursorReleased)
            {
                SetHoveredTarget(null, null);
                return;
            }

            RefreshHoveredTarget();

            if (WasInteractPressed() && HasHoveredTarget)
            {
                CmdTryInteract(hoveredIdentity.netId);
            }
        }

        private void RefreshHoveredTarget()
        {
            float cameraOffset = Vector3.Distance(interactionCamera.transform.position, transform.position);
            float raycastRange = interactionRange + serverRangeTolerance + cameraOffset;
            if (!TryGetFirstNonSelfHit(
                    interactionCamera.transform.position,
                    interactionCamera.transform.forward,
                    raycastRange,
                    out RaycastHit hit))
            {
                SetHoveredTarget(null, null);
                return;
            }

            NetworkIdentity targetIdentity = hit.collider.GetComponentInParent<NetworkIdentity>();
            if (targetIdentity == null || targetIdentity.netId == 0 ||
                !TryGetInteractable(targetIdentity, out INetworkInteractable interactable))
            {
                SetHoveredTarget(null, null);
                return;
            }

            SetHoveredTarget(targetIdentity, interactable);
        }

        private void SetHoveredTarget(NetworkIdentity identity, INetworkInteractable interactable)
        {
            if (hoveredIdentity == identity)
            {
                hoveredInteractable = interactable;
                return;
            }

            hoveredIdentity = identity;
            hoveredInteractable = interactable;
            HoveredTargetChanged?.Invoke(identity);
        }

        [Command]
        private void CmdTryInteract(uint targetNetId)
        {
            if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity) ||
                targetIdentity == netIdentity ||
                !TryGetInteractable(targetIdentity, out INetworkInteractable interactable))
            {
                return;
            }

            float allowedDistance = interactionRange + serverRangeTolerance;
            Vector3 interactionPosition = interactable.InteractionPosition;
            if ((interactionPosition - transform.position).sqrMagnitude > allowedDistance * allowedDistance ||
                !HasServerLineOfSightTo(targetIdentity, interactionPosition))
            {
                return;
            }

            interactable.TryInteractServer(netIdentity);
        }

        private bool HasServerLineOfSightTo(NetworkIdentity targetIdentity, Vector3 interactionPosition)
        {
            Vector3 origin = transform.TransformPoint(Vector3.up * serverViewHeight);
            Vector3 delta = interactionPosition - origin;
            if (delta.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            if (!TryGetFirstNonSelfHit(origin, delta.normalized, delta.magnitude + 0.05f, out RaycastHit hit))
            {
                return false;
            }

            Transform hitTransform = hit.collider.transform;
            return hitTransform == targetIdentity.transform || hitTransform.IsChildOf(targetIdentity.transform);
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
            NetworkIdentity targetIdentity,
            out INetworkInteractable interactable)
        {
            foreach (MonoBehaviour component in targetIdentity.GetComponents<MonoBehaviour>())
            {
                if (component is INetworkInteractable candidate)
                {
                    interactable = candidate;
                    return true;
                }
            }

            interactable = null;
            return false;
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
