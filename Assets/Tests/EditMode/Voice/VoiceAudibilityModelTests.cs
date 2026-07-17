using NUnit.Framework;
using UnityEngine;

namespace InterrogationRoom.Voice.Tests
{
    public sealed class VoiceAudibilityModelTests
    {
        private static readonly VoiceAudibilityTuning Tuning = VoiceAudibilityTuning.Default;

        [Test]
        public void SameRoomSpeechStaysClear()
        {
            VoiceAudibility result = VoiceAudibilityModel.Evaluate(
                new VoiceAudibilityQuery
                {
                    PathKind = VoicePathKind.Clear,
                    DirectDistance = 6f
                },
                Tuning);

            Assert.That(result.VolumeMultiplier, Is.EqualTo(1f),
                "Same-room speech must rely on distance rolloff alone, not an occlusion cut.");
            Assert.That(result.LowPassCutoff, Is.EqualTo(Tuning.ClearCutoff));
        }

        [Test]
        public void FullWallIsPracticallySilent()
        {
            VoiceAudibility result = VoiceAudibilityModel.Evaluate(
                new VoiceAudibilityQuery
                {
                    PathKind = VoicePathKind.Blocked,
                    DirectDistance = 3f
                },
                Tuning);

            Assert.That(result.VolumeMultiplier, Is.LessThanOrEqualTo(0.05f),
                "A full wall must practically block speech.");
            Assert.That(result.LowPassCutoff, Is.LessThanOrEqualTo(700f));
        }

        [Test]
        public void ClosedDoorFarFromTheListenerBehavesLikeAWall()
        {
            VoiceAudibility wall = VoiceAudibilityModel.Evaluate(
                new VoiceAudibilityQuery { PathKind = VoicePathKind.Blocked },
                Tuning);
            VoiceAudibility result = VoiceAudibilityModel.Evaluate(
                new VoiceAudibilityQuery
                {
                    PathKind = VoicePathKind.ClosedPortals,
                    ClosedPortalCount = 1,
                    DirectDistance = 8f,
                    ListenerDistanceToNearestClosedPortal = 6f
                },
                Tuning);

            Assert.That(result.VolumeMultiplier, Is.EqualTo(wall.VolumeMultiplier),
                "Eavesdropping must require standing next to the closed door.");
            Assert.That(result.LowPassCutoff, Is.EqualTo(wall.LowPassCutoff));
        }

        [Test]
        public void ListenerAdjacentToAClosedDoorHearsQuietMuffledSpeech()
        {
            VoiceAudibility result = VoiceAudibilityModel.Evaluate(
                new VoiceAudibilityQuery
                {
                    PathKind = VoicePathKind.ClosedPortals,
                    ClosedPortalCount = 1,
                    DirectDistance = 3f,
                    ListenerDistanceToNearestClosedPortal = 0.5f
                },
                Tuning);

            Assert.That(result.VolumeMultiplier, Is.EqualTo(Tuning.ClosedPortalVolume));
            Assert.That(result.VolumeMultiplier, Is.GreaterThan(Tuning.WallVolume));
            Assert.That(result.VolumeMultiplier, Is.LessThanOrEqualTo(0.2f),
                "Eavesdropped speech must stay quiet.");
            Assert.That(result.LowPassCutoff, Is.EqualTo(Tuning.ClosedPortalCutoff));
            Assert.That(result.LowPassCutoff, Is.LessThanOrEqualTo(1200f),
                "Eavesdropped speech must stay strongly filtered.");
        }

        [Test]
        public void EavesdropVolumeFadesTowardsWallInsideTheFalloffBand()
        {
            VoiceAudibility near = EvaluateClosedAtDoorDistance(Tuning.EavesdropRange);
            VoiceAudibility mid = EvaluateClosedAtDoorDistance(
                Tuning.EavesdropRange + Tuning.EavesdropFalloff * 0.5f);
            VoiceAudibility far = EvaluateClosedAtDoorDistance(
                Tuning.EavesdropRange + Tuning.EavesdropFalloff);

            Assert.That(mid.VolumeMultiplier, Is.LessThan(near.VolumeMultiplier));
            Assert.That(far.VolumeMultiplier, Is.LessThan(mid.VolumeMultiplier));
            Assert.That(far.VolumeMultiplier, Is.EqualTo(Tuning.WallVolume));
        }

        [Test]
        public void TwoClosedDoorsOnThePathAreSilent()
        {
            VoiceAudibility result = VoiceAudibilityModel.Evaluate(
                new VoiceAudibilityQuery
                {
                    PathKind = VoicePathKind.ClosedPortals,
                    ClosedPortalCount = 2,
                    DirectDistance = 4f,
                    ListenerDistanceToNearestClosedPortal = 0.5f
                },
                Tuning);

            Assert.That(result.VolumeMultiplier, Is.EqualTo(0f),
                "Multiple closed doors must fully block speech even next to the first door.");
        }

        [Test]
        public void OpenDoorWithoutDetourKeepsThePresetVolume()
        {
            VoiceAudibility result = VoiceAudibilityModel.Evaluate(
                new VoiceAudibilityQuery
                {
                    PathKind = VoicePathKind.OpenPortals,
                    DirectDistance = 4f,
                    PortalPathLength = 4f
                },
                Tuning);

            Assert.That(result.VolumeMultiplier, Is.EqualTo(Tuning.OpenPortalVolume));
            Assert.That(result.LowPassCutoff, Is.EqualTo(Tuning.OpenPortalCutoff));
        }

        [Test]
        public void LongerOpenPortalPathWeakensSpeech()
        {
            VoiceAudibility direct = VoiceAudibilityModel.Evaluate(
                new VoiceAudibilityQuery
                {
                    PathKind = VoicePathKind.OpenPortals,
                    DirectDistance = 5f,
                    PortalPathLength = 5f
                },
                Tuning);
            VoiceAudibility aroundCorner = VoiceAudibilityModel.Evaluate(
                new VoiceAudibilityQuery
                {
                    PathKind = VoicePathKind.OpenPortals,
                    DirectDistance = 5f,
                    PortalPathLength = 14f
                },
                Tuning);

            Assert.That(aroundCorner.VolumeMultiplier, Is.LessThan(direct.VolumeMultiplier),
                "Voice travelling around corners through open doors must arrive weaker.");
            Assert.That(aroundCorner.VolumeMultiplier, Is.GreaterThan(0f),
                "An open-door path must stay audible.");
        }

        [Test]
        public void DistanceRolloffKeepsNearbySpeechClearAndSilencesItAtTheLimit()
        {
            AnimationCurve curve = VoiceAudibilityModel.BuildDistanceRolloffCurve(
                conversationalDistance: 2f,
                audibleDistance: 15f);

            Assert.That(curve.Evaluate(2f / 15f), Is.EqualTo(1f).Within(0.001f));
            Assert.That(curve.Evaluate(8f / 15f), Is.LessThan(0.3f),
                "Speech beyond conversational range must no longer sound nearby.");
            Assert.That(curve.Evaluate(1f), Is.EqualTo(0f).Within(0.001f));
        }

        private static VoiceAudibility EvaluateClosedAtDoorDistance(float doorDistance)
        {
            return VoiceAudibilityModel.Evaluate(
                new VoiceAudibilityQuery
                {
                    PathKind = VoicePathKind.ClosedPortals,
                    ClosedPortalCount = 1,
                    DirectDistance = 3f,
                    ListenerDistanceToNearestClosedPortal = doorDistance
                },
                Tuning);
        }
    }
}
