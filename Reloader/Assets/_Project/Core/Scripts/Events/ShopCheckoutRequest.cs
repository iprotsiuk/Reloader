using System;

namespace Reloader.Core.Events
{
    [Serializable]
    public readonly struct ShopCheckoutLine
    {
        public string ItemId { get; }
        public int Quantity { get; }

        public ShopCheckoutLine(string itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }
    }

    [Serializable]
    public sealed class ShopCheckoutRequest
    {
        public ShopCheckoutLine[] Lines { get; }
        public string DeliveryOptionId { get; }
        public int DeliveryFee { get; }

        public ShopCheckoutRequest(ShopCheckoutLine[] lines, string deliveryOptionId, int deliveryFee)
        {
            Lines = lines ?? Array.Empty<ShopCheckoutLine>();
            DeliveryOptionId = string.IsNullOrWhiteSpace(deliveryOptionId) ? "inventory" : deliveryOptionId;
            DeliveryFee = Math.Max(0, deliveryFee);
        }
    }
}
