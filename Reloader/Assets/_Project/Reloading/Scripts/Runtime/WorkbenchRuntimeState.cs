using System;
using System.Collections.Generic;

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
            return _slotsById.TryGetValue(slotId, out slotState);
        }

        public bool TryInstall(string slotId, MountableItemDefinition item)
        {
            if (!TryGetSlotState(slotId, out var slotState))
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

                if (!_slotsById.ContainsKey(definition.SlotId))
                {
                    _slotsById[definition.SlotId] = new MountSlotState(definition, ownerNode: null);
                }
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

                var childState = new MountSlotState(childDefinition, mountedNode);
                mountedNode.AddChildSlot(childState);

                if (!_slotsById.ContainsKey(childDefinition.SlotId))
                {
                    _slotsById.Add(childDefinition.SlotId, childState);
                }
            }
        }
    }
}
