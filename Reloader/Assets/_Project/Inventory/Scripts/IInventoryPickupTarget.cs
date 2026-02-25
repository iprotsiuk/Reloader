namespace Reloader.Inventory
{
    public interface IInventoryPickupTarget
    {
        string ItemId { get; }
        void OnPickedUp();
    }
}
