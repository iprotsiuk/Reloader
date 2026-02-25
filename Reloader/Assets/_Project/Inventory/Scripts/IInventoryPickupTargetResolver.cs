namespace Reloader.Inventory
{
    public interface IInventoryPickupTargetResolver
    {
        bool TryResolvePickupTarget(out IInventoryPickupTarget target);
    }
}
