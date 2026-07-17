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
        /// <summary>Every Alibi has exactly this many readable points; other lengths are blocked.</summary>
        public const int RequiredFactCount = CaseDefinition.RequiredAlibiFactCount;

        [Tooltip("Tytuł sprawy (roboczy, nie pokazywany graczom).")]
        public string title;

        [Tooltip("Przestępstwo — jawne dla wszystkich graczy przez całą Rundę.")]
        [TextArea(2, 4)]
        public string crimeDescription;

        [Tooltip("Uporządkowana lista faktów Alibi. Kolejność jest częścią treści.")]
        public List<AuthoredFact> alibiFacts = new List<AuthoredFact>();

        [Tooltip("Ręcznie napisane Tropy powiązane ze stabilnymi id ukrywalnych faktów.")]
        public List<AuthoredAlibiClue> alibiClues = new List<AuthoredAlibiClue>();

        [Tooltip("Minimalna liczba faktów ukrywanych Winnemu.")]
        [Min(0)]
        public int minHiddenFacts = 2;

        [Tooltip("Maksymalna liczba faktów ukrywanych Winnemu.")]
        [Min(0)]
        public int maxHiddenFacts = 2;

        [Serializable]
        public sealed class AuthoredFact
        {
            [Tooltip("Stabilne id faktu. Wymagane, gdy wskazuje go Trop do Alibi.")]
            public string id;

            [Tooltip("Krótki fakt do ustnego relacjonowania, z detalami do przekręcenia.")]
            [TextArea(1, 3)]
            public string text;

            [Tooltip("możliwyDoUkrycia — tylko takie fakty mogą zostać ukryte Winnemu.")]
            public bool canBeHidden;

            [Tooltip("Zgodne warianty tego samego faktu (kolory, przekąski, przybliżone godziny). Tekst główny musi być jednym z nich; pusta lista wyłącza rotację.")]
            public List<string> variantTexts = new List<string>();

            [Tooltip("charakterystycznyDetal — zapamiętywalny, ale nieistotny dla przebiegu wydarzeń szczegół.")]
            public bool distinctiveDetail;
        }

        [Serializable]
        public sealed class AuthoredAlibiClue
        {
            [Tooltip("Stabilne id Tropu do Alibi.")]
            public string id;

            [Tooltip("Stabilne id ukrywalnego faktu, którego dotyczy Trop.")]
            public string linkedFactId;

            [Tooltip("Pośrednia treść do interpretacji, nigdy kopia faktu Alibi.")]
            [TextArea(1, 3)]
            public string content;
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
                .Select((fact, index) => new AlibiFact(
                    ResolveFactId(fact, index),
                    fact.text.Trim(),
                    fact.canBeHidden,
                    fact.variantTexts,
                    fact.distinctiveDetail))
                .ToList();
            var clues = (alibiClues ?? new List<AuthoredAlibiClue>())
                .Select(clue => new AlibiClueDefinition(
                    new AlibiClueId(clue.id.Trim()),
                    clue.linkedFactId.Trim(),
                    clue.content.Trim()))
                .ToList();
            return new CaseDefinition(
                title.Trim(),
                crimeDescription.Trim(),
                facts,
                minHiddenFacts,
                maxHiddenFacts,
                clues);
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
            if (alibiFacts.Count != RequiredFactCount)
                errors.Add($"Alibi must have exactly {RequiredFactCount} facts, got {alibiFacts.Count}.");
            if (alibiFacts.Any(f => f == null || string.IsNullOrWhiteSpace(f.text)))
                errors.Add("Alibi contains an empty fact.");
            if (!alibiFacts.Any(f => f != null && f.distinctiveDetail))
                errors.Add("Alibi must mark at least one fact as charakterystycznyDetal.");
            if (minHiddenFacts < 0 || minHiddenFacts > maxHiddenFacts)
                errors.Add($"Hidden-fact range {minHiddenFacts}..{maxHiddenFacts} is invalid.");
            CollectVariantErrors(errors);

            var hideableCount = alibiFacts.Count(f => f != null && f.canBeHidden);
            if (hideableCount < maxHiddenFacts)
                errors.Add($"Only {hideableCount} facts are marked możliwyDoUkrycia, but up to {maxHiddenFacts} must be hidden.");
            var factIds = alibiFacts
                .Select((fact, index) => fact == null ? null : ResolveFactId(fact, index))
                .ToArray();
            if (factIds.Where(id => id != null).Distinct().Count() != factIds.Count(id => id != null))
                errors.Add("Alibi contains duplicate fact ids.");

            var clues = alibiClues ?? new List<AuthoredAlibiClue>();
            if (clues.Any(clue => clue == null
                || string.IsNullOrWhiteSpace(clue.id)
                || string.IsNullOrWhiteSpace(clue.linkedFactId)
                || string.IsNullOrWhiteSpace(clue.content)))
            {
                errors.Add("Alibi clues contain missing id, fact link or content.");
            }

            var validClues = clues.Where(clue => clue != null
                && !string.IsNullOrWhiteSpace(clue.id)
                && !string.IsNullOrWhiteSpace(clue.linkedFactId)
                && !string.IsNullOrWhiteSpace(clue.content)).ToArray();
            if (validClues.Select(clue => clue.id.Trim()).Distinct().Count() != validClues.Length)
                errors.Add("Alibi clues contain duplicate ids.");

            foreach (var clue in validClues)
            {
                var linkedIndex = Array.FindIndex(
                    factIds,
                    factId => string.Equals(factId, clue.linkedFactId.Trim(), StringComparison.Ordinal));
                if (linkedIndex < 0)
                {
                    errors.Add($"Alibi clue '{clue.id}' has a missing fact link '{clue.linkedFactId}'.");
                    continue;
                }

                var linkedFact = alibiFacts[linkedIndex];
                if (!linkedFact.canBeHidden)
                    errors.Add($"Alibi clue '{clue.id}' must link to a hideable fact.");
                if (string.IsNullOrWhiteSpace(linkedFact.id))
                    errors.Add($"Alibi clue '{clue.id}' requires an explicit stable fact id.");
                var possibleTexts = linkedFact.variantTexts == null || linkedFact.variantTexts.Count == 0
                    ? new List<string> { linkedFact.text }
                    : linkedFact.variantTexts;
                if (possibleTexts
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .Any(text => clue.content.Trim().IndexOf(
                        text.Trim(),
                        StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    errors.Add($"Alibi clue '{clue.id}' cannot be a copy of any compatible variant text.");
                }
            }
        }

        private void CollectVariantErrors(List<string> errors)
        {
            var hasRotatingPool = false;
            for (var index = 0; index < alibiFacts.Count; index++)
            {
                var fact = alibiFacts[index];
                if (fact == null || fact.variantTexts == null || fact.variantTexts.Count == 0)
                    continue;

                var factId = ResolveFactId(fact, index);
                if (fact.variantTexts.Any(string.IsNullOrWhiteSpace))
                {
                    errors.Add($"Fact '{factId}' variant pool contains an empty text.");
                    continue;
                }

                var normalizedVariants = fact.variantTexts
                    .Select(text => text.Trim())
                    .ToArray();
                if (normalizedVariants.Distinct(StringComparer.OrdinalIgnoreCase).Count()
                    != normalizedVariants.Length)
                {
                    errors.Add($"Fact '{factId}' variant pool contains duplicate texts.");
                }

                var primaryText = fact.text?.Trim();
                if (!string.IsNullOrWhiteSpace(primaryText)
                    && !normalizedVariants.Contains(primaryText, StringComparer.Ordinal))
                {
                    errors.Add($"Fact '{factId}' variant pool must include its primary text.");
                }

                if (normalizedVariants.Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                    hasRotatingPool = true;
            }

            if (!hasRotatingPool)
                errors.Add("Alibi must define at least one rotating variant pool.");

            if (maxHiddenFacts > 0
                && !alibiFacts.Any(fact => fact != null && fact.canBeHidden && !fact.distinctiveDetail))
            {
                errors.Add("Alibi must have a hideable non-distinctive fact so a charakterystycznyDetal is never the only hidden fact.");
            }
        }

        private static string ResolveFactId(AuthoredFact fact, int index) =>
            string.IsNullOrWhiteSpace(fact.id) ? $"fact-{index}" : fact.id.Trim();

        private void OnValidate()
        {
            var errors = Validate();
            foreach (var error in errors)
                Debug.LogError($"CaseAsset '{name}': {error}", this);
        }
    }
}
