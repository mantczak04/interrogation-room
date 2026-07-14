using InterrogationRoom.Domain;
using NUnit.Framework;

namespace InterrogationRoom.UI.Tests
{
    public sealed class RoundPresenterTests
    {
        private static readonly PlayerId[] Players =
        {
            new PlayerId(1),
            new PlayerId(2),
            new PlayerId(3),
            new PlayerId(4)
        };

        [Test]
        public void BuildState_DetectivePreparation_HasNoAlibiShape()
        {
            var view = View(RoundPhase.Preparation, RoundRole.Detective, alibi: null);

            var state = RoundPresenter.BuildState(view, remainingSeconds: 0f, isHost: true);

            Assert.That(state.AlibiVisible, Is.False);
            Assert.That(state.AlibiText, Is.Null);
            Assert.That(state.EndPreparationVisible, Is.True);
            Assert.That(state.ExecutionVisible, Is.False);
        }

        [Test]
        public void BuildState_SuspectPreparation_RendersOnlyReceivedAlibi()
        {
            var alibi = new AlibiView(new[]
            {
                new AlibiEntry("f1", false, "Jawny fakt."),
                new AlibiEntry("f2", true, null)
            });

            var state = RoundPresenter.BuildState(
                View(RoundPhase.Preparation, RoundRole.Guilty, alibi),
                remainingSeconds: 0f,
                isHost: false);

            Assert.That(state.AlibiVisible, Is.True);
            Assert.That(state.AlibiText, Does.Contain("Jawny fakt."));
            Assert.That(state.AlibiText, Does.Contain("UKRYTY FAKT"));
            Assert.That(state.EndPreparationVisible, Is.False);
        }

        [Test]
        public void BuildState_DetectiveRound_HasNoClientExecutionFallback()
        {
            var state = RoundPresenter.BuildState(
                View(RoundPhase.Round, RoundRole.Detective, alibi: null),
                remainingSeconds: 83.2f,
                isHost: false);

            Assert.That(state.TimerVisible, Is.True);
            Assert.That(state.ExecutionVisible, Is.False);
            Assert.That(state.ExecutionTargets, Is.Empty);
        }

        [Test]
        public void BuildState_NonDetectiveRound_HidesExecutionControls()
        {
            var state = RoundPresenter.BuildState(
                View(RoundPhase.Round, RoundRole.Innocent, alibi: null, viewer: new PlayerId(2)),
                remainingSeconds: 60f,
                isHost: false);

            Assert.That(state.ExecutionVisible, Is.False);
            Assert.That(state.ExecutionTargets, Is.Empty);
        }

        [Test]
        public void CalculateRemainingSeconds_UsesSharedNetworkDeadlineWithoutAccumulatingTicks()
        {
            Assert.That(
                RoundPresenter.CalculateRemainingSeconds(173.5d, 100.25d, RoundPhase.Round),
                Is.EqualTo(73.25f));
            Assert.That(
                RoundPresenter.CalculateRemainingSeconds(173.5d, 200d, RoundPhase.Round),
                Is.Zero);
            Assert.That(
                RoundPresenter.CalculateRemainingSeconds(173.5d, 100.25d, RoundPhase.Finished),
                Is.Zero);
        }

        [Test]
        public void BuildState_Finished_RendersIndividualOutcomeCauseAndExecutedPlayer()
        {
            var result = new PlayerResultView(
                won: false,
                survived: false,
                detectiveWon: false,
                RoundEndCause.Execution,
                new PlayerId(2));

            var state = RoundPresenter.BuildState(
                View(RoundPhase.Finished, RoundRole.Innocent, alibi: null, viewer: new PlayerId(2), result: result),
                remainingSeconds: 0f,
                isHost: false);

            Assert.That(state.ResultVisible, Is.True);
            Assert.That(state.ResultText, Does.Contain("Przegrana"));
            Assert.That(state.ResultText, Does.Contain("Egzekucja"));
            Assert.That(state.ResultText, Does.Contain("Gracz 2"));
        }

        [Test]
        public void BuildState_DetectiveRound_RendersPrivateRegistryWithoutAuthor()
        {
            var view = new PlayerRoundView(
                new PlayerId(1),
                RoundPhase.Round,
                RoundRole.Detective,
                "Publiczne Przestępstwo",
                alibi: null,
                privateObjective: null,
                result: null,
                Players,
                new PlayerId(1),
                incidentRegistry: new[]
                {
                    new IncidentRegistryEntryView(
                        new IncidentId("alarm-001"),
                        IncidentKind.Loud,
                        new IncidentEffectId("archive-alarm"),
                        new IncidentLocationId("archive"),
                        new IncidentTimestamp(65000))
                });

            RoundUiState state = RoundPresenter.BuildState(view, 120f, isHost: false);

            Assert.That(state.PrivatePanelVisible, Is.True);
            Assert.That(state.PrivateTitle, Does.Contain("Rejestr"));
            Assert.That(state.PrivateText, Does.Contain("01:05"));
            Assert.That(state.PrivateText, Does.Contain("archive-alarm"));
            Assert.That(state.PrivateText, Does.Not.Contain("Gracz"));
        }

        [Test]
        public void BuildState_InnocentRound_RendersOnlyOwnersCurrentObjectiveStep()
        {
            var objective = new PrivateObjectiveView(
                new PrivateObjectiveId("sekretny-cel:2"),
                PrivateObjectiveKind.SecretObjective,
                new PrivateObjectiveStepId("wrobienie-podloz"),
                completedStepCount: 1,
                totalStepCount: 2,
                isCompleted: false,
                new PlayerId(3));
            var view = new PlayerRoundView(
                new PlayerId(2),
                RoundPhase.Round,
                RoundRole.Innocent,
                "Publiczne Przestępstwo",
                alibi: null,
                objective,
                result: null,
                Players,
                new PlayerId(1));

            RoundUiState state = RoundPresenter.BuildState(view, 120f, isHost: false);

            Assert.That(state.PrivateText, Does.Contain("wrobienie-podloz"));
            Assert.That(state.PrivateText, Does.Contain("1/2"));
            Assert.That(state.PrivateText, Does.Contain("Gracz 3"));
            Assert.That(state.PrivateText, Does.Not.Contain("osobista-sprawa-zakoncz"));
        }

        [Test]
        public void BuildState_Finished_RendersApprovedFullRevealWithoutFullAlibi()
        {
            var result = new PlayerResultView(
                won: true,
                survived: true,
                detectiveWon: false,
                RoundEndCause.Escape,
                executedPlayer: null,
                privateObjectiveCompleted: false,
                escaped: true);
            var revealedObjective = new PrivateObjectiveView(
                new PrivateObjectiveId("sekretny-cel:2"),
                PrivateObjectiveKind.SecretObjective,
                currentStep: null,
                completedStepCount: 2,
                totalStepCount: 2,
                isCompleted: true,
                new PlayerId(3));
            var reveal = new RoundRevealView(
                new[]
                {
                    new PlayerEndRevealView(new PlayerId(1), RoundRole.Detective, null,
                        new PlayerResultView(false, true, false, RoundEndCause.Escape, null)),
                    new PlayerEndRevealView(new PlayerId(2), RoundRole.Innocent, revealedObjective,
                        new PlayerResultView(true, true, false, RoundEndCause.Escape, null, true))
                },
                new[]
                {
                    new AlibiClueRevealView(
                        new AlibiClueId("receipt"),
                        "hidden-fact",
                        "Interpretacyjny Trop")
                },
                new EscapePlanRevealView(
                    new EscapePlanId("escape-prototype"),
                    new[]
                    {
                        new EscapeActionRevealView(
                            EscapeActionKind.Completed,
                            exitId: new EscapeExitId("escape-exit-a"))
                    },
                    new EscapeExitId("escape-exit-a")),
                new[]
                {
                    new IncidentRevealView(
                        new IncidentId("alarm-001"),
                        IncidentKind.Loud,
                        new IncidentEffectId("alarm"),
                        new IncidentLocationId("archive"),
                        new PlayerId(2))
                });
            var view = new PlayerRoundView(
                new PlayerId(2),
                RoundPhase.Finished,
                RoundRole.Innocent,
                "Publiczne Przestępstwo",
                alibi: null,
                privateObjective: null,
                result,
                Players,
                new PlayerId(1),
                roundReveal: reveal);

            RoundUiState state = RoundPresenter.BuildState(view, 0f, isHost: true);

            Assert.That(state.ResultText, Does.Contain("Ucieczka Winnego"));
            Assert.That(state.ResultText, Does.Contain("Cel Wrobienia: Gracz 3"));
            Assert.That(state.ResultText, Does.Contain("hidden-fact"));
            Assert.That(state.ResultText, Does.Contain("autor Gracz 2"));
            Assert.That(state.ResultText, Does.Contain("escape-exit-a"));
            Assert.That(state.ResultText, Does.Not.Contain("Pełne Alibi"));
            Assert.That(state.ReturnToLobbyVisible, Is.True);
        }

        private static PlayerRoundView View(
            RoundPhase phase,
            RoundRole role,
            AlibiView alibi,
            PlayerId? viewer = null,
            PlayerResultView result = null) =>
            new PlayerRoundView(
                viewer ?? new PlayerId(1),
                phase,
                role,
                "Ktoś przemalował pomnik.",
                alibi,
                secretObjective: null,
                result,
                Players,
                new PlayerId(1));
    }
}
