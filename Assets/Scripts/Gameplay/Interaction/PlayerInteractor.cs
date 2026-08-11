using System;
using System.Collections.Generic;
using InterrogationRoom.Gameplay;
using InterrogationRoom.Gameplay.Items;
using InterrogationRoom.Gameplay.Minigames;
using InterrogationRoom.Networking;
using InterrogationRoom.Settings;
using InterrogationRoom.UI;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    public enum TimedInteractionClientOutcome : byte
    {
        Cancelled,
        Completed,
        CompletedWithoutObjectiveProgress,
        CompletedWithoutEscapeProgress,
        MinigameFailed
    }

    public enum InteractionFeedbackKind : byte
    {
        None,
        Success,
        Warning,
        Cancelled
    }

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

        [Header("Timed interaction")]
        [SerializeField] private string interactionAnimatorBool = "IsInteracting";
        [SerializeField, Min(0.5f)] private float feedbackDuration = 4f;

        private PlayerGameplayController playerController;
        private Animator animator;
        private Component hoveredTarget;
        private NetworkIdentity hoveredIdentity;
        private INetworkInteractable hoveredInteractable;
        private bool hoveredMinigameCompleted;
        private INetworkTimedInteractable activeTimedTarget;
        private NetworkIdentity activeTimedTargetIdentity;
        private MinigameSpec activeMinigameSpec;
        private double activeTimedEndsAt;
        private bool localTimedInteractionActive;
        private bool localMinigameInteractionActive;
        private bool localMinigameCompletionPending;
        private uint localTimedTargetNetId;
        private double localTimedInteractionStartedAt;
        private double localTimedInteractionEndsAt;
        private string localTimedInteractionPrompt;
        private string localInteractionFeedback;
        private double localInteractionFeedbackEndsAt;
        private InteractionFeedbackKind localInteractionFeedbackKind;
        private readonly Dictionary<uint, int> completedMinigameTargetEpochs =
            new Dictionary<uint, int>();
        private readonly RaycastHit[] lineOfSightHitBuffer = new RaycastHit[32];

        [SyncVar(hook = nameof(OnInteractionMovementLockedChanged))]
        private bool interactionMovementLocked;

        public Component HoveredTarget => hoveredTarget;

        public bool HasHoveredTarget => hoveredTarget != null && hoveredInteractable != null;
        public bool HoveredMinigameCompleted => hoveredMinigameCompleted;

        public string HoveredPrompt => hoveredMinigameCompleted
            ? UiText.Get("To zadanie zostało już wykonane.")
            : hoveredInteractable?.InteractionPrompt;
        public bool HoveredInteractionRequiresHold =>
            !hoveredMinigameCompleted &&
            hoveredInteractable is INetworkTimedInteractable &&
            (hoveredTarget == null || hoveredTarget.GetComponent<MinigameSpec>() == null);
        public bool IsMovementLocked => interactionMovementLocked;
        public bool HasActiveTimedInteraction => localTimedInteractionActive;
        public NetworkCarryableItem HeldItem => NetworkCarryableItem.FindCarriedBy(netIdentity);
        public bool HasHeldItem => HeldItem != null;
        public string ActiveTimedInteractionPrompt => localTimedInteractionPrompt;
        public bool HasInteractionFeedback =>
            !string.IsNullOrEmpty(localInteractionFeedback) &&
            NetworkTime.time < localInteractionFeedbackEndsAt;
        public string InteractionFeedback => HasInteractionFeedback
            ? localInteractionFeedback
            : null;
        public InteractionFeedbackKind FeedbackKind => HasInteractionFeedback
            ? localInteractionFeedbackKind
            : InteractionFeedbackKind.None;
        public float TimedInteractionProgress01 => !localTimedInteractionActive
            ? 0f
            : Mathf.Clamp01((float)((NetworkTime.time - localTimedInteractionStartedAt) /
                Math.Max(0.001d, localTimedInteractionEndsAt - localTimedInteractionStartedAt)));

        public event Action<Component> HoveredTargetChanged;

        private void Awake()
        {
            playerController = GetComponent<PlayerGameplayController>();
            animator = GetComponent<Animator>();
        }

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
            if (NetworkServer.active)
                CancelActiveTimedInteractionServer(TimedInteractionCancellationReason.PerformerUnavailable);
            SetHoveredTarget(null, null);
            ClearLocalTimedInteraction();
        }

        private void Update()
        {
            if (isServer)
                UpdateActiveTimedInteractionServer();

            if (!isLocalPlayer || interactionCamera == null)
            {
                return;
            }

            if (localMinigameCompletionPending && NetworkTime.time >= localTimedInteractionEndsAt)
            {
                localMinigameCompletionPending = false;
                CmdCompleteMinigame();
            }

            if (!PlayerInputGate.CursorReleased &&
                !localTimedInteractionActive &&
                WasDropPressed() &&
                (playerController == null || (!playerController.IsDead && !playerController.IsSeated)))
            {
                CmdDropCarriedItem();
            }

            if (PlayerInputGate.CursorReleased)
            {
                if (localTimedInteractionActive && !localMinigameInteractionActive)
                {
                    ClearLocalTimedInteraction();
                    CmdCancelTimedInteraction();
                }

                SetHoveredTarget(null, null);
                return;
            }

            RefreshHoveredTarget();

            if (localTimedInteractionActive && !localMinigameInteractionActive && WasInteractReleased())
            {
                ClearLocalTimedInteraction();
                CmdCancelTimedInteraction();
                return;
            }

            if (!WasInteractPressed())
            {
                return;
            }

            if (playerController != null && playerController.TryRequestStand())
            {
                return;
            }

            if (!HasHoveredTarget)
            {
                return;
            }

            if (hoveredMinigameCompleted)
                return;

            if (hoveredIdentity != null && hoveredIdentity.netId != 0)
            {
                CmdTryInteract(hoveredIdentity.netId);
            }
            else
            {
                CmdTryInteractByDirection(interactionCamera.transform.forward);
            }
        }

        private void RefreshHoveredTarget()
        {
            if (playerController != null && (playerController.IsSeated || playerController.IsDead))
            {
                SetHoveredTarget(null, null);
                return;
            }

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

            if (!TryGetInteractable(hit.collider.transform, out INetworkInteractable interactable))
            {
                SetHoveredTarget(null, null);
                return;
            }

            bool completed = IsLocallyCompletedMinigameTarget(interactable as Component);
            if ((!completed && !IsHoverActionable(interactable)) ||
                (completed && !IsHoverReachable(interactable)))
            {
                SetHoveredTarget(null, null);
                return;
            }

            SetHoveredTarget(interactable as Component, interactable, completed);
        }

        /// <summary>
        /// Mirrors the server-side validation so the hover highlight and prompt only
        /// appear when the interaction request would actually be accepted.
        /// </summary>
        private bool IsHoverActionable(INetworkInteractable interactable)
        {
            if (!interactable.CanInteract(netIdentity))
            {
                return false;
            }

            return IsHoverReachable(interactable);
        }

        private bool IsHoverReachable(INetworkInteractable interactable)
        {

            Vector3 interactionPosition = interactable.InteractionPosition;
            if ((interactionPosition - transform.position).sqrMagnitude > interactionRange * interactionRange)
            {
                return false;
            }

            Component targetComponent = interactable as Component;
            NetworkIdentity targetIdentity = targetComponent != null
                ? targetComponent.GetComponentInParent<NetworkIdentity>()
                : null;

            if (targetIdentity != null && targetIdentity.netId != 0)
            {
                return HasLineOfSightTo(targetIdentity, interactionPosition);
            }

            // Direction fallback parity: the server re-raycasts from the player's
            // head along the camera direction, so require the same ray to resolve
            // to the same interactable here.
            Vector3 origin = transform.TransformPoint(Vector3.up * serverViewHeight);
            return TryGetFirstNonSelfHit(
                       origin,
                       interactionCamera.transform.forward,
                       interactionRange + serverRangeTolerance,
                       out RaycastHit headHit) &&
                   TryGetInteractable(headHit.collider.transform, out INetworkInteractable headInteractable) &&
                   ReferenceEquals(headInteractable, interactable);
        }

        private void SetHoveredTarget(
            Component target,
            INetworkInteractable interactable,
            bool minigameCompleted = false)
        {
            hoveredIdentity = target != null ? target.GetComponentInParent<NetworkIdentity>() : null;
            hoveredMinigameCompleted = minigameCompleted;

            if (hoveredTarget == target)
            {
                hoveredInteractable = interactable;
                return;
            }

            hoveredTarget = target;
            hoveredInteractable = interactable;
            HoveredTargetChanged?.Invoke(target);
        }

        private bool IsLocallyCompletedMinigameTarget(Component target)
        {
            if (target == null || target.GetComponent<MinigameSpec>() == null)
                return false;

            NetworkIdentity identity = target.GetComponentInParent<NetworkIdentity>();
            if (identity == null || identity.netId == 0 ||
                !completedMinigameTargetEpochs.TryGetValue(identity.netId, out int completedEpoch))
            {
                return false;
            }

            NetworkObjectiveWorldAction objectiveAction =
                target.GetComponent<NetworkObjectiveWorldAction>();
            if (objectiveAction != null && objectiveAction.CompletionEpoch != completedEpoch)
            {
                completedMinigameTargetEpochs.Remove(identity.netId);
                return false;
            }

            return true;
        }

        [Command]
        private void CmdTryInteract(uint targetNetId)
        {
            if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity) ||
                targetIdentity == netIdentity ||
                !TryGetInteractable(targetIdentity.transform, out INetworkInteractable interactable))
            {
                return;
            }

            float allowedDistance = interactionRange + serverRangeTolerance;
            Vector3 interactionPosition = interactable.InteractionPosition;
            if ((interactionPosition - transform.position).sqrMagnitude > allowedDistance * allowedDistance ||
                !HasLineOfSightTo(targetIdentity, interactionPosition))
            {
                return;
            }

            TryBeginOrCompleteInstantServer(targetIdentity, interactable);
        }

        [Command]
        private void CmdTryInteractByDirection(Vector3 requestedDirection)
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

            Component targetComponent = interactable as Component;
            NetworkIdentity targetIdentity = targetComponent != null
                ? targetComponent.GetComponentInParent<NetworkIdentity>()
                : null;
            TryBeginOrCompleteInstantServer(targetIdentity, interactable);
        }

        [Command]
        private void CmdCancelTimedInteraction()
        {
            CancelActiveTimedInteractionServer(TimedInteractionCancellationReason.Explicit);
        }

        [Command]
        private void CmdDropCarriedItem()
        {
            NetworkCarryableItem.FindCarriedBy(netIdentity)?.DropServer();
        }

        [Command]
        private void CmdCompleteMinigame()
        {
            TryCompleteMinigameServer();
        }

        [Server]
        private bool TryCompleteMinigameServer()
        {
            if (activeTimedTarget == null || activeMinigameSpec == null ||
                NetworkTime.time < activeTimedEndsAt)
            {
                return false;
            }

            NetworkIdentity actor = GetComponent<NetworkIdentity>();
            bool worldCompleted = activeTimedTarget.TryCompleteInteractionServer(actor);
            if (!worldCompleted)
            {
                activeTimedTarget.CancelInteractionServer(
                    actor,
                    TimedInteractionCancellationReason.CompletionRejected);
            }
            TimedInteractionClientOutcome outcome = ResolveCompletionOutcomeServer(
                activeTimedTarget,
                actor,
                worldCompleted);
            if (worldCompleted && activeMinigameSpec != null &&
                activeTimedTarget is NetworkObjectiveWorldAction objectiveAction)
            {
                objectiveAction.RetainActorCompletionServer(actor);
            }
            EndActiveTimedInteractionServer(outcome);
            return outcome == TimedInteractionClientOutcome.Completed;
        }

        [Command]
        private void CmdFailMinigame()
        {
            if (activeTimedTarget == null || activeMinigameSpec == null)
                return;

            activeMinigameSpec.ApplyFailureConsequenceServer(netIdentity);
            activeTimedTarget.CancelInteractionServer(
                netIdentity,
                TimedInteractionCancellationReason.MinigameFailed);
            EndActiveTimedInteractionServer(TimedInteractionClientOutcome.MinigameFailed);
        }

        [Server]
        private void TryBeginOrCompleteInstantServer(
            NetworkIdentity targetIdentity,
            INetworkInteractable interactable)
        {
            if (activeTimedTarget != null)
                return;

            if (interactable is INetworkTimedInteractable timedInteractable)
            {
                if (!AllowsTimedRoundInteraction() ||
                    targetIdentity == null ||
                    !timedInteractable.TryBeginInteractionServer(netIdentity))
                {
                    SendInteractionFeedbackServer(
                        InteractionFeedbackKind.Cancelled,
                        "Nie można teraz wykonać tej czynności.",
                        1.8f);
                    return;
                }

                // A synchronous server binder may reject the emitted begin
                // result and cancel the reservation before this method resumes.
                if (!timedInteractable.HasActiveInteractor)
                    return;

                activeTimedTarget = timedInteractable;
                activeTimedTargetIdentity = targetIdentity;
                Component timedComponent = timedInteractable as Component;
                activeMinigameSpec = timedComponent != null
                    ? timedComponent.GetComponent<MinigameSpec>()
                    : null;
                float minimumDuration = activeMinigameSpec != null
                    ? activeMinigameSpec.MinimumPlausibleDuration
                    : timedInteractable.InteractionDuration;
                activeTimedEndsAt = NetworkTime.time + Math.Max(0.05d, minimumDuration);
                interactionMovementLocked = true;
                SetInteractionAnimation(true);
                TargetBeginTimedInteraction(
                    connectionToClient,
                    targetIdentity.netId,
                    activeTimedEndsAt,
                    minimumDuration,
                    timedInteractable.InteractionPrompt,
                    activeMinigameSpec != null);
                return;
            }

            bool completed = interactable.TryInteractServer(netIdentity);
            if (ShouldShowInstantInteractionFeedback(completed))
            {
                SendInteractionFeedbackServer(
                    InteractionFeedbackKind.Cancelled,
                    "Nie można teraz wykonać tej czynności.",
                    1.8f);
            }
        }

        private static bool ShouldShowInstantInteractionFeedback(bool completed) => !completed;

        [Server]
        private void SendInteractionFeedbackServer(
            InteractionFeedbackKind kind,
            string message,
            float duration)
        {
            NetworkConnectionToClient targetConnection = netIdentity != null
                ? netIdentity.connectionToClient
                : null;
            if (targetConnection != null)
                TargetShowInteractionFeedback(targetConnection, kind, message, duration);
        }

        [TargetRpc]
        private void TargetShowInteractionFeedback(
            NetworkConnection target,
            InteractionFeedbackKind kind,
            string message,
            float duration)
        {
            SetLocalInteractionFeedback(kind, message, duration);
        }

        [Server]
        private void UpdateActiveTimedInteractionServer()
        {
            if (activeTimedTarget == null)
                return;

            if (!AllowsTimedRoundInteraction())
            {
                CancelActiveTimedInteractionServer(TimedInteractionCancellationReason.ValidationFailed);
                return;
            }

            Component targetComponent = activeTimedTarget as Component;
            if (targetComponent == null || activeTimedTargetIdentity == null ||
                !activeTimedTarget.HasActiveInteractor ||
                playerController == null || playerController.IsDead || playerController.IsSeated)
            {
                CancelActiveTimedInteractionServer(TimedInteractionCancellationReason.PerformerUnavailable);
                return;
            }

            float allowedDistance = interactionRange + serverRangeTolerance;
            Vector3 interactionPosition = activeTimedTarget.InteractionPosition;
            if ((interactionPosition - transform.position).sqrMagnitude > allowedDistance * allowedDistance ||
                !HasLineOfSightTo(activeTimedTargetIdentity, interactionPosition))
            {
                CancelActiveTimedInteractionServer(TimedInteractionCancellationReason.ValidationFailed);
                return;
            }

            if (activeMinigameSpec != null || NetworkTime.time < activeTimedEndsAt)
                return;

            bool worldCompleted = activeTimedTarget.TryCompleteInteractionServer(netIdentity);
            if (!worldCompleted)
                activeTimedTarget.CancelInteractionServer(
                    netIdentity,
                    TimedInteractionCancellationReason.CompletionRejected);
            EndActiveTimedInteractionServer(ResolveCompletionOutcomeServer(
                activeTimedTarget,
                netIdentity,
                worldCompleted));
        }

        private static bool AllowsTimedRoundInteraction()
        {
            NetworkRoundCoordinator coordinator =
                UnityEngine.Object.FindFirstObjectByType<NetworkRoundCoordinator>();
            return coordinator != null && coordinator.AllowsPhysicalRoundActions;
        }

        [Server]
        private void CancelActiveTimedInteractionServer(TimedInteractionCancellationReason reason)
        {
            activeTimedTarget?.CancelInteractionServer(netIdentity, reason);
            EndActiveTimedInteractionServer(TimedInteractionClientOutcome.Cancelled);
        }

        [Server]
        private void EndActiveTimedInteractionServer(TimedInteractionClientOutcome outcome)
        {
            activeTimedTarget = null;
            activeTimedTargetIdentity = null;
            activeMinigameSpec = null;
            activeTimedEndsAt = 0d;
            interactionMovementLocked = false;
            SetInteractionAnimation(false);
            NetworkIdentity identity = GetComponent<NetworkIdentity>();
            NetworkConnectionToClient targetConnection = identity != null
                ? identity.connectionToClient
                : null;
            if (targetConnection != null)
                TargetEndTimedInteraction(targetConnection, outcome);
        }

        [Server]
        private static TimedInteractionClientOutcome ResolveCompletionOutcomeServer(
            INetworkTimedInteractable target,
            NetworkIdentity actor,
            bool worldCompleted)
        {
            if (!worldCompleted)
                return TimedInteractionClientOutcome.Cancelled;

            if (target is NetworkObjectiveWorldAction objectiveAction &&
                !objectiveAction.HasActorCompletionServer(actor))
            {
                return target.CompletionPayloadId?.StartsWith(
                    "escape-prepare-",
                    StringComparison.Ordinal) == true
                    ? TimedInteractionClientOutcome.CompletedWithoutEscapeProgress
                    : TimedInteractionClientOutcome.CompletedWithoutObjectiveProgress;
            }

            return TimedInteractionClientOutcome.Completed;
        }

        [TargetRpc]
        private void TargetBeginTimedInteraction(
            NetworkConnection target,
            uint targetNetId,
            double endsAt,
            float duration,
            string prompt,
            bool hasMinigame)
        {
            localInteractionFeedback = null;
            localInteractionFeedbackEndsAt = 0d;
            localTimedInteractionActive = true;
            localMinigameInteractionActive = hasMinigame;
            localMinigameCompletionPending = false;
            localTimedTargetNetId = targetNetId;
            localTimedInteractionEndsAt = endsAt;
            localTimedInteractionStartedAt = endsAt - Math.Max(0.05d, duration);
            localTimedInteractionPrompt = prompt;

            if (!hasMinigame)
                return;

            MinigameSpec spec = ResolveClientMinigameSpec(targetNetId);
            if (spec == null)
            {
                ClearLocalTimedInteraction();
                CmdCancelTimedInteraction();
                return;
            }

            MinigamePanelHost.Open(
                spec,
                OnLocalMinigameSucceeded,
                OnLocalMinigameFailed,
                OnLocalMinigameCancelled);
        }

        [TargetRpc]
        private void TargetEndTimedInteraction(
            NetworkConnection target,
            TimedInteractionClientOutcome outcome)
        {
            if (localMinigameInteractionActive && IsCompletedOutcome(outcome))
                MarkLocalMinigameCompleted(localTimedTargetNetId);

            ClearLocalTimedInteraction();
            SetLocalInteractionFeedback(
                ResolveFeedbackKind(outcome),
                ResolveFeedback(outcome),
                ResolveFeedbackDuration(outcome));
        }

        private void SetLocalInteractionFeedback(
            InteractionFeedbackKind kind,
            string message,
            float duration)
        {
            localInteractionFeedbackKind = kind;
            localInteractionFeedback = message;
            localInteractionFeedbackEndsAt = NetworkTime.time +
                Mathf.Min(feedbackDuration, Mathf.Max(0.1f, duration));
        }

        private static InteractionFeedbackKind ResolveFeedbackKind(
            TimedInteractionClientOutcome outcome)
        {
            switch (outcome)
            {
                case TimedInteractionClientOutcome.Completed:
                    return InteractionFeedbackKind.Success;
                case TimedInteractionClientOutcome.CompletedWithoutObjectiveProgress:
                case TimedInteractionClientOutcome.CompletedWithoutEscapeProgress:
                    return InteractionFeedbackKind.Warning;
                default:
                    return InteractionFeedbackKind.Cancelled;
            }
        }

        private static float ResolveFeedbackDuration(TimedInteractionClientOutcome outcome)
        {
            switch (outcome)
            {
                case TimedInteractionClientOutcome.Completed:
                    return 1.4f;
                case TimedInteractionClientOutcome.CompletedWithoutObjectiveProgress:
                case TimedInteractionClientOutcome.CompletedWithoutEscapeProgress:
                    return 3.5f;
                case TimedInteractionClientOutcome.MinigameFailed:
                    return 2.5f;
                default:
                    return 1.8f;
            }
        }

        private static string ResolveFeedback(TimedInteractionClientOutcome outcome)
        {
            switch (outcome)
            {
                case TimedInteractionClientOutcome.Completed:
                    return "Czynność zatwierdzona.";
                case TimedInteractionClientOutcome.CompletedWithoutObjectiveProgress:
                    return "Czynność wykonana, ale nie rozwinęła twojego aktualnego celu.";
                case TimedInteractionClientOutcome.CompletedWithoutEscapeProgress:
                    return "Zamek otwarty, ale nie jest to etap Twojego celu.";
                case TimedInteractionClientOutcome.MinigameFailed:
                    return "Niepowodzenie. Możesz spróbować ponownie.";
                default:
                    return "Interakcja przerwana.";
            }
        }

        private void OnLocalMinigameSucceeded()
        {
            if (!localMinigameInteractionActive || localMinigameCompletionPending)
                return;

            if (NetworkTime.time < localTimedInteractionEndsAt)
            {
                localMinigameCompletionPending = true;
                return;
            }

            CmdCompleteMinigame();
        }

        private void OnLocalMinigameFailed()
        {
            if (!localMinigameInteractionActive)
                return;

            ClearLocalTimedInteraction();
            CmdFailMinigame();
        }

        private void OnLocalMinigameCancelled()
        {
            if (!localMinigameInteractionActive)
                return;

            ClearLocalTimedInteraction();
            CmdCancelTimedInteraction();
        }

        private static MinigameSpec ResolveClientMinigameSpec(uint targetNetId)
        {
            return targetNetId != 0 &&
                   NetworkClient.spawned.TryGetValue(targetNetId, out NetworkIdentity identity)
                ? identity.GetComponent<MinigameSpec>()
                : null;
        }

        private void MarkLocalMinigameCompleted(uint targetNetId)
        {
            if (targetNetId == 0 ||
                !NetworkClient.spawned.TryGetValue(targetNetId, out NetworkIdentity identity))
            {
                return;
            }

            NetworkObjectiveWorldAction objectiveAction =
                identity.GetComponent<NetworkObjectiveWorldAction>();
            completedMinigameTargetEpochs[targetNetId] =
                objectiveAction != null ? objectiveAction.CompletionEpoch : 0;
        }

        private static bool IsCompletedOutcome(TimedInteractionClientOutcome outcome)
        {
            return outcome == TimedInteractionClientOutcome.Completed ||
                   outcome == TimedInteractionClientOutcome.CompletedWithoutObjectiveProgress ||
                   outcome == TimedInteractionClientOutcome.CompletedWithoutEscapeProgress;
        }

        private void ClearLocalTimedInteraction()
        {
            localTimedInteractionActive = false;
            localMinigameInteractionActive = false;
            localMinigameCompletionPending = false;
            localTimedTargetNetId = 0;
            localTimedInteractionStartedAt = 0d;
            localTimedInteractionEndsAt = 0d;
            localTimedInteractionPrompt = null;
            MinigamePanelHost.Close(notifyCancellation: false);
        }

        private void OnInteractionMovementLockedChanged(bool _, bool locked)
        {
            SetInteractionAnimation(locked);
        }

        private void SetInteractionAnimation(bool active)
        {
            if (animator == null ||
                animator.runtimeAnimatorController == null ||
                string.IsNullOrWhiteSpace(interactionAnimatorBool))
                return;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Bool &&
                    parameter.name == interactionAnimatorBool)
                {
                    animator.SetBool(interactionAnimatorBool, active);
                    return;
                }
            }
        }

        private bool HasLineOfSightTo(NetworkIdentity targetIdentity, Vector3 interactionPosition)
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
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                direction,
                lineOfSightHitBuffer,
                maxDistance,
                interactionMask,
                QueryTriggerInteraction.Collide);

            closestHit = default;
            float closestDistance = float.PositiveInfinity;
            bool foundHit = false;

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = lineOfSightHitBuffer[index];
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

        private static bool WasDropPressed()
        {
            return GameInputBindings.WasPressedThisFrame(GameInputAction.Drop);
        }

        private static bool WasInteractPressed()
        {
            return GameInputBindings.WasPressedThisFrame(GameInputAction.Interact);
        }

        private static bool WasInteractReleased()
        {
            return GameInputBindings.WasReleasedThisFrame(GameInputAction.Interact);
        }
    }
}
