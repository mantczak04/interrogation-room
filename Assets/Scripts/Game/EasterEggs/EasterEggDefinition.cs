using System;

namespace InterrogationRoom.EasterEggs
{
    /// <summary>
    /// Immutable, hand-authored public world content. Deliberately contains no
    /// role, objective, damage, or Runda-outcome data.
    /// </summary>
    public sealed class EasterEggDefinition
    {
        public EasterEggDefinition(
            string id,
            string propId,
            string locationId,
            string effectId)
        {
            Id = RequireId(id, nameof(id));
            PropId = RequireId(propId, nameof(propId));
            LocationId = RequireId(locationId, nameof(locationId));
            EffectId = RequireId(effectId, nameof(effectId));
        }

        public string Id { get; }
        public string PropId { get; }
        public string LocationId { get; }
        public string EffectId { get; }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A stable authored id is required.", parameterName);
            return value.Trim();
        }
    }

    public readonly struct EasterEggSelection
    {
        private EasterEggSelection(EasterEggDefinition definition)
        {
            Definition = definition;
        }

        public bool HasSpawn => Definition != null;
        public EasterEggDefinition Definition { get; }

        public static EasterEggSelection None => default;

        public static EasterEggSelection Spawn(EasterEggDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            return new EasterEggSelection(definition);
        }
    }
}
