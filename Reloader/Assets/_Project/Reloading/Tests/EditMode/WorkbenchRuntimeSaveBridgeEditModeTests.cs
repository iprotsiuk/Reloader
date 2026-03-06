using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;
using Reloader.Core.Save.Modules;
using Reloader.Reloading.Runtime;
using Reloader.Reloading.World;
using UnityEngine;

namespace Reloader.Reloading.Tests.EditMode
{
    public class WorkbenchRuntimeSaveBridgeEditModeTests
    {
        [Test]
        public void CaptureToModule_CapturesNestedMountedGraph()
        {
            var bridgeType = Type.GetType("Reloader.Reloading.Runtime.WorkbenchRuntimeSaveBridge, Assembly-CSharp");
            Assert.That(bridgeType, Is.Not.Null, "WorkbenchRuntimeSaveBridge type should exist.");

            var benchDefinition = CreateWorkbenchDefinition("bench.main", new MountSlotDefinition("press-slot", new[] { "cap.press" }));
            var pressItem = CreateItem("press.single", new[] { "cap.press" }, new[] { new MountSlotDefinition("die-slot", new[] { "cap.die" }) });
            var dieItem = CreateItem("die.full-length", new[] { "cap.die" }, childSlots: null);

            var benchGo = new GameObject("Bench");
            var benchTarget = benchGo.AddComponent<ReloadingBenchTarget>();
            benchTarget.SetWorkbenchDefinitionForTests(benchDefinition);

            var loadout = benchTarget.LoadoutController;
            Assert.That(loadout.TryInstall("press-slot", pressItem, out _), Is.True);
            Assert.That(loadout.TryInstall("die-slot", dieItem, out _), Is.True);

            var module = new WorkbenchLoadoutModule();
            var bridgeGo = new GameObject("WorkbenchRuntimeSaveBridge");
            var bridge = bridgeGo.AddComponent(bridgeType);
            SetPrivateField(bridgeType, bridge, "_benchTargets", new List<ReloadingBenchTarget> { benchTarget });

            InvokeMethod(bridgeType, bridge, "SetWorkbenchLoadoutModuleForRuntime", module);
            InvokeMethod(bridgeType, bridge, "CaptureToModule");

            Assert.That(module.Workbenches.Count, Is.EqualTo(1));
            Assert.That(module.Workbenches[0].WorkbenchId, Is.EqualTo("bench.main"));
            Assert.That(module.Workbenches[0].SlotNodes.Count, Is.EqualTo(1));

            var pressNode = module.Workbenches[0].SlotNodes[0];
            Assert.That(pressNode.SlotId, Is.EqualTo("press-slot"));
            Assert.That(pressNode.MountedItemId, Is.EqualTo("press.single"));
            Assert.That(pressNode.ChildSlots.Count, Is.EqualTo(1));
            Assert.That(pressNode.ChildSlots[0].SlotId, Is.EqualTo("press-slot/die-slot"));
            Assert.That(pressNode.ChildSlots[0].MountedItemId, Is.EqualTo("die.full-length"));
            UnityEngine.Object.DestroyImmediate(bridgeGo);
            UnityEngine.Object.DestroyImmediate(benchGo);
            UnityEngine.Object.DestroyImmediate(dieItem);
            UnityEngine.Object.DestroyImmediate(pressItem);
            UnityEngine.Object.DestroyImmediate(benchDefinition);
        }

        [Test]
        public void RestoreFromModule_RestoresNestedMountedGraph()
        {
            var bridgeType = Type.GetType("Reloader.Reloading.Runtime.WorkbenchRuntimeSaveBridge, Assembly-CSharp");
            Assert.That(bridgeType, Is.Not.Null, "WorkbenchRuntimeSaveBridge type should exist.");

            var benchDefinition = CreateWorkbenchDefinition("bench.main", new MountSlotDefinition("press-slot", new[] { "cap.press" }));
            var pressItem = CreateItem("press.single", new[] { "cap.press" }, new[] { new MountSlotDefinition("die-slot", new[] { "cap.die" }) });
            var dieItem = CreateItem("die.full-length", new[] { "cap.die" }, childSlots: null);

            var benchGo = new GameObject("Bench");
            var benchTarget = benchGo.AddComponent<ReloadingBenchTarget>();
            benchTarget.SetWorkbenchDefinitionForTests(benchDefinition);

            var module = new WorkbenchLoadoutModule();
            module.Workbenches.Add(new WorkbenchLoadoutModule.WorkbenchRecord
            {
                WorkbenchId = "bench.main",
                SlotNodes = new List<WorkbenchLoadoutModule.SlotNodeRecord>
                {
                    new WorkbenchLoadoutModule.SlotNodeRecord
                    {
                        SlotId = "press-slot",
                        MountedItemId = "press.single",
                        ChildSlots = new List<WorkbenchLoadoutModule.SlotNodeRecord>
                        {
                            new WorkbenchLoadoutModule.SlotNodeRecord
                            {
                                SlotId = "press-slot/die-slot",
                                MountedItemId = "die.full-length"
                            }
                        }
                    }
                }
            });

            var bridgeGo = new GameObject("WorkbenchRuntimeSaveBridge");
            var bridge = bridgeGo.AddComponent(bridgeType);
            SetPrivateField(bridgeType, bridge, "_benchTargets", new List<ReloadingBenchTarget> { benchTarget });
            SetPrivateField(bridgeType, bridge, "_mountableItemCatalog", new List<MountableItemDefinition> { pressItem, dieItem });

            InvokeMethod(bridgeType, bridge, "SetWorkbenchLoadoutModuleForRuntime", module);
            InvokeMethod(bridgeType, bridge, "RestoreFromModule");

            Assert.That(benchTarget.RuntimeState.TryGetSlotState("press-slot", out var pressSlot), Is.True);
            Assert.That(pressSlot.IsOccupied, Is.True);
            Assert.That(pressSlot.MountedNode.ItemDefinition, Is.SameAs(pressItem));
            Assert.That(benchTarget.RuntimeState.TryGetSlotState("die-slot", out var dieSlot), Is.True);
            Assert.That(dieSlot.IsOccupied, Is.True);
            Assert.That(dieSlot.MountedNode.ItemDefinition, Is.SameAs(dieItem));
            UnityEngine.Object.DestroyImmediate(bridgeGo);
            UnityEngine.Object.DestroyImmediate(benchGo);
            UnityEngine.Object.DestroyImmediate(dieItem);
            UnityEngine.Object.DestroyImmediate(pressItem);
            UnityEngine.Object.DestroyImmediate(benchDefinition);
        }

        [Test]
        public void SaveCoordinator_CaptureEnvelope_InvokesWorkbenchBridge_CapturesWorkbenchLoadoutModule()
        {
            var benchDefinition = CreateWorkbenchDefinition("bench.main", new MountSlotDefinition("press-slot", new[] { "cap.press" }));
            var pressItem = CreateItem("press.single", new[] { "cap.press" }, childSlots: null);

            var benchGo = new GameObject("Bench");
            var benchTarget = benchGo.AddComponent<ReloadingBenchTarget>();
            benchTarget.SetWorkbenchDefinitionForTests(benchDefinition);
            Assert.That(benchTarget.LoadoutController.TryInstall("press-slot", pressItem, out _), Is.True);

            var bridgeGo = new GameObject("WorkbenchRuntimeSaveBridge");
            var bridge = bridgeGo.AddComponent<WorkbenchRuntimeSaveBridge>();
            SetPrivateField(typeof(WorkbenchRuntimeSaveBridge), bridge, "_benchTargets", new List<ReloadingBenchTarget> { benchTarget });

            var module = new WorkbenchLoadoutModule();
            var coordinator = CreateCoordinator(module);

            var envelope = coordinator.CaptureEnvelope("0.5.0-dev");
            module.RestoreModuleStateFromJson(envelope.Modules["WorkbenchLoadout"].PayloadJson);

            Assert.That(module.Workbenches.Count, Is.EqualTo(1));
            Assert.That(module.Workbenches[0].WorkbenchId, Is.EqualTo("bench.main"));
            Assert.That(module.Workbenches[0].SlotNodes.Count, Is.EqualTo(1));
            Assert.That(module.Workbenches[0].SlotNodes[0].MountedItemId, Is.EqualTo("press.single"));

            UnityEngine.Object.DestroyImmediate(bridgeGo);
            UnityEngine.Object.DestroyImmediate(benchGo);
            UnityEngine.Object.DestroyImmediate(pressItem);
            UnityEngine.Object.DestroyImmediate(benchDefinition);
        }

        [Test]
        public void SaveCoordinator_Load_InvokesWorkbenchBridge_RestoresRuntimeFromWorkbenchLoadoutModule()
        {
            var benchDefinition = CreateWorkbenchDefinition("bench.main", new MountSlotDefinition("press-slot", new[] { "cap.press" }));
            var pressItem = CreateItem("press.single", new[] { "cap.press" }, childSlots: null);

            var benchGo = new GameObject("Bench");
            var benchTarget = benchGo.AddComponent<ReloadingBenchTarget>();
            benchTarget.SetWorkbenchDefinitionForTests(benchDefinition);

            var bridgeGo = new GameObject("WorkbenchRuntimeSaveBridge");
            var bridge = bridgeGo.AddComponent<WorkbenchRuntimeSaveBridge>();
            SetPrivateField(typeof(WorkbenchRuntimeSaveBridge), bridge, "_benchTargets", new List<ReloadingBenchTarget> { benchTarget });
            SetPrivateField(typeof(WorkbenchRuntimeSaveBridge), bridge, "_mountableItemCatalog", new List<MountableItemDefinition> { pressItem });

            var saveModule = new WorkbenchLoadoutModule();
            saveModule.Workbenches.Add(new WorkbenchLoadoutModule.WorkbenchRecord
            {
                WorkbenchId = "bench.main",
                SlotNodes = new List<WorkbenchLoadoutModule.SlotNodeRecord>
                {
                    new WorkbenchLoadoutModule.SlotNodeRecord
                    {
                        SlotId = "press-slot",
                        MountedItemId = "press.single"
                    }
                }
            });

            var tempDir = Path.Combine(Path.GetTempPath(), "reloader-workbench-orchestration-tests-" + Guid.NewGuid().ToString("N"));
            var savePath = Path.Combine(tempDir, "slot01.json");
            Directory.CreateDirectory(tempDir);

            try
            {
                var repository = new SaveFileRepository();
                repository.WriteEnvelope(savePath, new SaveEnvelope
                {
                    SchemaVersion = 1,
                    BuildVersion = "0.5.0-dev",
                    CreatedAtUtc = "2026-03-01T00:00:00Z",
                    Modules = new Dictionary<string, ModuleSaveBlock>
                    {
                        {
                            "WorkbenchLoadout",
                            new ModuleSaveBlock
                            {
                                ModuleVersion = 1,
                                PayloadJson = saveModule.CaptureModuleStateJson()
                            }
                        }
                    }
                });

                var loadModule = new WorkbenchLoadoutModule();
                var coordinator = CreateCoordinator(loadModule);
                coordinator.Load(savePath);

                Assert.That(benchTarget.RuntimeState.TryGetSlotState("press-slot", out var slotState), Is.True);
                Assert.That(slotState.IsOccupied, Is.True);
                Assert.That(slotState.MountedNode.ItemDefinition, Is.SameAs(pressItem));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }

            UnityEngine.Object.DestroyImmediate(bridgeGo);
            UnityEngine.Object.DestroyImmediate(benchGo);
            UnityEngine.Object.DestroyImmediate(pressItem);
            UnityEngine.Object.DestroyImmediate(benchDefinition);
        }

        [Test]
        public void RestoreFromModule_ClearsLiveBenchState_WhenBenchHasNoSaveRecord()
        {
            var benchDefinitionA = CreateWorkbenchDefinition("bench.a", new MountSlotDefinition("press-slot", new[] { "cap.press" }));
            var benchDefinitionB = CreateWorkbenchDefinition("bench.b", new MountSlotDefinition("press-slot", new[] { "cap.press" }));
            var pressItem = CreateItem("press.single", new[] { "cap.press" }, childSlots: null);

            var benchGoA = new GameObject("BenchA");
            var benchA = benchGoA.AddComponent<ReloadingBenchTarget>();
            benchA.SetWorkbenchDefinitionForTests(benchDefinitionA);
            Assert.That(benchA.LoadoutController.TryInstall("press-slot", pressItem, out _), Is.True);

            var benchGoB = new GameObject("BenchB");
            var benchB = benchGoB.AddComponent<ReloadingBenchTarget>();
            benchB.SetWorkbenchDefinitionForTests(benchDefinitionB);
            Assert.That(benchB.LoadoutController.TryInstall("press-slot", pressItem, out _), Is.True);

            var module = new WorkbenchLoadoutModule();
            module.Workbenches.Add(new WorkbenchLoadoutModule.WorkbenchRecord
            {
                WorkbenchId = "bench.a",
                SlotNodes = new List<WorkbenchLoadoutModule.SlotNodeRecord>()
            });

            var bridgeGo = new GameObject("WorkbenchRuntimeSaveBridge");
            var bridge = bridgeGo.AddComponent<WorkbenchRuntimeSaveBridge>();
            SetPrivateField(typeof(WorkbenchRuntimeSaveBridge), bridge, "_benchTargets", new List<ReloadingBenchTarget> { benchA, benchB });
            SetPrivateField(typeof(WorkbenchRuntimeSaveBridge), bridge, "_mountableItemCatalog", new List<MountableItemDefinition> { pressItem });

            bridge.SetWorkbenchLoadoutModuleForRuntime(module);
            bridge.RestoreFromModule();

            Assert.That(benchA.RuntimeState.TryGetSlotState("press-slot", out var slotA), Is.True);
            Assert.That(slotA.IsOccupied, Is.False);
            Assert.That(benchB.RuntimeState.TryGetSlotState("press-slot", out var slotB), Is.True);
            Assert.That(slotB.IsOccupied, Is.False);

            UnityEngine.Object.DestroyImmediate(bridgeGo);
            UnityEngine.Object.DestroyImmediate(benchGoA);
            UnityEngine.Object.DestroyImmediate(benchGoB);
            UnityEngine.Object.DestroyImmediate(pressItem);
            UnityEngine.Object.DestroyImmediate(benchDefinitionA);
            UnityEngine.Object.DestroyImmediate(benchDefinitionB);
        }

        [Test]
        public void CaptureToModule_Rediscovery_IncludesBenchTargetsSpawnedAfterInitialCapture()
        {
            var benchDefinitionA = CreateWorkbenchDefinition("bench.a", new MountSlotDefinition("press-slot", new[] { "cap.press" }));
            var benchDefinitionB = CreateWorkbenchDefinition("bench.b", new MountSlotDefinition("press-slot", new[] { "cap.press" }));
            var pressItem = CreateItem("press.single", new[] { "cap.press" }, childSlots: null);

            var benchGoA = new GameObject("BenchA");
            var benchA = benchGoA.AddComponent<ReloadingBenchTarget>();
            benchA.SetWorkbenchDefinitionForTests(benchDefinitionA);
            Assert.That(benchA.LoadoutController.TryInstall("press-slot", pressItem, out _), Is.True);

            var module = new WorkbenchLoadoutModule();
            var bridgeGo = new GameObject("WorkbenchRuntimeSaveBridge");
            var bridge = bridgeGo.AddComponent<WorkbenchRuntimeSaveBridge>();
            SetPrivateField(typeof(WorkbenchRuntimeSaveBridge), bridge, "_benchTargets", new List<ReloadingBenchTarget> { benchA });
            SetPrivateField(typeof(WorkbenchRuntimeSaveBridge), bridge, "_mountableItemCatalog", new List<MountableItemDefinition> { pressItem });
            bridge.SetWorkbenchLoadoutModuleForRuntime(module);

            bridge.CaptureToModule();
            Assert.That(module.Workbenches.Count, Is.EqualTo(1));

            var benchGoB = new GameObject("BenchB");
            var benchB = benchGoB.AddComponent<ReloadingBenchTarget>();
            benchB.SetWorkbenchDefinitionForTests(benchDefinitionB);
            Assert.That(benchB.LoadoutController.TryInstall("press-slot", pressItem, out _), Is.True);

            bridge.CaptureToModule();
            Assert.That(module.Workbenches.Count, Is.EqualTo(2));
            Assert.That(module.Workbenches.Exists(x => x.WorkbenchId == "bench.a"), Is.True);
            Assert.That(module.Workbenches.Exists(x => x.WorkbenchId == "bench.b"), Is.True);

            UnityEngine.Object.DestroyImmediate(bridgeGo);
            UnityEngine.Object.DestroyImmediate(benchGoA);
            UnityEngine.Object.DestroyImmediate(benchGoB);
            UnityEngine.Object.DestroyImmediate(pressItem);
            UnityEngine.Object.DestroyImmediate(benchDefinitionA);
            UnityEngine.Object.DestroyImmediate(benchDefinitionB);
        }

        private static WorkbenchDefinition CreateWorkbenchDefinition(string workbenchId, params MountSlotDefinition[] topSlots)
        {
            var definition = ScriptableObject.CreateInstance<WorkbenchDefinition>();
            definition.SetValuesForTests(workbenchId, topSlots);
            return definition;
        }

        private static MountableItemDefinition CreateItem(string itemId, IEnumerable<string> tags, IEnumerable<MountSlotDefinition> childSlots)
        {
            var item = ScriptableObject.CreateInstance<MountableItemDefinition>();
            item.SetValuesForTests(itemId, tags, childSlots);
            return item;
        }

        private static void SetPrivateField(Type type, object target, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {type?.Name}.");
            field.SetValue(target, value);
        }

        private static void InvokeMethod(Type type, object target, string methodName, params object[] arguments)
        {
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' on {type?.Name}.");
            method.Invoke(target, arguments);
        }

        private static SaveCoordinator CreateCoordinator(WorkbenchLoadoutModule module)
        {
            return new SaveCoordinator(
                new SaveFileRepository(),
                new[]
                {
                    new SaveModuleRegistration(0, module)
                },
                currentSchemaVersion: 1);
        }
    }
}
