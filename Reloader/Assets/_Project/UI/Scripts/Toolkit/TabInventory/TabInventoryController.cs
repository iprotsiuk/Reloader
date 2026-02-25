using System.Collections.Generic;
using Reloader.Core;
using Reloader.Core.Events;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.TabInventory
{
    public sealed class TabInventoryController : MonoBehaviour, IUiController
    {
        [SerializeField] private PlayerInventoryController _inventoryController;
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;

        private TabInventoryViewBinder _viewBinder;
        private TabInventoryDragController _dragController;
        private IPlayerInputSource _inputSource;
        private bool _isOpen;
        private string _activeSection = "inventory";

        private void OnEnable()
        {
            GameEvents.OnInventoryChanged += HandleInventoryChanged;
            Refresh();
        }

        private void Update()
        {
            Tick();
        }

        public void Configure(TabInventoryViewBinder viewBinder, TabInventoryDragController dragController)
        {
            _viewBinder = viewBinder;
            _dragController = dragController;
            if (_dragController != null)
            {
                _dragController.IntentRaised += HandleIntent;
            }

            Refresh();
        }

        public void SetInputSource(IPlayerInputSource inputSource)
        {
            _inputSource = inputSource;
        }

        public void SetInventoryController(PlayerInventoryController inventoryController)
        {
            _inventoryController = inventoryController;
            Refresh();
        }

        public void Tick()
        {
            ResolveInputSource();

            if (_inputSource == null)
            {
                return;
            }

            if (_inputSource.ConsumeMenuTogglePressed())
            {
                _isOpen = !_isOpen;
                if (_isOpen)
                {
                    _activeSection = "inventory";
                }

                Refresh();
            }
        }

        public void HandleIntent(UiIntent intent)
        {
            if (intent.Key == "tab.menu.select" && intent.Payload is string sectionId)
            {
                _activeSection = NormalizeSection(sectionId);
                Refresh();
                return;
            }

            if (_inventoryController?.Runtime == null)
            {
                return;
            }

            if ((intent.Key == "inventory.drag.swap" || intent.Key == "inventory.drag.merge")
                && intent.Payload is TabInventoryDragController.DragIntentPayload dragPayload)
            {
                var moved = _inventoryController.TryMoveItem(
                    ResolveArea(dragPayload.SourceContainer),
                    dragPayload.SourceIndex,
                    ResolveArea(dragPayload.TargetContainer),
                    dragPayload.TargetIndex);

                if (moved)
                {
                    Refresh();
                }
            }
        }

        private void OnDisable()
        {
            GameEvents.OnInventoryChanged -= HandleInventoryChanged;
            if (_dragController != null)
            {
                _dragController.IntentRaised -= HandleIntent;
            }
        }

        private void Refresh()
        {
            if (_viewBinder == null)
            {
                return;
            }

            if (_inventoryController?.Runtime == null)
            {
                _viewBinder.Render(TabInventoryUiState.Create(
                    _isOpen,
                    new TabInventoryUiState.SlotState[PlayerInventoryRuntime.BeltSlotCount],
                    new TabInventoryUiState.SlotState[2],
                    string.Empty,
                    false,
                    _activeSection));
                return;
            }

            var runtime = _inventoryController.Runtime;
            var belt = new List<TabInventoryUiState.SlotState>(PlayerInventoryRuntime.BeltSlotCount);
            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                var itemId = runtime.BeltSlotItemIds[i];
                belt.Add(new TabInventoryUiState.SlotState(i, itemId, !string.IsNullOrWhiteSpace(itemId)));
            }

            const int backpackSlots = 2;
            var backpack = new List<TabInventoryUiState.SlotState>(backpackSlots);
            for (var i = 0; i < backpackSlots; i++)
            {
                var itemId = i < runtime.BackpackItemIds.Count ? runtime.BackpackItemIds[i] : null;
                backpack.Add(new TabInventoryUiState.SlotState(i, itemId, !string.IsNullOrWhiteSpace(itemId)));
            }

            _viewBinder.Render(TabInventoryUiState.Create(
                _isOpen,
                belt,
                backpack,
                string.Empty,
                false,
                _activeSection));
        }

        private void HandleInventoryChanged()
        {
            Refresh();
        }

        private static InventoryArea ResolveArea(string container)
        {
            return container == "backpack" ? InventoryArea.Backpack : InventoryArea.Belt;
        }

        private static string NormalizeSection(string sectionId)
        {
            return sectionId switch
            {
                "quests" => "quests",
                "journal" => "journal",
                "calendar" => "calendar",
                _ => "inventory"
            };
        }

        private void ResolveInputSource()
        {
            if (_inputSource != null)
            {
                return;
            }

            if (_inputSourceBehaviour is IPlayerInputSource directFromField)
            {
                _inputSource = directFromField;
                return;
            }

            _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponents<MonoBehaviour>());
            if (_inputSource != null)
            {
                return;
            }

            _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
        }
    }
}
