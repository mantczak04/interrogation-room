using System;
using System.Collections.Generic;
using System.Linq;

namespace InterrogationRoom.Domain
{
    /// <summary>
    /// State-changing intention handled by <see cref="RoundEngine.Handle"/>.
    /// Closed hierarchy: the MVP commands are StartRound, EndPreparation,
    /// Execute and TimeExpired.
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

            /// <summary>Skład Rundy — 4 to 6 distinct players.</summary>
            public IReadOnlyList<PlayerId> Players { get; }

            /// <summary>Seed for role assignment and hidden-fact selection; makes the Runda replayable in tests.</summary>
            public int Seed { get; }

            /// <summary>
            /// Number of Sekretny Cel assignments. The default MVP configuration
            /// is 0; the final count is an open design question.
            /// </summary>
            public int SecretObjectiveCount { get; }

            public StartRound(CaseDefinition caseDefinition, IEnumerable<PlayerId> players, int seed, int secretObjectiveCount = 0)
            {
                Case = caseDefinition;
                Players = players?.ToArray() ?? throw new ArgumentNullException(nameof(players));
                Seed = seed;
                SecretObjectiveCount = secretObjectiveCount;
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
    }
}
