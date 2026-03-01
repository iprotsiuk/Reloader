using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
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
            Assert.That(pressNode.ChildSlots[0].SlotId, Is.EqualTo("die-slot"));
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
                                SlotId = "die-slot",
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
    }
}
