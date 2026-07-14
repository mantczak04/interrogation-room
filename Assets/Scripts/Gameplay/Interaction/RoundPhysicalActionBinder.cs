using System;
using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Domain;
using InterrogationRoom.Networking;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    /// <summary>
    /// Area A scene adapter. It authenticates physical B4/B5 signals through
    /// NetworkRoundCoordinator and never resolves a role or secret locally.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoundPhysicalActionBinder : MonoBehaviour
    {
        [SerializeField] private NetworkRoundCoordinator coordinator;
        [SerializeField] private Transform bindingRoot;

        private readonly List<NetworkTimedInteractable> timedActions =
            new List<NetworkTimedInteractable>();
        private readonly List<NetworkObjectiveWorldAction> objectiveActions =
            new List<NetworkObjectiveWorldAction>();
        private readonly List<NetworkIncidentWorldAction> incidentActions =
            new List<NetworkIncidentWorldAction>();
        private readonly List<NetworkAlibiClueAction> clueActions =
            new List<NetworkAlibiClueAction>();
        private readonly List<NetworkEscapeExitAction> escapeExits =
            new List<NetworkEscapeExitAction>();
        private readonly List<QuietIncidentDiscoveryProbe> discoveryProbes =
            new List<QuietIncidentDiscoveryProbe>();

        public int BoundActionCount => timedActions.Count;

        private void Reset()
        {
            bindingRoot = transform;
            coordinator = FindFirstObjectByType<NetworkRoundCoordinator>();
        }

        private void OnEnable()
        {
            if (bindingRoot == null)
                bindingRoot = transform;
            if (coordinator == null)
                coordinator = FindFirstObjectByType<NetworkRoundCoordinator>();

            RefreshBindings();
            if (coordinator == null)
            {
                Debug.LogError("[RoundPhysicalActionBinder] NetworkRoundCoordinator is required.", this);
                enabled = false;
                return;
            }

            coordinator.ServerRoundReset += ResetPhysicalStateServer;
            coordinator.ServerExecutionAccepted += OnExecutionAcceptedServer;
        }

        private void OnDisable()
        {
            if (coordinator != null)
            {
                coordinator.ServerRoundReset -= ResetPhysicalStateServer;
                coordinator.ServerExecutionAccepted -= OnExecutionAcceptedServer;
            }

            UnsubscribeActions();
        }

        public void RefreshBindings()
        {
            UnsubscribeActions();
            Transform root = bindingRoot != null ? bindingRoot : transform;

            timedActions.AddRange(root.GetComponentsInChildren<NetworkTimedInteractable>(true));
            objectiveActions.AddRange(root.GetComponentsInChildren<NetworkObjectiveWorldAction>(true));
            incidentActions.AddRange(root.GetComponentsInChildren<NetworkIncidentWorldAction>(true));
            clueActions.AddRange(root.GetComponentsInChildren<NetworkAlibiClueAction>(true));
            escapeExits.AddRange(root.GetComponentsInChildren<NetworkEscapeExitAction>(true));
            discoveryProbes.AddRange(root.GetComponentsInChildren<QuietIncidentDiscoveryProbe>(true));

            foreach (NetworkObjectiveWorldAction action in objectiveActions)
            {
                if (!(action is NetworkIncidentWorldAction) && !(action is NetworkAlibiClueAction))
                    action.CompletedServer += OnObjectiveActionCompletedServer;
            }
            foreach (NetworkIncidentWorldAction action in incidentActions)
                action.IncidentRaisedServer += OnIncidentRaisedServer;
            foreach (NetworkAlibiClueAction action in clueActions)
                action.ClueAcquiredServer += OnClueAcquiredServer;
            foreach (NetworkEscapeExitAction exit in escapeExits)
            {
                exit.EscapeAttemptStartedServer += OnEscapeStartedServer;
                exit.EscapeAttemptInterruptedServer += OnEscapeInterruptedServer;
                exit.EscapeAttemptCompletedServer += OnEscapeCompletedServer;
            }
            foreach (QuietIncidentDiscoveryProbe probe in discoveryProbes)
                probe.DiscoveryCandidateServer += OnQuietDiscoveryCandidateServer;
        }

        private void UnsubscribeActions()
        {
            foreach (NetworkObjectiveWorldAction action in objectiveActions)
            {
                if (action != null)
                    action.CompletedServer -= OnObjectiveActionCompletedServer;
            }
            foreach (NetworkIncidentWorldAction action in incidentActions)
            {
                if (action != null)
                    action.IncidentRaisedServer -= OnIncidentRaisedServer;
            }
            foreach (NetworkAlibiClueAction action in clueActions)
            {
                if (action != null)
                    action.ClueAcquiredServer -= OnClueAcquiredServer;
            }
            foreach (NetworkEscapeExitAction exit in escapeExits)
            {
                if (exit == null)
                    continue;
                exit.EscapeAttemptStartedServer -= OnEscapeStartedServer;
                exit.EscapeAttemptInterruptedServer -= OnEscapeInterruptedServer;
                exit.EscapeAttemptCompletedServer -= OnEscapeCompletedServer;
            }
            foreach (QuietIncidentDiscoveryProbe probe in discoveryProbes)
            {
                if (probe != null)
                    probe.DiscoveryCandidateServer -= OnQuietDiscoveryCandidateServer;
            }

            timedActions.Clear();
            objectiveActions.Clear();
            incidentActions.Clear();
            clueActions.Clear();
            escapeExits.Clear();
            discoveryProbes.Clear();
        }

        private void OnObjectiveActionCompletedServer(NetworkInteractionCompletion completion)
        {
            var action = completion.Target != null
                ? completion.Target.GetComponent<NetworkObjectiveWorldAction>()
                : null;
            bool accepted;
            if (IsEscapePreparationStep(completion.PayloadId))
            {
                accepted = coordinator.TryPreparePhysicalEscape(
                    completion.Actor,
                    EscapePlanDefinitions.Prototype.Id.Value,
                    completion.PayloadId);
                if (accepted)
                    AuthorizePreparedExitRetry(completion.PayloadId);
            }
            else
            {
                accepted = coordinator.TryAdvancePhysicalObjective(
                    completion.Actor,
                    completion.PayloadId);
            }

            if (!accepted)
                action?.ReleaseActorCompletionServer(completion.Actor);
        }

        private void OnIncidentRaisedServer(PhysicalIncidentSignal signal)
        {
            bool accepted = coordinator.TryRegisterPhysicalIncident(
                signal.Actor,
                signal.IncidentId,
                ToDomainKind(signal.Kind),
                signal.EffectId,
                signal.LocationId,
                signal.ObjectiveStepId,
                out bool objectiveAdvanced);

            if ((!accepted || (!string.IsNullOrWhiteSpace(signal.ObjectiveStepId) && !objectiveAdvanced)) &&
                signal.Source != null)
            {
                signal.Source.GetComponent<NetworkObjectiveWorldAction>()
                    ?.ReleaseActorCompletionServer(signal.Actor);
            }
        }

        private void OnQuietDiscoveryCandidateServer(QuietIncidentDiscoveryCandidate candidate)
        {
            coordinator.TryDiscoverPhysicalQuietIncident(candidate.Viewer, candidate.IncidentId);
        }

        private void OnClueAcquiredServer(PhysicalAlibiClueSignal signal)
        {
            bool accepted = coordinator.TryAcquirePhysicalAlibiClue(
                signal.Actor,
                signal.ClueId,
                signal.IncidentId,
                ToDomainKind(signal.IncidentKind),
                signal.EffectId,
                signal.LocationId);
            if (!accepted && signal.Source != null)
            {
                signal.Source.GetComponent<NetworkObjectiveWorldAction>()
                    ?.ReleaseActorCompletionServer(signal.Actor);
            }
        }

        private void OnEscapeStartedServer(PhysicalEscapeAttemptStarted attempt)
        {
            var exit = attempt.Source != null
                ? attempt.Source.GetComponent<NetworkEscapeExitAction>()
                : null;
            bool accepted = coordinator.TryBeginPhysicalEscape(
                attempt.Actor,
                attempt.PlanId,
                attempt.ExitId,
                attempt.IncidentId);
            if (accepted)
                exit?.ConfirmBeginServer(attempt.Actor);
            else
                exit?.RejectBeginServer(attempt.Actor);
        }

        private void OnEscapeInterruptedServer(PhysicalEscapeAttemptInterrupted attempt)
        {
            coordinator.TryInterruptPhysicalEscape(
                attempt.Actor,
                attempt.PlanId,
                attempt.ExitId);
        }

        private void OnEscapeCompletedServer(PhysicalEscapeAttemptCompleted attempt)
        {
            coordinator.TryCompletePhysicalEscape(
                attempt.Actor,
                attempt.PlanId,
                attempt.ExitId);
        }

        private void OnExecutionAcceptedServer(NetworkIdentity target)
        {
            foreach (NetworkEscapeExitAction exit in escapeExits)
                exit.TryInterruptPerformerServer(target);
        }

        [Server]
        public void ResetPhysicalStateServer()
        {
            if (!NetworkServer.active)
                return;

            foreach (NetworkTimedInteractable action in timedActions)
                action.ResetInteractionStateServer();
            foreach (QuietIncidentDiscoveryProbe probe in discoveryProbes)
                probe.ResetDiscoveryStateServer();
        }

        private void AuthorizePreparedExitRetry(string preparationStepId)
        {
            EscapeExitDefinition definition = EscapePlanDefinitions.Prototype.Exits
                .FirstOrDefault(value => value.PreparationStepId.Value == preparationStepId);
            if (definition == null)
                return;

            escapeExits.FirstOrDefault(value => value.ExitId == definition.Id.Value)
                ?.AuthorizeRetryServer();
        }

        private static bool IsEscapePreparationStep(string stepId)
        {
            return EscapePlanDefinitions.Prototype.CommonSteps.Any(
                       value => value.Id.Value == stepId) ||
                   EscapePlanDefinitions.Prototype.Exits.Any(
                       value => value.PreparationStepId.Value == stepId);
        }

        private static IncidentKind ToDomainKind(PhysicalIncidentKind kind) =>
            kind == PhysicalIncidentKind.Loud ? IncidentKind.Loud : IncidentKind.Quiet;
    }
}
