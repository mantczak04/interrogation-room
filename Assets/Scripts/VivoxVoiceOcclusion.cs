using System.Collections.Generic;
using InterrogationRoom.Gameplay.Interaction;
using InterrogationRoom.Voice;
using UnityEngine;

public enum VoiceOcclusionState
{
    SameRoom,
    OpenPortalPath,
    ClosedPortalPath,
    Wall,
    Unknown
}

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioLowPassFilter))]
public sealed class VivoxVoiceOcclusion : MonoBehaviour
{
    private const float UpdateInterval = 0.1f;

    private readonly RaycastHit[] hits = new RaycastHit[16];
    private readonly List<VoicePortal> portalSnapshot = new List<VoicePortal>();

    private Transform listener;
    private Transform speaker;
    private AudioSource audioSource;
    private AudioLowPassFilter lowPassFilter;
    private LayerMask occlusionMask;
    private float nextUpdate;

    [Header("Acoustic presets")]
    [SerializeField] private VoiceAudibilityTuning tuning = VoiceAudibilityTuning.Default;
    [SerializeField, Min(0.1f)] private float volumeTransitionSpeed = 1.5f;
    [SerializeField, Min(10f)] private float cutoffTransitionSpeed = 30000f;

    public VoiceOcclusionState CurrentState { get; private set; } = VoiceOcclusionState.Unknown;

    public bool IsActivelyAttenuated =>
        CurrentState == VoiceOcclusionState.OpenPortalPath ||
        CurrentState == VoiceOcclusionState.ClosedPortalPath ||
        CurrentState == VoiceOcclusionState.Wall;

    public void Configure(
        Transform listenerTransform,
        Transform speakerTransform,
        AudioSource source,
        LayerMask mask)
    {
        listener = listenerTransform;
        speaker = speakerTransform;
        audioSource = source;
        occlusionMask = mask;
        lowPassFilter = GetComponent<AudioLowPassFilter>();
        ApplyImmediately(VoiceOcclusionState.Unknown);
    }

    private void Update()
    {
        if (listener == null || speaker == null || audioSource == null || Time.unscaledTime < nextUpdate)
        {
            return;
        }

        nextUpdate = Time.unscaledTime + UpdateInterval;
        string listenerRoom = ResolveRoomId(listener);
        string speakerRoom = ResolveRoomId(speaker);
        CopyPortalSnapshot(RoomPortalRegistry.ActivePortals, portalSnapshot);
        VoicePortalPath path = VoicePortalPathModel.Resolve(
            listenerRoom,
            speakerRoom,
            listener.position,
            speaker.position,
            portalSnapshot);

        VoiceOcclusionState state = MapOcclusionState(path.PathKind);
        if (state == VoiceOcclusionState.Unknown)
            state = HasBlockingGeometry(listener.position, speaker.position)
                ? VoiceOcclusionState.Wall
                : VoiceOcclusionState.SameRoom;

        VoiceAudibility target = VoiceAudibilityModel.Evaluate(BuildQuery(state, path), tuning);
        ApplySmoothly(state, target);
    }

    public static VoiceOcclusionState ResolvePortalState(
        string listenerRoom,
        string speakerRoom,
        IReadOnlyList<IRoomPortalState> portals)
    {
        return MapOcclusionState(
            ResolvePortalPath(listenerRoom, speakerRoom, Vector3.zero, Vector3.zero, portals).PathKind);
    }

    public static VoicePortalPath ResolvePortalPath(
        string listenerRoom,
        string speakerRoom,
        Vector3 listenerPosition,
        Vector3 speakerPosition,
        IReadOnlyList<IRoomPortalState> portals)
    {
        var snapshot = new List<VoicePortal>(portals?.Count ?? 0);
        CopyPortalSnapshot(portals, snapshot);
        return VoicePortalPathModel.Resolve(
            listenerRoom,
            speakerRoom,
            listenerPosition,
            speakerPosition,
            snapshot);
    }

    private static void CopyPortalSnapshot(
        IReadOnlyList<IRoomPortalState> portals,
        List<VoicePortal> destination)
    {
        destination.Clear();
        if (portals == null)
            return;

        for (int index = 0; index < portals.Count; index++)
        {
            IRoomPortalState portal = portals[index];
            if (portal == null)
                continue;

            destination.Add(new VoicePortal(
                portal.RoomAId,
                portal.RoomBId,
                portal.IsOpen,
                portal.PortalPosition));
        }
    }

    private static string ResolveRoomId(Transform player)
    {
        PlayerRoomTracker tracker = player.GetComponentInParent<PlayerRoomTracker>();
        if (tracker != null && !string.IsNullOrWhiteSpace(tracker.CurrentRoomId))
            return tracker.CurrentRoomId;

        return RoomVolume.ResolveRoomId(player.position);
    }

    private bool HasBlockingGeometry(Vector3 origin, Vector3 target)
    {
        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        if (distance <= Mathf.Epsilon)
            return false;

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            direction / distance,
            hits,
            distance,
            occlusionMask,
            QueryTriggerInteraction.Ignore);

        for (int index = 0; index < hitCount; index++)
        {
            Transform hitTransform = hits[index].transform;
            if (hitTransform != null &&
                !hitTransform.IsChildOf(listener) &&
                !hitTransform.IsChildOf(speaker))
            {
                return true;
            }
        }

        return false;
    }

    private VoiceAudibilityQuery BuildQuery(VoiceOcclusionState state, VoicePortalPath path)
    {
        return new VoiceAudibilityQuery
        {
            PathKind = MapPathKind(state),
            DirectDistance = Vector3.Distance(listener.position, speaker.position),
            PortalPathLength = path.PathLength,
            ClosedPortalCount = path.ClosedPortalCount,
            ListenerDistanceToNearestClosedPortal = path.ListenerDistanceToFirstClosedPortal
        };
    }

    private static VoiceOcclusionState MapOcclusionState(VoicePathKind pathKind)
    {
        switch (pathKind)
        {
            case VoicePathKind.Clear:
                return VoiceOcclusionState.SameRoom;
            case VoicePathKind.OpenPortals:
                return VoiceOcclusionState.OpenPortalPath;
            case VoicePathKind.ClosedPortals:
                return VoiceOcclusionState.ClosedPortalPath;
            case VoicePathKind.Blocked:
                return VoiceOcclusionState.Wall;
            default:
                return VoiceOcclusionState.Unknown;
        }
    }

    private static VoicePathKind MapPathKind(VoiceOcclusionState state)
    {
        switch (state)
        {
            case VoiceOcclusionState.OpenPortalPath:
                return VoicePathKind.OpenPortals;
            case VoiceOcclusionState.ClosedPortalPath:
                return VoicePathKind.ClosedPortals;
            case VoiceOcclusionState.Wall:
                return VoicePathKind.Blocked;
            default:
                return VoicePathKind.Clear;
        }
    }

    private void ApplySmoothly(VoiceOcclusionState state, VoiceAudibility target)
    {
        CurrentState = state;
        if (target.VolumeMultiplier <= 0f)
        {
            audioSource.volume = 0f;
            lowPassFilter.cutoffFrequency = target.LowPassCutoff;
            return;
        }

        audioSource.volume = Mathf.MoveTowards(
            audioSource.volume,
            target.VolumeMultiplier,
            volumeTransitionSpeed * UpdateInterval);
        lowPassFilter.cutoffFrequency = Mathf.MoveTowards(
            lowPassFilter.cutoffFrequency,
            target.LowPassCutoff,
            cutoffTransitionSpeed * UpdateInterval);
    }

    private void ApplyImmediately(VoiceOcclusionState state)
    {
        CurrentState = state;
        audioSource.volume = 1f;
        lowPassFilter.cutoffFrequency = tuning.ClearCutoff;
    }
}
