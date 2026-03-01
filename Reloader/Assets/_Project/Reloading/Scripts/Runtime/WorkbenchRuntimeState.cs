using System;
using System.Collections.Generic;
using System.Linq;

namespace Reloader.Reloading.Runtime
{
    public sealed class WorkbenchRuntimeState
    {
        private readonly Dictionary<string, MountSlotState> _slotsById = new Dictionary<string, MountSlotState>(StringComparer.Ordinal);

        public WorkbenchRuntimeState(WorkbenchDefinition workbenchDefinition)
        {
            WorkbenchDefinition = workbenchDefinition;
            InitializeTopLevelSlots();
        }

        public WorkbenchDefinition WorkbenchDefinition { get; }

        public IReadOnlyDictionary<string, MountSlotState> SlotsById => _slotsById;

        public bool TryGetSlotState(string slotId, out MountSlotState slotState)
        {
            if (_slotsById.TryGetValue(slotId, out slotState))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(slotId))
            {
                slotState = null;
                return false;
            }

            // Backward-compatible lookup for callers that still pass raw definition slot IDs.
            var matches = _slotsById.Values
                .Where(state => string.Equals(state.Definition?.SlotId, slotId, StringComparison.Ordinal))
                .ToArray();

            if (matches.Length == 1)
            {
                slotState = matches[0];
                return true;
            }

            slotState = null;
            return false;
        }

        public bool TryInstall(string slotId, MountableItemDefinition item)
        {
            if (!_slotsById.TryGetValue(slotId, out var slotState))
            {
                return false;
            }

            if (slotState.IsOccupied || item == null)
            {
                return false;
            }

            if (!slotState.Definition.CanAccept(item))
            {
                return false;
            }

            var mountedNode = new MountNode(Guid.NewGuid().ToString("N"), item, slotState);
            slotState.SetMountedNode(mountedNode);
            AddChildSlots(mountedNode, item);
            return true;
        }

        private void InitializeTopLevelSlots()
        {
            if (WorkbenchDefinition == null || WorkbenchDefinition.TopLevelSlots == null)
            {
                return;
            }

            for (var i = 0; i < WorkbenchDefinition.TopLevelSlots.Count; i++)
            {
                var definition = WorkbenchDefinition.TopLevelSlots[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.SlotId))
                {
                    continue;
                }

                var graphSlotId = BuildTopLevelGraphSlotId(definition.SlotId);
                _slotsById[graphSlotId] = new MountSlotState(definition, ownerNode: null, graphSlotId);
            }
        }

        private void AddChildSlots(MountNode mountedNode, MountableItemDefinition item)
        {
            var childSlots = item.ChildSlots;
            if (childSlots == null)
            {
                return;
            }

            for (var i = 0; i < childSlots.Count; i++)
            {
                var childDefinition = childSlots[i];
                if (childDefinition == null || string.IsNullOrWhiteSpace(childDefinition.SlotId))
                {
                    continue;
                }

                var graphSlotId = BuildChildGraphSlotId(mountedNode, childDefinition.SlotId);
                var childState = new MountSlotState(childDefinition, mountedNode, graphSlotId);
                mountedNode.AddChildSlot(childState);
                _slotsById[graphSlotId] = childState;
            }
        }

        private static string BuildTopLevelGraphSlotId(string slotId)
        {
            return slotId ?? string.Empty;
        }

        private static string BuildChildGraphSlotId(MountNode ownerNode, string slotId)
        {
            if (ownerNode == null)
            {
                return slotId ?? string.Empty;
            }

            return $"{ownerNode.NodeId}/{slotId}";
        }
    }
}
