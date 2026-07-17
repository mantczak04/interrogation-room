using System.Collections.Generic;

namespace InterrogationRoom.Domain
{
    /// <summary>Roles within the Skład Rundy.</summary>
    public enum RoundRole
    {
        /// <summary>Detektyw.</summary>
        Detective,

        /// <summary>Winny.</summary>
        Guilty,

        /// <summary>Niewinny.</summary>
        Innocent
    }

    /// <summary>
    /// The only read path out of the domain (ADR-0011). Filtered by role and
    /// phase for exactly one viewer; contains that viewer's secrets and nothing
    /// about anyone else's. Delivered to each client as a targeted message.
    /// </summary>
    public sealed class PlayerRoundView
    {
        public PlayerId Viewer { get; }

        public RoundPhase Phase { get; }

        /// <summary>The viewer's own role only.</summary>
        public RoundRole Role { get; }

        /// <summary>Public Skład Rundy. Contains identities only, never suspect roles.</summary>
        public IReadOnlyList<PlayerId> Players { get; }

        /// <summary>The publicly known Detektyw.</summary>
        public PlayerId Detective { get; }

        /// <summary>Przestępstwo — public, present in every phase of the Runda.</summary>
        public string CrimeDescription { get; }

        /// <summary>
        /// The viewer's Alibi version. Non-null only for suspects during
        /// Przygotowanie (ADR-0006/0007); always null for the Detektyw — no
        /// facts, no redaction markers, not even the fact count.
        /// </summary>
        public AlibiView Alibi { get; }

        /// <summary>The viewer's own Prywatny Cel and progress; null for non-Niewinny roles.</summary>
        public PrivateObjectiveView PrivateObjective { get; }

        /// <summary>
        /// Detektyw-only private Rejestr, ordered by report or discovery time.
        /// Null for every suspect.
        /// </summary>
        public IReadOnlyList<IncidentRegistryEntryView> IncidentRegistry { get; }

        /// <summary>
        /// Host-owned Incydent authors, revealed to every player only after the
        /// Runda ends. Null during live play.
        /// </summary>
        public IReadOnlyList<IncidentRevealView> RevealedIncidents { get; }

        /// <summary>Acquired interpretive Tropy; Winny-only until the final reveal.</summary>
        public IReadOnlyList<AlibiClueView> AcquiredAlibiClues { get; }

        /// <summary>Private Plan Ucieczki visible only to the Winny.</summary>
        public EscapePlanView EscapePlan { get; }

        /// <summary>Complete approved reveal, non-null only after the Runda ends.</summary>
        public RoundRevealView RoundReveal { get; }

        /// <summary>Public readiness count; non-zero only during Przygotowanie.</summary>
        public int ReadyPlayerCount { get; }

        /// <summary>Whether the viewer already declared Gotowość; irreversible.</summary>
        public bool IsReady { get; }

        /// <summary>
        /// Compatibility projection for the pre-A1 networking contract. The
        /// canonical model is <see cref="PrivateObjective"/>.
        /// </summary>
        public SecretObjectiveView SecretObjective =>
            PrivateObjective?.Kind == PrivateObjectiveKind.SecretObjective
            && PrivateObjective.Target.HasValue
                ? new SecretObjectiveView(PrivateObjective.Target.Value)
                : null;

        /// <summary>The viewer's individual outcome; non-null only when Zakończona.</summary>
        public PlayerResultView Result { get; }

        public PlayerRoundView(
            PlayerId viewer,
            RoundPhase phase,
            RoundRole role,
            string crimeDescription,
            AlibiView alibi,
            SecretObjectiveView secretObjective,
            PlayerResultView result,
            IReadOnlyList<PlayerId> players,
            PlayerId detective,
            IReadOnlyList<IncidentRegistryEntryView> incidentRegistry = null,
            IReadOnlyList<IncidentRevealView> revealedIncidents = null,
            IReadOnlyList<AlibiClueView> acquiredAlibiClues = null,
            EscapePlanView escapePlan = null,
            RoundRevealView roundReveal = null,
            int readyPlayerCount = 0,
            bool isReady = false)
            : this(
                viewer,
                phase,
                role,
                crimeDescription,
                alibi,
                FromLegacySecretObjective(secretObjective),
                result,
                players,
                detective,
                incidentRegistry,
                revealedIncidents,
                acquiredAlibiClues,
                escapePlan,
                roundReveal,
                readyPlayerCount,
                isReady)
        {
        }

        public PlayerRoundView(
            PlayerId viewer,
            RoundPhase phase,
            RoundRole role,
            string crimeDescription,
            AlibiView alibi,
            PrivateObjectiveView privateObjective,
            PlayerResultView result,
            IReadOnlyList<PlayerId> players,
            PlayerId detective,
            IReadOnlyList<IncidentRegistryEntryView> incidentRegistry = null,
            IReadOnlyList<IncidentRevealView> revealedIncidents = null,
            IReadOnlyList<AlibiClueView> acquiredAlibiClues = null,
            EscapePlanView escapePlan = null,
            RoundRevealView roundReveal = null,
            int readyPlayerCount = 0,
            bool isReady = false)
        {
            Viewer = viewer;
            Phase = phase;
            Role = role;
            Players = players ?? throw new System.ArgumentNullException(nameof(players));
            Detective = detective;
            CrimeDescription = crimeDescription;
            Alibi = alibi;
            PrivateObjective = privateObjective;
            IncidentRegistry = incidentRegistry;
            RevealedIncidents = revealedIncidents;
            AcquiredAlibiClues = acquiredAlibiClues;
            EscapePlan = escapePlan;
            RoundReveal = roundReveal;
            ReadyPlayerCount = readyPlayerCount;
            IsReady = isReady;
            Result = result;
        }

        private static PrivateObjectiveView FromLegacySecretObjective(SecretObjectiveView secretObjective)
        {
            if (secretObjective == null)
                return null;

            var definition = PrivateObjectiveDefinitions.SecretObjective;
            var firstStep = definition.Steps[0];
            return new PrivateObjectiveView(
                definition.Id,
                definition.Kind,
                definition.Title,
                definition.Motive,
                firstStep.AnchorActionId,
                firstStep.Description,
                firstStep.LocationHint,
                completedStepCount: 0,
                definition.Steps.Count,
                isCompleted: false,
                secretObjective.Target);
        }
    }

    /// <summary>
    /// One role-filtered rendering of the Alibi: complete for a Niewinny, the
    /// same ordered content with redaction markers for the Winny.
    /// </summary>
    public sealed class AlibiView
    {
        public IReadOnlyList<AlibiEntry> Entries { get; }

        public AlibiView(IReadOnlyList<AlibiEntry> entries)
        {
            Entries = entries;
        }
    }

    /// <summary>
    /// One Alibi slot. A hidden entry is a structural hole: the Winny sees that
    /// a fact exists there but never its text.
    /// </summary>
    public sealed class AlibiEntry
    {
        public string FactId { get; }

        public bool IsHidden { get; }

        /// <summary>Fact text; null when hidden.</summary>
        public string Text { get; }

        public AlibiEntry(string factId, bool isHidden, string text)
        {
            FactId = factId;
            IsHidden = isHidden;
            Text = text;
        }
    }

    /// <summary>
    /// Private Sekretny Cel of one Niewinny: win requires the owner's own
    /// Przetrwanie and the elimination of the Cel (always another Niewinny).
    /// </summary>
    public sealed class SecretObjectiveView
    {
        public PlayerId Target { get; }

        public SecretObjectiveView(PlayerId target)
        {
            Target = target;
        }
    }

    /// <summary>
    /// Owner-only projection of a Prywatny Cel. It exposes the current step and
    /// aggregate progress, never another player's assignment or future steps.
    /// </summary>
    public sealed class PrivateObjectiveView
    {
        public PrivateObjectiveId Id { get; }
        public PrivateObjectiveKind Kind { get; }
        public string Title { get; }
        public string Motive { get; }
        public PrivateObjectiveStepId? CurrentStep { get; }
        public string CurrentStepDescription { get; }
        public string CurrentStepLocationHint { get; }
        public int CompletedStepCount { get; }
        public int TotalStepCount { get; }
        public bool IsCompleted { get; }
        public PlayerId? Target { get; }

        public PrivateObjectiveView(
            PrivateObjectiveId id,
            PrivateObjectiveKind kind,
            PrivateObjectiveStepId? currentStep,
            int completedStepCount,
            int totalStepCount,
            bool isCompleted,
            PlayerId? target)
            : this(
                id,
                kind,
                id.Value,
                id.Value,
                currentStep,
                currentStep?.Value,
                null,
                completedStepCount,
                totalStepCount,
                isCompleted,
                target)
        {
        }

        public PrivateObjectiveView(
            PrivateObjectiveId id,
            PrivateObjectiveKind kind,
            string title,
            string motive,
            PrivateObjectiveStepId? currentStep,
            string currentStepDescription,
            string currentStepLocationHint,
            int completedStepCount,
            int totalStepCount,
            bool isCompleted,
            PlayerId? target)
        {
            Id = id;
            Kind = kind;
            Title = title;
            Motive = motive;
            CurrentStep = currentStep;
            CurrentStepDescription = currentStepDescription;
            CurrentStepLocationHint = currentStepLocationHint;
            CompletedStepCount = completedStepCount;
            TotalStepCount = totalStepCount;
            IsCompleted = isCompleted;
            Target = target;
        }
    }

    /// <summary>Individual outcome of the Runda for one viewer (ADR-0013).</summary>
    public sealed class PlayerResultView
    {
        /// <summary>Whether this viewer personally won the Runda.</summary>
        public bool Won { get; }

        /// <summary>Przetrwanie — whether this viewer avoided the Egzekucja.</summary>
        public bool Survived { get; }

        public bool DetectiveWon { get; }

        public RoundEndCause EndCause { get; }

        public PlayerId? ExecutedPlayer { get; }

        public bool PrivateObjectiveCompleted { get; }

        /// <summary>True only for the Winny who completed the Ucieczka.</summary>
        public bool Escaped { get; }

        public PlayerResultView(
            bool won,
            bool survived,
            bool detectiveWon,
            RoundEndCause endCause,
            PlayerId? executedPlayer,
            bool privateObjectiveCompleted = false,
            bool escaped = false)
        {
            Won = won;
            Survived = survived;
            DetectiveWon = detectiveWon;
            EndCause = endCause;
            ExecutedPlayer = executedPlayer;
            PrivateObjectiveCompleted = privateObjectiveCompleted;
            Escaped = escaped;
        }
    }
}
