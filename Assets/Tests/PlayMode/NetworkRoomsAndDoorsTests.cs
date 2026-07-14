using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Tests
{
    public sealed class NetworkRoomsAndDoorsTests
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
        public void DoorStateIsServerAuthoritativeAndInitialSnapshotSupportsLateJoin()
        {
            Type doorType = FindAssemblyCSharpType("InterrogationRoom.Gameplay.Interaction.NetworkDoor");
            Component serverDoor = CreateDoor("Server door", doorType);
            Component lateJoinDoor = CreateDoor("Late join door", doorType);
            PropertyInfo isOpen = doorType.GetProperty("IsOpen");
            MethodInfo setOpen = doorType.GetMethod("SetOpenServer");

            SetServerActive(true);
            Assert.That(setOpen.Invoke(serverDoor, new object[] { true }), Is.True);
            Assert.That(isOpen.GetValue(serverDoor), Is.True);
            Assert.That(GetBlockingCollider(serverDoor).enabled, Is.False,
                "An open door must not block movement or server shot raycasts.");

            var writer = new NetworkWriter();
            ((NetworkBehaviour)serverDoor).OnSerialize(writer, true);
            SetServerActive(false);
            ((NetworkBehaviour)lateJoinDoor).OnDeserialize(new NetworkReader(writer.ToArraySegment()), true);

            Assert.That(isOpen.GetValue(lateJoinDoor), Is.True,
                "A late join initial snapshot must include the public open state.");
            Assert.That(GetBlockingCollider(lateJoinDoor).enabled, Is.False);
        }

        [Test]
        public void EveryPlayerCanToggleDoorButCooldownRejectsImmediateSpam()
        {
            Type doorType = FindAssemblyCSharpType("InterrogationRoom.Gameplay.Interaction.NetworkDoor");
            Component door = CreateDoor("Door", doorType);
            var actor = CreateObject("Actor").AddComponent<NetworkIdentity>();
            MethodInfo interact = doorType.GetMethod("TryInteractServer");

            SetServerActive(true);
            Assert.That(interact.Invoke(door, new object[] { actor }), Is.True);
            Assert.That(interact.Invoke(door, new object[] { actor }), Is.False,
                "The server cooldown must reject door flapping without adding role locks.");
        }

        [Test]
        public void RoomMembershipChangesAtTheServerAndIsExposedForRemoteConsumers()
        {
            Type roomType = FindAssemblyCSharpType("InterrogationRoom.Gameplay.Interaction.RoomVolume");
            Type trackerType = FindAssemblyCSharpType("InterrogationRoom.Gameplay.Interaction.PlayerRoomTracker");
            CreateRoom("Room A", roomType, "interview-a", new Vector3(-2f, 1f, 0f));
            CreateRoom("Room B", roomType, "interview-b", new Vector3(2f, 1f, 0f));

            GameObject player = CreateObject("Tracked player");
            player.AddComponent<NetworkIdentity>();
            Component tracker = player.AddComponent(trackerType);
            MethodInfo refresh = trackerType.GetMethod("RefreshRoomServer");
            PropertyInfo currentRoomId = trackerType.GetProperty("CurrentRoomId");

            SetServerActive(true);
            player.transform.position = new Vector3(-2f, 1f, 0f);
            refresh.Invoke(tracker, null);
            Assert.That(currentRoomId.GetValue(tracker), Is.EqualTo("interview-a"));

            player.transform.position = new Vector3(2f, 1f, 0f);
            refresh.Invoke(tracker, null);
            Assert.That(currentRoomId.GetValue(tracker), Is.EqualTo("interview-b"));

            player.transform.position = new Vector3(0f, 5f, 0f);
            refresh.Invoke(tracker, null);
            Assert.That(currentRoomId.GetValue(tracker), Is.EqualTo(string.Empty));
        }

        [Test]
        public void VoicePathDistinguishesSameRoomOpenDoorClosedDoorAndWall()
        {
            Type doorType = FindAssemblyCSharpType("InterrogationRoom.Gameplay.Interaction.NetworkDoor");
            Type occlusionType = FindAssemblyCSharpType("VivoxVoiceOcclusion");
            Component door = CreateDoor("Acoustic portal", doorType);
            SetField(door, "roomAId", "interview");
            SetField(door, "roomBId", "corridor");
            MethodInfo resolve = occlusionType.GetMethod("ResolvePortalState");
            PropertyInfo portals = FindAssemblyCSharpType("InterrogationRoom.Gameplay.Interaction.RoomPortalRegistry")
                .GetProperty("ActivePortals");

            object sameRoom = resolve.Invoke(null, new[] { "interview", "interview", portals.GetValue(null) });
            object closedDoor = resolve.Invoke(null, new[] { "interview", "corridor", portals.GetValue(null) });
            object wall = resolve.Invoke(null, new[] { "interview", "evidence", portals.GetValue(null) });

            SetServerActive(true);
            doorType.GetMethod("SetOpenServer").Invoke(door, new object[] { true });
            object openDoor = resolve.Invoke(null, new[] { "interview", "corridor", portals.GetValue(null) });

            Assert.That(sameRoom.ToString(), Is.EqualTo("SameRoom"));
            Assert.That(openDoor.ToString(), Is.EqualTo("OpenPortalPath"));
            Assert.That(closedDoor.ToString(), Is.EqualTo("ClosedPortalPath"));
            Assert.That(wall.ToString(), Is.EqualTo("Wall"));
        }

        private Component CreateDoor(string name, Type doorType)
        {
            GameObject root = CreateObject(name);
            root.AddComponent<NetworkIdentity>();
            root.AddComponent<BoxCollider>().isTrigger = true;

            var leaf = new GameObject("DoorLeaf");
            leaf.transform.SetParent(root.transform, false);
            _createdObjects.Add(leaf);
            leaf.AddComponent<BoxCollider>();
            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(leaf.transform, false);
            _createdObjects.Add(visualRoot);

            return root.AddComponent(doorType);
        }

        private void CreateRoom(string name, Type roomType, string roomId, Vector3 position)
        {
            GameObject room = CreateObject(name);
            room.transform.position = position;
            var collider = room.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(3f, 2f, 3f);
            Component volume = room.AddComponent(roomType);
            SetField(volume, "roomId", roomId);
        }

        private static Collider GetBlockingCollider(Component door)
        {
            return door.transform.Find("DoorLeaf").GetComponent<Collider>();
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

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private static void SetServerActive(bool active)
        {
            typeof(NetworkServer)
                .GetProperty(nameof(NetworkServer.active), BindingFlags.Public | BindingFlags.Static)
                ?.SetValue(null, active);
        }
    }
}
