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
    public sealed class PhysicalObjectiveTracerTests
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
        public void CancellationHasNoEffectAndAnotherActorCannotConsumeRequiredAction()
        {
            Type actionType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.NetworkObjectiveWorldAction");
            Component action = CreateAction("Reusable objective action", actionType);
            NetworkIdentity owner = CreateIdentity("Owner");
            NetworkIdentity other = CreateIdentity("Other suspect");
            MethodInfo begin = actionType.GetMethod("TryBeginInteractionServer");
            MethodInfo cancel = actionType.GetMethod(
                "CancelInteractionServer",
                new[] { typeof(NetworkIdentity) });
            MethodInfo complete = actionType.GetMethod("TryCompleteInteractionServer");
            PropertyInfo revision = actionType.GetProperty("WorldRevision");
            int completionSignals = 0;
            SubscribeCountingHandler(action, actionType.GetEvent("CompletedServer"), () => completionSignals++);

            SetServerActive(true);
            Assert.That(begin.Invoke(action, new object[] { owner }), Is.True);
            cancel.Invoke(action, new object[] { owner });
            Assert.That(revision.GetValue(action), Is.EqualTo(0));
            Assert.That(completionSignals, Is.Zero);

            Assert.That(begin.Invoke(action, new object[] { other }), Is.True);
            Assert.That(complete.Invoke(action, new object[] { other }), Is.True);
            Assert.That(revision.GetValue(action), Is.EqualTo(1),
                "A bluff still applies the visible world effect.");
            Assert.That(begin.Invoke(action, new object[] { other }), Is.False,
                "One actor cannot spam the same stateful action.");

            Assert.That(actionType.GetMethod("ReleaseActorCompletionServer")
                .Invoke(action, new object[] { other }), Is.True);
            Assert.That(begin.Invoke(action, new object[] { other }), Is.True,
                "Area A can release an out-of-order completion without reverting the world.");
            cancel.Invoke(action, new object[] { other });

            Assert.That(begin.Invoke(action, new object[] { owner }), Is.True,
                "A foreign completion cannot permanently block the assigned owner.");
            Assert.That(complete.Invoke(action, new object[] { owner }), Is.True);
            Assert.That(revision.GetValue(action), Is.EqualTo(2));
            Assert.That(completionSignals, Is.EqualTo(2));

            actionType.GetMethod("ResetInteractionStateServer").Invoke(action, null);
            Assert.That(revision.GetValue(action), Is.Zero,
                "A new Runda must restore the authored world presentation.");
            Assert.That(begin.Invoke(action, new object[] { other }), Is.True,
                "Per-actor completion reservations must not leak into the next Runda.");
        }

        [Test]
        public void LoudIncidentEmitsStablePhysicalIdsImmediatelyWithoutGrantingProgress()
        {
            Type actionType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.NetworkIncidentWorldAction");
            Type kindType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.PhysicalIncidentKind");
            Component action = CreateAction("Loud incident", actionType);
            SetField(action, "incidentKind", Enum.Parse(kindType, "Loud"));
            SetField(action, "incidentIdPrefix", "alarm-archive");
            SetField(action, "effectId", "uruchomiony-alarm");
            SetField(action, "locationId", "archiwum");
            NetworkIdentity actor = CreateIdentity("Suspect");
            int incidentSignals = 0;
            SubscribeCountingHandler(action, actionType.GetEvent("IncidentRaisedServer"), () => incidentSignals++);

            SetServerActive(true);
            Assert.That(actionType.GetMethod("TryBeginInteractionServer")
                .Invoke(action, new object[] { actor }), Is.True);
            Assert.That(actionType.GetMethod("TryCompleteInteractionServer")
                .Invoke(action, new object[] { actor }), Is.True);

            Assert.That(incidentSignals, Is.EqualTo(1));
            Assert.That(actionType.GetProperty("LastIncidentId").GetValue(action),
                Is.EqualTo("alarm-archive-001"));
            Assert.That(actionType.GetProperty("LastEffectId").GetValue(action),
                Is.EqualTo("uruchomiony-alarm"));
            Assert.That(actionType.GetProperty("LastLocationId").GetValue(action),
                Is.EqualTo("archiwum"));
            Assert.That(actionType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Any(field => field.FieldType.Namespace == "InterrogationRoom.Domain"), Is.False,
                "The physical action must not resolve Round rules locally.");
        }

        [Test]
        public void QuietIncidentRequiresRangeAndDirectLineOfSightBeforeDiscoverySignal()
        {
            Type actionType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.NetworkIncidentWorldAction");
            Type probeType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.QuietIncidentDiscoveryProbe");
            Type kindType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.PhysicalIncidentKind");
            Component action = CreateAction("Quiet incident", actionType);
            SetField(action, "incidentKind", Enum.Parse(kindType, "Quiet"));
            SetField(action, "incidentIdPrefix", "planted-item");
            SetField(action, "effectId", "podlozony-przedmiot");
            SetField(action, "locationId", "szafka-celu");
            NetworkIdentity author = CreateIdentity("Author");
            NetworkIdentity viewer = CreateIdentity("Discovery candidate");
            viewer.transform.position = new Vector3(0f, 0f, -2f);

            Component probe = action.gameObject.AddComponent(probeType);
            SetField(probe, "incidentSource", action);
            SetField(probe, "discoveryPoint", action.transform);
            SetField(probe, "discoveryRange", 3f);
            int discoverySignals = 0;
            SubscribeCountingHandler(probe, probeType.GetEvent("DiscoveryCandidateServer"),
                () => discoverySignals++);

            GameObject blocker = CreateObject("Wall blocking discovery");
            blocker.transform.position = new Vector3(0f, 0.8f, -1f);
            blocker.AddComponent<BoxCollider>().size = new Vector3(2f, 2f, 0.2f);
            Physics.SyncTransforms();

            SetServerActive(true);
            Assert.That(actionType.GetMethod("TryBeginInteractionServer")
                .Invoke(action, new object[] { author }), Is.True);
            Assert.That(actionType.GetMethod("TryCompleteInteractionServer")
                .Invoke(action, new object[] { author }), Is.True);
            Assert.That(probeType.GetMethod("TryDiscoverServer")
                .Invoke(probe, new object[] { viewer }), Is.False);
            Assert.That(discoverySignals, Is.Zero);

            blocker.SetActive(false);
            Physics.SyncTransforms();
            Assert.That(probeType.GetMethod("TryDiscoverServer")
                .Invoke(probe, new object[] { viewer }), Is.True);
            Assert.That(discoverySignals, Is.EqualTo(1));
            Assert.That(probeType.GetMethod("TryDiscoverServer")
                .Invoke(probe, new object[] { viewer }), Is.False,
                "One player must not spam discovery for the same quiet Incydent.");
        }

        private Component CreateAction(string name, Type actionType)
        {
            GameObject root = CreateObject(name);
            root.AddComponent<NetworkIdentity>();
            var collider = root.AddComponent<BoxCollider>();
            collider.center = Vector3.up;
            collider.size = new Vector3(1f, 2f, 1f);
            Component action = root.AddComponent(actionType);
            SetField(action, "oneShot", false);
            return action;
        }

        private NetworkIdentity CreateIdentity(string name)
        {
            return CreateObject(name).AddComponent<NetworkIdentity>();
        }

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
            MethodInfo invoke = typeof(Action).GetMethod(nameof(Action.Invoke));
            Delegate handler = Expression.Lambda(
                    handlerType,
                    Expression.Call(Expression.Constant(onCalled), invoke),
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
