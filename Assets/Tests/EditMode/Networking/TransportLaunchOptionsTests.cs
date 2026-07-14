using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace InterrogationRoom.Networking.Tests
{
    public sealed class TransportLaunchOptionsTests
    {
        [TestCase("-force-kcp")]
        [TestCase("-FORCE-KCP")]
        public void ForceKcp_ExplicitArgument_ReturnsTrue(string argument)
        {
            Assert.That(TransportLaunchOptions.ForceKcp(new[] { "game.exe", argument }), Is.True);
        }

        [Test]
        public void ForceKcp_MissingOrNullArgument_ReturnsFalse()
        {
            Assert.That(TransportLaunchOptions.ForceKcp(new[] { "game.exe" }), Is.False);
            Assert.That(TransportLaunchOptions.ForceKcp(null), Is.False);
        }
    }

    public sealed class GameLaunchRequestTests
    {
        private static Type LaunchRequestType => AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType("GameLaunchRequest"))
            .FirstOrDefault(type => type != null);

        [TearDown]
        public void ClearPendingRequest()
        {
            Invoke("Consume");
            Invoke("TryConsumeSteamLobbyJoin", 0UL);
        }

        [Test]
        public void TryParseSteamLobbyJoin_ValidArgument_ReturnsLobbyId()
        {
            object[] arguments =
            {
                new[] { "game.exe", "+connect_lobby", "123456789" },
                0UL
            };
            bool parsed = (bool)Invoke("TryParseSteamLobbyJoin", arguments);

            Assert.That(parsed, Is.True);
            Assert.That((ulong)arguments[1], Is.EqualTo(123456789UL));
        }

        [TestCase(null)]
        [TestCase("0")]
        [TestCase("not-a-lobby")]
        public void TryParseSteamLobbyJoin_InvalidArgument_ReturnsFalse(string lobbyArgument)
        {
            string[] arguments = lobbyArgument == null
                ? null
                : new[] { "game.exe", "+connect_lobby", lobbyArgument };

            object[] invocationArguments = { arguments, 0UL };

            Assert.That((bool)Invoke("TryParseSteamLobbyJoin", invocationArguments), Is.False);
            Assert.That((ulong)invocationArguments[1], Is.Zero);
        }

        [Test]
        public void SetSteamLobbyJoin_TransfersLobbyExactlyOnceAndRequestsJoinMode()
        {
            Invoke("SetSteamLobbyJoin", 987654321UL);

            Assert.That(ReadProperty("HasPendingSteamLobbyJoin"), Is.EqualTo(true));
            Assert.That(Invoke("Consume").ToString(), Is.EqualTo("Join"));

            object[] consumeArguments = { 0UL };
            Assert.That((bool)Invoke("TryConsumeSteamLobbyJoin", consumeArguments), Is.True);
            Assert.That((ulong)consumeArguments[0], Is.EqualTo(987654321UL));
            Assert.That((bool)Invoke("TryConsumeSteamLobbyJoin", 0UL), Is.False);
        }

        private static object Invoke(string methodName, params object[] arguments)
        {
            Type type = LaunchRequestType;
            Assert.That(type, Is.Not.Null, "Assembly-CSharp must expose GameLaunchRequest.");
            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"GameLaunchRequest.{methodName} is required.");
            return method.Invoke(null, arguments);
        }

        private static object ReadProperty(string propertyName)
        {
            Type type = LaunchRequestType;
            Assert.That(type, Is.Not.Null, "Assembly-CSharp must expose GameLaunchRequest.");
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(property, Is.Not.Null, $"GameLaunchRequest.{propertyName} is required.");
            return property.GetValue(null);
        }
    }
}
