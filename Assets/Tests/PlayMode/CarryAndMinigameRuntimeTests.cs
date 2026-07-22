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
    public sealed class CarryAndMinigameRuntimeTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();
        private bool _originalServerActive;

        [SetUp]
        public void SetUp()
        {
            _originalServerActive = NetworkServer.active;
            SetServerActive(true);
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
        public void PlayerCarriesOneItemDropsItAndPlacesItInAnAcceptedSlot()
        {
            Type itemType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Items.NetworkCarryableItem");
            Type slotType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Items.NetworkItemSlot");
            NetworkIdentity actor = CreateIdentity("Actor");
            NetworkIdentity otherActor = CreateIdentity("Other actor");
            Component evidence = CreateCarryable("Evidence", itemType, "akta-kr-17");
            Component secondItem = CreateCarryable("Second item", itemType, "telefon");
            Component slot = CreateNetworkComponent("Evidence locker slot", slotType);
            SetField(slot, "acceptedItemIds", new[] { "akta-kr-17" });
            SetField(slot, "objectiveStepId", "ukryj-akta");
            int completionSignals = 0;
            SubscribeCountingHandler(slot, slotType.GetEvent("CompletedServer"), () => completionSignals++);

            Assert.That(itemType.GetMethod("TryPickupServer")
                .Invoke(evidence, new object[] { actor }), Is.True);
            Assert.That(itemType.GetMethod("TryPickupServer")
                .Invoke(secondItem, new object[] { actor }), Is.False,
                "The same player cannot carry two significant items.");
            Assert.That(itemType.GetMethod("DropServer").Invoke(evidence, null), Is.True);
            Assert.That(itemType.GetProperty("State").GetValue(evidence).ToString(), Is.EqualTo("Dropped"));

            Assert.That(itemType.GetMethod("TryPickupServer")
                .Invoke(evidence, new object[] { actor }), Is.True);
            Assert.That(slotType.GetMethod("TryInteractServer")
                .Invoke(slot, new object[] { actor }), Is.True);
            Assert.That(itemType.GetProperty("State").GetValue(evidence).ToString(), Is.EqualTo("Placed"));
            Assert.That(completionSignals, Is.EqualTo(1));

            Assert.That(itemType.GetMethod("TryPickupServer")
                .Invoke(evidence, new object[] { otherActor }), Is.True,
                "A visible placed item remains movable by another player.");
            Assert.That(slotType.GetProperty("IsOccupied").GetValue(slot), Is.False);
        }

        [Test]
        public void IdleDroppedItemReturnsToItsHomeAnchorAfterTimeout()
        {
            Type itemType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Items.NetworkCarryableItem");
            NetworkIdentity actor = CreateIdentity("Actor");
            Component item = CreateCarryable("Mandatory item", itemType, "klucz-magazynu");
            Vector3 homePosition = item.transform.position;

            Assert.That(itemType.GetMethod("TryPickupServer")
                .Invoke(item, new object[] { actor }), Is.True);
            Assert.That(itemType.GetMethod("DropServer").Invoke(item, null), Is.True);
            item.transform.position = new Vector3(8f, 0f, 4f);
            SetField(item, "droppedReturnTimeout", 5f);
            SetField(item, "lastDroppedAt", 10d);

            Assert.That(itemType.GetMethod("EvaluateRecoveryServer")
                .Invoke(item, new object[] { 14.9d }), Is.False);
            Assert.That(itemType.GetMethod("EvaluateRecoveryServer")
                .Invoke(item, new object[] { 15d }), Is.True);
            Assert.That(itemType.GetProperty("State").GetValue(item).ToString(), Is.EqualTo("AtHome"));
            Assert.That(item.transform.position, Is.EqualTo(homePosition));
        }

        [Test]
        public void MinigameCompletionIsRejectedBeforePlausibleDurationAndAcceptedAfterward()
        {
            Type targetType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.NetworkTimedInteractionHarnessTarget");
            Type interactorType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.PlayerInteractor");
            Type specType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Minigames.MinigameSpec");
            NetworkIdentity actor = CreateIdentity("Actor");
            Component interactor = actor.gameObject.AddComponent(interactorType);
            Component target = CreateNetworkComponent("Minigame target", targetType);
            Component spec = target.gameObject.AddComponent(specType);
            NetworkIdentity targetIdentity = target.GetComponent<NetworkIdentity>();

            Assert.That(targetType.GetMethod("TryBeginInteractionServer")
                .Invoke(target, new object[] { actor }), Is.True);
            SetField(interactor, "activeTimedTarget", target);
            SetField(interactor, "activeTimedTargetIdentity", targetIdentity);
            SetField(interactor, "activeMinigameSpec", spec);
            SetField(interactor, "activeTimedEndsAt", NetworkTime.time + 5d);
            MethodInfo completeServer = interactorType.GetMethod(
                "TryCompleteMinigameServer",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(completeServer.Invoke(interactor, null), Is.False);
            Assert.That(targetType.GetProperty("CompletionCount").GetValue(target), Is.Zero,
                "Client-reported success cannot bypass the server time cost.");
            Assert.That(targetType.GetProperty("HasActiveInteractor").GetValue(target), Is.True);

            SetField(interactor, "activeTimedEndsAt", NetworkTime.time - 0.01d);
            Assert.That(completeServer.Invoke(interactor, null), Is.True);
            Assert.That(targetType.GetProperty("CompletionCount").GetValue(target), Is.EqualTo(1));
            Assert.That(targetType.GetProperty("HasActiveInteractor").GetValue(target), Is.False);
        }

        [TestCase("PersonalMatterFinish", "personal-document")]
        [TestCase("SecretObjectivePlant", "suspicious-token")]
        public void DeveloperTaskPreparationGivesTheRequiredItemToTheControlledPlayer(
            string taskName,
            string itemId)
        {
            Type binderType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.RoundPhysicalActionBinder");
            Type itemType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Items.NetworkCarryableItem");
            Type taskType = FindAssemblyCSharpType(
                "InterrogationRoom.Networking.RoundDeveloperTask");
            GameObject integrationRoot = CreateObject("Physical integration");
            integrationRoot.SetActive(false);
            Component item = CreateCarryable("Required item", itemType, itemId);
            item.transform.SetParent(integrationRoot.transform);
            Component binder = integrationRoot.AddComponent(binderType);
            binderType.GetMethod("RefreshBindings").Invoke(binder, null);
            NetworkIdentity actor = CreateIdentity("Controlled player");
            object task = Enum.Parse(taskType, taskName);

            Assert.That(binderType.GetMethod("TryPrepareDeveloperTaskServer")
                .Invoke(binder, new[] { actor, task }), Is.True);
            Assert.That(itemType.GetProperty("IsCarried").GetValue(item), Is.True);
            Assert.That(itemType.GetMethod("IsCarriedBy")
                .Invoke(item, new object[] { actor }), Is.True);
        }

        private Component CreateCarryable(string name, Type itemType, string itemId)
        {
            GameObject itemObject = CreateObject(name);
            itemObject.AddComponent<NetworkIdentity>();
            itemObject.AddComponent<BoxCollider>();
            itemObject.AddComponent<Rigidbody>();
            Component item = itemObject.AddComponent(itemType);
            SetField(item, "itemId", itemId);
            return item;
        }

        private Component CreateNetworkComponent(string name, Type componentType)
        {
            GameObject target = CreateObject(name);
            target.AddComponent<NetworkIdentity>();
            target.AddComponent<BoxCollider>();
            return target.AddComponent(componentType);
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
