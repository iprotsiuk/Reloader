using System;

namespace Reloader.Inventory
{
    public sealed class StorageContainerRuntime
    {
        private readonly ItemStackState[] _slots;

        public string ContainerId { get; }
        public int SlotCount => _slots.Length;
        public StorageContainerPolicy Policy { get; }

        public StorageContainerRuntime(string containerId, int slotCount, StorageContainerPolicy policy)
        {
            ContainerId = string.IsNullOrWhiteSpace(containerId) ? string.Empty : containerId.Trim();
            Policy = policy;
            _slots = new ItemStackState[Math.Max(0, slotCount)];
        }

        public string GetSlotItemId(int index)
        {
            return TryGetSlotStack(index, out var stack) ? stack!.ItemId : null;
        }

        public int GetSlotQuantity(int index)
        {
            return TryGetSlotStack(index, out var stack) ? stack!.Quantity : 0;
        }

        public int GetSlotMaxStack(int index)
        {
            return TryGetSlotStack(index, out var stack) ? stack!.MaxStack : 0;
        }

        public bool IsValidSlot(int index)
        {
            return index >= 0 && index < _slots.Length;
        }

        public bool TrySetSlotItemId(int index, string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return TrySetSlotStack(index, null);
            }

            return TrySetSlotStack(index, new ItemStackState(itemId, 1, 1));
        }

        public bool TryGetSlotStack(int index, out ItemStackState stack)
        {
            if (!IsValidSlot(index) || _slots[index] == null)
            {
                stack = null;
                return false;
            }

            var source = _slots[index];
            stack = new ItemStackState(source.ItemId, source.Quantity, source.MaxStack);
            return true;
        }

        public bool TrySetSlotStack(int index, ItemStackState stack)
        {
            if (!IsValidSlot(index))
            {
                return false;
            }

            if (stack == null || string.IsNullOrWhiteSpace(stack.ItemId))
            {
                _slots[index] = null;
                return true;
            }

            _slots[index] = new ItemStackState(stack.ItemId, stack.Quantity, stack.MaxStack);
            return true;
        }
    }
}
