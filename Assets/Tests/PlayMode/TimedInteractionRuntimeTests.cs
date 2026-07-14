using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace InterrogationRoom.Gameplay.Tests
{
    public sealed class TimedInteractionRuntimeTests
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
            foreach (var createdObject in _createdObjects)
                UnityEngine.Object.DestroyImmediate(createdObject);
            _createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator CancelCompetitionAndOneShotCompletionRemainServerAuthoritative()
        {
            SetServerActive(true);
            Type targetType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.NetworkTimedInteractionHarnessTarget");
            var firstActor = CreateIdentity("First actor");
            var secondActor = CreateIdentity("Second actor");
            var targetObject = CreateObject("Timed target");
            targetObject.AddComponent<NetworkIdentity>();
            targetObject.AddComponent<BoxCollider>();
            Component target = targetObject.AddComponent(targetType);

            MethodInfo begin = targetType.GetMethod("TryBeginInteractionServer");
            MethodInfo cancel = targetType.GetMethod(
                "CancelInteractionServer",
                new[] { typeof(NetworkIdentity) });
            MethodInfo complete = targetType.GetMethod("TryCompleteInteractionServer");

            Assert.That(begin.Invoke(target, new object[] { firstActor }), Is.True);
            Assert.That(begin.Invoke(target, new object[] { secondActor }), Is.False,
                "Only one actor may reserve a timed action.");

            cancel.Invoke(target, new object[] { firstActor });
            Assert.That(targetType.GetProperty("CompletionCount")?.GetValue(target), Is.EqualTo(0),
                "Cancellation must not apply a partial world effect.");

            Assert.That(begin.Invoke(target, new object[] { secondActor }), Is.True,
                "A cancelled attempt releases the reservation and restarts from zero.");
            Assert.That(complete.Invoke(target, new object[] { secondActor }), Is.True);
            Assert.That(targetType.GetProperty("CompletionCount")?.GetValue(target), Is.EqualTo(1));
            Assert.That(begin.Invoke(target, new object[] { firstActor }), Is.False,
                "A consumed one-shot action cannot be completed again.");

            Assert.That(targetType.GetEvent("CompletedServer"), Is.Not.Null,
                "Physical completion must expose the neutral server event for Area A.");
            Assert.That(targetType.GetEvent("CancelledServer"), Is.Not.Null,
                "Timed cancellation must expose a server result for interruptible actions.");
            SetServerActive(_originalServerActive);
            yield return null;
        }

        [Test]
        public void MissingAnimatorParameterIsAnOptionalHook()
        {
            Type interactorType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.PlayerInteractor");
            var actorObject = CreateObject("Actor without interaction animation");
            actorObject.AddComponent<NetworkIdentity>();
            actorObject.AddComponent<Animator>();
            Component interactor = actorObject.AddComponent(interactorType);

            MethodInfo setAnimation = interactorType.GetMethod(
                "SetInteractionAnimation",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(setAnimation, Is.Not.Null);
            Assert.DoesNotThrow(() => setAnimation.Invoke(interactor, new object[] { true }));
            Assert.DoesNotThrow(() => setAnimation.Invoke(interactor, new object[] { false }));
            LogAssert.NoUnexpectedReceived();
        }

        private NetworkIdentity CreateIdentity(string name)
        {
            var createdObject = CreateObject(name);
            return createdObject.AddComponent<NetworkIdentity>();
        }

        private GameObject CreateObject(string name)
        {
            var createdObject = new GameObject(name);
            _createdObjects.Add(createdObject);
            return createdObject;
        }

        private static Type FindAssemblyCSharpType(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .First(type => type != null);

        private static void SetServerActive(bool active)
        {
            typeof(NetworkServer)
                .GetProperty(nameof(NetworkServer.active), BindingFlags.Public | BindingFlags.Static)
                ?.SetValue(null, active);
        }
    }
}
