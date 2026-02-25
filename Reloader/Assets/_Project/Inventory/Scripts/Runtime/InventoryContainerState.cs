namespace Reloader.Inventory
{
    public sealed class InventoryContainerState
    {
        private readonly ItemStackState[] _slots;

        public InventoryContainerType ContainerType { get; }
        public ContainerPermissions Permissions { get; }
        public int SlotCount => _slots.Length;

        public InventoryContainerState(InventoryContainerType containerType, int slotCount, ContainerPermissions permissions)
        {
            ContainerType = containerType;
            Permissions = permissions;
            _slots = new ItemStackState[slotCount < 0 ? 0 : slotCount];
        }

        public bool TryGetSlot(int index, out ItemStackState stack)
        {
            if (!IsValidIndex(index) || _slots[index] == null)
            {
                stack = null;
                return false;
            }

            stack = _slots[index];
            return true;
        }

        public bool TrySetSlot(int index, ItemStackState stack)
        {
            if (!IsValidIndex(index))
            {
                return false;
            }

            _slots[index] = stack;
            return true;
        }

        public bool TryClearSlot(int index)
        {
            if (!IsValidIndex(index))
            {
                return false;
            }

            _slots[index] = null;
            return true;
        }

        public bool IsValidIndex(int index)
        {
            return index >= 0 && index < _slots.Length;
        }
    }
}
