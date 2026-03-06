namespace Reloader.Core.Events
{
    public enum ShopTradeFailureReason
    {
        None = 0,
        NoActiveVendor = 1,
        UnknownItem = 2,
        InvalidQuantity = 3,
        InsufficientFunds = 4,
        InsufficientStock = 5,
        InsufficientPlayerQuantity = 6,
        InventoryFull = 7,
        Unrecognized = 255
    }

    public readonly struct ShopTradeResultPayload
    {
        public string ItemId { get; }
        public int Quantity { get; }
        public bool IsBuy { get; }
        public bool Success { get; }
        public ShopTradeFailureReason FailureReason { get; }

        public ShopTradeResultPayload(string itemId, int quantity, bool isBuy, bool success, ShopTradeFailureReason failureReason)
        {
            ItemId = itemId;
            Quantity = quantity;
            IsBuy = isBuy;
            Success = success;
            FailureReason = success ? ShopTradeFailureReason.None : failureReason;
        }
    }
}
