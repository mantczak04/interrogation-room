using System;

namespace InterrogationRoom.Domain
{
    /// <summary>
    /// Stable identity of a player inside one Runda. Mapping network connections
    /// to a <see cref="PlayerId"/> is owned exclusively by the network adapter
    /// (NetworkRoundCoordinator); the domain never sees connections.
    /// </summary>
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public int Value { get; }

        public PlayerId(int value)
        {
            Value = value;
        }

        public bool Equals(PlayerId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => $"Player({Value})";

        public static bool operator ==(PlayerId left, PlayerId right) => left.Equals(right);

        public static bool operator !=(PlayerId left, PlayerId right) => !left.Equals(right);
    }
}
