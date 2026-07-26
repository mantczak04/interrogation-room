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
        private float seatedHipsBackOffset;
        private float seatSurfaceHeight;
        private float backrestOffset;
        private float buttToHipsHeight;
        private float torsoBackDepth;
        private bool hasButtHeight;
        private int seatedFrameCount;

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
            ResetMeasurement();
        }

        public void SetSeatGeometry(float surfaceHeight, float seatBackrestOffset)
        {
            seatSurfaceHeight = surfaceHeight;
            backrestOffset = seatBackrestOffset;
        }

        public void SetLocalState(bool seated, bool enableCharacterController)
        {
            if (seated)
                ResetMeasurement();
            else
                RestoreVisualRoot();

            if (characterController != null)
                characterController.enabled = enableCharacterController;
        }

        public void Tick(bool seated, bool dead)
        {
            if (activeModelRoot == null)
                return;

            if (!seated || dead || animator == null || !animator.isHuman)
            {
                RestoreVisualRoot();
                ResetMeasurement();
                return;
            }

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null)
                return;

            seatedFrameCount++;
            float backOffset = ResolveBackOffset(
                seatedHipsBackOffset,
                backrestOffset,
                torsoBackDepth,
                hasButtHeight);
            Vector3 desired = transform.position - transform.forward * backOffset;
            Vector3 delta = desired - hips.position;
            delta.y = 0f;

            if (seatedFrameCount == 30 || seatedFrameCount == 90)
                MeasureSeatedBody(hips);

            if (hasButtHeight)
            {
                float seatTopY = transform.position.y +
                                 (seatSurfaceHeight > 0f ? seatSurfaceHeight : 0.46f);
                float desiredHipsY = seatTopY - 0.015f + buttToHipsHeight;
                delta.y = desiredHipsY - hips.position.y;
            }

            activeModelRoot.transform.position += delta;
        }

        public static float ResolveBackOffset(
            float configuredOffset,
            float seatBackrestOffset,
            float measuredTorsoBackDepth,
            bool hasMeasurement)
        {
            if (!hasMeasurement || seatBackrestOffset <= 0f)
                return configuredOffset;

            float backrestLimit = seatBackrestOffset - measuredTorsoBackDepth + 0.05f;
            return Mathf.Max(Mathf.Min(configuredOffset, backrestLimit), -0.10f);
        }

        private void MeasureSeatedBody(Transform hips)
        {
            SkinnedMeshRenderer skinnedRenderer =
                GetComponentInChildren<SkinnedMeshRenderer>(false);
            if (skinnedRenderer == null)
                return;

            seatedPoseMesh ??= new Mesh();
            skinnedRenderer.BakeMesh(seatedPoseMesh, true);

            Matrix4x4 localToWorld = skinnedRenderer.transform.localToWorldMatrix;
            Vector3 hipsPosition = hips.position;
            Vector3 back = -transform.forward;
            float lowestY = float.MaxValue;
            float backDepth = 0f;

            foreach (Vector3 vertex in seatedPoseMesh.vertices)
            {
                Vector3 world = localToWorld.MultiplyPoint3x4(vertex);
                Vector2 planar = new Vector2(
                    world.x - hipsPosition.x,
                    world.z - hipsPosition.z);
                if (planar.sqrMagnitude <= 0.17f * 0.17f &&
                    world.y <= hipsPosition.y &&
                    world.y >= hipsPosition.y - 0.3f &&
                    world.y < lowestY)
                {
                    lowestY = world.y;
                }

                if (world.y >= hipsPosition.y &&
                    world.y <= hipsPosition.y + 0.45f &&
                    planar.sqrMagnitude <= 0.3f * 0.3f)
                {
                    float depth = Vector3.Dot(world - hipsPosition, back);
                    if (depth > backDepth)
                        backDepth = depth;
                }
            }

            if (lowestY < float.MaxValue)
            {
                buttToHipsHeight = hipsPosition.y - lowestY;
                torsoBackDepth = backDepth;
                hasButtHeight = true;
            }
        }

        private void RestoreVisualRoot()
        {
            if (activeModelRoot != null)
                activeModelRoot.transform.localPosition = activeModelRootBaseLocalPosition;
        }

        private void ResetMeasurement()
        {
            seatedFrameCount = 0;
            hasButtHeight = false;
        }

        private void OnDestroy()
        {
            if (seatedPoseMesh != null)
                Destroy(seatedPoseMesh);
        }
    }
}
