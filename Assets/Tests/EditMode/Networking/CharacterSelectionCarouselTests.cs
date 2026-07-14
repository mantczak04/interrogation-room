using InterrogationRoom.Gameplay.Characters;
using NUnit.Framework;

namespace InterrogationRoom.Networking.Tests
{
    public sealed class CharacterSelectionCarouselTests
    {
        [TestCase(CharacterId.Malpa, 1, CharacterId.Wieprz)]
        [TestCase(CharacterId.Ptaku, 1, CharacterId.Malpa)]
        [TestCase(CharacterId.Malpa, -1, CharacterId.Ptaku)]
        [TestCase(CharacterId.Jak, 5, CharacterId.Jak)]
        public void StepWrapsAcrossTheFiveCharacterRoster(
            CharacterId current,
            int offset,
            CharacterId expected)
        {
            Assert.That(CharacterSelectionCarousel.Step(current, offset), Is.EqualTo(expected));
        }

        [Test]
        public void DisplayNameUsesPlayerFacingPolishName()
        {
            Assert.That(CharacterSelectionCarousel.DisplayName(CharacterId.Malpa), Is.EqualTo("Małpa"));
        }
    }
}
