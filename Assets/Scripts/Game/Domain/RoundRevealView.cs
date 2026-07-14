using System;
using System.Collections.Generic;

namespace InterrogationRoom.Domain
{
    public sealed class PlayerEndRevealView
    {
        public PlayerId Player { get; }
        public RoundRole Role { get; }
        public PrivateObjectiveView PrivateObjective { get; }
        public PlayerResultView Result { get; }

        public PlayerEndRevealView(
            PlayerId player,
            RoundRole role,
            PrivateObjectiveView privateObjective,
            PlayerResultView result)
        {
            Player = player;
            Role = role;
            PrivateObjective = privateObjective;
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }
    }

    /// <summary>Complete approved post-Runda reveal, excluding unresolved full Alibi presentation.</summary>
    public sealed class RoundRevealView
    {
        public IReadOnlyList<PlayerEndRevealView> Players { get; }
        public IReadOnlyList<AlibiClueRevealView> AcquiredAlibiClues { get; }
        public EscapePlanRevealView EscapePlan { get; }
        public IReadOnlyList<IncidentRevealView> Incidents { get; }

        public RoundRevealView(
            IReadOnlyList<PlayerEndRevealView> players,
            IReadOnlyList<AlibiClueRevealView> acquiredAlibiClues,
            EscapePlanRevealView escapePlan,
            IReadOnlyList<IncidentRevealView> incidents)
        {
            Players = players ?? throw new ArgumentNullException(nameof(players));
            AcquiredAlibiClues = acquiredAlibiClues
                ?? throw new ArgumentNullException(nameof(acquiredAlibiClues));
            EscapePlan = escapePlan ?? throw new ArgumentNullException(nameof(escapePlan));
            Incidents = incidents ?? throw new ArgumentNullException(nameof(incidents));
        }
    }
}
