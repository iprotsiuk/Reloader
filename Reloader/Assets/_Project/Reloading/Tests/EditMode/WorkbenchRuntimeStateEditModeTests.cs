using NUnit.Framework;
using UnityEngine;
using Reloader.Reloading.Runtime;

namespace Reloader.Reloading.Tests.EditMode
{
    public class WorkbenchRuntimeStateEditModeTests
    {
        [Test]
        public void InstallItem_WithChildSlots_InstantiatesNestedSlotStates()
        {
            var topSlot = new MountSlotDefinition("top-slot", new[] { "press" });
            var bench = ScriptableObject.CreateInstance<WorkbenchDefinition>();
            bench.SetValuesForTests("bench.main", new[] { topSlot });

            var childSlots = new[]
            {
                new MountSlotDefinition("die-slot", new[] { "die" }),
                new MountSlotDefinition("shellholder-slot", new[] { "shellholder" })
            };

            var press = ScriptableObject.CreateInstance<MountableItemDefinition>();
            press.SetValuesForTests("press.turret", new[] { "press" }, childSlots);

            var state = new WorkbenchRuntimeState(bench);

            var installed = state.TryInstall("top-slot", press);

            Assert.That(installed, Is.True);
            Assert.That(state.TryGetSlotState("top-slot", out var topSlotState), Is.True);
            Assert.That(topSlotState.MountedNode, Is.Not.Null);
            Assert.That(topSlotState.MountedNode.ChildSlots.Count, Is.EqualTo(2));
            Assert.That(state.TryGetSlotState("die-slot", out _), Is.True);
            Assert.That(state.TryGetSlotState("shellholder-slot", out _), Is.True);
        }

        [Test]
        public void TryInstall_ReturnsFalse_WhenItemInvalidForSlot()
        {
            var topSlot = new MountSlotDefinition("top-slot", new[] { "press" });
            var bench = ScriptableObject.CreateInstance<WorkbenchDefinition>();
            bench.SetValuesForTests("bench.main", new[] { topSlot });

            var incompatible = ScriptableObject.CreateInstance<MountableItemDefinition>();
            incompatible.SetValuesForTests("scale", new[] { "tool.scale" }, null);

            var state = new WorkbenchRuntimeState(bench);

            var installed = state.TryInstall("top-slot", incompatible);

            Assert.That(installed, Is.False);
            Assert.That(state.TryGetSlotState("top-slot", out var topSlotState), Is.True);
            Assert.That(topSlotState.MountedNode, Is.Null);
        }
    }
}
