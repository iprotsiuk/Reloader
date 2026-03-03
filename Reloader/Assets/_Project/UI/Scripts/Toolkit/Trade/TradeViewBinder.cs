using System;
using System.Collections.Generic;
using Reloader.UI;
using Reloader.UI.Toolkit;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;
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
        private VisualElement _tooltip;
        private Label _tooltipTitle;
        private Label _tooltipSpecs;
        private Button _tabBuyButton;
        private Button _tabSellButton;
        private Button _confirmBuyButton;
        private Button _confirmSellButton;
        private readonly List<VisualElement> _buySlots = new();
        private readonly List<VisualElement> _sellSlots = new();
        private readonly Dictionary<VisualElement, EventCallback<ClickEvent>> _buySlotCallbacks = new();
        private readonly Dictionary<VisualElement, EventCallback<ClickEvent>> _sellSlotCallbacks = new();
        private readonly Dictionary<VisualElement, EventCallback<PointerEnterEvent>> _buySlotEnterCallbacks = new();
        private readonly Dictionary<VisualElement, EventCallback<PointerMoveEvent>> _buySlotMoveCallbacks = new();
        private readonly Dictionary<VisualElement, EventCallback<PointerLeaveEvent>> _buySlotLeaveCallbacks = new();
        private readonly Dictionary<VisualElement, EventCallback<PointerEnterEvent>> _sellSlotEnterCallbacks = new();
        private readonly Dictionary<VisualElement, EventCallback<PointerMoveEvent>> _sellSlotMoveCallbacks = new();
        private readonly Dictionary<VisualElement, EventCallback<PointerLeaveEvent>> _sellSlotLeaveCallbacks = new();
        private InventoryItemTooltipPresenter _tooltipPresenter;
        private VisualElement _hoveredTooltipSlot;
        private string _hoveredTooltipItemId;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root)
        {
            UnbindButtons();
            _root = root;
            _buyPanel = root?.Q<VisualElement>("trade__buy-panel");
            _sellPanel = root?.Q<VisualElement>("trade__sell-panel");
            _orderPanel = root?.Q<VisualElement>("trade__order-panel");
            _cartTotal = root?.Q<Label>("trade__cart-total");
            _tooltip = root?.Q<VisualElement>("trade__tooltip");
            _tooltipTitle = root?.Q<Label>("trade__tooltip-title");
            _tooltipSpecs = root?.Q<Label>("trade__tooltip-specs");
            if (_tooltip == null && _root != null)
            {
                _tooltip = new VisualElement { name = "trade__tooltip" };
                _tooltip.AddToClassList("trade__tooltip");
                _tooltip.pickingMode = PickingMode.Ignore;
                _tooltipTitle = new Label { name = "trade__tooltip-title" };
                _tooltipTitle.AddToClassList("trade__tooltip-title");
                _tooltipSpecs = new Label { name = "trade__tooltip-specs" };
                _tooltipSpecs.AddToClassList("trade__tooltip-specs");
                _tooltip.Add(_tooltipTitle);
                _tooltip.Add(_tooltipSpecs);
                _root.Add(_tooltip);
            }
            _tooltipPresenter = InventoryItemTooltipPresenter.CreateOrBind(root, root, "trade");
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
            RevalidateHoveredTooltip();
            if (_root == null || !_root.visible)
            {
                HideTooltip();
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

                EventCallback<PointerEnterEvent> enter = evt => ShowTooltipForSlot(slot, evt.position);
                EventCallback<PointerMoveEvent> move = evt => ShowTooltipForSlot(slot, evt.position);
                EventCallback<PointerLeaveEvent> leave = _ => HideTooltip();
                _buySlotEnterCallbacks[slot] = enter;
                _buySlotMoveCallbacks[slot] = move;
                _buySlotLeaveCallbacks[slot] = leave;
                slot.RegisterCallback<PointerEnterEvent>(enter);
                slot.RegisterCallback<PointerMoveEvent>(move);
                slot.RegisterCallback<PointerLeaveEvent>(leave);
            }
        }

        private void UnbindBuySlots()
        {
            foreach (var kv in _buySlotCallbacks)
            {
                kv.Key.UnregisterCallback<ClickEvent>(kv.Value);
            }
            foreach (var kv in _buySlotEnterCallbacks)
            {
                kv.Key.UnregisterCallback<PointerEnterEvent>(kv.Value);
            }
            foreach (var kv in _buySlotMoveCallbacks)
            {
                kv.Key.UnregisterCallback<PointerMoveEvent>(kv.Value);
            }
            foreach (var kv in _buySlotLeaveCallbacks)
            {
                kv.Key.UnregisterCallback<PointerLeaveEvent>(kv.Value);
            }

            _buySlotCallbacks.Clear();
            _buySlotEnterCallbacks.Clear();
            _buySlotMoveCallbacks.Clear();
            _buySlotLeaveCallbacks.Clear();
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

                EventCallback<PointerEnterEvent> enter = evt => ShowTooltipForSlot(slot, evt.position);
                EventCallback<PointerMoveEvent> move = evt => ShowTooltipForSlot(slot, evt.position);
                EventCallback<PointerLeaveEvent> leave = _ => HideTooltip();
                _sellSlotEnterCallbacks[slot] = enter;
                _sellSlotMoveCallbacks[slot] = move;
                _sellSlotLeaveCallbacks[slot] = leave;
                slot.RegisterCallback<PointerEnterEvent>(enter);
                slot.RegisterCallback<PointerMoveEvent>(move);
                slot.RegisterCallback<PointerLeaveEvent>(leave);
            }
        }

        private void UnbindSellSlots()
        {
            foreach (var kv in _sellSlotCallbacks)
            {
                kv.Key.UnregisterCallback<ClickEvent>(kv.Value);
            }
            foreach (var kv in _sellSlotEnterCallbacks)
            {
                kv.Key.UnregisterCallback<PointerEnterEvent>(kv.Value);
            }
            foreach (var kv in _sellSlotMoveCallbacks)
            {
                kv.Key.UnregisterCallback<PointerMoveEvent>(kv.Value);
            }
            foreach (var kv in _sellSlotLeaveCallbacks)
            {
                kv.Key.UnregisterCallback<PointerLeaveEvent>(kv.Value);
            }

            _sellSlotCallbacks.Clear();
            _sellSlotEnterCallbacks.Clear();
            _sellSlotMoveCallbacks.Clear();
            _sellSlotLeaveCallbacks.Clear();
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
                slot.tooltip = vm?.DisplayText ?? string.Empty;
                slot.SetEnabled(vm?.IsEnabled ?? false);
                slot.EnableInClassList("is-active", vm?.IsSelected ?? false);
                slot.EnableInClassList("trade__cell--empty", vm == null);
                EnsureSlotVisual(slot, vm);
            }
        }

        private static void EnsureSlotVisual(VisualElement slot, TradeUiSlotViewModel vm)
        {
            if (slot == null)
            {
                return;
            }

            var icon = slot.Q<VisualElement>("trade__cell-icon");
            if (vm == null || string.IsNullOrWhiteSpace(vm.ItemId))
            {
                icon?.RemoveFromHierarchy();
                var staleLabel = slot.Q<Label>("trade__cell-label");
                staleLabel?.RemoveFromHierarchy();
                return;
            }

            if (icon == null)
            {
                icon = new VisualElement { name = "trade__cell-icon", pickingMode = PickingMode.Ignore };
                icon.AddToClassList("trade__cell-icon");
                slot.Add(icon);
            }

            icon.style.backgroundImage = ItemIconResolver.ResolveBackground(vm.ItemId);
            icon.EnableInClassList("is-missing", false);
        }

        public void SetVisible(bool isVisible)
        {
            if (_root == null)
            {
                return;
            }

            _root.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!isVisible)
            {
                HideTooltip();
            }
        }

        private void ShowTooltipForSlot(VisualElement slot, Vector2 panelPosition)
        {
            if (slot == null)
            {
                return;
            }

            var itemId = slot.userData as string;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                HideTooltip();
                return;
            }

            _hoveredTooltipSlot = slot;
            _hoveredTooltipItemId = itemId;
            var displayText = slot.tooltip;
            if (_tooltipPresenter != null)
            {
                _tooltipPresenter.TryShowItem(itemId, 1, 1, panelPosition, displayText);
            }
        }

        private void RevalidateHoveredTooltip()
        {
            if (_hoveredTooltipSlot == null)
            {
                return;
            }

            if (!IsElementEffectivelyVisibleAndEnabled(_hoveredTooltipSlot))
            {
                HideTooltip();
                return;
            }

            var currentItemId = _hoveredTooltipSlot.userData as string;
            if (string.IsNullOrWhiteSpace(currentItemId)
                || !string.Equals(currentItemId, _hoveredTooltipItemId, StringComparison.Ordinal))
            {
                HideTooltip();
            }
        }

        private static bool IsElementEffectivelyVisibleAndEnabled(VisualElement element)
        {
            if (element == null || element.panel == null || !element.enabledInHierarchy)
            {
                return false;
            }

            for (var current = element; current != null; current = current.parent)
            {
                if (!current.visible || current.resolvedStyle.display == DisplayStyle.None)
                {
                    return false;
                }
            }

            return true;
        }

        private void HideTooltip()
        {
            _hoveredTooltipSlot = null;
            _hoveredTooltipItemId = null;
            if (_tooltipPresenter != null)
            {
                _tooltipPresenter.Hide();
            }
            if (_tooltip != null)
            {
                _tooltip.style.display = DisplayStyle.None;
            }
        }
    }
}
