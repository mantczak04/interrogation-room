using System;
using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Domain;
using Mirror;
using NUnit.Framework;

namespace InterrogationRoom.Networking.Tests
{
    public sealed class RoundIntentMapperTests
    {
        [TestCase(4, true, 0)]
        [TestCase(4, false, 0)]
        [TestCase(5, true, 1)]
        [TestCase(5, false, 0)]
        [TestCase(6, true, 1)]
        [TestCase(6, false, 0)]
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
        public void MapperAlwaysUsesAuthenticatedSenderAsCommandOwner()
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
                    Is.True,
                    reason);
                Assert.That(CommandOwner(command), Is.EqualTo(sender), message.Kind.ToString());
            }
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
        public void MirrorSerialization_RoundTripsReturnToLobbyAndLobbyReset()
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
        }

        [Test]
        public void MapperRejectsMalformedStableIdsWithoutCallingDomain()
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
            Assert.That(reason, Is.Not.Empty);
        }

        private static PlayerId CommandOwner(RoundCommand command)
        {
            switch (command)
            {
                case RoundCommand.AdvancePrivateObjective value: return value.Player;
                case RoundCommand.RegisterIncident value: return value.Author;
                case RoundCommand.DiscoverQuietIncident value: return value.Detective;
                case RoundCommand.AcquireAlibiClue value: return value.Player;
                case RoundCommand.PrepareEscape value: return value.Player;
                case RoundCommand.BeginEscape value: return value.Player;
                case RoundCommand.InterruptEscape value: return value.Player;
                case RoundCommand.CompleteEscape value: return value.Player;
                default: throw new ArgumentOutOfRangeException(nameof(command));
            }
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
