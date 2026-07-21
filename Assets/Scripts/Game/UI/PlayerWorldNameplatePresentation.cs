using System.Collections.Generic;
using InterrogationRoom.Networking;

namespace InterrogationRoom.UI
{
    public static class PlayerWorldNameplatePresentation
    {
        public static bool TryResolveDisplayName(
            IReadOnlyList<LobbyPlayerInfo> players,
            uint networkIdentityNetId,
            out string displayName)
        {
            displayName = string.Empty;
            if (players == null || networkIdentityNetId == 0u)
                return false;

            for (int index = 0; index < players.Count; index++)
            {
                LobbyPlayerInfo player = players[index];
                if (player.IsSimulated || player.NetworkIdentityNetId != networkIdentityNetId)
                    continue;

                displayName = LobbyPlayerPresentation.NormalizeDisplayName(
                    player.DisplayName,
                    "Gracz");
                return true;
            }

            return false;
        }

        public static bool ShouldShow(bool hasDisplayName, bool isLocalPlayer, bool isThirdPerson) =>
            hasDisplayName && (!isLocalPlayer || isThirdPerson);
    }
}
