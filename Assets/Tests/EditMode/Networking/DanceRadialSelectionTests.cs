using InterrogationRoom.Gameplay.Characters;
using NUnit.Framework;
using UnityEngine;

namespace InterrogationRoom.Networking.Tests
{
    public sealed class DanceRadialSelectionTests
    {
        [TestCase(0f, 120f, 0)]
        [TestCase(120f, 0f, 1)]
        [TestCase(0f, -120f, 2)]
        [TestCase(-120f, 0f, 3)]
        public void PointerDirectionSelectsExpectedClockwiseSector(
            float x,
            float y,
            int expected)
        {
            Assert.That(
                DanceRadialSelection.FromPointerOffset(new Vector2(x, y), 80f),
                Is.EqualTo(expected));
        }

        [Test]
        public void PointerInsideDeadZoneDoesNotSelectDance()
        {
            Assert.That(
                DanceRadialSelection.FromPointerOffset(new Vector2(20f, 20f), 80f),
                Is.EqualTo(DanceRadialSelection.NoSelection));
        }
    }
}
