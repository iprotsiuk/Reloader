using System;
using System.Collections.Generic;
using Reloader.Core.Save;
using Reloader.Core.Save.Modules;
using Reloader.Reloading.World;
using UnityEngine;

namespace Reloader.Reloading.Runtime
{
    public sealed class WorkbenchRuntimeSaveBridge : MonoBehaviour, ISaveRuntimeBridge
    {
        [SerializeField] private List<ReloadingBenchTarget> _benchTargets = new List<ReloadingBenchTarget>();
        [SerializeField] private List<MountableItemDefinition> _mountableItemCatalog = new List<MountableItemDefinition>();
        [SerializeField] private bool _logWarnings;

        private WorkbenchLoadoutModule _workbenchLoadoutModule;

        private void OnEnable()
        {
            SaveRuntimeBridgeRegistry.Register(this);
        }

        private void OnDisable()
        {
            SaveRuntimeBridgeRegistry.Unregister(this);
        }

        public void SetWorkbenchLoadoutModuleForRuntime(WorkbenchLoadoutModule workbenchLoadoutModule)
        {
            _workbenchLoadoutModule = workbenchLoadoutModule;
        }

        public void PrepareForSave(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            SetWorkbenchLoadoutModuleForRuntime(ResolveWorkbenchLoadoutModule(moduleRegistrations));
            CaptureToModule();
        }

        public void FinalizeAfterLoad(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            SetWorkbenchLoadoutModuleForRuntime(ResolveWorkbenchLoadoutModule(moduleRegistrations));
            RestoreFromModule();
        }

        public void CaptureToModule()
        {
            if (!ResolveDependencies())
            {
                return;
            }

            _workbenchLoadoutModule.Workbenches.Clear();

            var targets = ResolveBenchTargets();
            var seenWorkbenchIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var runtimeState = target?.RuntimeState;
                var definition = runtimeState?.WorkbenchDefinition;
                var workbenchId = definition?.WorkbenchId;
                if (string.IsNullOrWhiteSpace(workbenchId))
                {
                    continue;
                }

                if (!seenWorkbenchIds.Add(workbenchId))
                {
                    if (_logWarnings)
                    {
                        Debug.LogWarning($"WorkbenchRuntimeSaveBridge encountered duplicate workbenchId '{workbenchId}' during capture.", this);
                    }

                    continue;
                }

                _workbenchLoadoutModule.Workbenches.Add(new WorkbenchLoadoutModule.WorkbenchRecord
                {
                    WorkbenchId = workbenchId,
                    SlotNodes = CaptureTopLevelSlots(runtimeState, definition)
                });
            }
        }

        public void RestoreFromModule()
        {
            if (!ResolveDependencies())
            {
                return;
            }

            var targetsByWorkbenchId = BuildTargetIndexByWorkbenchId(ResolveBenchTargets());
            var itemIndex = BuildMountableItemIndex(targetsByWorkbenchId);
            ClearAllResolvedWorkbenches(targetsByWorkbenchId);

            for (var i = 0; i < _workbenchLoadoutModule.Workbenches.Count; i++)
            {
                var workbench = _workbenchLoadoutModule.Workbenches[i];
                if (workbench == null || string.IsNullOrWhiteSpace(workbench.WorkbenchId))
                {
                    continue;
                }

                if (!targetsByWorkbenchId.TryGetValue(workbench.WorkbenchId, out var target) || target == null)
                {
                    continue;
                }

                var loadoutController = target.LoadoutController;
                var runtimeState = target.RuntimeState;
                var definition = runtimeState?.WorkbenchDefinition;
                if (loadoutController == null || runtimeState == null || definition == null)
                {
                    continue;
                }

                if (workbench.SlotNodes == null)
                {
                    continue;
                }

                for (var j = 0; j < workbench.SlotNodes.Count; j++)
                {
                    RestoreSlotNode(loadoutController, workbench.SlotNodes[j], itemIndex);
                }
            }
        }

        private bool ResolveDependencies()
        {
            var ready = _workbenchLoadoutModule != null;
            if (!ready && _logWarnings)
            {
                Debug.LogWarning("WorkbenchRuntimeSaveBridge requires WorkbenchLoadoutModule before capture/restore.", this);
            }

            return ready;
        }

        private static WorkbenchLoadoutModule ResolveWorkbenchLoadoutModule(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            if (moduleRegistrations == null)
            {
                return null;
            }

            for (var i = 0; i < moduleRegistrations.Count; i++)
            {
                if (moduleRegistrations[i]?.Module is WorkbenchLoadoutModule module)
                {
                    return module;
                }
            }

            return null;
        }

        private List<ReloadingBenchTarget> ResolveBenchTargets()
        {
            if (_benchTargets == null)
            {
                _benchTargets = new List<ReloadingBenchTarget>();
            }

            _benchTargets.RemoveAll(target => target == null);

            var discovered = FindObjectsByType<ReloadingBenchTarget>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
            for (var i = 0; i < discovered.Length; i++)
            {
                var target = discovered[i];
                if (target == null || _benchTargets.Contains(target))
                {
                    continue;
                }

                _benchTargets.Add(target);
            }

            return _benchTargets;
        }

        private static void ClearAllResolvedWorkbenches(IReadOnlyDictionary<string, ReloadingBenchTarget> targetsByWorkbenchId)
        {
            foreach (var kvp in targetsByWorkbenchId)
            {
                var target = kvp.Value;
                var loadoutController = target?.LoadoutController;
                var runtimeState = target?.RuntimeState;
                var definition = runtimeState?.WorkbenchDefinition;
                if (loadoutController == null || runtimeState == null || definition == null)
                {
                    continue;
                }

                ClearTopLevelSlots(loadoutController, runtimeState, definition);
            }
        }

        private static List<WorkbenchLoadoutModule.SlotNodeRecord> CaptureTopLevelSlots(WorkbenchRuntimeState runtimeState, WorkbenchDefinition definition)
        {
            var slotNodes = new List<WorkbenchLoadoutModule.SlotNodeRecord>();
            var topLevelSlots = definition.TopLevelSlots;
            if (topLevelSlots == null)
            {
                return slotNodes;
            }

            for (var i = 0; i < topLevelSlots.Count; i++)
            {
                var topSlotDefinition = topLevelSlots[i];
                var slotId = topSlotDefinition?.SlotId;
                if (string.IsNullOrWhiteSpace(slotId))
                {
                    continue;
                }

                if (!runtimeState.TryGetSlotState(slotId, out var slotState) || slotState == null)
                {
                    continue;
                }

                var slotNode = CaptureSlotNode(slotState);
                if (slotNode != null)
                {
                    slotNodes.Add(slotNode);
                }
            }

            return slotNodes;
        }

        private static WorkbenchLoadoutModule.SlotNodeRecord CaptureSlotNode(MountSlotState slotState)
        {
            var slotId = slotState.GraphSlotId;
            if (string.IsNullOrWhiteSpace(slotId))
            {
                return null;
            }

            var mountedNode = slotState.MountedNode;
            var mountedItemId = mountedNode?.ItemDefinition?.ItemId;
            if (mountedNode == null || string.IsNullOrWhiteSpace(mountedItemId))
            {
                return null;
            }

            var record = new WorkbenchLoadoutModule.SlotNodeRecord
            {
                SlotId = slotId,
                MountedItemId = mountedItemId,
                ChildSlots = new List<WorkbenchLoadoutModule.SlotNodeRecord>()
            };

            var childSlots = mountedNode.ChildSlots;
            for (var i = 0; i < childSlots.Count; i++)
            {
                var childRecord = CaptureSlotNode(childSlots[i]);
                if (childRecord != null)
                {
                    record.ChildSlots.Add(childRecord);
                }
            }

            return record;
        }

        private static Dictionary<string, ReloadingBenchTarget> BuildTargetIndexByWorkbenchId(IReadOnlyList<ReloadingBenchTarget> targets)
        {
            var targetsByWorkbenchId = new Dictionary<string, ReloadingBenchTarget>(StringComparer.Ordinal);
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var workbenchId = target?.RuntimeState?.WorkbenchDefinition?.WorkbenchId;
                if (string.IsNullOrWhiteSpace(workbenchId) || targetsByWorkbenchId.ContainsKey(workbenchId))
                {
                    continue;
                }

                targetsByWorkbenchId.Add(workbenchId, target);
            }

            return targetsByWorkbenchId;
        }

        private Dictionary<string, MountableItemDefinition> BuildMountableItemIndex(Dictionary<string, ReloadingBenchTarget> targetsByWorkbenchId)
        {
            var itemsById = new Dictionary<string, MountableItemDefinition>(StringComparer.Ordinal);

            for (var i = 0; i < _mountableItemCatalog.Count; i++)
            {
                RegisterItem(itemsById, _mountableItemCatalog[i]);
            }

            var loadedDefinitions = Resources.FindObjectsOfTypeAll<MountableItemDefinition>();
            for (var i = 0; i < loadedDefinitions.Length; i++)
            {
                RegisterItem(itemsById, loadedDefinitions[i]);
            }

            foreach (var kvp in targetsByWorkbenchId)
            {
                RegisterMountedItems(itemsById, kvp.Value?.RuntimeState?.WorkbenchDefinition, kvp.Value?.RuntimeState);
            }

            return itemsById;
        }

        private static void RegisterMountedItems(
            IDictionary<string, MountableItemDefinition> itemsById,
            WorkbenchDefinition definition,
            WorkbenchRuntimeState runtimeState)
        {
            var topLevelSlots = definition?.TopLevelSlots;
            if (topLevelSlots == null || runtimeState == null)
            {
                return;
            }

            for (var i = 0; i < topLevelSlots.Count; i++)
            {
                var topSlotId = topLevelSlots[i]?.SlotId;
                if (string.IsNullOrWhiteSpace(topSlotId))
                {
                    continue;
                }

                if (!runtimeState.TryGetSlotState(topSlotId, out var slotState) || slotState?.MountedNode == null)
                {
                    continue;
                }

                RegisterMountedNodeRecursive(itemsById, slotState.MountedNode);
            }
        }

        private static void RegisterMountedNodeRecursive(IDictionary<string, MountableItemDefinition> itemsById, MountNode mountedNode)
        {
            if (mountedNode == null)
            {
                return;
            }

            RegisterItem(itemsById, mountedNode.ItemDefinition);

            var childSlots = mountedNode.ChildSlots;
            for (var i = 0; i < childSlots.Count; i++)
            {
                RegisterMountedNodeRecursive(itemsById, childSlots[i]?.MountedNode);
            }
        }

        private static void RegisterItem(IDictionary<string, MountableItemDefinition> itemsById, MountableItemDefinition definition)
        {
            var itemId = definition?.ItemId;
            if (string.IsNullOrWhiteSpace(itemId) || itemsById.ContainsKey(itemId))
            {
                return;
            }

            itemsById.Add(itemId, definition);
        }

        private static void ClearTopLevelSlots(
            WorkbenchLoadoutController loadoutController,
            WorkbenchRuntimeState runtimeState,
            WorkbenchDefinition definition)
        {
            var topLevelSlots = definition.TopLevelSlots;
            if (topLevelSlots == null)
            {
                return;
            }

            for (var i = 0; i < topLevelSlots.Count; i++)
            {
                var topSlotId = topLevelSlots[i]?.SlotId;
                if (string.IsNullOrWhiteSpace(topSlotId))
                {
                    continue;
                }

                if (!runtimeState.TryGetSlotState(topSlotId, out var topSlotState)
                    || topSlotState == null
                    || !topSlotState.IsOccupied)
                {
                    continue;
                }

                loadoutController.TryUninstall(topSlotId, out _, out _);
            }
        }

        private static void RestoreSlotNode(
            WorkbenchLoadoutController loadoutController,
            WorkbenchLoadoutModule.SlotNodeRecord slotNode,
            IReadOnlyDictionary<string, MountableItemDefinition> itemsById)
        {
            if (slotNode == null || string.IsNullOrWhiteSpace(slotNode.SlotId) || string.IsNullOrWhiteSpace(slotNode.MountedItemId))
            {
                return;
            }

            if (!itemsById.TryGetValue(slotNode.MountedItemId, out var itemDefinition) || itemDefinition == null)
            {
                return;
            }

            if (!loadoutController.TryInstall(slotNode.SlotId, itemDefinition, out _))
            {
                return;
            }

            if (slotNode.ChildSlots == null)
            {
                return;
            }

            for (var i = 0; i < slotNode.ChildSlots.Count; i++)
            {
                RestoreSlotNode(loadoutController, slotNode.ChildSlots[i], itemsById);
            }
        }
    }
}
