using System;
using System.Collections.Generic;

namespace Reloader.Economy
{
    [Serializable]
    public sealed class TradeCartState
    {
        [Serializable]
        public sealed class LineItem
        {
            public string ItemId;
            public int Quantity;
            public int UnitPrice;

            public int TotalPrice => Math.Max(0, Quantity) * Math.Max(0, UnitPrice);
        }

        private readonly Dictionary<string, LineItem> _items = new Dictionary<string, LineItem>();

        public IReadOnlyCollection<LineItem> Items => _items.Values;

        public void Add(string itemId, int quantity, int unitPrice)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                return;
            }

            if (_items.TryGetValue(itemId, out var existing))
            {
                existing.Quantity += quantity;
                existing.UnitPrice = Math.Max(0, unitPrice);
                return;
            }

            _items[itemId] = new LineItem
            {
                ItemId = itemId,
                Quantity = quantity,
                UnitPrice = Math.Max(0, unitPrice)
            };
        }

        public void SetQuantity(string itemId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemId) || !_items.TryGetValue(itemId, out var item))
            {
                return;
            }

            if (quantity <= 0)
            {
                _items.Remove(itemId);
                return;
            }

            item.Quantity = quantity;
        }

        public void Remove(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            _items.Remove(itemId);
        }

        public int Total()
        {
            var total = 0;
            foreach (var item in _items.Values)
            {
                total += item.TotalPrice;
            }

            return Math.Max(0, total);
        }

        public void Clear()
        {
            _items.Clear();
        }
    }
}
