namespace Reloader.Inventory
{
    public interface IInventoryStackPickupTarget : IInventoryPickupTarget
    {
        int Quantity { get; }
    }
}
