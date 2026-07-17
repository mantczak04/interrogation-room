using Mirror;

namespace InterrogationRoom.Gameplay.EasterEggs
{
    /// <summary>
    /// Server-only observation of a public world action. It identifies what
    /// happened and who was physically present, but never supplies a motive,
    /// role, private objective, damage, or a Runda outcome.
    /// </summary>
    public readonly struct EasterEggWorldSignal
    {
        public EasterEggWorldSignal(
            string easterEggId,
            string propId,
            string locationId,
            string effectId,
            NetworkIdentity actor,
            NetworkIdentity source)
        {
            EasterEggId = easterEggId;
            PropId = propId;
            LocationId = locationId;
            EffectId = effectId;
            Actor = actor;
            Source = source;
        }

        public string EasterEggId { get; }
        public string PropId { get; }
        public string LocationId { get; }
        public string EffectId { get; }
        public NetworkIdentity Actor { get; }
        public NetworkIdentity Source { get; }
    }
}
