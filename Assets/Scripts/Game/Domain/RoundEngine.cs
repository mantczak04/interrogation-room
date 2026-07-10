using System;
using System.Collections.Generic;
using System.Linq;

namespace InterrogationRoom.Domain
{
    /// <summary>
    /// The single source of Runda rules: phases, roles, Alibi access, Limit
    /// Rundy resolution and the Egzekucja. Pure C# — no Unity, Mirror, UI or
    /// clock. Time enters only as the TimeExpired command; all randomness comes
    /// from the StartRound seed. Commands never throw for disallowed input:
    /// they return a rejection without changing state.
    /// </summary>
    public sealed class RoundEngine
    {
        public const int MinPlayers = 4;
        public const int MaxPlayers = 6;

        private RoundPhase _phase = RoundPhase.Lobby;
        private CaseDefinition _case;
        private PlayerId[] _players = Array.Empty<PlayerId>();
        private readonly Dictionary<PlayerId, RoundRole> _roles = new Dictionary<PlayerId, RoundRole>();
        private readonly HashSet<string> _hiddenFactIds = new HashSet<string>();
        private readonly Dictionary<PlayerId, PlayerId> _secretObjectives = new Dictionary<PlayerId, PlayerId>();
        private PlayerId? _detective;
        private PlayerId? _executedPlayer;
        private bool? _detectiveWon;
        private RoundEndCause? _endCause;

        public RoundTransition Handle(RoundCommand command)
        {
            switch (command)
            {
                case RoundCommand.StartRound start:
                    return HandleStartRound(start);
                case RoundCommand.EndPreparation _:
                    return HandleEndPreparation();
                case RoundCommand.Execute execute:
                    return HandleExecute(execute);
                case RoundCommand.TimeExpired _:
                    return HandleTimeExpired();
                case null:
                    return Reject("Command is null.");
                default:
                    return Reject($"Unknown command type: {command.GetType().Name}.");
            }
        }

        /// <summary>
        /// The only read path (ADR-0011). Returns the role- and phase-filtered
        /// view for one player, or null when the viewer is not part of the
        /// current Runda (including before StartRound).
        /// </summary>
        public PlayerRoundView ViewFor(PlayerId viewer)
        {
            if (!_roles.TryGetValue(viewer, out var role))
                return null;

            AlibiView alibi = null;
            if (_phase == RoundPhase.Preparation && role != RoundRole.Detective)
                alibi = BuildAlibiView(role);

            SecretObjectiveView secretObjective = null;
            if (_secretObjectives.TryGetValue(viewer, out var target))
                secretObjective = new SecretObjectiveView(target);

            PlayerResultView result = null;
            if (_phase == RoundPhase.Finished)
                result = BuildResult(viewer, role);

            return new PlayerRoundView(
                viewer,
                _phase,
                role,
                _case.CrimeDescription,
                alibi,
                secretObjective,
                result,
                Array.AsReadOnly((PlayerId[])_players.Clone()),
                _detective.Value);
        }

        private RoundTransition HandleStartRound(RoundCommand.StartRound start)
        {
            if (_phase != RoundPhase.Lobby)
                return Reject("StartRound is only allowed in Lobby.");
            if (start.Case == null)
                return Reject("StartRound requires a CaseDefinition.");

            var players = start.Players;
            if (players.Count < MinPlayers || players.Count > MaxPlayers)
                return Reject($"Skład Rundy requires {MinPlayers}-{MaxPlayers} players, got {players.Count}.");
            if (players.Distinct().Count() != players.Count)
                return Reject("Skład Rundy contains duplicate players.");

            var facts = start.Case.AlibiFacts;
            if (facts.Count == 0)
                return Reject("Case has no Alibi facts.");
            if (facts.Select(f => f.Id).Distinct().Count() != facts.Count)
                return Reject("Case has duplicate Alibi fact ids.");

            var hideableCount = facts.Count(f => f.CanBeHidden);
            if (start.Case.MinHiddenFacts < 0
                || start.Case.MinHiddenFacts > start.Case.MaxHiddenFacts
                || start.Case.MaxHiddenFacts > hideableCount)
                return Reject("Case hidden-fact range is invalid for its hideable facts.");

            var innocentCount = players.Count - 2;
            if (start.SecretObjectiveCount < 0 || start.SecretObjectiveCount > innocentCount)
                return Reject($"SecretObjectiveCount must be between 0 and {innocentCount}.");

            var rng = new Random(start.Seed);

            _case = start.Case;
            _players = players.ToArray();
            _roles.Clear();
            _hiddenFactIds.Clear();
            _secretObjectives.Clear();
            _executedPlayer = null;
            _detectiveWon = null;
            _endCause = null;

            var pool = _players.ToList();
            var detective = TakeRandom(pool, rng);
            var guilty = TakeRandom(pool, rng);
            _detective = detective;
            _roles[detective] = RoundRole.Detective;
            _roles[guilty] = RoundRole.Guilty;
            foreach (var innocent in pool)
                _roles[innocent] = RoundRole.Innocent;

            var hiddenCount = rng.Next(_case.MinHiddenFacts, _case.MaxHiddenFacts + 1);
            var hideable = facts.Where(f => f.CanBeHidden).ToList();
            for (var i = 0; i < hiddenCount; i++)
                _hiddenFactIds.Add(TakeRandom(hideable, rng).Id);

            var objectiveOwners = pool.ToList();
            for (var i = 0; i < start.SecretObjectiveCount; i++)
            {
                var owner = TakeRandom(objectiveOwners, rng);
                var candidates = pool.Where(p => p != owner).ToList();
                _secretObjectives[owner] = candidates[rng.Next(candidates.Count)];
            }

            _phase = RoundPhase.Preparation;
            return RoundTransition.Accept(BuildPublicState(), new RoundEvent.RoundStarted());
        }

        private RoundTransition HandleEndPreparation()
        {
            if (_phase != RoundPhase.Preparation)
                return Reject("EndPreparation is only allowed during Przygotowanie.");

            _phase = RoundPhase.Round;
            return RoundTransition.Accept(BuildPublicState(), new RoundEvent.PreparationEnded());
        }

        private RoundTransition HandleExecute(RoundCommand.Execute execute)
        {
            if (_phase != RoundPhase.Round)
                return Reject("Egzekucja is only allowed during the Runda.");
            if (!_roles.TryGetValue(execute.Target, out var targetRole))
                return Reject("Egzekucja target is not part of the Skład Rundy.");
            if (targetRole == RoundRole.Detective)
                return Reject("The Detektyw cannot be the target of the Egzekucja.");

            _executedPlayer = execute.Target;
            _detectiveWon = targetRole == RoundRole.Guilty;
            _endCause = RoundEndCause.Execution;
            _phase = RoundPhase.Finished;
            return RoundTransition.Accept(
                BuildPublicState(),
                new RoundEvent.PlayerExecuted(execute.Target),
                new RoundEvent.RoundEnded(_detectiveWon.Value, RoundEndCause.Execution));
        }

        private RoundTransition HandleTimeExpired()
        {
            if (_phase != RoundPhase.Round)
                return Reject("TimeExpired is only allowed during the Runda.");

            _detectiveWon = false;
            _endCause = RoundEndCause.TimeExpired;
            _phase = RoundPhase.Finished;
            return RoundTransition.Accept(
                BuildPublicState(),
                new RoundEvent.RoundEnded(false, RoundEndCause.TimeExpired));
        }

        private AlibiView BuildAlibiView(RoundRole role)
        {
            var entries = new List<AlibiEntry>(_case.AlibiFacts.Count);
            foreach (var fact in _case.AlibiFacts)
            {
                var hidden = role == RoundRole.Guilty && _hiddenFactIds.Contains(fact.Id);
                entries.Add(new AlibiEntry(fact.Id, hidden, hidden ? null : fact.Text));
            }

            return new AlibiView(entries);
        }

        private PlayerResultView BuildResult(PlayerId viewer, RoundRole role)
        {
            var survived = _executedPlayer != viewer;
            bool won;
            switch (role)
            {
                case RoundRole.Detective:
                    won = _detectiveWon == true;
                    break;
                case RoundRole.Guilty:
                    won = survived;
                    break;
                default:
                    won = _secretObjectives.TryGetValue(viewer, out var target)
                        ? survived && _executedPlayer == target
                        : survived;
                    break;
            }

            return new PlayerResultView(won, survived, _detectiveWon == true, _endCause.Value, _executedPlayer);
        }

        private RoundPublicState BuildPublicState() =>
            new RoundPublicState(_phase, _players, _detective, _executedPlayer, _detectiveWon, _endCause);

        private RoundTransition Reject(string reason) =>
            RoundTransition.Reject(reason, BuildPublicState());

        private static T TakeRandom<T>(List<T> pool, Random rng)
        {
            var index = rng.Next(pool.Count);
            var item = pool[index];
            pool.RemoveAt(index);
            return item;
        }
    }
}
