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
            Assert.That(copy.Eyebrow, Is.EqualTo("DŁUŻSZA CZYNNOŚĆ"));
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

            Assert.That(copy.Eyebrow, Is.EqualTo("PROGRESS 100%"));
            Assert.That(copy.Title, Is.EqualTo("Search evidence shelf"));
            Assert.That(copy.Instruction, Is.EqualTo("KEEP HOLDING • RELEASE TO CANCEL"));
            Assert.That(copy.Progress, Is.EqualTo(1f));
        }

        [TestCase(InteractionHudMode.Success, "POTWIERDZONE", "CZYNNOŚĆ ZAKOŃCZONA")]
        [TestCase(InteractionHudMode.Warning, "UWAGA", "SPRAWDŹ SWÓJ AKTUALNY CEL")]
        [TestCase(InteractionHudMode.Cancelled, "PRZERWANE", "MOŻESZ SPRÓBOWAĆ PONOWNIE")]
        public void OutcomeModesCommunicateTheirMeaning(
            InteractionHudMode mode,
            string expectedEyebrow,
            string expectedInstruction)
        {
            InteractionHudCopy copy = InteractionHudPresentation.Build(
                mode,
                "Wynik interakcji",
                0f,
                UiLanguage.Polish);

            Assert.That(copy.Eyebrow, Is.EqualTo(expectedEyebrow));
            Assert.That(copy.Instruction, Is.EqualTo(expectedInstruction));
        }
    }
}
