using System;
using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Content;
using InterrogationRoom.Domain;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace InterrogationRoom.Content.Tests
{
    /// <summary>
    /// Edit Mode tests for the CaseAsset → CaseDefinition seam: complete
    /// conversion, immutability of the produced definition, and blocking of
    /// cases that cannot hide the required number of facts.
    /// </summary>
    public sealed class CaseAssetTests
    {
        private readonly List<CaseAsset> _createdAssets = new List<CaseAsset>();

        [TearDown]
        public void DestroyCreatedAssets()
        {
            foreach (var asset in _createdAssets)
                UnityEngine.Object.DestroyImmediate(asset);
            _createdAssets.Clear();
        }

        private CaseAsset ValidAsset()
        {
            var asset = ScriptableObject.CreateInstance<CaseAsset>();
            _createdAssets.Add(asset);
            asset.name = "SprawaTestowa";
            asset.title = "Testowa Sprawa";
            asset.crimeDescription = "Ktoś pomalował ratuszowy zegar na różowo.";
            asset.minHiddenFacts = 2;
            asset.maxHiddenFacts = 2;
            asset.alibiFacts = new List<CaseAsset.AuthoredFact>
            {
                new CaseAsset.AuthoredFact { text = "O 18:00 grupa spotkała się przy fontannie.", canBeHidden = false },
                new CaseAsset.AuthoredFact { text = "Kelner wylał zupę na obrus.", canBeHidden = true },
                new CaseAsset.AuthoredFact { text = "Wszyscy śpiewali sto lat panu Henrykowi.", canBeHidden = true },
                new CaseAsset.AuthoredFact { text = "Ktoś zgubił klucze pod stołem.", canBeHidden = true },
                new CaseAsset.AuthoredFact { text = "Grupa wróciła tramwajem numer 12.", canBeHidden = false },
                new CaseAsset.AuthoredFact { text = "Na przystanku padał deszcz.", canBeHidden = false }
            };
            return asset;
        }

        [Test]
        public void ToDefinition_ProducesCompleteDefinition()
        {
            var asset = ValidAsset();

            var definition = asset.ToDefinition();

            Assert.That(definition.Title, Is.EqualTo("Testowa Sprawa"));
            Assert.That(definition.CrimeDescription, Is.EqualTo("Ktoś pomalował ratuszowy zegar na różowo."));
            Assert.That(definition.MinHiddenFacts, Is.EqualTo(2));
            Assert.That(definition.MaxHiddenFacts, Is.EqualTo(2));
            Assert.That(definition.AlibiFacts.Count, Is.EqualTo(6));
            Assert.That(definition.AlibiFacts.Select(f => f.Text), Is.EqualTo(asset.alibiFacts.Select(f => f.text)));
            Assert.That(definition.AlibiFacts.Select(f => f.CanBeHidden), Is.EqualTo(asset.alibiFacts.Select(f => f.canBeHidden)));
            Assert.That(definition.AlibiFacts.Select(f => f.Id).Distinct().Count(), Is.EqualTo(6), "fact ids are unique");
        }

        [Test]
        public void ToDefinition_MutatingAssetAfterConversion_DoesNotChangeDefinition()
        {
            var asset = ValidAsset();
            var definition = asset.ToDefinition();

            asset.title = "Zmieniony tytuł";
            asset.alibiFacts[0].text = "Podmieniony fakt.";
            asset.alibiFacts.RemoveAt(5);
            asset.maxHiddenFacts = 3;

            Assert.That(definition.Title, Is.EqualTo("Testowa Sprawa"));
            Assert.That(definition.AlibiFacts.Count, Is.EqualTo(6));
            Assert.That(definition.AlibiFacts[0].Text, Is.EqualTo("O 18:00 grupa spotkała się przy fontannie."));
            Assert.That(definition.MaxHiddenFacts, Is.EqualTo(2));
        }

        [Test]
        public void ToDefinition_CaseUnableToHideRequiredFacts_IsBlocked()
        {
            var asset = ValidAsset();
            asset.maxHiddenFacts = 4; // only 3 facts are możliwyDoUkrycia

            Assert.That(() => asset.ToDefinition(), Throws.InvalidOperationException);
        }

        [Test]
        public void ToDefinition_EmptyContent_IsBlocked()
        {
            var emptyFact = ValidAsset();
            emptyFact.alibiFacts[2].text = "   ";

            var noFacts = ValidAsset();
            noFacts.alibiFacts.Clear();

            var noCrime = ValidAsset();
            noCrime.crimeDescription = "";

            Assert.That(() => emptyFact.ToDefinition(), Throws.InvalidOperationException);
            Assert.That(() => noFacts.ToDefinition(), Throws.InvalidOperationException);
            Assert.That(() => noCrime.ToDefinition(), Throws.InvalidOperationException);
        }

        [Test]
        public void Validate_ReturnsAllAuthoringErrorsWithoutThrowing()
        {
            var asset = ValidAsset();
            asset.title = " ";
            asset.crimeDescription = null;
            asset.minHiddenFacts = 4;
            asset.maxHiddenFacts = 2;

            var errors = asset.Validate();

            Assert.That(errors, Has.Some.Contains("Title"));
            Assert.That(errors, Has.Some.Contains("Przestępstwo"));
            Assert.That(errors, Has.Some.Contains("range"));
        }

        [Test]
        public void Validate_NullFactList_IsReportedInsteadOfThrowing()
        {
            var asset = ValidAsset();
            asset.alibiFacts = null;

            var errors = asset.Validate();

            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0], Does.Contain("no facts"));
            Assert.That(() => asset.ToDefinition(), Throws.InvalidOperationException);
        }

        [Test]
        public void ToDefinition_OutputStartsARoundInTheEngine()
        {
            var players = Enumerable.Range(1, 5).Select(i => new PlayerId(i));

            var transition = new RoundEngine().Handle(
                new RoundCommand.StartRound(ValidAsset().ToDefinition(), players, seed: 3));

            Assert.That(transition.Accepted, Is.True, transition.RejectionReason);
            Assert.That(transition.State.Phase, Is.EqualTo(RoundPhase.Preparation));
        }

        [Test]
        public void AuthoredCaseLibrary_AllAssetsConvertToPlayableDefinitions()
        {
            var paths = AssetDatabase.FindAssets("t:CaseAsset", new[] { "Assets/Content/Cases" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .ToArray();

            Assert.That(paths, Has.Length.GreaterThanOrEqualTo(4));
            foreach (var path in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<CaseAsset>(path);
                var definition = asset.ToDefinition();

                Assert.That(asset.Validate(), Is.Empty, path);
                Assert.That(definition.AlibiFacts.Count, Is.InRange(CaseAsset.RecommendedMinFacts, 10), path);
                Assert.That(definition.AlibiFacts.Count(fact => fact.CanBeHidden),
                    Is.GreaterThanOrEqualTo(definition.MaxHiddenFacts), path);
            }
        }
    }
}
