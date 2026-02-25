using System;

namespace Reloader.Inventory
{
    public sealed class ItemStackState
    {
        public string ItemId { get; }
        public int Quantity { get; private set; }
        public int MaxStack { get; }

        public ItemStackState(string itemId, int quantity, int maxStack)
        {
            ItemId = itemId;
            Quantity = Math.Max(1, quantity);
            MaxStack = Math.Max(1, maxStack);
        }

        public void SetQuantity(int quantity)
        {
            Quantity = Math.Max(1, quantity);
        }
    }
}
