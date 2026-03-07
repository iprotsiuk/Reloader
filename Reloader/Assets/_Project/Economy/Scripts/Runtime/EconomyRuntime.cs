using System;
using System.Collections.Generic;

namespace Reloader.Economy
{
    public sealed class EconomyRuntime
    {
        public sealed class StateSnapshot
        {
            internal StateSnapshot(
                int money,
                string activeVendorId,
                Dictionary<string, Dictionary<string, int>> vendorStocks,
                Dictionary<string, ShopCatalogItemDefinition> activeCatalogItems)
            {
                Money = money;
                ActiveVendorId = activeVendorId;
                VendorStocks = vendorStocks;
                ActiveCatalogItems = activeCatalogItems;
            }

            internal int Money { get; }
            internal string ActiveVendorId { get; }
            internal Dictionary<string, Dictionary<string, int>> VendorStocks { get; }
            internal Dictionary<string, ShopCatalogItemDefinition> ActiveCatalogItems { get; }
        }

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

        public StateSnapshot CaptureStateSnapshot()
        {
            return new StateSnapshot(
                Money,
                _activeVendorId,
                CloneVendorStocks(_vendorStocks),
                new Dictionary<string, ShopCatalogItemDefinition>(_activeCatalogItems));
        }

        public void RestoreStateSnapshot(StateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            Money = Math.Max(0, snapshot.Money);
            _activeVendorId = snapshot.ActiveVendorId;

            _vendorStocks.Clear();
            foreach (var vendorEntry in snapshot.VendorStocks)
            {
                _vendorStocks[vendorEntry.Key] = new Dictionary<string, int>(vendorEntry.Value);
            }

            _activeCatalogItems.Clear();
            foreach (var activeItem in snapshot.ActiveCatalogItems)
            {
                _activeCatalogItems[activeItem.Key] = activeItem.Value;
            }
        }

public bool AwardMoney(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            Money += amount;
            return true;
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

            if (string.IsNullOrWhiteSpace(_activeVendorId) || !_vendorStocks.TryGetValue(_activeVendorId, out _))
            {
                reason = TradeFailureReason.NoActiveVendor;
                return false;
            }

            var stateSnapshot = CaptureStateSnapshot();
            var computedTotal = Math.Max(0, deliveryFee);

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var bought = TryBuy(line.itemId, line.quantity, out var lineCost, out reason);
                if (!bought)
                {
                    RestoreStateSnapshot(stateSnapshot);
                    totalPrice = 0;
                    return false;
                }

                computedTotal += lineCost;
            }

            if (deliveryFee > 0)
            {
                if (Money < deliveryFee)
                {
                    RestoreStateSnapshot(stateSnapshot);
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

            var stateSnapshot = CaptureStateSnapshot();

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var sold = TrySell(line.itemId, line.quantity, out var linePrice, out reason);
                if (!sold)
                {
                    RestoreStateSnapshot(stateSnapshot);
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

        private static Dictionary<string, Dictionary<string, int>> CloneVendorStocks(
            Dictionary<string, Dictionary<string, int>> source)
        {
            var clone = new Dictionary<string, Dictionary<string, int>>(source.Count);
            foreach (var vendorEntry in source)
            {
                clone[vendorEntry.Key] = new Dictionary<string, int>(vendorEntry.Value);
            }

            return clone;
        }
    }
}
