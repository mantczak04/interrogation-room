using System;
using System.Collections.Generic;
using System.Linq;

namespace InterrogationRoom.Domain
{
    /// <summary>
    /// The single source of Runda rules: phases, roles, Alibi access, Prywatne
    /// Cele, Limit Rundy resolution and the Egzekucja. Pure C# — no Unity, Mirror, UI or
    /// clock. Time enters only as the TimeExpired command; all randomness comes
    /// from the StartRound seed. Commands never throw for disallowed input:
    /// they return a rejection without changing state.
    /// </summary>
    public sealed class RoundEngine
    {
        public const int MinPlayers = 3;
        public const int MaxPlayers = 6;
        public const int MinPlayersForSecretObjective = 5;

        private RoundPhase _phase = RoundPhase.Lobby;
        private CaseDefinition _case;
        private PlayerId[] _players = Array.Empty<PlayerId>();
        private readonly Dictionary<PlayerId, RoundRole> _roles = new Dictionary<PlayerId, RoundRole>();
        private readonly HashSet<string> _hiddenFactIds = new HashSet<string>();
        private readonly Dictionary<string, string> _resolvedFactTexts = new Dictionary<string, string>();
        private readonly Dictionary<PlayerId, PrivateObjectiveState> _privateObjectives =
            new Dictionary<PlayerId, PrivateObjectiveState>();
        private readonly Dictionary<IncidentId, IncidentState> _incidents =
            new Dictionary<IncidentId, IncidentState>();
        private readonly List<IncidentState> _incidentOrder = new List<IncidentState>();
        private readonly List<IncidentRegistryEntryView> _incidentRegistry =
            new List<IncidentRegistryEntryView>();
        private readonly List<AlibiClueDefinition> _acquiredAlibiClues =
            new List<AlibiClueDefinition>();
        private readonly HashSet<AlibiClueId> _acquiredAlibiClueIds =
            new HashSet<AlibiClueId>();
        private readonly HashSet<PlayerId> _readyPlayers = new HashSet<PlayerId>();
        private EscapePlanState _escapePlan;
        private PlayerId? _detective;
        private PlayerId? _executedPlayer;
        private bool? _detectiveWon;
        private RoundEndCause? _endCause;
        private EscapeExitId? _successfulEscapeExit;

        public RoundTransition Handle(RoundCommand command)
        {
            switch (command)
            {
                case RoundCommand.StartRound start:
                    return HandleStartRound(start);
                case RoundCommand.MarkPlayerReady markPlayerReady:
                    return HandleMarkPlayerReady(markPlayerReady);
                case RoundCommand.EndPreparation _:
                    return HandleEndPreparation();
                case RoundCommand.Execute execute:
                    return HandleExecute(execute);
                case RoundCommand.TimeExpired _:
                    return HandleTimeExpired();
                case RoundCommand.AdvancePrivateObjective advance:
                    return HandleAdvancePrivateObjective(advance);
                case RoundCommand.RegisterIncident registerIncident:
                    return HandleRegisterIncident(registerIncident);
                case RoundCommand.DiscoverQuietIncident discoverQuietIncident:
                    return HandleDiscoverQuietIncident(discoverQuietIncident);
                case RoundCommand.AcquireAlibiClue acquireAlibiClue:
                    return HandleAcquireAlibiClue(acquireAlibiClue);
                case RoundCommand.PrepareEscape prepareEscape:
                    return HandlePrepareEscape(prepareEscape);
                case RoundCommand.BeginEscape beginEscape:
                    return HandleBeginEscape(beginEscape);
                case RoundCommand.InterruptEscape interruptEscape:
                    return HandleInterruptEscape(interruptEscape);
                case RoundCommand.CompleteEscape completeEscape:
                    return HandleCompleteEscape(completeEscape);
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

            PrivateObjectiveView privateObjective = null;
            if (_privateObjectives.TryGetValue(viewer, out var objective))
                privateObjective = BuildPrivateObjectiveView(objective);

            PlayerResultView result = null;
            if (_phase == RoundPhase.Finished)
                result = BuildResult(viewer, role);

            IReadOnlyList<IncidentRegistryEntryView> incidentRegistry = null;
            if (role == RoundRole.Detective)
                incidentRegistry = Array.AsReadOnly(_incidentRegistry.ToArray());

            IReadOnlyList<IncidentRevealView> revealedIncidents = null;
            if (_phase == RoundPhase.Finished)
            {
                revealedIncidents = Array.AsReadOnly(_incidentOrder
                    .Select(incident => new IncidentRevealView(
                        incident.Id,
                        incident.Kind,
                        incident.Effect,
                        incident.Location,
                        incident.Author))
                    .ToArray());
            }

            IReadOnlyList<AlibiClueView> acquiredAlibiClues = null;
            EscapePlanView escapePlan = null;
            if (role == RoundRole.Guilty)
            {
                acquiredAlibiClues = Array.AsReadOnly(_acquiredAlibiClues
                    .Select(clue => new AlibiClueView(clue.Id, clue.Content))
                    .ToArray());
                escapePlan = BuildEscapePlanView();
            }

            RoundRevealView roundReveal = null;
            if (_phase == RoundPhase.Finished)
                roundReveal = BuildRoundReveal(revealedIncidents);

            return new PlayerRoundView(
                viewer,
                _phase,
                role,
                _case.CrimeDescription,
                alibi,
                privateObjective,
                result,
                Array.AsReadOnly((PlayerId[])_players.Clone()),
                _detective.Value,
                incidentRegistry,
                revealedIncidents,
                acquiredAlibiClues,
                escapePlan,
                roundReveal,
                _phase == RoundPhase.Preparation ? _readyPlayers.Count : 0,
                _phase == RoundPhase.Preparation && _readyPlayers.Contains(viewer));
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
            if (facts.Count != CaseDefinition.RequiredAlibiFactCount)
                return Reject($"Case Alibi requires exactly {CaseDefinition.RequiredAlibiFactCount} facts.");
            if (facts.Any(fact => fact == null))
                return Reject("Case contains a null Alibi fact.");
            if (facts.Select(fact => fact.Id).Distinct().Count() != facts.Count)
                return Reject("Case has duplicate Alibi fact ids.");
            if (!facts.Any(fact => fact.DistinctiveDetail))
                return Reject("Case requires a charakterystycznyDetal.");
            if (!facts.Any(fact => fact.VariantTexts.Count > 1))
                return Reject("Case requires at least one rotating Alibi variant pool.");

            var hideableCount = facts.Count(fact => fact.CanBeHidden);
            if (start.Case.MinHiddenFacts < 0
                || start.Case.MinHiddenFacts > start.Case.MaxHiddenFacts
                || start.Case.MaxHiddenFacts > hideableCount)
                return Reject("Case hidden-fact range is invalid for its hideable facts.");
            if (start.Case.MaxHiddenFacts > 0
                && !facts.Any(fact => fact.CanBeHidden && !fact.DistinctiveDetail))
            {
                return Reject("Case requires a hideable non-distinctive fact.");
            }

            var clues = start.Case.AlibiClues;
            if (clues.Any(clue => clue == null)
                || clues.Select(clue => clue.Id).Distinct().Count() != clues.Count)
                return Reject("Case contains null or duplicate Alibi clues.");
            foreach (var clue in clues)
            {
                var linkedFact = facts.SingleOrDefault(fact => fact.Id == clue.LinkedFactId);
                if (linkedFact == null || !linkedFact.CanBeHidden)
                    return Reject("Every Alibi clue must link to an existing hideable fact.");
                foreach (var variant in linkedFact.VariantTexts)
                {
                    if (clue.Content.Trim().IndexOf(
                        variant.Trim(),
                        StringComparison.OrdinalIgnoreCase) >= 0)
                        return Reject("An Alibi clue cannot copy its linked fact text.");
                }
            }

            var personalMatterPool = start.PersonalMatters.Count == 0
                ? new[] { PrivateObjectiveDefinitions.PersonalMatter }
                : start.PersonalMatters.ToArray();
            if (personalMatterPool.Any(definition => definition == null))
                return Reject("Personal matter pool contains a null definition.");
            if (personalMatterPool.Select(definition => definition.Id).Distinct().Count()
                != personalMatterPool.Length)
                return Reject("Personal matter pool contains duplicate ids.");

            if (start.SecretObjectiveCount < 0)
                return Reject("SecretObjectiveCount cannot be negative.");

            int secretObjectiveCount;
            if (players.Count < MinPlayersForSecretObjective)
            {
                secretObjectiveCount = 0;
            }
            else if (!start.SecretObjectiveCount.HasValue)
            {
                secretObjectiveCount = 1;
            }
            else if (start.SecretObjectiveCount.Value <= 1)
            {
                secretObjectiveCount = start.SecretObjectiveCount.Value;
            }
            else
            {
                return Reject("Five- and six-player Rundy support 0 or 1 Sekretny Cel.");
            }

            var rng = new Random(start.Seed);

            _case = start.Case;
            _players = players.ToArray();
            _roles.Clear();
            _hiddenFactIds.Clear();
            _resolvedFactTexts.Clear();
            _privateObjectives.Clear();
            _incidents.Clear();
            _incidentOrder.Clear();
            _incidentRegistry.Clear();
            _acquiredAlibiClues.Clear();
            _acquiredAlibiClueIds.Clear();
            _readyPlayers.Clear();
            var selectedEscapePlan = start.EscapePlan
                ?? EscapePlanDefinitions.AuthoredPlans[rng.Next(EscapePlanDefinitions.AuthoredPlans.Count)];
            _escapePlan = new EscapePlanState(selectedEscapePlan);
            _executedPlayer = null;
            _detectiveWon = null;
            _endCause = null;
            _successfulEscapeExit = null;

            var pool = _players.ToList();
            var detective = TakeRandom(pool, rng);
            var guilty = TakeRandom(pool, rng);
            _detective = detective;
            _roles[detective] = RoundRole.Detective;
            _roles[guilty] = RoundRole.Guilty;
            foreach (var innocent in pool)
                _roles[innocent] = RoundRole.Innocent;

            foreach (var fact in facts)
            {
                _resolvedFactTexts[fact.Id] = fact.VariantTexts.Count == 1
                    ? fact.Text
                    : fact.VariantTexts[rng.Next(fact.VariantTexts.Count)];
            }

            var hiddenCount = rng.Next(_case.MinHiddenFacts, _case.MaxHiddenFacts + 1);
            var hideable = facts.Where(f => f.CanBeHidden).ToList();
            var hideableWithoutDetail = hideable.Where(f => !f.DistinctiveDetail).ToList();
            if (hiddenCount > 0 && hideableWithoutDetail.Count > 0)
            {
                var anchor = TakeRandom(hideableWithoutDetail, rng);
                hideable.Remove(anchor);
                _hiddenFactIds.Add(anchor.Id);
            }
            while (_hiddenFactIds.Count < hiddenCount)
                _hiddenFactIds.Add(TakeRandom(hideable, rng).Id);

            var secretOwners = new Dictionary<PlayerId, PlayerId>();
            var objectiveOwners = pool.ToList();
            for (var i = 0; i < secretObjectiveCount; i++)
            {
                var owner = TakeRandom(objectiveOwners, rng);
                var candidates = pool.Where(p => p != owner).ToList();
                secretOwners[owner] = candidates[rng.Next(candidates.Count)];
            }

            var personalMatterRotation = personalMatterPool.ToList();
            Shuffle(personalMatterRotation, rng);
            var personalMatterIndex = 0;
            foreach (var innocent in pool)
            {
                PrivateObjectiveDefinition definition;
                PlayerId? target;
                if (secretOwners.TryGetValue(innocent, out var secretTarget))
                {
                    definition = WrobienieDefinitions.Variants[
                        rng.Next(WrobienieDefinitions.Variants.Count)];
                    target = secretTarget;
                }
                else
                {
                    definition = personalMatterRotation[
                        personalMatterIndex % personalMatterRotation.Count];
                    personalMatterIndex++;
                    target = null;
                }

                _privateObjectives[innocent] = new PrivateObjectiveState(
                    definition,
                    BuildPrivateObjectiveAssignmentId(definition, innocent),
                    target);
            }

            _phase = RoundPhase.Preparation;
            return RoundTransition.Accept(BuildPublicState(), new RoundEvent.RoundStarted());
        }

        private RoundTransition HandleMarkPlayerReady(RoundCommand.MarkPlayerReady markPlayerReady)
        {
            if (_phase != RoundPhase.Preparation)
                return Reject("Gotowość is only allowed during Przygotowanie.");
            if (!_roles.ContainsKey(markPlayerReady.Player))
                return Reject("Gotowość can only be declared by a player in the Skład Rundy.");
            if (!_readyPlayers.Add(markPlayerReady.Player))
                return Reject("Gotowość has already been declared and cannot be repeated.");

            return RoundTransition.Accept(BuildPublicState());
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

        private RoundTransition HandleAdvancePrivateObjective(RoundCommand.AdvancePrivateObjective advance)
        {
            if (!TryAdvancePrivateObjective(
                    advance.Player,
                    advance.ObjectiveId,
                    advance.StepId,
                    out var objectiveEvent,
                    out var rejectionReason))
                return Reject(rejectionReason);

            return RoundTransition.Accept(
                BuildPublicState(),
                objectiveEvent);
        }

        private RoundTransition HandleRegisterIncident(RoundCommand.RegisterIncident register)
        {
            if (!TryRegisterIncident(
                register.Author,
                register.IncidentId,
                register.Kind,
                register.Effect,
                register.Location,
                register.OccurredAt,
                out var incident,
                out var rejectionReason))
                return Reject(rejectionReason);

            var events = new List<RoundEvent>
            {
                new RoundEvent.IncidentRegistered(incident.Id, incident.Kind)
            };

            if (incident.Kind == IncidentKind.Loud)
                ReportIncident(incident, incident.OccurredAt);

            if (register.ObjectiveStep != null
                && TryAdvancePrivateObjective(
                    register.Author,
                    register.ObjectiveStep.ObjectiveId,
                    register.ObjectiveStep.StepId,
                    out var objectiveEvent,
                    out _))
            {
                events.Add(objectiveEvent);
            }

            return RoundTransition.Accept(BuildPublicState(), events.ToArray());
        }

        private RoundTransition HandleDiscoverQuietIncident(RoundCommand.DiscoverQuietIncident discover)
        {
            if (_phase != RoundPhase.Round)
                return Reject("Quiet Incydenty can only be discovered during the Runda.");
            if (!_roles.TryGetValue(discover.Detective, out var role)
                || role != RoundRole.Detective)
                return Reject("Only the Detektyw can discover a quiet Incydent.");
            if (!_incidents.TryGetValue(discover.IncidentId, out var incident))
                return Reject("The quiet Incydent does not exist.");
            if (incident.Kind != IncidentKind.Quiet)
                return Reject("Only a quiet Incydent requires discovery.");
            if (incident.IsReported)
                return Reject("The quiet Incydent has already been discovered.");
            if (discover.DiscoveredAt < incident.OccurredAt)
                return Reject("An Incydent cannot be discovered before its world effect occurred.");

            ReportIncident(incident, discover.DiscoveredAt);
            return RoundTransition.Accept(
                BuildPublicState(),
                new RoundEvent.QuietIncidentDiscovered(incident.Id));
        }

        private RoundTransition HandleAcquireAlibiClue(RoundCommand.AcquireAlibiClue acquire)
        {
            if (_phase != RoundPhase.Round)
                return Reject("Tropy do Alibi can only be acquired during the Runda.");
            if (!_roles.TryGetValue(acquire.Player, out var role) || role != RoundRole.Guilty)
                return Reject("Only the Winny can acquire a Trop do Alibi.");

            var clue = _case.AlibiClues.SingleOrDefault(candidate => candidate.Id == acquire.ClueId);
            if (clue == null)
                return Reject("The requested Trop do Alibi is not part of this Sprawa.");
            if (!_hiddenFactIds.Contains(clue.LinkedFactId))
                return Reject("A Trop can only be acquired for a fact hidden in this Runda.");
            if (_acquiredAlibiClueIds.Contains(clue.Id))
                return Reject("The Trop do Alibi has already been acquired.");

            if (!TryRegisterIncident(
                acquire.Player,
                acquire.IncidentId,
                acquire.IncidentKind,
                acquire.Effect,
                acquire.Location,
                acquire.OccurredAt,
                out var incident,
                out var rejectionReason))
                return Reject(rejectionReason);

            _acquiredAlibiClueIds.Add(clue.Id);
            _acquiredAlibiClues.Add(clue);
            if (incident.Kind == IncidentKind.Loud)
                ReportIncident(incident, incident.OccurredAt);

            return RoundTransition.Accept(
                BuildPublicState(),
                new RoundEvent.IncidentRegistered(incident.Id, incident.Kind));
        }

        private RoundTransition HandlePrepareEscape(RoundCommand.PrepareEscape prepare)
        {
            if (!TryGetEscapeActor(prepare.Player, prepare.PlanId, out var rejectionReason))
                return Reject(rejectionReason);
            if (_escapePlan.ActiveExit.HasValue)
                return Reject("The active Ucieczka attempt must finish or be interrupted first.");

            if (!_escapePlan.CommonPreparationComplete)
            {
                var expected = _escapePlan.Definition.CommonSteps[_escapePlan.CompletedCommonStepCount].Id;
                if (prepare.StepId != expected)
                    return Reject("The reported step is not the current Plan Ucieczki step.");

                _escapePlan.CompletedCommonStepCount++;
                _escapePlan.Actions.Add(new EscapeActionRevealView(
                    EscapeActionKind.PreparedCommonStep,
                    stepId: prepare.StepId));
                return RoundTransition.Accept(BuildPublicState());
            }

            var exit = _escapePlan.Definition.Exits.SingleOrDefault(
                candidate => candidate.PreparationStepId == prepare.StepId);
            if (exit == null)
                return Reject("The reported step does not prepare a compatible Ucieczka exit.");
            if (!_escapePlan.PreparedExits.Add(exit.Id))
                return Reject("That Ucieczka exit is already prepared.");

            _escapePlan.Actions.Add(new EscapeActionRevealView(
                EscapeActionKind.PreparedExit,
                stepId: prepare.StepId,
                exitId: exit.Id));
            return RoundTransition.Accept(BuildPublicState());
        }

        private RoundTransition HandleBeginEscape(RoundCommand.BeginEscape begin)
        {
            if (!TryGetEscapeActor(begin.Player, begin.PlanId, out var rejectionReason))
                return Reject(rejectionReason);
            if (_escapePlan.ActiveExit.HasValue)
                return Reject("An Ucieczka attempt is already active.");
            if (!_escapePlan.CommonPreparationComplete)
                return Reject("The common Plan Ucieczki steps are incomplete.");

            var exit = _escapePlan.Definition.Exits.SingleOrDefault(candidate => candidate.Id == begin.ExitId);
            if (exit == null)
                return Reject("The requested exit is not compatible with this Plan Ucieczki.");
            if (!_escapePlan.PreparedExits.Contains(exit.Id))
                return Reject("The requested Ucieczka exit is not prepared.");

            if (!TryRegisterIncident(
                begin.Player,
                begin.IncidentId,
                IncidentKind.Loud,
                EscapePlanDefinitions.FinalEffect,
                exit.Location,
                begin.OccurredAt,
                out var incident,
                out rejectionReason))
                return Reject(rejectionReason);

            _escapePlan.ActiveExit = exit.Id;
            _escapePlan.Actions.Add(new EscapeActionRevealView(
                EscapeActionKind.AttemptStarted,
                exitId: exit.Id));
            ReportIncident(incident, incident.OccurredAt);
            return RoundTransition.Accept(
                BuildPublicState(),
                new RoundEvent.IncidentRegistered(incident.Id, incident.Kind),
                new RoundEvent.EscapeAttemptStarted(exit.Id, exit.Location));
        }

        private RoundTransition HandleInterruptEscape(RoundCommand.InterruptEscape interrupt)
        {
            if (!TryGetEscapeActor(interrupt.Player, interrupt.PlanId, out var rejectionReason))
                return Reject(rejectionReason);
            if (_escapePlan.ActiveExit != interrupt.ExitId)
                return Reject("The requested Ucieczka attempt is not active.");

            _escapePlan.ActiveExit = null;
            _escapePlan.PreparedExits.Remove(interrupt.ExitId);
            _escapePlan.Actions.Add(new EscapeActionRevealView(
                EscapeActionKind.AttemptInterrupted,
                exitId: interrupt.ExitId));
            return RoundTransition.Accept(
                BuildPublicState(),
                new RoundEvent.EscapeAttemptInterrupted(interrupt.ExitId));
        }

        private RoundTransition HandleCompleteEscape(RoundCommand.CompleteEscape complete)
        {
            if (!TryGetEscapeActor(complete.Player, complete.PlanId, out var rejectionReason))
                return Reject(rejectionReason);
            if (_escapePlan.ActiveExit != complete.ExitId)
                return Reject("The requested Ucieczka attempt is not active.");

            _escapePlan.ActiveExit = null;
            _escapePlan.SuccessfulExit = complete.ExitId;
            _escapePlan.Actions.Add(new EscapeActionRevealView(
                EscapeActionKind.Completed,
                exitId: complete.ExitId));
            _successfulEscapeExit = complete.ExitId;
            _detectiveWon = false;
            _endCause = RoundEndCause.Escape;
            _phase = RoundPhase.Finished;
            return RoundTransition.Accept(
                BuildPublicState(),
                new RoundEvent.PlayerEscaped(complete.ExitId),
                new RoundEvent.RoundEnded(false, RoundEndCause.Escape));
        }

        private bool TryGetEscapeActor(
            PlayerId player,
            EscapePlanId planId,
            out string rejectionReason)
        {
            rejectionReason = null;
            if (_phase != RoundPhase.Round)
            {
                rejectionReason = "Plan Ucieczki commands are only allowed during the Runda.";
                return false;
            }
            if (!_roles.TryGetValue(player, out var role) || role != RoundRole.Guilty)
            {
                rejectionReason = "Only the Winny can advance the Plan Ucieczki.";
                return false;
            }
            if (_escapePlan == null || _escapePlan.Definition.Id != planId)
            {
                rejectionReason = "The reported Plan Ucieczki is not assigned to this Runda.";
                return false;
            }

            return true;
        }

        private bool TryRegisterIncident(
            PlayerId author,
            IncidentId incidentId,
            IncidentKind kind,
            IncidentEffectId effect,
            IncidentLocationId location,
            IncidentTimestamp occurredAt,
            out IncidentState incident,
            out string rejectionReason)
        {
            incident = null;
            rejectionReason = null;
            if (_phase != RoundPhase.Round)
            {
                rejectionReason = "Incydenty can only be registered during the Runda.";
                return false;
            }
            if (!_roles.TryGetValue(author, out var authorRole) || authorRole == RoundRole.Detective)
            {
                rejectionReason = "Only a Podejrzany in the current Runda can author an Incydent.";
                return false;
            }
            if (kind != IncidentKind.Loud && kind != IncidentKind.Quiet)
            {
                rejectionReason = "The Incydent kind must be Loud or Quiet.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(incidentId.Value)
                || string.IsNullOrWhiteSpace(effect.Value)
                || string.IsNullOrWhiteSpace(location.Value))
            {
                rejectionReason = "The Incydent requires stable effect and location identifiers.";
                return false;
            }
            if (_incidents.ContainsKey(incidentId))
            {
                rejectionReason = "The Incydent world effect has already been registered.";
                return false;
            }

            incident = new IncidentState(incidentId, kind, effect, location, author, occurredAt);
            _incidents.Add(incident.Id, incident);
            _incidentOrder.Add(incident);
            return true;
        }

        private bool TryAdvancePrivateObjective(
            PlayerId player,
            PrivateObjectiveId objectiveId,
            PrivateObjectiveStepId stepId,
            out RoundEvent.PrivateObjectiveAdvanced objectiveEvent,
            out string rejectionReason)
        {
            objectiveEvent = null;
            rejectionReason = null;

            if (_phase != RoundPhase.Round)
            {
                rejectionReason = "Prywatny Cel progress is only allowed during the Runda.";
                return false;
            }

            if (!_roles.TryGetValue(player, out var role) || role != RoundRole.Innocent)
            {
                rejectionReason = "Only a Niewinny can advance their Prywatny Cel.";
                return false;
            }

            if (!_privateObjectives.TryGetValue(player, out var objective))
            {
                rejectionReason = "The player has no Prywatny Cel assignment.";
                return false;
            }

            if (objective.AssignmentId != objectiveId)
            {
                rejectionReason = "The reported Prywatny Cel does not belong to the player.";
                return false;
            }

            if (objective.IsCompleted)
            {
                rejectionReason = "The Prywatny Cel is already completed.";
                return false;
            }

            var expectedStep = objective.Definition.Steps[objective.CompletedStepCount].AnchorActionId;
            if (expectedStep != stepId)
            {
                rejectionReason = "The reported step is not the current Prywatny Cel step.";
                return false;
            }

            objective.CompletedStepCount++;
            objectiveEvent = new RoundEvent.PrivateObjectiveAdvanced(
                player,
                objective.AssignmentId,
                stepId,
                objective.IsCompleted);
            return true;
        }

        private void ReportIncident(IncidentState incident, IncidentTimestamp reportedAt)
        {
            incident.IsReported = true;
            _incidentRegistry.Add(new IncidentRegistryEntryView(
                incident.Id,
                incident.Kind,
                incident.Effect,
                incident.Location,
                reportedAt));
        }

        private AlibiView BuildAlibiView(RoundRole role)
        {
            var entries = new List<AlibiEntry>(_case.AlibiFacts.Count);
            foreach (var fact in _case.AlibiFacts)
            {
                var hidden = role == RoundRole.Guilty && _hiddenFactIds.Contains(fact.Id);
                entries.Add(new AlibiEntry(fact.Id, hidden, hidden ? null : _resolvedFactTexts[fact.Id]));
            }

            return new AlibiView(entries);
        }

        private static PrivateObjectiveView BuildPrivateObjectiveView(PrivateObjectiveState objective)
        {
            var currentDefinition = objective.IsCompleted
                ? null
                : objective.Definition.Steps[objective.CompletedStepCount];
            PrivateObjectiveStepId? currentStep = currentDefinition?.AnchorActionId;
            return new PrivateObjectiveView(
                objective.AssignmentId,
                objective.Definition.Kind,
                objective.Definition.Title,
                objective.Definition.Motive,
                currentStep,
                currentDefinition?.Description,
                currentDefinition?.LocationHint,
                objective.CompletedStepCount,
                objective.Definition.Steps.Count,
                objective.IsCompleted,
                objective.Target);
        }

        private EscapePlanView BuildEscapePlanView()
        {
            var currentDefinition = _escapePlan.CommonPreparationComplete
                ? null
                : _escapePlan.Definition.CommonSteps[_escapePlan.CompletedCommonStepCount];
            EscapeStepId? currentStep = currentDefinition?.Id;
            var exitOptions = _escapePlan.Definition.Exits
                .Select(exit => new EscapeExitOptionView(
                    exit.Id,
                    exit.PreparationStepId,
                    exit.Location,
                    exit.Description,
                    exit.LocationHint,
                    _escapePlan.PreparedExits.Contains(exit.Id)))
                .ToArray();
            return new EscapePlanView(
                _escapePlan.Definition.Id,
                _escapePlan.Definition.Title,
                _escapePlan.Definition.Motive,
                currentStep,
                currentDefinition?.Description,
                currentDefinition?.LocationHint,
                _escapePlan.CompletedCommonStepCount,
                _escapePlan.Definition.CommonSteps.Count,
                _escapePlan.PreparedExits.Count > 0,
                _escapePlan.ActiveExit,
                Array.AsReadOnly(exitOptions));
        }

        private RoundRevealView BuildRoundReveal(
            IReadOnlyList<IncidentRevealView> revealedIncidents)
        {
            var players = _players.Select(player =>
            {
                var role = _roles[player];
                PrivateObjectiveView objective = null;
                if (_privateObjectives.TryGetValue(player, out var objectiveState))
                    objective = BuildPrivateObjectiveView(objectiveState);
                return new PlayerEndRevealView(player, role, objective, BuildResult(player, role));
            }).ToArray();
            var clues = _acquiredAlibiClues
                .Select(clue => new AlibiClueRevealView(
                    clue.Id,
                    clue.LinkedFactId,
                    clue.Content))
                .ToArray();
            var escape = new EscapePlanRevealView(
                _escapePlan.Definition.Id,
                Array.AsReadOnly(_escapePlan.Actions.ToArray()),
                _escapePlan.SuccessfulExit);
            return new RoundRevealView(
                Array.AsReadOnly(players),
                Array.AsReadOnly(clues),
                escape,
                revealedIncidents);
        }

        private PlayerResultView BuildResult(PlayerId viewer, RoundRole role)
        {
            var survived = _executedPlayer != viewer;
            var privateObjectiveCompleted = _privateObjectives.TryGetValue(viewer, out var objective)
                && objective.IsCompleted;
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
                    won = survived
                        && privateObjectiveCompleted
                        && (objective.Definition.Kind != PrivateObjectiveKind.SecretObjective
                            || _executedPlayer == objective.Target);
                    break;
            }

            return new PlayerResultView(
                won,
                survived,
                _detectiveWon == true,
                _endCause.Value,
                _executedPlayer,
                privateObjectiveCompleted,
                escaped: role == RoundRole.Guilty && _endCause == RoundEndCause.Escape);
        }

        private RoundPublicState BuildPublicState() =>
            new RoundPublicState(
                _phase,
                _players,
                _detective,
                _executedPlayer,
                _detectiveWon,
                _endCause,
                _successfulEscapeExit,
                _phase == RoundPhase.Preparation ? _readyPlayers.Count : 0);

        private RoundTransition Reject(string reason) =>
            RoundTransition.Reject(reason, BuildPublicState());

        private static T TakeRandom<T>(List<T> pool, Random rng)
        {
            var index = rng.Next(pool.Count);
            var item = pool[index];
            pool.RemoveAt(index);
            return item;
        }

        private static void Shuffle<T>(IList<T> values, Random rng)
        {
            for (var index = values.Count - 1; index > 0; index--)
            {
                var swapIndex = rng.Next(index + 1);
                var value = values[index];
                values[index] = values[swapIndex];
                values[swapIndex] = value;
            }
        }

        private static PrivateObjectiveId BuildPrivateObjectiveAssignmentId(
            PrivateObjectiveDefinition definition,
            PlayerId owner) =>
            new PrivateObjectiveId($"{definition.Id.Value}:{owner.Value}");

        private sealed class PrivateObjectiveState
        {
            public PrivateObjectiveDefinition Definition { get; }
            public PrivateObjectiveId AssignmentId { get; }
            public PlayerId? Target { get; }
            public int CompletedStepCount { get; set; }
            public bool IsCompleted => CompletedStepCount == Definition.Steps.Count;

            public PrivateObjectiveState(
                PrivateObjectiveDefinition definition,
                PrivateObjectiveId assignmentId,
                PlayerId? target)
            {
                Definition = definition ?? throw new ArgumentNullException(nameof(definition));
                AssignmentId = assignmentId;
                Target = target;
            }
        }

        private sealed class IncidentState
        {
            public IncidentId Id { get; }
            public IncidentKind Kind { get; }
            public IncidentEffectId Effect { get; }
            public IncidentLocationId Location { get; }
            public PlayerId Author { get; }
            public IncidentTimestamp OccurredAt { get; }
            public bool IsReported { get; set; }

            public IncidentState(
                IncidentId id,
                IncidentKind kind,
                IncidentEffectId effect,
                IncidentLocationId location,
                PlayerId author,
                IncidentTimestamp occurredAt)
            {
                Id = id;
                Kind = kind;
                Effect = effect;
                Location = location;
                Author = author;
                OccurredAt = occurredAt;
            }
        }

        private sealed class EscapePlanState
        {
            public EscapePlanDefinition Definition { get; }
            public int CompletedCommonStepCount { get; set; }
            public HashSet<EscapeExitId> PreparedExits { get; } = new HashSet<EscapeExitId>();
            public EscapeExitId? ActiveExit { get; set; }
            public EscapeExitId? SuccessfulExit { get; set; }
            public List<EscapeActionRevealView> Actions { get; } =
                new List<EscapeActionRevealView>();
            public bool CommonPreparationComplete =>
                CompletedCommonStepCount == Definition.CommonSteps.Count;

            public EscapePlanState(EscapePlanDefinition definition)
            {
                Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            }
        }
    }
}
