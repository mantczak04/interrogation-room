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
            Assert.That(state.PreparationInstructionText, Does.Contain("Przesłuchaj każdego Podejrzanego"));
            Assert.That(state.PreparationInstructionText, Does.Contain("jedną Egzekucję"));
            Assert.That(state.PreparationInstructionText, Does.Not.Contain("Alibi składa"),
                "The Detective must not learn the private Alibi's shape.");
            Assert.That(state.ReadyButtonVisible, Is.True,
                "Every player, including the Detektyw, gets the Gotowy button.");
            Assert.That(state.ReadyButtonEnabled, Is.True);
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
            Assert.That(state.AlibiText, Does.Contain("1. Jawny fakt."));
            Assert.That(state.AlibiText, Does.Contain("2. [Brak w Twojej wersji Alibi]"));
            Assert.That(state.PreparationInstructionText, Does.Contain("nie będzie można jej ponownie otworzyć"));
            Assert.That(state.ReadyButtonVisible, Is.True);
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
        public void BuildState_AfterPreparation_ClearsAlibiEvenIfViewStillCarriesOne()
        {
            var alibi = new AlibiView(new[] { new AlibiEntry("f1", false, "Jawny fakt.") });

            var roundState = RoundPresenter.BuildState(
                View(RoundPhase.Round, RoundRole.Innocent, alibi),
                remainingSeconds: 60f,
                isHost: false);
            var finishedState = RoundPresenter.BuildState(
                View(RoundPhase.Finished, RoundRole.Innocent, alibi,
                    result: new PlayerResultView(false, true, false, RoundEndCause.TimeExpired, null)),
                remainingSeconds: 0f,
                isHost: false);

            Assert.That(roundState.AlibiVisible, Is.False);
            Assert.That(roundState.AlibiText, Is.Null);
            Assert.That(roundState.ReadyButtonVisible, Is.False);
            Assert.That(finishedState.AlibiVisible, Is.False);
            Assert.That(finishedState.AlibiText, Is.Null);
        }

        [Test]
        public void BuildState_Preparation_RendersReadyCountAndIrreversibleSelfReadiness()
        {
            var readyView = new PlayerRoundView(
                new PlayerId(1),
                RoundPhase.Preparation,
                RoundRole.Innocent,
                "Ktoś przemalował pomnik.",
                new AlibiView(new[] { new AlibiEntry("f1", false, "Jawny fakt.") }),
                secretObjective: null,
                result: null,
                Players,
                new PlayerId(1),
                readyPlayerCount: 2,
                isReady: true);

            var state = RoundPresenter.BuildState(
                readyView,
                remainingSeconds: 0f,
                isHost: false,
                unlimitedTime: false,
                preparationRemainingSeconds: 12.4f);

            Assert.That(state.ReadyButtonVisible, Is.True);
            Assert.That(state.ReadyButtonEnabled, Is.False,
                "Gotowość is irreversible — the button stays disabled after clicking.");
            Assert.That(state.ReadyCountText, Is.EqualTo("Gotowi: 2/4"));
            Assert.That(state.PreparationTimerVisible, Is.True);
            Assert.That(state.PreparationRemainingSeconds, Is.EqualTo(12.4f));
        }

        [Test]
        public void CalculatePreparationRemainingSeconds_CountsDownOnlyDuringPreparation()
        {
            Assert.That(
                RoundPresenter.CalculatePreparationRemainingSeconds(130.5d, 100.25d, RoundPhase.Preparation),
                Is.EqualTo(30.25f));
            Assert.That(
                RoundPresenter.CalculatePreparationRemainingSeconds(130.5d, 200d, RoundPhase.Preparation),
                Is.Zero);
            Assert.That(
                RoundPresenter.CalculatePreparationRemainingSeconds(0d, 100d, RoundPhase.Preparation),
                Is.Zero,
                "A developer scenario has no preparation deadline to render.");
            Assert.That(
                RoundPresenter.CalculatePreparationRemainingSeconds(130.5d, 100.25d, RoundPhase.Round),
                Is.Zero);
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

        [TestCase(RoundPhase.Round, 0d, true)]
        [TestCase(RoundPhase.Round, -1d, true)]
        [TestCase(RoundPhase.Round, 10d, false)]
        [TestCase(RoundPhase.Preparation, 0d, false)]
        public void IsUnlimitedRound_UsesZeroDeadlineOnlyDuringRunda(
            RoundPhase phase,
            double deadline,
            bool expected)
        {
            Assert.That(RoundPresenter.IsUnlimitedRound(phase, deadline), Is.EqualTo(expected));
        }

        [TestCase(60f, false)]
        [TestCase(59.99f, true)]
        [TestCase(1f, true)]
        [TestCase(0f, false)]
        public void IsCriticalRoundTime_UsesApprovedFinalMinuteWindow(float remaining, bool expected)
        {
            Assert.That(RoundPresenter.IsCriticalRoundTime(remaining), Is.EqualTo(expected));
        }

        [Test]
        public void BuildState_DeveloperRound_ShowsUnlimitedTime()
        {
            var state = RoundPresenter.BuildState(
                View(RoundPhase.Round, RoundRole.Innocent, alibi: null),
                remainingSeconds: 0f,
                isHost: true,
                unlimitedTime: true);

            Assert.That(state.TimerVisible, Is.True);
            Assert.That(state.UnlimitedTime, Is.True);
        }

        [TestCase(RoundPhase.Lobby, false, false, true)]
        [TestCase(RoundPhase.Preparation, false, false, true)]
        [TestCase(RoundPhase.Round, false, false, false)]
        [TestCase(RoundPhase.Round, true, false, true)]
        [TestCase(RoundPhase.Round, false, true, true)]
        [TestCase(RoundPhase.Finished, false, false, true)]
        public void ShouldReleaseCursor_CoordinatesFullScreenPhasesAndGameplay(
            RoundPhase phase,
            bool developerMenuOpen,
            bool requiresPointer,
            bool expected)
        {
            Assert.That(
                RoundPresenter.ShouldReleaseCursor(phase, developerMenuOpen, requiresPointer),
                Is.EqualTo(expected));
        }

        [Test]
        public void PlayerInputGate_UsesOneStateForUiAndPlayerControllerBridge()
        {
            try
            {
                PlayerInputGate.SetUiInputBlocked(true);
                PlayerInputGate.SetPlayerCursorReleased(false);
                Assert.That(PlayerInputGate.CursorReleased, Is.True,
                    "Gameplay code must not reclaim input while full-screen UI is active.");

                PlayerInputGate.SetUiInputBlocked(false);
                Assert.That(PlayerInputGate.CursorReleased, Is.False);

                PlayerInputGate.SetPlayerCursorReleased(true);
                Assert.That(PlayerInputGate.CursorReleased, Is.True);

                PlayerInputGate.SetUiInputBlocked(false);
                Assert.That(PlayerInputGate.CursorReleased, Is.True,
                    "A repeated gameplay render must preserve a player-opened cursor.");
            }
            finally
            {
                PlayerInputGate.SetUiInputBlocked(true);
            }
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
            Assert.That(state.ResultVerdictText, Is.EqualTo("PRZEGRANA"));
            Assert.That(state.ResultReasonText, Is.EqualTo("Egzekucja została wykonana na Tobie."));
            Assert.That(state.ResultIsLoss, Is.True);
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
            Assert.That(state.PrivateText, Does.Contain("Archive alarm"));
            Assert.That(state.PrivateText, Does.Not.Contain("Gracz"));
        }

        [Test]
        public void BuildState_DetectiveRegistry_ListsNewestIncidentFirstWithoutTechnicalIds()
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
                        new IncidentId("older-id"),
                        IncidentKind.Quiet,
                        new IncidentEffectId("otwarte-akta"),
                        new IncidentLocationId("archiwum"),
                        new IncidentTimestamp(15000)),
                    new IncidentRegistryEntryView(
                        new IncidentId("newer-id"),
                        IncidentKind.Loud,
                        new IncidentEffectId("alarm-drzwi"),
                        new IncidentLocationId("magazyn-dowodow"),
                        new IncidentTimestamp(65000))
                });

            RoundUiState state = RoundPresenter.BuildState(view, 120f, isHost: false);

            Assert.That(state.PrivateText.IndexOf("01:05"), Is.LessThan(state.PrivateText.IndexOf("00:15")));
            Assert.That(state.PrivateText, Does.Contain("Magazyn dowodow"));
            Assert.That(state.PrivateText, Does.Not.Contain("newer-id"));
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

            Assert.That(state.PrivateStep, Does.Contain("wrobienie-podloz"));
            Assert.That(state.PrivateProgress, Does.Contain("1/2"));
            Assert.That(state.PrivateText, Does.Contain("Gracz 3"));
            Assert.That(state.PrivateStep, Does.Not.Contain("osobista-sprawa-zakoncz"));
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
            Assert.That(state.ResultText, Does.Contain("Interpretacyjny Trop"));
            Assert.That(state.ResultText, Does.Contain("autor Gracz 2"));
            Assert.That(state.ResultText, Does.Contain("przygotowany punkt Ucieczki"));
            Assert.That(state.ResultText, Does.Not.Contain("hidden-fact"));
            Assert.That(state.ResultText, Does.Not.Contain("escape-exit-a"));
            Assert.That(state.ResultText, Does.Not.Contain("Pełne Alibi"));
            Assert.That(state.ResultVerdictText, Is.EqualTo("WYGRANA"));
            Assert.That(state.ResultReasonText, Is.EqualTo("Winny ukończył Ucieczkę."));
            Assert.That(state.ResultIsLoss, Is.False);
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
