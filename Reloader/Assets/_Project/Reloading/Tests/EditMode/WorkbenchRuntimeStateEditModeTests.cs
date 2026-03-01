using System.Linq;
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

        [Test]
        public void TryInstall_UsesRawChildSlotId_WhenSingleMatchExists()
        {
            var topSlot = new MountSlotDefinition("top-slot", new[] { "press" });
            var bench = ScriptableObject.CreateInstance<WorkbenchDefinition>();
            bench.SetValuesForTests("bench.main", new[] { topSlot });

            var press = ScriptableObject.CreateInstance<MountableItemDefinition>();
            press.SetValuesForTests(
                "press.turret",
                new[] { "press" },
                new[] { new MountSlotDefinition("die-slot", new[] { "die" }) });

            var die = ScriptableObject.CreateInstance<MountableItemDefinition>();
            die.SetValuesForTests("die.full-length", new[] { "die" }, null);

            var state = new WorkbenchRuntimeState(bench);

            Assert.That(state.TryInstall("top-slot", press), Is.True);
            Assert.That(state.TryInstall("die-slot", die), Is.True);
            Assert.That(state.TryGetSlotState("die-slot", out var dieSlotState), Is.True);
            Assert.That(dieSlotState.IsOccupied, Is.True);
        }

        [Test]
        public void InstallItem_WithDuplicateChildSlotDefinitions_IndexesEachChildSlotUniquely()
        {
            var bench = ScriptableObject.CreateInstance<WorkbenchDefinition>();
            bench.SetValuesForTests("bench.main", new[]
            {
                new MountSlotDefinition("top-slot-a", new[] { "press" }),
                new MountSlotDefinition("top-slot-b", new[] { "press" })
            });

            var press = ScriptableObject.CreateInstance<MountableItemDefinition>();
            press.SetValuesForTests(
                "press.turret",
                new[] { "press" },
                new[] { new MountSlotDefinition("die-slot", new[] { "die" }) });

            var state = new WorkbenchRuntimeState(bench);
            Assert.That(state.TryInstall("top-slot-a", press), Is.True);
            Assert.That(state.TryInstall("top-slot-b", press), Is.True);

            var graphKeys = state.SlotsById.Keys.Where(key => key.EndsWith("/die-slot", System.StringComparison.Ordinal)).ToArray();

            Assert.That(graphKeys.Length, Is.EqualTo(2));
            Assert.That(graphKeys[0], Is.Not.EqualTo(graphKeys[1]));
            Assert.That(state.TryGetSlotState(graphKeys[0], out var childSlotA), Is.True);
            Assert.That(state.TryGetSlotState(graphKeys[1], out var childSlotB), Is.True);
            Assert.That(childSlotA, Is.Not.SameAs(childSlotB));
            Assert.That(state.TryGetSlotState("die-slot", out _), Is.False, "Raw slotId should be ambiguous when duplicate child slots exist.");
        }
    }
}
