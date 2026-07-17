using System.Linq;
using InterrogationRoom.Domain;
using Mirror;
using NUnit.Framework;

namespace InterrogationRoom.Networking.Tests
{
    public sealed class RoundDeveloperScenarioFlowTests
    {
        private static CaseDefinition TestCase() => new CaseDefinition(
            "Przepływ developerski",
            "Ktoś ukradł pomnikowi kapelusz.",
            new[]
            {
                new AlibiFact("f1", "Spotkanie zaczęło się o dziewiętnastej.", false),
                new AlibiFact("f2", "Kelner pomylił rachunek.", true),
                new AlibiFact("f3", "Na chwilę zgasło światło.", true),
                new AlibiFact("f4", "Wróciliśmy wspólnie tramwajem.", false),
                new AlibiFact("f5", "Przed wyjściem zamówiliśmy herbatę.", false),
                new AlibiFact(
                    "f6",
                    "Kelner miał zieloną muchę.",
                    false,
                    new[] { "Kelner miał zieloną muchę.", "Kelner miał granatową muchę." },
                    distinctiveDetail: true)
            },
            minHiddenFacts: 1,
            maxHiddenFacts: 1,
            new[]
            {
                new AlibiClueDefinition(
                    new AlibiClueId("clue-f2"),
                    "f2",
                    "Paragon zawiera dopisaną pozycję.")
            });

        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void GuiltyScenario_RunsThroughWireEscapeRevealAndFreshSecondRound(int playerCount)
        {
            var controlled = new PlayerId(0);
            Assert.That(RoundDeveloperScenarioPlanner.TryCreate(
                TestCase(),
                new[] { controlled },
                controlled,
                playerCount,
                RoundDeveloperScenario.GuiltyEscape,
                out var plan,
                out var reason), Is.True, reason);

            var first = Start(plan);
            AssertPreparationWireViews(first, plan);
            Assert.That(first.Handle(new RoundCommand.EndPreparation()).Accepted, Is.True);

            var clue = TestCase().AlibiClues.Single();
            Assert.That(first.Handle(new RoundCommand.AcquireAlibiClue(
                controlled,
                clue.Id,
                new IncidentId("dev-clue"),
                IncidentKind.Quiet,
                new IncidentEffectId("searched"),
                new IncidentLocationId("evidence-room"),
                new IncidentTimestamp(100))).Accepted, Is.True);

            while (first.ViewFor(controlled).EscapePlan.CurrentStep.HasValue)
            {
                var escape = first.ViewFor(controlled).EscapePlan;
                Assert.That(first.Handle(new RoundCommand.PrepareEscape(
                    controlled,
                    escape.Id,
                    escape.CurrentStep.Value)).Accepted, Is.True);
            }

            var preparedPlan = first.ViewFor(controlled).EscapePlan;
            var exit = preparedPlan.ExitOptions[0];
            Assert.That(first.Handle(new RoundCommand.PrepareEscape(
                controlled,
                preparedPlan.Id,
                exit.PreparationStepId)).Accepted, Is.True);
            Assert.That(first.Handle(new RoundCommand.BeginEscape(
                controlled,
                preparedPlan.Id,
                exit.Id,
                new IncidentId("dev-escape"),
                new IncidentTimestamp(200))).Accepted, Is.True);
            Assert.That(first.Handle(new RoundCommand.CompleteEscape(
                controlled,
                preparedPlan.Id,
                exit.Id)).Accepted, Is.True);

            foreach (var player in plan.Players)
            {
                var restored = RoundTrip(first.ViewFor(player));
                Assert.That(restored.Phase, Is.EqualTo(RoundPhase.Finished));
                Assert.That(restored.RoundReveal.Players.Count, Is.EqualTo(playerCount));
                Assert.That(restored.RoundReveal.AcquiredAlibiClues.Count, Is.EqualTo(1));
                Assert.That(restored.RoundReveal.EscapePlan.SuccessfulExit, Is.EqualTo(exit.Id));
            }

            var second = Start(plan);
            foreach (var player in plan.Players)
            {
                var fresh = second.ViewFor(player);
                Assert.That(fresh.Phase, Is.EqualTo(RoundPhase.Preparation));
                Assert.That(fresh.Result, Is.Null);
                Assert.That(fresh.RoundReveal, Is.Null);
                Assert.That(fresh.RevealedIncidents, Is.Null);
                if (fresh.Role == RoundRole.Guilty)
                {
                    Assert.That(fresh.AcquiredAlibiClues, Is.Empty);
                    Assert.That(fresh.EscapePlan.CompletedCommonStepCount, Is.Zero);
                    Assert.That(fresh.EscapePlan.ExitOptions.Any(option => option.IsPrepared), Is.False);
                }
            }
        }

        private static RoundEngine Start(RoundDeveloperPlan plan)
        {
            var engine = new RoundEngine();
            var transition = engine.Handle(new RoundCommand.StartRound(
                TestCase(),
                plan.Players,
                plan.Seed,
                plan.SecretObjectiveCount));
            Assert.That(transition.Accepted, Is.True, transition.RejectionReason);
            return engine;
        }

        private static void AssertPreparationWireViews(RoundEngine engine, RoundDeveloperPlan plan)
        {
            foreach (var player in plan.Players)
            {
                var source = engine.ViewFor(player);
                var restored = RoundTrip(source);
                Assert.That(restored.Viewer, Is.EqualTo(player));
                Assert.That(restored.Players.Count, Is.EqualTo(plan.Players.Count));
                Assert.That(restored.Role, Is.EqualTo(source.Role));
                Assert.That(restored.RoundReveal, Is.Null);
                if (source.Role == RoundRole.Detective)
                    Assert.That(restored.Alibi, Is.Null);
            }
        }

        private static PlayerRoundView RoundTrip(PlayerRoundView source)
        {
            RoundMessageSerialization.Register();
            using (var writer = NetworkWriterPool.Get())
            {
                writer.Write(RoundViewMessage.FromView(source, 123d));
                using (var reader = NetworkReaderPool.Get(writer.ToArraySegment()))
                    return reader.Read<RoundViewMessage>().ToView();
            }
        }
    }
}
