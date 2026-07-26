namespace InterrogationRoom.Voice
{
    public readonly struct VoiceChannelModeState
    {
        public bool ActiveSpatial { get; }
        public bool? JoiningSpatial { get; }
        public bool EffectiveSpatial => JoiningSpatial ?? ActiveSpatial;

        public VoiceChannelModeState(bool activeSpatial, bool? joiningSpatial = null)
        {
            ActiveSpatial = activeSpatial;
            JoiningSpatial = joiningSpatial;
        }

        public VoiceChannelModeState BeginJoin(bool spatial) =>
            new VoiceChannelModeState(ActiveSpatial, spatial);

        public VoiceChannelModeState CommitJoin() =>
            JoiningSpatial.HasValue
                ? new VoiceChannelModeState(JoiningSpatial.Value)
                : this;

        public VoiceChannelModeState RollbackJoin() =>
            new VoiceChannelModeState(ActiveSpatial);
    }
}
