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

        public PrivateObjectiveStepDefinition(PrivateObjectiveStepId id)
        {
            Id = id;
        }
    }

    /// <summary>Immutable, Unity-free definition of one sequential Prywatny Cel.</summary>
    public sealed class PrivateObjectiveDefinition
    {
        public PrivateObjectiveId Id { get; }
        public PrivateObjectiveKind Kind { get; }
        public IReadOnlyList<PrivateObjectiveStepDefinition> Steps { get; }

        public PrivateObjectiveDefinition(
            PrivateObjectiveId id,
            PrivateObjectiveKind kind,
            IEnumerable<PrivateObjectiveStepDefinition> steps)
        {
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

            Id = id;
            Kind = kind;
            Steps = Array.AsReadOnly(copiedSteps);
        }
    }

    /// <summary>
    /// Stable A1 contracts for physical gameplay. Later authored variants may
    /// replace these definitions without changing the ids reported to Handle.
    /// </summary>
    public static class PrivateObjectiveDefinitions
    {
        public static readonly PrivateObjectiveDefinition PersonalMatter = new PrivateObjectiveDefinition(
            new PrivateObjectiveId("osobista-sprawa"),
            PrivateObjectiveKind.PersonalMatter,
            new[]
            {
                new PrivateObjectiveStepDefinition(new PrivateObjectiveStepId("osobista-sprawa-przygotuj")),
                new PrivateObjectiveStepDefinition(new PrivateObjectiveStepId("osobista-sprawa-zakoncz"))
            });

        public static readonly PrivateObjectiveDefinition SecretObjective = new PrivateObjectiveDefinition(
            new PrivateObjectiveId("sekretny-cel"),
            PrivateObjectiveKind.SecretObjective,
            new[]
            {
                new PrivateObjectiveStepDefinition(new PrivateObjectiveStepId("wrobienie-przygotuj")),
                new PrivateObjectiveStepDefinition(new PrivateObjectiveStepId("wrobienie-podloz"))
            });
    }
}
