using System;
using System.Collections.Generic;

namespace Reloader.Reloading.Runtime
{
    public sealed class MountNode
    {
        private readonly List<MountSlotState> _childSlots = new List<MountSlotState>();

        public MountNode(string nodeId, MountableItemDefinition itemDefinition, MountSlotState parentSlot)
        {
            NodeId = string.IsNullOrWhiteSpace(nodeId) ? Guid.NewGuid().ToString("N") : nodeId;
            ItemDefinition = itemDefinition;
            ParentSlot = parentSlot;
        }

        public string NodeId { get; }

        public MountableItemDefinition ItemDefinition { get; }

        public MountSlotState ParentSlot { get; }

        public IReadOnlyList<MountSlotState> ChildSlots => _childSlots;

        public void AddChildSlot(MountSlotState slotState)
        {
            _childSlots.Add(slotState);
        }
    }
}
