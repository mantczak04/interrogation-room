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

        /// <summary>Sekretny Cel of the viewer; null when the viewer owns none.</summary>
        public SecretObjectiveView SecretObjective { get; }

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
            PlayerId detective)
        {
            Viewer = viewer;
            Phase = phase;
            Role = role;
            Players = players ?? throw new System.ArgumentNullException(nameof(players));
            Detective = detective;
            CrimeDescription = crimeDescription;
            Alibi = alibi;
            SecretObjective = secretObjective;
            Result = result;
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

    /// <summary>Individual outcome of the Runda for one viewer (ADR-0002).</summary>
    public sealed class PlayerResultView
    {
        /// <summary>Whether this viewer personally won the Runda.</summary>
        public bool Won { get; }

        /// <summary>Przetrwanie — whether this viewer avoided the Egzekucja.</summary>
        public bool Survived { get; }

        public bool DetectiveWon { get; }

        public RoundEndCause EndCause { get; }

        public PlayerId? ExecutedPlayer { get; }

        public PlayerResultView(bool won, bool survived, bool detectiveWon, RoundEndCause endCause, PlayerId? executedPlayer)
        {
            Won = won;
            Survived = survived;
            DetectiveWon = detectiveWon;
            EndCause = endCause;
            ExecutedPlayer = executedPlayer;
        }
    }
}
