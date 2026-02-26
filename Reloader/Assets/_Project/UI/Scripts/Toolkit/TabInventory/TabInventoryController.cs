using System.Collections.Generic;
using Reloader.Core;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.TabInventory
{
    public sealed class TabInventoryController : MonoBehaviour, IUiController
    {
        private const int MinimumBackpackUiSlots = 16;

        [SerializeField] private PlayerInventoryController _inventoryController;
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;

        private TabInventoryViewBinder _viewBinder;
        private TabInventoryDragController _dragController;
        private IPlayerInputSource _inputSource;
        private IInventoryEvents _inventoryEvents;
        private IInventoryEvents _subscribedInventoryEvents;
        private bool _useRuntimeKernelInventoryEvents = true;
        private IUiStateEvents _uiStateEvents;
        private bool _useRuntimeKernelUiStateEvents = true;
        private bool _isOpen;
        private string _activeSection = "inventory";

        private void OnEnable()
        {
            SubscribeToRuntimeHubReconfigure();
            SubscribeToInventoryEvents(ResolveInventoryEvents());
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

        public void Configure(IUiStateEvents uiStateEvents = null)
        {
            _useRuntimeKernelUiStateEvents = uiStateEvents == null;
            _uiStateEvents = uiStateEvents;
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
                SetMenuOpen(!_isOpen);
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
                var sourceArea = ResolveArea(dragPayload.SourceContainer);
                var targetArea = ResolveArea(dragPayload.TargetContainer);
                var targetIndex = NormalizeTargetIndex(sourceArea, targetArea, dragPayload.TargetIndex);
                var moved = _inventoryController.TryMoveItem(
                    sourceArea,
                    dragPayload.SourceIndex,
                    targetArea,
                    targetIndex);

                if (moved)
                {
                    Refresh();
                }
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            UnsubscribeFromInventoryEvents();
            if (_isOpen)
            {
                SetMenuOpen(false);
            }

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
                    new TabInventoryUiState.SlotState[MinimumBackpackUiSlots],
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
                var occupied = !string.IsNullOrWhiteSpace(itemId);
                var quantity = occupied ? runtime.GetSlotQuantity(InventoryArea.Belt, i) : 0;
                belt.Add(new TabInventoryUiState.SlotState(i, itemId, occupied, quantity));
            }

            var backpackSlots = Mathf.Max(MinimumBackpackUiSlots, runtime.BackpackCapacity);
            var backpack = new List<TabInventoryUiState.SlotState>(backpackSlots);
            for (var i = 0; i < backpackSlots; i++)
            {
                var itemId = i < runtime.BackpackItemIds.Count ? runtime.BackpackItemIds[i] : null;
                var occupied = !string.IsNullOrWhiteSpace(itemId);
                var quantity = occupied ? runtime.GetSlotQuantity(InventoryArea.Backpack, i) : 0;
                backpack.Add(new TabInventoryUiState.SlotState(i, itemId, occupied, quantity));
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

        private int NormalizeTargetIndex(InventoryArea sourceArea, InventoryArea targetArea, int requestedTargetIndex)
        {
            if (sourceArea == InventoryArea.Belt
                && targetArea == InventoryArea.Backpack
                && _inventoryController?.Runtime != null
                && requestedTargetIndex >= _inventoryController.Runtime.BackpackItemIds.Count)
            {
                return _inventoryController.Runtime.BackpackItemIds.Count;
            }

            return requestedTargetIndex;
        }

        private void ResolveInputSource()
        {
            if (!IsInputSourceAlive(_inputSource))
            {
                _inputSource = null;
            }

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

        private static bool IsInputSourceAlive(IPlayerInputSource inputSource)
        {
            if (inputSource == null)
            {
                return false;
            }

            if (inputSource is UnityEngine.Object unityObject && unityObject == null)
            {
                return false;
            }

            return true;
        }

        private void SetMenuOpen(bool isOpen)
        {
            if (_isOpen == isOpen)
            {
                return;
            }

            _isOpen = isOpen;
            ResolveUiStateEvents()?.RaiseTabInventoryVisibilityChanged(_isOpen);
        }

        private IUiStateEvents ResolveUiStateEvents()
        {
            if (_useRuntimeKernelUiStateEvents)
            {
                _uiStateEvents = RuntimeKernelBootstrapper.UiStateEvents;
            }

            return _uiStateEvents;
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_useRuntimeKernelInventoryEvents)
            {
                SubscribeToInventoryEvents(ResolveInventoryEvents());
            }

            if (_useRuntimeKernelUiStateEvents)
            {
                ResolveUiStateEvents()?.RaiseTabInventoryVisibilityChanged(_isOpen);
            }
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
        }

        private void UnsubscribeFromInventoryEvents()
        {
            if (_subscribedInventoryEvents == null)
            {
                return;
            }

            _subscribedInventoryEvents.OnInventoryChanged -= HandleInventoryChanged;
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
