using System;
using System.Linq;
using InterrogationRoom.Domain;
using Mirror;
using NUnit.Framework;

namespace InterrogationRoom.Networking.Tests
{
    public sealed class RoundViewMessageTests
    {
        private static readonly PlayerId[] Players =
        {
            new PlayerId(10),
            new PlayerId(11),
            new PlayerId(12),
            new PlayerId(13),
            new PlayerId(14)
        };

        private static CaseDefinition TestCase() =>
            new CaseDefinition(
                "Różowy pomnik",
                "Ktoś przemalował pomnik burmistrza na różowo.",
                new[]
                {
                    new AlibiFact("f0", "Spotkaliśmy się o dziewiętnastej.", false),
                    new AlibiFact("f1", "Kelner pomylił rachunek.", true),
                    new AlibiFact("f2", "Na chwilę zgasło światło.", true),
                    new AlibiFact("f3", "Wróciliśmy razem tramwajem.", false)
                },
                minHiddenFacts: 1,
                maxHiddenFacts: 1);

        private static RoundEngine StartedEngine()
        {
            var engine = new RoundEngine();
            var transition = engine.Handle(new RoundCommand.StartRound(TestCase(), Players, seed: 17));
            Assert.That(transition.Accepted, Is.True, transition.RejectionReason);
            return engine;
        }

        [Test]
        public void FromView_RoundTripsOnlyTheRecipientsPrivateViewAndTimer()
        {
            var engine = StartedEngine();
            var recipient = Players.First(player => engine.ViewFor(player).Role != RoundRole.Detective);
            var source = engine.ViewFor(recipient);

            var message = RoundViewMessage.FromView(source, roundEndsAtNetworkTime: 173.5d);
            var restored = message.ToView();

            Assert.That(restored.Viewer, Is.EqualTo(source.Viewer));
            Assert.That(restored.Phase, Is.EqualTo(source.Phase));
            Assert.That(restored.Role, Is.EqualTo(source.Role));
            Assert.That(restored.Players, Is.EqualTo(source.Players));
            Assert.That(restored.Detective, Is.EqualTo(source.Detective));
            Assert.That(restored.CrimeDescription, Is.EqualTo(source.CrimeDescription));
            Assert.That(restored.Alibi.Entries.Select(entry => entry.FactId),
                Is.EqualTo(source.Alibi.Entries.Select(entry => entry.FactId)));
            Assert.That(restored.Alibi.Entries.Select(entry => entry.IsHidden),
                Is.EqualTo(source.Alibi.Entries.Select(entry => entry.IsHidden)));
            Assert.That(restored.Alibi.Entries.Select(entry => entry.Text),
                Is.EqualTo(source.Alibi.Entries.Select(entry => entry.Text)));
            Assert.That(message.RoundEndsAtNetworkTime, Is.EqualTo(173.5d));
        }

        [Test]
        public void RoundIntentWireContract_HasNoClientAuthoredExecution()
        {
            Assert.That(Enum.GetNames(typeof(RoundIntentKind)), Does.Not.Contain("Execute"));
        }

        [Test]
        public void FromView_DetectivePayloadContainsNoAlibiShape()
        {
            var engine = StartedEngine();
            var detective = Players.Single(player => engine.ViewFor(player).Role == RoundRole.Detective);

            var message = RoundViewMessage.FromView(engine.ViewFor(detective), roundEndsAtNetworkTime: 0d);
            var restored = message.ToView();

            Assert.That(message.HasAlibi, Is.False);
            Assert.That(message.AlibiEntries, Is.Empty,
                "Detektyw must not receive fact count or redaction markers.");
            Assert.That(restored.Alibi, Is.Null);
        }

        [Test]
        public void FromView_AfterPreparationPayloadContainsNoAlibi()
        {
            var engine = StartedEngine();
            var suspect = Players.First(player => engine.ViewFor(player).Role == RoundRole.Innocent);
            engine.Handle(new RoundCommand.EndPreparation());

            var message = RoundViewMessage.FromView(engine.ViewFor(suspect), roundEndsAtNetworkTime: 190d);

            Assert.That(message.HasAlibi, Is.False);
            Assert.That(message.AlibiEntries, Is.Empty);
            Assert.That(message.ToView().Alibi, Is.Null);
            Assert.That(message.ToView().PrivateObjective, Is.Not.Null,
                "Reconnect restores the owner's current Cel but never the hidden Alibi.");
        }

        [Test]
        public void MirrorSerialization_RoundTripsRecipientOptionalFields()
        {
            var source = new PlayerRoundView(
                new PlayerId(12),
                RoundPhase.Finished,
                RoundRole.Innocent,
                "Ktoś przemalował pomnik burmistrza na różowo.",
                alibi: null,
                new SecretObjectiveView(new PlayerId(13)),
                new PlayerResultView(
                    won: true,
                    survived: true,
                    detectiveWon: false,
                    RoundEndCause.Execution,
                    new PlayerId(14)),
                new[] { new PlayerId(10), new PlayerId(11), new PlayerId(12), new PlayerId(13), new PlayerId(14) },
                new PlayerId(10));
            var message = RoundViewMessage.FromView(source, roundEndsAtNetworkTime: 0d);

            RoundViewMessage restoredMessage;
            using (var writer = NetworkWriterPool.Get())
            {
                NetworkMessages.Pack(message, writer);
                using (var reader = NetworkReaderPool.Get(writer.ToArraySegment()))
                {
                    Assert.That(NetworkMessages.UnpackId(reader, out var messageId), Is.True);
                    Assert.That(messageId, Is.EqualTo(NetworkMessages.GetId<RoundViewMessage>()));
                    restoredMessage = reader.Read<RoundViewMessage>();
                }
            }

            var restored = restoredMessage.ToView();
            Assert.That(restored.SecretObjective.Target, Is.EqualTo(new PlayerId(13)));
            Assert.That(restored.Result.Won, Is.True);
            Assert.That(restored.Result.Survived, Is.True);
            Assert.That(restored.Result.DetectiveWon, Is.False);
            Assert.That(restored.Result.EndCause, Is.EqualTo(RoundEndCause.Execution));
            Assert.That(restored.Result.ExecutedPlayer, Is.EqualTo(new PlayerId(14)));
            Assert.That(restored.Players.Select(player => player.Value), Is.EqualTo(new[] { 10, 11, 12, 13, 14 }));
            Assert.That(restored.Detective, Is.EqualTo(new PlayerId(10)));
        }

        [Test]
        public void MirrorSerialization_RoundTripsAllExtendedPrivateFieldsAndReveal()
        {
            var objective = new PrivateObjectiveView(
                new PrivateObjectiveId("sekretny-cel"),
                PrivateObjectiveKind.SecretObjective,
                new PrivateObjectiveStepId("wrobienie-podloz"),
                completedStepCount: 1,
                totalStepCount: 2,
                isCompleted: false,
                new PlayerId(13));
            var result = new PlayerResultView(
                won: true,
                survived: true,
                detectiveWon: false,
                RoundEndCause.Escape,
                executedPlayer: null,
                privateObjectiveCompleted: true,
                escaped: false);
            var revealedIncident = new IncidentRevealView(
                new IncidentId("revealed"),
                IncidentKind.Quiet,
                new IncidentEffectId("missing-file"),
                new IncidentLocationId("archive"),
                new PlayerId(12));
            var source = new PlayerRoundView(
                new PlayerId(12),
                RoundPhase.Finished,
                RoundRole.Innocent,
                "Publiczne Przestępstwo",
                alibi: null,
                objective,
                result,
                Players,
                new PlayerId(10),
                incidentRegistry: new[]
                {
                    new IncidentRegistryEntryView(
                        new IncidentId("registry"),
                        IncidentKind.Loud,
                        new IncidentEffectId("alarm"),
                        new IncidentLocationId("hall"),
                        new IncidentTimestamp(750))
                },
                revealedIncidents: new[] { revealedIncident },
                acquiredAlibiClues: new[]
                {
                    new AlibiClueView(new AlibiClueId("clue"), "Interpretowalny Trop")
                },
                escapePlan: new EscapePlanView(
                    new EscapePlanId("plan"),
                    new EscapeStepId("prepare-exit"),
                    completedCommonStepCount: 2,
                    totalCommonStepCount: 2,
                    isPrepared: true,
                    new EscapeExitId("exit-a"),
                    new[]
                    {
                        new EscapeExitOptionView(
                            new EscapeExitId("exit-a"),
                            new EscapeStepId("prepare-a"),
                            new IncidentLocationId("yard"),
                            isPrepared: true),
                        new EscapeExitOptionView(
                            new EscapeExitId("exit-b"),
                            new EscapeStepId("prepare-b"),
                            new IncidentLocationId("roof"),
                            isPrepared: false)
                    }),
                roundReveal: new RoundRevealView(
                    new[]
                    {
                        new PlayerEndRevealView(new PlayerId(12), RoundRole.Innocent, objective, result)
                    },
                    new[]
                    {
                        new AlibiClueRevealView(
                            new AlibiClueId("clue"),
                            "hidden-fact",
                            "Interpretowalny Trop")
                    },
                    new EscapePlanRevealView(
                        new EscapePlanId("plan"),
                        new[]
                        {
                            new EscapeActionRevealView(
                                EscapeActionKind.PreparedCommonStep,
                                new EscapeStepId("prepare")),
                            new EscapeActionRevealView(
                                EscapeActionKind.Completed,
                                exitId: new EscapeExitId("exit-a"))
                        },
                        new EscapeExitId("exit-a")),
                    new[] { revealedIncident }));

            var restored = RoundTrip(RoundViewMessage.FromView(source, 0d)).ToView();

            Assert.That(restored.PrivateObjective.Id, Is.EqualTo(objective.Id));
            Assert.That(restored.PrivateObjective.CurrentStep, Is.EqualTo(objective.CurrentStep));
            Assert.That(restored.PrivateObjective.Target, Is.EqualTo(objective.Target));
            Assert.That(restored.IncidentRegistry.Single().ReportedAt, Is.EqualTo(new IncidentTimestamp(750)));
            Assert.That(restored.RevealedIncidents.Single().Author, Is.EqualTo(new PlayerId(12)));
            Assert.That(restored.AcquiredAlibiClues.Single().Content, Is.EqualTo("Interpretowalny Trop"));
            Assert.That(restored.EscapePlan.ExitOptions.Select(option => option.Location.Value),
                Is.EqualTo(new[] { "yard", "roof" }));
            Assert.That(restored.Result.PrivateObjectiveCompleted, Is.True);
            Assert.That(restored.Result.Escaped, Is.False);
            Assert.That(restored.RoundReveal.Players.Single().PrivateObjective.Target,
                Is.EqualTo(new PlayerId(13)));
            Assert.That(restored.RoundReveal.AcquiredAlibiClues.Single().LinkedFactId,
                Is.EqualTo("hidden-fact"));
            Assert.That(restored.RoundReveal.EscapePlan.Actions.Select(action => action.Kind),
                Is.EqualTo(new[] { EscapeActionKind.PreparedCommonStep, EscapeActionKind.Completed }));
            Assert.That(restored.RoundReveal.Incidents.Single().Author, Is.EqualTo(new PlayerId(12)));
        }

        [Test]
        public void LivePayloadsContainOnlyTheRecipientsRoleSpecificPrivateData()
        {
            var engine = StartedEngine();
            var detective = Players.Single(player => engine.ViewFor(player).Role == RoundRole.Detective);
            var guilty = Players.Single(player => engine.ViewFor(player).Role == RoundRole.Guilty);
            var innocent = Players.First(player => engine.ViewFor(player).Role == RoundRole.Innocent);
            engine.Handle(new RoundCommand.EndPreparation());
            Assert.That(engine.Handle(new RoundCommand.RegisterIncident(
                innocent,
                new IncidentId("public-effect-private-author"),
                IncidentKind.Loud,
                new IncidentEffectId("alarm"),
                new IncidentLocationId("hall"),
                new IncidentTimestamp(500))).Accepted, Is.True);

            var detectiveMessage = RoundViewMessage.FromView(engine.ViewFor(detective), 100d);
            var guiltyMessage = RoundViewMessage.FromView(engine.ViewFor(guilty), 100d);
            var innocentMessage = RoundViewMessage.FromView(engine.ViewFor(innocent), 100d);

            Assert.That(detectiveMessage.HasIncidentRegistry, Is.True);
            Assert.That(detectiveMessage.IncidentRegistry, Has.Length.EqualTo(1));
            Assert.That(typeof(IncidentRegistryEntryMessage).GetField("AuthorPlayerId"), Is.Null);
            Assert.That(detectiveMessage.HasPrivateObjective, Is.False);
            Assert.That(detectiveMessage.HasAcquiredAlibiClues, Is.False);
            Assert.That(detectiveMessage.HasEscapePlan, Is.False);

            Assert.That(guiltyMessage.HasAcquiredAlibiClues, Is.True);
            Assert.That(guiltyMessage.HasEscapePlan, Is.True);
            Assert.That(guiltyMessage.HasPrivateObjective, Is.False);
            Assert.That(guiltyMessage.HasIncidentRegistry, Is.False);

            Assert.That(innocentMessage.HasPrivateObjective, Is.True);
            Assert.That(innocentMessage.PrivateObjective.TargetPlayerId,
                Is.EqualTo(engine.ViewFor(innocent).PrivateObjective.Target?.Value ?? 0));
            Assert.That(innocentMessage.HasIncidentRegistry, Is.False);
            Assert.That(innocentMessage.HasAcquiredAlibiClues, Is.False);
            Assert.That(innocentMessage.HasEscapePlan, Is.False);

            Assert.That(new[]
            {
                detectiveMessage.HasRevealedIncidents,
                guiltyMessage.HasRevealedIncidents,
                innocentMessage.HasRevealedIncidents
            }, Is.All.False);
        }

        [Test]
        public void SerializerRejectsUnboundedEscapeOptionPayload()
        {
            var message = RoundViewMessage.FromView(StartedEngine().ViewFor(Players[0]), 0d);
            message.HasEscapePlan = true;
            message.EscapePlan = new EscapePlanMessage
            {
                Id = "oversized",
                ExitOptions = new EscapeExitOptionMessage[33]
            };

            using (var writer = NetworkWriterPool.Get())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => writer.Write(message));
            }
        }

        private static RoundViewMessage RoundTrip(RoundViewMessage source)
        {
            using (var writer = NetworkWriterPool.Get())
            {
                writer.Write(source);
                using (var reader = NetworkReaderPool.Get(writer.ToArraySegment()))
                    return reader.Read<RoundViewMessage>();
            }
        }
    }
}
