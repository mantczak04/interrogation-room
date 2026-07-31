using NUnit.Framework;

namespace InterrogationRoom.UI.Tests
{
    public sealed class EscapeInputRoutingTests
    {
        [Test]
        public void HighestActiveModalHandlesEscapeExactlyOnce()
        {
            var stack = new EscapeHandlerStack();
            object gameplayContext = new();
            object settings = new();
            object minigame = new();
            int gameplayInvocations = 0;
            int settingsInvocations = 0;
            int minigameInvocations = 0;

            stack.Register(
                gameplayContext,
                EscapeHandlerPriority.Context,
                () => true,
                () => gameplayInvocations++);
            stack.Register(
                settings,
                EscapeHandlerPriority.Settings,
                () => true,
                () => settingsInvocations++);
            stack.Register(
                minigame,
                EscapeHandlerPriority.Modal,
                () => true,
                () => minigameInvocations++);

            Assert.That(stack.TryHandleEscape(), Is.True);
            Assert.That(minigameInvocations, Is.EqualTo(1));
            Assert.That(settingsInvocations, Is.Zero);
            Assert.That(gameplayInvocations, Is.Zero);
        }

        [Test]
        public void InactiveHigherPriorityHandlerFallsThroughToContext()
        {
            var stack = new EscapeHandlerStack();
            int contextInvocations = 0;
            int modalInvocations = 0;

            stack.Register(
                new object(),
                EscapeHandlerPriority.Context,
                () => true,
                () => contextInvocations++);
            stack.Register(
                new object(),
                EscapeHandlerPriority.Modal,
                () => false,
                () => modalInvocations++);

            Assert.That(stack.TryHandleEscape(), Is.True);
            Assert.That(contextInvocations, Is.EqualTo(1));
            Assert.That(modalInvocations, Is.Zero);
        }

        [Test]
        public void UnregisterRemovesOnlyMatchingOwner()
        {
            var stack = new EscapeHandlerStack();
            object context = new();
            object settings = new();
            int contextInvocations = 0;

            stack.Register(
                context,
                EscapeHandlerPriority.Context,
                () => true,
                () => contextInvocations++);
            stack.Register(
                settings,
                EscapeHandlerPriority.Settings,
                () => true,
                () => { });

            stack.Unregister(settings);

            Assert.That(stack.TryHandleEscape(), Is.True);
            Assert.That(contextInvocations, Is.EqualTo(1));
        }

        [Test]
        public void NestedModalOwnersKeepGameplayAndCursorBlockedUntilLastOwnerCloses()
        {
            object settings = new();
            object minigame = new();
            PlayerInputGate.SetUiInputBlocked(false);
            PlayerInputGate.SetPlayerCursorReleased(false);

            try
            {
                PlayerInputGate.SetModalInputBlocked(settings, true);
                PlayerInputGate.SetModalInputBlocked(minigame, true);
                PlayerInputGate.SetModalInputBlocked(minigame, false);

                Assert.That(PlayerInputGate.GameplayInputBlocked, Is.True);
                Assert.That(PlayerInputGate.CursorReleased, Is.True);

                PlayerInputGate.SetModalInputBlocked(settings, false);

                Assert.That(PlayerInputGate.GameplayInputBlocked, Is.False);
                Assert.That(PlayerInputGate.CursorReleased, Is.False);
            }
            finally
            {
                PlayerInputGate.SetModalInputBlocked(minigame, false);
                PlayerInputGate.SetModalInputBlocked(settings, false);
                PlayerInputGate.SetUiInputBlocked(true);
            }
        }
    }
}
