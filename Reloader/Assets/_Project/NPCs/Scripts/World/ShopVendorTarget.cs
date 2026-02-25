using UnityEngine;

namespace Reloader.NPCs.World
{
    public sealed class ShopVendorTarget : MonoBehaviour, IShopVendorTarget
    {
        [SerializeField] private string _vendorId = "vendor-reloading-store";

        public string VendorId => _vendorId;
        public int OpenCount { get; private set; }

        public void OnTradeOpened()
        {
            OpenCount++;
        }
    }
}
