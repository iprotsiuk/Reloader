using System;

namespace Reloader.Economy
{
    [Serializable]
    public sealed class TradeOrderState
    {
        public string DeliveryOptionId { get; private set; } = "inventory";
        public int DeliveryFee { get; private set; }

        public void SetDeliveryOption(string optionId, int fee)
        {
            DeliveryOptionId = string.IsNullOrWhiteSpace(optionId) ? "inventory" : optionId;
            DeliveryFee = Math.Max(0, fee);
        }

        public int FinalTotal(int cartTotal)
        {
            return Math.Max(0, cartTotal) + DeliveryFee;
        }
    }
}
