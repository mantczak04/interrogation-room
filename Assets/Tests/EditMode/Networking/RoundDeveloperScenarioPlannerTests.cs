using System.Linq;
using InterrogationRoom.Domain;
using NUnit.Framework;

namespace InterrogationRoom.Networking.Tests
{
    public sealed class RoundDeveloperScenarioPlannerTests
    {
        private static CaseDefinition TestCase() => new CaseDefinition(
            "Developerska Sprawa",
            "Ktoś przemalował pomnik.",
            new[]
            {
                new AlibiFact("f1", "Spotkaliśmy się przy fontannie.", false),
                new AlibiFact("f2", "Kelner pomylił rachunek.", true),
                new AlibiFact("f3", "Na chwilę zgasło światło.", true),
                new AlibiFact("f4", "Wróciliśmy tramwajem.", false),
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
                    "Paragon z dopisaną pozycją.")
            });

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(8)]
        public void Create_FillsRequestedRosterWithoutCreatingConnections(int targetPlayerCount)
        {
            var connected = new[] { new PlayerId(0) };

            bool created = RoundDeveloperScenarioPlanner.TryCreate(
                TestCase(),
                connected,
                connected[0],
                targetPlayerCount,
                RoundDeveloperScenario.PersonalMatter,
                out var plan,
                out var reason);

            Assert.That(created, Is.True, reason);
            Assert.That(plan.Players.Count, Is.EqualTo(targetPlayerCount));
            Assert.That(plan.Players.Distinct().Count(), Is.EqualTo(targetPlayerCount));
            Assert.That(plan.ConnectedPlayerCount, Is.EqualTo(1));
            Assert.That(plan.TechnicalPlayerCount, Is.EqualTo(targetPlayerCount - 1));
            Assert.That(plan.Players, Does.Contain(connected[0]));
        }

        [TestCase(RoundDeveloperScenario.PersonalMatter, 3, RoundRole.Innocent, PrivateObjectiveKind.PersonalMatter)]
        [TestCase(RoundDeveloperScenario.SecretObjective, 5, RoundRole.Innocent, PrivateObjectiveKind.SecretObjective)]
        [TestCase(RoundDeveloperScenario.GuiltyEscape, 4, RoundRole.Guilty, null)]
        [TestCase(RoundDeveloperScenario.DetectiveIncidents, 4, RoundRole.Detective, null)]
        public void Create_SelectsTheRequestedControlledView(
            RoundDeveloperScenario scenario,
            int targetPlayerCount,
            RoundRole expectedRole,
            PrivateObjectiveKind? expectedObjective)
        {
            var controlled = new PlayerId(7);

            bool created = RoundDeveloperScenarioPlanner.TryCreate(
                TestCase(),
                new[] { controlled },
                controlled,
                targetPlayerCount,
                scenario,
                out var plan,
                out var reason);

            Assert.That(created, Is.True, reason);
            var engine = new RoundEngine();
            Assert.That(engine.Handle(new RoundCommand.StartRound(
                TestCase(),
                plan.Players,
                plan.Seed,
                plan.SecretObjectiveCount)).Accepted, Is.True);
            var view = engine.ViewFor(controlled);
            Assert.That(view.Role, Is.EqualTo(expectedRole));
            if (expectedObjective.HasValue)
                Assert.That(view.PrivateObjective.Kind, Is.EqualTo(expectedObjective.Value));
            if (scenario == RoundDeveloperScenario.GuiltyEscape)
                Assert.That(view.Alibi.Entries.Single(entry => entry.FactId == "f2").IsHidden, Is.True);
        }

        [Test]
        public void Create_IsDeterministicForTheSameInputs()
        {
            var connected = new[] { new PlayerId(2), new PlayerId(9) };

            Assert.That(RoundDeveloperScenarioPlanner.TryCreate(
                TestCase(), connected, connected[1], 6, RoundDeveloperScenario.GuiltyEscape,
                out var first, out var firstReason), Is.True, firstReason);
            Assert.That(RoundDeveloperScenarioPlanner.TryCreate(
                TestCase(), connected, connected[1], 6, RoundDeveloperScenario.GuiltyEscape,
                out var second, out var secondReason), Is.True, secondReason);

            Assert.That(second.Seed, Is.EqualTo(first.Seed));
            Assert.That(second.Players, Is.EqualTo(first.Players));
        }

        [Test]
        public void Create_RequestedPhysicalClueControlsWhichFactIsHidden()
        {
            var source = TestCase();
            var requestedClue = new AlibiClueDefinition(
                new AlibiClueId("clue-f3"),
                "f3",
                "Notatka wspomina nagłą ciemność.");
            var caseWithTwoClues = new CaseDefinition(
                source.Title,
                source.CrimeDescription,
                source.AlibiFacts,
                source.MinHiddenFacts,
                source.MaxHiddenFacts,
                source.AlibiClues.Concat(new[] { requestedClue }));
            var controlled = new PlayerId(3);

            Assert.That(RoundDeveloperScenarioPlanner.TryCreate(
                caseWithTwoClues,
                new[] { controlled },
                controlled,
                4,
                RoundDeveloperScenario.GuiltyEscape,
                requestedClue.Id,
                out var plan,
                out var reason), Is.True, reason);

            var engine = new RoundEngine();
            Assert.That(engine.Handle(new RoundCommand.StartRound(
                caseWithTwoClues,
                plan.Players,
                plan.Seed,
                plan.SecretObjectiveCount)).Accepted, Is.True);
            Assert.That(engine.ViewFor(controlled).Alibi.Entries
                .Single(entry => entry.FactId == "f3").IsHidden, Is.True);
        }

        [Test]
        public void Create_RejectsThreeAndFourPlayerSecretObjectiveAndUnknownController()
        {
            var connected = new[] { new PlayerId(1) };

            foreach (var playerCount in new[] { 3, 4 })
            {
                Assert.That(RoundDeveloperScenarioPlanner.TryCreate(
                    TestCase(), connected, connected[0], playerCount, RoundDeveloperScenario.SecretObjective,
                    out _, out var reason), Is.False);
                Assert.That(reason, Does.Contain("three- and four-player"));
            }

            Assert.That(RoundDeveloperScenarioPlanner.TryCreate(
                TestCase(), connected, new PlayerId(99), 5, RoundDeveloperScenario.PersonalMatter,
                out _, out var controllerReason), Is.False);
            Assert.That(controllerReason, Does.Contain("connected player"));
        }

        [TestCase(RoundDeveloperTask.PersonalMatterPrepare, RoundDeveloperScenario.PersonalMatter, RoundRole.Innocent)]
        [TestCase(RoundDeveloperTask.PersonalMatterFinish, RoundDeveloperScenario.PersonalMatter, RoundRole.Innocent)]
        [TestCase(RoundDeveloperTask.SecretObjectivePrepare, RoundDeveloperScenario.SecretObjective, RoundRole.Innocent)]
        [TestCase(RoundDeveloperTask.SecretObjectivePlant, RoundDeveloperScenario.SecretObjective, RoundRole.Innocent)]
        [TestCase(RoundDeveloperTask.AlibiClue, RoundDeveloperScenario.GuiltyEscape, RoundRole.Guilty)]
        [TestCase(RoundDeveloperTask.EscapeFindTool, RoundDeveloperScenario.GuiltyEscape, RoundRole.Guilty)]
        [TestCase(RoundDeveloperTask.EscapeOpenRoute, RoundDeveloperScenario.GuiltyEscape, RoundRole.Guilty)]
        [TestCase(RoundDeveloperTask.EscapePrepareVent, RoundDeveloperScenario.GuiltyEscape, RoundRole.Guilty)]
        [TestCase(RoundDeveloperTask.EscapeFinalVent, RoundDeveloperScenario.GuiltyEscape, RoundRole.Guilty)]
        [TestCase(RoundDeveloperTask.EscapePrepareGate, RoundDeveloperScenario.GuiltyEscape, RoundRole.Guilty)]
        [TestCase(RoundDeveloperTask.EscapeFinalGate, RoundDeveloperScenario.GuiltyEscape, RoundRole.Guilty)]
        public void TaskCatalog_MapsEachTaskToItsRoleAndScenario(
            RoundDeveloperTask task,
            RoundDeveloperScenario expectedScenario,
            RoundRole expectedRole)
        {
            Assert.That(RoundDeveloperTaskCatalog.ScenarioFor(task), Is.EqualTo(expectedScenario));
            Assert.That(RoundDeveloperTaskCatalog.RoleFor(task), Is.EqualTo(expectedRole));
        }

        [Test]
        public void TaskCatalog_NextCyclesWithinTheSelectedRole()
        {
            Assert.That(
                RoundDeveloperTaskCatalog.Next(RoundDeveloperTask.PersonalMatterFinish),
                Is.EqualTo(RoundDeveloperTask.SecretObjectivePrepare));
            Assert.That(
                RoundDeveloperTaskCatalog.Next(RoundDeveloperTask.SecretObjectivePlant),
                Is.EqualTo(RoundDeveloperTask.PersonalMatterPrepare));
            Assert.That(
                RoundDeveloperTaskCatalog.Next(RoundDeveloperTask.EscapeFinalGate),
                Is.EqualTo(RoundDeveloperTask.AlibiClue));
        }
    }
}
