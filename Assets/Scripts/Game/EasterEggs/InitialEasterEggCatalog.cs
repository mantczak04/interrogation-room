using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace InterrogationRoom.EasterEggs
{
    /// <summary>
    /// Stable ids for the initial hand-authored pool. Scene props bind to an
    /// entry by Id and provide its concrete visual/audio presentation.
    /// </summary>
    public static class InitialEasterEggCatalog
    {
        private static readonly ReadOnlyCollection<EasterEggDefinition> AuthoredDefinitions =
            Array.AsReadOnly(new[]
            {
                new EasterEggDefinition(
                    "break-room-mug-choir",
                    "off-key-mug-stack",
                    "break-room-coffee-counter",
                    "mugs-hum-police-theme"),
                new EasterEggDefinition(
                    "records-typewriter-confession",
                    "dusty-typewriter",
                    "records-room-side-desk",
                    "typewriter-prints-innocent-fish-confession"),
                new EasterEggDefinition(
                    "evidence-pigeon-inspector",
                    "cardboard-pigeon",
                    "evidence-locker-top-shelf",
                    "pigeon-turns-and-stamps-form"),
                new EasterEggDefinition(
                    "front-desk-intercom-forecast",
                    "retired-desk-intercom",
                    "front-desk-reception",
                    "intercom-announces-indoor-fog"),
            });

        public static IReadOnlyList<EasterEggDefinition> Definitions => AuthoredDefinitions;
    }
}
