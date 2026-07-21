using System.Collections.Generic;

namespace InterrogationRoom.Voice
{
    /// <summary>
    /// Public, cosmetic lobby state. Voice audio remains owned by Vivox; this
    /// collection only maps server-validated Mirror identities to UI activity.
    /// </summary>
    public sealed class VoiceSpeakingState
    {
        private readonly HashSet<uint> _speakingPlayers = new();

        public bool IsSpeaking(uint networkIdentityNetId) =>
            networkIdentityNetId != 0u && _speakingPlayers.Contains(networkIdentityNetId);

        public bool Apply(uint networkIdentityNetId, bool isSpeaking)
        {
            if (networkIdentityNetId == 0u)
                return false;

            return isSpeaking
                ? _speakingPlayers.Add(networkIdentityNetId)
                : _speakingPlayers.Remove(networkIdentityNetId);
        }

        public void Clear() => _speakingPlayers.Clear();
    }
}
