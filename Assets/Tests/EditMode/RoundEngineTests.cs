using System.Linq;
using InterrogationRoom.Domain;
using NUnit.Framework;

namespace InterrogationRoom.Domain.Tests
{
    /// <summary>
    /// Edit Mode tests for the RoundEngine public interface only — the test
    /// list approved in docs/architecture/MVP-ARCHITECTURE.md plus the Alibi
    /// redaction acceptance criteria. No UnityEngine, no assets.
    /// </summary>
    public sealed class RoundEngineTests
    {
        private static readonly PlayerId[] FivePlayers =
            { new PlayerId(1), new PlayerId(2), new PlayerId(3), new PlayerId(4), new PlayerId(5) };

        private static CaseDefinition TestCase(int minHidden = 2, int maxHidden = 2) =>
            new CaseDefinition(
                "Testowa Sprawa",
                "Ktoś pomalował ratuszowy zegar na różowo.",
                new[]
                {
                    new AlibiFact("f1", "O 18:00 grupa spotkała się przy fontannie.", false),
                    new AlibiFact("f2", "Kelner wylał zupę na obrus.", true),
                    new AlibiFact("f3", "Wszyscy śpiewali sto lat panu Henrykowi.", true),
                    new AlibiFact("f4", "Ktoś zgubił klucze pod stołem.", true),
                    new AlibiFact("f5", "Grupa wróciła tramwajem numer 12.", false),
                    new AlibiFact(
                        "f6",
                        "Na przystanku padał deszcz.",
                        false,
                        new[]
                        {
                            "Na przystanku padał deszcz.",
                            "Na przystanku padała drobna mżawka."
                        },
                        distinctiveDetail: true)
                },
                minHidden,
                maxHidden,
                new[]
                {
                    new AlibiClueDefinition(
                        new AlibiClueId("clue-f2"),
                        "f2",
                        "Paragon: dwie zupy naliczone o 18:17."),
                    new AlibiClueDefinition(
                        new AlibiClueId("clue-f3"),
                        "f3",
                        "Rozmazane zdjęcie tortu z zapaloną cyfrą siedem."),
                    new AlibiClueDefinition(
                        new AlibiClueId("clue-f4"),
                        "f4",
                        "Wiadomość: sprawdź jeszcze raz pod długim stołem.")
                });

        private static RoundEngine StartedEngine(int seed = 7, int? secretObjectives = null, CaseDefinition caseDefinition = null)
        {
            var engine = new RoundEngine();
            var transition = engine.Handle(
                new RoundCommand.StartRound(caseDefinition ?? TestCase(), FivePlayers, seed, secretObjectives));
            Assert.That(transition.Accepted, Is.True, transition.RejectionReason);
            return engine;
        }

        private static RoundEngine StartedEngine(
            PlayerId[] players,
            int seed = 7,
            int? secretObjectives = null)
        {
            var engine = new RoundEngine();
            var transition = engine.Handle(new RoundCommand.StartRound(TestCase(), players, seed, secretObjectives));
            Assert.That(transition.Accepted, Is.True, transition.RejectionReason);
            return engine;
        }

        private static PlayerId FindByRole(RoundEngine engine, RoundRole role) =>
            FivePlayers.First(p => engine.ViewFor(p).Role == role);

        // 1. Poprawny Skład Rundy zawsze ma jednego Detektywa i jednego Winnego.
        [Test]
        public void StartRound_ValidComposition_HasExactlyOneDetectiveAndOneGuilty([Range(0, 19)] int seed)
        {
            var engine = StartedEngine(seed);

            var roles = FivePlayers.Select(p => engine.ViewFor(p).Role).ToList();

            Assert.That(roles.Count(r => r == RoundRole.Detective), Is.EqualTo(1));
            Assert.That(roles.Count(r => r == RoundRole.Guilty), Is.EqualTo(1));
            Assert.That(roles.Count(r => r == RoundRole.Innocent), Is.EqualTo(3));
        }

        [Test]
        public void StartRound_ThreePlayers_HasOnePlayerInEachRole([Range(0, 19)] int seed)
        {
            var players = Enumerable.Range(1, 3).Select(value => new PlayerId(value)).ToArray();
            var engine = StartedEngine(players, seed);

            var roles = players.Select(player => engine.ViewFor(player).Role).ToList();

            Assert.That(roles.Count(role => role == RoundRole.Detective), Is.EqualTo(1));
            Assert.That(roles.Count(role => role == RoundRole.Guilty), Is.EqualTo(1));
            Assert.That(roles.Count(role => role == RoundRole.Innocent), Is.EqualTo(1));
        }

        [Test]
        public void StartRound_SameSeed_IsDeterministic()
        {
            var first = StartedEngine(seed: 42);
            var second = StartedEngine(seed: 42);

            foreach (var player in FivePlayers)
            {
                Assert.That(second.ViewFor(player).Role, Is.EqualTo(first.ViewFor(player).Role));

                var firstAlibi = first.ViewFor(player).Alibi;
                var secondAlibi = second.ViewFor(player).Alibi;
                if (firstAlibi == null)
                {
                    Assert.That(secondAlibi, Is.Null);
                    continue;
                }

                Assert.That(
                    secondAlibi.Entries.Select(e => e.IsHidden),
                    Is.EqualTo(firstAlibi.Entries.Select(e => e.IsHidden)));
                Assert.That(
                    secondAlibi.Entries.Select(e => e.Text),
                    Is.EqualTo(firstAlibi.Entries.Select(e => e.Text)));

                Assert.That(second.ViewFor(player).PrivateObjective?.Kind,
                    Is.EqualTo(first.ViewFor(player).PrivateObjective?.Kind));
                Assert.That(second.ViewFor(player).PrivateObjective?.Id,
                    Is.EqualTo(first.ViewFor(player).PrivateObjective?.Id));
                Assert.That(second.ViewFor(player).PrivateObjective?.Target,
                    Is.EqualTo(first.ViewFor(player).PrivateObjective?.Target));
            }
        }

        [Test]
        public void StartRound_InvalidComposition_IsRejected()
        {
            var tooFew = new RoundEngine().Handle(new RoundCommand.StartRound(
                TestCase(), FivePlayers.Take(2), seed: 1));
            var tooMany = new RoundEngine().Handle(new RoundCommand.StartRound(
                TestCase(), Enumerable.Range(1, 7).Select(i => new PlayerId(i)), seed: 1));
            var duplicates = new RoundEngine().Handle(new RoundCommand.StartRound(
                TestCase(), new[] { new PlayerId(1), new PlayerId(1), new PlayerId(2), new PlayerId(3) }, seed: 1));

            Assert.That(tooFew.Accepted, Is.False);
            Assert.That(tooMany.Accepted, Is.False);
            Assert.That(duplicates.Accepted, Is.False);
            Assert.That(tooFew.State.Phase, Is.EqualTo(RoundPhase.Lobby));
            Assert.That(tooFew.Events, Is.Empty);
        }

        [Test]
        public void StartRound_CaseUnableToHideRequiredFacts_IsRejected()
        {
            var impossibleCase = TestCase(minHidden: 4, maxHidden: 4); // only 3 facts are hideable

            var transition = new RoundEngine().Handle(
                new RoundCommand.StartRound(impossibleCase, FivePlayers, seed: 1));

            Assert.That(transition.Accepted, Is.False);
        }

        [Test]
        public void StartRound_CaseWithoutExactlySixFacts_IsRejected()
        {
            var source = TestCase();
            var fiveFacts = new CaseDefinition(
                source.Title,
                source.CrimeDescription,
                source.AlibiFacts.Take(5),
                source.MinHiddenFacts,
                source.MaxHiddenFacts,
                source.AlibiClues);

            var transition = new RoundEngine().Handle(
                new RoundCommand.StartRound(fiveFacts, FivePlayers, seed: 1));

            Assert.That(transition.Accepted, Is.False);
            Assert.That(transition.RejectionReason, Does.Contain("exactly 6"));
        }

        [Test]
        public void StartRound_CaseWithoutControlledVariantOrDistinctiveDetail_IsRejected()
        {
            var source = TestCase();
            var fixedFacts = source.AlibiFacts
                .Select(fact => new AlibiFact(fact.Id, fact.Text, fact.CanBeHidden))
                .ToArray();
            var invalid = new CaseDefinition(
                source.Title,
                source.CrimeDescription,
                fixedFacts,
                source.MinHiddenFacts,
                source.MaxHiddenFacts,
                source.AlibiClues);

            var transition = new RoundEngine().Handle(
                new RoundCommand.StartRound(invalid, FivePlayers, seed: 1));

            Assert.That(transition.Accepted, Is.False);
        }

        // 2. Każdy gracz otrzymuje wyłącznie dozwolone informacje.
        [Test]
        public void ViewFor_DuringPreparation_FiltersInformationByRole()
        {
            var engine = StartedEngine();
            var detective = FindByRole(engine, RoundRole.Detective);
            var guilty = FindByRole(engine, RoundRole.Guilty);
            var innocent = FindByRole(engine, RoundRole.Innocent);

            var detectiveView = engine.ViewFor(detective);
            var guiltyView = engine.ViewFor(guilty);
            var innocentView = engine.ViewFor(innocent);

            // Detektyw: no facts, no markers, not even a fact count.
            Assert.That(detectiveView.Alibi, Is.Null);

            // Niewinny: the complete Alibi, nothing hidden.
            Assert.That(innocentView.Alibi.Entries.Count, Is.EqualTo(6));
            Assert.That(innocentView.Alibi.Entries.All(e => !e.IsHidden && e.Text != null), Is.True);

            // Winny: same ordered structure with redaction holes.
            Assert.That(guiltyView.Alibi.Entries.Count, Is.EqualTo(6));

            // Przestępstwo is public for everyone.
            Assert.That(detectiveView.CrimeDescription, Is.Not.Empty);
            Assert.That(guiltyView.CrimeDescription, Is.EqualTo(detectiveView.CrimeDescription));
            Assert.That(innocentView.CrimeDescription, Is.EqualTo(detectiveView.CrimeDescription));
        }

        [Test]
        public void ViewFor_ExposesOnlyPublicRosterAndDetectiveIdentity()
        {
            var engine = StartedEngine();
            var detective = FindByRole(engine, RoundRole.Detective);

            foreach (var viewer in FivePlayers)
            {
                var view = engine.ViewFor(viewer);

                Assert.That(view.Players, Is.EqualTo(FivePlayers));
                Assert.That(view.Detective, Is.EqualTo(detective));
                Assert.That(view.Players.Distinct().Count(), Is.EqualTo(FivePlayers.Length));
            }
        }

        [Test]
        public void ViewFor_UnknownPlayerOrLobby_ReturnsNull()
        {
            Assert.That(new RoundEngine().ViewFor(new PlayerId(1)), Is.Null);
            Assert.That(StartedEngine().ViewFor(new PlayerId(99)), Is.Null);
        }

        // 3. Winny widzi dokładnie skonfigurowane braki w Alibi.
        [Test]
        public void GuiltyView_HasExactlyConfiguredHiddenFacts_OnlyAmongHideable([Range(0, 9)] int seed)
        {
            var engine = StartedEngine(seed);
            var guiltyView = engine.ViewFor(FindByRole(engine, RoundRole.Guilty));
            var hideableIds = new[] { "f2", "f3", "f4" };

            var hidden = guiltyView.Alibi.Entries.Where(e => e.IsHidden).ToList();

            Assert.That(hidden.Count, Is.EqualTo(2), "case configures exactly 2 hidden facts");
            Assert.That(hidden.All(e => hideableIds.Contains(e.FactId)), Is.True);
            Assert.That(hidden.All(e => e.Text == null), Is.True, "hidden entries carry no text");
        }

        [Test]
        public void GuiltyView_VisiblePlusHiddenEqualsFullAlibi()
        {
            var engine = StartedEngine();
            var innocentEntries = engine.ViewFor(FindByRole(engine, RoundRole.Innocent)).Alibi.Entries;
            var guiltyEntries = engine.ViewFor(FindByRole(engine, RoundRole.Guilty)).Alibi.Entries;

            // Same base content in the same order (ADR-0006).
            Assert.That(
                guiltyEntries.Select(e => e.FactId),
                Is.EqualTo(innocentEntries.Select(e => e.FactId)));
            foreach (var (guiltyEntry, innocentEntry) in guiltyEntries.Zip(innocentEntries, (g, i) => (g, i)))
            {
                if (!guiltyEntry.IsHidden)
                    Assert.That(guiltyEntry.Text, Is.EqualTo(innocentEntry.Text));
            }
        }

        // 4. Po Przygotowaniu żaden Podejrzany nie otrzymuje treści Alibi.
        [Test]
        public void AfterEndPreparation_NoSuspectReceivesAlibi()
        {
            var engine = StartedEngine();

            var transition = engine.Handle(new RoundCommand.EndPreparation());

            Assert.That(transition.Accepted, Is.True);
            Assert.That(transition.State.Phase, Is.EqualTo(RoundPhase.Round));
            foreach (var player in FivePlayers)
                Assert.That(engine.ViewFor(player).Alibi, Is.Null);
        }

        [Test]
        public void AfterRoundEnds_AlibiStaysUnavailable()
        {
            var engine = StartedEngine();
            engine.Handle(new RoundCommand.EndPreparation());
            engine.Handle(new RoundCommand.TimeExpired());

            foreach (var player in FivePlayers)
                Assert.That(engine.ViewFor(player).Alibi, Is.Null);
        }

        [Test]
        public void MarkPlayerReady_IsAcceptedOnlyDuringPreparation()
        {
            var lobbyTransition = new RoundEngine().Handle(
                new RoundCommand.MarkPlayerReady(FivePlayers[0]));

            var engine = StartedEngine();
            var preparationTransition = engine.Handle(
                new RoundCommand.MarkPlayerReady(FivePlayers[0]));
            engine.Handle(new RoundCommand.EndPreparation());
            var roundTransition = engine.Handle(
                new RoundCommand.MarkPlayerReady(FivePlayers[1]));

            Assert.That(lobbyTransition.Accepted, Is.False);
            Assert.That(preparationTransition.Accepted, Is.True);
            Assert.That(preparationTransition.State.ReadyPlayerCount, Is.EqualTo(1));
            Assert.That(roundTransition.Accepted, Is.False);
        }

        [Test]
        public void MarkPlayerReady_UnknownPlayer_IsRejected()
        {
            var engine = StartedEngine();

            var transition = engine.Handle(new RoundCommand.MarkPlayerReady(new PlayerId(99)));

            Assert.That(transition.Accepted, Is.False);
            Assert.That(transition.State.ReadyPlayerCount, Is.Zero);
        }

        [Test]
        public void MarkPlayerReady_IsIrreversibleAndRejectsRepeats()
        {
            var engine = StartedEngine();
            Assert.That(engine.Handle(new RoundCommand.MarkPlayerReady(FivePlayers[0])).Accepted, Is.True);

            var repeated = engine.Handle(new RoundCommand.MarkPlayerReady(FivePlayers[0]));

            Assert.That(repeated.Accepted, Is.False);
            Assert.That(repeated.State.ReadyPlayerCount, Is.EqualTo(1),
                "A rejected repeat neither clears nor doubles the declared Gotowość.");
            Assert.That(engine.ViewFor(FivePlayers[0]).IsReady, Is.True);
        }

        [Test]
        public void MarkPlayerReady_AllPlayersReady_IsVisibleInPublicStateAndViews()
        {
            var engine = StartedEngine();

            RoundTransition last = null;
            foreach (var player in FivePlayers)
                last = engine.Handle(new RoundCommand.MarkPlayerReady(player));

            Assert.That(last.Accepted, Is.True);
            Assert.That(last.State.ReadyPlayerCount, Is.EqualTo(FivePlayers.Length));
            foreach (var player in FivePlayers)
            {
                Assert.That(engine.ViewFor(player).IsReady, Is.True);
                Assert.That(engine.ViewFor(player).ReadyPlayerCount, Is.EqualTo(FivePlayers.Length));
            }
        }

        [Test]
        public void MarkPlayerReady_ReadyStateIsNotExposedAfterPreparation()
        {
            var engine = StartedEngine();
            foreach (var player in FivePlayers)
                engine.Handle(new RoundCommand.MarkPlayerReady(player));

            var endTransition = engine.Handle(new RoundCommand.EndPreparation());

            Assert.That(endTransition.State.ReadyPlayerCount, Is.Zero);
            foreach (var player in FivePlayers)
            {
                Assert.That(engine.ViewFor(player).IsReady, Is.False);
                Assert.That(engine.ViewFor(player).ReadyPlayerCount, Is.Zero);
            }
        }

        // 5. Egzekucja Winnego daje zwycięstwo Detektywowi.
        [Test]
        public void ExecutingGuilty_DetectiveWins()
        {
            var engine = StartedEngine();
            var detective = FindByRole(engine, RoundRole.Detective);
            var guilty = FindByRole(engine, RoundRole.Guilty);
            engine.Handle(new RoundCommand.EndPreparation());

            var transition = engine.Handle(new RoundCommand.Execute(guilty));

            Assert.That(transition.Accepted, Is.True);
            Assert.That(transition.State.Phase, Is.EqualTo(RoundPhase.Finished));
            Assert.That(transition.State.DetectiveWon, Is.True);
            Assert.That(transition.Events.OfType<RoundEvent.PlayerExecuted>().Single().Target, Is.EqualTo(guilty));
            Assert.That(transition.Events.OfType<RoundEvent.RoundEnded>().Single().DetectiveWon, Is.True);
            Assert.That(engine.ViewFor(detective).Result.Won, Is.True);
            Assert.That(engine.ViewFor(guilty).Result.Won, Is.False);
            Assert.That(engine.ViewFor(guilty).Result.Survived, Is.False);
        }

        // 6. Egzekucja Niewinnego daje przegraną Detektywowi i kończy Rundę.
        [Test]
        public void ExecutingInnocent_DetectiveLosesAndRoundEnds()
        {
            var engine = StartedEngine();
            var detective = FindByRole(engine, RoundRole.Detective);
            var guilty = FindByRole(engine, RoundRole.Guilty);
            var innocent = FindByRole(engine, RoundRole.Innocent);
            engine.Handle(new RoundCommand.EndPreparation());

            var transition = engine.Handle(new RoundCommand.Execute(innocent));

            Assert.That(transition.Accepted, Is.True);
            Assert.That(transition.State.Phase, Is.EqualTo(RoundPhase.Finished));
            Assert.That(transition.State.DetectiveWon, Is.False);
            Assert.That(engine.ViewFor(detective).Result.Won, Is.False);
            Assert.That(engine.ViewFor(innocent).Result.Won, Is.False, "executed Niewinny loses Przetrwanie");
            Assert.That(engine.ViewFor(guilty).Result.Won, Is.True, "surviving Winny wins");
        }

        // 7. Druga Egzekucja oraz komendy po zakończeniu Rundy są odrzucane.
        [Test]
        public void SecondExecutionAndPostRoundCommands_AreRejected()
        {
            var engine = StartedEngine();
            var guilty = FindByRole(engine, RoundRole.Guilty);
            var innocent = FindByRole(engine, RoundRole.Innocent);
            engine.Handle(new RoundCommand.EndPreparation());
            engine.Handle(new RoundCommand.Execute(innocent));

            var secondExecution = engine.Handle(new RoundCommand.Execute(guilty));
            var endPreparation = engine.Handle(new RoundCommand.EndPreparation());
            var timeExpired = engine.Handle(new RoundCommand.TimeExpired());
            var restart = engine.Handle(new RoundCommand.StartRound(TestCase(), FivePlayers, seed: 1));

            Assert.That(secondExecution.Accepted, Is.False);
            Assert.That(endPreparation.Accepted, Is.False);
            Assert.That(timeExpired.Accepted, Is.False);
            Assert.That(restart.Accepted, Is.False);

            // Rejections change nothing: the first Egzekucja still stands.
            Assert.That(secondExecution.State.Phase, Is.EqualTo(RoundPhase.Finished));
            Assert.That(secondExecution.State.ExecutedPlayer, Is.EqualTo(innocent));
            Assert.That(secondExecution.Events, Is.Empty);
        }

        // 8. Upływ Limitu Rundy bez Egzekucji kończy Rundę przegraną Detektywa.
        [Test]
        public void TimeExpiredWithoutExecution_DetectiveLoses()
        {
            var engine = StartedEngine();
            var detective = FindByRole(engine, RoundRole.Detective);
            var guilty = FindByRole(engine, RoundRole.Guilty);
            engine.Handle(new RoundCommand.EndPreparation());

            var transition = engine.Handle(new RoundCommand.TimeExpired());

            Assert.That(transition.Accepted, Is.True);
            Assert.That(transition.State.Phase, Is.EqualTo(RoundPhase.Finished));
            Assert.That(transition.State.DetectiveWon, Is.False);
            Assert.That(transition.State.EndCause, Is.EqualTo(RoundEndCause.TimeExpired));
            Assert.That(engine.ViewFor(detective).Result.Won, Is.False);
            Assert.That(engine.ViewFor(guilty).Result.Won, Is.True);
            foreach (var innocent in FivePlayers.Where(p => engine.ViewFor(p).Role == RoundRole.Innocent))
            {
                Assert.That(engine.ViewFor(innocent).Result.Survived, Is.True);
                Assert.That(engine.ViewFor(innocent).Result.Won, Is.False,
                    "Przetrwanie without a completed Prywatny Cel is not a win");
            }
        }

        [Test]
        public void Execute_DetectiveOrUnknownTargetOrWrongPhase_IsRejected()
        {
            var engine = StartedEngine();
            var detective = FindByRole(engine, RoundRole.Detective);
            var guilty = FindByRole(engine, RoundRole.Guilty);

            var duringPreparation = engine.Handle(new RoundCommand.Execute(guilty));
            engine.Handle(new RoundCommand.EndPreparation());
            var selfExecution = engine.Handle(new RoundCommand.Execute(detective));
            var unknownTarget = engine.Handle(new RoundCommand.Execute(new PlayerId(99)));

            Assert.That(duringPreparation.Accepted, Is.False);
            Assert.That(selfExecution.Accepted, Is.False);
            Assert.That(unknownTarget.Accepted, Is.False);
            Assert.That(unknownTarget.State.Phase, Is.EqualTo(RoundPhase.Round));
        }

        // Prywatne Cele: mandatory assignment, sequential progress, privacy and individual results.
        [Test]
        public void StartRound_EveryInnocentGetsExactlyOnePrivateObjective()
        {
            var engine = StartedEngine();

            foreach (var player in FivePlayers)
            {
                var view = engine.ViewFor(player);
                if (view.Role == RoundRole.Innocent)
                    Assert.That(view.PrivateObjective, Is.Not.Null, player.ToString());
                else
                    Assert.That(view.PrivateObjective, Is.Null, player.ToString());
            }
        }

        [TestCase(3, 0)]
        [TestCase(4, 0)]
        [TestCase(5, 1)]
        [TestCase(6, 1)]
        public void StartRound_DefaultSecretObjectiveCount_DependsOnPlayerCount(int playerCount, int expectedCount)
        {
            var players = Enumerable.Range(1, playerCount).Select(value => new PlayerId(value)).ToArray();
            var engine = StartedEngine(players);

            var secretCount = players
                .Select(player => engine.ViewFor(player))
                .Count(view => view.PrivateObjective?.Kind == PrivateObjectiveKind.SecretObjective);

            Assert.That(secretCount, Is.EqualTo(expectedCount));
        }

        [Test]
        public void StartRound_HostCanDisableSecretObjectiveForFiveOrSixPlayers()
        {
            foreach (var playerCount in new[] { 5, 6 })
            {
                var players = Enumerable.Range(1, playerCount).Select(value => new PlayerId(value)).ToArray();
                var engine = StartedEngine(players, secretObjectives: 0);

                Assert.That(players.Select(player => engine.ViewFor(player))
                    .Count(view => view.PrivateObjective?.Kind == PrivateObjectiveKind.SecretObjective), Is.Zero);
                Assert.That(players.Select(player => engine.ViewFor(player))
                    .Where(view => view.Role == RoundRole.Innocent)
                    .All(view => view.PrivateObjective.Kind == PrivateObjectiveKind.PersonalMatter), Is.True);
            }
        }

        [Test]
        public void StartRound_ThreeOrFourPlayersNeverGetSecretObjectiveEvenWhenRequested()
        {
            foreach (var playerCount in new[] { 3, 4 })
            {
                var players = Enumerable.Range(1, playerCount).Select(value => new PlayerId(value)).ToArray();

                var engine = StartedEngine(players, secretObjectives: 1);

                Assert.That(players.Select(player => engine.ViewFor(player))
                    .Any(view => view.PrivateObjective?.Kind == PrivateObjectiveKind.SecretObjective), Is.False);
            }
        }

        [Test]
        public void SecretObjective_AssignedToInnocent_TargetsAnotherInnocentWithoutInformingTarget()
        {
            var engine = StartedEngine();

            var owners = FivePlayers
                .Select(p => engine.ViewFor(p))
                .Where(v => v.PrivateObjective?.Kind == PrivateObjectiveKind.SecretObjective)
                .ToList();

            Assert.That(owners.Count, Is.EqualTo(1));
            Assert.That(owners[0].Role, Is.EqualTo(RoundRole.Innocent));
            var target = owners[0].PrivateObjective.Target.Value;
            Assert.That(target, Is.Not.EqualTo(owners[0].Viewer));
            Assert.That(engine.ViewFor(target).Role, Is.EqualTo(RoundRole.Innocent), "Cel is always a Niewinny");
            Assert.That(engine.ViewFor(target).PrivateObjective.Target, Is.Null,
                "the designated Cel must not learn about the assignment");
        }

        [Test]
        public void AdvancePrivateObjective_RequiresCurrentStepInOrder()
        {
            var engine = StartedEngine(secretObjectives: 0);
            var owner = FivePlayers.First(player => engine.ViewFor(player).Role == RoundRole.Innocent);
            engine.Handle(new RoundCommand.EndPreparation());
            var objective = engine.ViewFor(owner).PrivateObjective;
            var secondStep = PrivateObjectiveDefinitions.PersonalMatter.Steps[1].Id;

            var skipped = engine.Handle(new RoundCommand.AdvancePrivateObjective(owner, objective.Id, secondStep));
            var first = engine.Handle(new RoundCommand.AdvancePrivateObjective(owner, objective.Id, objective.CurrentStep.Value));
            var afterFirst = engine.ViewFor(owner).PrivateObjective;
            var repeated = engine.Handle(new RoundCommand.AdvancePrivateObjective(
                owner,
                objective.Id,
                objective.CurrentStep.Value));
            var second = engine.Handle(new RoundCommand.AdvancePrivateObjective(owner, objective.Id, afterFirst.CurrentStep.Value));

            Assert.That(skipped.Accepted, Is.False);
            Assert.That(first.Accepted, Is.True);
            Assert.That(afterFirst.CompletedStepCount, Is.EqualTo(1));
            Assert.That(afterFirst.CurrentStep, Is.EqualTo(secondStep));
            Assert.That(repeated.Accepted, Is.False);
            Assert.That(second.Accepted, Is.True);
            Assert.That(engine.ViewFor(owner).PrivateObjective.IsCompleted, Is.True);
            Assert.That(engine.ViewFor(owner).PrivateObjective.CurrentStep, Is.Null);
        }

        [Test]
        public void AdvancePrivateObjective_CannotAdvanceAnotherPlayersObjective()
        {
            var engine = StartedEngine();
            var owner = FivePlayers.Select(player => engine.ViewFor(player))
                .Single(view => view.PrivateObjective?.Kind == PrivateObjectiveKind.SecretObjective);
            var other = FivePlayers.Select(player => engine.ViewFor(player))
                .First(view => view.Role == RoundRole.Innocent && view.Viewer != owner.Viewer);
            engine.Handle(new RoundCommand.EndPreparation());

            var transition = engine.Handle(new RoundCommand.AdvancePrivateObjective(
                other.Viewer,
                owner.PrivateObjective.Id,
                owner.PrivateObjective.CurrentStep.Value));

            Assert.That(transition.Accepted, Is.False);
            Assert.That(engine.ViewFor(owner.Viewer).PrivateObjective.CompletedStepCount, Is.Zero);
            Assert.That(engine.ViewFor(other.Viewer).PrivateObjective.CompletedStepCount, Is.Zero);
        }

        [Test]
        public void PrivateObjective_IsVisibleOnlyInOwnersView()
        {
            var engine = StartedEngine();
            var owner = FivePlayers.Select(player => engine.ViewFor(player))
                .Single(view => view.PrivateObjective?.Kind == PrivateObjectiveKind.SecretObjective);
            var target = engine.ViewFor(owner.PrivateObjective.Target.Value);
            var detective = engine.ViewFor(FindByRole(engine, RoundRole.Detective));

            Assert.That(owner.PrivateObjective.Target, Is.Not.Null);
            Assert.That(target.PrivateObjective.Target, Is.Null);
            Assert.That(detective.PrivateObjective, Is.Null);
            Assert.That(target.PrivateObjective.Id, Is.Not.EqualTo(owner.PrivateObjective.Id));
        }

        [Test]
        public void PersonalMatter_ResultRequiresCompletionAndSurvival()
        {
            var incompleteEngine = StartedEngine(secretObjectives: 0);
            var incompleteOwner = FivePlayers.First(player => incompleteEngine.ViewFor(player).Role == RoundRole.Innocent);
            incompleteEngine.Handle(new RoundCommand.EndPreparation());
            incompleteEngine.Handle(new RoundCommand.TimeExpired());

            Assert.That(incompleteEngine.ViewFor(incompleteOwner).Result.Survived, Is.True);
            Assert.That(incompleteEngine.ViewFor(incompleteOwner).Result.PrivateObjectiveCompleted, Is.False);
            Assert.That(incompleteEngine.ViewFor(incompleteOwner).Result.Won, Is.False);

            var winningEngine = StartedEngine(secretObjectives: 0);
            var winner = FivePlayers.First(player => winningEngine.ViewFor(player).Role == RoundRole.Innocent);
            winningEngine.Handle(new RoundCommand.EndPreparation());
            CompletePrivateObjective(winningEngine, winner);
            winningEngine.Handle(new RoundCommand.TimeExpired());

            Assert.That(winningEngine.ViewFor(winner).Result.PrivateObjectiveCompleted, Is.True);
            Assert.That(winningEngine.ViewFor(winner).Result.Survived, Is.True);
            Assert.That(winningEngine.ViewFor(winner).Result.Won, Is.True);

            var executedEngine = StartedEngine(secretObjectives: 0);
            var executed = FivePlayers.First(player => executedEngine.ViewFor(player).Role == RoundRole.Innocent);
            executedEngine.Handle(new RoundCommand.EndPreparation());
            CompletePrivateObjective(executedEngine, executed);
            executedEngine.Handle(new RoundCommand.Execute(executed));

            Assert.That(executedEngine.ViewFor(executed).Result.PrivateObjectiveCompleted, Is.True);
            Assert.That(executedEngine.ViewFor(executed).Result.Survived, Is.False);
            Assert.That(executedEngine.ViewFor(executed).Result.Won, Is.False);
        }

        [Test]
        public void SecretObjective_ResultRequiresFramingTargetExecutionAndSurvival()
        {
            var winningEngine = StartedEngine();
            var winner = FivePlayers.Select(player => winningEngine.ViewFor(player))
                .Single(view => view.PrivateObjective?.Kind == PrivateObjectiveKind.SecretObjective);
            winningEngine.Handle(new RoundCommand.EndPreparation());
            CompletePrivateObjective(winningEngine, winner.Viewer);
            winningEngine.Handle(new RoundCommand.Execute(winner.PrivateObjective.Target.Value));

            Assert.That(winningEngine.ViewFor(winner.Viewer).Result.Won, Is.True);

            var incompleteEngine = StartedEngine();
            var incompleteOwner = FivePlayers.Select(player => incompleteEngine.ViewFor(player))
                .Single(view => view.PrivateObjective?.Kind == PrivateObjectiveKind.SecretObjective);
            incompleteEngine.Handle(new RoundCommand.EndPreparation());
            incompleteEngine.Handle(new RoundCommand.Execute(incompleteOwner.PrivateObjective.Target.Value));

            Assert.That(incompleteEngine.ViewFor(incompleteOwner.Viewer).Result.Won, Is.False,
                "target execution without completed Wrobienie is insufficient");

            var noExecutionEngine = StartedEngine();
            var noExecutionOwner = FivePlayers.Select(player => noExecutionEngine.ViewFor(player))
                .Single(view => view.PrivateObjective?.Kind == PrivateObjectiveKind.SecretObjective);
            noExecutionEngine.Handle(new RoundCommand.EndPreparation());
            CompletePrivateObjective(noExecutionEngine, noExecutionOwner.Viewer);
            noExecutionEngine.Handle(new RoundCommand.TimeExpired());

            Assert.That(noExecutionEngine.ViewFor(noExecutionOwner.Viewer).Result.Won, Is.False,
                "completed Wrobienie without the Cel's execution is insufficient");

            var executedOwnerEngine = StartedEngine();
            var executedOwner = FivePlayers.Select(player => executedOwnerEngine.ViewFor(player))
                .Single(view => view.PrivateObjective?.Kind == PrivateObjectiveKind.SecretObjective);
            executedOwnerEngine.Handle(new RoundCommand.EndPreparation());
            CompletePrivateObjective(executedOwnerEngine, executedOwner.Viewer);
            executedOwnerEngine.Handle(new RoundCommand.Execute(executedOwner.Viewer));

            Assert.That(executedOwnerEngine.ViewFor(executedOwner.Viewer).Result.PrivateObjectiveCompleted, Is.True);
            Assert.That(executedOwnerEngine.ViewFor(executedOwner.Viewer).Result.Survived, Is.False);
            Assert.That(executedOwnerEngine.ViewFor(executedOwner.Viewer).Result.Won, Is.False);
        }

        // Incydenty: host-owned authors, private Detective registry and post-Runda reveal.
        [Test]
        public void LoudIncident_IsImmediatelyReportedToDetectiveWithoutAuthor()
        {
            var engine = StartedEngine();
            var detective = FindByRole(engine, RoundRole.Detective);
            var author = FindByRole(engine, RoundRole.Guilty);
            engine.Handle(new RoundCommand.EndPreparation());

            var transition = engine.Handle(Incident(
                author,
                "alarm-archive",
                IncidentKind.Loud,
                "alarm",
                "archive",
                1200));

            Assert.That(transition.Accepted, Is.True, transition.RejectionReason);
            var registry = engine.ViewFor(detective).IncidentRegistry;
            Assert.That(registry, Has.Count.EqualTo(1));
            Assert.That(registry[0].Id, Is.EqualTo(new IncidentId("alarm-archive")));
            Assert.That(registry[0].Kind, Is.EqualTo(IncidentKind.Loud));
            Assert.That(registry[0].Effect, Is.EqualTo(new IncidentEffectId("alarm")));
            Assert.That(registry[0].Location, Is.EqualTo(new IncidentLocationId("archive")));
            Assert.That(registry[0].ReportedAt, Is.EqualTo(new IncidentTimestamp(1200)));
            Assert.That(typeof(IncidentRegistryEntryView).GetProperty("Author"), Is.Null);
            Assert.That(typeof(IncidentRegistryEntryView).GetProperty("Role"), Is.Null);
            Assert.That(typeof(IncidentRegistryEntryView).GetProperty("Motive"), Is.Null);
            Assert.That(typeof(IncidentRegistryEntryView).GetProperty("OccurredAt"), Is.Null);
            Assert.That(typeof(RoundEvent.IncidentRegistered).GetProperty("Author"), Is.Null);
            Assert.That(engine.ViewFor(author).IncidentRegistry, Is.Null);
        }

        [Test]
        public void QuietIncident_RemainsHiddenUntilDetectiveDiscoversIt()
        {
            var engine = StartedEngine();
            var detective = FindByRole(engine, RoundRole.Detective);
            var author = FindByRole(engine, RoundRole.Innocent);
            var otherSuspect = FivePlayers.First(player =>
                player != author && engine.ViewFor(player).Role != RoundRole.Detective);
            engine.Handle(new RoundCommand.EndPreparation());
            var incidentId = new IncidentId("missing-key");

            var registered = engine.Handle(Incident(
                author,
                incidentId.Value,
                IncidentKind.Quiet,
                "missing-item",
                "evidence-room",
                1000));
            var foreignDiscovery = engine.Handle(new RoundCommand.DiscoverQuietIncident(
                otherSuspect,
                incidentId,
                new IncidentTimestamp(2000)));
            var discovered = engine.Handle(new RoundCommand.DiscoverQuietIncident(
                detective,
                incidentId,
                new IncidentTimestamp(2500)));
            var repeated = engine.Handle(new RoundCommand.DiscoverQuietIncident(
                detective,
                incidentId,
                new IncidentTimestamp(3000)));

            Assert.That(registered.Accepted, Is.True);
            Assert.That(foreignDiscovery.Accepted, Is.False);
            Assert.That(discovered.Accepted, Is.True, discovered.RejectionReason);
            Assert.That(repeated.Accepted, Is.False);
            Assert.That(engine.ViewFor(detective).IncidentRegistry, Has.Count.EqualTo(1));
            Assert.That(engine.ViewFor(detective).IncidentRegistry[0].ReportedAt,
                Is.EqualTo(new IncidentTimestamp(2500)));
        }

        [Test]
        public void IncidentRegistry_IsOrderedByReportOrDiscovery_NotQuietActionTime()
        {
            var engine = StartedEngine();
            var detective = FindByRole(engine, RoundRole.Detective);
            var author = FindByRole(engine, RoundRole.Guilty);
            engine.Handle(new RoundCommand.EndPreparation());

            engine.Handle(Incident(author, "quiet-first", IncidentKind.Quiet, "missing-item", "archive", 500));
            engine.Handle(Incident(author, "loud-second", IncidentKind.Loud, "alarm", "hall", 1000));
            engine.Handle(new RoundCommand.DiscoverQuietIncident(
                detective,
                new IncidentId("quiet-first"),
                new IncidentTimestamp(1500)));

            var registry = engine.ViewFor(detective).IncidentRegistry;
            Assert.That(registry.Select(entry => entry.Id), Is.EqualTo(new[]
            {
                new IncidentId("loud-second"),
                new IncidentId("quiet-first")
            }));
            Assert.That(registry.Select(entry => entry.ReportedAt), Is.EqualTo(new[]
            {
                new IncidentTimestamp(1000),
                new IncidentTimestamp(1500)
            }));
        }

        [Test]
        public void RegisterIncident_DuplicateWorldEffectIsRejectedWithoutRegistrySpam()
        {
            var engine = StartedEngine();
            var detective = FindByRole(engine, RoundRole.Detective);
            var author = FindByRole(engine, RoundRole.Guilty);
            engine.Handle(new RoundCommand.EndPreparation());

            var first = engine.Handle(Incident(author, "one-shot-alarm", IncidentKind.Loud, "alarm", "hall", 1000));
            var duplicate = engine.Handle(Incident(author, "one-shot-alarm", IncidentKind.Loud, "alarm", "hall", 2000));

            Assert.That(first.Accepted, Is.True);
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(engine.ViewFor(detective).IncidentRegistry, Has.Count.EqualTo(1));
            Assert.That(engine.ViewFor(detective).IncidentRegistry[0].ReportedAt,
                Is.EqualTo(new IncidentTimestamp(1000)));
        }

        [Test]
        public void RegisterIncident_ValidatesRoundPhaseAndSuspectAuthor()
        {
            var engine = StartedEngine();
            var detective = FindByRole(engine, RoundRole.Detective);
            var suspect = FindByRole(engine, RoundRole.Guilty);

            var duringPreparation = engine.Handle(Incident(
                suspect,
                "too-early",
                IncidentKind.Loud,
                "alarm",
                "hall",
                0));
            engine.Handle(new RoundCommand.EndPreparation());
            var detectiveAsAuthor = engine.Handle(Incident(
                detective,
                "detective-action",
                IncidentKind.Loud,
                "alarm",
                "hall",
                1000));
            var unknownAuthor = engine.Handle(Incident(
                new PlayerId(99),
                "unknown-action",
                IncidentKind.Loud,
                "alarm",
                "hall",
                1000));

            Assert.That(duringPreparation.Accepted, Is.False);
            Assert.That(detectiveAsAuthor.Accepted, Is.False);
            Assert.That(unknownAuthor.Accepted, Is.False);
            Assert.That(engine.ViewFor(detective).IncidentRegistry, Is.Empty);
        }

        [Test]
        public void IncidentAuthor_IsRevealedOnlyAfterRoundEnds()
        {
            var engine = StartedEngine();
            var author = FindByRole(engine, RoundRole.Guilty);
            engine.Handle(new RoundCommand.EndPreparation());
            engine.Handle(Incident(author, "revealed-later", IncidentKind.Quiet, "missing-file", "office", 1000));

            foreach (var player in FivePlayers)
                Assert.That(engine.ViewFor(player).RevealedIncidents, Is.Null);

            engine.Handle(new RoundCommand.TimeExpired());

            foreach (var player in FivePlayers)
            {
                var revealed = engine.ViewFor(player).RevealedIncidents;
                Assert.That(revealed, Has.Count.EqualTo(1));
                Assert.That(revealed[0].Id, Is.EqualTo(new IncidentId("revealed-later")));
                Assert.That(revealed[0].Author, Is.EqualTo(author));
            }
        }

        [Test]
        public void IncidentObjectiveLink_AdvancesOnlyAuthorsExactCurrentStep()
        {
            var engine = StartedEngine();
            var owner = FivePlayers.Select(player => engine.ViewFor(player))
                .Single(view => view.PrivateObjective?.Kind == PrivateObjectiveKind.SecretObjective);
            var other = FivePlayers.Select(player => engine.ViewFor(player))
                .First(view => view.Role == RoundRole.Innocent && view.Viewer != owner.Viewer);
            engine.Handle(new RoundCommand.EndPreparation());
            var ownersStep = new PrivateObjectiveStepReference(
                owner.PrivateObjective.Id,
                owner.PrivateObjective.CurrentStep.Value);

            var bluff = engine.Handle(Incident(
                other.Viewer,
                "bluff-action",
                IncidentKind.Quiet,
                "suspicious-item",
                "locker",
                1000,
                ownersStep));
            var real = engine.Handle(Incident(
                owner.Viewer,
                "real-action",
                IncidentKind.Quiet,
                "suspicious-item",
                "locker",
                1500,
                ownersStep));
            var duplicate = engine.Handle(Incident(
                owner.Viewer,
                "real-action",
                IncidentKind.Quiet,
                "suspicious-item",
                "locker",
                2000,
                ownersStep));

            Assert.That(owner.PrivateObjective.Id, Is.Not.EqualTo(other.PrivateObjective.Id));
            Assert.That(bluff.Accepted, Is.True, bluff.RejectionReason);
            Assert.That(real.Accepted, Is.True, real.RejectionReason);
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(engine.ViewFor(other.Viewer).PrivateObjective.CompletedStepCount, Is.Zero);
            Assert.That(engine.ViewFor(owner.Viewer).PrivateObjective.CompletedStepCount, Is.EqualTo(1));
        }

        private static RoundCommand.RegisterIncident Incident(
            PlayerId author,
            string id,
            IncidentKind kind,
            string effect,
            string location,
            long occurredAt,
            PrivateObjectiveStepReference objectiveStep = null) =>
            new RoundCommand.RegisterIncident(
                author,
                new IncidentId(id),
                kind,
                new IncidentEffectId(effect),
                new IncidentLocationId(location),
                new IncidentTimestamp(occurredAt),
                objectiveStep);

        // Tropy do Alibi and Plan Ucieczki.
        [Test]
        public void StartRound_AlibiClueLinkedToMissingOrVisibleFact_IsRejected()
        {
            var source = TestCase();
            var missing = new CaseDefinition(
                source.Title,
                source.CrimeDescription,
                source.AlibiFacts,
                source.MinHiddenFacts,
                source.MaxHiddenFacts,
                new[]
                {
                    new AlibiClueDefinition(new AlibiClueId("bad-missing"), "missing", "Pośredni ślad.")
                });
            var visible = new CaseDefinition(
                source.Title,
                source.CrimeDescription,
                source.AlibiFacts,
                source.MinHiddenFacts,
                source.MaxHiddenFacts,
                new[]
                {
                    new AlibiClueDefinition(new AlibiClueId("bad-visible"), "f1", "Pośredni ślad.")
                });

            Assert.That(new RoundEngine().Handle(
                new RoundCommand.StartRound(missing, FivePlayers, seed: 7)).Accepted, Is.False);
            Assert.That(new RoundEngine().Handle(
                new RoundCommand.StartRound(visible, FivePlayers, seed: 7)).Accepted, Is.False);
        }

        [Test]
        public void AcquireAlibiClue_IsInterpretivePrivateAndCanMixWithEscapePreparation()
        {
            var engine = StartedEngine();
            var guilty = FindByRole(engine, RoundRole.Guilty);
            var detective = FindByRole(engine, RoundRole.Detective);
            var innocent = FindByRole(engine, RoundRole.Innocent);
            var hiddenEntry = engine.ViewFor(guilty).Alibi.Entries.First(entry => entry.IsHidden);
            var hiddenFactText = TestCase().AlibiFacts.Single(fact => fact.Id == hiddenEntry.FactId).Text;
            var clueId = new AlibiClueId($"clue-{hiddenEntry.FactId}");
            engine.Handle(new RoundCommand.EndPreparation());

            var acquired = engine.Handle(new RoundCommand.AcquireAlibiClue(
                guilty,
                clueId,
                new IncidentId("clue-search"),
                IncidentKind.Quiet,
                new IncidentEffectId("searched-confiscated-property"),
                new IncidentLocationId("evidence-room"),
                new IncidentTimestamp(1000)));

            Assert.That(acquired.Accepted, Is.True, acquired.RejectionReason);
            var guiltyView = engine.ViewFor(guilty);
            Assert.That(guiltyView.Alibi, Is.Null, "a Trop never restores the Alibi screen");
            Assert.That(guiltyView.AcquiredAlibiClues, Has.Count.EqualTo(1));
            Assert.That(guiltyView.AcquiredAlibiClues[0].Id, Is.EqualTo(clueId));
            Assert.That(guiltyView.AcquiredAlibiClues[0].Content, Is.Not.EqualTo(hiddenFactText));
            Assert.That(guiltyView.AcquiredAlibiClues[0].Content, Does.Not.Contain(hiddenFactText));
            Assert.That(typeof(AlibiClueView).GetProperty("LinkedFactId"), Is.Null);
            Assert.That(engine.ViewFor(detective).AcquiredAlibiClues, Is.Null);
            Assert.That(engine.ViewFor(innocent).AcquiredAlibiClues, Is.Null);
            Assert.That(engine.ViewFor(detective).IncidentRegistry, Is.Empty,
                "the quiet search still needs personal discovery");
            Assert.That(guiltyView.RoundReveal, Is.Null);

            var firstEscapeStep = guiltyView.EscapePlan.CurrentStep.Value;
            var prepared = engine.Handle(new RoundCommand.PrepareEscape(
                guilty,
                guiltyView.EscapePlan.Id,
                firstEscapeStep));

            Assert.That(prepared.Accepted, Is.True, prepared.RejectionReason);
            Assert.That(engine.ViewFor(guilty).EscapePlan.CompletedCommonStepCount, Is.EqualTo(1));
            Assert.That(engine.ViewFor(guilty).AcquiredAlibiClues, Has.Count.EqualTo(1));
        }

        [Test]
        public void EscapePlan_TwoExitsAndInterruptionRequireAnotherPreparation()
        {
            var engine = StartedEngine();
            var guilty = FindByRole(engine, RoundRole.Guilty);
            var detective = FindByRole(engine, RoundRole.Detective);
            engine.Handle(new RoundCommand.EndPreparation());
            CompleteCommonEscapeSteps(engine, guilty);
            var plan = engine.ViewFor(guilty).EscapePlan;
            Assert.That(plan.ExitOptions, Has.Count.GreaterThanOrEqualTo(2));
            var first = plan.ExitOptions[0];
            var second = plan.ExitOptions[1];

            Assert.That(engine.Handle(new RoundCommand.PrepareEscape(
                guilty, plan.Id, first.PreparationStepId)).Accepted, Is.True);
            Assert.That(engine.Handle(new RoundCommand.PrepareEscape(
                guilty, plan.Id, second.PreparationStepId)).Accepted, Is.True);

            var firstStart = engine.Handle(new RoundCommand.BeginEscape(
                guilty,
                plan.Id,
                first.Id,
                new IncidentId("escape-first"),
                new IncidentTimestamp(1000)));
            var firstInterrupted = engine.Handle(new RoundCommand.InterruptEscape(
                guilty, plan.Id, first.Id));
            var immediateFirstRetry = engine.Handle(new RoundCommand.BeginEscape(
                guilty,
                plan.Id,
                first.Id,
                new IncidentId("escape-first-immediate-retry"),
                new IncidentTimestamp(1500)));
            var secondStart = engine.Handle(new RoundCommand.BeginEscape(
                guilty,
                plan.Id,
                second.Id,
                new IncidentId("escape-second"),
                new IncidentTimestamp(2000)));
            var secondInterrupted = engine.Handle(new RoundCommand.InterruptEscape(
                guilty, plan.Id, second.Id));
            var immediateSecondRetry = engine.Handle(new RoundCommand.BeginEscape(
                guilty,
                plan.Id,
                second.Id,
                new IncidentId("escape-second-immediate-retry"),
                new IncidentTimestamp(2500)));
            var secondPreparedAgain = engine.Handle(new RoundCommand.PrepareEscape(
                guilty, plan.Id, second.PreparationStepId));
            var secondRetry = engine.Handle(new RoundCommand.BeginEscape(
                guilty,
                plan.Id,
                second.Id,
                new IncidentId("escape-second-retry"),
                new IncidentTimestamp(3000)));

            Assert.That(firstStart.Accepted, Is.True, firstStart.RejectionReason);
            Assert.That(firstInterrupted.Accepted, Is.True, firstInterrupted.RejectionReason);
            Assert.That(immediateFirstRetry.Accepted, Is.False);
            Assert.That(secondStart.Accepted, Is.True, secondStart.RejectionReason);
            Assert.That(secondInterrupted.Accepted, Is.True, secondInterrupted.RejectionReason);
            Assert.That(immediateSecondRetry.Accepted, Is.False);
            Assert.That(secondPreparedAgain.Accepted, Is.True, secondPreparedAgain.RejectionReason);
            Assert.That(secondRetry.Accepted, Is.True, secondRetry.RejectionReason);
            Assert.That(engine.ViewFor(detective).IncidentRegistry, Has.Count.EqualTo(3));
            Assert.That(engine.ViewFor(detective).IncidentRegistry.All(
                incident => incident.Kind == IncidentKind.Loud), Is.True);
        }

        [Test]
        public void FirstAcceptedRoundEnding_EscapeExecutionOrTimeout_WinsTheRace()
        {
            var escapeEngine = StartedEngine();
            var escapeGuilty = FindByRole(escapeEngine, RoundRole.Guilty);
            escapeEngine.Handle(new RoundCommand.EndPreparation());
            var escapeExit = PrepareFirstEscapeExit(escapeEngine, escapeGuilty);
            Assert.That(BeginEscape(escapeEngine, escapeGuilty, escapeExit, "race-escape", 1000).Accepted, Is.True);
            var escaped = escapeEngine.Handle(new RoundCommand.CompleteEscape(
                escapeGuilty,
                escapeEngine.ViewFor(escapeGuilty).EscapePlan.Id,
                escapeExit));
            var executionAfterEscape = escapeEngine.Handle(new RoundCommand.Execute(
                FivePlayers.First(player => escapeEngine.ViewFor(player).Role == RoundRole.Innocent)));
            var timeoutAfterEscape = escapeEngine.Handle(new RoundCommand.TimeExpired());
            Assert.That(escaped.Accepted, Is.True, escaped.RejectionReason);
            Assert.That(escaped.State.EndCause, Is.EqualTo(RoundEndCause.Escape));
            Assert.That(executionAfterEscape.Accepted, Is.False);
            Assert.That(timeoutAfterEscape.Accepted, Is.False);

            var executionEngine = StartedEngine();
            var executedGuilty = FindByRole(executionEngine, RoundRole.Guilty);
            executionEngine.Handle(new RoundCommand.EndPreparation());
            var executionExit = PrepareFirstEscapeExit(executionEngine, executedGuilty);
            BeginEscape(executionEngine, executedGuilty, executionExit, "race-execution", 1000);
            var execution = executionEngine.Handle(new RoundCommand.Execute(executedGuilty));
            var escapeAfterExecution = executionEngine.Handle(new RoundCommand.CompleteEscape(
                executedGuilty,
                executionEngine.ViewFor(executedGuilty).EscapePlan.Id,
                executionExit));
            Assert.That(execution.Accepted, Is.True);
            Assert.That(execution.State.EndCause, Is.EqualTo(RoundEndCause.Execution));
            Assert.That(escapeAfterExecution.Accepted, Is.False);

            var timeoutEngine = StartedEngine();
            var timeoutGuilty = FindByRole(timeoutEngine, RoundRole.Guilty);
            timeoutEngine.Handle(new RoundCommand.EndPreparation());
            var timeoutExit = PrepareFirstEscapeExit(timeoutEngine, timeoutGuilty);
            BeginEscape(timeoutEngine, timeoutGuilty, timeoutExit, "race-timeout", 1000);
            var timeout = timeoutEngine.Handle(new RoundCommand.TimeExpired());
            var escapeAfterTimeout = timeoutEngine.Handle(new RoundCommand.CompleteEscape(
                timeoutGuilty,
                timeoutEngine.ViewFor(timeoutGuilty).EscapePlan.Id,
                timeoutExit));
            Assert.That(timeout.Accepted, Is.True);
            Assert.That(timeout.State.EndCause, Is.EqualTo(RoundEndCause.TimeExpired));
            Assert.That(escapeAfterTimeout.Accepted, Is.False);
        }

        [Test]
        public void Escape_ResolvesEveryRoleAndRevealsTheTrueRoundCourse()
        {
            var engine = StartedEngine(secretObjectives: 0);
            var detective = FindByRole(engine, RoundRole.Detective);
            var guilty = FindByRole(engine, RoundRole.Guilty);
            var innocents = FivePlayers.Where(player => engine.ViewFor(player).Role == RoundRole.Innocent).ToArray();
            var completedInnocent = innocents[0];
            var incompleteInnocent = innocents[1];
            var hiddenFactId = engine.ViewFor(guilty).Alibi.Entries.First(entry => entry.IsHidden).FactId;
            engine.Handle(new RoundCommand.EndPreparation());
            CompletePrivateObjective(engine, completedInnocent);
            Assert.That(engine.Handle(new RoundCommand.AcquireAlibiClue(
                guilty,
                new AlibiClueId($"clue-{hiddenFactId}"),
                new IncidentId("reveal-clue"),
                IncidentKind.Quiet,
                new IncidentEffectId("searched-property"),
                new IncidentLocationId("archive"),
                new IncidentTimestamp(500))).Accepted, Is.True);
            var exit = PrepareFirstEscapeExit(engine, guilty);
            BeginEscape(engine, guilty, exit, "reveal-escape", 1000);

            var completed = engine.Handle(new RoundCommand.CompleteEscape(
                guilty,
                engine.ViewFor(guilty).EscapePlan.Id,
                exit));

            Assert.That(completed.Accepted, Is.True, completed.RejectionReason);
            Assert.That(engine.ViewFor(detective).Result.Won, Is.False);
            Assert.That(engine.ViewFor(guilty).Result.Won, Is.True);
            Assert.That(engine.ViewFor(guilty).Result.Escaped, Is.True);
            Assert.That(engine.ViewFor(completedInnocent).Result.Survived, Is.True);
            Assert.That(engine.ViewFor(completedInnocent).Result.Won, Is.True);
            Assert.That(engine.ViewFor(incompleteInnocent).Result.Survived, Is.True);
            Assert.That(engine.ViewFor(incompleteInnocent).Result.Won, Is.False);

            foreach (var player in FivePlayers)
            {
                var reveal = engine.ViewFor(player).RoundReveal;
                Assert.That(reveal, Is.Not.Null);
                Assert.That(reveal.Players, Has.Count.EqualTo(FivePlayers.Length));
                Assert.That(reveal.Players.Select(result => result.Role), Has.Member(RoundRole.Detective));
                Assert.That(reveal.Players.Select(result => result.Role), Has.Member(RoundRole.Guilty));
                Assert.That(reveal.AcquiredAlibiClues, Has.Count.EqualTo(1));
                Assert.That(reveal.EscapePlan.SuccessfulExit, Is.EqualTo(exit));
                Assert.That(reveal.EscapePlan.Actions.Any(
                    action => action.Kind == EscapeActionKind.Completed), Is.True);
                Assert.That(reveal.Incidents, Has.Count.EqualTo(2));
            }
        }

        private static void CompleteCommonEscapeSteps(RoundEngine engine, PlayerId guilty)
        {
            while (engine.ViewFor(guilty).EscapePlan.CurrentStep.HasValue)
            {
                var plan = engine.ViewFor(guilty).EscapePlan;
                var transition = engine.Handle(new RoundCommand.PrepareEscape(
                    guilty,
                    plan.Id,
                    plan.CurrentStep.Value));
                Assert.That(transition.Accepted, Is.True, transition.RejectionReason);
            }
        }

        private static EscapeExitId PrepareFirstEscapeExit(RoundEngine engine, PlayerId guilty)
        {
            CompleteCommonEscapeSteps(engine, guilty);
            var plan = engine.ViewFor(guilty).EscapePlan;
            var option = plan.ExitOptions[0];
            var prepared = engine.Handle(new RoundCommand.PrepareEscape(
                guilty,
                plan.Id,
                option.PreparationStepId));
            Assert.That(prepared.Accepted, Is.True, prepared.RejectionReason);
            return option.Id;
        }

        private static RoundTransition BeginEscape(
            RoundEngine engine,
            PlayerId guilty,
            EscapeExitId exit,
            string incidentId,
            long occurredAt) =>
            engine.Handle(new RoundCommand.BeginEscape(
                guilty,
                engine.ViewFor(guilty).EscapePlan.Id,
                exit,
                new IncidentId(incidentId),
                new IncidentTimestamp(occurredAt)));

        private static void CompletePrivateObjective(RoundEngine engine, PlayerId owner)
        {
            while (engine.ViewFor(owner).PrivateObjective.CurrentStep.HasValue)
            {
                var objective = engine.ViewFor(owner).PrivateObjective;
                var transition = engine.Handle(new RoundCommand.AdvancePrivateObjective(
                    owner,
                    objective.Id,
                    objective.CurrentStep.Value));
                Assert.That(transition.Accepted, Is.True, transition.RejectionReason);
            }
        }
    }
}
