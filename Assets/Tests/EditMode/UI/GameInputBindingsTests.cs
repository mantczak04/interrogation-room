using System.Linq;
using InterrogationRoom.Settings;
using NUnit.Framework;

namespace InterrogationRoom.UI.Tests
{
    public sealed class GameInputBindingsTests
    {
        private sealed class InMemorySettingsStore : ISettingsStore
        {
            private readonly System.Collections.Generic.Dictionary<string, float> floats = new();
            private readonly System.Collections.Generic.Dictionary<string, string> strings = new();

            public bool TryGetFloat(string key, out float value) => floats.TryGetValue(key, out value);
            public void SetFloat(string key, float value) => floats[key] = value;
            public bool TryGetString(string key, out string value) => strings.TryGetValue(key, out value);
            public void SetString(string key, string value) => strings[key] = value;
            public void DeleteKey(string key)
            {
                floats.Remove(key);
                strings.Remove(key);
            }
            public void Save() { }
        }

        private GameSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = new GameSettings(new InMemorySettingsStore());
        }

        [Test]
        public void CatalogContainsOnlyApprovedRebindableGameplayActions()
        {
            CollectionAssert.AreEquivalent(
                new[]
                {
                    GameInputAction.MoveForward,
                    GameInputAction.MoveBackward,
                    GameInputAction.MoveLeft,
                    GameInputAction.MoveRight,
                    GameInputAction.Sprint,
                    GameInputAction.Jump,
                    GameInputAction.Interact,
                    GameInputAction.Drop,
                    GameInputAction.Dance,
                    GameInputAction.View,
                    GameInputAction.PrivateObjective,
                    GameInputAction.Fire,
                    GameInputAction.VoiceMute
                },
                GameInputBindingCatalog.Actions);
        }

        [TestCase("<Keyboard>/escape")]
        [TestCase("<Keyboard>/f8")]
        [TestCase("<Keyboard>/digit1")]
        [TestCase("<Keyboard>/digit2")]
        [TestCase("<Keyboard>/digit3")]
        [TestCase("<Keyboard>/digit4")]
        [TestCase("<Keyboard>/digit5")]
        [TestCase("<Keyboard>/1")]
        [TestCase("<Keyboard>/2")]
        [TestCase("<Keyboard>/3")]
        [TestCase("<Keyboard>/4")]
        [TestCase("<Keyboard>/5")]
        public void ReservedControlsCannotBeAssigned(string path)
        {
            InputBindingValidation result =
                GameInputBindingCatalog.ValidateOverride(
                    GameInputAction.Interact,
                    path,
                    settings);

            Assert.That(result.Status, Is.EqualTo(InputBindingValidationStatus.Reserved));
        }

        [Test]
        public void ExistingBindingIsReportedAsConflict()
        {
            settings.SetInputBindingOverride(
                GameInputAction.Jump,
                "<Keyboard>/f");

            InputBindingValidation result =
                GameInputBindingCatalog.ValidateOverride(
                    GameInputAction.Interact,
                    "<Keyboard>/f",
                    settings);

            Assert.That(result.Status, Is.EqualTo(InputBindingValidationStatus.Conflict));
            Assert.That(result.ConflictingAction, Is.EqualTo(GameInputAction.Jump));
        }

        [Test]
        public void DefaultBindingsAreUniqueAndNonEmpty()
        {
            string[] paths = GameInputBindingCatalog.Actions
                .Select(GameInputBindingCatalog.GetDefaultPath)
                .ToArray();

            Assert.That(paths, Has.All.Not.Empty);
            Assert.That(
                paths.Distinct(System.StringComparer.OrdinalIgnoreCase).Count(),
                Is.EqualTo(paths.Length));
        }

        [Test]
        public void OverrideBecomesEffectiveImmediatelyAndResetRestoresDefault()
        {
            settings.SetInputBindingOverride(
                GameInputAction.Interact,
                "<Keyboard>/f");

            Assert.That(
                GameInputBindingCatalog.GetEffectivePath(
                    GameInputAction.Interact,
                    settings),
                Is.EqualTo("<Keyboard>/f"));

            settings.ResetInputBindingOverride(GameInputAction.Interact);

            Assert.That(
                GameInputBindingCatalog.GetEffectivePath(
                    GameInputAction.Interact,
                    settings),
                Is.EqualTo(GameInputBindingCatalog.GetDefaultPath(
                    GameInputAction.Interact)));
        }
    }
}
