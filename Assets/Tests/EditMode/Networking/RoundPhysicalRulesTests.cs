using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InterrogationRoom.Domain;
using Mirror;
using NUnit.Framework;
using UnityEngine;

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

        private static void SetServerActive(bool active)
        {
            typeof(NetworkServer)
                .GetProperty(nameof(NetworkServer.active), BindingFlags.Public | BindingFlags.Static)
                ?.SetValue(null, active);
        }
    }
}
