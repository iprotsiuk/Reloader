using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Toolkit.Trade
{
    public enum TradeUiTab
    {
        Buy,
        Sell
    }

    public sealed class TradeUiState : UiRenderState
    {
        public TradeUiState(TradeUiTab activeTab, bool isOrderScreenOpen, string cartTotalText, bool canConfirmBuy, bool canConfirmSell)
            : base("trade-ui")
        {
            ActiveTab = activeTab;
            IsOrderScreenOpen = isOrderScreenOpen;
            CartTotalText = string.IsNullOrWhiteSpace(cartTotalText) ? "$0" : cartTotalText;
            CanConfirmBuy = canConfirmBuy;
            CanConfirmSell = canConfirmSell;
        }

        public TradeUiTab ActiveTab { get; }
        public bool IsOrderScreenOpen { get; }
        public string CartTotalText { get; }
        public bool CanConfirmBuy { get; }
        public bool CanConfirmSell { get; }
    }
}
