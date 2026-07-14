using System.Collections;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkDoor : NetworkBehaviour, INetworkInteractable, IRoomPortalState
    {
        [Header("Portal identity")]
        [SerializeField] private string roomAId = "room-a";
        [SerializeField] private string roomBId = "room-b";

        [Header("Stable gameplay references")]
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform doorLeaf;
        [SerializeField] private Collider blockingCollider;

        [Header("Presentation")]
        [SerializeField] private Vector3 openLocalEulerAngles = new Vector3(0f, 90f, 0f);
        [SerializeField, Min(0f)] private float animationDuration = 0.2f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openClip;
        [SerializeField] private AudioClip closeClip;

        [Header("Interaction")]
        [SerializeField] private string actionId = "door-toggle";
        [SerializeField] private string closedPrompt = "Open door";
        [SerializeField] private string openPrompt = "Close door";
        [SerializeField, Min(0f)] private float toggleCooldown = 0.3f;

        [SyncVar(hook = nameof(OnOpenStateChanged))]
        private bool isOpen;

        private Quaternion closedLocalRotation;
        private double nextToggleAt;
        private Coroutine animationRoutine;
        private bool lastPresentedOpen;
        private bool hasPresentedState;

        public string RoomAId => roomAId ?? string.Empty;
        public string RoomBId => roomBId ?? string.Empty;
        public string ActionId => actionId ?? string.Empty;
        public bool IsOpen => isOpen;
        public Vector3 InteractionPosition => interactionPoint != null
            ? interactionPoint.position
            : transform.position + transform.up;
        public string InteractionPrompt => isOpen ? openPrompt : closedPrompt;

        private void Awake()
        {
            ResolveReferences();
            if (doorLeaf != null)
                closedLocalRotation = doorLeaf.localRotation;
            lastPresentedOpen = isOpen;
            hasPresentedState = true;
        }

        private void OnEnable()
        {
            RoomPortalRegistry.Register(this);
        }

        private void OnDisable()
        {
            RoomPortalRegistry.Unregister(this);
            if (animationRoutine != null)
                StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            ApplyDoorState(isOpen, false, false);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyDoorState(isOpen, false, false);
        }

        public bool CanInteract(NetworkIdentity interactor)
        {
            if (interactor == null)
                return false;

            return !interactor.TryGetComponent(out PlayerController playerController) ||
                   !playerController.IsDead && !playerController.IsSeated;
        }

        public bool TryInteractServer(NetworkIdentity interactor)
        {
            if (!NetworkServer.active ||
                !CanInteract(interactor) ||
                NetworkTime.time < nextToggleAt)
            {
                return false;
            }

            nextToggleAt = NetworkTime.time + toggleCooldown;
            return SetOpenServer(!isOpen);
        }

        [Server]
        public bool SetOpenServer(bool open)
        {
            if (isOpen == open)
                return false;

            isOpen = open;
            ApplyDoorState(open, true, true);
            return true;
        }

        private void OnOpenStateChanged(bool _, bool open)
        {
            ApplyDoorState(open, true, true);
        }

        private void ApplyDoorState(bool open, bool animate, bool playSound)
        {
            ResolveReferences();
            bool audibleStateChanged = !hasPresentedState || lastPresentedOpen != open;
            if (blockingCollider != null)
                blockingCollider.enabled = !open;

            if (doorLeaf != null)
            {
                Quaternion targetRotation = open
                    ? closedLocalRotation * Quaternion.Euler(openLocalEulerAngles)
                    : closedLocalRotation;

                if (animationRoutine != null)
                    StopCoroutine(animationRoutine);

                if (animate && animationDuration > 0f && isActiveAndEnabled)
                    animationRoutine = StartCoroutine(AnimateDoor(targetRotation));
                else
                    doorLeaf.localRotation = targetRotation;
            }

            if (playSound && audibleStateChanged && audioSource != null)
            {
                AudioClip clip = open ? openClip : closeClip;
                if (clip != null)
                    audioSource.PlayOneShot(clip);
            }

            lastPresentedOpen = open;
            hasPresentedState = true;
        }

        private IEnumerator AnimateDoor(Quaternion targetRotation)
        {
            Quaternion startRotation = doorLeaf.localRotation;
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                doorLeaf.localRotation = Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    Mathf.Clamp01(elapsed / animationDuration));
                yield return null;
            }

            doorLeaf.localRotation = targetRotation;
            animationRoutine = null;
        }

        private void ResolveReferences()
        {
            if (doorLeaf == null)
                doorLeaf = transform.Find("DoorLeaf");
            if (visualRoot == null && doorLeaf != null)
                visualRoot = doorLeaf.Find("VisualRoot");

            // Backward-compatible fallback for early placeholder layouts.
            if (visualRoot == null)
                visualRoot = transform.Find("VisualRoot");
            if (doorLeaf == null && visualRoot != null)
                doorLeaf = visualRoot.Find("DoorLeaf");
            if (blockingCollider == null && doorLeaf != null)
                blockingCollider = doorLeaf.GetComponent<Collider>();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
