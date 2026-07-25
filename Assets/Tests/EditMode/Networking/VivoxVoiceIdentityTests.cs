using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace InterrogationRoom.Gameplay.Tests
{
    public sealed class VivoxVoiceIdentityTests
    {
        private const BindingFlags StaticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;

        [Test]
        public void SessionScopesChannelAndPlayerIdentity()
        {
            const string firstSession = "kcp-first-host-session";
            const string secondSession = "kcp-second-host-session";

            string firstChannel = BuildChannelName(firstSession);
            string sameSessionChannel = BuildChannelName(firstSession);
            string secondChannel = BuildChannelName(secondSession);
            string firstPlayer = BuildPlayerId(firstSession, 1);
            string secondPlayer = BuildPlayerId(secondSession, 1);
            string reconnectedPlayer = BuildPlayerId(firstSession, 2);

            Assert.That(sameSessionChannel, Is.EqualTo(firstChannel),
                "Every client given the same host session id must join the same positional channel.");
            Assert.That(secondChannel, Is.Not.EqualTo(firstChannel),
                "Independent KCP hosts must not share a positional channel.");
            Assert.That(secondPlayer, Is.Not.EqualTo(firstPlayer),
                "A repeated Mirror netId must still have a session-scoped Vivox identity.");
            Assert.That(reconnectedPlayer, Is.Not.EqualTo(firstPlayer),
                "A reconnect with a new Mirror netId must use a new Vivox identity.");
        }

        [Test]
        public void PlayerIdentityMapsOnlyInsideItsCurrentSession()
        {
            const string currentSession = "kcp-current-session";
            const string otherSession = "kcp-other-session";
            string playerId = BuildPlayerId(currentSession, 42);

            Assert.That(TryParsePlayerId(currentSession, playerId, out uint netId), Is.True);
            Assert.That(netId, Is.EqualTo(42));
            Assert.That(TryParsePlayerId(otherSession, playerId, out _), Is.False,
                "A participant from another voice session must never bind to a local Mirror identity.");
        }

        [Test]
        public void LobbyAndRoundUseSeparateSharedModesInsideTheSameSession()
        {
            const string session = "kcp-shared-session";

            string lobby = BuildModeChannelName(session, spatial: false);
            string round = BuildModeChannelName(session, spatial: true);

            Assert.That(lobby, Does.EndWith("-lobby"));
            Assert.That(round, Does.EndWith("-round"));
            Assert.That(lobby, Is.Not.EqualTo(round));
        }

        private static string BuildChannelName(string sessionId) =>
            (string)GetRuntimeMethod("BuildChannelName").Invoke(
                null,
                new object[] { "interrogation-room", sessionId });

        private static string BuildPlayerId(string sessionId, uint netId) =>
            (string)GetRuntimeMethod("BuildPlayerId").Invoke(null, new object[] { sessionId, netId });

        private static string BuildModeChannelName(string sessionId, bool spatial) =>
            (string)GetRuntimeMethod("BuildModeChannelName").Invoke(
                null,
                new object[] { "interrogation-room", sessionId, spatial });

        private static bool TryParsePlayerId(string sessionId, string playerId, out uint netId)
        {
            object[] arguments = { sessionId, playerId, 0u };
            bool parsed = (bool)GetRuntimeMethod("TryParsePlayerId").Invoke(null, arguments);
            netId = (uint)arguments[2];
            return parsed;
        }

        private static MethodInfo GetRuntimeMethod(string methodName) =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("VivoxVoiceRuntime", false))
                .First(type => type != null)
                .GetMethod(methodName, StaticNonPublic);
    }
}
