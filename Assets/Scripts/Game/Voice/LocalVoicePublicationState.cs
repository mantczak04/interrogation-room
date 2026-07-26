namespace InterrogationRoom.Voice
{
    public readonly struct LocalVoicePublication
    {
        public bool IsSpeaking { get; }
        public bool IsMuted { get; }
        public bool ShouldPublish { get; }

        public LocalVoicePublication(bool isSpeaking, bool isMuted, bool shouldPublish)
        {
            IsSpeaking = isSpeaking;
            IsMuted = isMuted;
            ShouldPublish = shouldPublish;
        }
    }

    public sealed class LocalVoicePublicationState
    {
        private bool hasPublished;
        private bool publishedSpeaking;
        private bool publishedMuted;

        public bool IsSpeaking { get; private set; }

        public LocalVoicePublication Evaluate(
            bool speechDetected,
            bool isMuted,
            bool force = false)
        {
            IsSpeaking = !isMuted && speechDetected;
            bool shouldPublish = force ||
                !hasPublished ||
                publishedSpeaking != IsSpeaking ||
                publishedMuted != isMuted;
            return new LocalVoicePublication(IsSpeaking, isMuted, shouldPublish);
        }

        public void MarkPublished(LocalVoicePublication publication)
        {
            publishedSpeaking = publication.IsSpeaking;
            publishedMuted = publication.IsMuted;
            hasPublished = true;
        }

        public void Reset()
        {
            IsSpeaking = false;
            hasPublished = false;
            publishedSpeaking = false;
            publishedMuted = false;
        }
    }
}
