using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Content;
using InterrogationRoom.Domain;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace InterrogationRoom.Content.Tests
{
    public sealed class PersonalMatterAssetTests
    {
        private PersonalMatterAsset _asset;

        [SetUp]
        public void SetUp()
        {
            _asset = ScriptableObject.CreateInstance<PersonalMatterAsset>();
            _asset.name = "OS-Test";
            _asset.stableId = "OS-TEST";
            _asset.title = "Odzyskaj prywatną rzecz";
            _asset.motive = "W depozycie znajduje się rzecz, której nie chcesz zostawić policji.";
            _asset.steps = new List<PersonalMatterAsset.AuthoredStep>
            {
                new PersonalMatterAsset.AuthoredStep
                {
                    stableStepId = "OS-TEST-znajdz",
                    description = "Znajdź właściwy klucz.",
                    locationHint = "magazyn dowodów — listwa kluczy",
                    anchorActionId = "osobista-sprawa-przygotuj"
                },
                new PersonalMatterAsset.AuthoredStep
                {
                    stableStepId = "OS-TEST-odzyskaj",
                    description = "Otwórz depozyt i odzyskaj rzecz.",
                    locationHint = "magazyn dowodów — szafki depozytowe",
                    anchorActionId = "osobista-sprawa-zakoncz",
                    createsIncident = true
                }
            };
            _asset.reservedItemIds = new List<string> { "testowy-przedmiot" };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_asset);
        }

        [Test]
        public void ToDefinition_CopiesNarrativeStepsAnchorsAndReservedItems()
        {
            PersonalMatterDefinition definition = _asset.ToDefinition();

            Assert.That(definition.Id, Is.EqualTo(new PrivateObjectiveId("OS-TEST")));
            Assert.That(definition.Title, Is.EqualTo("Odzyskaj prywatną rzecz"));
            Assert.That(definition.Motive, Does.Contain("depozycie"));
            Assert.That(definition.Steps, Has.Count.EqualTo(2));
            Assert.That(definition.Steps[0].Id,
                Is.EqualTo(new PrivateObjectiveStepId("OS-TEST-znajdz")));
            Assert.That(definition.Steps[0].AnchorActionId,
                Is.EqualTo(new PrivateObjectiveStepId("osobista-sprawa-przygotuj")));
            Assert.That(definition.Steps[0].Description, Does.Contain("klucz"));
            Assert.That(definition.Steps[0].LocationHint, Does.Contain("listwa"));
            Assert.That(definition.Steps[1].CreatesIncident, Is.True);
            Assert.That(definition.ReservedItemIds, Is.EqualTo(new[] { "testowy-przedmiot" }));
        }

        [Test]
        public void Validate_RejectsMissingNarrativeAnchorAndInvalidStepCount()
        {
            _asset.motive = string.Empty;
            _asset.steps[0].description = string.Empty;
            _asset.steps[1].anchorActionId = string.Empty;
            _asset.steps.Add(null);
            _asset.steps.Add(new PersonalMatterAsset.AuthoredStep());

            var errors = _asset.Validate();

            Assert.That(errors, Has.Some.Contains("Motive"));
            Assert.That(errors, Has.Some.Contains("2-3 steps"));
        }

        [Test]
        public void Validate_RejectsDuplicateStepAndReservedItemIds()
        {
            _asset.steps[1].stableStepId = _asset.steps[0].stableStepId;
            _asset.reservedItemIds.Add("testowy-przedmiot");

            var errors = _asset.Validate();

            Assert.That(errors, Has.Some.Contains("Step stable ids"));
            Assert.That(errors, Has.Some.Contains("Reserved item ids"));
        }

        [Test]
        public void AuthoredPersonalMatterLibrary_ContainsAllFifteenValidDefinitions()
        {
            var expectedPaths = Enumerable.Range(1, 15)
                .Select(number => $"Assets/Content/PersonalMatters/OS-{number:00}.asset")
                .ToArray();
            var paths = AssetDatabase.FindAssets(
                    "t:PersonalMatterAsset",
                    new[] { "Assets/Content/PersonalMatters" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();

            Assert.That(paths, Is.SupersetOf(expectedPaths),
                "Run Interrogation Room/Content/Sync Personal Matter Assets before validating the authored library.");
            foreach (var path in expectedPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<PersonalMatterAsset>(path);
                var definition = asset.ToDefinition();

                Assert.That(asset.Validate(), Is.Empty, path);
                Assert.That(definition.Steps, Has.Count.InRange(2, 3), path);
                Assert.That(definition.ReservedItemIds, Is.Not.Empty, path);
            }
        }
    }
}
