using System;
using System.Collections.Generic;
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
        private readonly List<VisualElement> _buySlots = new();
        private readonly List<VisualElement> _sellSlots = new();
        private readonly Dictionary<VisualElement, EventCallback<ClickEvent>> _buySlotCallbacks = new();
        private readonly Dictionary<VisualElement, EventCallback<ClickEvent>> _sellSlotCallbacks = new();

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
            BindBuySlots();
            BindSellSlots();
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
            RenderBuySlots(tradeState.BuySlots);
            RenderSellSlots(tradeState.SellSlots);
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

            UnbindBuySlots();
            UnbindSellSlots();
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

        private void BindBuySlots()
        {
            UnbindBuySlots();
            if (_buyPanel == null)
            {
                return;
            }

            var slotQuery = _buyPanel.Query<VisualElement>(className: "trade__cell");
            var slots = slotQuery.ToList();
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                _buySlots.Add(slot);
                EventCallback<ClickEvent> callback = _ =>
                {
                    var itemId = slot.userData as string;
                    if (!string.IsNullOrWhiteSpace(itemId))
                    {
                        IntentRaised?.Invoke(new UiIntent("trade.buy.slot", itemId));
                    }
                };

                _buySlotCallbacks[slot] = callback;
                slot.RegisterCallback<ClickEvent>(callback);
            }
        }

        private void UnbindBuySlots()
        {
            foreach (var kv in _buySlotCallbacks)
            {
                kv.Key.UnregisterCallback<ClickEvent>(kv.Value);
            }

            _buySlotCallbacks.Clear();
            _buySlots.Clear();
        }

        private void BindSellSlots()
        {
            UnbindSellSlots();
            if (_sellPanel == null)
            {
                return;
            }

            var slotQuery = _sellPanel.Query<VisualElement>(className: "trade__cell");
            var slots = slotQuery.ToList();
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                _sellSlots.Add(slot);
                EventCallback<ClickEvent> callback = _ =>
                {
                    var itemId = slot.userData as string;
                    if (!string.IsNullOrWhiteSpace(itemId))
                    {
                        IntentRaised?.Invoke(new UiIntent("trade.sell.slot", itemId));
                    }
                };

                _sellSlotCallbacks[slot] = callback;
                slot.RegisterCallback<ClickEvent>(callback);
            }
        }

        private void UnbindSellSlots()
        {
            foreach (var kv in _sellSlotCallbacks)
            {
                kv.Key.UnregisterCallback<ClickEvent>(kv.Value);
            }

            _sellSlotCallbacks.Clear();
            _sellSlots.Clear();
        }

        private void RenderBuySlots(IReadOnlyList<TradeUiSlotViewModel> slots)
        {
            RenderSlots(_buySlots, slots);
        }

        private void RenderSellSlots(IReadOnlyList<TradeUiSlotViewModel> slots)
        {
            RenderSlots(_sellSlots, slots);
        }

        private static void RenderSlots(IReadOnlyList<VisualElement> slotElements, IReadOnlyList<TradeUiSlotViewModel> slots)
        {
            if (slotElements == null)
            {
                return;
            }

            for (var i = 0; i < slotElements.Count; i++)
            {
                var slot = slotElements[i];
                var vm = slots != null && i < slots.Count ? slots[i] : null;
                slot.userData = vm?.ItemId;
                slot.SetEnabled(vm?.IsEnabled ?? false);
                slot.EnableInClassList("is-active", vm?.IsSelected ?? false);
                slot.EnableInClassList("trade__cell--empty", vm == null);

                var label = EnsureSlotLabel(slot);
                if (label != null)
                {
                    label.text = vm?.DisplayText ?? string.Empty;
                }
            }
        }

        private static Label EnsureSlotLabel(VisualElement slot)
        {
            if (slot == null)
            {
                return null;
            }

            var label = slot.Q<Label>("trade__cell-label");
            if (label != null)
            {
                return label;
            }

            label = new Label
            {
                name = "trade__cell-label"
            };
            label.AddToClassList("trade__cell-label");
            slot.Add(label);
            return label;
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
