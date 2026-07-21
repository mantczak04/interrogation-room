using System;
using System.Collections.Generic;
using UnityEngine;

namespace InterrogationRoom.Voice
{
    public enum VoicePathKind
    {
        Unknown,
        Clear,
        OpenPortals,
        ClosedPortals,
        Blocked
    }

    public readonly struct VoicePortal
    {
        public VoicePortal(string roomAId, string roomBId, bool isOpen, Vector3 position)
        {
            RoomAId = roomAId ?? string.Empty;
            RoomBId = roomBId ?? string.Empty;
            IsOpen = isOpen;
            Position = position;
        }

        public string RoomAId { get; }

        public string RoomBId { get; }

        public bool IsOpen { get; }

        public Vector3 Position { get; }
    }

    public readonly struct VoicePortalPath
    {
        public VoicePortalPath(
            VoicePathKind pathKind,
            int closedPortalCount,
            float pathLength,
            float listenerDistanceToFirstClosedPortal)
        {
            PathKind = pathKind;
            ClosedPortalCount = closedPortalCount;
            PathLength = pathLength;
            ListenerDistanceToFirstClosedPortal = listenerDistanceToFirstClosedPortal;
        }

        public VoicePathKind PathKind { get; }

        public int ClosedPortalCount { get; }

        public float PathLength { get; }

        public float ListenerDistanceToFirstClosedPortal { get; }
    }

    [Serializable]
    public struct VoiceAudibilityTuning
    {
        public float ClearCutoff;
        public float OpenPortalVolume;
        public float OpenPortalCutoff;
        public float OpenPathDetourHalfDistance;
        public float ClosedPortalVolume;
        public float ClosedPortalCutoff;
        public float EavesdropRange;
        public float EavesdropFalloff;
        public float WallVolume;
        public float WallCutoff;

        public static VoiceAudibilityTuning Default => new VoiceAudibilityTuning
        {
            ClearCutoff = 22000f,
            OpenPortalVolume = 0.85f,
            OpenPortalCutoff = 14000f,
            OpenPathDetourHalfDistance = 8f,
            ClosedPortalVolume = 0.1f,
            ClosedPortalCutoff = 750f,
            EavesdropRange = 0.75f,
            EavesdropFalloff = 0.75f,
            WallVolume = 0f,
            WallCutoff = 500f
        };
    }

    public struct VoiceAudibilityQuery
    {
        public VoicePathKind PathKind;
        public float DirectDistance;
        public float PortalPathLength;
        public int ClosedPortalCount;
        public float ListenerDistanceToNearestClosedPortal;
    }

    public readonly struct VoiceAudibility
    {
        public VoiceAudibility(float volumeMultiplier, float lowPassCutoff)
        {
            VolumeMultiplier = volumeMultiplier;
            LowPassCutoff = lowPassCutoff;
        }

        public float VolumeMultiplier { get; }

        public float LowPassCutoff { get; }
    }

    public static class VoiceAudibilityModel
    {
        public static VoiceAudibility Evaluate(VoiceAudibilityQuery query, VoiceAudibilityTuning tuning)
        {
            switch (query.PathKind)
            {
                case VoicePathKind.OpenPortals:
                    return EvaluateOpenPortals(query, tuning);
                case VoicePathKind.ClosedPortals:
                    return EvaluateClosedPortals(query, tuning);
                case VoicePathKind.Blocked:
                    return new VoiceAudibility(tuning.WallVolume, tuning.WallCutoff);
                case VoicePathKind.Clear:
                    return new VoiceAudibility(1f, tuning.ClearCutoff);
                default:
                    return new VoiceAudibility(tuning.WallVolume, tuning.WallCutoff);
            }
        }

        public static AnimationCurve BuildDistanceRolloffCurve(
            float conversationalDistance,
            float audibleDistance)
        {
            float safeAudibleDistance = Mathf.Max(1f, audibleDistance);
            float plateau = Mathf.Clamp(conversationalDistance / safeAudibleDistance, 0.01f, 0.5f);
            var curve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(plateau, 1f),
                new Keyframe(Mathf.Lerp(plateau, 1f, 0.25f), 0.5f),
                new Keyframe(Mathf.Lerp(plateau, 1f, 0.55f), 0.15f),
                new Keyframe(Mathf.Lerp(plateau, 1f, 0.8f), 0.05f),
                new Keyframe(1f, 0f));
            for (int index = 0; index < curve.length; index++)
                curve.SmoothTangents(index, 0f);

            return curve;
        }

        private static VoiceAudibility EvaluateOpenPortals(
            VoiceAudibilityQuery query,
            VoiceAudibilityTuning tuning)
        {
            float detour = Mathf.Max(0f, query.PortalPathLength - query.DirectDistance);
            float halfDistance = Mathf.Max(0.01f, tuning.OpenPathDetourHalfDistance);
            float detourFactor = halfDistance / (halfDistance + detour);
            return new VoiceAudibility(tuning.OpenPortalVolume * detourFactor, tuning.OpenPortalCutoff);
        }

        private static VoiceAudibility EvaluateClosedPortals(
            VoiceAudibilityQuery query,
            VoiceAudibilityTuning tuning)
        {
            if (query.ClosedPortalCount > 1)
                return new VoiceAudibility(0f, tuning.WallCutoff);

            float eavesdropEnd = tuning.EavesdropRange + Mathf.Max(0f, tuning.EavesdropFalloff);
            if (query.ListenerDistanceToNearestClosedPortal >= eavesdropEnd)
                return new VoiceAudibility(tuning.WallVolume, tuning.WallCutoff);

            float proximity = Mathf.InverseLerp(
                eavesdropEnd,
                tuning.EavesdropRange,
                query.ListenerDistanceToNearestClosedPortal);
            return new VoiceAudibility(
                Mathf.Lerp(tuning.WallVolume, tuning.ClosedPortalVolume, proximity),
                Mathf.Lerp(tuning.WallCutoff, tuning.ClosedPortalCutoff, proximity));
        }
    }

    public static class VoicePortalPathModel
    {
        public static VoicePortalPath Resolve(
            string listenerRoom,
            string speakerRoom,
            Vector3 listenerPosition,
            Vector3 speakerPosition,
            IReadOnlyList<VoicePortal> portals)
        {
            if (string.IsNullOrWhiteSpace(listenerRoom) || string.IsNullOrWhiteSpace(speakerRoom))
                return UnknownPath();

            if (string.Equals(listenerRoom, speakerRoom, StringComparison.Ordinal))
            {
                return new VoicePortalPath(
                    VoicePathKind.Clear,
                    0,
                    Vector3.Distance(listenerPosition, speakerPosition),
                    float.PositiveInfinity);
            }

            var visitedRooms = new HashSet<string>(StringComparer.Ordinal) { listenerRoom };
            VoicePortalPath bestPath = default;
            bool hasPath = false;
            Search(
                listenerRoom,
                speakerRoom,
                listenerPosition,
                speakerPosition,
                listenerPosition,
                0f,
                0,
                float.PositiveInfinity,
                portals,
                visitedRooms,
                ref bestPath,
                ref hasPath);

            return hasPath
                ? bestPath
                : new VoicePortalPath(
                    VoicePathKind.Blocked,
                    0,
                    0f,
                    float.PositiveInfinity);
        }

        private static void Search(
            string currentRoom,
            string speakerRoom,
            Vector3 listenerPosition,
            Vector3 speakerPosition,
            Vector3 arrivalPosition,
            float length,
            int closedPortalCount,
            float firstClosedPortalDistance,
            IReadOnlyList<VoicePortal> portals,
            HashSet<string> visitedRooms,
            ref VoicePortalPath bestPath,
            ref bool hasPath)
        {
            if (string.Equals(currentRoom, speakerRoom, StringComparison.Ordinal))
            {
                var candidate = new VoicePortalPath(
                    closedPortalCount == 0
                        ? VoicePathKind.OpenPortals
                        : VoicePathKind.ClosedPortals,
                    closedPortalCount,
                    length + Vector3.Distance(arrivalPosition, speakerPosition),
                    firstClosedPortalDistance);
                if (!hasPath || IsBetter(candidate, bestPath))
                {
                    bestPath = candidate;
                    hasPath = true;
                }

                return;
            }

            if (portals == null)
                return;

            for (int index = 0; index < portals.Count; index++)
            {
                VoicePortal portal = portals[index];
                string nextRoom = ResolveConnectedRoom(portal, currentRoom);
                if (string.IsNullOrWhiteSpace(nextRoom) || !visitedRooms.Add(nextRoom))
                    continue;

                int nextClosedCount = closedPortalCount + (portal.IsOpen ? 0 : 1);
                if (!hasPath || nextClosedCount <= bestPath.ClosedPortalCount)
                {
                    float nextFirstClosedDistance = firstClosedPortalDistance;
                    if (!portal.IsOpen && float.IsPositiveInfinity(nextFirstClosedDistance))
                    {
                        nextFirstClosedDistance = Vector3.Distance(listenerPosition, portal.Position);
                    }

                    Search(
                        nextRoom,
                        speakerRoom,
                        listenerPosition,
                        speakerPosition,
                        portal.Position,
                        length + Vector3.Distance(arrivalPosition, portal.Position),
                        nextClosedCount,
                        nextFirstClosedDistance,
                        portals,
                        visitedRooms,
                        ref bestPath,
                        ref hasPath);
                }

                visitedRooms.Remove(nextRoom);
            }
        }

        private static string ResolveConnectedRoom(VoicePortal portal, string currentRoom)
        {
            if (string.Equals(portal.RoomAId, currentRoom, StringComparison.Ordinal))
                return portal.RoomBId;
            if (string.Equals(portal.RoomBId, currentRoom, StringComparison.Ordinal))
                return portal.RoomAId;
            return string.Empty;
        }

        private static bool IsBetter(VoicePortalPath candidate, VoicePortalPath current) =>
            candidate.ClosedPortalCount < current.ClosedPortalCount ||
            candidate.ClosedPortalCount == current.ClosedPortalCount &&
            candidate.PathLength < current.PathLength;

        private static VoicePortalPath UnknownPath() => new VoicePortalPath(
            VoicePathKind.Unknown,
            0,
            0f,
            float.PositiveInfinity);
    }
}
