using System;
using System.Collections.Generic;

namespace InterrogationRoom.Items
{
    public enum CarryItemState
    {
        AtHome,
        Dropped,
        Carried,
        Placed
    }

    public static class CarryItemRules
    {
        public static bool CanPickup(
            CarryItemState state,
            bool actorCanAct,
            bool actorAlreadyCarriesItem)
        {
            return actorCanAct &&
                   !actorAlreadyCarriesItem &&
                   state != CarryItemState.Carried;
        }

        public static bool SlotAccepts(
            string itemId,
            bool acceptsAnyItem,
            IReadOnlyList<string> acceptedItemIds)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;
            if (acceptsAnyItem)
                return true;
            if (acceptedItemIds == null)
                return false;

            for (int index = 0; index < acceptedItemIds.Count; index++)
            {
                if (string.Equals(itemId, acceptedItemIds[index], StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public static bool ShouldReturnHome(
            CarryItemState state,
            double now,
            double droppedAt,
            double returnTimeout,
            double worldY,
            double outOfBoundsY)
        {
            if (state != CarryItemState.Dropped)
                return false;
            if (worldY < outOfBoundsY)
                return true;

            return returnTimeout > 0d &&
                   droppedAt >= 0d &&
                   now - droppedAt >= returnTimeout;
        }
    }
}
