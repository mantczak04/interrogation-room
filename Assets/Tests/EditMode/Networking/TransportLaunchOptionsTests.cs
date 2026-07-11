using NUnit.Framework;

namespace InterrogationRoom.Networking.Tests
{
    public sealed class TransportLaunchOptionsTests
    {
        [TestCase("-force-kcp")]
        [TestCase("-FORCE-KCP")]
        public void ForceKcp_ExplicitArgument_ReturnsTrue(string argument)
        {
            Assert.That(TransportLaunchOptions.ForceKcp(new[] { "game.exe", argument }), Is.True);
        }

        [Test]
        public void ForceKcp_MissingOrNullArgument_ReturnsFalse()
        {
            Assert.That(TransportLaunchOptions.ForceKcp(new[] { "game.exe" }), Is.False);
            Assert.That(TransportLaunchOptions.ForceKcp(null), Is.False);
        }
    }
}
