using InterrogationRoom.Gameplay.Interaction;
using InterrogationRoom.Minigames;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Minigames
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkTimedInteractable))]
    public sealed class MinigameSpec : MonoBehaviour
    {
        [Header("Presentation")]
        [SerializeField] private MinigameKind kind = MinigameKind.FileSearch;
        [SerializeField, TextArea(2, 5)] private string introText =
            "Znajdź właściwy wpis zgodnie z poleceniem.";
        [SerializeField] private int seed = 1;

        [Header("Server time gate")]
        [SerializeField, Min(0.25f)] private float minimumPlausibleDuration = 2f;

        [Header("Przeszukiwanie akt")]
        [SerializeField, Range(8, 12)] private int folderCount = 10;
        [SerializeField, Range(1900, 2100)] private int targetYear = 1998;
        [SerializeField, Min(0.25f)] private float wrongChoiceDelay = 2f;

        [Header("Zamek szyfrowy")]
        [SerializeField, Range(-1, 999)] private int contextCode = -1;
        [SerializeField, Range(1, 6)] private int maximumCodeAttempts = 3;

        [Header("Terminal kartoteki")]
        [SerializeField, Range(4, 9)] private int recordCount = 6;

        [Header("Optional failure consequence")]
        [SerializeField] private NetworkIncidentWorldAction failureIncidentSource;

        public MinigameKind Kind => kind;
        public string IntroText => introText ?? string.Empty;
        public int Seed => seed;
        public float MinimumPlausibleDuration => Mathf.Max(0.25f, minimumPlausibleDuration);
        public int FolderCount => Mathf.Clamp(folderCount, 8, 12);
        public int TargetYear => Mathf.Clamp(targetYear, 1900, 2100);
        public float WrongChoiceDelay => Mathf.Max(0.25f, wrongChoiceDelay);
        public int Code => contextCode >= 0 ? Mathf.Clamp(contextCode, 0, 999) : Mathf.Abs(seed % 1000);
        public int MaximumCodeAttempts => Mathf.Clamp(maximumCodeAttempts, 1, 6);
        public int RecordCount => Mathf.Clamp(recordCount, 4, 9);
        public bool RaisesIncidentOnFailure => failureIncidentSource != null;

        [Server]
        public bool ApplyFailureConsequenceServer(NetworkIdentity actor)
        {
            if (!NetworkServer.active || actor == null || failureIncidentSource == null)
                return false;
            if (!failureIncidentSource.TryBeginInteractionServer(actor))
                return false;

            bool completed = failureIncidentSource.TryCompleteInteractionServer(actor);
            if (!completed)
            {
                failureIncidentSource.CancelInteractionServer(
                    actor,
                    TimedInteractionCancellationReason.CompletionRejected);
            }
            return completed;
        }
    }
}
