using NUnit.Framework;
using Reloader.Reloading.Runtime;
using Reloader.Reloading.World;
using UnityEngine;

namespace Reloader.Reloading.Tests.PlayMode
{
    public class WorkbenchLoadoutControllerPlayModeTests
    {
        [Test]
        public void InstallRejectsIncompatibleItem_WithDiagnosticReason()
        {
            var benchDefinition = CreateWorkbenchDefinition(
                "bench.main",
                new MountSlotDefinition("press-slot", requiredTags: new[] { "cap.press" }));

            var runtimeState = new WorkbenchRuntimeState(benchDefinition);
            var controller = new WorkbenchLoadoutController(runtimeState, new WorkbenchCompatibilityEvaluator());
            var incompatible = CreateItem("tool.scale", "cap.scale");

            var installed = controller.TryInstall("press-slot", incompatible, out var diagnostic);

            Assert.That(installed, Is.False);
            Assert.That(diagnostic.IsCompatible, Is.False);
            Assert.That(diagnostic.DiagnosticCodes, Does.Contain("tags.missing-required"));
            Assert.That(diagnostic.MissingRequiredTags, Does.Contain("cap.press"));

            Object.DestroyImmediate(benchDefinition);
            Object.DestroyImmediate(incompatible);
        }

        [Test]
        public void UninstallOccupiedSlot_RemovesMountedItem()
        {
            var benchDefinition = CreateWorkbenchDefinition(
                "bench.main",
                new MountSlotDefinition("press-slot", requiredTags: new[] { "cap.press" }));

            var runtimeState = new WorkbenchRuntimeState(benchDefinition);
            var controller = new WorkbenchLoadoutController(runtimeState, new WorkbenchCompatibilityEvaluator());
            var press = CreateItem("press.single", "cap.press");

            Assert.That(controller.TryInstall("press-slot", press, out var installDiagnostic), Is.True);
            Assert.That(installDiagnostic.IsCompatible, Is.True);

            var uninstalled = controller.TryUninstall("press-slot", out var removedItem, out var uninstallDiagnosticCode);

            Assert.That(uninstalled, Is.True);
            Assert.That(removedItem, Is.SameAs(press));
            Assert.That(uninstallDiagnosticCode, Is.Null.Or.Empty);
            Assert.That(runtimeState.TryGetSlotState("press-slot", out var slotState), Is.True);
            Assert.That(slotState.IsOccupied, Is.False);

            Object.DestroyImmediate(benchDefinition);
            Object.DestroyImmediate(press);
        }

        [Test]
        public void UninstallEmptySlot_ReturnsDiagnosticCode()
        {
            var benchDefinition = CreateWorkbenchDefinition(
                "bench.main",
                new MountSlotDefinition("press-slot", requiredTags: new[] { "cap.press" }));

            var runtimeState = new WorkbenchRuntimeState(benchDefinition);
            var controller = new WorkbenchLoadoutController(runtimeState, new WorkbenchCompatibilityEvaluator());

            var uninstalled = controller.TryUninstall("press-slot", out var removedItem, out var diagnosticCode);

            Assert.That(uninstalled, Is.False);
            Assert.That(removedItem, Is.Null);
            Assert.That(diagnosticCode, Is.EqualTo("slot.empty"));

            Object.DestroyImmediate(benchDefinition);
        }

        [Test]
        public void ReloadingBenchTarget_ExposesLoadoutController_WhenWorkbenchDefinitionAssigned()
        {
            var benchDefinition = CreateWorkbenchDefinition(
                "bench.main",
                new MountSlotDefinition("press-slot", requiredTags: new[] { "cap.press" }));

            var go = new GameObject("Bench");
            var target = go.AddComponent<ReloadingBenchTarget>();
            target.SetWorkbenchDefinitionForTests(benchDefinition);

            var loadoutController = target.LoadoutController;

            Assert.That(loadoutController, Is.Not.Null);
            Assert.That(target.RuntimeState, Is.Not.Null);
            Assert.That(target.RuntimeState.WorkbenchDefinition, Is.SameAs(benchDefinition));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(benchDefinition);
        }

        private static WorkbenchDefinition CreateWorkbenchDefinition(string workbenchId, params MountSlotDefinition[] slots)
        {
            var definition = ScriptableObject.CreateInstance<WorkbenchDefinition>();
            definition.SetValuesForTests(workbenchId, slots);
            return definition;
        }

        private static MountableItemDefinition CreateItem(string itemId, params string[] tags)
        {
            var item = ScriptableObject.CreateInstance<MountableItemDefinition>();
            item.SetValuesForTests(itemId, tags, childSlots: null);
            return item;
        }
    }
}
