using InterrogationRoom.Domain;

namespace InterrogationRoom.Networking
{
    public static class VoiceModeRules
    {
        public static bool UsesSpatialAudio(RoundPhase phase) =>
            phase != RoundPhase.Lobby;
    }
}
