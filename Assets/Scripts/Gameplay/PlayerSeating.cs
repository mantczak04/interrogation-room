using UnityEngine;

namespace InterrogationRoom.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerSeating : MonoBehaviour
    {
        private CharacterController characterController;
        private Animator animator;
        private GameObject activeModelRoot;
        private Vector3 activeModelRootBaseLocalPosition;
        private Mesh seatedPoseMesh;
        private readonly RaycastHit[] footSupportHits = new RaycastHit[16];
        private float seatedHipsBackOffset;
        private float seatSurfaceHeight;
        private float backrestOffset;
        private float buttToHipsHeight;
        private float torsoBackDepth;
        private float leftSoleToFootHeight;
        private float rightSoleToFootHeight;
        private float leftLowerLegLength;
        private float rightLowerLegLength;
        private bool hasSeatedPoseCalibration;
        private bool hasSeatedFootCalibration;

        private static readonly int SittingState = Animator.StringToHash("Base Layer.Sitting");
        private const float SeatSurfaceClearance = 0.005f;
        private const float BackrestClearance = 0.01f;
        private const float MaximumForwardSeatAdjustment = -0.2f;
        private const float MinimumFootDropBelowSeat = 0.05f;
        private const float FootSupportRayHeight = 0.15f;
        private const float MaximumFootSupportDistance = 2.5f;
        private const float MinimumSupportNormalY = 0.4f;

        public void Configure(
            CharacterController controller,
            Animator targetAnimator,
            float hipsBackOffset)
        {
            characterController = controller;
            animator = targetAnimator;
            seatedHipsBackOffset = hipsBackOffset;
        }

        public void SetVisualRoot(GameObject root, Vector3 baseLocalPosition)
        {
            activeModelRoot = root;
            activeModelRootBaseLocalPosition = baseLocalPosition;
            ResetCalibration();
        }

        public void SetSeatGeometry(float surfaceHeight, float seatBackrestOffset)
        {
            seatSurfaceHeight = surfaceHeight;
            backrestOffset = seatBackrestOffset;
        }

        public void SetLocalState(bool seated, bool enableCharacterController)
        {
            if (!seated)
                RestoreVisualRoot();

            if (characterController != null)
                characterController.enabled = enableCharacterController;
        }

        /// <summary>
        /// Samples the authored seated state synchronously before the character can render.
        /// Runtime seating then consumes only these cached measurements and never depends on
        /// which frame of the Animator transition happened to be observed.
        /// </summary>
        public bool CalibrateSeatedPose(
            RuntimeAnimatorController controller,
            Avatar avatar)
        {
            ResetCalibration();
            if (animator == null ||
                activeModelRoot == null ||
                controller == null ||
                avatar == null)
            {
                return false;
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.Rebind();
            animator.Update(0f);
            if (!animator.isHuman || !animator.HasState(0, SittingState))
            {
                return false;
            }

            try
            {
                animator.Play(SittingState, 0, 0.5f);
                animator.Update(0f);

                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips == null || !MeasureSeatedBody(hips))
                    return false;

                hasSeatedFootCalibration = MeasureSeatedLegs();
                return true;
            }
            finally
            {
                animator.Rebind();
                animator.Update(0f);
                RestoreVisualRoot();
            }
        }

        public void Tick(bool seated, bool dead)
        {
            if (activeModelRoot == null)
                return;

            if (!seated || dead || animator == null || !animator.isHuman)
            {
                RestoreVisualRoot();
                return;
            }

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null)
                return;

            float backOffset = ResolveBackOffset(
                seatedHipsBackOffset,
                backrestOffset,
                torsoBackDepth,
                hasSeatedPoseCalibration);
            Vector3 desired = transform.position - transform.forward * backOffset;
            Vector3 delta = desired - hips.position;
            delta.y = 0f;

            if (hasSeatedPoseCalibration)
            {
                float seatTopY = transform.position.y +
                                 (seatSurfaceHeight > 0f ? seatSurfaceHeight : 0.46f);
                float desiredHipsY =
                    seatTopY + SeatSurfaceClearance + buttToHipsHeight;
                delta.y = desiredHipsY - hips.position.y;
            }

            activeModelRoot.transform.position += delta;
        }

        public void ApplySeatedLegPose(bool seated, bool dead)
        {
            if (animator == null ||
                !animator.isHuman ||
                !seated ||
                dead ||
                !hasSeatedFootCalibration)
            {
                return;
            }

            ApplyLegPose(
                HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot,
                leftSoleToFootHeight,
                leftLowerLegLength);
            ApplyLegPose(
                HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightFoot,
                rightSoleToFootHeight,
                rightLowerLegLength);
        }

        public static float ResolveBackOffset(
            float configuredOffset,
            float seatBackrestOffset,
            float measuredTorsoBackDepth,
            bool hasMeasurement)
        {
            if (!hasMeasurement || seatBackrestOffset <= 0f)
                return configuredOffset;

            float backrestLimit =
                seatBackrestOffset - measuredTorsoBackDepth - BackrestClearance;
            return Mathf.Max(
                Mathf.Min(configuredOffset, backrestLimit),
                MaximumForwardSeatAdjustment);
        }

        public static Vector3 ResolveLowerLegTarget(
            Vector3 kneePosition,
            Vector3 animatedFootPosition,
            float supportHeight,
            float soleToFootHeight,
            float lowerLegLength,
            Vector3 fallbackForward)
        {
            float length = Mathf.Max(0f, lowerLegLength);
            if (length <= Mathf.Epsilon)
                return animatedFootPosition;

            float desiredFootY = supportHeight + soleToFootHeight;
            float verticalDrop = kneePosition.y - desiredFootY;
            Vector3 planarDirection = Vector3.ProjectOnPlane(
                animatedFootPosition - kneePosition,
                Vector3.up);
            if (planarDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                planarDirection =
                    Vector3.ProjectOnPlane(fallbackForward, Vector3.up);
            }

            if (verticalDrop >= 0f && verticalDrop <= length)
            {
                planarDirection = planarDirection.sqrMagnitude > Mathf.Epsilon
                    ? planarDirection.normalized
                    : Vector3.forward;
                float horizontalReach = Mathf.Sqrt(Mathf.Max(
                    0f,
                    length * length - verticalDrop * verticalDrop));
                return new Vector3(
                    kneePosition.x + planarDirection.x * horizontalReach,
                    desiredFootY,
                    kneePosition.z + planarDirection.z * horizontalReach);
            }

            Vector3 desired = new Vector3(
                animatedFootPosition.x,
                desiredFootY,
                animatedFootPosition.z);
            Vector3 desiredLowerLeg = desired - kneePosition;
            return desiredLowerLeg.sqrMagnitude > Mathf.Epsilon
                ? kneePosition + desiredLowerLeg.normalized * length
                : kneePosition;
        }

        private bool MeasureSeatedBody(Transform hips)
        {
            SkinnedMeshRenderer[] skinnedRenderers =
                GetComponentsInChildren<SkinnedMeshRenderer>(false);
            if (skinnedRenderers.Length == 0)
                return false;

            seatedPoseMesh ??= new Mesh();
            Vector3 hipsPosition = hips.position;
            Vector3 back = -transform.forward;
            float lowestY = float.MaxValue;
            float backDepth = 0f;

            foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers)
            {
                skinnedRenderer.BakeMesh(seatedPoseMesh, true);
                Matrix4x4 localToWorld =
                    skinnedRenderer.transform.localToWorldMatrix;

                foreach (Vector3 vertex in seatedPoseMesh.vertices)
                {
                    Vector3 world = localToWorld.MultiplyPoint3x4(vertex);
                    Vector3 relativeToHips = world - hipsPosition;
                    float sideways = Vector3.Dot(relativeToHips, transform.right);
                    float forward = Vector3.Dot(relativeToHips, transform.forward);

                    // Measure the whole posterior contact patch, not only a
                    // narrow circle under the hips. The wider Jak and Karton
                    // meshes otherwise sink into the seat outside that circle.
                    if (Mathf.Abs(sideways) <= 0.45f &&
                        forward >= -0.4f &&
                        forward <= 0.08f &&
                        relativeToHips.y >= -0.4f &&
                        relativeToHips.y <= 0.02f &&
                        world.y < lowestY)
                    {
                        lowestY = world.y;
                    }

                    // Include the full seated torso width and its lower edge.
                    // The former radial sample missed the outer silhouettes of
                    // the broadest characters when they met a shallow backrest.
                    if (relativeToHips.y >= -0.3f &&
                        relativeToHips.y <= 0.65f &&
                        Mathf.Abs(sideways) <= 0.5f &&
                        forward <= 0.15f)
                    {
                        float depth = Vector3.Dot(relativeToHips, back);
                        if (depth > backDepth)
                            backDepth = depth;
                    }
                }
            }

            if (lowestY < float.MaxValue)
            {
                buttToHipsHeight = hipsPosition.y - lowestY;
                torsoBackDepth = backDepth;
                hasSeatedPoseCalibration = true;
            }

            return hasSeatedPoseCalibration;
        }

        private bool MeasureSeatedLegs()
        {
            Transform leftLowerLeg =
                animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            Transform leftFoot =
                animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightLowerLeg =
                animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            Transform rightFoot =
                animator.GetBoneTransform(HumanBodyBones.RightFoot);

            if (leftLowerLeg == null ||
                leftFoot == null ||
                rightLowerLeg == null ||
                rightFoot == null)
            {
                return false;
            }

            leftLowerLegLength =
                Vector3.Distance(leftLowerLeg.position, leftFoot.position);
            rightLowerLegLength =
                Vector3.Distance(rightLowerLeg.position, rightFoot.position);

            SkinnedMeshRenderer[] skinnedRenderers =
                GetComponentsInChildren<SkinnedMeshRenderer>(false);
            return leftLowerLegLength > 0f &&
                   rightLowerLegLength > 0f &&
                   TryMeasureSoleToFootHeight(
                       leftFoot,
                       skinnedRenderers,
                       out leftSoleToFootHeight) &&
                   TryMeasureSoleToFootHeight(
                       rightFoot,
                       skinnedRenderers,
                       out rightSoleToFootHeight);
        }

        private bool TryMeasureSoleToFootHeight(
            Transform foot,
            SkinnedMeshRenderer[] skinnedRenderers,
            out float soleToFootHeight)
        {
            const float sampleRadius = 0.25f;
            const float minimumSampleHeight = -0.3f;
            const float maximumSampleHeight = 0.2f;
            float lowestY = float.MaxValue;

            foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers)
            {
                skinnedRenderer.BakeMesh(seatedPoseMesh, true);
                Matrix4x4 localToWorld =
                    skinnedRenderer.transform.localToWorldMatrix;

                foreach (Vector3 vertex in seatedPoseMesh.vertices)
                {
                    Vector3 world = localToWorld.MultiplyPoint3x4(vertex);
                    Vector3 relativeToFoot = world - foot.position;
                    if (relativeToFoot.y < minimumSampleHeight ||
                        relativeToFoot.y > maximumSampleHeight)
                    {
                        continue;
                    }

                    float horizontalDistanceSquared =
                        relativeToFoot.x * relativeToFoot.x +
                        relativeToFoot.z * relativeToFoot.z;
                    if (horizontalDistanceSquared <= sampleRadius * sampleRadius &&
                        world.y < lowestY)
                    {
                        lowestY = world.y;
                    }
                }
            }

            if (lowestY == float.MaxValue)
            {
                soleToFootHeight = 0f;
                return false;
            }

            soleToFootHeight = Mathf.Max(0f, foot.position.y - lowestY);
            return true;
        }

        private void ApplyLegPose(
            HumanBodyBones lowerLegBone,
            HumanBodyBones footBone,
            float soleToFootHeight,
            float lowerLegLength)
        {
            Transform lowerLeg = animator.GetBoneTransform(lowerLegBone);
            Transform foot = animator.GetBoneTransform(footBone);
            if (lowerLeg == null || foot == null)
                return;

            float supportHeight = FindFootSupportHeight(foot.position);
            Vector3 target = ResolveLowerLegTarget(
                lowerLeg.position,
                foot.position,
                supportHeight,
                soleToFootHeight,
                lowerLegLength,
                transform.forward);

            Quaternion authoredFootRotation = foot.rotation;
            lowerLeg.rotation =
                Quaternion.FromToRotation(
                    foot.position - lowerLeg.position,
                    target - lowerLeg.position) *
                lowerLeg.rotation;
            foot.rotation = authoredFootRotation;
        }

        private float FindFootSupportHeight(Vector3 animatedFootPosition)
        {
            float seatTopY = transform.position.y +
                             (seatSurfaceHeight > 0f ? seatSurfaceHeight : 0.46f);
            float maximumSupportY = seatTopY - MinimumFootDropBelowSeat;
            float rayStartY = Mathf.Max(
                seatTopY + FootSupportRayHeight,
                animatedFootPosition.y + FootSupportRayHeight);
            Vector3 rayOrigin =
                new Vector3(animatedFootPosition.x, rayStartY, animatedFootPosition.z);
            int hitCount = Physics.RaycastNonAlloc(
                rayOrigin,
                Vector3.down,
                footSupportHits,
                MaximumFootSupportDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            bool foundSupport = false;
            float supportHeight = float.MinValue;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = footSupportHits[index];
                if (hit.collider == null ||
                    hit.normal.y < MinimumSupportNormalY ||
                    hit.point.y > maximumSupportY ||
                    hit.collider.transform == transform ||
                    hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (!foundSupport || hit.point.y > supportHeight)
                {
                    foundSupport = true;
                    supportHeight = hit.point.y;
                }
            }

            return foundSupport ? supportHeight : transform.position.y;
        }

        private void RestoreVisualRoot()
        {
            if (activeModelRoot != null)
                activeModelRoot.transform.localPosition = activeModelRootBaseLocalPosition;
        }

        private void ResetCalibration()
        {
            hasSeatedPoseCalibration = false;
            hasSeatedFootCalibration = false;
            buttToHipsHeight = 0f;
            torsoBackDepth = 0f;
            leftSoleToFootHeight = 0f;
            rightSoleToFootHeight = 0f;
            leftLowerLegLength = 0f;
            rightLowerLegLength = 0f;
        }

        private void OnDestroy()
        {
            if (seatedPoseMesh != null)
                Destroy(seatedPoseMesh);
        }
    }
}
