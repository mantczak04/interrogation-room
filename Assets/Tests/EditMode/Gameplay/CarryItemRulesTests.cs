using InterrogationRoom.Items;
using NUnit.Framework;

namespace InterrogationRoom.Gameplay.Tests
{
    public sealed class CarryItemRulesTests
    {
        [TestCase(CarryItemState.AtHome)]
        [TestCase(CarryItemState.Dropped)]
        [TestCase(CarryItemState.Placed)]
        public void AvailableItemCanBePickedUpWhenActorHasFreeHands(CarryItemState state)
        {
            Assert.That(CarryItemRules.CanPickup(state, actorCanAct: true, actorAlreadyCarriesItem: false), Is.True);
        }

        [Test]
        public void ActorCannotCarryASecondSignificantItem()
        {
            Assert.That(CarryItemRules.CanPickup(
                CarryItemState.Dropped,
                actorCanAct: true,
                actorAlreadyCarriesItem: true), Is.False);
        }

        [Test]
        public void SlotAcceptsAnyItemOrAnExplicitlyAuthoredItemId()
        {
            Assert.That(CarryItemRules.SlotAccepts("akta-kr-17", true, new[] { "klucz" }), Is.True);
            Assert.That(CarryItemRules.SlotAccepts("akta-kr-17", false, new[] { "klucz", "akta-kr-17" }), Is.True);
            Assert.That(CarryItemRules.SlotAccepts("telefon", false, new[] { "klucz", "akta-kr-17" }), Is.False);
        }

        [Test]
        public void DroppedMandatoryItemReturnsAfterTimeoutOrOutOfBounds()
        {
            Assert.That(CarryItemRules.ShouldReturnHome(
                CarryItemState.Dropped,
                now: 31d,
                droppedAt: 10d,
                returnTimeout: 20d,
                worldY: 0d,
                outOfBoundsY: -20d), Is.True);
            Assert.That(CarryItemRules.ShouldReturnHome(
                CarryItemState.Dropped,
                now: 11d,
                droppedAt: 10d,
                returnTimeout: 20d,
                worldY: -21d,
                outOfBoundsY: -20d), Is.True);
            Assert.That(CarryItemRules.ShouldReturnHome(
                CarryItemState.Placed,
                now: 100d,
                droppedAt: 0d,
                returnTimeout: 20d,
                worldY: -100d,
                outOfBoundsY: -20d), Is.False,
                "A deliberately placed item remains available in its prepared slot.");
        }
    }
}
