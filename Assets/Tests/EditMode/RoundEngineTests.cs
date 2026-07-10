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
                    new AlibiFact("f6", "Na przystanku padał deszcz.", false)
                },
                minHidden,
                maxHidden);

        private static RoundEngine StartedEngine(int seed = 7, int secretObjectives = 0, CaseDefinition caseDefinition = null)
        {
            var engine = new RoundEngine();
            var transition = engine.Handle(
                new RoundCommand.StartRound(caseDefinition ?? TestCase(), FivePlayers, seed, secretObjectives));
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
            }
        }

        [Test]
        public void StartRound_InvalidComposition_IsRejected()
        {
            var tooFew = new RoundEngine().Handle(new RoundCommand.StartRound(
                TestCase(), FivePlayers.Take(3), seed: 1));
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
            engine.Handle(new RoundCommand.EndPreparation());

            var transition = engine.Handle(new RoundCommand.TimeExpired());

            Assert.That(transition.Accepted, Is.True);
            Assert.That(transition.State.Phase, Is.EqualTo(RoundPhase.Finished));
            Assert.That(transition.State.DetectiveWon, Is.False);
            Assert.That(transition.State.EndCause, Is.EqualTo(RoundEndCause.TimeExpired));
            Assert.That(engine.ViewFor(detective).Result.Won, Is.False);
            foreach (var suspect in FivePlayers.Where(p => p != detective))
            {
                Assert.That(engine.ViewFor(suspect).Result.Survived, Is.True);
                Assert.That(engine.ViewFor(suspect).Result.Won, Is.True);
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

        // Sekretne Cele: konfiguracja 0..N, wygrana = Przetrwanie właściciela + eliminacja Celu.
        [Test]
        public void SecretObjective_AssignedToInnocent_TargetsAnotherInnocent()
        {
            var engine = StartedEngine(secretObjectives: 1);

            var owners = FivePlayers
                .Select(p => engine.ViewFor(p))
                .Where(v => v.SecretObjective != null)
                .ToList();

            Assert.That(owners.Count, Is.EqualTo(1));
            Assert.That(owners[0].Role, Is.EqualTo(RoundRole.Innocent));
            var target = owners[0].SecretObjective.Target;
            Assert.That(target, Is.Not.EqualTo(owners[0].Viewer));
            Assert.That(engine.ViewFor(target).Role, Is.EqualTo(RoundRole.Innocent), "Cel is always a Niewinny");
        }

        [Test]
        public void SecretObjective_OwnerWinsOnlyWhenSurvivingAndTargetExecuted()
        {
            // Scenario A: the target is executed — the owner wins despite the Detektyw losing.
            var engine = StartedEngine(secretObjectives: 1);
            var ownerView = FivePlayers.Select(p => engine.ViewFor(p)).First(v => v.SecretObjective != null);
            engine.Handle(new RoundCommand.EndPreparation());
            engine.Handle(new RoundCommand.Execute(ownerView.SecretObjective.Target));

            Assert.That(engine.ViewFor(ownerView.Viewer).Result.Won, Is.True);

            // Scenario B: everyone survives — mere Przetrwanie is not enough for the owner.
            var timeoutEngine = StartedEngine(secretObjectives: 1);
            var timeoutOwner = FivePlayers.Select(p => timeoutEngine.ViewFor(p)).First(v => v.SecretObjective != null).Viewer;
            timeoutEngine.Handle(new RoundCommand.EndPreparation());
            timeoutEngine.Handle(new RoundCommand.TimeExpired());

            Assert.That(timeoutEngine.ViewFor(timeoutOwner).Result.Survived, Is.True);
            Assert.That(timeoutEngine.ViewFor(timeoutOwner).Result.Won, Is.False);
        }
    }
}
