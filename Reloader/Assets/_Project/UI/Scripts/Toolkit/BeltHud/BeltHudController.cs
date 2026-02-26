using System.Collections.Generic;
using Reloader.Core.Events;
using Reloader.Inventory;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.BeltHud
{
    public sealed class BeltHudController : MonoBehaviour, IUiController
    {
        [SerializeField] private PlayerInventoryController _inventoryController;

        private BeltHudViewBinder _viewBinder;

        private void OnEnable()
        {
            GameEvents.OnInventoryChanged += HandleInventoryChanged;
            GameEvents.OnBeltSelectionChanged += HandleBeltSelectionChanged;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.OnInventoryChanged -= HandleInventoryChanged;
            GameEvents.OnBeltSelectionChanged -= HandleBeltSelectionChanged;
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
                GameEvents.RaiseBeltSelectionChanged(_inventoryController.Runtime.SelectedBeltIndex);
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
    }
}
