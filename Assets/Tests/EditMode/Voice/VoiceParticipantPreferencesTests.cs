using InterrogationRoom.Settings;
using NUnit.Framework;

namespace InterrogationRoom.Voice.Tests
{
    public sealed class VoiceParticipantPreferencesTests
    {
        [Test]
        public void VolumeDefaultsAndClampsPerParticipant()
        {
            var preferences = new VoiceParticipantPreferences();

            Assert.That(preferences.GetVolumePercent(17u),
                Is.EqualTo(GameSettings.DefaultVoicePercent));
            Assert.That(preferences.SetVolumePercent(17u, 240f), Is.True);
            Assert.That(preferences.GetVolumePercent(17u),
                Is.EqualTo(GameSettings.MaxVoicePercent));
            Assert.That(preferences.SetVolumePercent(0u, 25f), Is.False);
        }

        [Test]
        public void LocalMuteCanBeSetAndClearedPerParticipant()
        {
            var preferences = new VoiceParticipantPreferences();

            Assert.That(preferences.SetMuted(17u, true), Is.True);
            Assert.That(preferences.IsMuted(17u), Is.True);
            Assert.That(preferences.IsMuted(18u), Is.False);
            Assert.That(preferences.SetMuted(17u, false), Is.True);
            Assert.That(preferences.IsMuted(17u), Is.False);
            Assert.That(preferences.SetMuted(0u, true), Is.False);
        }

        [Test]
        public void ClearRestoresDefaults()
        {
            var preferences = new VoiceParticipantPreferences();
            preferences.SetVolumePercent(17u, 25f);
            preferences.SetMuted(17u, true);

            preferences.Clear();

            Assert.That(preferences.GetVolumePercent(17u),
                Is.EqualTo(GameSettings.DefaultVoicePercent));
            Assert.That(preferences.IsMuted(17u), Is.False);
        }
    }

    public sealed class LocalVoicePublicationStateTests
    {
        [Test]
        public void MutedInputNeverPublishesAsSpeaking()
        {
            var state = new LocalVoicePublicationState();

            LocalVoicePublication publication = state.Evaluate(
                speechDetected: true,
                isMuted: true);

            Assert.That(publication.IsSpeaking, Is.False);
            Assert.That(publication.IsMuted, Is.True);
            Assert.That(publication.ShouldPublish, Is.True);
        }

        [Test]
        public void UnchangedPublishedStateIsNotSentAgainUnlessForced()
        {
            var state = new LocalVoicePublicationState();
            LocalVoicePublication first = state.Evaluate(
                speechDetected: true,
                isMuted: false);
            state.MarkPublished(first);

            Assert.That(state.Evaluate(true, false).ShouldPublish, Is.False);
            Assert.That(state.Evaluate(true, false, force: true).ShouldPublish, Is.True);
        }

        [Test]
        public void SpeechOrMuteChangesRequirePublication()
        {
            var state = new LocalVoicePublicationState();
            LocalVoicePublication silent = state.Evaluate(
                speechDetected: false,
                isMuted: false);
            state.MarkPublished(silent);

            Assert.That(state.Evaluate(true, false).ShouldPublish, Is.True);
            LocalVoicePublication muted = state.Evaluate(true, true);
            Assert.That(muted.IsSpeaking, Is.False);
            Assert.That(muted.ShouldPublish, Is.True);
        }

        [Test]
        public void UnsentStateRemainsPendingUntilNetworkAcceptsIt()
        {
            var state = new LocalVoicePublicationState();

            Assert.That(state.Evaluate(true, false).ShouldPublish, Is.True);
            Assert.That(state.Evaluate(true, false).ShouldPublish, Is.True);
        }
    }
}
