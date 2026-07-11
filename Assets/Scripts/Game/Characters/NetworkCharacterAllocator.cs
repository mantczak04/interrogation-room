using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Characters
{
    [DisallowMultipleComponent]
    public sealed class NetworkCharacterAllocator : MonoBehaviour
    {
        private CharacterAssignmentRoster roster;

        public static NetworkCharacterAllocator Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"Only one {nameof(NetworkCharacterAllocator)} may exist.", this);
                enabled = false;
                return;
            }

            Instance = this;
            roster = new CharacterAssignmentRoster(count => Random.Range(0, count));
        }

        public CharacterId Acquire(int connectionId)
        {
            if (!NetworkServer.active)
            {
                Debug.LogError("Only the active server may assign characters.", this);
                return CharacterId.Malpa;
            }

            return roster.Acquire(connectionId);
        }

        public void Release(int connectionId)
        {
            if (roster != null)
            {
                roster.Release(connectionId);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
