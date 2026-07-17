using System;
using System.Collections.Generic;
using System.Linq;

namespace InterrogationRoom.Domain
{
    public readonly struct EscapePlanId : IEquatable<EscapePlanId>
    {
        public string Value { get; }

        public EscapePlanId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Escape plan id cannot be empty.", nameof(value));
            Value = value;
        }

        public bool Equals(EscapePlanId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EscapePlanId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(EscapePlanId left, EscapePlanId right) => left.Equals(right);
        public static bool operator !=(EscapePlanId left, EscapePlanId right) => !left.Equals(right);
    }

    public readonly struct EscapeStepId : IEquatable<EscapeStepId>
    {
        public string Value { get; }

        public EscapeStepId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Escape step id cannot be empty.", nameof(value));
            Value = value;
        }

        public bool Equals(EscapeStepId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EscapeStepId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(EscapeStepId left, EscapeStepId right) => left.Equals(right);
        public static bool operator !=(EscapeStepId left, EscapeStepId right) => !left.Equals(right);
    }

    public readonly struct EscapeExitId : IEquatable<EscapeExitId>
    {
        public string Value { get; }

        public EscapeExitId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Escape exit id cannot be empty.", nameof(value));
            Value = value;
        }

        public bool Equals(EscapeExitId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EscapeExitId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(EscapeExitId left, EscapeExitId right) => left.Equals(right);
        public static bool operator !=(EscapeExitId left, EscapeExitId right) => !left.Equals(right);
    }

    public sealed class EscapeStepDefinition
    {
        public EscapeStepId Id { get; }
        public string Description { get; }
        public string LocationHint { get; }

        public EscapeStepDefinition(EscapeStepId id)
            : this(id, id.Value, "posterunek")
        {
        }

        public EscapeStepDefinition(EscapeStepId id, string description, string locationHint)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Escape step description cannot be empty.", nameof(description));
            if (string.IsNullOrWhiteSpace(locationHint))
                throw new ArgumentException("Escape step location hint cannot be empty.", nameof(locationHint));

            Id = id;
            Description = description.Trim();
            LocationHint = locationHint.Trim();
        }
    }

    /// <summary>One compatible final point and the step that prepares it.</summary>
    public sealed class EscapeExitDefinition
    {
        public EscapeExitId Id { get; }
        public EscapeStepId PreparationStepId { get; }
        public IncidentLocationId Location { get; }
        public string Description { get; }
        public string LocationHint { get; }

        public EscapeExitDefinition(
            EscapeExitId id,
            EscapeStepId preparationStepId,
            IncidentLocationId location)
            : this(id, preparationStepId, location, preparationStepId.Value, location.Value)
        {
        }

        public EscapeExitDefinition(
            EscapeExitId id,
            EscapeStepId preparationStepId,
            IncidentLocationId location,
            string description,
            string locationHint)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Escape exit description cannot be empty.", nameof(description));
            if (string.IsNullOrWhiteSpace(locationHint))
                throw new ArgumentException("Escape exit location hint cannot be empty.", nameof(locationHint));

            Id = id;
            PreparationStepId = preparationStepId;
            Location = location;
            Description = description.Trim();
            LocationHint = locationHint.Trim();
        }
    }

    /// <summary>
    /// Immutable map-authored Plan Ucieczki contract: sequential common work,
    /// then at least two compatible final points prepared independently.
    /// </summary>
    public sealed class EscapePlanDefinition
    {
        public EscapePlanId Id { get; }
        public string Title { get; }
        public string Motive { get; }
        public IReadOnlyList<EscapeStepDefinition> CommonSteps { get; }
        public IReadOnlyList<EscapeExitDefinition> Exits { get; }

        public EscapePlanDefinition(
            EscapePlanId id,
            IEnumerable<EscapeStepDefinition> commonSteps,
            IEnumerable<EscapeExitDefinition> exits)
            : this(id, id.Value, id.Value, commonSteps, exits)
        {
        }

        public EscapePlanDefinition(
            EscapePlanId id,
            string title,
            string motive,
            IEnumerable<EscapeStepDefinition> commonSteps,
            IEnumerable<EscapeExitDefinition> exits)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Escape plan title cannot be empty.", nameof(title));
            if (string.IsNullOrWhiteSpace(motive))
                throw new ArgumentException("Escape plan motive cannot be empty.", nameof(motive));
            if (commonSteps == null) throw new ArgumentNullException(nameof(commonSteps));
            if (exits == null) throw new ArgumentNullException(nameof(exits));
            var copiedSteps = commonSteps.ToArray();
            var copiedExits = exits.ToArray();
            if (copiedSteps.Length == 0)
                throw new ArgumentException("Escape plan requires a common preparation step.", nameof(commonSteps));
            if (copiedSteps.Any(step => step == null)
                || copiedSteps.Select(step => step.Id).Distinct().Count() != copiedSteps.Length)
                throw new ArgumentException("Escape plan common steps must be non-null and unique.", nameof(commonSteps));
            if (copiedExits.Length < 2)
                throw new ArgumentException("Escape plan requires at least two compatible exits.", nameof(exits));
            if (copiedExits.Any(exit => exit == null)
                || copiedExits.Select(exit => exit.Id).Distinct().Count() != copiedExits.Length
                || copiedExits.Select(exit => exit.PreparationStepId).Distinct().Count() != copiedExits.Length)
                throw new ArgumentException("Escape exits and their preparation steps must be unique.", nameof(exits));
            if (copiedExits.Any(exit => copiedSteps.Any(step => step.Id == exit.PreparationStepId)))
                throw new ArgumentException("Exit preparation ids cannot duplicate common step ids.", nameof(exits));

            Id = id;
            Title = title.Trim();
            Motive = motive.Trim();
            CommonSteps = Array.AsReadOnly(copiedSteps);
            Exits = Array.AsReadOnly(copiedExits);
        }
    }

    public static class EscapePlanDefinitions
    {
        public static readonly IncidentEffectId FinalEffect =
            new IncidentEffectId("escape-final-alarm");

        public static readonly EscapePlanDefinition Prototype = Create(
            "Klucz i dokumenty służbowe",
            "Zdobądź klucz oraz dokument potrzebny do przygotowania jednej z dwóch dróg wyjścia.",
            "Zabierz właściwy klucz z listwy.",
            "magazyn dowodów — listwa kluczy",
            "Wyjmij dokument z archiwum, aby ustalić bezpieczną trasę.",
            "archiwum — szafa dokumentów służbowych",
            "Przygotuj tylne wyjście i pozostaw je gotowe do głośnej próby Ucieczki.",
            "zaplecze posterunku — tylne wyjście",
            "Przygotuj wyjście techniczne i pozostaw je gotowe do głośnej próby Ucieczki.",
            "korytarz techniczny — punkt końcowy");

        public static readonly EscapePlanDefinition DepositRoute = Create(
            "Depozyt i droga serwisowa",
            "Sprawdź depozyt, zdobądź narzędzie i przygotuj jedną z dróg serwisowych.",
            "Przeszukaj oznaczoną szafkę depozytową i znajdź małe narzędzie.",
            "magazyn dowodów — szafki depozytowe",
            "Użyj narzędzia przy zamknięciu drogi serwisowej.",
            "zaplecze — przejście serwisowe",
            "Przygotuj drzwi na dziedziniec do finałowej próby Ucieczki.",
            "dziedziniec — drzwi od zaplecza",
            "Przygotuj wyjście przez garaż do finałowej próby Ucieczki.",
            "garaż — brama serwisowa");

        public static readonly EscapePlanDefinition TelephoneRoute = Create(
            "Telefon i zmiana posterunku",
            "Zdobądź informacje o obsadzie posterunku, a następnie przygotuj wybrane wyjście.",
            "Znajdź kartę z numerem służbowym potrzebnym do wykonania telefonu.",
            "archiwum — książka kontaktów",
            "Użyj telefonu stacjonarnego i sprawdź, która droga pozostanie bez nadzoru.",
            "biuro dyżurnego — telefon stacjonarny",
            "Przygotuj wyjście od strony dziedzińca.",
            "dziedziniec — punkt końcowy",
            "Przygotuj wyjście od strony garażu.",
            "garaż — punkt końcowy");

        public static readonly IReadOnlyList<EscapePlanDefinition> AuthoredPlans =
            Array.AsReadOnly(new[] { Prototype, DepositRoute, TelephoneRoute });

        private static EscapePlanDefinition Create(
            string title,
            string motive,
            string firstDescription,
            string firstLocation,
            string secondDescription,
            string secondLocation,
            string exitADescription,
            string exitALocation,
            string exitBDescription,
            string exitBLocation) =>
            new EscapePlanDefinition(
                new EscapePlanId("escape-prototype"),
                title,
                motive,
                new[]
                {
                    new EscapeStepDefinition(
                        new EscapeStepId("escape-find-tool"),
                        firstDescription,
                        firstLocation),
                    new EscapeStepDefinition(
                        new EscapeStepId("escape-open-route"),
                        secondDescription,
                        secondLocation)
                },
                new[]
                {
                    new EscapeExitDefinition(
                        new EscapeExitId("escape-exit-a"),
                        new EscapeStepId("escape-prepare-exit-a"),
                        new IncidentLocationId("escape-exit-a"),
                        exitADescription,
                        exitALocation),
                    new EscapeExitDefinition(
                        new EscapeExitId("escape-exit-b"),
                        new EscapeStepId("escape-prepare-exit-b"),
                        new IncidentLocationId("escape-exit-b"),
                        exitBDescription,
                        exitBLocation)
                });
    }

    public sealed class EscapeExitOptionView
    {
        public EscapeExitId Id { get; }
        public EscapeStepId PreparationStepId { get; }
        public IncidentLocationId Location { get; }
        public string Description { get; }
        public string LocationHint { get; }
        public bool IsPrepared { get; }

        public EscapeExitOptionView(
            EscapeExitId id,
            EscapeStepId preparationStepId,
            IncidentLocationId location,
            bool isPrepared)
            : this(id, preparationStepId, location, preparationStepId.Value, location.Value, isPrepared)
        {
        }

        public EscapeExitOptionView(
            EscapeExitId id,
            EscapeStepId preparationStepId,
            IncidentLocationId location,
            string description,
            string locationHint,
            bool isPrepared)
        {
            Id = id;
            PreparationStepId = preparationStepId;
            Location = location;
            Description = description;
            LocationHint = locationHint;
            IsPrepared = isPrepared;
        }
    }

    public sealed class EscapePlanView
    {
        public EscapePlanId Id { get; }
        public string Title { get; }
        public string Motive { get; }
        public EscapeStepId? CurrentStep { get; }
        public string CurrentStepDescription { get; }
        public string CurrentStepLocationHint { get; }
        public int CompletedCommonStepCount { get; }
        public int TotalCommonStepCount { get; }
        public bool IsPrepared { get; }
        public EscapeExitId? ActiveExit { get; }
        public IReadOnlyList<EscapeExitOptionView> ExitOptions { get; }

        public EscapePlanView(
            EscapePlanId id,
            EscapeStepId? currentStep,
            int completedCommonStepCount,
            int totalCommonStepCount,
            bool isPrepared,
            EscapeExitId? activeExit,
            IReadOnlyList<EscapeExitOptionView> exitOptions)
            : this(
                id,
                id.Value,
                id.Value,
                currentStep,
                currentStep?.Value,
                null,
                completedCommonStepCount,
                totalCommonStepCount,
                isPrepared,
                activeExit,
                exitOptions)
        {
        }

        public EscapePlanView(
            EscapePlanId id,
            string title,
            string motive,
            EscapeStepId? currentStep,
            string currentStepDescription,
            string currentStepLocationHint,
            int completedCommonStepCount,
            int totalCommonStepCount,
            bool isPrepared,
            EscapeExitId? activeExit,
            IReadOnlyList<EscapeExitOptionView> exitOptions)
        {
            Id = id;
            Title = title;
            Motive = motive;
            CurrentStep = currentStep;
            CurrentStepDescription = currentStepDescription;
            CurrentStepLocationHint = currentStepLocationHint;
            CompletedCommonStepCount = completedCommonStepCount;
            TotalCommonStepCount = totalCommonStepCount;
            IsPrepared = isPrepared;
            ActiveExit = activeExit;
            ExitOptions = exitOptions ?? throw new ArgumentNullException(nameof(exitOptions));
        }
    }

    public enum EscapeActionKind
    {
        PreparedCommonStep,
        PreparedExit,
        AttemptStarted,
        AttemptInterrupted,
        Completed
    }

    public sealed class EscapeActionRevealView
    {
        public EscapeActionKind Kind { get; }
        public EscapeStepId? StepId { get; }
        public EscapeExitId? ExitId { get; }

        public EscapeActionRevealView(
            EscapeActionKind kind,
            EscapeStepId? stepId = null,
            EscapeExitId? exitId = null)
        {
            Kind = kind;
            StepId = stepId;
            ExitId = exitId;
        }
    }

    public sealed class EscapePlanRevealView
    {
        public EscapePlanId Id { get; }
        public IReadOnlyList<EscapeActionRevealView> Actions { get; }
        public EscapeExitId? SuccessfulExit { get; }

        public EscapePlanRevealView(
            EscapePlanId id,
            IReadOnlyList<EscapeActionRevealView> actions,
            EscapeExitId? successfulExit)
        {
            Id = id;
            Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            SuccessfulExit = successfulExit;
        }
    }
}
