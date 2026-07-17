using System.Linq;
using InterrogationRoom.Domain;
using NUnit.Framework;

namespace InterrogationRoom.Domain.Tests
{
    public sealed class ObjectiveContentDefinitionTests
    {
        [Test]
        public void WrobienieLibrary_HasFourValidNarrativeTwoStepVariants()
        {
            Assert.That(WrobienieDefinitions.Variants, Has.Count.EqualTo(4));
            Assert.That(WrobienieDefinitions.Variants.Select(value => value.Id).Distinct().Count(),
                Is.EqualTo(4));

            foreach (var definition in WrobienieDefinitions.Variants)
            {
                Assert.That(definition.Kind, Is.EqualTo(PrivateObjectiveKind.SecretObjective));
                Assert.That(definition.Title, Is.Not.Empty);
                Assert.That(definition.Motive, Is.Not.Empty);
                Assert.That(definition.Steps, Has.Count.EqualTo(2));
                Assert.That(definition.Steps.All(step => !string.IsNullOrWhiteSpace(step.Description)), Is.True);
                Assert.That(definition.Steps.All(step => !string.IsNullOrWhiteSpace(step.LocationHint)), Is.True);
                Assert.That(definition.Steps.All(step => step.CreatesIncident), Is.True);
                Assert.That(definition.ReservedItemIds, Is.Not.Empty);
            }
        }

        [Test]
        public void EscapeLibrary_HasThreeCompatibleNarrativePlans()
        {
            Assert.That(EscapePlanDefinitions.AuthoredPlans, Has.Count.EqualTo(3));
            foreach (var plan in EscapePlanDefinitions.AuthoredPlans)
            {
                Assert.That(plan.Title, Is.Not.Empty);
                Assert.That(plan.Motive, Is.Not.Empty);
                Assert.That(plan.CommonSteps, Has.Count.EqualTo(2));
                Assert.That(plan.Exits, Has.Count.GreaterThanOrEqualTo(2));
                Assert.That(plan.CommonSteps.All(step => !string.IsNullOrWhiteSpace(step.Description)), Is.True);
                Assert.That(plan.CommonSteps.All(step => !string.IsNullOrWhiteSpace(step.LocationHint)), Is.True);
                Assert.That(plan.Exits.All(exit => !string.IsNullOrWhiteSpace(exit.Description)), Is.True);
                Assert.That(plan.Exits.All(exit => !string.IsNullOrWhiteSpace(exit.LocationHint)), Is.True);
            }
        }

        [Test]
        public void EscapeLibrary_PreservesPrototypePhysicalContract()
        {
            var prototype = EscapePlanDefinitions.Prototype;
            foreach (var plan in EscapePlanDefinitions.AuthoredPlans)
            {
                Assert.That(plan.Id, Is.EqualTo(prototype.Id));
                Assert.That(plan.CommonSteps.Select(step => step.Id),
                    Is.EqualTo(prototype.CommonSteps.Select(step => step.Id)));
                Assert.That(plan.Exits.Select(exit => exit.Id),
                    Is.EqualTo(prototype.Exits.Select(exit => exit.Id)));
                Assert.That(plan.Exits.Select(exit => exit.PreparationStepId),
                    Is.EqualTo(prototype.Exits.Select(exit => exit.PreparationStepId)));
            }
        }
    }
}
