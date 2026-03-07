using System;
using System.Collections.Generic;
using Reloader.Core;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
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
        [SerializeField] private string _defaultVendorId = "vendor-reloading-store";
        [SerializeField] private ShopCatalogDefinition _defaultVendorCatalog;

        private PlayerInventoryController _inventoryController;
        private EconomyRuntime _runtime;
        private IShopEvents _shopEvents;
        private IInventoryEvents _inventoryEvents;
        private IShopEvents _subscribedShopEvents;
        private bool _useRuntimeKernelShopEvents = true;
        private bool _useRuntimeKernelInventoryEvents = true;
        private bool _attemptedInventoryResolution;
        private bool _loggedMissingInventoryController;

        public EconomyRuntime Runtime => _runtime;

        private void Awake()
        {
            _runtime = new EconomyRuntime(_startingMoney);
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToRuntimeHubReconfigure();
            SubscribeToShopEvents(ResolveShopEvents());
            ResolveInventoryEvents()?.RaiseMoneyChanged(_runtime.Money);
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            UnsubscribeFromShopEvents();
        }

        public void Configure(IShopEvents shopEvents = null, IInventoryEvents inventoryEvents = null)
        {
            _useRuntimeKernelShopEvents = shopEvents == null;
            _shopEvents = shopEvents;
            _useRuntimeKernelInventoryEvents = inventoryEvents == null;
            _inventoryEvents = inventoryEvents;
            if (isActiveAndEnabled)
            {
                SubscribeToShopEvents(ResolveShopEvents());
                ResolveInventoryEvents();
            }
        }

public bool TryAwardMoney(int amount)
        {
            if (_runtime == null || !_runtime.AwardMoney(amount))
            {
                return false;
            }

            ResolveInventoryEvents()?.RaiseMoneyChanged(_runtime.Money);
            return true;
        }


        private void HandleTradeOpenRequested(string vendorId)
        {
            if (!TryGetCatalog(vendorId, out var catalog) || !_runtime.OpenVendor(vendorId, catalog))
            {
                RaiseTradeResult(string.Empty, 0, true, false, TradeFailureReason.NoActiveVendor);
                return;
            }

            ResolveShopEvents()?.RaiseShopTradeOpened(vendorId);
            ResolveInventoryEvents()?.RaiseMoneyChanged(_runtime.Money);
        }

        private void HandleBuyRequested(string itemId, int quantity)
        {
            ResolveReferences();
            if (_inventoryController?.Runtime == null)
            {
                RaiseTradeResult(itemId, quantity, true, false, TradeFailureReason.InventoryFull);
                return;
            }

            if (!_inventoryController.Runtime.CanAcceptStackQuantity(itemId, quantity))
            {
                RaiseTradeResult(itemId, quantity, true, false, TradeFailureReason.InventoryFull);
                return;
            }

            var purchased = _runtime.TryBuy(itemId, quantity, out _, out var reason);
            if (!purchased)
            {
                RaiseTradeResult(itemId, quantity, true, false, reason);
                return;
            }

            var stored = _inventoryController.Runtime.TryAddStackItem(itemId, quantity, out _, out _, out _);
            if (!stored)
            {
                _runtime.TrySell(itemId, quantity, out _, out _);
                RaiseTradeResult(itemId, quantity, true, false, TradeFailureReason.InventoryFull);
                return;
            }

            ResolveInventoryEvents()?.RaiseMoneyChanged(_runtime.Money);
            ResolveInventoryEvents()?.RaiseInventoryChanged();
            RaiseTradeResult(itemId, quantity, true, true, TradeFailureReason.None);
        }

        private void HandleSellRequested(string itemId, int quantity)
        {
            ResolveReferences();
            if (_inventoryController?.Runtime == null)
            {
                RaiseTradeResult(itemId, quantity, false, false, TradeFailureReason.InsufficientPlayerQuantity);
                return;
            }

            if (_inventoryController.Runtime.GetItemQuantity(itemId) < quantity || quantity <= 0)
            {
                RaiseTradeResult(itemId, quantity, false, false, TradeFailureReason.InsufficientPlayerQuantity);
                return;
            }

            var sold = _runtime.TrySell(itemId, quantity, out _, out var reason);
            if (!sold)
            {
                RaiseTradeResult(itemId, quantity, false, false, reason);
                return;
            }

            var removed = _inventoryController.Runtime.TryRemoveStackItem(itemId, quantity);
            if (!removed)
            {
                _runtime.TryBuy(itemId, quantity, out _, out _);
                RaiseTradeResult(itemId, quantity, false, false, TradeFailureReason.InsufficientPlayerQuantity);
                return;
            }

            ResolveInventoryEvents()?.RaiseMoneyChanged(_runtime.Money);
            ResolveInventoryEvents()?.RaiseInventoryChanged();
            RaiseTradeResult(itemId, quantity, false, true, TradeFailureReason.None);
        }

        private void HandleBuyCheckoutRequested(ShopCheckoutRequest request)
        {
            ResolveReferences();
            if (_inventoryController?.Runtime == null || request == null || request.Lines == null || request.Lines.Length == 0)
            {
                RaiseTradeResult(string.Empty, 0, true, false, TradeFailureReason.InvalidQuantity);
                return;
            }

            var lines = new List<(string itemId, int quantity)>();
            for (var i = 0; i < request.Lines.Length; i++)
            {
                var line = request.Lines[i];
                if (line.Quantity <= 0 || string.IsNullOrWhiteSpace(line.ItemId))
                {
                    RaiseTradeResult(string.Empty, 0, true, false, TradeFailureReason.InvalidQuantity);
                    return;
                }

                if (!_inventoryController.Runtime.CanAcceptStackQuantity(line.ItemId, line.Quantity))
                {
                    RaiseTradeResult(line.ItemId, line.Quantity, true, false, TradeFailureReason.InventoryFull);
                    return;
                }

                lines.Add((line.ItemId, line.Quantity));
            }

            var economySnapshot = _runtime.CaptureStateSnapshot();
            if (!_runtime.TryBuyBatch(lines, request.DeliveryFee, out _, out var buyReason))
            {
                RaiseTradeResult(string.Empty, 0, true, false, buyReason);
                return;
            }

            var addedLines = new List<(string itemId, int quantity)>(lines.Count);
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var stored = _inventoryController.Runtime.TryAddStackItem(line.itemId, line.quantity, out _, out _, out _);
                if (stored)
                {
                    addedLines.Add(line);
                    continue;
                }

                for (var addedIndex = 0; addedIndex < addedLines.Count; addedIndex++)
                {
                    var added = addedLines[addedIndex];
                    _inventoryController.Runtime.TryRemoveStackItem(added.itemId, added.quantity);
                }

                _runtime.RestoreStateSnapshot(economySnapshot);
                RaiseTradeResult(line.itemId, line.quantity, true, false, TradeFailureReason.InventoryFull);
                return;
            }

            ResolveInventoryEvents()?.RaiseMoneyChanged(_runtime.Money);
            ResolveInventoryEvents()?.RaiseInventoryChanged();
            RaiseTradeResult(string.Empty, 0, true, true, TradeFailureReason.None);
        }

        private void HandleSellCheckoutRequested(ShopCheckoutRequest request)
        {
            ResolveReferences();
            if (_inventoryController?.Runtime == null || request == null || request.Lines == null || request.Lines.Length == 0)
            {
                RaiseTradeResult(string.Empty, 0, false, false, TradeFailureReason.InvalidQuantity);
                return;
            }

            var lines = new List<(string itemId, int quantity)>();
            var requestedTotals = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < request.Lines.Length; i++)
            {
                var line = request.Lines[i];
                if (line.Quantity <= 0 || string.IsNullOrWhiteSpace(line.ItemId))
                {
                    RaiseTradeResult(string.Empty, 0, false, false, TradeFailureReason.InvalidQuantity);
                    return;
                }

                if (!requestedTotals.TryGetValue(line.ItemId, out var existing))
                {
                    existing = 0;
                }

                var nextTotal = existing + line.Quantity;
                if (_inventoryController.Runtime.GetItemQuantity(line.ItemId) < nextTotal)
                {
                    RaiseTradeResult(line.ItemId, line.Quantity, false, false, TradeFailureReason.InsufficientPlayerQuantity);
                    return;
                }

                requestedTotals[line.ItemId] = nextTotal;
                lines.Add((line.ItemId, line.Quantity));
            }

            if (!_runtime.TrySellBatch(lines, out _, out var sellReason))
            {
                RaiseTradeResult(string.Empty, 0, false, false, sellReason);
                return;
            }

            var removedLines = new List<(string itemId, int quantity)>(lines.Count);
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var removed = _inventoryController.Runtime.TryRemoveStackItem(line.itemId, line.quantity);
                if (removed)
                {
                    removedLines.Add(line);
                    continue;
                }

                for (var removedIndex = 0; removedIndex < removedLines.Count; removedIndex++)
                {
                    var removedLine = removedLines[removedIndex];
                    _inventoryController.Runtime.TryAddStackItem(removedLine.itemId, removedLine.quantity, out _, out _, out _);
                }

                _runtime.TryBuyBatch(lines, 0, out _, out _);
                RaiseTradeResult(line.itemId, line.quantity, false, false, TradeFailureReason.InsufficientPlayerQuantity);
                return;
            }

            ResolveInventoryEvents()?.RaiseMoneyChanged(_runtime.Money);
            ResolveInventoryEvents()?.RaiseInventoryChanged();
            RaiseTradeResult(string.Empty, 0, false, true, TradeFailureReason.None);
        }

        private void RaiseTradeResult(string itemId, int quantity, bool isBuy, bool success, TradeFailureReason failureReason)
        {
            ResolveShopEvents()?.RaiseShopTradeResult(new ShopTradeResultPayload(
                itemId,
                quantity,
                isBuy,
                success,
                MapFailureReason(failureReason)));
        }

        private static ShopTradeFailureReason MapFailureReason(TradeFailureReason reason)
        {
            return reason switch
            {
                TradeFailureReason.None => ShopTradeFailureReason.None,
                TradeFailureReason.NoActiveVendor => ShopTradeFailureReason.NoActiveVendor,
                TradeFailureReason.UnknownItem => ShopTradeFailureReason.UnknownItem,
                TradeFailureReason.InvalidQuantity => ShopTradeFailureReason.InvalidQuantity,
                TradeFailureReason.InsufficientFunds => ShopTradeFailureReason.InsufficientFunds,
                TradeFailureReason.InsufficientStock => ShopTradeFailureReason.InsufficientStock,
                TradeFailureReason.InsufficientPlayerQuantity => ShopTradeFailureReason.InsufficientPlayerQuantity,
                TradeFailureReason.InventoryFull => ShopTradeFailureReason.InventoryFull,
                _ => ShopTradeFailureReason.Unrecognized
            };
        }

        private bool TryGetCatalog(string vendorId, out ShopCatalogDefinition catalog)
        {
            catalog = null;
            if (_vendors != null)
            {
                for (var i = 0; i < _vendors.Count; i++)
                {
                    var binding = _vendors[i];
                    if (binding == null
                        || binding.Catalog == null
                        || !string.Equals(binding.VendorId, vendorId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    catalog = binding.Catalog;
                    return true;
                }
            }

            if (_defaultVendorCatalog != null)
            {
                if (!string.IsNullOrWhiteSpace(_defaultVendorId)
                    && string.Equals(_defaultVendorId, vendorId, StringComparison.Ordinal))
                {
                    catalog = _defaultVendorCatalog;
                    return true;
                }

                catalog = _defaultVendorCatalog;
                return true;
            }

            return false;
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_useRuntimeKernelShopEvents)
            {
                SubscribeToShopEvents(ResolveShopEvents());
            }

            if (_useRuntimeKernelInventoryEvents)
            {
                ResolveInventoryEvents();
            }
        }

        private void ResolveReferences()
        {
            _inventoryController ??= _inventoryControllerBehaviour as PlayerInventoryController;
            DependencyResolutionGuard.ResolveOnce(
                ref _inventoryController,
                ref _attemptedInventoryResolution,
                FindFirstObjectByType<PlayerInventoryController>);
            DependencyResolutionGuard.HasRequiredReferences(
                ref _loggedMissingInventoryController,
                this,
                "EconomyController requires a PlayerInventoryController reference.",
                _inventoryController);
        }

        private IShopEvents ResolveShopEvents()
        {
            if (_useRuntimeKernelShopEvents)
            {
                var runtimeShopEvents = RuntimeKernelBootstrapper.ShopEvents;
                if (!ReferenceEquals(_shopEvents, runtimeShopEvents))
                {
                    _shopEvents = runtimeShopEvents;
                    SubscribeToShopEvents(_shopEvents);
                }
                else if (!ReferenceEquals(_subscribedShopEvents, _shopEvents))
                {
                    SubscribeToShopEvents(_shopEvents);
                }

                return _shopEvents;
            }

            if (!ReferenceEquals(_subscribedShopEvents, _shopEvents))
            {
                SubscribeToShopEvents(_shopEvents);
            }

            return _shopEvents;
        }

        private IInventoryEvents ResolveInventoryEvents()
        {
            if (_useRuntimeKernelInventoryEvents)
            {
                _inventoryEvents = RuntimeKernelBootstrapper.InventoryEvents;
            }

            return _inventoryEvents;
        }

        private void SubscribeToShopEvents(IShopEvents shopEvents)
        {
            if (shopEvents == null)
            {
                UnsubscribeFromShopEvents();
                return;
            }

            if (ReferenceEquals(_subscribedShopEvents, shopEvents))
            {
                return;
            }

            UnsubscribeFromShopEvents();
            _subscribedShopEvents = shopEvents;
            _subscribedShopEvents.OnShopTradeOpenRequested += HandleTradeOpenRequested;
            _subscribedShopEvents.OnShopBuyRequested += HandleBuyRequested;
            _subscribedShopEvents.OnShopSellRequested += HandleSellRequested;
            _subscribedShopEvents.OnShopBuyCheckoutRequested += HandleBuyCheckoutRequested;
            _subscribedShopEvents.OnShopSellCheckoutRequested += HandleSellCheckoutRequested;
        }

        private void UnsubscribeFromShopEvents()
        {
            if (_subscribedShopEvents == null)
            {
                return;
            }

            _subscribedShopEvents.OnShopTradeOpenRequested -= HandleTradeOpenRequested;
            _subscribedShopEvents.OnShopBuyRequested -= HandleBuyRequested;
            _subscribedShopEvents.OnShopSellRequested -= HandleSellRequested;
            _subscribedShopEvents.OnShopBuyCheckoutRequested -= HandleBuyCheckoutRequested;
            _subscribedShopEvents.OnShopSellCheckoutRequested -= HandleSellCheckoutRequested;
            _subscribedShopEvents = null;
        }

        private void SubscribeToRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            RuntimeKernelBootstrapper.EventsReconfigured += HandleRuntimeEventsReconfigured;
        }

        private void UnsubscribeFromRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
        }
    }
}
