namespace Reloader.NPCs.World
{
    public interface IPlayerShopVendorResolver
    {
        bool TryResolveVendorTarget(out IShopVendorTarget target);
    }
}
