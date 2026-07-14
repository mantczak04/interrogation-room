using System;
using System.Collections.Generic;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RoomVolume : MonoBehaviour
    {
        private static readonly List<RoomVolume> ActiveVolumes = new List<RoomVolume>();

        [SerializeField] private string roomId = "room";
        [SerializeField] private int overlapPriority;
        [SerializeField] private BoxCollider volumeCollider;

        public string RoomId => roomId ?? string.Empty;

        private void Awake()
        {
            if (volumeCollider == null)
                volumeCollider = GetComponent<BoxCollider>();
        }

        private void OnEnable()
        {
            if (!ActiveVolumes.Contains(this))
                ActiveVolumes.Add(this);
        }

        private void OnDisable()
        {
            ActiveVolumes.Remove(this);
        }

        public bool Contains(Vector3 worldPosition)
        {
            if (volumeCollider == null || !volumeCollider.enabled)
                return false;

            Vector3 localPoint = volumeCollider.transform.InverseTransformPoint(worldPosition);
            Vector3 halfSize = volumeCollider.size * 0.5f;
            Vector3 delta = localPoint - volumeCollider.center;
            return Mathf.Abs(delta.x) <= halfSize.x &&
                   Mathf.Abs(delta.y) <= halfSize.y &&
                   Mathf.Abs(delta.z) <= halfSize.z;
        }

        public static string ResolveRoomId(Vector3 worldPosition)
        {
            RoomVolume best = null;
            foreach (RoomVolume candidate in ActiveVolumes)
            {
                if (candidate == null ||
                    string.IsNullOrWhiteSpace(candidate.roomId) ||
                    !candidate.Contains(worldPosition))
                {
                    continue;
                }

                if (best == null ||
                    candidate.overlapPriority > best.overlapPriority ||
                    candidate.overlapPriority == best.overlapPriority &&
                    string.Compare(candidate.roomId, best.roomId, StringComparison.Ordinal) < 0)
                {
                    best = candidate;
                }
            }

            return best != null ? best.RoomId : string.Empty;
        }

        private void OnValidate()
        {
            if (volumeCollider == null)
                volumeCollider = GetComponent<BoxCollider>();

            if (volumeCollider != null)
                volumeCollider.isTrigger = true;
        }
    }
}
