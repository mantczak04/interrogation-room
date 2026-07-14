using System;
using System.Globalization;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    public sealed class NetworkEscapeExitAction : NetworkTimedInteractable
    {
        [Header("A3 stable ids")]
        [SerializeField] private string anchorId = "escape-exit-anchor";
        [SerializeField] private string planId = "escape-prototype";
        [SerializeField] private string exitId = "escape-exit-a";
        [SerializeField] private string incidentIdPrefix = "escape-attempt";
        [SerializeField] private string locationId = "escape-exit-a";

        [Header("Public presentation")]
        [SerializeField] private GameObject attemptVisual;
        [SerializeField] private GameObject completedVisual;

        [SyncVar(hook = nameof(OnActivePerformerChanged))]
        private uint activePerformerNetId;

        [SyncVar(hook = nameof(OnRetryLockedChanged))]
        private bool retryLocked;

        [SyncVar]
        private bool completed;

        private NetworkIdentity pendingPerformer;
        private bool beginConfirmed;
        private int attemptSequence;

        public string AnchorId => anchorId ?? string.Empty;
        public string PlanId => planId ?? string.Empty;
        public string ExitId => exitId ?? string.Empty;
        public string PublicReportLocationId => locationId ?? string.Empty;
        public uint ActivePerformerNetId => activePerformerNetId;
        public bool HasActivePerformerServer => pendingPerformer != null;
        public bool RetryLocked => retryLocked;
        public bool IsCompleted => completed;

        public event Action<PhysicalEscapeAttemptStarted> EscapeAttemptStartedServer;
        public event Action<PhysicalEscapeAttemptInterrupted> EscapeAttemptInterruptedServer;
        public event Action<PhysicalEscapeAttemptCompleted> EscapeAttemptCompletedServer;

        private void Awake()
        {
            ApplyPresentation();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyPresentation();
        }

        public override bool CanInteract(NetworkIdentity interactor)
        {
            return base.CanInteract(interactor) &&
                   !retryLocked &&
                   !completed &&
                   !string.IsNullOrWhiteSpace(planId) &&
                   !string.IsNullOrWhiteSpace(exitId) &&
                   !string.IsNullOrWhiteSpace(incidentIdPrefix) &&
                   !string.IsNullOrWhiteSpace(locationId);
        }

        protected override void OnInteractionBeganServer(NetworkIdentity interactor)
        {
            pendingPerformer = interactor;
            beginConfirmed = false;
            attemptSequence++;
            activePerformerNetId = interactor.netId;
            ApplyPresentation();

            string incidentId = $"{incidentIdPrefix}-{attemptSequence.ToString("D3", CultureInfo.InvariantCulture)}";
            EscapeAttemptStartedServer?.Invoke(new PhysicalEscapeAttemptStarted(
                planId,
                exitId,
                incidentId,
                locationId,
                interactor,
                netIdentity));
        }

        [Server]
        public bool ConfirmBeginServer(NetworkIdentity interactor)
        {
            if (!NetworkServer.active || !ReferenceEquals(pendingPerformer, interactor) || retryLocked)
                return false;

            beginConfirmed = true;
            return true;
        }

        [Server]
        public bool RejectBeginServer(NetworkIdentity interactor)
        {
            if (!NetworkServer.active || !ReferenceEquals(pendingPerformer, interactor) || beginConfirmed)
                return false;

            CancelInteractionServer(interactor, TimedInteractionCancellationReason.BeginRejected);
            return true;
        }

        [Server]
        public bool AuthorizeRetryServer()
        {
            if (!NetworkServer.active || pendingPerformer != null || !retryLocked || completed)
                return false;

            retryLocked = false;
            ApplyPresentation();
            return true;
        }

        /// <summary>Server hook used after the existing weapon seam accepts a hit.</summary>
        [Server]
        public bool TryInterruptPerformerServer(NetworkIdentity performer)
        {
            if (!NetworkServer.active || !ReferenceEquals(pendingPerformer, performer))
                return false;

            CancelInteractionServer(performer, TimedInteractionCancellationReason.ServerInterruption);
            return true;
        }

        protected override void OnInteractionCancelledServer(
            NetworkIdentity interactor,
            TimedInteractionCancellationReason reason)
        {
            bool shouldInterruptDomain = beginConfirmed;
            pendingPerformer = null;
            beginConfirmed = false;
            activePerformerNetId = 0;

            if (shouldInterruptDomain)
            {
                retryLocked = true;
                EscapeAttemptInterruptedServer?.Invoke(new PhysicalEscapeAttemptInterrupted(
                    planId,
                    exitId,
                    interactor,
                    reason));
            }

            ApplyPresentation();
        }

        protected override bool ApplyCompletedEffectServer(NetworkIdentity interactor)
        {
            if (!beginConfirmed || !ReferenceEquals(pendingPerformer, interactor))
                return false;

            completed = true;
            pendingPerformer = null;
            beginConfirmed = false;
            activePerformerNetId = 0;
            ApplyPresentation();
            EscapeAttemptCompletedServer?.Invoke(new PhysicalEscapeAttemptCompleted(
                planId,
                exitId,
                interactor));
            return true;
        }

        private void OnActivePerformerChanged(uint _, uint __)
        {
            ApplyPresentation();
        }

        private void OnRetryLockedChanged(bool _, bool __)
        {
            ApplyPresentation();
        }

        private void ApplyPresentation()
        {
            if (attemptVisual != null)
                attemptVisual.SetActive(activePerformerNetId != 0 || pendingPerformer != null);
            if (completedVisual != null)
                completedVisual.SetActive(completed);
        }

        public override void OnStopServer()
        {
            pendingPerformer = null;
            beginConfirmed = false;
            base.OnStopServer();
        }

        [Server]
        public override void ResetInteractionStateServer()
        {
            base.ResetInteractionStateServer();
            pendingPerformer = null;
            beginConfirmed = false;
            attemptSequence = 0;
            activePerformerNetId = 0;
            retryLocked = false;
            completed = false;
            ApplyPresentation();
        }
    }
}
