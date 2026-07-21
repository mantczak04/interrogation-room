using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Domain;
using Mirror;
using NUnit.Framework;

namespace InterrogationRoom.Networking.Tests
{
    public sealed class RoundIntentMapperTests
    {
        [TestCase(3, true, 0)]
        [TestCase(3, false, 0)]
        [TestCase(4, true, 0)]
        [TestCase(4, false, 0)]
        [TestCase(5, true, 1)]
        [TestCase(5, false, 0)]
        [TestCase(6, true, 1)]
        [TestCase(6, false, 0)]
        [TestCase(8, true, 1)]
        [TestCase(8, false, 0)]
        [TestCase(9, true, 0)]
        public void LobbySecretObjectiveConfigurationFollowsApprovedPlayerCounts(
            int playerCount,
            bool hostAllowsSecretObjective,
            int expected)
        {
            Assert.That(
                RoundLobbyRules.ResolveSecretObjectiveCount(playerCount, hostAllowsSecretObjective),
                Is.EqualTo(expected));
        }

        [Test]
        public void IntentPayloadContainsNoClientAuthoredPlayerIdentity()
        {
            var forbiddenNames = new[] { "PlayerId", "Player", "Author", "Detective", "Owner" };
            var fields = typeof(RoundIntentMessage).GetFields();

            Assert.That(fields.Any(field => field.FieldType == typeof(PlayerId)), Is.False);
            Assert.That(fields.Any(field => forbiddenNames.Contains(field.Name)), Is.False);
        }

        [Test]
        public void MapperRejectsEveryClientAuthoredPhysicalResult()
        {
            var sender = new PlayerId(42);
            var timestamp = new IncidentTimestamp(1250);
            var messages = new[]
            {
                RoundIntentMessage.AdvancePrivateObjective(
                    new PrivateObjectiveId("objective"),
                    new PrivateObjectiveStepId("step")),
                RoundIntentMessage.RegisterIncident(
                    new IncidentId("incident"),
                    IncidentKind.Quiet,
                    new IncidentEffectId("effect"),
                    new IncidentLocationId("location"),
                    new PrivateObjectiveStepReference(
                        new PrivateObjectiveId("objective"),
                        new PrivateObjectiveStepId("step"))),
                RoundIntentMessage.DiscoverQuietIncident(new IncidentId("incident")),
                RoundIntentMessage.AcquireAlibiClue(
                    new AlibiClueId("clue"),
                    new IncidentId("clue-incident"),
                    IncidentKind.Quiet,
                    new IncidentEffectId("searched"),
                    new IncidentLocationId("archive")),
                RoundIntentMessage.PrepareEscape(
                    new EscapePlanId("plan"),
                    new EscapeStepId("prepare")),
                RoundIntentMessage.BeginEscape(
                    new EscapePlanId("plan"),
                    new EscapeExitId("exit"),
                    new IncidentId("escape-incident")),
                RoundIntentMessage.InterruptEscape(
                    new EscapePlanId("plan"),
                    new EscapeExitId("exit")),
                RoundIntentMessage.CompleteEscape(
                    new EscapePlanId("plan"),
                    new EscapeExitId("exit"))
            };

            foreach (var message in messages)
            {
                Assert.That(
                    RoundIntentMapper.TryMap(message, sender, timestamp, out var command, out var reason),
                    Is.False,
                    message.Kind.ToString());
                Assert.That(command, Is.Null, message.Kind.ToString());
                Assert.That(reason, Does.Contain("server-authoritative"), message.Kind.ToString());
            }
        }

        [Test]
        public void MapperAcceptsPlayerReadyForTheAuthenticatedSenderOnly()
        {
            var sender = new PlayerId(42);

            Assert.That(
                RoundIntentMapper.TryMap(
                    RoundIntentMessage.PlayerReady(),
                    sender,
                    new IncidentTimestamp(0),
                    out var command,
                    out var reason),
                Is.True);
            Assert.That(reason, Is.Null);
            var markReady = command as RoundCommand.MarkPlayerReady;
            Assert.That(markReady, Is.Not.Null);
            Assert.That(markReady.Player, Is.EqualTo(sender),
                "Gotowość is always attributed to the sender connection, never a payload identity.");
        }

        [Test]
        public void MirrorSerialization_RoundTripsPlayerReadyIntent()
        {
            RoundIntentMessage restored;
            using (var writer = NetworkWriterPool.Get())
            {
                writer.Write(RoundIntentMessage.PlayerReady());
                using (var reader = NetworkReaderPool.Get(writer.ToArraySegment()))
                    restored = reader.Read<RoundIntentMessage>();
            }

            Assert.That(restored.Kind, Is.EqualTo(RoundIntentKind.PlayerReady));
        }

        [Test]
        public void MirrorSerialization_RoundTripsEveryPhysicalIntentPayload()
        {
            var messages = new[]
            {
                RoundIntentMessage.AdvancePrivateObjective(
                    new PrivateObjectiveId("objective"),
                    new PrivateObjectiveStepId("step")),
                RoundIntentMessage.RegisterIncident(
                    new IncidentId("incident"),
                    IncidentKind.Loud,
                    new IncidentEffectId("effect"),
                    new IncidentLocationId("location"),
                    new PrivateObjectiveStepReference(
                        new PrivateObjectiveId("objective"),
                        new PrivateObjectiveStepId("step"))),
                RoundIntentMessage.DiscoverQuietIncident(new IncidentId("incident")),
                RoundIntentMessage.AcquireAlibiClue(
                    new AlibiClueId("clue"),
                    new IncidentId("clue-incident"),
                    IncidentKind.Quiet,
                    new IncidentEffectId("searched"),
                    new IncidentLocationId("archive")),
                RoundIntentMessage.PrepareEscape(new EscapePlanId("plan"), new EscapeStepId("prepare")),
                RoundIntentMessage.BeginEscape(
                    new EscapePlanId("plan"),
                    new EscapeExitId("exit"),
                    new IncidentId("escape-incident")),
                RoundIntentMessage.InterruptEscape(new EscapePlanId("plan"), new EscapeExitId("exit")),
                RoundIntentMessage.CompleteEscape(new EscapePlanId("plan"), new EscapeExitId("exit"))
            };

            foreach (var source in messages)
            {
                RoundIntentMessage restored;
                using (var writer = NetworkWriterPool.Get())
                {
                    writer.Write(source);
                    using (var reader = NetworkReaderPool.Get(writer.ToArraySegment()))
                        restored = reader.Read<RoundIntentMessage>();
                }

                Assert.That(IntentFields(restored), Is.EqualTo(IntentFields(source)), source.Kind.ToString());
            }
        }

        [Test]
        public void MirrorSerialization_RoundTripsLobbyMessages()
        {
            RoundIntentMessage source = RoundIntentMessage.ReturnToLobby();
            RoundIntentMessage restored;
            using (var writer = NetworkWriterPool.Get())
            {
                writer.Write(source);
                using (var reader = NetworkReaderPool.Get(writer.ToArraySegment()))
                    restored = reader.Read<RoundIntentMessage>();
            }

            Assert.That(restored.Kind, Is.EqualTo(RoundIntentKind.ReturnToLobby));

            using (var writer = NetworkWriterPool.Get())
            {
                writer.Write(new RoundLobbyResetMessage());
                using (var reader = NetworkReaderPool.Get(writer.ToArraySegment()))
                    Assert.That(reader.Read<RoundLobbyResetMessage>(), Is.TypeOf<RoundLobbyResetMessage>());
            }

            var lobbyState = new RoundLobbyStateMessage
            {
                PlayerCount = 2,
                SecretObjectiveEnabled = true,
                Players = new[]
                {
                    new RoundLobbyPlayerMessage
                    {
                        PlayerId = 0,
                        NetworkIdentityNetId = 42,
                        DisplayName = "Łukasz Śledź",
                        IsHost = true,
                        IsSimulated = false,
                        IsReady = true
                    },
                    new RoundLobbyPlayerMessage
                    {
                        PlayerId = -1,
                        DisplayName = "Alicja Żur",
                        IsSimulated = true
                    }
                }
            };
            using (var writer = NetworkWriterPool.Get())
            {
                writer.Write(lobbyState);
                using (var reader = NetworkReaderPool.Get(writer.ToArraySegment()))
                {
                    RoundLobbyStateMessage restoredLobby = reader.Read<RoundLobbyStateMessage>();
                    Assert.That(restoredLobby.PlayerCount, Is.EqualTo(2));
                    Assert.That(restoredLobby.SecretObjectiveEnabled, Is.True);
                    Assert.That(restoredLobby.Players, Has.Length.EqualTo(2));
                    Assert.That(restoredLobby.Players[0].DisplayName, Is.EqualTo("Łukasz Śledź"));
                    Assert.That(restoredLobby.Players[0].NetworkIdentityNetId, Is.EqualTo(42));
                    Assert.That(restoredLobby.Players[0].IsHost, Is.True);
                    Assert.That(restoredLobby.Players[0].IsReady, Is.True);
                    Assert.That(restoredLobby.Players[1].IsSimulated, Is.True);
                }
            }

            using (var writer = NetworkWriterPool.Get())
            {
                writer.Write(new RoundLobbyReadyMessage { IsReady = true });
                using (var reader = NetworkReaderPool.Get(writer.ToArraySegment()))
                    Assert.That(reader.Read<RoundLobbyReadyMessage>().IsReady, Is.True);
            }

            using (var writer = NetworkWriterPool.Get())
            {
                writer.Write(new RoundLobbyProfileMessage { DisplayName = "Zośka Bąk" });
                using (var reader = NetworkReaderPool.Get(writer.ToArraySegment()))
                    Assert.That(reader.Read<RoundLobbyProfileMessage>().DisplayName, Is.EqualTo("Zośka Bąk"));
            }

            Assert.That(typeof(RoundLobbyStateMessage).GetFields().Select(field => field.Name),
                Is.EqualTo(new[]
                {
                    nameof(RoundLobbyStateMessage.PlayerCount),
                    nameof(RoundLobbyStateMessage.SecretObjectiveEnabled),
                    nameof(RoundLobbyStateMessage.Players)
                }),
                "Public lobby synchronization must not grow into a secret-bearing payload.");
        }

        [Test]
        public void MapperRejectsPhysicalIntentBeforeParsingItsPayload()
        {
            var malformed = new RoundIntentMessage
            {
                Kind = RoundIntentKind.AdvancePrivateObjective,
                ObjectiveId = "",
                ObjectiveStepId = "step"
            };

            Assert.That(
                RoundIntentMapper.TryMap(
                    malformed,
                    new PlayerId(42),
                    new IncidentTimestamp(100),
                    out var command,
                    out var reason),
                Is.False);
            Assert.That(command, Is.Null);
            Assert.That(reason, Does.Contain("server-authoritative"));
        }

        private static IReadOnlyList<object> IntentFields(RoundIntentMessage message) => new object[]
        {
            message.Kind,
            message.ObjectiveId,
            message.ObjectiveStepId,
            message.HasObjectiveStepReference,
            message.IncidentId,
            message.IncidentKind,
            message.EffectId,
            message.LocationId,
            message.AlibiClueId,
            message.EscapePlanId,
            message.EscapeStepId,
            message.EscapeExitId
        };
    }
}
