using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Core.UI;
using Reloader.Economy;
using Reloader.Inventory;
using Reloader.UI.Toolkit.Contracts;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloader.UI.Toolkit.Trade
{
    public sealed class TradeController : MonoBehaviour, IUiController
    {
        private TradeViewBinder _viewBinder;
        private RuntimeHubChannelBinder<IShopEvents> _shopEventsBinder;
        private bool _isOpen;
        private TradeUiTab _activeTab = TradeUiTab.Buy;
        private string _selectedBuyItemId;
        private string _selectedSellItemId;
        private EconomyController _economyController;
        private PlayerInventoryController _inventoryController;
        private Dictionary<string, string> _itemDisplayNameById;

        private void OnEnable()
        {
            SubscribeToRuntimeHubReconfigure();
            ShopEventsBinder.ResolveAndBind();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            ShopEventsBinder.Unbind();
        }

        public void Configure(IShopEvents shopEvents = null)
        {
            ShopEventsBinder.Configure(shopEvents);
            if (isActiveAndEnabled)
            {
                ShopEventsBinder.ResolveAndBind();
            }
        }

        public void SetViewBinder(TradeViewBinder binder)
        {
            _viewBinder = binder;
            Refresh();
        }

        public void HandleIntent(UiIntent intent)
        {
            if (intent.Key == "trade.tab.buy")
            {
                _activeTab = TradeUiTab.Buy;
            }
            else if (intent.Key == "trade.tab.sell")
            {
                _activeTab = TradeUiTab.Sell;
            }
            else if (intent.Key == "trade.confirm.buy")
            {
                _activeTab = TradeUiTab.Buy;
                if (TryResolveCheckoutRequest(intent.Payload, out var request))
                {
                    ResolveShopEvents()?.RaiseShopBuyCheckoutRequested(request);
                }
                else if (!string.IsNullOrWhiteSpace(_selectedBuyItemId))
                {
                    ResolveShopEvents()?.RaiseShopBuyRequested(_selectedBuyItemId, 1);
                }
            }
            else if (intent.Key == "trade.confirm.sell")
            {
                _activeTab = TradeUiTab.Sell;
                if (TryResolveCheckoutRequest(intent.Payload, out var request))
                {
                    ResolveShopEvents()?.RaiseShopSellCheckoutRequested(request);
                }
                else if (!string.IsNullOrWhiteSpace(_selectedSellItemId))
                {
                    ResolveShopEvents()?.RaiseShopSellRequested(_selectedSellItemId, 1);
                }
            }
            else if (intent.Key == "trade.buy.slot")
            {
                _selectedBuyItemId = intent.Payload as string;
            }
            else if (intent.Key == "trade.sell.slot")
            {
                _selectedSellItemId = intent.Payload as string;
            }

            Refresh();
        }

        public void Render(TradeUiState state)
        {
            _viewBinder?.Render(state);
        }

        private void HandleTradeOpened(string _)
        {
            _isOpen = true;
            _selectedBuyItemId = null;
            _selectedSellItemId = null;
            Refresh();
        }

        private void HandleTradeClosed()
        {
            _isOpen = false;
            _selectedBuyItemId = null;
            _selectedSellItemId = null;
            Refresh();
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled || !ShopEventsBinder.UsesRuntimeChannel)
            {
                return;
            }

            ShopEventsBinder.ResolveAndBind();
            ReconcileVisibilityAfterRuntimeHubSwap();
        }

        private void Refresh()
        {
            if (_viewBinder == null)
            {
                return;
            }

            _viewBinder.SetVisible(_isOpen);
            if (!_isOpen)
            {
                return;
            }

            var buySlots = BuildBuySlots();
            var sellSlots = BuildSellSlots();
            Render(new TradeUiState(
                _activeTab,
                false,
                "$0",
                canConfirmBuy: CanConfirmSelectedBuyItem(buySlots),
                canConfirmSell: CanConfirmSelectedSellItem(sellSlots),
                buySlots: buySlots,
                sellSlots: sellSlots));
        }

        private void ReconcileVisibilityAfterRuntimeHubSwap()
        {
            _isOpen = RuntimeKernelBootstrapper.UiStateEvents?.IsShopTradeMenuOpen ?? false;
            Refresh();
        }

        private static bool TryResolveCheckoutRequest(object payload, out ShopCheckoutRequest request)
        {
            request = payload as ShopCheckoutRequest;
            return request?.Lines != null && request.Lines.Length > 0;
        }

        private IShopEvents ResolveShopEvents()
        {
            return ShopEventsBinder.ResolveAndBind();
        }

        private void SubscribeToShopEvents(IShopEvents shopEvents)
        {
            shopEvents.OnShopTradeOpened += HandleTradeOpened;
            shopEvents.OnShopTradeClosed += HandleTradeClosed;
            shopEvents.OnShopTradeResultReceived += HandleTradeResult;
        }

        private void UnsubscribeFromShopEvents(IShopEvents shopEvents)
        {
            shopEvents.OnShopTradeOpened -= HandleTradeOpened;
            shopEvents.OnShopTradeClosed -= HandleTradeClosed;
            shopEvents.OnShopTradeResultReceived -= HandleTradeResult;
        }

        private void HandleTradeResult(ShopTradeResultPayload payload)
        {
            if (_isOpen && payload.Success)
            {
                Refresh();
            }
        }

        private IReadOnlyList<TradeUiSlotViewModel> BuildBuySlots()
        {
            var slots = new List<TradeUiSlotViewModel>(6);
            var economy = ResolveEconomyController();
            var runtime = economy?.Runtime;
            var catalogItems = runtime?.GetActiveCatalogItems();
            if (catalogItems != null)
            {
                for (var i = 0; i < catalogItems.Count && slots.Count < 6; i++)
                {
                    var item = catalogItems[i];
                    if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                    {
                        continue;
                    }

                    runtime.TryGetStock(item.ItemId, out var stock);
                    var isEnabled = stock > 0;
                    var isSelected = !string.IsNullOrWhiteSpace(_selectedBuyItemId)
                                     && string.Equals(_selectedBuyItemId, item.ItemId, System.StringComparison.Ordinal);
                    var label = $"{item.DisplayName}\n${item.UnitPrice} | x{stock}";
                    slots.Add(new TradeUiSlotViewModel(item.ItemId, label, isEnabled, isSelected));
                }
            }

            while (slots.Count < 6)
            {
                slots.Add(null);
            }

            return slots;
        }

        private IReadOnlyList<TradeUiSlotViewModel> BuildSellSlots()
        {
            var slots = new List<TradeUiSlotViewModel>(6);
            var economy = ResolveEconomyController();
            var runtime = economy?.Runtime;
            var inventoryRuntime = ResolveInventoryController()?.Runtime;
            if (runtime == null || inventoryRuntime == null)
            {
                while (slots.Count < 6)
                {
                    slots.Add(null);
                }

                return slots;
            }

            var uniqueItemIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                var itemId = inventoryRuntime.BeltSlotItemIds[i];
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    uniqueItemIds.Add(itemId);
                }
            }

            for (var i = 0; i < inventoryRuntime.BackpackItemIds.Count; i++)
            {
                var itemId = inventoryRuntime.BackpackItemIds[i];
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    uniqueItemIds.Add(itemId);
                }
            }

            foreach (var itemId in uniqueItemIds)
            {
                if (slots.Count >= 6)
                {
                    break;
                }

                if (!runtime.TryGetUnitPrice(itemId, out var unitPrice))
                {
                    continue;
                }

                var quantity = inventoryRuntime.GetItemQuantity(itemId);
                if (quantity <= 0)
                {
                    continue;
                }

                var isSelected = !string.IsNullOrWhiteSpace(_selectedSellItemId)
                                 && string.Equals(_selectedSellItemId, itemId, StringComparison.Ordinal);
                var displayName = ResolveItemDisplayName(itemId);
                var label = $"{displayName}\n${unitPrice} | x{quantity}";
                slots.Add(new TradeUiSlotViewModel(itemId, label, isEnabled: true, isSelected: isSelected));
            }

            while (slots.Count < 6)
            {
                slots.Add(null);
            }

            return slots;
        }

        private bool CanConfirmSelectedBuyItem(IReadOnlyList<TradeUiSlotViewModel> buySlots)
        {
            if (string.IsNullOrWhiteSpace(_selectedBuyItemId) || buySlots == null)
            {
                return false;
            }

            for (var i = 0; i < buySlots.Count; i++)
            {
                var slot = buySlots[i];
                if (slot == null)
                {
                    continue;
                }

                if (string.Equals(slot.ItemId, _selectedBuyItemId, System.StringComparison.Ordinal))
                {
                    return slot.IsEnabled;
                }
            }

            return false;
        }

        private bool CanConfirmSelectedSellItem(IReadOnlyList<TradeUiSlotViewModel> sellSlots)
        {
            if (string.IsNullOrWhiteSpace(_selectedSellItemId) || sellSlots == null)
            {
                return false;
            }

            for (var i = 0; i < sellSlots.Count; i++)
            {
                var slot = sellSlots[i];
                if (slot == null)
                {
                    continue;
                }

                if (string.Equals(slot.ItemId, _selectedSellItemId, StringComparison.Ordinal))
                {
                    return slot.IsEnabled;
                }
            }

            return false;
        }

        private EconomyController ResolveEconomyController()
        {
            if (_economyController == null)
            {
                _economyController = FindFirstObjectByType<EconomyController>(FindObjectsInactive.Include);
            }

            return _economyController;
        }

        private PlayerInventoryController ResolveInventoryController()
        {
            if (_inventoryController == null)
            {
                _inventoryController = FindFirstObjectByType<PlayerInventoryController>(FindObjectsInactive.Include);
            }

            return _inventoryController;
        }

        private string ResolveItemDisplayName(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return string.Empty;
            }

            _itemDisplayNameById ??= BuildItemDisplayNameLookup();
            if (_itemDisplayNameById.TryGetValue(itemId, out var displayName))
            {
                return displayName;
            }

            return itemId;
        }

        private Dictionary<string, string> BuildItemDisplayNameLookup()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            var definitions = ResolveInventoryController()?.GetItemDefinitionRegistrySnapshot();
            if (definitions == null)
            {
                return map;
            }

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId))
                {
                    continue;
                }

                var displayName = string.IsNullOrWhiteSpace(definition.DisplayName)
                    ? definition.DefinitionId
                    : definition.DisplayName;

                map[definition.DefinitionId] = displayName;
            }

            return map;
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

        private RuntimeHubChannelBinder<IShopEvents> ShopEventsBinder => _shopEventsBinder ??=
            new RuntimeHubChannelBinder<IShopEvents>(
                () => RuntimeKernelBootstrapper.ShopEvents,
                SubscribeToShopEvents,
                UnsubscribeFromShopEvents);
    }
}
