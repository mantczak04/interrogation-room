using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    public sealed class QuietIncidentDiscoveryProbe : MonoBehaviour
    {
        [SerializeField] private NetworkIncidentWorldAction incidentSource;
        [SerializeField] private Transform discoveryPoint;
        [SerializeField, Min(0.25f)] private float discoveryRange = 2.5f;
        [SerializeField, Min(0f)] private float viewerEyeHeight = 1.6f;
        [SerializeField, Min(0.05f)] private float scanInterval = 0.15f;
        [SerializeField] private LayerMask lineOfSightMask = ~0;

        private readonly HashSet<string> emittedDiscoveries = new HashSet<string>();
        private double nextScanAt;

        public event Action<QuietIncidentDiscoveryCandidate> DiscoveryCandidateServer;

        private void Awake()
        {
            if (incidentSource == null)
                incidentSource = GetComponentInParent<NetworkIncidentWorldAction>();
            if (discoveryPoint == null)
                discoveryPoint = transform;
        }

        private void Update()
        {
            if (!NetworkServer.active || NetworkTime.time < nextScanAt)
                return;

            nextScanAt = NetworkTime.time + scanInterval;
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (connection?.identity != null)
                    TryDiscoverServer(connection.identity);
            }
        }

        public bool TryDiscoverServer(NetworkIdentity viewer)
        {
            if (!NetworkServer.active ||
                viewer == null ||
                incidentSource == null ||
                incidentSource.IncidentKind != PhysicalIncidentKind.Quiet ||
                incidentSource.RaisedIncidentsServer.Count == 0)
            {
                return false;
            }

            Vector3 target = discoveryPoint != null ? discoveryPoint.position : transform.position;
            if ((target - viewer.transform.position).sqrMagnitude > discoveryRange * discoveryRange ||
                !HasDirectLineOfSight(viewer, target))
            {
                return false;
            }

            bool emittedAny = false;
            foreach (PhysicalIncidentSignal incident in incidentSource.RaisedIncidentsServer)
            {
                string key = $"{incident.IncidentId}|{GetViewerKey(viewer)}";
                if (!emittedDiscoveries.Add(key))
                    continue;

                emittedAny = true;
                DiscoveryCandidateServer?.Invoke(new QuietIncidentDiscoveryCandidate(
                    incident.IncidentId,
                    viewer,
                    incident.Source));
            }

            return emittedAny;
        }

        private bool HasDirectLineOfSight(NetworkIdentity viewer, Vector3 target)
        {
            Vector3 origin = viewer.transform.TransformPoint(Vector3.up * viewerEyeHeight);
            Vector3 delta = target - origin;
            if (delta.sqrMagnitude < 0.0001f)
                return true;

            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                delta.normalized,
                delta.magnitude + 0.05f,
                lineOfSightMask,
                QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits.OrderBy(value => value.distance))
            {
                Transform hitTransform = hit.collider.transform;
                if (hitTransform == viewer.transform || hitTransform.IsChildOf(viewer.transform))
                    continue;

                Transform sourceTransform = incidentSource.transform;
                return hitTransform == sourceTransform || hitTransform.IsChildOf(sourceTransform);
            }

            return true;
        }

        private static int GetViewerKey(NetworkIdentity viewer) =>
            viewer.netId != 0
                ? unchecked((int)viewer.netId)
                : viewer.GetEntityId().GetHashCode();

        [Server]
        public void ResetDiscoveryStateServer()
        {
            if (!NetworkServer.active)
                return;

            emittedDiscoveries.Clear();
            nextScanAt = 0d;
        }
    }
}
