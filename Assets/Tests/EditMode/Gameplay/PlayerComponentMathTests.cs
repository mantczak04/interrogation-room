using NUnit.Framework;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

#if ENABLE_INPUT_SYSTEM
    public sealed class PlayerCameraRigInputTests : InputTestFixture
    {
        [Test]
        public void ThirdPersonMouseLookTurnsPlayerBody()
        {
            GameObject playerObject = new GameObject("Third Person Player");
            GameObject cameraObject = new GameObject("Player Camera");

            try
            {
                cameraObject.transform.SetParent(playerObject.transform, false);
                Camera camera = cameraObject.AddComponent<Camera>();
                PlayerCameraRig rig = playerObject.AddComponent<PlayerCameraRig>();
                rig.Configure(camera, null, 1f, 6f, 0.5f);

                Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
                Mouse mouse = InputSystem.AddDevice<Mouse>();
                Press(keyboard.cKey, queueEventOnly: true);
                Set(mouse.delta, new Vector2(100f, 0f), queueEventOnly: true);
                InputSystem.Update();

                rig.Tick(isSeated: false, mouseSensitivity: 1f);

                Assert.That(rig.IsThirdPerson, Is.True);
                Assert.That(
                    Mathf.DeltaAngle(0f, playerObject.transform.eulerAngles.y),
                    Is.EqualTo(10f).Within(0.01f));
                Assert.That(
                    Mathf.DeltaAngle(0f, cameraObject.transform.localEulerAngles.y),
                    Is.Zero.Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
            }
        }
    }
#endif
}
