using NUnit.Framework;
using UnityEngine;

namespace InterrogationRoom.Voice.Tests
{
    public sealed class VoicePortalPathModelTests
    {
        [Test]
        public void SameRoomUsesTheDirectClearPath()
        {
            VoicePortalPath result = VoicePortalPathModel.Resolve(
                "interview",
                "interview",
                Vector3.zero,
                new Vector3(0f, 0f, 6f),
                new VoicePortal[0]);

            Assert.That(result.PathKind, Is.EqualTo(VoicePathKind.Clear));
            Assert.That(result.PathLength, Is.EqualTo(6f).Within(0.001f));
        }

        [Test]
        public void MissingPortalConnectionIsAFullWall()
        {
            VoicePortalPath result = VoicePortalPathModel.Resolve(
                "interview",
                "evidence",
                Vector3.zero,
                Vector3.right,
                new VoicePortal[0]);

            Assert.That(result.PathKind, Is.EqualTo(VoicePathKind.Blocked));
        }

        [Test]
        public void SynchronizedDoorStateChangesTheResolvedPath()
        {
            var closedDoor = new VoicePortal(
                "interview",
                "corridor",
                isOpen: false,
                new Vector3(1f, 0f, 0f));
            var openDoor = new VoicePortal(
                "interview",
                "corridor",
                isOpen: true,
                new Vector3(1f, 0f, 0f));

            VoicePortalPath closed = Resolve(closedDoor);
            VoicePortalPath open = Resolve(openDoor);

            Assert.That(closed.PathKind, Is.EqualTo(VoicePathKind.ClosedPortals));
            Assert.That(closed.ClosedPortalCount, Is.EqualTo(1));
            Assert.That(closed.ListenerDistanceToFirstClosedPortal, Is.EqualTo(1f).Within(0.001f));
            Assert.That(open.PathKind, Is.EqualTo(VoicePathKind.OpenPortals));
            Assert.That(open.ClosedPortalCount, Is.Zero);
        }

        [Test]
        public void CompletePathChoosesDoorNearestTheSpeakerWhenListenerSegmentsTie()
        {
            var portals = new[]
            {
                new VoicePortal("a", "b", true, new Vector3(0f, 0f, 2f)),
                new VoicePortal("a", "b", true, new Vector3(0f, 0f, -2f))
            };

            VoicePortalPath result = VoicePortalPathModel.Resolve(
                "a",
                "b",
                Vector3.zero,
                new Vector3(0f, 0f, -3f),
                portals);

            Assert.That(result.PathKind, Is.EqualTo(VoicePathKind.OpenPortals));
            Assert.That(result.PathLength, Is.EqualTo(3f).Within(0.001f),
                "Route selection must include the final portal-to-speaker segment.");
        }

        [Test]
        public void OpenRouteWinsOverAClosedShortcut()
        {
            var portals = new[]
            {
                new VoicePortal("a", "b", false, new Vector3(1f, 0f, 0f)),
                new VoicePortal("a", "c", true, new Vector3(0f, 0f, 3f)),
                new VoicePortal("c", "b", true, new Vector3(4f, 0f, 3f))
            };

            VoicePortalPath result = VoicePortalPathModel.Resolve(
                "a",
                "b",
                Vector3.zero,
                new Vector3(2f, 0f, 0f),
                portals);

            Assert.That(result.PathKind, Is.EqualTo(VoicePathKind.OpenPortals));
            Assert.That(result.ClosedPortalCount, Is.Zero);
        }

        private static VoicePortalPath Resolve(VoicePortal portal)
        {
            return VoicePortalPathModel.Resolve(
                "interview",
                "corridor",
                Vector3.zero,
                new Vector3(2f, 0f, 0f),
                new[] { portal });
        }
    }
}
