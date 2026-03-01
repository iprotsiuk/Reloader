using NUnit.Framework;
using UnityEngine;
using Reloader.Reloading.Runtime;

namespace Reloader.Reloading.Tests.EditMode
{
    public class WorkbenchMountDefinitionsEditModeTests
    {
        [Test]
        public void MountSlotDefinition_RequiresAllTagsAndRejectsForbiddenTags()
        {
            var slot = new MountSlotDefinition("press-slot", new[] { "press" }, new[] { "tool.scale" });

            var candidate = ScriptableObject.CreateInstance<MountableItemDefinition>();
            candidate.SetValuesForTests("press.single", new[] { "press" }, null);

            var incompatible = ScriptableObject.CreateInstance<MountableItemDefinition>();
            incompatible.SetValuesForTests("scale.combo", new[] { "press", "tool.scale" }, null);

            Assert.That(slot.CanAccept(candidate), Is.True);
            Assert.That(slot.CanAccept(incompatible), Is.False);
        }

        [Test]
        public void CompatibilityRuleSet_EnforcesRequiredAndForbiddenTags()
        {
            var ruleSet = new CompatibilityRuleSet(new[] { "shellholder" }, new[] { "deprecated" });
            var tags = new[] { "shellholder", "precision" };

            Assert.That(ruleSet.AreSatisfiedBy(tags), Is.True);
            Assert.That(ruleSet.AreSatisfiedBy(new[] { "precision" }), Is.False);
            Assert.That(ruleSet.AreSatisfiedBy(new[] { "shellholder", "deprecated" }), Is.False);
        }

        [Test]
        public void WorkbenchDefinition_StoresTopLevelSlots()
        {
            var bench = ScriptableObject.CreateInstance<WorkbenchDefinition>();
            var slots = new[]
            {
                new MountSlotDefinition("slot-a"),
                new MountSlotDefinition("slot-b")
            };

            bench.SetValuesForTests("bench.alpha", slots);

            Assert.That(bench.WorkbenchId, Is.EqualTo("bench.alpha"));
            Assert.That(bench.TopLevelSlots.Count, Is.EqualTo(2));
            Assert.That(bench.TopLevelSlots[0].SlotId, Is.EqualTo("slot-a"));
        }
    }
}
