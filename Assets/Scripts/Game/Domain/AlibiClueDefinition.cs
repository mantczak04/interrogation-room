using System;

namespace InterrogationRoom.Domain
{
    /// <summary>Stable authored identity of one Trop do Alibi.</summary>
    public readonly struct AlibiClueId : IEquatable<AlibiClueId>
    {
        public string Value { get; }

        public AlibiClueId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Alibi clue id cannot be empty.", nameof(value));

            Value = value;
        }

        public bool Equals(AlibiClueId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is AlibiClueId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(AlibiClueId left, AlibiClueId right) => left.Equals(right);
        public static bool operator !=(AlibiClueId left, AlibiClueId right) => !left.Equals(right);
    }

    /// <summary>
    /// Immutable authored Trop tied to one hideable fact. Content is an
    /// interpretive fragment, never the Alibi fact itself.
    /// </summary>
    public sealed class AlibiClueDefinition
    {
        public AlibiClueId Id { get; }
        public string LinkedFactId { get; }
        public string Content { get; }

        public AlibiClueDefinition(AlibiClueId id, string linkedFactId, string content)
        {
            if (string.IsNullOrWhiteSpace(linkedFactId))
                throw new ArgumentException("Linked fact id cannot be empty.", nameof(linkedFactId));
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Alibi clue content cannot be empty.", nameof(content));

            Id = id;
            LinkedFactId = linkedFactId;
            Content = content;
        }
    }

    /// <summary>
    /// Winny-only live view. The authored fact link stays host-side so the
    /// Trop remains material to interpret rather than an automatic answer.
    /// </summary>
    public sealed class AlibiClueView
    {
        public AlibiClueId Id { get; }
        public string Content { get; }

        public AlibiClueView(AlibiClueId id, string content)
        {
            Id = id;
            Content = content;
        }
    }

    /// <summary>Post-Runda reveal of an acquired Trop and its authored link.</summary>
    public sealed class AlibiClueRevealView
    {
        public AlibiClueId Id { get; }
        public string LinkedFactId { get; }
        public string Content { get; }

        public AlibiClueRevealView(AlibiClueId id, string linkedFactId, string content)
        {
            Id = id;
            LinkedFactId = linkedFactId;
            Content = content;
        }
    }
}
