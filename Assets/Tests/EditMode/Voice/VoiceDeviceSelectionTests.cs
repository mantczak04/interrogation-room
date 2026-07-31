using System.Collections.Generic;
using NUnit.Framework;

namespace InterrogationRoom.Voice.Tests
{
    public sealed class VoiceDeviceSelectionTests
    {
        private static readonly IReadOnlyList<VoiceAudioDevice> Devices =
            new[]
            {
                new VoiceAudioDevice("default", "Default System Device"),
                new VoiceAudioDevice("usb", "USB Headset")
            };

        [Test]
        public void PreferredAvailableDeviceWinsOverCurrentActiveDevice()
        {
            Assert.That(
                VoiceDeviceSelection.ResolveDeviceId("usb", "default", Devices),
                Is.EqualTo("usb"));
        }

        [Test]
        public void MissingPreferredDeviceFallsBackWithoutForgettingPreference()
        {
            IReadOnlyList<VoiceAudioDevice> unplugged =
                new[] { new VoiceAudioDevice("default", "Default System Device") };

            Assert.That(
                VoiceDeviceSelection.ResolveDeviceId("usb", "default", unplugged),
                Is.EqualTo("default"));
            Assert.That(
                VoiceDeviceSelection.ResolveDeviceId("usb", "default", Devices),
                Is.EqualTo("usb"),
                "When the preferred device is plugged back in it should be selected again.");
        }

        [Test]
        public void MissingPreferredAndActiveDevicesUseFirstAvailableFallback()
        {
            Assert.That(
                VoiceDeviceSelection.ResolveDeviceId("missing", "also-missing", Devices),
                Is.EqualTo("default"));
        }

        [Test]
        public void EmptyDeviceListHasNoSelection()
        {
            Assert.That(
                VoiceDeviceSelection.ResolveDeviceId(
                    "usb",
                    "default",
                    System.Array.Empty<VoiceAudioDevice>()),
                Is.Null);
        }

        [TestCase("physical-id", "USB Headset", true)]
        [TestCase("default", "Default Communication Device", true)]
        [TestCase("none", "No Device", false)]
        [TestCase("", "", false)]
        public void UsableInputDeviceRejectsVivoxNullDevice(
            string deviceId,
            string deviceName,
            bool expected)
        {
            Assert.That(
                VoiceDeviceSelection.IsUsableInputDevice(
                    deviceId,
                    deviceName),
                Is.EqualTo(expected));
        }

    }
}
