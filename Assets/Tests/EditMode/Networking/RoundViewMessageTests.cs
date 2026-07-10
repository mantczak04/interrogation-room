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

            var message = RoundViewMessage.FromView(source, remainingSeconds: 73.5f);
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
            Assert.That(message.RemainingSeconds, Is.EqualTo(73.5f));
        }

        [Test]
        public void FromView_DetectivePayloadContainsNoAlibiShape()
        {
            var engine = StartedEngine();
            var detective = Players.Single(player => engine.ViewFor(player).Role == RoundRole.Detective);

            var message = RoundViewMessage.FromView(engine.ViewFor(detective), remainingSeconds: 0f);
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
            var suspect = Players.First(player => engine.ViewFor(player).Role != RoundRole.Detective);
            engine.Handle(new RoundCommand.EndPreparation());

            var message = RoundViewMessage.FromView(engine.ViewFor(suspect), remainingSeconds: 90f);

            Assert.That(message.HasAlibi, Is.False);
            Assert.That(message.AlibiEntries, Is.Empty);
            Assert.That(message.ToView().Alibi, Is.Null);
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
            var message = RoundViewMessage.FromView(source, remainingSeconds: 0f);

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
    }
}
