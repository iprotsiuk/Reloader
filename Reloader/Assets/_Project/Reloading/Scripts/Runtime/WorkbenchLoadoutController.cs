using System;
using System.Collections.Generic;
using System.Reflection;

namespace Reloader.Reloading.Runtime
{
    public sealed class WorkbenchLoadoutController
    {
        private readonly WorkbenchRuntimeState _runtimeState;
        private readonly WorkbenchCompatibilityEvaluator _compatibilityEvaluator;
        private readonly Dictionary<string, MountSlotState> _slotIndex;

        public WorkbenchLoadoutController(
            WorkbenchRuntimeState runtimeState,
            WorkbenchCompatibilityEvaluator compatibilityEvaluator)
        {
            _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            _compatibilityEvaluator = compatibilityEvaluator ?? throw new ArgumentNullException(nameof(compatibilityEvaluator));
            _slotIndex = ResolveSlotIndex(_runtimeState);
        }

        public WorkbenchRuntimeState RuntimeState => _runtimeState;

        public IReadOnlyDictionary<string, MountSlotState> SlotsById => _runtimeState.SlotsById;

        public bool TryInstall(string slotId, MountableItemDefinition item, out WorkbenchCompatibilityResult diagnostic)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                diagnostic = WorkbenchCompatibilityResult.Incompatible(null, null, new[] { "slot.invalid" });
                return false;
            }

            if (!_runtimeState.TryGetSlotState(slotId, out var slotState) || slotState == null)
            {
                diagnostic = WorkbenchCompatibilityResult.Incompatible(null, null, new[] { "slot.not-found" });
                return false;
            }

            if (slotState.IsOccupied)
            {
                diagnostic = WorkbenchCompatibilityResult.Incompatible(null, null, new[] { "slot.occupied" });
                return false;
            }

            diagnostic = _compatibilityEvaluator.Evaluate(slotState.Definition, item);
            if (!diagnostic.IsCompatible)
            {
                return false;
            }

            if (_runtimeState.TryInstall(slotId, item))
            {
                diagnostic = WorkbenchCompatibilityResult.Compatible();
                return true;
            }

            diagnostic = WorkbenchCompatibilityResult.Incompatible(null, null, new[] { "install.failed" });
            return false;
        }

        public bool TryUninstall(string slotId, out MountableItemDefinition removedItem, out string diagnosticCode)
        {
            removedItem = null;

            if (string.IsNullOrWhiteSpace(slotId))
            {
                diagnosticCode = "slot.invalid";
                return false;
            }

            if (!_runtimeState.TryGetSlotState(slotId, out var slotState) || slotState == null)
            {
                diagnosticCode = "slot.not-found";
                return false;
            }

            if (!slotState.IsOccupied || slotState.MountedNode == null)
            {
                diagnosticCode = "slot.empty";
                return false;
            }

            removedItem = slotState.MountedNode.ItemDefinition;
            RemoveMountedNodeRecursive(slotState.MountedNode);
            slotState.SetMountedNode(null);
            diagnosticCode = null;
            return true;
        }

        private void RemoveMountedNodeRecursive(MountNode node)
        {
            if (node == null)
            {
                return;
            }

            var childSlots = node.ChildSlots;
            for (var i = 0; i < childSlots.Count; i++)
            {
                var childSlot = childSlots[i];
                if (childSlot == null)
                {
                    continue;
                }

                if (childSlot.MountedNode != null)
                {
                    RemoveMountedNodeRecursive(childSlot.MountedNode);
                }

                childSlot.SetMountedNode(null);
                var childSlotId = childSlot.Definition?.SlotId;
                if (!string.IsNullOrWhiteSpace(childSlotId)
                    && _slotIndex.TryGetValue(childSlotId, out var existing)
                    && ReferenceEquals(existing, childSlot))
                {
                    _slotIndex.Remove(childSlotId);
                }
            }
        }

        private static Dictionary<string, MountSlotState> ResolveSlotIndex(WorkbenchRuntimeState state)
        {
            var field = typeof(WorkbenchRuntimeState).GetField("_slotsById", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException("WorkbenchRuntimeState slot index field not found.");
            }

            if (field.GetValue(state) is Dictionary<string, MountSlotState> slotsById)
            {
                return slotsById;
            }

            throw new InvalidOperationException("WorkbenchRuntimeState slot index field has unexpected type.");
        }
    }
}
