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
        public const int RequiredAlibiFactCount = 6;

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

        /// <summary>
        /// Author-approved compatible tellings of the same fact (colors, snacks,
        /// approximate times). Always non-empty and always contains
        /// <see cref="Text"/>; one entry is resolved per Runda from the seed.
        /// </summary>
        public IReadOnlyList<string> VariantTexts { get; }

        /// <summary>
        /// charakterystycznyDetal — a memorable detail that is irrelevant to the
        /// course of events. It may be hidden from the Winny, but never as the
        /// only hidden fact.
        /// </summary>
        public bool DistinctiveDetail { get; }

        public AlibiFact(
            string id,
            string text,
            bool canBeHidden,
            IEnumerable<string> variantTexts = null,
            bool distinctiveDetail = false)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            CanBeHidden = canBeHidden;
            DistinctiveDetail = distinctiveDetail;

            var pool = new List<string>();
            foreach (var variant in variantTexts ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(variant))
                    throw new ArgumentException($"Fact '{id}' has an empty variant text.", nameof(variantTexts));
                var trimmed = variant.Trim();
                if (!pool.Contains(trimmed))
                    pool.Add(trimmed);
            }

            if (pool.Count == 0)
                pool.Add(Text);
            else if (!pool.Contains(Text))
                throw new ArgumentException($"Variant pool of fact '{id}' must include its primary text.", nameof(variantTexts));
            VariantTexts = pool.ToArray();
        }
    }
}
