using System;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.Trade
{
    public sealed class TradeViewBinder : IUiViewBinder
    {
        private VisualElement _root;
        private VisualElement _buyPanel;
        private VisualElement _sellPanel;
        private VisualElement _orderPanel;
        private Label _cartTotal;
        private Button _tabBuyButton;
        private Button _tabSellButton;
        private Button _confirmBuyButton;
        private Button _confirmSellButton;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root)
        {
            UnbindButtons();
            _root = root;
            _buyPanel = root?.Q<VisualElement>("trade__buy-panel");
            _sellPanel = root?.Q<VisualElement>("trade__sell-panel");
            _orderPanel = root?.Q<VisualElement>("trade__order-panel");
            _cartTotal = root?.Q<Label>("trade__cart-total");
            _tabBuyButton = root?.Q<Button>("trade__tab-buy");
            _tabSellButton = root?.Q<Button>("trade__tab-sell");
            _confirmBuyButton = root?.Q<Button>("trade__confirm-buy");
            _confirmSellButton = root?.Q<Button>("trade__confirm-sell");
            BindButtons();
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

            _confirmBuyButton?.SetEnabled(tradeState.CanConfirmBuy);
            _confirmSellButton?.SetEnabled(tradeState.CanConfirmSell);

            SetTabActive(_tabBuyButton, tradeState.ActiveTab == TradeUiTab.Buy);
            SetTabActive(_tabSellButton, tradeState.ActiveTab == TradeUiTab.Sell);
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

        private void BindButtons()
        {
            if (_tabBuyButton != null)
            {
                _tabBuyButton.clicked += OnTabBuyClicked;
            }

            if (_tabSellButton != null)
            {
                _tabSellButton.clicked += OnTabSellClicked;
            }

            if (_confirmBuyButton != null)
            {
                _confirmBuyButton.clicked += OnConfirmBuyClicked;
            }

            if (_confirmSellButton != null)
            {
                _confirmSellButton.clicked += OnConfirmSellClicked;
            }
        }

        private void UnbindButtons()
        {
            if (_tabBuyButton != null)
            {
                _tabBuyButton.clicked -= OnTabBuyClicked;
            }

            if (_tabSellButton != null)
            {
                _tabSellButton.clicked -= OnTabSellClicked;
            }

            if (_confirmBuyButton != null)
            {
                _confirmBuyButton.clicked -= OnConfirmBuyClicked;
            }

            if (_confirmSellButton != null)
            {
                _confirmSellButton.clicked -= OnConfirmSellClicked;
            }
        }

        private void OnTabBuyClicked()
        {
            IntentRaised?.Invoke(new UiIntent("trade.tab.buy"));
        }

        private void OnTabSellClicked()
        {
            IntentRaised?.Invoke(new UiIntent("trade.tab.sell"));
        }

        private void OnConfirmBuyClicked()
        {
            TryRaiseConfirmBuyIntent();
        }

        private void OnConfirmSellClicked()
        {
            TryRaiseConfirmSellIntent();
        }

        private static void SetTabActive(VisualElement tabButton, bool isActive)
        {
            tabButton?.EnableInClassList("is-active", isActive);
        }

        public void SetVisible(bool isVisible)
        {
            if (_root == null)
            {
                return;
            }

            _root.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
