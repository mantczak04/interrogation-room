using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InterrogationRoom.Domain;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace InterrogationRoom.Networking.Tests
{
    public sealed class RoundPhysicalRulesTests
    {
        private bool _originalServerActive;
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

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

            foreach (var createdObject in _createdObjects)
                UnityEngine.Object.DestroyImmediate(createdObject);
            _createdObjects.Clear();
        }

        [TestCase(false, false, false)]
        [TestCase(false, true, false)]
        [TestCase(true, false, true)]
        [TestCase(true, true, false)]
        public void EquipRequiresServerAuthorizationAndAnEmptyHand(
            bool isAuthorized,
            bool hasWeapon,
            bool expected)
        {
            Assert.That(
                RoundPhysicalRules.CanEquipWeapon(isAuthorized, hasWeapon),
                Is.EqualTo(expected));
        }

        [TestCase(false, false, false)]
        [TestCase(false, true, false)]
        [TestCase(true, false, false)]
        [TestCase(true, true, true)]
        public void FireRequiresAuthorizationAndPossession(
            bool isAuthorized,
            bool hasWeapon,
            bool expected)
        {
            Assert.That(
                RoundPhysicalRules.CanFireWeapon(isAuthorized, hasWeapon),
                Is.EqualTo(expected));
        }

        [TestCase(RoundPhase.Preparation, RoundRole.Detective, true, true, RoundRole.Innocent, false, false)]
        [TestCase(RoundPhase.Round, RoundRole.Guilty, true, true, RoundRole.Innocent, false, false)]
        [TestCase(RoundPhase.Round, RoundRole.Detective, false, true, RoundRole.Innocent, false, false)]
        [TestCase(RoundPhase.Round, RoundRole.Detective, true, false, RoundRole.Innocent, false, false)]
        [TestCase(RoundPhase.Round, RoundRole.Detective, true, true, RoundRole.Detective, false, false)]
        [TestCase(RoundPhase.Round, RoundRole.Detective, true, true, RoundRole.Innocent, true, false)]
        [TestCase(RoundPhase.Round, RoundRole.Detective, true, true, RoundRole.Innocent, false, true)]
        public void ExecutionHitRequiresAnArmedDetectiveAndLivingSuspectDuringRound(
            RoundPhase phase,
            RoundRole shooterRole,
            bool shooterAuthorized,
            bool shooterHasWeapon,
            RoundRole targetRole,
            bool targetEliminated,
            bool expected)
        {
            Assert.That(
                RoundPhysicalRules.CanSubmitExecutionHit(
                    phase,
                    shooterRole,
                    shooterAuthorized,
                    shooterHasWeapon,
                    targetRole,
                    targetEliminated),
                Is.EqualTo(expected));
        }

        [Test]
        public void PublicLobbyPlayerCountUsesRemoteSnapshotOnlyOffServer()
        {
            LogAssert.Expect(
                LogType.Error,
                "[NetworkRoundCoordinator] At least one CaseAsset is required.");
            var coordinator = CreateTestObject("Lobby count coordinator")
                .AddComponent<NetworkRoundCoordinator>();
            SetField(coordinator, "_publicLobbyPlayerCount", 4);

            SetServerActive(false);
            Assert.That(coordinator.PublicLobbyPlayerCount, Is.EqualTo(4));

            var connections = (IDictionary)typeof(NetworkRoundCoordinator)
                .GetField("_connectionsByPlayerId", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(coordinator);
            connections?.Add(7, new NetworkConnectionToClient(7));

            SetServerActive(true);
            Assert.That(coordinator.PublicLobbyPlayerCount, Is.EqualTo(1),
                "A host must ignore the cached client snapshot and report its authoritative roster.");
        }

        [TestCase(RoundPhase.Lobby, false)]
        [TestCase(RoundPhase.Preparation, false)]
        [TestCase(RoundPhase.Round, true)]
        [TestCase(RoundPhase.Finished, false)]
        public void PhysicalWorldActionsAreAcceptedOnlyDuringTheActiveRound(
            RoundPhase phase,
            bool expected)
        {
            LogAssert.Expect(
                LogType.Error,
                "[NetworkRoundCoordinator] At least one CaseAsset is required.");
            var coordinator = CreateTestObject("Physical action phase coordinator")
                .AddComponent<NetworkRoundCoordinator>();
            SetField(coordinator, "_phase", phase);

            Assert.That(coordinator.AllowsPhysicalRoundActions, Is.EqualTo(expected));
        }

        [Test]
        public void ObjectiveWorldStateSynchronizesEffectWithoutPublishingLastActor()
        {
            Type actionType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.NetworkObjectiveWorldAction");

            Assert.That(actionType.GetProperty("WorldRevision"), Is.Not.Null);
            Assert.That(actionType.GetField(
                "worldRevision",
                BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(actionType.GetProperty("LastActorNetId"), Is.Null);
            Assert.That(actionType.GetField(
                "lastActorNetId",
                BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        }

        [Test]
        public void InterruptedEscapeReleasesOnlyItsOwnPreparationForTheSameActor()
        {
            Type actionType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.NetworkObjectiveWorldAction");
            Type binderType = FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Interaction.RoundPhysicalActionBinder");
            EscapeExitDefinition interruptedExit = EscapePlanDefinitions.Prototype.Exits[0];
            EscapeExitDefinition otherExit = EscapePlanDefinitions.Prototype.Exits[1];

            Component interruptedPreparation = CreateObjectiveAction(
                actionType,
                "Interrupted exit preparation",
                interruptedExit.PreparationStepId.Value);
            Component otherPreparation = CreateObjectiveAction(
                actionType,
                "Other exit preparation",
                otherExit.PreparationStepId.Value);
            NetworkIdentity guilty = CreateTestObject("Guilty").AddComponent<NetworkIdentity>();
            NetworkIdentity otherActor = CreateTestObject("Other suspect").AddComponent<NetworkIdentity>();

            CompleteObjectiveAction(actionType, interruptedPreparation, guilty);
            CompleteObjectiveAction(actionType, interruptedPreparation, otherActor);
            CompleteObjectiveAction(actionType, otherPreparation, guilty);

            GameObject binderObject = CreateTestObject("Physical action binder");
            binderObject.SetActive(false);
            Component binder = binderObject.AddComponent(binderType);
            var objectiveActions = (IList)binderType
                .GetField("objectiveActions", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(binder);
            objectiveActions?.Add(interruptedPreparation);
            objectiveActions?.Add(otherPreparation);

            bool released = (bool)binderType.GetMethod(
                    "ReleaseInterruptedExitPreparationServer",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(binder, new object[] { interruptedExit.Id.Value, guilty });

            MethodInfo canInteract = actionType.GetMethod("CanInteract");
            Assert.That(released, Is.True);
            Assert.That(canInteract?.Invoke(interruptedPreparation, new object[] { guilty }), Is.True,
                "The interrupted Winny must be able to perform the required preparation again.");
            Assert.That(canInteract?.Invoke(interruptedPreparation, new object[] { otherActor }), Is.False,
                "Retry authorization must not release another actor's completion.");
            Assert.That(canInteract?.Invoke(otherPreparation, new object[] { guilty }), Is.False,
                "Retry authorization must not release a different exit or common action.");
        }

        [Test]
        public void WeaponPortRejectsUnauthorizedEquipAndRevocationRemovesPossession()
        {
            var player = CreateTestObject("Weapon port test player");
            player.AddComponent<NetworkIdentity>();
            var weapon = (IRoundWeaponPort)player.AddComponent(FindAssemblyCSharpType(
                "InterrogationRoom.Gameplay.Weapons.PlayerWeaponController"));

            Assert.That(weapon.TryEquipWeaponServer(), Is.False);
            Assert.That(weapon.SetWeaponAuthorizationServer(true), Is.True);
            Assert.That(weapon.TryEquipWeaponServer(), Is.True);
            Assert.That(weapon.HasWeapon, Is.True);

            Assert.That(weapon.SetWeaponAuthorizationServer(false), Is.True);
            Assert.That(weapon.IsWeaponAuthorized, Is.False);
            Assert.That(weapon.HasWeapon, Is.False);
            Assert.That(weapon.TryEquipWeaponServer(), Is.False);
        }

        [Test]
        public void HitSourceReportsShooterAndTargetExactlyOncePerServerHit()
        {
            var shooter = CreateTestObject("Hit seam shooter");
            var shooterIdentity = shooter.AddComponent<NetworkIdentity>();

            var target = CreateTestObject("Hit seam target");
            var targetIdentity = target.AddComponent<NetworkIdentity>();
            var targetCollider = target.AddComponent<BoxCollider>();
            Type hitboxType = FindAssemblyCSharpType("InterrogationRoom.Gameplay.Weapons.ShotHitbox");
            var hitSource = (IRoundHitSource)target.AddComponent(hitboxType);

            int eventCount = 0;
            RoundPlayerHit received = default;
            hitSource.PlayerHitReceivedServer += hit =>
            {
                eventCount++;
                received = hit;
            };

            Type contextType = FindAssemblyCSharpType("InterrogationRoom.Gameplay.Weapons.ShotHitContext");
            object context = Activator.CreateInstance(
                contextType,
                shooterIdentity,
                targetCollider,
                Vector3.one,
                Vector3.up,
                Vector3.forward);
            hitboxType.GetMethod("ReceiveShotServer")?.Invoke(hitSource, new[] { context });

            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(received.Shooter, Is.SameAs(shooterIdentity));
            Assert.That(received.Target, Is.SameAs(targetIdentity));
        }

        private static Type FindAssemblyCSharpType(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .First(type => type != null);

        private GameObject CreateTestObject(string name)
        {
            var createdObject = new GameObject(name);
            _createdObjects.Add(createdObject);
            return createdObject;
        }

        private Component CreateObjectiveAction(Type actionType, string name, string payloadId)
        {
            GameObject actionObject = CreateTestObject(name);
            actionObject.AddComponent<NetworkIdentity>();
            Component action = actionObject.AddComponent(actionType);
            SetField(action, "completionPayloadId", payloadId);
            SetField(action, "oneShot", false);
            return action;
        }

        private static void CompleteObjectiveAction(
            Type actionType,
            Component action,
            NetworkIdentity actor)
        {
            Assert.That(actionType.GetMethod("TryBeginInteractionServer")
                ?.Invoke(action, new object[] { actor }), Is.True);
            Assert.That(actionType.GetMethod("TryCompleteInteractionServer")
                ?.Invoke(action, new object[] { actor }), Is.True);
        }

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
