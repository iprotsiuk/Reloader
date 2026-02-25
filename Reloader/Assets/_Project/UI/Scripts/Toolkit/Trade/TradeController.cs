using Reloader.Core.Events;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.Trade
{
    public sealed class TradeController : MonoBehaviour, IUiController
    {
        private TradeViewBinder _viewBinder;
        private bool _isOpen;
        private TradeUiTab _activeTab = TradeUiTab.Buy;

        private void OnEnable()
        {
            GameEvents.OnShopTradeOpened += HandleTradeOpened;
            GameEvents.OnShopTradeClosed += HandleTradeClosed;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.OnShopTradeOpened -= HandleTradeOpened;
            GameEvents.OnShopTradeClosed -= HandleTradeClosed;
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
            }
            else if (intent.Key == "trade.confirm.sell")
            {
                _activeTab = TradeUiTab.Sell;
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
    }
}
