using System;

namespace InterrogationRoom.Domain
{
    /// <summary>How an Incydent reaches the Detektyw's private Rejestr.</summary>
    public enum IncidentKind
    {
        /// <summary>Reported immediately when the world effect is accepted.</summary>
        Loud,

        /// <summary>Reported only after the Detektyw personally discovers it.</summary>
        Quiet
    }

    /// <summary>Stable identity of one accepted Incydent world effect.</summary>
    public readonly struct IncidentId : IEquatable<IncidentId>
    {
        public string Value { get; }

        public IncidentId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Incident id cannot be empty.", nameof(value));

            Value = value;
        }

        public bool Equals(IncidentId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is IncidentId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(IncidentId left, IncidentId right) => left.Equals(right);
        public static bool operator !=(IncidentId left, IncidentId right) => !left.Equals(right);
    }

    /// <summary>Stable authored identity of the visible world effect.</summary>
    public readonly struct IncidentEffectId : IEquatable<IncidentEffectId>
    {
        public string Value { get; }

        public IncidentEffectId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Incident effect id cannot be empty.", nameof(value));

            Value = value;
        }

        public bool Equals(IncidentEffectId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is IncidentEffectId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(IncidentEffectId left, IncidentEffectId right) => left.Equals(right);
        public static bool operator !=(IncidentEffectId left, IncidentEffectId right) => !left.Equals(right);
    }

    /// <summary>Stable authored identity of an Incydent location.</summary>
    public readonly struct IncidentLocationId : IEquatable<IncidentLocationId>
    {
        public string Value { get; }

        public IncidentLocationId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Incident location id cannot be empty.", nameof(value));

            Value = value;
        }

        public bool Equals(IncidentLocationId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is IncidentLocationId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(IncidentLocationId left, IncidentLocationId right) => left.Equals(right);
        public static bool operator !=(IncidentLocationId left, IncidentLocationId right) => !left.Equals(right);
    }

    /// <summary>
    /// Adapter-supplied monotonic time since the Runda began. The pure domain
    /// never reads a clock.
    /// </summary>
    public readonly struct IncidentTimestamp : IEquatable<IncidentTimestamp>, IComparable<IncidentTimestamp>
    {
        public long MillisecondsSinceRoundStart { get; }

        public IncidentTimestamp(long millisecondsSinceRoundStart)
        {
            if (millisecondsSinceRoundStart < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(millisecondsSinceRoundStart),
                    "Incident timestamp cannot be negative.");

            MillisecondsSinceRoundStart = millisecondsSinceRoundStart;
        }

        public int CompareTo(IncidentTimestamp other) =>
            MillisecondsSinceRoundStart.CompareTo(other.MillisecondsSinceRoundStart);

        public bool Equals(IncidentTimestamp other) =>
            MillisecondsSinceRoundStart == other.MillisecondsSinceRoundStart;

        public override bool Equals(object obj) => obj is IncidentTimestamp other && Equals(other);
        public override int GetHashCode() => MillisecondsSinceRoundStart.GetHashCode();
        public override string ToString() => MillisecondsSinceRoundStart.ToString();
        public static bool operator ==(IncidentTimestamp left, IncidentTimestamp right) => left.Equals(right);
        public static bool operator !=(IncidentTimestamp left, IncidentTimestamp right) => !left.Equals(right);
        public static bool operator <(IncidentTimestamp left, IncidentTimestamp right) => left.CompareTo(right) < 0;
        public static bool operator >(IncidentTimestamp left, IncidentTimestamp right) => left.CompareTo(right) > 0;
    }

    /// <summary>
    /// Optional explicit link from a physical action to one assigned Prywatny
    /// Cel step. A foreign or stale link never advances progress.
    /// </summary>
    public sealed class PrivateObjectiveStepReference
    {
        public PrivateObjectiveId ObjectiveId { get; }
        public PrivateObjectiveStepId StepId { get; }

        public PrivateObjectiveStepReference(
            PrivateObjectiveId objectiveId,
            PrivateObjectiveStepId stepId)
        {
            ObjectiveId = objectiveId;
            StepId = stepId;
        }
    }

    /// <summary>
    /// One Detektyw-only Rejestr entry. It deliberately excludes the author,
    /// their role, motive and the quiet action's original time.
    /// </summary>
    public sealed class IncidentRegistryEntryView
    {
        public IncidentId Id { get; }
        public IncidentKind Kind { get; }
        public IncidentEffectId Effect { get; }
        public IncidentLocationId Location { get; }
        public IncidentTimestamp ReportedAt { get; }

        public IncidentRegistryEntryView(
            IncidentId id,
            IncidentKind kind,
            IncidentEffectId effect,
            IncidentLocationId location,
            IncidentTimestamp reportedAt)
        {
            Id = id;
            Kind = kind;
            Effect = effect;
            Location = location;
            ReportedAt = reportedAt;
        }
    }

    /// <summary>Post-Runda reveal of the host-owned Incydent author.</summary>
    public sealed class IncidentRevealView
    {
        public IncidentId Id { get; }
        public IncidentKind Kind { get; }
        public IncidentEffectId Effect { get; }
        public IncidentLocationId Location { get; }
        public PlayerId Author { get; }

        public IncidentRevealView(
            IncidentId id,
            IncidentKind kind,
            IncidentEffectId effect,
            IncidentLocationId location,
            PlayerId author)
        {
            Id = id;
            Kind = kind;
            Effect = effect;
            Location = location;
            Author = author;
        }
    }
}
