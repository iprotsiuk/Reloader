using System;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.Trade
{
    public sealed class TradeViewBinder : IUiViewBinder
    {
        private VisualElement _buyPanel;
        private VisualElement _sellPanel;
        private VisualElement _orderPanel;
        private Label _cartTotal;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root)
        {
            _buyPanel = root?.Q<VisualElement>("trade__buy-panel");
            _sellPanel = root?.Q<VisualElement>("trade__sell-panel");
            _orderPanel = root?.Q<VisualElement>("trade__order-panel");
            _cartTotal = root?.Q<Label>("trade__cart-total");
        }

        public void Render(UiRenderState state)
        {
            if (state is not TradeUiState tradeState)
            {
                return;
            }

            if (_buyPanel != null)
            {
                _buyPanel.style.display = tradeState.ActiveTab == TradeUiTab.Buy ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_sellPanel != null)
            {
                _sellPanel.style.display = tradeState.ActiveTab == TradeUiTab.Sell ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_orderPanel != null)
            {
                _orderPanel.style.display = tradeState.IsOrderScreenOpen ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_cartTotal != null)
            {
                _cartTotal.text = tradeState.CartTotalText;
            }
        }

        public bool TryRaiseConfirmBuyIntent()
        {
            IntentRaised?.Invoke(new UiIntent("trade.confirm.buy"));
            return true;
        }

        public bool TryRaiseConfirmSellIntent()
        {
            IntentRaised?.Invoke(new UiIntent("trade.confirm.sell"));
            return true;
        }
    }
}
