using Mirror;
using UnityEngine;
using InterrogationRoom.Gameplay.Interaction;

namespace InterrogationRoom.Gameplay.Weapons
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkWeaponPickup : NetworkBehaviour, INetworkInteractable
    {
        [SerializeField] private Transform pickupPoint;

        private bool consumed;

        public Vector3 PickupPosition => pickupPoint != null ? pickupPoint.position : transform.position;

        public Vector3 InteractionPosition => PickupPosition;

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (GetComponentInChildren<Collider>(true) == null)
            {
                Debug.LogError(
                    $"[{nameof(NetworkWeaponPickup)}] {name} requires a Collider on the pickup or one of its children.",
                    this);
            }
        }

        [Server]
        public bool TryInteractServer(NetworkIdentity interactor)
        {
            if (consumed || !NetworkServer.active || interactor == null ||
                !interactor.TryGetComponent(out PlayerWeaponController weaponController) ||
                !weaponController.TryEquipWeaponServer())
            {
                return false;
            }

            consumed = true;
            NetworkServer.Destroy(gameObject);
            return true;
        }
    }
}
