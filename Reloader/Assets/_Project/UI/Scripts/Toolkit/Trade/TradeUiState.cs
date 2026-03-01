using Reloader.UI.Toolkit.Contracts;
using System;
using System.Collections.Generic;

namespace Reloader.UI.Toolkit.Trade
{
    public enum TradeUiTab
    {
        Buy,
        Sell
    }

    public sealed class TradeUiState : UiRenderState
    {
        public TradeUiState(
            TradeUiTab activeTab,
            bool isOrderScreenOpen,
            string cartTotalText,
            bool canConfirmBuy,
            bool canConfirmSell,
            IReadOnlyList<TradeUiSlotViewModel> buySlots = null,
            IReadOnlyList<TradeUiSlotViewModel> sellSlots = null)
            : base("trade-ui")
        {
            ActiveTab = activeTab;
            IsOrderScreenOpen = isOrderScreenOpen;
            CartTotalText = string.IsNullOrWhiteSpace(cartTotalText) ? "$0" : cartTotalText;
            CanConfirmBuy = canConfirmBuy;
            CanConfirmSell = canConfirmSell;
            BuySlots = buySlots ?? Array.Empty<TradeUiSlotViewModel>();
            SellSlots = sellSlots ?? Array.Empty<TradeUiSlotViewModel>();
        }

        public TradeUiTab ActiveTab { get; }
        public bool IsOrderScreenOpen { get; }
        public string CartTotalText { get; }
        public bool CanConfirmBuy { get; }
        public bool CanConfirmSell { get; }
        public IReadOnlyList<TradeUiSlotViewModel> BuySlots { get; }
        public IReadOnlyList<TradeUiSlotViewModel> SellSlots { get; }
    }

    public sealed class TradeUiSlotViewModel
    {
        public TradeUiSlotViewModel(string itemId, string displayText, bool isEnabled, bool isSelected)
        {
            ItemId = itemId ?? string.Empty;
            DisplayText = string.IsNullOrWhiteSpace(displayText) ? "-" : displayText;
            IsEnabled = isEnabled;
            IsSelected = isSelected;
        }

        public string ItemId { get; }
        public string DisplayText { get; }
        public bool IsEnabled { get; }
        public bool IsSelected { get; }
    }
}
