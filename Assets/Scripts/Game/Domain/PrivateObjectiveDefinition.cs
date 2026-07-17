using System;
using System.Collections.Generic;
using System.Linq;

namespace InterrogationRoom.Domain
{
    public enum PrivateObjectiveKind
    {
        PersonalMatter,
        SecretObjective
    }

    /// <summary>Stable identifier reported by physical interactions without resolving rules.</summary>
    public readonly struct PrivateObjectiveId : IEquatable<PrivateObjectiveId>
    {
        public string Value { get; }

        public PrivateObjectiveId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Prywatny Cel id cannot be empty.", nameof(value));
            Value = value;
        }

        public bool Equals(PrivateObjectiveId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PrivateObjectiveId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(PrivateObjectiveId left, PrivateObjectiveId right) => left.Equals(right);
        public static bool operator !=(PrivateObjectiveId left, PrivateObjectiveId right) => !left.Equals(right);
    }

    /// <summary>Stable identifier of one sequential step within a Prywatny Cel.</summary>
    public readonly struct PrivateObjectiveStepId : IEquatable<PrivateObjectiveStepId>
    {
        public string Value { get; }

        public PrivateObjectiveStepId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Prywatny Cel step id cannot be empty.", nameof(value));
            Value = value;
        }

        public bool Equals(PrivateObjectiveStepId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PrivateObjectiveStepId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(PrivateObjectiveStepId left, PrivateObjectiveStepId right) => left.Equals(right);
        public static bool operator !=(PrivateObjectiveStepId left, PrivateObjectiveStepId right) => !left.Equals(right);
    }

    public sealed class PrivateObjectiveStepDefinition
    {
        public PrivateObjectiveStepId Id { get; }
        public PrivateObjectiveStepId AnchorActionId { get; }
        public string Description { get; }
        public string LocationHint { get; }
        public bool CreatesIncident { get; }

        public PrivateObjectiveStepDefinition(PrivateObjectiveStepId id)
            : this(id, id, id.Value, "posterunek")
        {
        }

        public PrivateObjectiveStepDefinition(
            PrivateObjectiveStepId id,
            PrivateObjectiveStepId anchorActionId,
            string description,
            string locationHint,
            bool createsIncident = false)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Prywatny Cel step description cannot be empty.", nameof(description));
            if (string.IsNullOrWhiteSpace(locationHint))
                throw new ArgumentException("Prywatny Cel step location hint cannot be empty.", nameof(locationHint));

            Id = id;
            AnchorActionId = anchorActionId;
            Description = description.Trim();
            LocationHint = locationHint.Trim();
            CreatesIncident = createsIncident;
        }
    }

    /// <summary>Immutable, Unity-free definition of one sequential Prywatny Cel.</summary>
    public class PrivateObjectiveDefinition
    {
        public PrivateObjectiveId Id { get; }
        public PrivateObjectiveKind Kind { get; }
        public string Title { get; }
        public string Motive { get; }
        public IReadOnlyList<PrivateObjectiveStepDefinition> Steps { get; }
        public IReadOnlyList<string> ReservedItemIds { get; }

        public PrivateObjectiveDefinition(
            PrivateObjectiveId id,
            PrivateObjectiveKind kind,
            IEnumerable<PrivateObjectiveStepDefinition> steps)
            : this(id, kind, id.Value, id.Value, steps)
        {
        }

        public PrivateObjectiveDefinition(
            PrivateObjectiveId id,
            PrivateObjectiveKind kind,
            string title,
            string motive,
            IEnumerable<PrivateObjectiveStepDefinition> steps,
            IEnumerable<string> reservedItemIds = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Prywatny Cel title cannot be empty.", nameof(title));
            if (string.IsNullOrWhiteSpace(motive))
                throw new ArgumentException("Prywatny Cel motive cannot be empty.", nameof(motive));
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            var copiedSteps = steps.ToArray();
            if (copiedSteps.Length == 0)
                throw new ArgumentException("Prywatny Cel requires at least one step.", nameof(steps));
            if (copiedSteps.Any(step => step == null))
                throw new ArgumentException("Prywatny Cel contains a null step.", nameof(steps));
            if (copiedSteps.Select(step => step.Id).Distinct().Count() != copiedSteps.Length)
                throw new ArgumentException("Prywatny Cel contains duplicate step ids.", nameof(steps));
            if (kind == PrivateObjectiveKind.SecretObjective && copiedSteps.Length != 2)
                throw new ArgumentException("Sekretny Cel requires exactly two Wrobienie steps.", nameof(steps));

            var copiedReservedItemIds = (reservedItemIds ?? Array.Empty<string>())
                .Select(value => value?.Trim())
                .ToArray();
            if (copiedReservedItemIds.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Reserved item ids cannot contain empty values.", nameof(reservedItemIds));
            if (copiedReservedItemIds.Distinct().Count() != copiedReservedItemIds.Length)
                throw new ArgumentException("Reserved item ids must be unique.", nameof(reservedItemIds));

            Id = id;
            Kind = kind;
            Title = title.Trim();
            Motive = motive.Trim();
            Steps = Array.AsReadOnly(copiedSteps);
            ReservedItemIds = Array.AsReadOnly(copiedReservedItemIds);
        }
    }

    public sealed class PersonalMatterDefinition : PrivateObjectiveDefinition
    {
        public PersonalMatterDefinition(
            PrivateObjectiveId id,
            string title,
            string motive,
            IEnumerable<PrivateObjectiveStepDefinition> steps,
            IEnumerable<string> reservedItemIds = null)
            : base(
                id,
                PrivateObjectiveKind.PersonalMatter,
                title,
                motive,
                steps,
                reservedItemIds)
        {
            if (Steps.Count < 2 || Steps.Count > 3)
                throw new ArgumentException("Osobista Sprawa requires two or three sequential steps.", nameof(steps));
        }
    }

    public static class PrivateObjectiveDefinitions
    {
        public static readonly PersonalMatterDefinition PersonalMatter = new PersonalMatterDefinition(
            new PrivateObjectiveId("osobista-sprawa"),
            "Odzyskaj swoją rzecz",
            "W depozycie została rzecz, której nie chcesz zostawiać w policyjnych rękach.",
            new[]
            {
                new PrivateObjectiveStepDefinition(
                    new PrivateObjectiveStepId("osobista-sprawa-przygotuj"),
                    new PrivateObjectiveStepId("osobista-sprawa-przygotuj"),
                    "Znajdź informację lub przedmiot potrzebny do otwarcia właściwego depozytu.",
                    "magazyn dowodów albo archiwum"),
                new PrivateObjectiveStepDefinition(
                    new PrivateObjectiveStepId("osobista-sprawa-zakoncz"),
                    new PrivateObjectiveStepId("osobista-sprawa-zakoncz"),
                    "Otwórz wskazaną skrytkę i odzyskaj swoją rzecz.",
                    "szafki depozytowe albo archiwalna skrytka",
                    createsIncident: true)
            });

        public static readonly PrivateObjectiveDefinition SecretObjective = WrobienieDefinitions.Variants[0];
    }

    public static class WrobienieDefinitions
    {
        private static readonly PrivateObjectiveStepId AcquireAnchor =
            new PrivateObjectiveStepId("wrobienie-przygotuj");
        private static readonly PrivateObjectiveStepId PlantAnchor =
            new PrivateObjectiveStepId("wrobienie-podloz");

        public static readonly IReadOnlyList<PrivateObjectiveDefinition> Variants =
            Array.AsReadOnly(new[]
            {
                Create(
                    "WR-01",
                    "Podrzucone papierosy",
                    "Chcesz skierować podejrzenia na Cel, pozostawiając przy nim rzecz z policyjnego depozytu.",
                    "Zabierz paczkę papierosów albo zapalniczkę z depozytu.",
                    "magazyn dowodów — depozyt drobnych przedmiotów",
                    "Podłóż przedmiot w rzeczach osobistych Celu.",
                    "szafki osobiste — skrytka przypisana do Celu",
                    "papierosy-depozytowe"),
                Create(
                    "WR-02",
                    "Klucz przy Celu",
                    "Brakujący klucz znaleziony przy Celu ma stworzyć miękki trop, ale nie dowód autora.",
                    "Zabierz brakujący klucz z listwy.",
                    "magazyn dowodów — listwa kluczy",
                    "Umieść klucz w kopercie albo szafce Celu.",
                    "depozyt osobisty — przegródka Celu",
                    "klucz-z-listwy"),
                Create(
                    "WR-03",
                    "Podmieniony protokół",
                    "Manipulowany dokument ma zasugerować związek Celu ze sprawą, pozostawiając widoczne ślady podmiany.",
                    "Przygotuj podmienioną stronę protokołu.",
                    "archiwum — stanowisko dokumentów",
                    "Włóż podmienioną stronę do teczki Celu.",
                    "archiwum — teczki osobowe",
                    "strona-protokolu"),
                Create(
                    "WR-04",
                    "Cudza wiadomość",
                    "Cudza rzecz pozostawiona przy depozycie Celu ma wywołać pytania o to, kto ją przeniósł.",
                    "Zabierz telefon albo kartę wiadomości z depozytu.",
                    "magazyn dowodów — elektronika i wiadomości",
                    "Zostaw rzecz przy depozycie Celu.",
                    "szafki depozytowe — numer Celu",
                    "telefon-lub-karta-wiadomosci")
            });

        private static PrivateObjectiveDefinition Create(
            string id,
            string title,
            string motive,
            string acquireDescription,
            string acquireLocation,
            string plantDescription,
            string plantLocation,
            string reservedItemId) =>
            new PrivateObjectiveDefinition(
                new PrivateObjectiveId(id),
                PrivateObjectiveKind.SecretObjective,
                title,
                motive,
                new[]
                {
                    new PrivateObjectiveStepDefinition(
                        new PrivateObjectiveStepId($"{id}-zdobadz"),
                        AcquireAnchor,
                        acquireDescription,
                        acquireLocation,
                        createsIncident: true),
                    new PrivateObjectiveStepDefinition(
                        new PrivateObjectiveStepId($"{id}-podloz"),
                        PlantAnchor,
                        plantDescription,
                        plantLocation,
                        createsIncident: true)
                },
                new[] { reservedItemId });
    }
}
