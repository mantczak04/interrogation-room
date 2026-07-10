using System.Linq;
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
        public void BuildState_DetectiveRound_OffersOnlyPublicSuspectIdsForOneExecutionIntent()
        {
            var state = RoundPresenter.BuildState(
                View(RoundPhase.Round, RoundRole.Detective, alibi: null),
                remainingSeconds: 83.2f,
                isHost: false);

            Assert.That(state.TimerVisible, Is.True);
            Assert.That(state.ExecutionVisible, Is.True);
            Assert.That(state.ExecutionTargets.Select(target => target.PlayerId),
                Is.EqualTo(new[] { new PlayerId(2), new PlayerId(3), new PlayerId(4) }));
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
