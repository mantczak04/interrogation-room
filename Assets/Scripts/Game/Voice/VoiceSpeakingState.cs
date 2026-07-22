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
        private readonly HashSet<uint> _mutedPlayers = new();

        public bool IsSpeaking(uint networkIdentityNetId) =>
            networkIdentityNetId != 0u && _speakingPlayers.Contains(networkIdentityNetId);

        public bool IsMuted(uint networkIdentityNetId) =>
            networkIdentityNetId != 0u && _mutedPlayers.Contains(networkIdentityNetId);

        public bool Apply(uint networkIdentityNetId, bool isSpeaking)
        {
            return Apply(networkIdentityNetId, isSpeaking, IsMuted(networkIdentityNetId));
        }

        public bool Apply(uint networkIdentityNetId, bool isSpeaking, bool isMuted)
        {
            if (networkIdentityNetId == 0u)
                return false;

            bool speakingChanged = isSpeaking
                ? _speakingPlayers.Add(networkIdentityNetId)
                : _speakingPlayers.Remove(networkIdentityNetId);
            bool mutedChanged = isMuted
                ? _mutedPlayers.Add(networkIdentityNetId)
                : _mutedPlayers.Remove(networkIdentityNetId);
            return speakingChanged || mutedChanged;
        }

        public void Clear()
        {
            _speakingPlayers.Clear();
            _mutedPlayers.Clear();
        }
    }
}
