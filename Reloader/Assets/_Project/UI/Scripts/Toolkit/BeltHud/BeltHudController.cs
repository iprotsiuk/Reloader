using System.Collections.Generic;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Core.UI;
using Reloader.Inventory;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.BeltHud
{
    public sealed class BeltHudController : MonoBehaviour, IUiController
    {
        [SerializeField] private PlayerInventoryController _inventoryController;

        private BeltHudViewBinder _viewBinder;
        private RuntimeHubChannelBinder<IInventoryEvents> _inventoryEventsBinder;

        private void OnEnable()
        {
            SubscribeToRuntimeHubReconfigure();
            InventoryEventsBinder.ResolveAndBind();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            InventoryEventsBinder.Unbind();
        }

        public void Configure(IInventoryEvents inventoryEvents = null)
        {
            InventoryEventsBinder.Configure(inventoryEvents);
            if (isActiveAndEnabled)
            {
                InventoryEventsBinder.ResolveAndBind();
            }
        }

        public void SetInventoryController(PlayerInventoryController controller)
        {
            _inventoryController = controller;
            Refresh();
        }

        public void SetViewBinder(BeltHudViewBinder binder)
        {
            _viewBinder = binder;
            Refresh();
        }

        public void HandleIntent(UiIntent intent)
        {
            if (intent.Key != "belt.slot.select")
            {
                return;
            }

            if (_inventoryController?.Runtime == null || intent.Payload is not int slotIndex)
            {
                return;
            }

            var previous = _inventoryController.Runtime.SelectedBeltIndex;
            _inventoryController.Runtime.SelectBeltSlot(slotIndex);
            if (_inventoryController.Runtime.SelectedBeltIndex != previous)
            {
                ResolveInventoryEvents()?.RaiseBeltSelectionChanged(_inventoryController.Runtime.SelectedBeltIndex);
            }

            Refresh();
        }

        public void Refresh()
        {
            if (_viewBinder == null || _inventoryController?.Runtime == null)
            {
                return;
            }

            var runtime = _inventoryController.Runtime;
            var slotStates = new List<BeltHudUiState.SlotState>(PlayerInventoryRuntime.BeltSlotCount);
            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                var itemId = runtime.BeltSlotItemIds[i];
                var occupied = !string.IsNullOrWhiteSpace(itemId);
                var selected = runtime.SelectedBeltIndex == i;
                var quantity = occupied ? runtime.GetSlotQuantity(InventoryArea.Belt, i) : 0;
                slotStates.Add(new BeltHudUiState.SlotState(i, itemId, occupied, selected, quantity));
            }

            _viewBinder.Render(BeltHudUiState.Create(slotStates));
        }

        private void HandleInventoryChanged()
        {
            Refresh();
        }

        private void HandleBeltSelectionChanged(int _)
        {
            Refresh();
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled || !InventoryEventsBinder.UsesRuntimeChannel)
            {
                return;
            }

            InventoryEventsBinder.ResolveAndBind();
        }

        private IInventoryEvents ResolveInventoryEvents()
        {
            return InventoryEventsBinder.ResolveAndBind();
        }

        private void SubscribeToInventoryEvents(IInventoryEvents inventoryEvents)
        {
            inventoryEvents.OnInventoryChanged += HandleInventoryChanged;
            inventoryEvents.OnBeltSelectionChanged += HandleBeltSelectionChanged;
        }

        private void UnsubscribeFromInventoryEvents(IInventoryEvents inventoryEvents)
        {
            inventoryEvents.OnInventoryChanged -= HandleInventoryChanged;
            inventoryEvents.OnBeltSelectionChanged -= HandleBeltSelectionChanged;
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

        private RuntimeHubChannelBinder<IInventoryEvents> InventoryEventsBinder => _inventoryEventsBinder ??=
            new RuntimeHubChannelBinder<IInventoryEvents>(
                () => RuntimeKernelBootstrapper.InventoryEvents,
                SubscribeToInventoryEvents,
                UnsubscribeFromInventoryEvents);
    }
}
