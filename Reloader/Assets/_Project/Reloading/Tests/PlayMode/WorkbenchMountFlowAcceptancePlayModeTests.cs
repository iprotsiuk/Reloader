using NUnit.Framework;
using Reloader.Reloading.Runtime;
using UnityEngine;

namespace Reloader.Reloading.Tests.PlayMode
{
    public class WorkbenchMountFlowAcceptancePlayModeTests
    {
        [Test]
        public void MountFlow_InstallPressThenDie_EnablesOperateRequirements()
        {
            var topSlot = new MountSlotDefinition("press-slot", requiredTags: new[] { "press" });
            var workbench = ScriptableObject.CreateInstance<WorkbenchDefinition>();
            workbench.SetValuesForTests("bench-main", new[] { topSlot });

            var dieSlot = new MountSlotDefinition("die-slot", requiredTags: new[] { "die" });
            var press = ScriptableObject.CreateInstance<MountableItemDefinition>();
            press.SetValuesForTests("press.single", new[] { "press" }, new[] { dieSlot });

            var die = ScriptableObject.CreateInstance<MountableItemDefinition>();
            die.SetValuesForTests("die.resize", new[] { "die" }, null);

            try
            {
                var runtimeState = new WorkbenchRuntimeState(workbench);

                var pressInstalled = runtimeState.TryInstall("press-slot", press);
                Assert.That(pressInstalled, Is.True);
                Assert.That(runtimeState.TryGetSlotState("die-slot", out var dieSlotState), Is.True);
                Assert.That(dieSlotState.IsOccupied, Is.False);
                Assert.That(GetOperateReadinessDiagnostic(runtimeState), Is.EqualTo("Missing required mount: die-slot"));

                var dieInstalled = runtimeState.TryInstall("die-slot", die);
                Assert.That(dieInstalled, Is.True);
                Assert.That(dieSlotState.IsOccupied, Is.True);
                Assert.That(GetOperateReadinessDiagnostic(runtimeState), Is.EqualTo(string.Empty));
            }
            finally
            {
                Object.DestroyImmediate(workbench);
                Object.DestroyImmediate(press);
                Object.DestroyImmediate(die);
            }
        }

        [Test]
        public void MountFlow_IncompatibleCandidate_ReportsDiagnosticsAndLeavesSlotEmpty()
        {
            var topSlot = new MountSlotDefinition("press-slot", requiredTags: new[] { "press" });
            var workbench = ScriptableObject.CreateInstance<WorkbenchDefinition>();
            workbench.SetValuesForTests("bench-main", new[] { topSlot });

            var wrongItem = ScriptableObject.CreateInstance<MountableItemDefinition>();
            wrongItem.SetValuesForTests("tool.scale", new[] { "tool.scale" }, null);

            try
            {
                var runtimeState = new WorkbenchRuntimeState(workbench);
                var evaluator = new WorkbenchCompatibilityEvaluator();
                Assert.That(runtimeState.TryGetSlotState("press-slot", out var pressSlotState), Is.True);

                var compatibility = evaluator.Evaluate(pressSlotState.Definition, wrongItem);
                var installed = runtimeState.TryInstall("press-slot", wrongItem);

                Assert.That(compatibility.IsCompatible, Is.False);
                Assert.That(compatibility.MissingRequiredTags, Does.Contain("press"));
                Assert.That(compatibility.DiagnosticCodes, Does.Contain("tags.missing-required"));
                Assert.That(installed, Is.False);
                Assert.That(pressSlotState.IsOccupied, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(workbench);
                Object.DestroyImmediate(wrongItem);
            }
        }

        private static string GetOperateReadinessDiagnostic(WorkbenchRuntimeState runtimeState)
        {
            if (!runtimeState.TryGetSlotState("press-slot", out var pressSlot) || !pressSlot.IsOccupied)
            {
                return "Missing required mount: press-slot";
            }

            if (!runtimeState.TryGetSlotState("die-slot", out var dieSlot) || !dieSlot.IsOccupied)
            {
                return "Missing required mount: die-slot";
            }

            return string.Empty;
        }
    }
}
