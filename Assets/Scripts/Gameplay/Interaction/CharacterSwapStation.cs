using System;
using InterrogationRoom.Gameplay.Characters;
using InterrogationRoom.UI;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    /// <summary>
    /// A standing character model in the scene. Interacting with it exchanges the
    /// player's current character with the displayed one, so every character stays
    /// unique across players and stations. Intended for animation/model testing.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class CharacterSwapStation : NetworkBehaviour, INetworkInteractable
    {
        [Serializable]
        private sealed class DisplayModel
        {
            public CharacterId characterId;
            public GameObject modelRoot;
        }

        [Header("Display")]
        [SerializeField] private DisplayModel[] displayModels = Array.Empty<DisplayModel>();
        [SerializeField] private CharacterId initialCharacter = CharacterId.Malpa;

        [Header("Interaction")]
        [SerializeField] private Transform interactionPoint;

        [SyncVar(hook = nameof(OnDisplayedCharacterChanged))]
        private CharacterId displayedCharacter;

        public Vector3 InteractionPosition => interactionPoint != null
            ? interactionPoint.position
            : transform.position + transform.up * 1f;

        public string InteractionPrompt => UiText.FormatCharacterSwapPrompt(
            displayedCharacter,
            UiText.CurrentLanguage);

        private void Awake()
        {
            if (GetComponentInChildren<Collider>(true) == null)
            {
                Debug.LogError(
                    $"[{nameof(CharacterSwapStation)}] {name} requires a Collider on the station or one of its children.",
                    this);
            }

            ApplyDisplayedCharacter(initialCharacter);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            displayedCharacter = initialCharacter;
            ApplyDisplayedCharacter(displayedCharacter);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyDisplayedCharacter(displayedCharacter);
        }

        public bool CanInteract(NetworkIdentity interactor)
        {
            return interactor != null &&
                   interactor.TryGetComponent(out PlayerController playerController) &&
                   !playerController.IsDead &&
                   !playerController.IsSeated &&
                   playerController.CharacterId != displayedCharacter &&
                   playerController.HasVisualFor(displayedCharacter) &&
                   HasModelFor(playerController.CharacterId);
        }

        public bool TryInteractServer(NetworkIdentity interactor)
        {
            if (!NetworkServer.active ||
                !CanInteract(interactor) ||
                !interactor.TryGetComponent(out PlayerController playerController) ||
                !playerController.TrySwapCharacterServer(displayedCharacter, out CharacterId previousCharacter))
            {
                return false;
            }

            displayedCharacter = previousCharacter;
            return true;
        }

        private bool HasModelFor(CharacterId candidate)
        {
            foreach (DisplayModel model in displayModels)
            {
                if (model != null && model.characterId == candidate && model.modelRoot != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDisplayedCharacterChanged(CharacterId _, CharacterId newCharacter)
        {
            ApplyDisplayedCharacter(newCharacter);
        }

        private void ApplyDisplayedCharacter(CharacterId activeCharacter)
        {
            foreach (DisplayModel model in displayModels)
            {
                if (model?.modelRoot != null)
                {
                    model.modelRoot.SetActive(model.characterId == activeCharacter);
                }
            }
        }
    }
}
