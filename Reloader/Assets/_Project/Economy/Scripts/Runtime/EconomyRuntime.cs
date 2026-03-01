using System;
using System.Collections.Generic;

namespace Reloader.Economy
{
    public sealed class EconomyRuntime
    {
        private readonly Dictionary<string, Dictionary<string, int>> _vendorStocks = new Dictionary<string, Dictionary<string, int>>();
        private readonly Dictionary<string, ShopCatalogItemDefinition> _activeCatalogItems = new Dictionary<string, ShopCatalogItemDefinition>();
        private string _activeVendorId;

        public EconomyRuntime(int startingMoney = 500)
        {
            Money = Math.Max(0, startingMoney);
        }

        public int Money { get; private set; }
        public string ActiveVendorId => _activeVendorId;

        public bool OpenVendor(string vendorId, ShopCatalogDefinition catalog)
        {
            if (string.IsNullOrWhiteSpace(vendorId) || catalog == null)
            {
                return false;
            }

            _activeVendorId = vendorId;
            _activeCatalogItems.Clear();

            if (!_vendorStocks.TryGetValue(vendorId, out var stock))
            {
                stock = new Dictionary<string, int>();
                _vendorStocks[vendorId] = stock;
            }

            var items = catalog.Items;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    continue;
                }

                _activeCatalogItems[item.ItemId] = item;
                if (!stock.ContainsKey(item.ItemId))
                {
                    stock[item.ItemId] = item.StartingStock;
                }
            }

            return true;
        }

        public void CloseVendor()
        {
            _activeVendorId = null;
            _activeCatalogItems.Clear();
        }

        public bool TryGetUnitPrice(string itemId, out int unitPrice)
        {
            unitPrice = 0;
            if (!_activeCatalogItems.TryGetValue(itemId, out var item) || item == null)
            {
                return false;
            }

            unitPrice = item.UnitPrice;
            return true;
        }

        public bool TryGetStock(string itemId, out int stock)
        {
            stock = 0;
            if (string.IsNullOrWhiteSpace(_activeVendorId) || !_vendorStocks.TryGetValue(_activeVendorId, out var vendorStock))
            {
                return false;
            }

            return vendorStock.TryGetValue(itemId, out stock);
        }

        public IReadOnlyList<ShopCatalogItemDefinition> GetActiveCatalogItems()
        {
            if (_activeCatalogItems.Count == 0)
            {
                return Array.Empty<ShopCatalogItemDefinition>();
            }

            var items = new List<ShopCatalogItemDefinition>(_activeCatalogItems.Count);
            foreach (var item in _activeCatalogItems.Values)
            {
                if (item == null)
                {
                    continue;
                }

                items.Add(item);
            }

            return items;
        }

        public bool TryBuy(string itemId, int quantity, out int totalPrice, out TradeFailureReason reason)
        {
            totalPrice = 0;
            reason = TradeFailureReason.None;

            if (!TryValidateActiveItem(itemId, quantity, out var item, out var vendorStock, out reason))
            {
                return false;
            }

            if (!vendorStock.TryGetValue(itemId, out var stock) || stock < quantity)
            {
                reason = TradeFailureReason.InsufficientStock;
                return false;
            }

            totalPrice = item.UnitPrice * quantity;
            if (Money < totalPrice)
            {
                reason = TradeFailureReason.InsufficientFunds;
                return false;
            }

            vendorStock[itemId] = stock - quantity;
            Money -= totalPrice;
            return true;
        }

        public bool TrySell(string itemId, int quantity, out int totalPrice, out TradeFailureReason reason)
        {
            totalPrice = 0;
            reason = TradeFailureReason.None;

            if (!TryValidateActiveItem(itemId, quantity, out var item, out var vendorStock, out reason))
            {
                return false;
            }

            totalPrice = item.UnitPrice * quantity;
            vendorStock[itemId] = vendorStock[itemId] + quantity;
            Money += totalPrice;
            return true;
        }

        public bool TryBuyBatch(
            IReadOnlyList<(string itemId, int quantity)> lines,
            int deliveryFee,
            out int totalPrice,
            out TradeFailureReason reason)
        {
            totalPrice = 0;
            reason = TradeFailureReason.None;

            if (lines == null || lines.Count == 0)
            {
                reason = TradeFailureReason.InvalidQuantity;
                return false;
            }

            if (string.IsNullOrWhiteSpace(_activeVendorId) || !_vendorStocks.TryGetValue(_activeVendorId, out var vendorStock))
            {
                reason = TradeFailureReason.NoActiveVendor;
                return false;
            }

            var initialMoney = Money;
            var stockSnapshot = new Dictionary<string, int>(vendorStock);
            var computedTotal = Math.Max(0, deliveryFee);

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var bought = TryBuy(line.itemId, line.quantity, out var lineCost, out reason);
                if (!bought)
                {
                    RestoreSnapshot(vendorStock, stockSnapshot);
                    Money = initialMoney;
                    totalPrice = 0;
                    return false;
                }

                computedTotal += lineCost;
            }

            if (deliveryFee > 0)
            {
                if (Money < deliveryFee)
                {
                    RestoreSnapshot(vendorStock, stockSnapshot);
                    Money = initialMoney;
                    totalPrice = 0;
                    reason = TradeFailureReason.InsufficientFunds;
                    return false;
                }

                Money -= deliveryFee;
            }

            totalPrice = computedTotal;
            reason = TradeFailureReason.None;
            return true;
        }

        public bool TrySellBatch(
            IReadOnlyList<(string itemId, int quantity)> lines,
            out int totalPrice,
            out TradeFailureReason reason)
        {
            totalPrice = 0;
            reason = TradeFailureReason.None;

            if (lines == null || lines.Count == 0)
            {
                reason = TradeFailureReason.InvalidQuantity;
                return false;
            }

            if (string.IsNullOrWhiteSpace(_activeVendorId) || !_vendorStocks.TryGetValue(_activeVendorId, out var vendorStock))
            {
                reason = TradeFailureReason.NoActiveVendor;
                return false;
            }

            var initialMoney = Money;
            var stockSnapshot = new Dictionary<string, int>(vendorStock);

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var sold = TrySell(line.itemId, line.quantity, out var linePrice, out reason);
                if (!sold)
                {
                    RestoreSnapshot(vendorStock, stockSnapshot);
                    Money = initialMoney;
                    totalPrice = 0;
                    return false;
                }

                totalPrice += linePrice;
            }

            reason = TradeFailureReason.None;
            return true;
        }

        private bool TryValidateActiveItem(
            string itemId,
            int quantity,
            out ShopCatalogItemDefinition item,
            out Dictionary<string, int> vendorStock,
            out TradeFailureReason reason)
        {
            item = null;
            vendorStock = null;
            reason = TradeFailureReason.None;

            if (quantity <= 0)
            {
                reason = TradeFailureReason.InvalidQuantity;
                return false;
            }

            if (string.IsNullOrWhiteSpace(_activeVendorId) || !_vendorStocks.TryGetValue(_activeVendorId, out vendorStock))
            {
                reason = TradeFailureReason.NoActiveVendor;
                return false;
            }

            if (!_activeCatalogItems.TryGetValue(itemId, out item) || item == null)
            {
                reason = TradeFailureReason.UnknownItem;
                return false;
            }

            if (!vendorStock.ContainsKey(itemId))
            {
                reason = TradeFailureReason.UnknownItem;
                return false;
            }

            return true;
        }

        private static void RestoreSnapshot(Dictionary<string, int> vendorStock, Dictionary<string, int> snapshot)
        {
            vendorStock.Clear();
            foreach (var kv in snapshot)
            {
                vendorStock[kv.Key] = kv.Value;
            }
        }
    }
}
