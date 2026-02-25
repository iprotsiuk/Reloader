using System;
using System.Collections.Generic;
using Reloader.Core.Events;
using Reloader.Inventory;
using UnityEngine;

namespace Reloader.Economy
{
    public sealed class EconomyController : MonoBehaviour
    {
        [Serializable]
        private sealed class VendorCatalogBinding
        {
            [SerializeField] private string _vendorId;
            [SerializeField] private ShopCatalogDefinition _catalog;

            public string VendorId => _vendorId;
            public ShopCatalogDefinition Catalog => _catalog;
        }

        [SerializeField] private MonoBehaviour _inventoryControllerBehaviour;
        [SerializeField] private int _startingMoney = 500;
        [SerializeField] private List<VendorCatalogBinding> _vendors = new List<VendorCatalogBinding>();

        private PlayerInventoryController _inventoryController;
        private EconomyRuntime _runtime;

        public EconomyRuntime Runtime => _runtime;

        private void Awake()
        {
            _runtime = new EconomyRuntime(_startingMoney);
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            GameEvents.OnShopTradeOpenRequested += HandleTradeOpenRequested;
            GameEvents.OnShopBuyRequested += HandleBuyRequested;
            GameEvents.OnShopSellRequested += HandleSellRequested;
            GameEvents.OnShopBuyCheckoutRequested += HandleBuyCheckoutRequested;
            GameEvents.OnShopSellCheckoutRequested += HandleSellCheckoutRequested;
            GameEvents.RaiseMoneyChanged(_runtime.Money);
        }

        private void OnDisable()
        {
            GameEvents.OnShopTradeOpenRequested -= HandleTradeOpenRequested;
            GameEvents.OnShopBuyRequested -= HandleBuyRequested;
            GameEvents.OnShopSellRequested -= HandleSellRequested;
            GameEvents.OnShopBuyCheckoutRequested -= HandleBuyCheckoutRequested;
            GameEvents.OnShopSellCheckoutRequested -= HandleSellCheckoutRequested;
        }

        private void HandleTradeOpenRequested(string vendorId)
        {
            if (!TryGetCatalog(vendorId, out var catalog) || !_runtime.OpenVendor(vendorId, catalog))
            {
                GameEvents.RaiseShopTradeResult(string.Empty, 0, true, false, TradeFailureReason.NoActiveVendor.ToString());
                return;
            }

            GameEvents.RaiseShopTradeOpened(vendorId);
            GameEvents.RaiseMoneyChanged(_runtime.Money);
        }

        private void HandleBuyRequested(string itemId, int quantity)
        {
            ResolveReferences();
            if (_inventoryController?.Runtime == null)
            {
                GameEvents.RaiseShopTradeResult(itemId, quantity, true, false, TradeFailureReason.InventoryFull.ToString());
                return;
            }

            if (!_inventoryController.Runtime.CanAcceptStackItem(itemId))
            {
                GameEvents.RaiseShopTradeResult(itemId, quantity, true, false, TradeFailureReason.InventoryFull.ToString());
                return;
            }

            var purchased = _runtime.TryBuy(itemId, quantity, out _, out var reason);
            if (!purchased)
            {
                GameEvents.RaiseShopTradeResult(itemId, quantity, true, false, reason.ToString());
                return;
            }

            var stored = _inventoryController.Runtime.TryAddStackItem(itemId, quantity, out _, out _, out _);
            if (!stored)
            {
                _runtime.TrySell(itemId, quantity, out _, out _);
                GameEvents.RaiseShopTradeResult(itemId, quantity, true, false, TradeFailureReason.InventoryFull.ToString());
                return;
            }

            GameEvents.RaiseMoneyChanged(_runtime.Money);
            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseShopTradeResult(itemId, quantity, true, true, string.Empty);
        }

        private void HandleSellRequested(string itemId, int quantity)
        {
            ResolveReferences();
            if (_inventoryController?.Runtime == null)
            {
                GameEvents.RaiseShopTradeResult(itemId, quantity, false, false, TradeFailureReason.InsufficientPlayerQuantity.ToString());
                return;
            }

            if (_inventoryController.Runtime.GetItemQuantity(itemId) < quantity || quantity <= 0)
            {
                GameEvents.RaiseShopTradeResult(itemId, quantity, false, false, TradeFailureReason.InsufficientPlayerQuantity.ToString());
                return;
            }

            var sold = _runtime.TrySell(itemId, quantity, out _, out var reason);
            if (!sold)
            {
                GameEvents.RaiseShopTradeResult(itemId, quantity, false, false, reason.ToString());
                return;
            }

            var removed = _inventoryController.Runtime.TryRemoveStackItem(itemId, quantity);
            if (!removed)
            {
                _runtime.TryBuy(itemId, quantity, out _, out _);
                GameEvents.RaiseShopTradeResult(itemId, quantity, false, false, TradeFailureReason.InsufficientPlayerQuantity.ToString());
                return;
            }

            GameEvents.RaiseMoneyChanged(_runtime.Money);
            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseShopTradeResult(itemId, quantity, false, true, string.Empty);
        }

        private void HandleBuyCheckoutRequested(ShopCheckoutRequest request)
        {
            ResolveReferences();
            if (_inventoryController?.Runtime == null || request == null || request.Lines == null || request.Lines.Length == 0)
            {
                GameEvents.RaiseShopTradeResult(string.Empty, 0, true, false, TradeFailureReason.InvalidQuantity.ToString());
                return;
            }

            var lines = new List<(string itemId, int quantity)>();
            for (var i = 0; i < request.Lines.Length; i++)
            {
                var line = request.Lines[i];
                if (line.Quantity <= 0 || string.IsNullOrWhiteSpace(line.ItemId))
                {
                    GameEvents.RaiseShopTradeResult(string.Empty, 0, true, false, TradeFailureReason.InvalidQuantity.ToString());
                    return;
                }

                if (!_inventoryController.Runtime.CanAcceptStackItem(line.ItemId))
                {
                    GameEvents.RaiseShopTradeResult(line.ItemId, line.Quantity, true, false, TradeFailureReason.InventoryFull.ToString());
                    return;
                }

                lines.Add((line.ItemId, line.Quantity));
            }

            if (!_runtime.TryBuyBatch(lines, request.DeliveryFee, out _, out var buyReason))
            {
                GameEvents.RaiseShopTradeResult(string.Empty, 0, true, false, buyReason.ToString());
                return;
            }

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var stored = _inventoryController.Runtime.TryAddStackItem(line.itemId, line.quantity, out _, out _, out _);
                if (stored)
                {
                    continue;
                }

                _runtime.TrySellBatch(lines, out _, out _);
                GameEvents.RaiseShopTradeResult(line.itemId, line.quantity, true, false, TradeFailureReason.InventoryFull.ToString());
                return;
            }

            GameEvents.RaiseMoneyChanged(_runtime.Money);
            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseShopTradeResult(string.Empty, 0, true, true, string.Empty);
        }

        private void HandleSellCheckoutRequested(ShopCheckoutRequest request)
        {
            ResolveReferences();
            if (_inventoryController?.Runtime == null || request == null || request.Lines == null || request.Lines.Length == 0)
            {
                GameEvents.RaiseShopTradeResult(string.Empty, 0, false, false, TradeFailureReason.InvalidQuantity.ToString());
                return;
            }

            var lines = new List<(string itemId, int quantity)>();
            for (var i = 0; i < request.Lines.Length; i++)
            {
                var line = request.Lines[i];
                if (line.Quantity <= 0 || string.IsNullOrWhiteSpace(line.ItemId))
                {
                    GameEvents.RaiseShopTradeResult(string.Empty, 0, false, false, TradeFailureReason.InvalidQuantity.ToString());
                    return;
                }

                if (_inventoryController.Runtime.GetItemQuantity(line.ItemId) < line.Quantity)
                {
                    GameEvents.RaiseShopTradeResult(line.ItemId, line.Quantity, false, false, TradeFailureReason.InsufficientPlayerQuantity.ToString());
                    return;
                }

                lines.Add((line.ItemId, line.Quantity));
            }

            if (!_runtime.TrySellBatch(lines, out _, out var sellReason))
            {
                GameEvents.RaiseShopTradeResult(string.Empty, 0, false, false, sellReason.ToString());
                return;
            }

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var removed = _inventoryController.Runtime.TryRemoveStackItem(line.itemId, line.quantity);
                if (removed)
                {
                    continue;
                }

                _runtime.TryBuyBatch(lines, 0, out _, out _);
                GameEvents.RaiseShopTradeResult(line.itemId, line.quantity, false, false, TradeFailureReason.InsufficientPlayerQuantity.ToString());
                return;
            }

            GameEvents.RaiseMoneyChanged(_runtime.Money);
            GameEvents.RaiseInventoryChanged();
            GameEvents.RaiseShopTradeResult(string.Empty, 0, false, true, string.Empty);
        }

        private bool TryGetCatalog(string vendorId, out ShopCatalogDefinition catalog)
        {
            catalog = null;
            for (var i = 0; i < _vendors.Count; i++)
            {
                var binding = _vendors[i];
                if (binding == null || binding.Catalog == null || binding.VendorId != vendorId)
                {
                    continue;
                }

                catalog = binding.Catalog;
                return true;
            }

            return false;
        }

        private void ResolveReferences()
        {
            _inventoryController ??= _inventoryControllerBehaviour as PlayerInventoryController;
            if (_inventoryController != null)
            {
                return;
            }

            _inventoryController = FindFirstObjectByType<PlayerInventoryController>();
        }
    }
}
