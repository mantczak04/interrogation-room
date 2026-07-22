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

    public enum RoundDeveloperTask
    {
        PersonalMatterPrepare,
        PersonalMatterFinish,
        SecretObjectivePrepare,
        SecretObjectivePlant,
        AlibiClue,
        EscapeFindTool,
        EscapeOpenRoute,
        EscapePrepareVent,
        EscapeFinalVent,
        EscapePrepareGate,
        EscapeFinalGate
    }

    public static class RoundDeveloperTaskCatalog
    {
        private static readonly RoundDeveloperTask[] InnocentTasks =
        {
            RoundDeveloperTask.PersonalMatterPrepare,
            RoundDeveloperTask.PersonalMatterFinish,
            RoundDeveloperTask.SecretObjectivePrepare,
            RoundDeveloperTask.SecretObjectivePlant
        };

        private static readonly RoundDeveloperTask[] GuiltyTasks =
        {
            RoundDeveloperTask.AlibiClue,
            RoundDeveloperTask.EscapeFindTool,
            RoundDeveloperTask.EscapeOpenRoute,
            RoundDeveloperTask.EscapePrepareVent,
            RoundDeveloperTask.EscapeFinalVent,
            RoundDeveloperTask.EscapePrepareGate,
            RoundDeveloperTask.EscapeFinalGate
        };

        public static IReadOnlyList<RoundDeveloperTask> TasksFor(RoundRole role)
        {
            switch (role)
            {
                case RoundRole.Innocent: return InnocentTasks;
                case RoundRole.Guilty: return GuiltyTasks;
                default: return Array.Empty<RoundDeveloperTask>();
            }
        }

        public static RoundDeveloperScenario ScenarioFor(RoundDeveloperTask task)
        {
            switch (task)
            {
                case RoundDeveloperTask.PersonalMatterPrepare:
                case RoundDeveloperTask.PersonalMatterFinish:
                    return RoundDeveloperScenario.PersonalMatter;
                case RoundDeveloperTask.SecretObjectivePrepare:
                case RoundDeveloperTask.SecretObjectivePlant:
                    return RoundDeveloperScenario.SecretObjective;
                default:
                    return RoundDeveloperScenario.GuiltyEscape;
            }
        }

        public static RoundRole RoleFor(RoundDeveloperTask task) =>
            ScenarioFor(task) == RoundDeveloperScenario.GuiltyEscape
                ? RoundRole.Guilty
                : RoundRole.Innocent;

        public static RoundDeveloperTask Next(RoundDeveloperTask task)
        {
            IReadOnlyList<RoundDeveloperTask> tasks = TasksFor(RoleFor(task));
            int index = tasks.IndexOf(task);
            return tasks[(index + 1) % tasks.Count];
        }

        private static int IndexOf(
            this IReadOnlyList<RoundDeveloperTask> tasks,
            RoundDeveloperTask task)
        {
            for (var index = 0; index < tasks.Count; index++)
            {
                if (tasks[index] == task)
                    return index;
            }

            throw new ArgumentOutOfRangeException(nameof(task), task, "Unknown developer task.");
        }
    }

    public static class RoundDeveloperTaskSetup
    {
        public static bool TryPrepare(
            RoundEngine engine,
            RoundDeveloperPlan plan,
            RoundDeveloperTask task,
            out string rejectionReason)
        {
            return TryPrepare(
                engine,
                plan,
                task,
                command => engine.Handle(command).Accepted,
                out rejectionReason);
        }

        public static bool TryPrepare(
            RoundEngine engine,
            RoundDeveloperPlan plan,
            RoundDeveloperTask task,
            Func<RoundCommand, bool> submit,
            out string rejectionReason)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (submit == null)
                throw new ArgumentNullException(nameof(submit));
            if (RoundDeveloperTaskCatalog.ScenarioFor(task) != plan.Scenario)
                return Reject("The selected task does not belong to the active scenario.", out rejectionReason);
            if (!Handle(submit, new RoundCommand.EndPreparation(), out rejectionReason))
                return false;

            switch (task)
            {
                case RoundDeveloperTask.PersonalMatterFinish:
                case RoundDeveloperTask.SecretObjectivePlant:
                    return AdvanceCurrentObjective(engine, submit, plan.ControlledPlayer, out rejectionReason);

                case RoundDeveloperTask.EscapeOpenRoute:
                    return AdvanceCommonEscapeSteps(engine, submit, plan.ControlledPlayer, 1, out rejectionReason);

                case RoundDeveloperTask.EscapePrepareVent:
                case RoundDeveloperTask.EscapePrepareGate:
                    return AdvanceAllCommonEscapeSteps(engine, submit, plan.ControlledPlayer, out rejectionReason);

                case RoundDeveloperTask.EscapeFinalVent:
                    return PrepareExit(engine, submit, plan.ControlledPlayer, 0, out rejectionReason);

                case RoundDeveloperTask.EscapeFinalGate:
                    return PrepareExit(engine, submit, plan.ControlledPlayer, 1, out rejectionReason);

                default:
                    rejectionReason = null;
                    return true;
            }
        }

        private static bool AdvanceCurrentObjective(
            RoundEngine engine,
            Func<RoundCommand, bool> submit,
            PlayerId player,
            out string rejectionReason)
        {
            PrivateObjectiveView objective = engine.ViewFor(player)?.PrivateObjective;
            if (objective?.CurrentStep == null)
                return Reject("The selected player has no current Prywatny Cel step.", out rejectionReason);

            return Handle(submit, new RoundCommand.AdvancePrivateObjective(
                player,
                objective.Id,
                objective.CurrentStep.Value), out rejectionReason);
        }

        private static bool AdvanceAllCommonEscapeSteps(
            RoundEngine engine,
            Func<RoundCommand, bool> submit,
            PlayerId player,
            out string rejectionReason)
        {
            while (engine.ViewFor(player)?.EscapePlan?.CurrentStep != null)
            {
                if (!AdvanceCommonEscapeSteps(engine, submit, player, 1, out rejectionReason))
                    return false;
            }

            rejectionReason = null;
            return true;
        }

        private static bool AdvanceCommonEscapeSteps(
            RoundEngine engine,
            Func<RoundCommand, bool> submit,
            PlayerId player,
            int count,
            out string rejectionReason)
        {
            for (var index = 0; index < count; index++)
            {
                EscapePlanView plan = engine.ViewFor(player)?.EscapePlan;
                if (plan?.CurrentStep == null)
                    return Reject("The selected player has no current Plan Ucieczki step.", out rejectionReason);
                if (!Handle(submit, new RoundCommand.PrepareEscape(
                    player,
                    plan.Id,
                    plan.CurrentStep.Value), out rejectionReason))
                    return false;
            }

            rejectionReason = null;
            return true;
        }

        private static bool PrepareExit(
            RoundEngine engine,
            Func<RoundCommand, bool> submit,
            PlayerId player,
            int exitIndex,
            out string rejectionReason)
        {
            if (!AdvanceAllCommonEscapeSteps(engine, submit, player, out rejectionReason))
                return false;

            EscapePlanView plan = engine.ViewFor(player)?.EscapePlan;
            if (plan == null || exitIndex < 0 || exitIndex >= plan.ExitOptions.Count)
                return Reject("The selected Plan Ucieczki exit is unavailable.", out rejectionReason);

            return Handle(submit, new RoundCommand.PrepareEscape(
                player,
                plan.Id,
                plan.ExitOptions[exitIndex].PreparationStepId), out rejectionReason);
        }

        private static bool Handle(
            Func<RoundCommand, bool> submit,
            RoundCommand command,
            out string rejectionReason)
        {
            bool accepted = submit(command);
            rejectionReason = accepted ? null : "RoundEngine rejected the developer task setup.";
            return accepted;
        }

        private static bool Reject(string reason, out string rejectionReason)
        {
            rejectionReason = reason;
            return false;
        }
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
