using System.Collections.Generic;
using InterrogationRoom.Gameplay.Interaction;
using InterrogationRoom.Items;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Items
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class NetworkCarryableItem : NetworkBehaviour, INetworkInteractable
    {
        private static readonly HashSet<NetworkCarryableItem> Instances =
            new HashSet<NetworkCarryableItem>();

        [Header("Identity")]
        [SerializeField] private string itemId = "significant-item";
        [SerializeField] private string displayName = "Istotny przedmiot";
        [SerializeField] private string pickupPrompt = "Podnieś";
        [SerializeField] private Transform interactionPoint;

        [Header("Carry presentation")]
        [SerializeField] private string[] carryAnchorNames =
        {
            "ItemCarryAnchor", "RightHand", "mixamorig:RightHand", "Spine2", "mixamorig:Spine2"
        };
        [SerializeField] private Vector3 fallbackCarryOffset = new Vector3(0.32f, 1.05f, 0.18f);
        [SerializeField] private Vector3 carriedLocalPosition;
        [SerializeField] private Vector3 carriedLocalEulerAngles;

        [Header("Safe recovery")]
        [SerializeField] private Transform homeAnchor;
        [SerializeField, Min(1f)] private float droppedReturnTimeout = 45f;
        [SerializeField] private float outOfBoundsY = -20f;

        [SyncVar(hook = nameof(OnCarrierNetIdChanged))]
        private uint carrierNetId;

        [SyncVar(hook = nameof(OnStateChanged))]
        private CarryItemState state = CarryItemState.AtHome;

        [SyncVar(hook = nameof(OnPlacedSlotNetIdChanged))]
        private uint placedSlotNetId;

        private Rigidbody itemRigidbody;
        private Collider[] itemColliders;
        private NetworkIdentity activeCarrierServer;
        private NetworkItemSlot activeSlotServer;
        private Transform homeParent;
        private Vector3 homeLocalPosition;
        private Quaternion homeLocalRotation;
        private Vector3 homeWorldPosition;
        private Quaternion homeWorldRotation;
        private double lastDroppedAt = -1d;

        public string ItemId => itemId ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public CarryItemState State => state;
        public uint CarrierNetId => carrierNetId;
        public uint PlacedSlotNetId => placedSlotNetId;
        public bool IsCarried => state == CarryItemState.Carried;
        public bool IsPlaced => state == CarryItemState.Placed;
        public Vector3 InteractionPosition => interactionPoint != null
            ? interactionPoint.position
            : transform.position;
        public string InteractionPrompt => pickupPrompt;

        private void Awake()
        {
            itemRigidbody = GetComponent<Rigidbody>();
            itemColliders = GetComponentsInChildren<Collider>(true);
            CaptureHomePose();
        }

        private void OnEnable()
        {
            Instances.Add(this);
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            CaptureHomePose();
            ReturnHomeServer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            RefreshPresentation();
        }

        private void Update()
        {
            if (isServer)
            {
                if (state == CarryItemState.Carried && !IsCarrierAvailableServer())
                    DropServer();
                else
                    EvaluateRecoveryServer(NetworkTime.time);
            }
        }

        private void LateUpdate()
        {
            if (state == CarryItemState.Carried || state == CarryItemState.Placed)
                RefreshAttachment();
        }

        public bool CanInteract(NetworkIdentity interactor)
        {
            bool actorCanAct = IsActorAvailable(interactor);
            bool actorAlreadyCarriesItem = FindCarriedBy(interactor) != null;
            return CarryItemRules.CanPickup(state, actorCanAct, actorAlreadyCarriesItem);
        }

        [Server]
        public bool TryInteractServer(NetworkIdentity interactor)
        {
            return TryPickupServer(interactor);
        }

        [Server]
        public bool TryPickupServer(NetworkIdentity interactor)
        {
            if (!NetworkServer.active || !CanInteract(interactor))
                return false;

            ReleaseSlotServer();
            activeCarrierServer = interactor;
            carrierNetId = interactor.netId;
            placedSlotNetId = 0;
            state = CarryItemState.Carried;
            lastDroppedAt = -1d;
            RefreshPresentation();
            return true;
        }

        [Server]
        public bool DropServer()
        {
            if (!NetworkServer.active || state != CarryItemState.Carried)
                return false;

            transform.SetParent(null, true);
            activeCarrierServer = null;
            carrierNetId = 0;
            placedSlotNetId = 0;
            state = CarryItemState.Dropped;
            lastDroppedAt = NetworkTime.time;
            RefreshPresentation();
            return true;
        }

        [Server]
        public bool TryPlaceServer(NetworkItemSlot slot, NetworkIdentity actor)
        {
            if (!NetworkServer.active || slot == null || actor == null ||
                state != CarryItemState.Carried || !IsCarriedBy(actor) ||
                !slot.CanAccept(this))
            {
                return false;
            }

            transform.SetParent(null, true);
            activeCarrierServer = null;
            carrierNetId = 0;
            activeSlotServer = slot;
            NetworkIdentity slotIdentity = slot.GetComponent<NetworkIdentity>();
            placedSlotNetId = slotIdentity != null ? slotIdentity.netId : 0;
            state = CarryItemState.Placed;
            lastDroppedAt = -1d;
            slot.CommitPlacementServer(this, actor);
            RefreshPresentation();
            return true;
        }

        [Server]
        public bool ReturnHomeServer()
        {
            if (!NetworkServer.active)
                return false;

            ReleaseSlotServer();
            activeCarrierServer = null;
            carrierNetId = 0;
            placedSlotNetId = 0;
            state = CarryItemState.AtHome;
            lastDroppedAt = -1d;
            RestoreHomePose();
            RefreshPresentation();
            return true;
        }

        [Server]
        public bool EvaluateRecoveryServer(double now)
        {
            if (!NetworkServer.active || !CarryItemRules.ShouldReturnHome(
                    state,
                    now,
                    lastDroppedAt,
                    droppedReturnTimeout,
                    transform.position.y,
                    outOfBoundsY))
            {
                return false;
            }

            ReturnHomeServer();
            return true;
        }

        [Server]
        public void ResetInteractionStateServer()
        {
            ReturnHomeServer();
        }

        public bool IsCarriedBy(NetworkIdentity actor)
        {
            if (actor == null || state != CarryItemState.Carried)
                return false;
            if (ReferenceEquals(activeCarrierServer, actor))
                return true;
            return actor.netId != 0 && actor.netId == carrierNetId;
        }

        public static NetworkCarryableItem FindCarriedBy(NetworkIdentity actor)
        {
            if (actor == null)
                return null;

            foreach (NetworkCarryableItem item in Instances)
            {
                if (item != null && item.IsCarriedBy(actor))
                    return item;
            }

            return null;
        }

        public static NetworkCarryableItem FindCarriedBy(uint actorNetId)
        {
            if (actorNetId == 0)
                return null;

            foreach (NetworkCarryableItem item in Instances)
            {
                if (item != null && item.state == CarryItemState.Carried &&
                    item.carrierNetId == actorNetId)
                {
                    return item;
                }
            }

            return null;
        }

        private void CaptureHomePose()
        {
            homeParent = homeAnchor != null ? homeAnchor : transform.parent;
            if (homeAnchor != null)
            {
                homeLocalPosition = Vector3.zero;
                homeLocalRotation = Quaternion.identity;
                homeWorldPosition = homeAnchor.position;
                homeWorldRotation = homeAnchor.rotation;
            }
            else
            {
                homeLocalPosition = transform.localPosition;
                homeLocalRotation = transform.localRotation;
                homeWorldPosition = transform.position;
                homeWorldRotation = transform.rotation;
            }
        }

        private void RestoreHomePose()
        {
            if (homeParent != null)
            {
                transform.SetParent(homeParent, false);
                transform.localPosition = homeLocalPosition;
                transform.localRotation = homeLocalRotation;
            }
            else
            {
                transform.SetParent(null, true);
                transform.SetPositionAndRotation(homeWorldPosition, homeWorldRotation);
            }
        }

        private bool IsCarrierAvailableServer()
        {
            NetworkIdentity carrier = ResolveCarrierIdentity();
            if (carrier == null || !carrier.isActiveAndEnabled)
                return false;
            if (carrier.netId != 0 && !NetworkServer.spawned.ContainsKey(carrier.netId))
                return false;

            PlayerController controller = carrier.GetComponent<PlayerController>();
            return controller == null || !controller.IsDead;
        }

        private static bool IsActorAvailable(NetworkIdentity actor)
        {
            if (actor == null || !actor.isActiveAndEnabled)
                return false;

            PlayerController controller = actor.GetComponent<PlayerController>();
            return controller == null || (!controller.IsDead && !controller.IsSeated);
        }

        private NetworkIdentity ResolveCarrierIdentity()
        {
            if (activeCarrierServer != null)
                return activeCarrierServer;
            if (carrierNetId == 0)
                return null;
            if (NetworkServer.active && NetworkServer.spawned.TryGetValue(carrierNetId, out NetworkIdentity serverIdentity))
                return serverIdentity;
            return NetworkClient.spawned.TryGetValue(carrierNetId, out NetworkIdentity clientIdentity)
                ? clientIdentity
                : null;
        }

        private void ReleaseSlotServer()
        {
            NetworkItemSlot slot = activeSlotServer;
            if (slot == null && placedSlotNetId != 0)
                slot = NetworkItemSlot.FindByNetId(placedSlotNetId);
            slot?.ReleasePlacementServer(this);
            activeSlotServer = null;
        }

        private void RefreshPresentation()
        {
            if (state != CarryItemState.Carried && state != CarryItemState.Placed && transform.parent != homeParent)
                transform.SetParent(null, true);

            bool carried = state == CarryItemState.Carried;
            bool placed = state == CarryItemState.Placed;
            bool anchored = state != CarryItemState.Dropped;
            if (itemRigidbody != null)
            {
                itemRigidbody.isKinematic = anchored || !NetworkServer.active;
                itemRigidbody.useGravity = !anchored;
            }

            if (itemColliders != null)
            {
                foreach (Collider itemCollider in itemColliders)
                {
                    if (itemCollider != null)
                        itemCollider.enabled = !carried;
                }
            }

            if (carried || placed)
                RefreshAttachment();
        }

        private void RefreshAttachment()
        {
            if (state == CarryItemState.Carried)
            {
                NetworkIdentity carrier = ResolveCarrierIdentity();
                if (carrier == null)
                    return;

                Transform anchor = FindCarryAnchor(carrier.transform);
                transform.SetParent(null, true);
                if (anchor != null)
                {
                    transform.SetPositionAndRotation(
                        anchor.TransformPoint(carriedLocalPosition),
                        anchor.rotation * Quaternion.Euler(carriedLocalEulerAngles));
                }
                else
                {
                    transform.SetPositionAndRotation(
                        carrier.transform.TransformPoint(fallbackCarryOffset),
                        carrier.transform.rotation * Quaternion.Euler(carriedLocalEulerAngles));
                }
                return;
            }

            if (state == CarryItemState.Placed)
            {
                NetworkItemSlot slot = activeSlotServer;
                if (slot == null && placedSlotNetId != 0)
                    slot = NetworkItemSlot.FindByNetId(placedSlotNetId);
                if (slot == null)
                    return;

                Transform anchor = slot.PlacementAnchor;
                transform.SetParent(null, true);
                transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            }
        }

        private Transform FindCarryAnchor(Transform root)
        {
            if (root == null || carryAnchorNames == null)
                return null;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                for (int index = 0; index < carryAnchorNames.Length; index++)
                {
                    if (!string.IsNullOrWhiteSpace(carryAnchorNames[index]) &&
                        string.Equals(child.name, carryAnchorNames[index], System.StringComparison.OrdinalIgnoreCase))
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        private void OnCarrierNetIdChanged(uint _, uint __)
        {
            RefreshPresentation();
        }

        private void OnStateChanged(CarryItemState _, CarryItemState __)
        {
            RefreshPresentation();
        }

        private void OnPlacedSlotNetIdChanged(uint _, uint __)
        {
            RefreshPresentation();
        }

        public override void OnStopServer()
        {
            activeCarrierServer = null;
            activeSlotServer = null;
            base.OnStopServer();
        }
    }
}
