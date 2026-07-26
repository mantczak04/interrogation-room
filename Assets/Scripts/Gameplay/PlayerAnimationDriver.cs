using UnityEngine;

namespace InterrogationRoom.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        private const float LookAtDistance = 5f;
        private const float LookTargetSmoothSpeed = 25f;
        private const float MinLookAtHumanScale = 0.25f;
        private const float MaxLookAtHumanScale = 4f;
        private const float MaxVisualRootScaleDeviation = 0.05f;
        private const float MaxLookDownDegrees = 50f;
        private const float MaxLookUpDegrees = 60f;

        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int LookPitchParameter = Animator.StringToHash("LookPitch");
        private static readonly int IsSeatedParameter = Animator.StringToHash("IsSeated");
        private static readonly int PunchParameter = Animator.StringToHash("Punch");
        private static readonly int PunchVariantParameter = Animator.StringToHash("PunchVariant");
        private static readonly int IsDeadParameter = Animator.StringToHash("IsDead");
        private static readonly int DanceParameter = Animator.StringToHash("Dance");

        private Animator animator;
        private Vector3 smoothedLookTarget;
        private bool hasSmoothedLookTarget;

        public void Configure(Animator target)
        {
            animator = target;
        }

        public void Rebind(
            RuntimeAnimatorController controller,
            Avatar avatar,
            bool isSeated,
            bool isDead)
        {
            if (animator == null)
                return;

            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.Rebind();
            animator.Update(0f);
            animator.SetBool(IsSeatedParameter, isSeated);
            animator.SetBool(IsDeadParameter, isDead);
            ResetLookAtIkState();
        }

        public void SetMovementSpeed(float speed, bool damped)
        {
            if (animator == null)
                return;

            if (damped)
                animator.SetFloat(SpeedParameter, speed, 0.1f, Time.deltaTime);
            else
                animator.SetFloat(SpeedParameter, speed);
        }

        public void SetLookPitch(float pitch) => animator?.SetFloat(LookPitchParameter, pitch);

        public void SetSeated(bool seated)
        {
            if (animator == null)
                return;

            animator.SetFloat(SpeedParameter, 0f);
            animator.SetBool(IsSeatedParameter, seated);
            ResetLookAtIkState();
        }

        public void SetDead(bool dead) => animator?.SetBool(IsDeadParameter, dead);

        public void SetDancing(bool dancing)
        {
            if (HasParameter(DanceParameter, AnimatorControllerParameterType.Bool))
                animator.SetBool(DanceParameter, dancing);
        }

        public void PlayPunch(int variant)
        {
            if (animator == null)
                return;

            if (HasParameter(PunchVariantParameter, AnimatorControllerParameterType.Int))
                animator.SetInteger(PunchVariantParameter, variant);

            animator.SetTrigger(PunchParameter);
        }

        public void ApplyLookAtIk(
            bool isDead,
            bool isLocalPlayer,
            bool isThirdPerson,
            float bodyRelativePitch,
            bool hasVisualRootScale,
            Vector3 visualRootScale)
        {
            if (animator == null || !animator.isHuman || isDead ||
                (isLocalPlayer && isThirdPerson))
            {
                ResetLookAtIkState();
                return;
            }

            if (!hasVisualRootScale ||
                !IsHumanoidIkScaleValid(animator.humanScale, visualRootScale))
            {
                animator.SetLookAtWeight(0f);
                ResetLookAtIkState();
                return;
            }

            Transform anchor = animator.GetBoneTransform(HumanBodyBones.Neck) ??
                               animator.GetBoneTransform(HumanBodyBones.Head);
            if (anchor == null)
            {
                animator.SetLookAtWeight(0f);
                ResetLookAtIkState();
                return;
            }

            float pitch = Mathf.Clamp(
                bodyRelativePitch,
                -MaxLookDownDegrees,
                MaxLookUpDegrees);
            Vector3 lookDirection =
                (Quaternion.AngleAxis(pitch, transform.right) * transform.forward).normalized;
            Vector3 desiredLookTarget = anchor.position + lookDirection * LookAtDistance;
            if (!hasSmoothedLookTarget)
            {
                smoothedLookTarget = desiredLookTarget;
                hasSmoothedLookTarget = true;
            }
            else
            {
                float smoothFactor = 1f - Mathf.Exp(-LookTargetSmoothSpeed * Time.deltaTime);
                smoothedLookTarget =
                    Vector3.Lerp(smoothedLookTarget, desiredLookTarget, smoothFactor);
            }

            animator.SetLookAtWeight(1f, 0.2f, 0.85f, 0.35f, 0.5f);
            animator.SetLookAtPosition(smoothedLookTarget);
        }

        public float GetRemoteLookPitch() =>
            animator != null ? animator.GetFloat(LookPitchParameter) : 0f;

        public static float GetPitchFromDirection(Vector3 worldDirection, Vector3 right)
        {
            if (worldDirection.sqrMagnitude <= Mathf.Epsilon)
                return 0f;

            Vector3 flatForward = Vector3.ProjectOnPlane(worldDirection, right);
            if (flatForward.sqrMagnitude <= Mathf.Epsilon)
                return worldDirection.y >= 0f ? MaxLookUpDegrees : -MaxLookDownDegrees;

            flatForward.Normalize();
            return Vector3.SignedAngle(flatForward, worldDirection.normalized, right);
        }

        public static bool IsHumanoidIkScaleValid(
            float humanScale,
            Vector3 visualRootScale)
        {
            if (humanScale < MinLookAtHumanScale || humanScale > MaxLookAtHumanScale)
                return false;

            float maxAxisDeviation = Mathf.Max(
                Mathf.Abs(visualRootScale.x - 1f),
                Mathf.Abs(visualRootScale.y - 1f),
                Mathf.Abs(visualRootScale.z - 1f));
            return maxAxisDeviation <= MaxVisualRootScaleDeviation;
        }

        private bool HasParameter(
            int parameterHash,
            AnimatorControllerParameterType parameterType)
        {
            if (animator == null)
                return false;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == parameterHash && parameter.type == parameterType)
                    return true;
            }

            return false;
        }

        private void ResetLookAtIkState()
        {
            hasSmoothedLookTarget = false;
        }
    }
}
