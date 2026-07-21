using System.Collections.Generic;
using InterrogationRoom.Gameplay.Interaction;
using InterrogationRoom.Voice;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Ambience
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class AmbientRoomOcclusion : MonoBehaviour
    {
        private const float UpdateInterval = 0.1f;

        private readonly List<VoicePortal> portalSnapshot = new List<VoicePortal>();

        [SerializeField] private string sourceRoomId = "korytarz";
        [SerializeField, Range(0f, 1f)] private float baseVolume = 0.06f;
        [SerializeField, Range(0f, 1f)] private float openDoorMultiplier = 0.8f;
        [SerializeField, Range(0f, 1f)] private float closedDoorMultiplier = 0.18f;
        [SerializeField, Range(0f, 1f)] private float blockedMultiplier = 0.08f;
        [SerializeField, Min(0.01f)] private float transitionSpeed = 0.15f;

        private AudioSource audioSource;
        private AudioListener listener;
        private float nextUpdate;
        private float targetVolume;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            targetVolume = baseVolume;
            audioSource.volume = baseVolume;
        }

        private void Update()
        {
            if (audioSource == null)
                return;

            if (Time.unscaledTime >= nextUpdate)
            {
                nextUpdate = Time.unscaledTime + UpdateInterval;
                targetVolume = ResolveTargetVolume();
            }

            audioSource.volume = Mathf.MoveTowards(
                audioSource.volume,
                targetVolume,
                transitionSpeed * Time.unscaledDeltaTime);
        }

        private float ResolveTargetVolume()
        {
            if (listener == null || !listener.isActiveAndEnabled)
                listener = FindFirstObjectByType<AudioListener>();

            if (listener == null)
                return baseVolume;

            string listenerRoomId = RoomVolume.ResolveRoomId(listener.transform.position);
            if (string.Equals(listenerRoomId, sourceRoomId, System.StringComparison.Ordinal))
                return baseVolume;

            if (string.IsNullOrWhiteSpace(listenerRoomId))
                return baseVolume * blockedMultiplier;

            CopyPortalSnapshot();
            VoicePortalPath path = VoicePortalPathModel.Resolve(
                sourceRoomId,
                listenerRoomId,
                transform.position,
                listener.transform.position,
                portalSnapshot);

            float multiplier = path.PathKind switch
            {
                VoicePathKind.OpenPortals => openDoorMultiplier,
                VoicePathKind.ClosedPortals => closedDoorMultiplier,
                VoicePathKind.Clear => 1f,
                _ => blockedMultiplier
            };

            return baseVolume * multiplier;
        }

        private void CopyPortalSnapshot()
        {
            portalSnapshot.Clear();
            IReadOnlyList<IRoomPortalState> activePortals = RoomPortalRegistry.ActivePortals;
            for (int index = 0; index < activePortals.Count; index++)
            {
                IRoomPortalState portal = activePortals[index];
                if (portal == null)
                    continue;

                portalSnapshot.Add(new VoicePortal(
                    portal.RoomAId,
                    portal.RoomBId,
                    portal.IsOpen,
                    portal.PortalPosition));
            }
        }

        private void OnValidate()
        {
            closedDoorMultiplier = Mathf.Min(closedDoorMultiplier, openDoorMultiplier);
        }
    }
}
