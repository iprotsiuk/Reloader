using NUnit.Framework;
using UnityEngine;
using Reloader.Reloading.Runtime;

namespace Reloader.Reloading.Tests.EditMode
{
    public class WorkbenchCompatibilityEvaluatorEditModeTests
    {
        [Test]
        public void Evaluate_ReturnsMissingRequiredTagFailure()
        {
            var slot = new MountSlotDefinition("slot", new[] { "shellholder-coax" });
            var candidate = ScriptableObject.CreateInstance<MountableItemDefinition>();
            candidate.SetValuesForTests("holder.classic", new[] { "shellholder-classic" }, null);

            var evaluator = new WorkbenchCompatibilityEvaluator();

            var result = evaluator.Evaluate(slot, candidate);

            Assert.That(result.IsCompatible, Is.False);
            Assert.That(result.MissingRequiredTags, Does.Contain("shellholder-coax"));
            Assert.That(result.PresentForbiddenTags, Is.Empty);
        }

        [Test]
        public void Evaluate_ReturnsForbiddenTagFailure()
        {
            var slot = new MountSlotDefinition("slot", forbiddenTags: new[] { "deprecated" });
            var candidate = ScriptableObject.CreateInstance<MountableItemDefinition>();
            candidate.SetValuesForTests("item", new[] { "deprecated" }, null);

            var evaluator = new WorkbenchCompatibilityEvaluator();

            var result = evaluator.Evaluate(slot, candidate);

            Assert.That(result.IsCompatible, Is.False);
            Assert.That(result.PresentForbiddenTags, Does.Contain("deprecated"));
        }

        [Test]
        public void Evaluate_RunsAdditionalRuleCallback()
        {
            var slot = new MountSlotDefinition("slot");
            var candidate = ScriptableObject.CreateInstance<MountableItemDefinition>();
            candidate.SetValuesForTests("item", new[] { "press" }, null);

            var evaluator = new WorkbenchCompatibilityEvaluator();

            var result = evaluator.Evaluate(slot, candidate, (_, __) =>
            {
                return WorkbenchCompatibilityResult.Incompatible(
                    missingRequiredTags: new[] { "custom-required" },
                    presentForbiddenTags: null,
                    diagnosticCodes: new[] { "rule.custom.denied" });
            });

            Assert.That(result.IsCompatible, Is.False);
            Assert.That(result.MissingRequiredTags, Does.Contain("custom-required"));
            Assert.That(result.DiagnosticCodes, Does.Contain("rule.custom.denied"));
        }
    }
}
