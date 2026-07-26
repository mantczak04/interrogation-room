using System.Collections.Generic;
using InterrogationRoom.Settings;

namespace InterrogationRoom.Voice
{
    public sealed class VoiceParticipantPreferences
    {
        private readonly Dictionary<uint, float> volumePercentByNetId = new();
        private readonly HashSet<uint> mutedNetIds = new();

        public float GetVolumePercent(uint netId) =>
            volumePercentByNetId.TryGetValue(netId, out float volume)
                ? volume
                : GameSettings.DefaultVoicePercent;

        public bool SetVolumePercent(uint netId, float volumePercent)
        {
            if (netId == 0u)
                return false;

            volumePercentByNetId[netId] = GameSettings.ClampVoicePercent(volumePercent);
            return true;
        }

        public bool IsMuted(uint netId) =>
            netId != 0u && mutedNetIds.Contains(netId);

        public bool SetMuted(uint netId, bool muted)
        {
            if (netId == 0u)
                return false;

            if (muted)
                mutedNetIds.Add(netId);
            else
                mutedNetIds.Remove(netId);
            return true;
        }

        public void Clear()
        {
            volumePercentByNetId.Clear();
            mutedNetIds.Clear();
        }
    }
}
