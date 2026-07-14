using System;
using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Domain;

namespace InterrogationRoom.Networking
{
    public enum RoundDeveloperScenario
    {
        PersonalMatter,
        SecretObjective,
        GuiltyEscape,
        DetectiveIncidents
    }

    public enum RoundDeveloperFinish
    {
        TimeExpired,
        ExecuteGuilty,
        ExecuteInnocent,
        ExecuteSecretTarget
    }

    /// <summary>
    /// Deterministic host-only setup for exercising a real Runda with fewer
    /// connected clients. Technical players exist only in the domain roster;
    /// they never become Mirror connections or receive private views.
    /// </summary>
    public sealed class RoundDeveloperPlan
    {
        public RoundDeveloperScenario Scenario { get; }
        public PlayerId ControlledPlayer { get; }
        public IReadOnlyList<PlayerId> Players { get; }
        public int Seed { get; }
        public int SecretObjectiveCount { get; }
        public int ConnectedPlayerCount { get; }
        public int TechnicalPlayerCount => Players.Count - ConnectedPlayerCount;

        public RoundDeveloperPlan(
            RoundDeveloperScenario scenario,
            PlayerId controlledPlayer,
            IReadOnlyList<PlayerId> players,
            int seed,
            int secretObjectiveCount,
            int connectedPlayerCount)
        {
            Scenario = scenario;
            ControlledPlayer = controlledPlayer;
            Players = players ?? throw new ArgumentNullException(nameof(players));
            Seed = seed;
            SecretObjectiveCount = secretObjectiveCount;
            ConnectedPlayerCount = connectedPlayerCount;
        }
    }

    public static class RoundDeveloperScenarioPlanner
    {
        private const int MaxSeedAttempts = 10000;
        private const int FirstTechnicalPlayerId = int.MinValue;

        public static bool TryCreate(
            CaseDefinition caseDefinition,
            IEnumerable<PlayerId> connectedPlayers,
            PlayerId controlledPlayer,
            int targetPlayerCount,
            RoundDeveloperScenario scenario,
            out RoundDeveloperPlan plan,
            out string rejectionReason) =>
            TryCreate(
                caseDefinition,
                connectedPlayers,
                controlledPlayer,
                targetPlayerCount,
                scenario,
                null,
                out plan,
                out rejectionReason);

        public static bool TryCreate(
            CaseDefinition caseDefinition,
            IEnumerable<PlayerId> connectedPlayers,
            PlayerId controlledPlayer,
            int targetPlayerCount,
            RoundDeveloperScenario scenario,
            AlibiClueId? requiredClueId,
            out RoundDeveloperPlan plan,
            out string rejectionReason)
        {
            plan = null;
            rejectionReason = null;

            if (caseDefinition == null)
                return Reject("A developer Runda requires a CaseDefinition.", out rejectionReason);

            var connected = connectedPlayers?.Distinct().OrderBy(player => player.Value).ToArray()
                            ?? Array.Empty<PlayerId>();
            if (connected.Length == 0)
                return Reject("Start a host and spawn at least one real player first.", out rejectionReason);
            if (!connected.Contains(controlledPlayer))
                return Reject("The controlled player must be a connected player.", out rejectionReason);
            if (targetPlayerCount < RoundEngine.MinPlayers || targetPlayerCount > RoundEngine.MaxPlayers)
                return Reject($"Developer roster must contain {RoundEngine.MinPlayers}-{RoundEngine.MaxPlayers} players.", out rejectionReason);
            if (connected.Length > targetPlayerCount)
                return Reject("The selected roster is smaller than the number of connected players.", out rejectionReason);
            if (scenario == RoundDeveloperScenario.SecretObjective
                && targetPlayerCount < RoundEngine.MinPlayersForSecretObjective)
                return Reject("Sekretny Cel is disabled for three- and four-player Rundy.", out rejectionReason);

            AlibiClueDefinition requiredClue = null;
            if (scenario == RoundDeveloperScenario.GuiltyEscape)
            {
                requiredClue = requiredClueId.HasValue
                    ? caseDefinition.AlibiClues.FirstOrDefault(clue => clue.Id == requiredClueId.Value)
                    : caseDefinition.AlibiClues.FirstOrDefault();
                if (requiredClue == null)
                    return Reject(
                        requiredClueId.HasValue
                            ? $"The selected Sprawa has no Trop '{requiredClueId.Value.Value}'."
                            : "The selected Sprawa has no authored Trop do Alibi.",
                        out rejectionReason);
            }

            var roster = connected.ToList();
            var technicalValue = FirstTechnicalPlayerId;
            while (roster.Count < targetPlayerCount)
            {
                var candidate = new PlayerId(technicalValue++);
                if (!roster.Contains(candidate))
                    roster.Add(candidate);
            }

            int secretObjectiveCount = scenario == RoundDeveloperScenario.SecretObjective ? 1 : 0;
            for (var seed = 0; seed < MaxSeedAttempts; seed++)
            {
                var preview = new RoundEngine();
                var transition = preview.Handle(new RoundCommand.StartRound(
                    caseDefinition,
                    roster,
                    seed,
                    secretObjectiveCount));
                if (!transition.Accepted)
                    return Reject(transition.RejectionReason, out rejectionReason);

                var view = preview.ViewFor(controlledPlayer);
                if (!MatchesScenario(view, scenario, requiredClue))
                    continue;

                plan = new RoundDeveloperPlan(
                    scenario,
                    controlledPlayer,
                    Array.AsReadOnly(roster.ToArray()),
                    seed,
                    secretObjectiveCount,
                    connected.Length);
                return true;
            }

            return Reject("No deterministic seed matched the selected developer scenario.", out rejectionReason);
        }

        private static bool MatchesScenario(
            PlayerRoundView view,
            RoundDeveloperScenario scenario,
            AlibiClueDefinition requiredClue)
        {
            if (view == null)
                return false;

            switch (scenario)
            {
                case RoundDeveloperScenario.PersonalMatter:
                    return view.Role == RoundRole.Innocent
                           && view.PrivateObjective?.Kind == PrivateObjectiveKind.PersonalMatter;
                case RoundDeveloperScenario.SecretObjective:
                    return view.Role == RoundRole.Innocent
                           && view.PrivateObjective?.Kind == PrivateObjectiveKind.SecretObjective;
                case RoundDeveloperScenario.GuiltyEscape:
                    return view.Role == RoundRole.Guilty
                           && view.Alibi != null
                           && view.Alibi.Entries.Any(entry =>
                               entry.IsHidden && entry.FactId == requiredClue.LinkedFactId);
                case RoundDeveloperScenario.DetectiveIncidents:
                    return view.Role == RoundRole.Detective;
                default:
                    return false;
            }
        }

        private static bool Reject(string reason, out string rejectionReason)
        {
            rejectionReason = reason;
            return false;
        }
    }
}
