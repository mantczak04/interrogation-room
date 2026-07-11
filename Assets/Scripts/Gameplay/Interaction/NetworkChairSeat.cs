using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    public sealed class NetworkChairSeat : MonoBehaviour, INetworkInteractable
    {
        [Header("Optional anchors")]
        [SerializeField] private Transform seatPoint;
        [SerializeField] private Transform standPoint;

        [Header("Fallback pose")]
        [SerializeField, Min(0f)] private float seatHeight = 0.02f;
        [SerializeField, Min(0.25f)] private float standDistance = 0.75f;

        private uint occupantNetId;

        public Vector3 SeatPosition => seatPoint != null
            ? seatPoint.position
            : transform.position + transform.up * seatHeight;

        public Quaternion SeatRotation => seatPoint != null ? seatPoint.rotation : transform.rotation;

        public Vector3 StandPosition => standPoint != null
            ? standPoint.position
            : transform.position + transform.forward * standDistance;

        public Vector3 InteractionPosition => SeatPosition + transform.up * 0.45f;

        private void Awake()
        {
            if (GetComponentInChildren<Collider>(true) == null)
            {
                Debug.LogError(
                    $"[{nameof(NetworkChairSeat)}] {name} requires a Collider on the chair or one of its children.",
                    this);
            }
        }

        public bool TryInteractServer(NetworkIdentity interactor)
        {
            return NetworkServer.active &&
                   occupantNetId == 0 &&
                   interactor != null &&
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
