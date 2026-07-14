using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Tests
{
    public sealed class GuiltyPhysicalTracerTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();
        private bool _originalServerActive;

        [SetUp]
        public void SetUp()
        {
            _originalServerActive = NetworkServer.active;
        }

        [TearDown]
        public void TearDown()
        {
            SetServerActive(_originalServerActive);
            foreach (GameObject createdObject in _createdObjects)
                UnityEngine.Object.DestroyImmediate(createdObject);
            _createdObjects.Clear();
        }

        [Test]
        public void AlibiClueEmitsA3IdsButStoresNeitherHiddenFactNorPrivateClueText()
        {
            Type clueType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.NetworkAlibiClueAction");
            Component clue = CreateTimedAction("Receipt clue", clueType);
            SetField(clue, "clueId", "paragon-cztery-kompoty");
            SetField(clue, "incidentIdPrefix", "receipt-search");
            SetField(clue, "effectId", "searched-confiscated-property");
            SetField(clue, "locationId", "evidence-room");
            SetField(clue, "publicPropDescription", "Crumpled receipt");
            NetworkIdentity actor = CreateIdentity("Suspect");
            int clueSignals = 0;
            SubscribeCountingHandler(clue, clueType.GetEvent("ClueAcquiredServer"), () => clueSignals++);

            SetServerActive(true);
            Assert.That(clueType.GetMethod("TryBeginInteractionServer")
                .Invoke(clue, new object[] { actor }), Is.True);
            Assert.That(clueType.GetMethod("TryCompleteInteractionServer")
                .Invoke(clue, new object[] { actor }), Is.True);

            Assert.That(clueSignals, Is.EqualTo(1));
            Assert.That(clueType.GetProperty("ClueId").GetValue(clue),
                Is.EqualTo("paragon-cztery-kompoty"));
            Assert.That(clueType.GetProperty("LastIncidentId").GetValue(clue),
                Is.EqualTo("receipt-search-001"));
            Assert.That(clueType.GetProperty("PublicPropDescription").GetValue(clue),
                Is.EqualTo("Crumpled receipt"));
            Assert.That(clueType.GetProperty("LinkedFactId"), Is.Null);
            Assert.That(clueType.GetProperty("AuthoredClueText"), Is.Null);
            Assert.That(clueType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Any(field => field.Name.IndexOf("fact", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              field.Name.IndexOf("content", StringComparison.OrdinalIgnoreCase) >= 0),
                Is.False,
                "The private authored clue arrives only through A3's targeted player view.");
        }

        [Test]
        public void InterruptedExitLocksRetryWhileAnotherExitRemainsIndependent()
        {
            Type exitType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.NetworkEscapeExitAction");
            Component exitA = CreateEscapeExit("Exit A", exitType, "escape-exit-a");
            Component exitB = CreateEscapeExit("Exit B", exitType, "escape-exit-b");
            NetworkIdentity performer = CreateIdentity("Performer");
            int startedA = 0;
            int interruptedA = 0;
            int completedB = 0;
            bool rejectNextBegin = true;
            SubscribeCountingHandler(exitA, exitType.GetEvent("EscapeAttemptStartedServer"), () =>
            {
                startedA++;
                if (!rejectNextBegin)
                    return;
                rejectNextBegin = false;
                exitType.GetMethod("RejectBeginServer")
                    .Invoke(exitA, new object[] { performer });
            });
            SubscribeCountingHandler(exitA, exitType.GetEvent("EscapeAttemptInterruptedServer"), () => interruptedA++);
            SubscribeCountingHandler(exitB, exitType.GetEvent("EscapeAttemptCompletedServer"), () => completedB++);

            MethodInfo begin = exitType.GetMethod("TryBeginInteractionServer");
            MethodInfo complete = exitType.GetMethod("TryCompleteInteractionServer");
            MethodInfo cancel = exitType.GetMethod(
                "CancelInteractionServer",
                new[] { typeof(NetworkIdentity) });

            SetServerActive(true);
            Assert.That(begin.Invoke(exitA, new object[] { performer }), Is.True);
            Assert.That(exitType.GetProperty("HasActivePerformerServer").GetValue(exitA), Is.False,
                "A synchronous domain rejection must release the B2 reservation before movement locks.");
            Assert.That(exitType.GetProperty("RetryLocked").GetValue(exitA), Is.False);
            Assert.That(interruptedA, Is.Zero,
                "A domain-rejected begin is not an accepted escape interruption.");

            Assert.That(begin.Invoke(exitA, new object[] { performer }), Is.True);
            Assert.That(exitType.GetMethod("ConfirmBeginServer")
                .Invoke(exitA, new object[] { performer }), Is.True);
            cancel.Invoke(exitA, new object[] { performer });
            Assert.That(exitType.GetProperty("HasActivePerformerServer").GetValue(exitA), Is.False);
            Assert.That(exitType.GetProperty("RetryLocked").GetValue(exitA), Is.True);
            Assert.That(interruptedA, Is.EqualTo(1));
            Assert.That(begin.Invoke(exitA, new object[] { performer }), Is.False,
                "The same exit cannot be retried until Area A accepts another preparation.");

            Assert.That(begin.Invoke(exitB, new object[] { performer }), Is.True,
                "The second authored exit remains an independent option.");
            Assert.That(exitType.GetMethod("ConfirmBeginServer")
                .Invoke(exitB, new object[] { performer }), Is.True);
            Assert.That(complete.Invoke(exitB, new object[] { performer }), Is.True);
            Assert.That(completedB, Is.EqualTo(1));
            Assert.That(startedA, Is.EqualTo(2));
        }

        [Test]
        public void ExistingWeaponHitHookCanInterruptConfirmedPerformerWithoutRoundTimerDependency()
        {
            Type exitType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.NetworkEscapeExitAction");
            Type startedSignalType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.PhysicalEscapeAttemptStarted");
            Component exit = CreateEscapeExit("Execution interrupt exit", exitType, "escape-exit-a");
            NetworkIdentity performer = CreateIdentity("Escaping suspect");
            int interruptions = 0;
            SubscribeCountingHandler(exit, exitType.GetEvent("EscapeAttemptInterruptedServer"),
                () => interruptions++);

            SetServerActive(true);
            Assert.That(exitType.GetMethod("TryBeginInteractionServer")
                .Invoke(exit, new object[] { performer }), Is.True);
            Assert.That(exitType.GetMethod("ConfirmBeginServer")
                .Invoke(exit, new object[] { performer }), Is.True);
            Assert.That(exitType.GetMethod("TryInterruptPerformerServer")
                .Invoke(exit, new object[] { performer }), Is.True);

            Assert.That(interruptions, Is.EqualTo(1));
            Assert.That(exitType.GetProperty("RetryLocked").GetValue(exit), Is.True);
            Assert.That(startedSignalType.GetProperty("LocationId"), Is.Not.Null);
            Assert.That(startedSignalType.GetProperty("PerformerName"), Is.Null,
                "The automatic report carries location, never a player name.");
            Assert.That(exitType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Any(field => field.Name.IndexOf("remaining", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              field.Name.IndexOf("roundTime", StringComparison.OrdinalIgnoreCase) >= 0),
                Is.False,
                "The physical exit has no hard dependency on remaining Runda time.");
        }

        private Component CreateEscapeExit(string name, Type exitType, string exitId)
        {
            Component exit = CreateTimedAction(name, exitType);
            SetField(exit, "planId", "escape-prototype");
            SetField(exit, "exitId", exitId);
            SetField(exit, "incidentIdPrefix", exitId + "-attempt");
            SetField(exit, "locationId", exitId);
            SetField(exit, "interactionDuration", 6.5f);
            return exit;
        }

        private Component CreateTimedAction(string name, Type type)
        {
            GameObject root = CreateObject(name);
            root.AddComponent<NetworkIdentity>();
            root.AddComponent<BoxCollider>();
            return root.AddComponent(type);
        }

        private NetworkIdentity CreateIdentity(string name) =>
            CreateObject(name).AddComponent<NetworkIdentity>();

        private GameObject CreateObject(string name)
        {
            var createdObject = new GameObject(name);
            _createdObjects.Add(createdObject);
            return createdObject;
        }

        private static void SubscribeCountingHandler(object source, EventInfo eventInfo, Action onCalled)
        {
            Type handlerType = eventInfo.EventHandlerType;
            ParameterInfo[] parameters = handlerType.GetMethod("Invoke").GetParameters();
            var expressions = parameters
                .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
                .ToArray();
            Delegate handler = Expression.Lambda(
                    handlerType,
                    Expression.Call(
                        Expression.Constant(onCalled),
                        typeof(Action).GetMethod(nameof(Action.Invoke))),
                    expressions)
                .Compile();
            eventInfo.AddEventHandler(source, handler);
        }

        private static Type FindAssemblyCSharpType(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .First(type => type != null);

        private static void SetField(object target, string fieldName, object value)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null)
                    continue;
                field.SetValue(target, value);
                return;
            }

            Assert.Fail($"Field {fieldName} was not found on {target.GetType().FullName}.");
        }

        private static void SetServerActive(bool active)
        {
            typeof(NetworkServer)
                .GetProperty(nameof(NetworkServer.active), BindingFlags.Public | BindingFlags.Static)
                ?.SetValue(null, active);
        }
    }
}
