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

        [Test]
        public void CeilingContactCancelsUpwardJumpVelocity()
        {
            Assert.That(
                PlayerJumpMotion.ClampVerticalVelocityAtCeiling(5.4f, touchingCeiling: true),
                Is.Zero);
        }

        [Test]
        public void CeilingContactKeepsFallingVelocity()
        {
            Assert.That(
                PlayerJumpMotion.ClampVerticalVelocityAtCeiling(-3f, touchingCeiling: true),
                Is.EqualTo(-3f));
            Assert.That(
                PlayerJumpMotion.ClampVerticalVelocityAtCeiling(5.4f, touchingCeiling: false),
                Is.EqualTo(5.4f));
        }

        [Test]
        public void SprintDrainsTheBudgetOverTheConfiguredDuration()
        {
            float charge = 1f;
            float delay = 0f;
            bool sprinting = true;

            // 3 s of held sprint at 60 fps must exactly empty a 3 s budget.
            for (int frame = 0; frame < 180; frame++)
            {
                sprinting = Advance(true, sprinting, ref charge, ref delay);
            }

            Assert.That(charge, Is.Zero.Within(0.0001f));
            Assert.That(
                Advance(true, sprinting, ref charge, ref delay),
                Is.False,
                "An empty budget must refuse to sprint.");
        }

        [Test]
        public void SprintWaitsForTheRecoveryDelayBeforeRefilling()
        {
            float charge = 0.5f;
            float delay = 0f;
            bool sprinting = Advance(true, false, ref charge, ref delay);

            Assert.That(sprinting, Is.True);
            Assert.That(delay, Is.EqualTo(0.6f).Within(0.0001f));

            float chargeAfterSprint = charge;
            Advance(false, sprinting, ref charge, ref delay);

            Assert.That(charge, Is.EqualTo(chargeAfterSprint), "Recovery must not start during the delay.");

            for (int frame = 0; frame < 60; frame++)
            {
                Advance(false, false, ref charge, ref delay);
            }

            Assert.That(charge, Is.GreaterThan(chargeAfterSprint));
        }

        [Test]
        public void DrainedSprintCannotRestartUntilTheMinimumChargeIsBack()
        {
            float charge = 0.1f;
            float delay = 0f;

            Assert.That(
                Advance(true, wasSprinting: false, ref charge, ref delay),
                Is.False,
                "0.1 is below the 0.25 restart threshold.");

            charge = 0.1f;
            Assert.That(
                Advance(true, wasSprinting: true, ref charge, ref delay),
                Is.True,
                "An ongoing sprint may run the budget down to zero.");
        }

        private static bool Advance(
            bool wantsSprint,
            bool wasSprinting,
            ref float charge,
            ref float delay)
        {
            return PlayerSprintStamina.Advance(
                wantsSprint,
                wasSprinting,
                deltaTime: 1f / 60f,
                sprintDurationSeconds: 3f,
                recoverySeconds: 6f,
                recoveryDelaySeconds: 0.6f,
                minimumChargeToStart: 0.25f,
                charge01: ref charge,
                recoveryDelayRemaining: ref delay);
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
