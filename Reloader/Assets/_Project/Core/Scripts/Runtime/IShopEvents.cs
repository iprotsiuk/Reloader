using System;
using Reloader.Core.Events;

namespace Reloader.Core.Runtime
{
    public interface IShopEvents
    {
        event Action<string> OnShopTradeOpenRequested;
        event Action<string> OnShopTradeOpened;
        event Action OnShopTradeClosed;
        event Action<string, int> OnShopBuyRequested;
        event Action<string, int> OnShopSellRequested;
        event Action<ShopCheckoutRequest> OnShopBuyCheckoutRequested;
        event Action<ShopCheckoutRequest> OnShopSellCheckoutRequested;
        event Action<ShopTradeResultPayload> OnShopTradeResultReceived;

        void RaiseShopTradeOpenRequested(string vendorId);
        void RaiseShopTradeOpened(string vendorId);
        void RaiseShopTradeClosed();
        void RaiseShopBuyRequested(string itemId, int quantity);
        void RaiseShopSellRequested(string itemId, int quantity);
        void RaiseShopBuyCheckoutRequested(ShopCheckoutRequest request);
        void RaiseShopSellCheckoutRequested(ShopCheckoutRequest request);
        void RaiseShopTradeResult(ShopTradeResultPayload payload);
    }
}
