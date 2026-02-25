namespace Reloader.NPCs.World
{
    public interface IShopVendorTarget
    {
        string VendorId { get; }
        void OnTradeOpened();
    }
}
