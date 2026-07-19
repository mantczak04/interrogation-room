using System.Collections.Generic;
using InterrogationRoom.Gameplay.Characters;
using InterrogationRoom.Settings;
using NUnit.Framework;
using UnityEngine;

namespace InterrogationRoom.UI.Tests
{
    public sealed class GameSettingsTests
    {
        private sealed class InMemorySettingsStore : ISettingsStore
        {
            public readonly Dictionary<string, float> Values = new Dictionary<string, float>();
            public int SaveCount;

            public bool TryGetFloat(string key, out float value) => Values.TryGetValue(key, out value);

            public void SetFloat(string key, float value) => Values[key] = value;

            public void Save() => SaveCount++;
        }

        private InMemorySettingsStore store;
        private GameSettings settings;

        [SetUp]
        public void SetUp()
        {
            store = new InMemorySettingsStore();
            settings = new GameSettings(store);
        }

        [Test]
        public void MouseSensitivity_WithoutStoredValue_UsesDefault()
        {
            Assert.That(settings.MouseSensitivity, Is.EqualTo(GameSettings.DefaultMouseSensitivity));
        }

        [Test]
        public void Language_WithoutStoredValue_DefaultsToPolish()
        {
            Assert.That(settings.Language, Is.EqualTo(UiLanguage.Polish));
        }

        [Test]
        public void SetLanguage_PersistsAndRaisesChanged()
        {
            int raised = 0;
            settings.Changed += () => raised++;

            settings.SetLanguage(UiLanguage.English);

            Assert.That(store.Values[GameSettings.LanguageKey], Is.EqualTo(1f));
            Assert.That(settings.Language, Is.EqualTo(UiLanguage.English));
            Assert.That(store.SaveCount, Is.EqualTo(1));
            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void Language_InvalidStoredValue_FallsBackToPolish()
        {
            store.Values[GameSettings.LanguageKey] = 99f;

            Assert.That(settings.Language, Is.EqualTo(UiLanguage.Polish));
        }

        [Test]
        public void UiText_LocalizesInBothDirections()
        {
            Assert.That(UiText.Get("Ustawienia", UiLanguage.English), Is.EqualTo("Settings"));
            Assert.That(UiText.Get("Open door", UiLanguage.Polish), Is.EqualTo("Otwórz drzwi"));
        }

        [TestCase(UiLanguage.Polish, CharacterId.Jak, "Graj jako Jak")]
        [TestCase(UiLanguage.Polish, CharacterId.Wieprz, "Graj jako Wieprz")]
        [TestCase(UiLanguage.English, CharacterId.Jak, "Play as Yak")]
        [TestCase(UiLanguage.English, CharacterId.Wieprz, "Play as Boar")]
        public void UiText_CharacterSwapPrompt_DoesNotMixLanguages(
            UiLanguage language,
            CharacterId character,
            string expected)
        {
            Assert.That(
                UiText.FormatCharacterSwapPrompt(character, language),
                Is.EqualTo(expected));
        }

        [Test]
        public void UiText_LocalizesMinigameCopyInEnglish()
        {
            Assert.That(
                UiText.Get("Przeszukiwanie akt", UiLanguage.English),
                Is.EqualTo("File Search"));
            Assert.That(
                UiText.Format(
                    "Błędny kod. Pozostało prób: {0}.",
                    UiLanguage.English,
                    2),
                Is.EqualTo("Wrong code. Attempts remaining: 2."));
        }

        [Test]
        public void MouseSensitivity_WithoutStoredValue_UsesConfiguredFallback()
        {
            settings.SetMouseSensitivityFallback(3.5f);

            Assert.That(settings.MouseSensitivity, Is.EqualTo(3.5f));
        }

        [Test]
        public void SetMouseSensitivityFallback_IsClamped()
        {
            settings.SetMouseSensitivityFallback(100f);

            Assert.That(settings.MouseSensitivity, Is.EqualTo(GameSettings.MaxMouseSensitivity));
        }

        [Test]
        public void MouseSensitivity_StoredValue_OverridesFallback()
        {
            store.Values[GameSettings.MouseSensitivityKey] = 4.25f;
            settings.SetMouseSensitivityFallback(1f);

            Assert.That(settings.MouseSensitivity, Is.EqualTo(4.25f));
        }

        [Test]
        public void MouseSensitivity_StoredValueOutOfRange_IsClampedOnRead()
        {
            store.Values[GameSettings.MouseSensitivityKey] = 999f;

            Assert.That(settings.MouseSensitivity, Is.EqualTo(GameSettings.MaxMouseSensitivity));
        }

        [Test]
        public void SetMouseSensitivity_PersistsClampedValueUnderKey()
        {
            settings.SetMouseSensitivity(0.01f);

            Assert.That(store.Values[GameSettings.MouseSensitivityKey], Is.EqualTo(GameSettings.MinMouseSensitivity));
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void SetMouseSensitivity_MatchingFallbackWithoutStoredValue_StillPersists()
        {
            settings.SetMouseSensitivity(GameSettings.DefaultMouseSensitivity);

            Assert.That(
                store.Values[GameSettings.MouseSensitivityKey],
                Is.EqualTo(GameSettings.DefaultMouseSensitivity));
        }

        [Test]
        public void SetMouseSensitivity_RaisesChanged()
        {
            int raised = 0;
            settings.Changed += () => raised++;

            settings.SetMouseSensitivity(5f);

            Assert.That(raised, Is.EqualTo(1));
            Assert.That(settings.MouseSensitivity, Is.EqualTo(5f));
        }

        [Test]
        public void SetMouseSensitivity_SameStoredValue_DoesNotRaiseChanged()
        {
            settings.SetMouseSensitivity(5f);
            int raised = 0;
            settings.Changed += () => raised++;

            settings.SetMouseSensitivity(5f);

            Assert.That(raised, Is.Zero);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void ClampMouseSensitivity_NaN_FallsBackToDefault()
        {
            Assert.That(
                GameSettings.ClampMouseSensitivity(float.NaN),
                Is.EqualTo(GameSettings.DefaultMouseSensitivity));
        }

        [Test]
        public void ClampMouseSensitivity_KeepsValuesInsideRange()
        {
            Assert.That(GameSettings.ClampMouseSensitivity(3f), Is.EqualTo(3f));
            Assert.That(GameSettings.ClampMouseSensitivity(-2f), Is.EqualTo(GameSettings.MinMouseSensitivity));
            Assert.That(GameSettings.ClampMouseSensitivity(50f), Is.EqualTo(GameSettings.MaxMouseSensitivity));
        }

        [Test]
        public void PlayerPrefsSettingsStore_RoundTripsFloatValues()
        {
            const string testKey = "settings.tests.roundtrip";
            var prefsStore = new PlayerPrefsSettingsStore();
            try
            {
                PlayerPrefs.DeleteKey(testKey);
                Assert.That(prefsStore.TryGetFloat(testKey, out _), Is.False);

                prefsStore.SetFloat(testKey, 3.75f);
                prefsStore.Save();

                Assert.That(prefsStore.TryGetFloat(testKey, out float stored), Is.True);
                Assert.That(stored, Is.EqualTo(3.75f));
            }
            finally
            {
                PlayerPrefs.DeleteKey(testKey);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void GameSettings_OverPlayerPrefsStore_RoundTripsMouseSensitivity()
        {
            bool hadValue = PlayerPrefs.HasKey(GameSettings.MouseSensitivityKey);
            float originalValue = hadValue ? PlayerPrefs.GetFloat(GameSettings.MouseSensitivityKey) : 0f;
            try
            {
                var prefsSettings = new GameSettings(new PlayerPrefsSettingsStore());
                prefsSettings.SetMouseSensitivity(6.5f);

                var reloaded = new GameSettings(new PlayerPrefsSettingsStore());
                Assert.That(reloaded.MouseSensitivity, Is.EqualTo(6.5f));
            }
            finally
            {
                if (hadValue)
                {
                    PlayerPrefs.SetFloat(GameSettings.MouseSensitivityKey, originalValue);
                }
                else
                {
                    PlayerPrefs.DeleteKey(GameSettings.MouseSensitivityKey);
                }

                PlayerPrefs.Save();
            }
        }
    }
}
