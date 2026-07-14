using System;
using System.Collections.Generic;
using InterrogationRoom.Gameplay.Interaction;
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
    private const float ClearCutoff = 22000f;
    private const float UpdateInterval = 0.1f;

    private readonly RaycastHit[] hits = new RaycastHit[16];

    private Transform listener;
    private Transform speaker;
    private AudioSource audioSource;
    private AudioLowPassFilter lowPassFilter;
    private LayerMask occlusionMask;
    private float nextUpdate;

    [Header("Acoustic presets")]
    [SerializeField, Range(0f, 1f)] private float openPortalVolume = 0.8f;
    [SerializeField, Min(10f)] private float openPortalCutoff = 12000f;
    [SerializeField, Range(0f, 1f)] private float closedPortalVolume = 0.25f;
    [SerializeField, Min(10f)] private float closedPortalCutoff = 1800f;
    [SerializeField, Range(0f, 1f)] private float wallVolume = 0.08f;
    [SerializeField, Min(10f)] private float wallCutoff = 900f;
    [SerializeField, Min(0.1f)] private float volumeTransitionSpeed = 5f;
    [SerializeField, Min(10f)] private float cutoffTransitionSpeed = 60000f;

    public VoiceOcclusionState CurrentState { get; private set; } = VoiceOcclusionState.Unknown;

    public bool IsActivelyAttenuated =>
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
        VoiceOcclusionState state = ResolvePortalState(
            listenerRoom,
            speakerRoom,
            RoomPortalRegistry.ActivePortals);

        if (state == VoiceOcclusionState.Unknown)
            state = HasBlockingGeometry(listener.position, speaker.position)
                ? VoiceOcclusionState.Wall
                : VoiceOcclusionState.SameRoom;

        ApplySmoothly(state);
    }

    public static VoiceOcclusionState ResolvePortalState(
        string listenerRoom,
        string speakerRoom,
        IReadOnlyList<IRoomPortalState> portals)
    {
        if (string.IsNullOrWhiteSpace(listenerRoom) || string.IsNullOrWhiteSpace(speakerRoom))
            return VoiceOcclusionState.Unknown;
        if (string.Equals(listenerRoom, speakerRoom, StringComparison.Ordinal))
            return VoiceOcclusionState.SameRoom;

        if (HasPortalPath(listenerRoom, speakerRoom, portals, openOnly: true))
            return VoiceOcclusionState.OpenPortalPath;
        if (HasPortalPath(listenerRoom, speakerRoom, portals, openOnly: false))
            return VoiceOcclusionState.ClosedPortalPath;

        return VoiceOcclusionState.Wall;
    }

    private static bool HasPortalPath(
        string startRoom,
        string destinationRoom,
        IReadOnlyList<IRoomPortalState> portals,
        bool openOnly)
    {
        var queue = new Queue<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal) { startRoom };
        queue.Enqueue(startRoom);

        while (queue.Count > 0)
        {
            string currentRoom = queue.Dequeue();
            for (int index = 0; index < portals.Count; index++)
            {
                IRoomPortalState portal = portals[index];
                if (portal == null || openOnly && !portal.IsOpen)
                    continue;

                string nextRoom = null;
                if (string.Equals(portal.RoomAId, currentRoom, StringComparison.Ordinal))
                    nextRoom = portal.RoomBId;
                else if (string.Equals(portal.RoomBId, currentRoom, StringComparison.Ordinal))
                    nextRoom = portal.RoomAId;

                if (string.IsNullOrWhiteSpace(nextRoom))
                    continue;

                if (!visited.Add(nextRoom))
                    continue;

                if (string.Equals(nextRoom, destinationRoom, StringComparison.Ordinal))
                    return true;

                queue.Enqueue(nextRoom);
            }
        }

        return false;
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

    private void ApplySmoothly(VoiceOcclusionState state)
    {
        CurrentState = state;
        GetPreset(state, out float targetVolume, out float targetCutoff);
        audioSource.volume = Mathf.MoveTowards(
            audioSource.volume,
            targetVolume,
            volumeTransitionSpeed * UpdateInterval);
        lowPassFilter.cutoffFrequency = Mathf.MoveTowards(
            lowPassFilter.cutoffFrequency,
            targetCutoff,
            cutoffTransitionSpeed * UpdateInterval);
    }

    private void ApplyImmediately(VoiceOcclusionState state)
    {
        CurrentState = state;
        GetPreset(state, out float targetVolume, out float targetCutoff);
        audioSource.volume = targetVolume;
        lowPassFilter.cutoffFrequency = targetCutoff;
    }

    private void GetPreset(VoiceOcclusionState state, out float volume, out float cutoff)
    {
        switch (state)
        {
            case VoiceOcclusionState.OpenPortalPath:
                volume = openPortalVolume;
                cutoff = openPortalCutoff;
                break;
            case VoiceOcclusionState.ClosedPortalPath:
                volume = closedPortalVolume;
                cutoff = closedPortalCutoff;
                break;
            case VoiceOcclusionState.Wall:
                volume = wallVolume;
                cutoff = wallCutoff;
                break;
            default:
                volume = 1f;
                cutoff = ClearCutoff;
                break;
        }
    }
}
