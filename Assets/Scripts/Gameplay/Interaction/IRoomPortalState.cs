using System.Collections.Generic;

namespace InterrogationRoom.Gameplay.Interaction
{
    /// <summary>
    /// Public, domain-independent state consumed later by spatial voice.
    /// </summary>
    public interface IRoomPortalState
    {
        string RoomAId { get; }

        string RoomBId { get; }

        bool IsOpen { get; }
    }

    public static class RoomPortalRegistry
    {
        private static readonly List<IRoomPortalState> Portals = new List<IRoomPortalState>();

        public static IReadOnlyList<IRoomPortalState> ActivePortals => Portals;

        internal static void Register(IRoomPortalState portal)
        {
            if (portal != null && !Portals.Contains(portal))
                Portals.Add(portal);
        }

        internal static void Unregister(IRoomPortalState portal)
        {
            if (portal != null)
                Portals.Remove(portal);
        }
    }
}
