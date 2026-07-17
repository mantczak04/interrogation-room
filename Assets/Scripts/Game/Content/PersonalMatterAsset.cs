using System;
using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Domain;
using UnityEngine;

namespace InterrogationRoom.Content
{
    [CreateAssetMenu(
        menuName = "Interrogation Room/Osobista Sprawa",
        fileName = "NewPersonalMatter")]
    public sealed class PersonalMatterAsset : ScriptableObject
    {
        [Tooltip("Stabilne id modułu, np. OS-01.")]
        public string stableId;

        [Tooltip("Krótki tytuł Prywatnego Celu pokazywany właścicielowi.")]
        public string title;

        [Tooltip("Fabularny powód działania — czytelny motyw właściciela.")]
        [TextArea(2, 4)]
        public string motive;

        [Tooltip("Dwa lub trzy sekwencyjne kroki Osobistej Sprawy.")]
        public List<AuthoredStep> steps = new List<AuthoredStep>();

        [Tooltip("Id istotnych przedmiotów rezerwowanych na przyszłe sprawdzanie konfliktów.")]
        public List<string> reservedItemIds = new List<string>();

        [Serializable]
        public sealed class AuthoredStep
        {
            [Tooltip("Stabilne id kroku w authoringu.")]
            public string stableStepId;

            [Tooltip("Instrukcja dla gracza: czego szuka i co ma zrobić.")]
            [TextArea(1, 3)]
            public string description;

            [Tooltip("Użyteczna wskazówka miejsca bez dokładnego markera.")]
            public string locationHint;

            [Tooltip("Id completionPayloadId zgłaszane przez istniejący world anchor.")]
            public string anchorActionId;

            [Tooltip("Krok powinien pozostawić trwały Incydent lub czytelny stan świata.")]
            public bool createsIncident;
        }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(stableId))
                errors.Add("Stable id is empty.");
            if (string.IsNullOrWhiteSpace(title))
                errors.Add("Title is empty.");
            if (string.IsNullOrWhiteSpace(motive))
                errors.Add("Motive is empty.");

            if (steps == null || steps.Count < 2 || steps.Count > 3)
            {
                errors.Add($"Osobista Sprawa requires 2-3 steps, got {steps?.Count ?? 0}.");
            }
            else
            {
                if (steps.Any(step => step == null
                    || string.IsNullOrWhiteSpace(step.stableStepId)
                    || string.IsNullOrWhiteSpace(step.description)
                    || string.IsNullOrWhiteSpace(step.locationHint)
                    || string.IsNullOrWhiteSpace(step.anchorActionId)))
                {
                    errors.Add("Every step requires stable id, description, location hint and anchor action id.");
                }

                var validStepIds = steps
                    .Where(step => step != null && !string.IsNullOrWhiteSpace(step.stableStepId))
                    .Select(step => step.stableStepId.Trim())
                    .ToArray();
                if (validStepIds.Distinct().Count() != validStepIds.Length)
                    errors.Add("Step stable ids must be unique within one Osobista Sprawa.");
            }

            var items = reservedItemIds ?? new List<string>();
            if (items.Any(string.IsNullOrWhiteSpace))
                errors.Add("Reserved item ids cannot contain empty values.");
            var validItems = items.Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .ToArray();
            if (validItems.Distinct().Count() != validItems.Length)
                errors.Add("Reserved item ids must be unique.");

            return errors;
        }

        public PersonalMatterDefinition ToDefinition()
        {
            var errors = Validate();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"PersonalMatterAsset '{name}' is invalid: {string.Join(" | ", errors)}");
            }

            return new PersonalMatterDefinition(
                new PrivateObjectiveId(stableId.Trim()),
                title.Trim(),
                motive.Trim(),
                steps.Select(step => new PrivateObjectiveStepDefinition(
                    new PrivateObjectiveStepId(step.stableStepId.Trim()),
                    new PrivateObjectiveStepId(step.anchorActionId.Trim()),
                    step.description.Trim(),
                    step.locationHint.Trim(),
                    step.createsIncident)),
                (reservedItemIds ?? new List<string>()).Select(item => item.Trim()));
        }

        private void OnValidate()
        {
            foreach (var error in Validate())
                Debug.LogError($"PersonalMatterAsset '{name}': {error}", this);
        }
    }
}
