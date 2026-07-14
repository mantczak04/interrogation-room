using System;
using System.Collections.Generic;
using System.Linq;

namespace InterrogationRoom.Domain
{
    /// <summary>
    /// State-changing intention handled by <see cref="RoundEngine.Handle"/>.
    /// Closed hierarchy of intentions accepted by the current Runda domain.
    /// </summary>
    public abstract class RoundCommand
    {
        private RoundCommand()
        {
        }

        /// <summary>
        /// Validates the Skład Rundy, assigns roles and redacts the Alibi from
        /// the seed, then enters Przygotowanie. Allowed only in Lobby.
        /// </summary>
        public sealed class StartRound : RoundCommand
        {
            public CaseDefinition Case { get; }

            /// <summary>Skład Rundy — 3 to 6 distinct players.</summary>
            public IReadOnlyList<PlayerId> Players { get; }

            /// <summary>Seed for role assignment and hidden-fact selection; makes the Runda replayable in tests.</summary>
            public int Seed { get; }

            /// <summary>
            /// Requested number of Sekretny Cel assignments. Null selects the
            /// approved default: 0 for three or four players and 1 for five or six.
            /// </summary>
            public int? SecretObjectiveCount { get; }

            /// <summary>Map-authored Plan Ucieczki; defaults to the first prototype contract.</summary>
            public EscapePlanDefinition EscapePlan { get; }

            public StartRound(
                CaseDefinition caseDefinition,
                IEnumerable<PlayerId> players,
                int seed,
                int? secretObjectiveCount = null,
                EscapePlanDefinition escapePlan = null)
            {
                Case = caseDefinition;
                Players = players?.ToArray() ?? throw new ArgumentNullException(nameof(players));
                Seed = seed;
                SecretObjectiveCount = secretObjectiveCount;
                EscapePlan = escapePlan ?? EscapePlanDefinitions.Prototype;
            }
        }

        /// <summary>
        /// Ends Przygotowanie (host command or safety timer) and starts the
        /// Runda with its Limit Rundy. The Alibi becomes permanently unavailable.
        /// </summary>
        public sealed class EndPreparation : RoundCommand
        {
        }

        /// <summary>
        /// The Detektyw's single, irreversible Egzekucja of a Podejrzany.
        /// Resolves the Runda immediately. Allowed only during Runda.
        /// </summary>
        public sealed class Execute : RoundCommand
        {
            public PlayerId Target { get; }

            public Execute(PlayerId target)
            {
                Target = target;
            }
        }

        /// <summary>
        /// Limit Rundy expired without an Egzekucja — the Detektyw loses. Time
        /// is owned by the network adapter; the domain never reads a clock.
        /// </summary>
        public sealed class TimeExpired : RoundCommand
        {
        }

        /// <summary>
        /// Reports completion of the owner's current physical Prywatny Cel step.
        /// Stable ids keep the interaction layer independent of Runda rules.
        /// </summary>
        public sealed class AdvancePrivateObjective : RoundCommand
        {
            public PlayerId Player { get; }
            public PrivateObjectiveId ObjectiveId { get; }
            public PrivateObjectiveStepId StepId { get; }

            public AdvancePrivateObjective(
                PlayerId player,
                PrivateObjectiveId objectiveId,
                PrivateObjectiveStepId stepId)
            {
                Player = player;
                ObjectiveId = objectiveId;
                StepId = stepId;
            }
        }

        /// <summary>
        /// Registers one durable Incydent after its world effect succeeds. The
        /// optional objective reference advances only the author's exact,
        /// current Prywatny Cel step.
        /// </summary>
        public sealed class RegisterIncident : RoundCommand
        {
            public PlayerId Author { get; }
            public IncidentId IncidentId { get; }
            public IncidentKind Kind { get; }
            public IncidentEffectId Effect { get; }
            public IncidentLocationId Location { get; }
            public IncidentTimestamp OccurredAt { get; }
            public PrivateObjectiveStepReference ObjectiveStep { get; }

            public RegisterIncident(
                PlayerId author,
                IncidentId incidentId,
                IncidentKind kind,
                IncidentEffectId effect,
                IncidentLocationId location,
                IncidentTimestamp occurredAt,
                PrivateObjectiveStepReference objectiveStep = null)
            {
                Author = author;
                IncidentId = incidentId;
                Kind = kind;
                Effect = effect;
                Location = location;
                OccurredAt = occurredAt;
                ObjectiveStep = objectiveStep;
            }
        }

        /// <summary>
        /// Records the Detektyw's personal discovery of a quiet Incydent.
        /// </summary>
        public sealed class DiscoverQuietIncident : RoundCommand
        {
            public PlayerId Detective { get; }
            public IncidentId IncidentId { get; }
            public IncidentTimestamp DiscoveredAt { get; }

            public DiscoverQuietIncident(
                PlayerId detective,
                IncidentId incidentId,
                IncidentTimestamp discoveredAt)
            {
                Detective = detective;
                IncidentId = incidentId;
                DiscoveredAt = discoveredAt;
            }
        }

        /// <summary>
        /// Accepts one authored Trop only after its physical search completed;
        /// the same accepted action also creates its durable Incydent.
        /// </summary>
        public sealed class AcquireAlibiClue : RoundCommand
        {
            public PlayerId Player { get; }
            public AlibiClueId ClueId { get; }
            public IncidentId IncidentId { get; }
            public IncidentKind IncidentKind { get; }
            public IncidentEffectId Effect { get; }
            public IncidentLocationId Location { get; }
            public IncidentTimestamp OccurredAt { get; }

            public AcquireAlibiClue(
                PlayerId player,
                AlibiClueId clueId,
                IncidentId incidentId,
                IncidentKind incidentKind,
                IncidentEffectId effect,
                IncidentLocationId location,
                IncidentTimestamp occurredAt)
            {
                Player = player;
                ClueId = clueId;
                IncidentId = incidentId;
                IncidentKind = incidentKind;
                Effect = effect;
                Location = location;
                OccurredAt = occurredAt;
            }
        }

        /// <summary>Completes the current common step or prepares one compatible exit.</summary>
        public sealed class PrepareEscape : RoundCommand
        {
            public PlayerId Player { get; }
            public EscapePlanId PlanId { get; }
            public EscapeStepId StepId { get; }

            public PrepareEscape(PlayerId player, EscapePlanId planId, EscapeStepId stepId)
            {
                Player = player;
                PlanId = planId;
                StepId = stepId;
            }
        }

        /// <summary>
        /// Starts the visible final action at a prepared exit and creates its
        /// immediate loud Incydent. Runtime owns the action duration.
        /// </summary>
        public sealed class BeginEscape : RoundCommand
        {
            public PlayerId Player { get; }
            public EscapePlanId PlanId { get; }
            public EscapeExitId ExitId { get; }
            public IncidentId IncidentId { get; }
            public IncidentTimestamp OccurredAt { get; }

            public BeginEscape(
                PlayerId player,
                EscapePlanId planId,
                EscapeExitId exitId,
                IncidentId incidentId,
                IncidentTimestamp occurredAt)
            {
                Player = player;
                PlanId = planId;
                ExitId = exitId;
                IncidentId = incidentId;
                OccurredAt = occurredAt;
            }
        }

        public sealed class InterruptEscape : RoundCommand
        {
            public PlayerId Player { get; }
            public EscapePlanId PlanId { get; }
            public EscapeExitId ExitId { get; }

            public InterruptEscape(PlayerId player, EscapePlanId planId, EscapeExitId exitId)
            {
                Player = player;
                PlanId = planId;
                ExitId = exitId;
            }
        }

        /// <summary>Runtime-reported completion of the currently active final action.</summary>
        public sealed class CompleteEscape : RoundCommand
        {
            public PlayerId Player { get; }
            public EscapePlanId PlanId { get; }
            public EscapeExitId ExitId { get; }

            public CompleteEscape(PlayerId player, EscapePlanId planId, EscapeExitId exitId)
            {
                Player = player;
                PlanId = planId;
                ExitId = exitId;
            }
        }
    }
}
