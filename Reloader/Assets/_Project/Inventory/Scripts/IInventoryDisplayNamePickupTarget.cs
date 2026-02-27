namespace Reloader.Inventory
{
    public interface IInventoryDisplayNamePickupTarget : IInventoryPickupTarget
    {
        string DisplayName { get; }
    }
}
