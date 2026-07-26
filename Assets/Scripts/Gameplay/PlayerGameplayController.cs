using System;
using InterrogationRoom.Gameplay.Characters;
using InterrogationRoom.Gameplay.Interaction;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay
{
    /// <summary>
    /// Stable gameplay-facing surface of the network player.
    /// The concrete controller can remain in a later-compiled assembly while
    /// gameplay components compile behind their own assembly boundary.
    /// </summary>
    public abstract class PlayerGameplayController : NetworkBehaviour
    {
        public static event Action<PlayerGameplayController> ClientStarted;

        public abstract bool IsSeated { get; }
        public abstract bool IsDead { get; }
        public abstract bool IsThirdPerson { get; }
        public abstract CharacterId CharacterId { get; }
        public abstract Camera PlayerCamera { get; }

        public abstract bool TryRequestStand();
        public abstract bool TrySitServer(NetworkChairSeat seat);
        public abstract bool TrySwapCharacterServer(
            CharacterId newCharacter,
            out CharacterId previousCharacter);
        public abstract bool HasVisualFor(CharacterId candidate);
        public abstract void SetLocalModelVisible(bool visible);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            ClientStarted = null;
        }

        protected static void NotifyClientStarted(PlayerGameplayController player)
        {
            ClientStarted?.Invoke(player);
        }
    }
}
