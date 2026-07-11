using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkChairSeat : NetworkBehaviour, INetworkInteractable
    {
        [Header("Optional anchors")]
        [SerializeField] private Transform seatPoint;
        [SerializeField] private Transform standPoint;

        [Header("Fallback pose")]
        [SerializeField, Min(0f)] private float seatHeight = 0.02f;
        [SerializeField, Min(0.25f)] private float standDistance = 0.75f;
        [SerializeField] private LayerMask standObstructionMask = ~0;

        [Header("Interaction")]
        [SerializeField] private string interactionPrompt = "Sit down";

        [SyncVar]
        private uint occupantNetId;

        public Vector3 SeatPosition => seatPoint != null
            ? seatPoint.position
            : transform.position + transform.up * seatHeight;

        public Quaternion SeatRotation => seatPoint != null ? seatPoint.rotation : transform.rotation;

        public Vector3 StandPosition => standPoint != null
            ? standPoint.position
            : SeatPosition + SeatRotation * Vector3.forward * standDistance;

        /// <summary>
        /// Picks a free spot around the seat to place the standing player, so
        /// chairs tucked against tables or walls never eject the player into
        /// furniture. Falls back to the plain StandPosition when every side is
        /// blocked.
        /// </summary>
        public Vector3 GetStandPositionServer()
        {
            Vector3 seat = SeatPosition;
            Quaternion facing = SeatRotation;
            Vector3[] candidates =
            {
                StandPosition,
                seat + facing * Vector3.left * standDistance,
                seat + facing * Vector3.right * standDistance,
                seat + facing * Vector3.back * standDistance
            };

            foreach (Vector3 candidate in candidates)
            {
                if (IsStandSpotFree(candidate))
                {
                    return candidate;
                }
            }

            return StandPosition;
        }

        private bool IsStandSpotFree(Vector3 groundPosition)
        {
            // The capsule starts 0.1 above the ground so low furniture (e.g. a
            // sunken table top) blocks the spot while the floor itself does not.
            Collider[] overlaps = Physics.OverlapCapsule(
                groundPosition + Vector3.up * 0.4f,
                groundPosition + Vector3.up * 1.4f,
                0.3f,
                standObstructionMask,
                QueryTriggerInteraction.Ignore);

            foreach (Collider overlap in overlaps)
            {
                if (!overlap.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            if (!Physics.Raycast(
                    groundPosition + Vector3.up * 0.35f,
                    Vector3.down,
                    out RaycastHit floorHit,
                    0.7f,
                    standObstructionMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return floorHit.point.y <= groundPosition.y + 0.09f;
        }

        public Vector3 InteractionPosition => SeatPosition + transform.up * 0.45f;

        public string InteractionPrompt => interactionPrompt;

        private void Awake()
        {
            if (GetComponentInChildren<Collider>(true) == null)
            {
                Debug.LogError(
                    $"[{nameof(NetworkChairSeat)}] {name} requires a Collider on the chair or one of its children.",
                    this);
            }
        }

        public bool CanInteract(NetworkIdentity interactor)
        {
            return occupantNetId == 0 &&
                   interactor != null &&
                   interactor.TryGetComponent(out PlayerController playerController) &&
                   !playerController.IsDead &&
                   !playerController.IsSeated;
        }

        public bool TryInteractServer(NetworkIdentity interactor)
        {
            return NetworkServer.active &&
                   CanInteract(interactor) &&
                   interactor.TryGetComponent(out PlayerController playerController) &&
                   playerController.TrySitServer(this);
        }

        public bool TryOccupyServer(NetworkIdentity interactor)
        {
            if (!NetworkServer.active || occupantNetId != 0 || interactor == null || interactor.netId == 0)
            {
                return false;
            }

            occupantNetId = interactor.netId;
            return true;
        }

        public void ReleaseServer(NetworkIdentity interactor)
        {
            if (interactor != null && occupantNetId == interactor.netId)
            {
                occupantNetId = 0;
            }
        }
    }
}
