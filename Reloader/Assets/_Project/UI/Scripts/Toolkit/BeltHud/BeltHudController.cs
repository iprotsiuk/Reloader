using System.Collections.Generic;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Inventory;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.BeltHud
{
    public sealed class BeltHudController : MonoBehaviour, IUiController
    {
        [SerializeField] private PlayerInventoryController _inventoryController;

        private BeltHudViewBinder _viewBinder;
        private IInventoryEvents _inventoryEvents;
        private IInventoryEvents _subscribedInventoryEvents;
        private bool _useRuntimeKernelInventoryEvents = true;

        private void OnEnable()
        {
            SubscribeToRuntimeHubReconfigure();
            SubscribeToInventoryEvents(ResolveInventoryEvents());
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            UnsubscribeFromInventoryEvents();
        }

        public void Configure(IInventoryEvents inventoryEvents = null)
        {
            _useRuntimeKernelInventoryEvents = inventoryEvents == null;
            _inventoryEvents = inventoryEvents;
            if (isActiveAndEnabled)
            {
                SubscribeToInventoryEvents(ResolveInventoryEvents());
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
            if (!isActiveAndEnabled || !_useRuntimeKernelInventoryEvents)
            {
                return;
            }

            SubscribeToInventoryEvents(ResolveInventoryEvents());
        }

        private IInventoryEvents ResolveInventoryEvents()
        {
            if (_useRuntimeKernelInventoryEvents)
            {
                var runtimeInventoryEvents = RuntimeKernelBootstrapper.InventoryEvents;
                if (!ReferenceEquals(_inventoryEvents, runtimeInventoryEvents))
                {
                    _inventoryEvents = runtimeInventoryEvents;
                    SubscribeToInventoryEvents(_inventoryEvents);
                }
                else if (!ReferenceEquals(_subscribedInventoryEvents, _inventoryEvents))
                {
                    SubscribeToInventoryEvents(_inventoryEvents);
                }

                return _inventoryEvents;
            }

            if (!ReferenceEquals(_subscribedInventoryEvents, _inventoryEvents))
            {
                SubscribeToInventoryEvents(_inventoryEvents);
            }

            return _inventoryEvents;
        }

        private void SubscribeToInventoryEvents(IInventoryEvents inventoryEvents)
        {
            if (inventoryEvents == null)
            {
                UnsubscribeFromInventoryEvents();
                return;
            }

            if (ReferenceEquals(_subscribedInventoryEvents, inventoryEvents))
            {
                return;
            }

            UnsubscribeFromInventoryEvents();
            _subscribedInventoryEvents = inventoryEvents;
            _subscribedInventoryEvents.OnInventoryChanged += HandleInventoryChanged;
            _subscribedInventoryEvents.OnBeltSelectionChanged += HandleBeltSelectionChanged;
        }

        private void UnsubscribeFromInventoryEvents()
        {
            if (_subscribedInventoryEvents == null)
            {
                return;
            }

            _subscribedInventoryEvents.OnInventoryChanged -= HandleInventoryChanged;
            _subscribedInventoryEvents.OnBeltSelectionChanged -= HandleBeltSelectionChanged;
            _subscribedInventoryEvents = null;
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
