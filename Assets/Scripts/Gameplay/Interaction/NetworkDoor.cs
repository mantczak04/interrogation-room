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
        [SerializeField] private Vector3 hingeLocalOffset;
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
        private Vector3 closedLocalPosition;
        private Vector3 resolvedHingeLocalOffset;
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
            SeparateFlatSceneDoorLeaf();
            if (doorLeaf != null)
            {
                closedLocalRotation = doorLeaf.localRotation;
                closedLocalPosition = doorLeaf.localPosition;
                resolvedHingeLocalOffset = ResolveHingeLocalOffset();
            }
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
            // The leaf collider rotates out of the doorway with the visual. Keeping it enabled
            // also keeps scene-authored doors discoverable for the next interaction.
            if (blockingCollider != null)
                blockingCollider.enabled = true;

            if (doorLeaf != null)
            {
                Quaternion relativeRotation = open
                    ? ResolveOpenRelativeRotation()
                    : Quaternion.identity;
                Quaternion targetRotation = closedLocalRotation * relativeRotation;
                Vector3 scaledHingeOffset = Vector3.Scale(doorLeaf.localScale, resolvedHingeLocalOffset);
                Vector3 hingeInParent = closedLocalPosition + closedLocalRotation * scaledHingeOffset;
                Vector3 targetPosition = hingeInParent - targetRotation * scaledHingeOffset;

                if (animationRoutine != null)
                    StopCoroutine(animationRoutine);

                if (animate && animationDuration > 0f && isActiveAndEnabled)
                    animationRoutine = StartCoroutine(AnimateDoor(targetPosition, targetRotation));
                else
                {
                    doorLeaf.localPosition = targetPosition;
                    doorLeaf.localRotation = targetRotation;
                }
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

        private Quaternion ResolveOpenRelativeRotation()
        {
            Quaternion configuredRotation = Quaternion.Euler(openLocalEulerAngles);
            string roomId = string.Equals(roomAId, "korytarz", System.StringComparison.Ordinal)
                ? roomBId
                : string.Equals(roomBId, "korytarz", System.StringComparison.Ordinal)
                    ? roomAId
                    : string.Empty;

            if (string.IsNullOrWhiteSpace(roomId) ||
                !RoomVolume.TryGetCenter(roomId, out Vector3 roomCenter))
            {
                return configuredRotation;
            }

            Quaternion oppositeRotation = Quaternion.Euler(-openLocalEulerAngles);
            Vector3 configuredPosition = ResolveTargetLocalPosition(configuredRotation);
            Vector3 oppositePosition = ResolveTargetLocalPosition(oppositeRotation);
            Vector3 configuredWorldPosition = doorLeaf.parent.TransformPoint(configuredPosition);
            Vector3 oppositeWorldPosition = doorLeaf.parent.TransformPoint(oppositePosition);
            return (oppositeWorldPosition - roomCenter).sqrMagnitude <
                   (configuredWorldPosition - roomCenter).sqrMagnitude
                ? oppositeRotation
                : configuredRotation;
        }

        private Vector3 ResolveTargetLocalPosition(Quaternion relativeRotation)
        {
            Quaternion targetRotation = closedLocalRotation * relativeRotation;
            Vector3 scaledHingeOffset = Vector3.Scale(doorLeaf.localScale, resolvedHingeLocalOffset);
            Vector3 hingeInParent = closedLocalPosition + closedLocalRotation * scaledHingeOffset;
            return hingeInParent - targetRotation * scaledHingeOffset;
        }

        private IEnumerator AnimateDoor(Vector3 targetPosition, Quaternion targetRotation)
        {
            Vector3 startPosition = doorLeaf.localPosition;
            Quaternion startRotation = doorLeaf.localRotation;
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                doorLeaf.localPosition = Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    Mathf.Clamp01(elapsed / animationDuration));
                doorLeaf.localRotation = Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    Mathf.Clamp01(elapsed / animationDuration));
                yield return null;
            }

            doorLeaf.localPosition = targetPosition;
            doorLeaf.localRotation = targetRotation;
            animationRoutine = null;
        }

        private Vector3 ResolveHingeLocalOffset()
        {
            if (hingeLocalOffset.sqrMagnitude > Mathf.Epsilon)
                return hingeLocalOffset;

            if (blockingCollider is BoxCollider boxCollider && blockingCollider.transform == doorLeaf)
            {
                Vector3 scale = doorLeaf.lossyScale;
                float worldWidthX = Mathf.Abs(boxCollider.size.x * scale.x);
                float worldWidthZ = Mathf.Abs(boxCollider.size.z * scale.z);
                return worldWidthX >= worldWidthZ
                    ? boxCollider.center + Vector3.right * boxCollider.size.x * 0.5f
                    : boxCollider.center + Vector3.forward * boxCollider.size.z * 0.5f;
            }

            return Vector3.zero;
        }

        private void SeparateFlatSceneDoorLeaf()
        {
            if (doorLeaf != transform ||
                blockingCollider is not BoxCollider sourceCollider ||
                !TryGetComponent(out MeshFilter sourceFilter) ||
                !TryGetComponent(out MeshRenderer sourceRenderer))
            {
                return;
            }

            Vector3 sourceScale = transform.localScale;
            var leafObject = new GameObject("RuntimeDoorLeaf")
            {
                layer = gameObject.layer
            };
            Transform leafTransform = leafObject.transform;
            leafTransform.SetParent(transform, false);
            leafTransform.localScale = sourceScale;

            var leafFilter = leafObject.AddComponent<MeshFilter>();
            leafFilter.sharedMesh = sourceFilter.sharedMesh;

            var leafRenderer = leafObject.AddComponent<MeshRenderer>();
            leafRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
            leafRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
            leafRenderer.receiveShadows = sourceRenderer.receiveShadows;
            leafRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
            leafRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;

            var leafCollider = leafObject.AddComponent<BoxCollider>();
            leafCollider.center = sourceCollider.center;
            leafCollider.size = sourceCollider.size;
            leafCollider.sharedMaterial = sourceCollider.sharedMaterial;

            transform.localScale = Vector3.one;
            sourceCollider.center = Vector3.Scale(sourceCollider.center, sourceScale);
            sourceCollider.size = Vector3.Scale(sourceCollider.size, Abs(sourceScale));
            sourceCollider.isTrigger = true;
            sourceRenderer.enabled = false;

            doorLeaf = leafTransform;
            visualRoot = leafTransform;
            blockingCollider = leafCollider;
        }

        private static Vector3 Abs(Vector3 value) => new Vector3(
            Mathf.Abs(value.x),
            Mathf.Abs(value.y),
            Mathf.Abs(value.z));

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
            if (doorLeaf == null &&
                TryGetComponent<MeshFilter>(out _) &&
                TryGetComponent<MeshRenderer>(out _) &&
                TryGetComponent<Collider>(out _))
            {
                doorLeaf = transform;
            }
            if (blockingCollider == null && doorLeaf != null)
                blockingCollider = doorLeaf.GetComponent<Collider>();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
