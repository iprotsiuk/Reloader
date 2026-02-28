using System;

namespace Reloader.Inventory
{
    public sealed class StorageContainerRuntime
    {
        private readonly string[] _slotItemIds;

        public string ContainerId { get; }
        public int SlotCount => _slotItemIds.Length;
        public StorageContainerPolicy Policy { get; }

        public StorageContainerRuntime(string containerId, int slotCount, StorageContainerPolicy policy)
        {
            ContainerId = string.IsNullOrWhiteSpace(containerId) ? string.Empty : containerId.Trim();
            Policy = policy;
            _slotItemIds = new string[Math.Max(0, slotCount)];
        }

        public string GetSlotItemId(int index)
        {
            return IsValidSlot(index) ? _slotItemIds[index] : null;
        }

        public bool IsValidSlot(int index)
        {
            return index >= 0 && index < _slotItemIds.Length;
        }

        public bool TrySetSlotItemId(int index, string itemId)
        {
            if (!IsValidSlot(index))
            {
                return false;
            }

            _slotItemIds[index] = string.IsNullOrWhiteSpace(itemId) ? null : itemId;
            return true;
        }
    }
}
