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
    public sealed class NetworkEasterEggRuntimeTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private bool originalServerActive;
        private object capturedSignal;

        [SetUp]
        public void SetUp()
        {
            originalServerActive = NetworkServer.active;
            capturedSignal = null;
        }

        [TearDown]
        public void TearDown()
        {
            SetServerActive(originalServerActive);
            foreach (GameObject createdObject in createdObjects)
                UnityEngine.Object.DestroyImmediate(createdObject);
            createdObjects.Clear();
        }

        [Test]
        public void SpotRejectsClientAuthorityAndPublishesOnlyMotiveNeutralWorldEffect()
        {
            Type spotType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.EasterEggs.NetworkEasterEggSpot");
            Component spot = CreateNetworkComponent("Rare prop", spotType);
            SetField(spot, "easterEggId", "break-room-mug-choir");
            SetField(spot, "propId", "off-key-mug-stack");
            SetField(spot, "locationId", "break-room-coffee-counter");
            SetField(spot, "effectId", "mugs-hum-police-theme");
            NetworkIdentity actor = CreateObject("Actor").AddComponent<NetworkIdentity>();

            SetServerActive(false);
            Assert.That(spotType.GetMethod("SetAvailableForRundaServer")
                .Invoke(spot, new object[] { true }), Is.False);
            Assert.That(spotType.GetProperty("IsAvailable").GetValue(spot), Is.False);

            SetServerActive(true);
            Assert.That(spotType.GetMethod("SetAvailableForRundaServer")
                .Invoke(spot, new object[] { true }), Is.True);
            SubscribeSignal(spot, spotType.GetEvent("EffectTriggeredServer"));

            Assert.That(spotType.GetMethod("TryBeginInteractionServer")
                .Invoke(spot, new object[] { actor }), Is.True);
            Assert.That(spotType.GetMethod("TryCompleteInteractionServer")
                .Invoke(spot, new object[] { actor }), Is.True);
            Assert.That(spotType.GetProperty("EffectRevision").GetValue(spot), Is.EqualTo(1));
            Assert.That(spotType.GetMethod("TryBeginInteractionServer")
                .Invoke(spot, new object[] { actor }), Is.False,
                "The rare effect is one-shot and cannot block any repeatable objective interaction.");

            Assert.That(capturedSignal, Is.Not.Null);
            Type signalType = capturedSignal.GetType();
            Assert.That(signalType.GetProperty("EasterEggId").GetValue(capturedSignal),
                Is.EqualTo("break-room-mug-choir"));
            Assert.That(signalType.GetProperty("EffectId").GetValue(capturedSignal),
                Is.EqualTo("mugs-hum-police-theme"));
            Assert.That(signalType.GetProperties().Select(property => property.Name),
                Has.None.Matches<string>(name =>
                    name.Contains("Role") ||
                    name.Contains("Objective") ||
                    name.Contains("Damage") ||
                    name.Contains("Outcome") ||
                    name.Contains("Winner")));
        }

        [Test]
        public void DirectorValidatesAllAuthoredSpotsAndSelectsExactlyOneOnTheHost()
        {
            Type spotType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.EasterEggs.NetworkEasterEggSpot");
            Type directorType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.EasterEggs.NetworkEasterEggDirector");
            string[,] authoredIds =
            {
                { "break-room-mug-choir", "off-key-mug-stack", "break-room-coffee-counter", "mugs-hum-police-theme" },
                { "records-typewriter-confession", "dusty-typewriter", "records-room-side-desk", "typewriter-prints-innocent-fish-confession" },
                { "evidence-pigeon-inspector", "cardboard-pigeon", "evidence-locker-top-shelf", "pigeon-turns-and-stamps-form" },
                { "front-desk-intercom-forecast", "retired-desk-intercom", "front-desk-reception", "intercom-announces-indoor-fog" },
            };
            var spots = new Component[authoredIds.GetLength(0)];
            for (int index = 0; index < spots.Length; index++)
            {
                spots[index] = CreateNetworkComponent($"Easter egg {index}", spotType);
                SetField(spots[index], "easterEggId", authoredIds[index, 0]);
                SetField(spots[index], "propId", authoredIds[index, 1]);
                SetField(spots[index], "locationId", authoredIds[index, 2]);
                SetField(spots[index], "effectId", authoredIds[index, 3]);
            }

            Component director = CreateNetworkComponent("Easter egg director", directorType);
            Array typedSpots = Array.CreateInstance(spotType, spots.Length);
            for (int index = 0; index < spots.Length; index++)
                typedSpots.SetValue(spots[index], index);
            SetField(director, "authoredSpots", typedSpots);
            SetField(director, "spawnChancePercent", 25);

            object[] validationArguments = { null };
            Assert.That(directorType.GetMethod("TryValidateWiring")
                .Invoke(director, validationArguments), Is.True, validationArguments[0] as string);

            SetServerActive(true);
            object firstSelection = directorType.GetMethod("BeginRundaServer")
                .Invoke(director, new object[] { 6 });
            string firstId = (string)directorType.GetProperty("ActiveEasterEggId").GetValue(director);
            Assert.That(firstSelection.GetType().GetProperty("HasSpawn").GetValue(firstSelection), Is.True);
            Assert.That(firstId, Is.Not.Empty);
            Assert.That(spots.Count(spot =>
                (bool)spotType.GetProperty("IsAvailable").GetValue(spot)), Is.EqualTo(1));

            directorType.GetMethod("BeginRundaServer").Invoke(director, new object[] { 6 });
            Assert.That(directorType.GetProperty("ActiveEasterEggId").GetValue(director), Is.EqualTo(firstId));

            directorType.GetMethod("EndRundaServer").Invoke(director, null);
            Assert.That(spots, Has.None.Matches<Component>(spot =>
                (bool)spotType.GetProperty("IsAvailable").GetValue(spot)));
        }

        private Component CreateNetworkComponent(string name, Type componentType)
        {
            GameObject target = CreateObject(name);
            target.AddComponent<NetworkIdentity>();
            target.AddComponent<BoxCollider>();
            return target.AddComponent(componentType);
        }

        private GameObject CreateObject(string name)
        {
            var createdObject = new GameObject(name);
            createdObjects.Add(createdObject);
            return createdObject;
        }

        private void SubscribeSignal(object source, EventInfo eventInfo)
        {
            Type handlerType = eventInfo.EventHandlerType;
            ParameterInfo parameter = handlerType.GetMethod("Invoke").GetParameters().Single();
            ParameterExpression signal = Expression.Parameter(parameter.ParameterType, parameter.Name);
            MethodInfo capture = GetType().GetMethod(
                nameof(CaptureSignal),
                BindingFlags.Instance | BindingFlags.NonPublic);
            Delegate handler = Expression.Lambda(
                    handlerType,
                    Expression.Call(Expression.Constant(this), capture, Expression.Convert(signal, typeof(object))),
                    signal)
                .Compile();
            eventInfo.AddEventHandler(source, handler);
        }

        private void CaptureSignal(object signal)
        {
            capturedSignal = signal;
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
