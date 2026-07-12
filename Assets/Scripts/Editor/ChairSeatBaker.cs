using System.Text;
using InterrogationRoom.Gameplay.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InterrogationRoom.EditorTools
{
    /// <summary>
    /// Bakes seating data for every NetworkChairSeat in the open scene so any
    /// chair asset gets a correct sit pose without hand-tuned offsets:
    /// 1. Seat centre comes from the chair's renderer bounds.
    /// 2. Facing is derived from the mesh itself — the backrest is where the
    ///    upper-half vertices cluster, and the sitter faces away from it.
    /// 3. A knee-zone box in front of the seat is checked against colliders;
    ///    a blocked chair is pulled away from the obstacle (e.g. a low table)
    ///    in small steps until the seated body no longer clips it.
    /// Re-running the bake is idempotent.
    /// </summary>
    public static class ChairSeatBaker
    {
        private const float KneeZoneForwardCenter = 0.3f;
        private const float KneeZoneHeightCenter = 0.375f;
        private static readonly Vector3 KneeZoneHalfExtents = new(0.17f, 0.225f, 0.25f);

        // The widest seated body (Wieprz) reaches ~0.32 sideways and ~0.34
        // behind the seat centre; the zone covers hips to shoulders.
        private static readonly Vector3 BodyZoneHalfExtents = new(0.32f, 0.45f, 0.35f);
        private const float BodyZoneHeightCenter = 0.6f;

        private const float PullBackStep = 0.05f;
        private const float MaxPullBack = 0.5f;

        [MenuItem("Tools/Interrogation Room/Bake Chair Seats")]
        public static void BakeAll()
        {
            var report = new StringBuilder("Chair seat bake:\n");
            foreach (NetworkChairSeat chair in Object.FindObjectsByType<NetworkChairSeat>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                report.AppendLine(Bake(chair));
            }

            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log(report.ToString());
        }

        private static string Bake(NetworkChairSeat chair)
        {
            Renderer[] renderers = chair.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return $"{chair.name}: SKIPPED (no renderer)";
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            Undo.RecordObject(chair.transform, "Bake Chair Seat");

            Vector3 facing = ResolveFacing(chair, bounds);
            Vector3 seatCenter = new(bounds.center.x, chair.transform.position.y, bounds.center.z);
            Transform seatPoint = GetOrCreateSeatPoint(chair);
            seatPoint.SetPositionAndRotation(seatCenter, Quaternion.LookRotation(facing, Vector3.up));

            float surfaceHeight = ResolveSeatSurfaceHeight(chair, bounds, seatCenter.y);
            float backrestOffset = ResolveBackrestOffset(chair, seatPoint, facing, surfaceHeight);
            var serialized = new SerializedObject(chair);
            serialized.FindProperty("seatSurfaceHeight").floatValue = surfaceHeight;
            serialized.FindProperty("backrestOffset").floatValue = backrestOffset;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            float pulledBack = PullChairClearOfObstacles(chair, seatPoint, facing);

            EditorUtility.SetDirty(chair);
            return $"{chair.name}: seat={seatPoint.position} facing={facing} " +
                   $"surfaceHeight={surfaceHeight:F2} backrestOffset={backrestOffset:F2} " +
                   $"pulledBack={pulledBack:F2}m";
        }

        /// <summary>
        /// The sitter faces away from the backrest. The backrest is found by
        /// averaging the mesh vertices in the top 40% of the chair: they sit
        /// horizontally off-centre on the backrest side.
        /// </summary>
        private static Vector3 ResolveFacing(NetworkChairSeat chair, Bounds bounds)
        {
            Vector3 backrestSum = Vector3.zero;
            int backrestCount = 0;
            float topThreshold = bounds.min.y + bounds.size.y * 0.6f;

            foreach (MeshFilter meshFilter in chair.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                foreach (Vector3 vertex in mesh.vertices)
                {
                    Vector3 worldVertex = meshFilter.transform.TransformPoint(vertex);
                    if (worldVertex.y >= topThreshold)
                    {
                        backrestSum += worldVertex;
                        backrestCount++;
                    }
                }
            }

            if (backrestCount > 0)
            {
                Vector3 backrestDirection = backrestSum / backrestCount - bounds.center;
                backrestDirection.y = 0f;
                if (backrestDirection.sqrMagnitude > 0.0004f)
                {
                    return -backrestDirection.normalized;
                }
            }

            Debug.LogWarning(
                $"[{nameof(ChairSeatBaker)}] {chair.name}: could not locate the backrest from the mesh; " +
                "falling back to -transform.forward. Verify the seat direction manually.",
                chair);
            return -chair.transform.forward;
        }

        /// <summary>
        /// Finds the sittable surface by raycasting straight down at the seat
        /// centre against the chair's actual meshes (via temporary
        /// MeshColliders). The backrest sits at the chair's edge, so the ray
        /// through the centre hits the seat plate.
        /// </summary>
        private static float ResolveSeatSurfaceHeight(NetworkChairSeat chair, Bounds bounds, float groundY)
        {
            var ray = new Ray(
                new Vector3(bounds.center.x, bounds.max.y + 0.5f, bounds.center.z),
                Vector3.down);

            if (!RaycastChairMeshes(chair, ray, bounds.size.y + 1f, out RaycastHit hit))
            {
                Debug.LogWarning(
                    $"[{nameof(ChairSeatBaker)}] {chair.name}: seat surface probe found no mesh under " +
                    "the seat centre; keeping the default height.",
                    chair);
                return 0.46f;
            }

            return Mathf.Max(0.1f, hit.point.y - groundY);
        }

        /// <summary>
        /// Finds the backrest's front face by raycasting backward at lower
        /// torso height from in front of the seat. Returns a large value when
        /// nothing is hit (a stool has no backrest to clip into).
        /// </summary>
        private static float ResolveBackrestOffset(
            NetworkChairSeat chair,
            Transform seatPoint,
            Vector3 facing,
            float surfaceHeight)
        {
            // Slatted backrests have gaps, so probe a fan of heights and keep
            // the closest hit.
            float best = float.MaxValue;
            for (float height = 0.1f; height <= 0.45f; height += 0.05f)
            {
                var ray = new Ray(
                    seatPoint.position + facing * 0.5f + Vector3.up * (surfaceHeight + height),
                    -facing);
                if (RaycastChairMeshes(chair, ray, 1.2f, out RaycastHit hit))
                {
                    float behindCenter = Vector3.Dot(hit.point - seatPoint.position, -facing);
                    if (behindCenter > 0.02f && behindCenter < best)
                    {
                        best = behindCenter;
                    }
                }
            }

            return best < float.MaxValue ? Mathf.Min(best, 0.5f) : 0.5f;
        }

        private static bool RaycastChairMeshes(
            NetworkChairSeat chair,
            Ray ray,
            float maxDistance,
            out RaycastHit closestHit)
        {
            closestHit = default;
            float closestDistance = float.MaxValue;

            foreach (MeshFilter meshFilter in chair.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                var probeObject = new GameObject("TEMP_SeatProbe");
                try
                {
                    probeObject.transform.SetPositionAndRotation(
                        meshFilter.transform.position,
                        meshFilter.transform.rotation);
                    probeObject.transform.localScale = meshFilter.transform.lossyScale;
                    var probe = probeObject.AddComponent<MeshCollider>();
                    probe.sharedMesh = mesh;

                    if (probe.Raycast(ray, out RaycastHit hit, maxDistance) &&
                        hit.distance < closestDistance)
                    {
                        closestDistance = hit.distance;
                        closestHit = hit;
                    }
                }
                finally
                {
                    Object.DestroyImmediate(probeObject);
                }
            }

            return closestDistance < float.MaxValue;
        }

        private static Transform GetOrCreateSeatPoint(NetworkChairSeat chair)
        {
            Transform seatPoint = chair.transform.Find("SeatPoint");
            if (seatPoint == null)
            {
                seatPoint = new GameObject("SeatPoint").transform;
                seatPoint.SetParent(chair.transform, false);
                Undo.RegisterCreatedObjectUndo(seatPoint.gameObject, "Bake Chair Seat");
            }

            var serialized = new SerializedObject(chair);
            serialized.FindProperty("seatPoint").objectReferenceValue = seatPoint;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return seatPoint;
        }

        /// <summary>
        /// Moves the whole chair away from whatever blocks the seated body's
        /// knee zone, as long as the chair itself has room to move into.
        /// </summary>
        private static float PullChairClearOfObstacles(
            NetworkChairSeat chair,
            Transform seatPoint,
            Vector3 facing)
        {
            float pulledBack = 0f;
            while (pulledBack < MaxPullBack && IsKneeZoneBlocked(chair, seatPoint, facing))
            {
                if (!CanChairOccupy(chair, -facing * PullBackStep))
                {
                    break;
                }

                chair.transform.position -= facing * PullBackStep;
                Physics.SyncTransforms();

                // Retreat must not push the sitter's torso into whatever is
                // behind or beside the chair (e.g. a wall); undo and stop.
                if (IsBodyZoneBlocked(chair, seatPoint, facing))
                {
                    chair.transform.position += facing * PullBackStep;
                    Physics.SyncTransforms();
                    break;
                }

                pulledBack += PullBackStep;
            }

            if (IsKneeZoneBlocked(chair, seatPoint, facing) || IsBodyZoneBlocked(chair, seatPoint, facing))
            {
                Debug.LogWarning(
                    $"[{nameof(ChairSeatBaker)}] {chair.name}: the seated body still overlaps nearby " +
                    "colliders after the pull-back; the chair is boxed in. Reposition it manually.",
                    chair);
            }

            return pulledBack;
        }

        private static bool IsKneeZoneBlocked(NetworkChairSeat chair, Transform seatPoint, Vector3 facing)
        {
            Vector3 center = seatPoint.position +
                             facing * KneeZoneForwardCenter +
                             Vector3.up * KneeZoneHeightCenter;
            return IsZoneBlocked(chair, center, KneeZoneHalfExtents, facing);
        }

        /// <summary>
        /// The space the seated torso occupies, including its sides and back.
        /// The front face stays behind the knees so a table the sitter faces
        /// does not trigger it.
        /// </summary>
        private static bool IsBodyZoneBlocked(NetworkChairSeat chair, Transform seatPoint, Vector3 facing)
        {
            Vector3 center = seatPoint.position + Vector3.up * BodyZoneHeightCenter;
            return IsZoneBlocked(chair, center, BodyZoneHalfExtents, facing);
        }

        private static bool IsZoneBlocked(
            NetworkChairSeat chair,
            Vector3 center,
            Vector3 halfExtents,
            Vector3 facing)
        {
            foreach (Collider overlap in Physics.OverlapBox(
                         center,
                         halfExtents,
                         Quaternion.LookRotation(facing, Vector3.up),
                         ~0,
                         QueryTriggerInteraction.Ignore))
            {
                if (!overlap.transform.IsChildOf(chair.transform))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanChairOccupy(NetworkChairSeat chair, Vector3 offset)
        {
            foreach (Collider own in chair.GetComponentsInChildren<Collider>(true))
            {
                var alreadyTouching = new System.Collections.Generic.HashSet<Collider>(
                    OverlapChairBox(chair, own.bounds));

                Bounds moved = own.bounds;
                moved.center += offset;
                foreach (Collider overlap in OverlapChairBox(chair, moved))
                {
                    // Colliders the chair already touches (e.g. the very table
                    // it is being pulled away from) only get farther away, so
                    // they do not veto the move.
                    if (!alreadyTouching.Contains(overlap))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static System.Collections.Generic.List<Collider> OverlapChairBox(
            NetworkChairSeat chair,
            Bounds bounds)
        {
            // Trim the bottom of the box so the floor under the chair does not
            // count as an obstacle.
            float bottom = bounds.min.y + 0.08f;
            var center = new Vector3(bounds.center.x, (bottom + bounds.max.y) * 0.5f, bounds.center.z);
            var extents = new Vector3(bounds.extents.x, (bounds.max.y - bottom) * 0.5f, bounds.extents.z);

            var result = new System.Collections.Generic.List<Collider>();
            foreach (Collider overlap in Physics.OverlapBox(
                         center,
                         extents,
                         Quaternion.identity,
                         ~0,
                         QueryTriggerInteraction.Ignore))
            {
                if (!overlap.transform.IsChildOf(chair.transform))
                {
                    result.Add(overlap);
                }
            }

            return result;
        }
    }
}
