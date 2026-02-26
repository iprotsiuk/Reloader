using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.Trade
{
    public sealed class TradeController : MonoBehaviour, IUiController
    {
        private TradeViewBinder _viewBinder;
        private IShopEvents _shopEvents;
        private IShopEvents _subscribedShopEvents;
        private bool _useRuntimeKernelShopEvents = true;
        private bool _isOpen;
        private TradeUiTab _activeTab = TradeUiTab.Buy;

        private void OnEnable()
        {
            SubscribeToShopEvents(ResolveShopEvents());
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeFromShopEvents();
        }

        public void Configure(IShopEvents shopEvents = null)
        {
            _useRuntimeKernelShopEvents = shopEvents == null;
            _shopEvents = shopEvents;
            if (isActiveAndEnabled)
            {
                SubscribeToShopEvents(ResolveShopEvents());
            }
        }

        public void SetViewBinder(TradeViewBinder binder)
        {
            _viewBinder = binder;
            Refresh();
        }

        public void HandleIntent(UiIntent intent)
        {
            if (intent.Key == "trade.confirm.buy")
            {
                _activeTab = TradeUiTab.Buy;
                if (TryResolveCheckoutRequest(intent.Payload, out var request))
                {
                    ResolveShopEvents()?.RaiseShopBuyCheckoutRequested(request);
                }
            }
            else if (intent.Key == "trade.confirm.sell")
            {
                _activeTab = TradeUiTab.Sell;
                if (TryResolveCheckoutRequest(intent.Payload, out var request))
                {
                    ResolveShopEvents()?.RaiseShopSellCheckoutRequested(request);
                }
            }

            Refresh();
        }

        public void Render(TradeUiState state)
        {
            _viewBinder?.Render(state);
        }

        private void HandleTradeOpened(string _)
        {
            _isOpen = true;
            Refresh();
        }

        private void HandleTradeClosed()
        {
            _isOpen = false;
            Refresh();
        }

        private void Refresh()
        {
            if (_viewBinder == null)
            {
                return;
            }

            _viewBinder.SetVisible(_isOpen);
            if (!_isOpen)
            {
                return;
            }

            Render(new TradeUiState(_activeTab, false, "$245", true, true));
        }

        private static bool TryResolveCheckoutRequest(object payload, out ShopCheckoutRequest request)
        {
            request = payload as ShopCheckoutRequest;
            return request?.Lines != null && request.Lines.Length > 0;
        }

        private IShopEvents ResolveShopEvents()
        {
            if (_useRuntimeKernelShopEvents)
            {
                var runtimeShopEvents = RuntimeKernelBootstrapper.ShopEvents;
                if (!ReferenceEquals(_shopEvents, runtimeShopEvents))
                {
                    _shopEvents = runtimeShopEvents;
                    SubscribeToShopEvents(_shopEvents);
                }
                else if (!ReferenceEquals(_subscribedShopEvents, _shopEvents))
                {
                    SubscribeToShopEvents(_shopEvents);
                }

                return _shopEvents;
            }

            if (!ReferenceEquals(_subscribedShopEvents, _shopEvents))
            {
                SubscribeToShopEvents(_shopEvents);
            }

            return _shopEvents;
        }

        private void SubscribeToShopEvents(IShopEvents shopEvents)
        {
            if (shopEvents == null)
            {
                UnsubscribeFromShopEvents();
                return;
            }

            if (ReferenceEquals(_subscribedShopEvents, shopEvents))
            {
                return;
            }

            UnsubscribeFromShopEvents();
            _subscribedShopEvents = shopEvents;
            _subscribedShopEvents.OnShopTradeOpened += HandleTradeOpened;
            _subscribedShopEvents.OnShopTradeClosed += HandleTradeClosed;
        }

        private void UnsubscribeFromShopEvents()
        {
            if (_subscribedShopEvents == null)
            {
                return;
            }

            _subscribedShopEvents.OnShopTradeOpened -= HandleTradeOpened;
            _subscribedShopEvents.OnShopTradeClosed -= HandleTradeClosed;
            _subscribedShopEvents = null;
        }
    }
}
