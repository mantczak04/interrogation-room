using InterrogationRoom.Settings;
using NUnit.Framework;

namespace InterrogationRoom.UI.Tests
{
    public sealed class InteractionHudPresentationTests
    {
        [Test]
        public void TimedTargetExplainsThatTheKeyMustBeHeld()
        {
            InteractionHudCopy copy = InteractionHudPresentation.Build(
                InteractionHudMode.HoldAvailable,
                "Otwórz akta",
                0f,
                UiLanguage.Polish);

            Assert.That(copy.Key, Is.EqualTo("E"));
            Assert.That(copy.Title, Is.EqualTo("Otwórz akta"));
            Assert.That(copy.Instruction, Is.EqualTo("PRZYTRZYMAJ, ABY ROZPOCZĄĆ"));
        }

        [Test]
        public void ActiveInteractionShowsClampedLocalizedProgress()
        {
            InteractionHudCopy copy = InteractionHudPresentation.Build(
                InteractionHudMode.Active,
                "Search evidence shelf",
                1.4f,
                UiLanguage.English);

            Assert.That(copy.Key, Is.EqualTo("E"));
            Assert.That(copy.Title, Is.EqualTo("Search evidence shelf"));
            Assert.That(copy.Instruction, Is.EqualTo("KEEP HOLDING • RELEASE TO CANCEL"));
            Assert.That(copy.Progress, Is.EqualTo(1f));
        }

        [TestCase(InteractionHudMode.Success, "CZYNNOŚĆ ZAKOŃCZONA")]
        [TestCase(InteractionHudMode.Warning, "SPRAWDŹ SWÓJ AKTUALNY CEL")]
        [TestCase(InteractionHudMode.Cancelled, "MOŻESZ SPRÓBOWAĆ PONOWNIE")]
        public void OutcomeModesCommunicateTheirMeaning(
            InteractionHudMode mode,
            string expectedInstruction)
        {
            InteractionHudCopy copy = InteractionHudPresentation.Build(
                mode,
                "Wynik interakcji",
                0f,
                UiLanguage.Polish);

            Assert.That(copy.Title, Is.EqualTo("Wynik interakcji"));
            Assert.That(copy.Instruction, Is.EqualTo(expectedInstruction));
        }
    }
}
