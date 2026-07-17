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
                new CaseAsset.AuthoredFact { id = "fontanna", text = "O 18:00 grupa spotkała się przy fontannie.", canBeHidden = false },
                new CaseAsset.AuthoredFact { id = "zupa", text = "Kelner wylał zupę na obrus.", canBeHidden = true },
                new CaseAsset.AuthoredFact { id = "sto-lat", text = "Wszyscy śpiewali sto lat panu Henrykowi.", canBeHidden = true },
                new CaseAsset.AuthoredFact { id = "klucze", text = "Ktoś zgubił klucze pod stołem.", canBeHidden = true },
                new CaseAsset.AuthoredFact { id = "tramwaj", text = "Grupa wróciła tramwajem numer 12.", canBeHidden = false },
                new CaseAsset.AuthoredFact
                {
                    id = "pogoda",
                    text = "Na przystanku padał deszcz.",
                    canBeHidden = false,
                    distinctiveDetail = true,
                    variantTexts = new List<string>
                    {
                        "Na przystanku padał deszcz.",
                        "Na przystanku padała drobna mżawka."
                    }
                }
            };
            asset.alibiClues = new List<CaseAsset.AuthoredAlibiClue>
            {
                new CaseAsset.AuthoredAlibiClue
                {
                    id = "paragon-zupa",
                    linkedFactId = "zupa",
                    content = "Paragon z restauracji: dwie zupy naliczone o 18:17."
                }
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
            Assert.That(definition.AlibiFacts[5].DistinctiveDetail, Is.True);
            Assert.That(definition.AlibiFacts[5].VariantTexts, Is.EqualTo(asset.alibiFacts[5].variantTexts));
            Assert.That(definition.AlibiFacts.Select(f => f.Id).Distinct().Count(), Is.EqualTo(6), "fact ids are unique");
            Assert.That(definition.AlibiFacts[1].Id, Is.EqualTo("zupa"));
            Assert.That(definition.AlibiClues.Count, Is.EqualTo(1));
            Assert.That(definition.AlibiClues[0].Id, Is.EqualTo(new AlibiClueId("paragon-zupa")));
            Assert.That(definition.AlibiClues[0].LinkedFactId, Is.EqualTo("zupa"));
            Assert.That(definition.AlibiClues[0].Content,
                Is.EqualTo("Paragon z restauracji: dwie zupy naliczone o 18:17."));
        }

        [Test]
        public void ToDefinition_MutatingAssetAfterConversion_DoesNotChangeDefinition()
        {
            var asset = ValidAsset();
            var definition = asset.ToDefinition();

            asset.title = "Zmieniony tytuł";
            asset.alibiFacts[0].text = "Podmieniony fakt.";
            asset.alibiClues[0].content = "Podmieniony Trop.";
            asset.alibiFacts[5].variantTexts[0] = "Podmieniony wariant.";
            asset.alibiFacts.RemoveAt(5);
            asset.maxHiddenFacts = 3;

            Assert.That(definition.Title, Is.EqualTo("Testowa Sprawa"));
            Assert.That(definition.AlibiFacts.Count, Is.EqualTo(6));
            Assert.That(definition.AlibiClues[0].Content,
                Is.EqualTo("Paragon z restauracji: dwie zupy naliczone o 18:17."));
            Assert.That(definition.AlibiFacts[0].Text, Is.EqualTo("O 18:00 grupa spotkała się przy fontannie."));
            Assert.That(definition.AlibiFacts[5].VariantTexts[0], Is.EqualTo("Na przystanku padał deszcz."));
            Assert.That(definition.MaxHiddenFacts, Is.EqualTo(2));
        }

        [Test]
        public void ToDefinition_InvalidAlibiClueBindingsAndCopiedFactText_AreBlocked()
        {
            var missingFact = ValidAsset();
            missingFact.alibiClues[0].linkedFactId = "brakujacy-fakt";

            var visibleFact = ValidAsset();
            visibleFact.alibiClues[0].linkedFactId = "fontanna";

            var copiedFact = ValidAsset();
            copiedFact.alibiClues[0].content = copiedFact.alibiFacts[1].text;

            Assert.That(() => missingFact.ToDefinition(), Throws.InvalidOperationException);
            Assert.That(() => visibleFact.ToDefinition(), Throws.InvalidOperationException);
            Assert.That(() => copiedFact.ToDefinition(), Throws.InvalidOperationException);
            Assert.That(missingFact.Validate(), Has.Some.Contains("missing"));
            Assert.That(visibleFact.Validate(), Has.Some.Contains("hideable"));
            Assert.That(copiedFact.Validate(), Has.Some.Contains("copy"));
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
        public void Validate_RequiresExactlySixFactsAndControlledVariants()
        {
            var fiveFacts = ValidAsset();
            fiveFacts.alibiFacts.RemoveAt(4);

            var noRotation = ValidAsset();
            noRotation.alibiFacts[5].variantTexts.Clear();

            var noDistinctiveDetail = ValidAsset();
            noDistinctiveDetail.alibiFacts[5].distinctiveDetail = false;

            Assert.That(fiveFacts.Validate(), Has.Some.Contains("exactly"));
            Assert.That(noRotation.Validate(), Has.Some.Contains("rotating"));
            Assert.That(noDistinctiveDetail.Validate(), Has.Some.Contains("charakterystycznyDetal"));
        }

        [Test]
        public void Validate_RejectsInvalidVariantPoolsAndCluesCopyingAlternateText()
        {
            var blankVariant = ValidAsset();
            blankVariant.alibiFacts[5].variantTexts.Add(" ");

            var duplicateVariant = ValidAsset();
            duplicateVariant.alibiFacts[5].variantTexts.Add("  NA PRZYSTANKU PADAŁ DESZCZ.  ");

            var missingPrimary = ValidAsset();
            missingPrimary.alibiFacts[5].variantTexts.RemoveAt(0);

            var copiedAlternate = ValidAsset();
            copiedAlternate.alibiFacts[5].canBeHidden = true;
            copiedAlternate.alibiClues[0].linkedFactId = "pogoda";
            copiedAlternate.alibiClues[0].content = "Na przystanku padała drobna mżawka.";

            Assert.That(blankVariant.Validate(), Has.Some.Contains("empty"));
            Assert.That(duplicateVariant.Validate(), Has.Some.Contains("duplicate"));
            Assert.That(missingPrimary.Validate(), Has.Some.Contains("primary"));
            Assert.That(copiedAlternate.Validate(), Has.Some.Contains("compatible"));
        }

        [Test]
        public void Validate_RequiresHideableNonDistinctiveFact()
        {
            var asset = ValidAsset();
            foreach (var fact in asset.alibiFacts.Where(fact => fact.canBeHidden))
                fact.distinctiveDetail = true;

            Assert.That(asset.Validate(), Has.Some.Contains("hideable non-distinctive"));
        }

        [Test]
        public void AuthoredCaseLibrary_AllAssetsConvertToPlayableDefinitions()
        {
            var expectedPaths = new[]
            {
                "Assets/Content/Cases/SprawaRozowyPomnik.asset",
                "Assets/Content/Cases/Case_Wesele.asset",
                "Assets/Content/Cases/Case_Obserwatorium.asset",
                "Assets/Content/Cases/Case_Muzeum.asset",
                "Assets/Content/Cases/Case_C05_GumoweKaczki.asset",
                "Assets/Content/Cases/Case_C06_KogutWKajdankach.asset",
                "Assets/Content/Cases/Case_C07_KozaJejWysokosc.asset",
                "Assets/Content/Cases/Case_C08_Galaretobus.asset",
                "Assets/Content/Cases/Case_C09_HejnalZKaczka.asset",
                "Assets/Content/Cases/Case_C10_PierogOdplywa.asset",
                "Assets/Content/Cases/Case_C11_WirujacaPrzykrywka.asset",
                "Assets/Content/Cases/Case_C12_ZupaUrzedowa.asset",
                "Assets/Content/Cases/Case_C13_RejsBezBiletu.asset",
                "Assets/Content/Cases/Case_C14_SmokZaKulisami.asset",
                "Assets/Content/Cases/Case_C15_Dozynki.asset",
                "Assets/Content/Cases/Case_C16_SyrenaWKapieli.asset",
                "Assets/Content/Cases/Case_C17_FontannaZeSniadaniem.asset",
                "Assets/Content/Cases/Case_C18_CzekoladoweParkometry.asset"
            };
            var paths = AssetDatabase.FindAssets("t:CaseAsset", new[] { "Assets/Content/Cases" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .ToArray();

            Assert.That(paths, Is.SupersetOf(expectedPaths),
                "Run Interrogation Room/Content/Sync Case Assets before validating the authored library.");
            foreach (var path in expectedPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<CaseAsset>(path);
                var definition = asset.ToDefinition();

                Assert.That(asset.Validate(), Is.Empty, path);
                Assert.That(definition.AlibiFacts.Count, Is.EqualTo(CaseAsset.RequiredFactCount), path);
                Assert.That(definition.AlibiFacts.Any(fact => fact.DistinctiveDetail), Is.True, path);
                Assert.That(definition.AlibiFacts.Any(fact => fact.VariantTexts.Count > 1), Is.True, path);
                Assert.That(definition.AlibiFacts.Count(fact => fact.CanBeHidden),
                    Is.GreaterThanOrEqualTo(definition.MaxHiddenFacts), path);
                Assert.That(definition.AlibiClues.Count, Is.EqualTo(3), path);
            }
        }

        [Test]
        public void PinkMonumentCase_ContainsThePhysicalDeveloperClue()
        {
            const string path = "Assets/Content/Cases/SprawaRozowyPomnik.asset";
            var asset = AssetDatabase.LoadAssetAtPath<CaseAsset>(path);

            Assert.That(asset, Is.Not.Null, path);
            Assert.That(
                asset.ToDefinition().AlibiClues.Select(clue => clue.Id.Value),
                Does.Contain("paragon-cztery-kompoty"),
                "The Room scene's physical guilty clue must remain compatible with the developer scenario.");
        }
    }
}
