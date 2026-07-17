using System;
using InterrogationRoom.Gameplay.Interaction;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.EasterEggs
{
    /// <summary>
    /// A scene-authored public prop. Selection and completion are accepted only
    /// on the server; clients receive just availability and visible effect state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkEasterEggSpot : NetworkTimedInteractable
    {
        [Header("Stable hand-authored ids")]
        [SerializeField] private string easterEggId = string.Empty;
        [SerializeField] private string propId = string.Empty;
        [SerializeField] private string locationId = string.Empty;
        [SerializeField] private string effectId = string.Empty;

        [Header("Public presentation")]
        [SerializeField] private GameObject propRoot;
        [SerializeField] private GameObject dormantVisual;
        [SerializeField] private GameObject triggeredVisual;
        [SerializeField] private AudioSource effectAudioSource;
        [SerializeField] private AudioClip effectAudioClip;

        [SyncVar(hook = nameof(OnAvailabilityChanged))]
        private bool isAvailable;

        [SyncVar(hook = nameof(OnEffectRevisionChanged))]
        private int effectRevision;

        private int lastPresentedRevision;

        public string EasterEggId => easterEggId ?? string.Empty;
        public string PropId => propId ?? string.Empty;
        public string LocationId => locationId ?? string.Empty;
        public string EffectId => effectId ?? string.Empty;
        public bool IsAvailable => isAvailable;
        public int EffectRevision => effectRevision;

        public event Action<EasterEggWorldSignal> EffectTriggeredServer;

        private void Awake()
        {
            OneShot = true;
            ApplyPresentation();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyPresentation();
        }

        public override bool CanInteract(NetworkIdentity interactor)
        {
            return isAvailable && effectRevision == 0 && base.CanInteract(interactor);
        }

        [Server]
        public bool SetAvailableForRundaServer(bool available)
        {
            if (!NetworkServer.active || !HasCompleteAuthoredIds())
                return false;

            base.ResetInteractionStateServer();
            isAvailable = available;
            effectRevision = 0;
            lastPresentedRevision = 0;
            ApplyPresentation();
            return true;
        }

        protected override bool ApplyCompletedEffectServer(NetworkIdentity interactor)
        {
            if (!NetworkServer.active || !isAvailable || effectRevision != 0 || interactor == null)
                return false;

            effectRevision++;
            ApplyPresentation();
            EffectTriggeredServer?.Invoke(new EasterEggWorldSignal(
                EasterEggId,
                PropId,
                LocationId,
                EffectId,
                interactor,
                netIdentity));
            return true;
        }

        public bool HasCompleteAuthoredIds()
        {
            return !string.IsNullOrWhiteSpace(EasterEggId) &&
                   !string.IsNullOrWhiteSpace(PropId) &&
                   !string.IsNullOrWhiteSpace(LocationId) &&
                   !string.IsNullOrWhiteSpace(EffectId);
        }

        private void OnAvailabilityChanged(bool _, bool __)
        {
            ApplyPresentation();
        }

        private void OnEffectRevisionChanged(int _, int __)
        {
            ApplyPresentation();
        }

        private void ApplyPresentation()
        {
            if (propRoot != null)
                propRoot.SetActive(isAvailable);
            if (dormantVisual != null)
                dormantVisual.SetActive(isAvailable && effectRevision == 0);
            if (triggeredVisual != null)
                triggeredVisual.SetActive(isAvailable && effectRevision > 0);

            if (isAvailable &&
                effectRevision > lastPresentedRevision &&
                effectAudioSource != null &&
                effectAudioClip != null)
            {
                effectAudioSource.PlayOneShot(effectAudioClip);
            }

            lastPresentedRevision = effectRevision;
        }
    }
}
