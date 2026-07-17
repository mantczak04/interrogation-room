using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.EasterEggs;
using NUnit.Framework;

namespace InterrogationRoom.Gameplay.Tests
{
    public sealed class RareEasterEggSelectionTests
    {
        [Test]
        public void SameSeedProducesTheSameDecisionAndAuthoredSelection()
        {
            IReadOnlyList<EasterEggDefinition> catalog = InitialEasterEggCatalog.Definitions;

            EasterEggSelection first = EasterEggSelector.Select(317_521, catalog, 25);
            EasterEggSelection second = EasterEggSelector.Select(317_521, catalog, 25);

            Assert.That(second.HasSpawn, Is.EqualTo(first.HasSpawn));
            Assert.That(
                second.HasSpawn ? second.Definition.Id : string.Empty,
                Is.EqualTo(first.HasSpawn ? first.Definition.Id : string.Empty));
        }

        [Test]
        public void DefaultChanceSkipsMostRundasButStillSelectsOccasionally()
        {
            int spawned = 0;
            const int sampleSize = 2_000;

            for (int seed = 0; seed < sampleSize; seed++)
            {
                if (EasterEggSelector.Select(
                        seed,
                        InitialEasterEggCatalog.Definitions,
                        EasterEggSelector.DefaultSpawnChancePercent).HasSpawn)
                {
                    spawned++;
                }
            }

            Assert.That(spawned, Is.GreaterThan(0));
            Assert.That(spawned, Is.LessThan(sampleSize / 4),
                "The default catalog must remain a rare surprise rather than appearing every Runda.");
        }

        [Test]
        public void AuthoredCatalogContainsDistinctPropsEffectsAndLogicalLocations()
        {
            IReadOnlyList<EasterEggDefinition> catalog = InitialEasterEggCatalog.Definitions;

            Assert.That(catalog, Has.Count.GreaterThanOrEqualTo(4));
            Assert.That(catalog.Select(entry => entry.Id).Distinct().Count(), Is.EqualTo(catalog.Count));
            Assert.That(catalog.Select(entry => entry.PropId).Distinct().Count(), Is.EqualTo(catalog.Count));
            Assert.That(catalog.Select(entry => entry.EffectId).Distinct().Count(), Is.EqualTo(catalog.Count));
            Assert.That(catalog.Select(entry => entry.LocationId).Distinct().Count(), Is.EqualTo(catalog.Count));
        }

        [Test]
        public void DeterministicSeedsCanReachMultipleAuthoredEasterEggs()
        {
            var selectedIds = new HashSet<string>();

            for (int seed = 0; seed < 5_000; seed++)
            {
                EasterEggSelection selection = EasterEggSelector.Select(
                    seed,
                    InitialEasterEggCatalog.Definitions,
                    EasterEggSelector.MaximumSpawnChancePercent);
                if (selection.HasSpawn)
                    selectedIds.Add(selection.Definition.Id);
            }

            Assert.That(selectedIds.Count, Is.EqualTo(InitialEasterEggCatalog.Definitions.Count));
        }

        [Test]
        public void SpawnChanceIsCappedSoConfigurationCannotMakeItRoutine()
        {
            int cappedSpawns = 0;
            int excessiveSpawns = 0;

            for (int seed = 0; seed < 1_000; seed++)
            {
                if (EasterEggSelector.Select(
                        seed,
                        InitialEasterEggCatalog.Definitions,
                        EasterEggSelector.MaximumSpawnChancePercent).HasSpawn)
                {
                    cappedSpawns++;
                }

                if (EasterEggSelector.Select(
                        seed,
                        InitialEasterEggCatalog.Definitions,
                        100).HasSpawn)
                {
                    excessiveSpawns++;
                }
            }

            Assert.That(excessiveSpawns, Is.EqualTo(cappedSpawns));
            Assert.That(excessiveSpawns, Is.LessThan(400));
        }
    }
}
