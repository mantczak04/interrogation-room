using System;
using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Domain;
using UnityEngine;

namespace InterrogationRoom.Content
{
    /// <summary>
    /// Authoring-only ScriptableObject for one hand-authored case (ADR-0010):
    /// the public Przestępstwo and the ordered Alibi facts. Holds data and
    /// authoring validation, never game logic. Converted to an immutable
    /// <see cref="CaseDefinition"/> before StartRound; the domain and its tests
    /// never see this type.
    /// </summary>
    [CreateAssetMenu(menuName = "Interrogation Room/Case", fileName = "NewCase")]
    public sealed class CaseAsset : ScriptableObject
    {
        /// <summary>Recommended authored length; shorter cases only warn, they are not blocked.</summary>
        public const int RecommendedMinFacts = 6;

        [Tooltip("Tytuł sprawy (roboczy, nie pokazywany graczom).")]
        public string title;

        [Tooltip("Przestępstwo — jawne dla wszystkich graczy przez całą Rundę.")]
        [TextArea(2, 4)]
        public string crimeDescription;

        [Tooltip("Uporządkowana lista faktów Alibi. Kolejność jest częścią treści.")]
        public List<AuthoredFact> alibiFacts = new List<AuthoredFact>();

        [Tooltip("Minimalna liczba faktów ukrywanych Winnemu.")]
        [Min(0)]
        public int minHiddenFacts = 2;

        [Tooltip("Maksymalna liczba faktów ukrywanych Winnemu.")]
        [Min(0)]
        public int maxHiddenFacts = 2;

        [Serializable]
        public sealed class AuthoredFact
        {
            [Tooltip("Krótki fakt do ustnego relacjonowania, z detalami do przekręcenia.")]
            [TextArea(1, 3)]
            public string text;

            [Tooltip("możliwyDoUkrycia — tylko takie fakty mogą zostać ukryte Winnemu.")]
            public bool canBeHidden;
        }

        /// <summary>
        /// Converts the authored data into an immutable, Unity-free
        /// <see cref="CaseDefinition"/>. Later mutation of this asset does not
        /// affect a definition created earlier. Throws when the case is invalid
        /// (e.g. it cannot hide the required number of facts) so that a broken
        /// case never reaches the RoundEngine.
        /// </summary>
        public CaseDefinition ToDefinition()
        {
            var errors = Validate();
            if (errors.Count > 0)
                throw new InvalidOperationException($"CaseAsset '{name}' is invalid: {string.Join(" | ", errors)}");

            var facts = alibiFacts
                .Select((fact, index) => new AlibiFact($"fact-{index}", fact.text.Trim(), fact.canBeHidden))
                .ToList();
            return new CaseDefinition(title.Trim(), crimeDescription.Trim(), facts, minHiddenFacts, maxHiddenFacts);
        }

        /// <summary>
        /// Returns all deterministic authoring errors without logging or throwing.
        /// Editor tooling and the host lobby can use this before a Runda starts.
        /// </summary>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            CollectErrors(errors);
            return errors;
        }

        private void CollectErrors(List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(title))
                errors.Add("Title is empty.");
            if (string.IsNullOrWhiteSpace(crimeDescription))
                errors.Add("Przestępstwo text is empty.");
            if (alibiFacts == null || alibiFacts.Count == 0)
            {
                errors.Add("Alibi has no facts.");
                return;
            }
            if (alibiFacts.Any(f => f == null || string.IsNullOrWhiteSpace(f.text)))
                errors.Add("Alibi contains an empty fact.");
            if (minHiddenFacts < 0 || minHiddenFacts > maxHiddenFacts)
                errors.Add($"Hidden-fact range {minHiddenFacts}..{maxHiddenFacts} is invalid.");

            var hideableCount = alibiFacts.Count(f => f != null && f.canBeHidden);
            if (hideableCount < maxHiddenFacts)
                errors.Add($"Only {hideableCount} facts are marked możliwyDoUkrycia, but up to {maxHiddenFacts} must be hidden.");
        }

        private void OnValidate()
        {
            var errors = Validate();
            foreach (var error in errors)
                Debug.LogError($"CaseAsset '{name}': {error}", this);

            if (alibiFacts != null && alibiFacts.Count > 0 && alibiFacts.Count < RecommendedMinFacts)
                Debug.LogWarning($"CaseAsset '{name}': {alibiFacts.Count} facts — MVP recommends {RecommendedMinFacts}-10.", this);
        }
    }
}
