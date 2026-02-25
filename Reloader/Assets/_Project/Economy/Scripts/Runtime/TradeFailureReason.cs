namespace Reloader.Economy
{
    public enum TradeFailureReason
    {
        None = 0,
        NoActiveVendor = 1,
        UnknownItem = 2,
        InvalidQuantity = 3,
        InsufficientFunds = 4,
        InsufficientStock = 5,
        InsufficientPlayerQuantity = 6,
        InventoryFull = 7
    }
}
