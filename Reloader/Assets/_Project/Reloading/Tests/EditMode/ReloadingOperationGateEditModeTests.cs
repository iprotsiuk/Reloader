using NUnit.Framework;
using Reloader.Reloading.Runtime;
using UnityEngine;

namespace Reloader.Reloading.Tests.EditMode
{
    public class ReloadingOperationGateEditModeTests
    {
        [Test]
        public void SeatBullet_Disabled_WhenRequiredPressOrDieMissing()
        {
            var workbench = CreateWorkbenchDefinition(
                "bench.main",
                new MountSlotDefinition("press-slot"),
                new MountSlotDefinition("die-slot"));

            var runtimeState = new WorkbenchRuntimeState(workbench);
            var loadout = new WorkbenchLoadoutController(runtimeState, new WorkbenchCompatibilityEvaluator());
            var gate = new ReloadingOperationGate(loadout);
            var press = CreateItem("press.single", "cap.press");

            Assert.That(loadout.TryInstall("press-slot", press, out _), Is.True);

            var allowed = gate.IsOperationAllowed(ReloadingOperationType.SeatBullet, out var blocked);

            Assert.That(allowed, Is.False);
            Assert.That(blocked.DiagnosticCode, Is.EqualTo("gate.missing-capabilities"));
            Assert.That(blocked.MissingCapabilities, Does.Contain("cap.die.seat"));

            var seatDie = CreateItem("die.seater", "cap.die.seat");
            Assert.That(loadout.TryInstall("die-slot", seatDie, out _), Is.True);

            var allowedAfterMount = gate.IsOperationAllowed(ReloadingOperationType.SeatBullet, out var open);

            Assert.That(allowedAfterMount, Is.True);
            Assert.That(open.DiagnosticCode, Is.Null.Or.Empty);

            Object.DestroyImmediate(workbench);
            Object.DestroyImmediate(press);
            Object.DestroyImmediate(seatDie);
        }

        [Test]
        public void TryApply_WhenGateBlocksOperation_ReturnsFailureWithoutStateMutation()
        {
            var workbench = CreateWorkbenchDefinition(
                "bench.main",
                new MountSlotDefinition("press-slot"),
                new MountSlotDefinition("die-slot"));

            var runtimeState = new WorkbenchRuntimeState(workbench);
            var loadout = new WorkbenchLoadoutController(runtimeState, new WorkbenchCompatibilityEvaluator());
            var gate = new ReloadingOperationGate(loadout);
            var flow = new ReloadingFlowController
            {
                OperationGate = gate
            };

            var result = flow.TryApply(ReloadingOperationType.SeatBullet);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("gate.missing-capabilities"));
            Assert.That(flow.SessionState.BulletSeated, Is.False);

            Object.DestroyImmediate(workbench);
        }

        [Test]
        public void InspectCase_AllowedWithoutAnyMountedCapabilities()
        {
            var workbench = CreateWorkbenchDefinition("bench.main", new MountSlotDefinition("utility-slot"));
            var runtimeState = new WorkbenchRuntimeState(workbench);
            var loadout = new WorkbenchLoadoutController(runtimeState, new WorkbenchCompatibilityEvaluator());
            var gate = new ReloadingOperationGate(loadout);

            var allowed = gate.IsOperationAllowed(ReloadingOperationType.InspectCase, out var status);

            Assert.That(allowed, Is.True);
            Assert.That(status.IsAllowed, Is.True);
            Assert.That(status.DiagnosticCode, Is.Null.Or.Empty);

            Object.DestroyImmediate(workbench);
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
