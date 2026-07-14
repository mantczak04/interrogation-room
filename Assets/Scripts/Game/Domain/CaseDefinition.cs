using System;
using System.Collections.Generic;
using System.Linq;

namespace InterrogationRoom.Domain
{
    /// <summary>
    /// Immutable, Unity-free description of one authored case: the public
    /// Przestępstwo and the ordered Alibi facts. Produced from a CaseAsset before
    /// StartRound; Edit Mode tests build it directly in code.
    /// </summary>
    public sealed class CaseDefinition
    {
        public string Title { get; }

        /// <summary>Przestępstwo — public for every player at all times.</summary>
        public string CrimeDescription { get; }

        /// <summary>Ordered list of Alibi facts; the order is authored content.</summary>
        public IReadOnlyList<AlibiFact> AlibiFacts { get; }

        /// <summary>Authored interpretive Tropy linked to hideable fact ids.</summary>
        public IReadOnlyList<AlibiClueDefinition> AlibiClues { get; }

        /// <summary>
        /// Inclusive range of facts hidden from the Winny. The exact count is
        /// drawn from the Runda seed at StartRound.
        /// </summary>
        public int MinHiddenFacts { get; }

        public int MaxHiddenFacts { get; }

        public CaseDefinition(
            string title,
            string crimeDescription,
            IEnumerable<AlibiFact> alibiFacts,
            int minHiddenFacts,
            int maxHiddenFacts,
            IEnumerable<AlibiClueDefinition> alibiClues = null)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            CrimeDescription = crimeDescription ?? throw new ArgumentNullException(nameof(crimeDescription));
            if (alibiFacts == null) throw new ArgumentNullException(nameof(alibiFacts));
            AlibiFacts = alibiFacts.ToArray();
            AlibiClues = (alibiClues ?? Array.Empty<AlibiClueDefinition>()).ToArray();
            MinHiddenFacts = minHiddenFacts;
            MaxHiddenFacts = maxHiddenFacts;
        }
    }

    /// <summary>One short, orally relayable Alibi fact.</summary>
    public sealed class AlibiFact
    {
        /// <summary>Stable identifier of the fact within its case.</summary>
        public string Id { get; }

        public string Text { get; }

        /// <summary>
        /// możliwyDoUkrycia — only facts marked by the author as hideable may be
        /// hidden from the Winny.
        /// </summary>
        public bool CanBeHidden { get; }

        public AlibiFact(string id, string text, bool canBeHidden)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            CanBeHidden = canBeHidden;
        }
    }
}
