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

        public EscapeStepDefinition(EscapeStepId id)
        {
            Id = id;
        }
    }

    /// <summary>One compatible final point and the step that prepares it.</summary>
    public sealed class EscapeExitDefinition
    {
        public EscapeExitId Id { get; }
        public EscapeStepId PreparationStepId { get; }
        public IncidentLocationId Location { get; }

        public EscapeExitDefinition(
            EscapeExitId id,
            EscapeStepId preparationStepId,
            IncidentLocationId location)
        {
            Id = id;
            PreparationStepId = preparationStepId;
            Location = location;
        }
    }

    /// <summary>
    /// Immutable map-authored Plan Ucieczki contract: sequential common work,
    /// then at least two compatible final points prepared independently.
    /// </summary>
    public sealed class EscapePlanDefinition
    {
        public EscapePlanId Id { get; }
        public IReadOnlyList<EscapeStepDefinition> CommonSteps { get; }
        public IReadOnlyList<EscapeExitDefinition> Exits { get; }

        public EscapePlanDefinition(
            EscapePlanId id,
            IEnumerable<EscapeStepDefinition> commonSteps,
            IEnumerable<EscapeExitDefinition> exits)
        {
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
            CommonSteps = Array.AsReadOnly(copiedSteps);
            Exits = Array.AsReadOnly(copiedExits);
        }
    }

    /// <summary>Stable first-prototype contract handed to physical gameplay.</summary>
    public static class EscapePlanDefinitions
    {
        public static readonly IncidentEffectId FinalEffect =
            new IncidentEffectId("escape-final-alarm");

        public static readonly EscapePlanDefinition Prototype = new EscapePlanDefinition(
            new EscapePlanId("escape-prototype"),
            new[]
            {
                new EscapeStepDefinition(new EscapeStepId("escape-find-tool")),
                new EscapeStepDefinition(new EscapeStepId("escape-open-route"))
            },
            new[]
            {
                new EscapeExitDefinition(
                    new EscapeExitId("escape-exit-a"),
                    new EscapeStepId("escape-prepare-exit-a"),
                    new IncidentLocationId("escape-exit-a")),
                new EscapeExitDefinition(
                    new EscapeExitId("escape-exit-b"),
                    new EscapeStepId("escape-prepare-exit-b"),
                    new IncidentLocationId("escape-exit-b"))
            });
    }

    public sealed class EscapeExitOptionView
    {
        public EscapeExitId Id { get; }
        public EscapeStepId PreparationStepId { get; }
        public IncidentLocationId Location { get; }
        public bool IsPrepared { get; }

        public EscapeExitOptionView(
            EscapeExitId id,
            EscapeStepId preparationStepId,
            IncidentLocationId location,
            bool isPrepared)
        {
            Id = id;
            PreparationStepId = preparationStepId;
            Location = location;
            IsPrepared = isPrepared;
        }
    }

    public sealed class EscapePlanView
    {
        public EscapePlanId Id { get; }
        public EscapeStepId? CurrentStep { get; }
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
        {
            Id = id;
            CurrentStep = currentStep;
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
