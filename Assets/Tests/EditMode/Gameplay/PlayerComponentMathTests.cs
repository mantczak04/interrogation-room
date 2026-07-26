using NUnit.Framework;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Tests
{
    public sealed class PlayerComponentMathTests
    {
        [Test]
        public void CameraObstacleDistanceUsesClosestValidHit()
        {
            float distance = PlayerCameraRig.ResolveClosestObstacleDistance(
                6f,
                new[] { 4.5f, 2.25f, 3f });

            Assert.That(distance, Is.EqualTo(2.25f));
        }

        [Test]
        public void CameraObstacleDistanceKeepsMaximumWithoutHits()
        {
            Assert.That(
                PlayerCameraRig.ResolveClosestObstacleDistance(6f, null),
                Is.EqualTo(6f));
        }

        [Test]
        public void SeatingBackOffsetRespectsMeasuredBackrestLimit()
        {
            float offset = PlayerSeating.ResolveBackOffset(
                configuredOffset: 0.06f,
                seatBackrestOffset: 0.12f,
                measuredTorsoBackDepth: 0.18f,
                hasMeasurement: true);

            Assert.That(offset, Is.EqualTo(-0.01f).Within(0.0001f));
        }

        [TestCase(0.2f, 1f, false)]
        [TestCase(1f, 1.1f, false)]
        [TestCase(1f, 1.04f, true)]
        public void HumanoidIkScaleValidationRejectsUnsafeRigScales(
            float humanScale,
            float visualScale,
            bool expected)
        {
            Assert.That(
                PlayerAnimationDriver.IsHumanoidIkScaleValid(
                    humanScale,
                    Vector3.one * visualScale),
                Is.EqualTo(expected));
        }
    }
}
