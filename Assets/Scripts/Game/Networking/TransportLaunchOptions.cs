using System;
using System.Collections.Generic;

namespace InterrogationRoom.Networking
{
    /// <summary>Pure parser for local multiplayer launch overrides.</summary>
    public static class TransportLaunchOptions
    {
        public const string ForceKcpArgument = "-force-kcp";

        public static bool ForceKcp(IReadOnlyList<string> arguments)
        {
            if (arguments == null)
                return false;

            for (var index = 0; index < arguments.Count; index++)
            {
                if (string.Equals(arguments[index], ForceKcpArgument, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
