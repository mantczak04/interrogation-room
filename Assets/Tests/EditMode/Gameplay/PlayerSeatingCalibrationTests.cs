using System.Collections.Generic;
using InterrogationRoom.Gameplay.Characters;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Tests
{
    public sealed class PlayerSeatingCalibrationTests
    {
        private const float ShallowestBackrestOffset = 0.161f;
        private const float RequiredSurfaceClearance = 0.004f;
        private const float RequiredBackrestClearance = 0.009f;
        private static readonly int SittingState =
            Animator.StringToHash("Base Layer.Sitting");

        [TestCaseSource(nameof(CharacterSeatHeights))]
        public void CalibratedSittingPoseClearsSeatAndShallowestBackrest(
            CharacterId character,
            float seatSurfaceHeight)
        {
            GameObject playerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            Assert.That(playerPrefab, Is.Not.Null);

            Component visualSource = playerPrefab.GetComponent("PlayerController");
            Assert.That(visualSource, Is.Not.Null);
            System.Reflection.MethodInfo createPreview =
                visualSource.GetType().GetMethod("CreateCharacterPreview");
            Assert.That(createPreview, Is.Not.Null);

            var seatRoot = new GameObject($"TEMP_{character}_SeatingTest");
            Mesh bakedMesh = null;

            try
            {
                var preview = (GameObject)createPreview.Invoke(
                    visualSource,
                    new object[] { character, seatRoot.transform });
                Assert.That(preview, Is.Not.Null);

                Animator animator = preview.GetComponent<Animator>();
                Assert.That(animator, Is.Not.Null);
                RuntimeAnimatorController controller =
                    animator.runtimeAnimatorController;
                Avatar avatar = animator.avatar;

                PlayerSeating seating = seatRoot.AddComponent<PlayerSeating>();
                seating.Configure(null, animator, hipsBackOffset: 0.06f);
                seating.SetVisualRoot(preview, preview.transform.localPosition);
                seating.SetSeatGeometry(
                    seatSurfaceHeight,
                    ShallowestBackrestOffset);

                Assert.That(
                    seating.CalibrateSeatedPose(controller, avatar),
                    Is.True,
                    $"{character} must synchronously calibrate its Sitting pose.");

                animator.Play(SittingState, 0, 0.5f);
                animator.Update(0f);
                seating.Tick(seated: true, dead: false);
                seating.ApplySeatedLegPose(seated: true, dead: false);

                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                SkinnedMeshRenderer renderer =
                    preview.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Assert.That(hips, Is.Not.Null);
                Assert.That(renderer, Is.Not.Null);

                bakedMesh = new Mesh();
                renderer.BakeMesh(bakedMesh, true);

                float lowestPosteriorY = float.MaxValue;
                float torsoBackDepth = 0f;
                Matrix4x4 localToWorld = renderer.transform.localToWorldMatrix;
                foreach (Vector3 vertex in bakedMesh.vertices)
                {
                    Vector3 world = localToWorld.MultiplyPoint3x4(vertex);
                    Vector3 relativeToHips = world - hips.position;
                    float sideways = Vector3.Dot(relativeToHips, preview.transform.right);
                    float forward = Vector3.Dot(relativeToHips, preview.transform.forward);

                    if (Mathf.Abs(sideways) <= 0.45f &&
                        forward >= -0.4f &&
                        forward <= 0.08f &&
                        relativeToHips.y >= -0.4f &&
                        relativeToHips.y <= 0.02f)
                    {
                        lowestPosteriorY = Mathf.Min(lowestPosteriorY, world.y);
                    }

                    if (relativeToHips.y >= -0.3f &&
                        relativeToHips.y <= 0.65f &&
                        Mathf.Abs(sideways) <= 0.5f &&
                        forward <= 0.15f)
                    {
                        torsoBackDepth = Mathf.Max(torsoBackDepth, -forward);
                    }
                }

                float seatTopY =
                    seatRoot.transform.position.y + seatSurfaceHeight;
                Assert.That(
                    lowestPosteriorY,
                    Is.GreaterThanOrEqualTo(
                        seatTopY + RequiredSurfaceClearance),
                    $"{character} enters the seat surface.");

                float hipsBehindSeat = Vector3.Dot(
                    hips.position - seatRoot.transform.position,
                    -seatRoot.transform.forward);
                Assert.That(
                    hipsBehindSeat + torsoBackDepth,
                    Is.LessThanOrEqualTo(
                        ShallowestBackrestOffset -
                        RequiredBackrestClearance),
                    $"{character} enters the shallowest configured backrest.");
            }
            finally
            {
                if (bakedMesh != null)
                    Object.DestroyImmediate(bakedMesh);

                Object.DestroyImmediate(seatRoot);
            }
        }

        private static IEnumerable<TestCaseData> CharacterSeatHeights()
        {
            CharacterId[] characters =
            {
                CharacterId.Malpa,
                CharacterId.Wieprz,
                CharacterId.Jak,
                CharacterId.Karton,
                CharacterId.Ptaku
            };
            float[] seatHeights =
            {
                0.43f,
                0.5f,
                0.54f,
                0.546f,
                0.588f,
                0.844f,
                0.87f
            };

            foreach (CharacterId character in characters)
            {
                foreach (float seatHeight in seatHeights)
                {
                    yield return new TestCaseData(character, seatHeight)
                        .SetName(
                            $"{nameof(CalibratedSittingPoseClearsSeatAndShallowestBackrest)}" +
                            $"({character},{seatHeight:0.###})");
                }
            }
        }
    }
}
