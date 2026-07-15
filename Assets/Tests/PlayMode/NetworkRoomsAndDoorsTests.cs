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
            Assert.That(GetBlockingCollider(serverDoor).enabled, Is.True,
                "The rotated leaf collider must remain discoverable so the door can be closed again.");

            var writer = new NetworkWriter();
            ((NetworkBehaviour)serverDoor).OnSerialize(writer, true);
            SetServerActive(false);
            ((NetworkBehaviour)lateJoinDoor).OnDeserialize(new NetworkReader(writer.ToArraySegment()), true);

            Assert.That(isOpen.GetValue(lateJoinDoor), Is.True,
                "A late join initial snapshot must include the public open state.");
            Assert.That(GetBlockingCollider(lateJoinDoor).enabled, Is.True);
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

            SetField(door, "nextToggleAt", -1d);
            Assert.That(interact.Invoke(door, new object[] { actor }), Is.True,
                "The same door must remain interactive after it has opened.");
            Assert.That(doorType.GetProperty("IsOpen")?.GetValue(door), Is.False);
        }

        [Test]
        public void DoorRotatesAroundDetectedLeafEdgeAndReturnsToClosedPose()
        {
            Type doorType = FindAssemblyCSharpType("InterrogationRoom.Gameplay.Interaction.NetworkDoor");
            Component door = CreateDoor("Hinged door", doorType);
            Transform leaf = door.transform.Find("DoorLeaf");
            var collider = leaf.GetComponent<BoxCollider>();
            collider.size = new Vector3(0.1f, 2f, 1.5f);
            SetField(door, "animationDuration", 0f);
            InvokePrivate(door, "Awake");
            Vector3 closedPosition = leaf.localPosition;

            SetServerActive(true);
            Assert.That(doorType.GetMethod("SetOpenServer")?.Invoke(door, new object[] { true }), Is.True);
            Assert.That(leaf.localPosition, Is.Not.EqualTo(closedPosition),
                "Opening must translate the centered mesh so it rotates around an edge, not its middle.");

            Assert.That(doorType.GetMethod("SetOpenServer")?.Invoke(door, new object[] { false }), Is.True);
            Assert.That(Vector3.Distance(leaf.localPosition, closedPosition), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(leaf.localRotation, Quaternion.identity), Is.LessThan(0.01f));
        }

        [Test]
        public void DoorChoosesOpeningSideClosestToTheRoomInsteadOfTheCorridor()
        {
            Type doorType = FindAssemblyCSharpType("InterrogationRoom.Gameplay.Interaction.NetworkDoor");
            Type roomType = FindAssemblyCSharpType("InterrogationRoom.Gameplay.Interaction.RoomVolume");
            CreateRoom("Room behind door", roomType, "target-room", new Vector3(0f, 0f, -4f));
            Component door = CreateDoor("Corridor door", doorType);
            door.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            SetField(door, "roomAId", "korytarz");
            SetField(door, "roomBId", "target-room");
            SetField(door, "hingeLocalOffset", Vector3.forward * 0.5f);
            SetField(door, "animationDuration", 0f);
            InvokePrivate(door, "Awake");

            SetServerActive(true);
            Assert.That(doorType.GetMethod("SetOpenServer")?.Invoke(door, new object[] { true }), Is.True);
            Assert.That(door.transform.Find("DoorLeaf").position.z, Is.LessThan(0f),
                "The leaf must swing toward the room and away from the corridor.");
        }

        [Test]
        public void FlatSceneDoorIsSeparatedIntoFixedInteractionRootAndAnimatedLeaf()
        {
            Type doorType = FindAssemblyCSharpType("InterrogationRoom.Gameplay.Interaction.NetworkDoor");
            GameObject root = CreateObject("Flat scene door");
            root.transform.localScale = new Vector3(0.05f, 2.1f, 1.5f);
            root.AddComponent<NetworkIdentity>();
            var sourceRenderer = root.AddComponent<MeshRenderer>();
            root.AddComponent<MeshFilter>();
            var sourceCollider = root.AddComponent<BoxCollider>();
            Component door = root.AddComponent(doorType);
            SetField(door, "doorLeaf", root.transform);
            SetField(door, "blockingCollider", sourceCollider);
            InvokePrivate(door, "SeparateFlatSceneDoorLeaf");
            SetField(door, "hingeLocalOffset", Vector3.forward * 0.5f);
            InvokePrivate(door, "Awake");

            Transform runtimeLeaf = root.transform.Find("RuntimeDoorLeaf");
            Assert.That(runtimeLeaf, Is.Not.Null);
            Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(sourceRenderer.enabled, Is.False);
            Assert.That(sourceCollider.isTrigger, Is.True,
                "The fixed doorway collider must remain available for repeated raycast interaction.");
            Assert.That(runtimeLeaf.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(runtimeLeaf.GetComponent<BoxCollider>().isTrigger, Is.False);

            SetField(door, "animationDuration", 0f);
            SetServerActive(true);
            Assert.That(doorType.GetMethod("SetOpenServer")?.Invoke(door, new object[] { true }), Is.True);
            Assert.That(root.transform.localPosition, Is.EqualTo(Vector3.zero),
                "Opening the leaf must never move the networked doorway root.");
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

        private static void InvokePrivate(object target, string methodName)
        {
            target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(target, null);
        }

        private static void SetServerActive(bool active)
        {
            typeof(NetworkServer)
                .GetProperty(nameof(NetworkServer.active), BindingFlags.Public | BindingFlags.Static)
                ?.SetValue(null, active);
        }
    }
}
