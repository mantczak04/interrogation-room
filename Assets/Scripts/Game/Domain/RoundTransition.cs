using System;
using System.Collections.Generic;

namespace InterrogationRoom.Domain
{
    /// <summary>
    /// Phases of one Runda: Lobby → Przygotowanie → Runda → Zakończona.
    /// </summary>
    public enum RoundPhase
    {
        Lobby,

        /// <summary>Przygotowanie — suspects study their Alibi versions.</summary>
        Preparation,

        /// <summary>Runda — free-roaming play under the Limit Rundy.</summary>
        Round,

        /// <summary>Zakończona — only result reads are allowed.</summary>
        Finished
    }

    public enum RoundEndCause
    {
        /// <summary>Egzekucja resolved the Runda.</summary>
        Execution,

        /// <summary>The Limit Rundy expired without an Egzekucja.</summary>
        TimeExpired,

        /// <summary>The Winny completed a final Ucieczka action.</summary>
        Escape
    }

    /// <summary>
    /// Result of <see cref="RoundEngine.Handle"/>: either an accepted command
    /// with the new public state and events to broadcast, or a rejection with
    /// no state change and no events. Never throws for disallowed commands.
    /// </summary>
    public sealed class RoundTransition
    {
        public bool Accepted { get; }

        /// <summary>Adapter-facing reason; null when accepted.</summary>
        public string RejectionReason { get; }

        /// <summary>Public state after the command (unchanged on rejection). Contains no secrets.</summary>
        public RoundPublicState State { get; }

        /// <summary>Events for the adapter to broadcast; empty on rejection.</summary>
        public IReadOnlyList<RoundEvent> Events { get; }

        private RoundTransition(bool accepted, string rejectionReason, RoundPublicState state, IReadOnlyList<RoundEvent> events)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            State = state;
            Events = events;
        }

        public static RoundTransition Accept(RoundPublicState state, params RoundEvent[] events) =>
            new RoundTransition(true, null, state, events ?? Array.Empty<RoundEvent>());

        public static RoundTransition Reject(string reason, RoundPublicState state) =>
            new RoundTransition(false, reason, state, Array.Empty<RoundEvent>());
    }

    /// <summary>
    /// Snapshot of everything about the Runda that is public to all players.
    /// Roles other than the Detektyw, Alibi content and Sekretne Cele are never
    /// part of it — they travel only inside a targeted <see cref="PlayerRoundView"/>.
    /// </summary>
    public sealed class RoundPublicState
    {
        public RoundPhase Phase { get; }

        public IReadOnlyList<PlayerId> Players { get; }

        /// <summary>The Detektyw is publicly known; null before StartRound.</summary>
        public PlayerId? Detective { get; }

        public PlayerId? ExecutedPlayer { get; }

        /// <summary>Outcome of the Runda for the Detektyw; null until Zakończona.</summary>
        public bool? DetectiveWon { get; }

        public RoundEndCause? EndCause { get; }

        /// <summary>Public final exit; null unless the Runda ended by Ucieczka.</summary>
        public EscapeExitId? SuccessfulEscapeExit { get; }

        /// <summary>Public readiness count; non-zero only during Przygotowanie.</summary>
        public int ReadyPlayerCount { get; }

        public RoundPublicState(
            RoundPhase phase,
            IReadOnlyList<PlayerId> players,
            PlayerId? detective,
            PlayerId? executedPlayer,
            bool? detectiveWon,
            RoundEndCause? endCause,
            EscapeExitId? successfulEscapeExit = null,
            int readyPlayerCount = 0)
        {
            Phase = phase;
            Players = players;
            Detective = detective;
            ExecutedPlayer = executedPlayer;
            DetectiveWon = detectiveWon;
            EndCause = endCause;
            SuccessfulEscapeExit = successfulEscapeExit;
            ReadyPlayerCount = readyPlayerCount;
        }
    }

    /// <summary>Domain event to broadcast after an accepted command.</summary>
    public abstract class RoundEvent
    {
        private RoundEvent()
        {
        }

        public sealed class RoundStarted : RoundEvent
        {
        }

        public sealed class PreparationEnded : RoundEvent
        {
        }

        public sealed class PlayerExecuted : RoundEvent
        {
            public PlayerId Target { get; }

            public PlayerExecuted(PlayerId target)
            {
                Target = target;
            }
        }

        public sealed class PrivateObjectiveAdvanced : RoundEvent
        {
            public PlayerId Player { get; }
            public PrivateObjectiveId ObjectiveId { get; }
            public PrivateObjectiveStepId StepId { get; }
            public bool Completed { get; }

            public PrivateObjectiveAdvanced(
                PlayerId player,
                PrivateObjectiveId objectiveId,
                PrivateObjectiveStepId stepId,
                bool completed)
            {
                Player = player;
                ObjectiveId = objectiveId;
                StepId = stepId;
                Completed = completed;
            }
        }

        /// <summary>
        /// Public-safe notification that a world effect became an Incydent.
        /// The author remains host-only during the Runda.
        /// </summary>
        public sealed class IncidentRegistered : RoundEvent
        {
            public IncidentId IncidentId { get; }
            public IncidentKind Kind { get; }

            public IncidentRegistered(IncidentId incidentId, IncidentKind kind)
            {
                IncidentId = incidentId;
                Kind = kind;
            }
        }

        /// <summary>Public-safe notification that a quiet Incydent was discovered.</summary>
        public sealed class QuietIncidentDiscovered : RoundEvent
        {
            public IncidentId IncidentId { get; }

            public QuietIncidentDiscovered(IncidentId incidentId)
            {
                IncidentId = incidentId;
            }
        }

        /// <summary>Public-safe loud final attempt notification without an author.</summary>
        public sealed class EscapeAttemptStarted : RoundEvent
        {
            public EscapeExitId ExitId { get; }
            public IncidentLocationId Location { get; }

            public EscapeAttemptStarted(EscapeExitId exitId, IncidentLocationId location)
            {
                ExitId = exitId;
                Location = location;
            }
        }

        public sealed class EscapeAttemptInterrupted : RoundEvent
        {
            public EscapeExitId ExitId { get; }

            public EscapeAttemptInterrupted(EscapeExitId exitId)
            {
                ExitId = exitId;
            }
        }

        public sealed class PlayerEscaped : RoundEvent
        {
            public EscapeExitId ExitId { get; }

            public PlayerEscaped(EscapeExitId exitId)
            {
                ExitId = exitId;
            }
        }

        public sealed class RoundEnded : RoundEvent
        {
            public bool DetectiveWon { get; }

            public RoundEndCause Cause { get; }

            public RoundEnded(bool detectiveWon, RoundEndCause cause)
            {
                DetectiveWon = detectiveWon;
                Cause = cause;
            }
        }
    }
}
