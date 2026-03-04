using System;
using System.Collections.Generic;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.TabInventory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloader.UI.Toolkit.ChestInventory
{
    public sealed class ChestInventoryController : MonoBehaviour, IUiController
    {
        private const int DefaultChestSlots = 20;

        [SerializeField] private PlayerInventoryController _inventoryController;
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;

        private ChestInventoryViewBinder _viewBinder;
        private IPlayerInputSource _inputSource;

        public void SetInventoryController(PlayerInventoryController inventoryController)
        {
            _inventoryController = inventoryController;
        }

        public void SetInputSource(IPlayerInputSource inputSource)
        {
            _inputSource = inputSource;
        }

        public void Configure(ChestInventoryViewBinder viewBinder)
        {
            if (_viewBinder != null)
            {
                _viewBinder.IntentRaised -= HandleIntent;
            }

            _viewBinder = viewBinder;
            if (_viewBinder != null)
            {
                _viewBinder.IntentRaised += HandleIntent;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_viewBinder != null)
            {
                _viewBinder.IntentRaised -= HandleIntent;
            }
        }

        private void Update()
        {
            Tick();
        }

        public void Tick()
        {
            ResolveInputSource();
            if (StorageUiSession.IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                StorageUiSession.Close();
            }

            if (_inputSource != null && _inputSource.ConsumeMenuTogglePressed() && StorageUiSession.IsOpen)
            {
                StorageUiSession.Close();
            }

            Refresh();
        }

        public void HandleIntent(UiIntent intent)
        {
            if (_inventoryController?.Runtime == null)
            {
                return;
            }

            if (!StorageUiSession.IsOpen || string.IsNullOrWhiteSpace(StorageUiSession.ActiveContainerId))
            {
                return;
            }

            if (intent.Payload is not TabInventoryDragController.DragIntentPayload drag)
            {
                return;
            }

            var targetContainerLocator = ToLocator(drag.TargetContainer);
            var sourceContainerLocator = ToLocator(drag.SourceContainer);
            if (string.IsNullOrWhiteSpace(sourceContainerLocator) || string.IsNullOrWhiteSpace(targetContainerLocator))
            {
                return;
            }

            StorageTransferEngine.TryMove(
                _inventoryController.Runtime,
                StorageRuntimeBridge.Registry,
                sourceContainerLocator,
                drag.SourceIndex,
                targetContainerLocator,
                drag.TargetIndex);

            Refresh();
        }

        private void Refresh()
        {
            if (_viewBinder == null)
            {
                return;
            }

            var hasContainer = StorageRuntimeBridge.Registry.TryGet(StorageUiSession.ActiveContainerId, out var container);
            var open = StorageUiSession.IsOpen
                       && !string.IsNullOrWhiteSpace(StorageUiSession.ActiveContainerId)
                       && hasContainer
                       && container != null
                       && _inventoryController?.Runtime != null;

            if (!open)
            {
                _viewBinder.Render(ChestInventoryUiState.Create(false, Array.Empty<ChestInventoryUiState.SlotState>(), Array.Empty<ChestInventoryUiState.SlotState>()));
                return;
            }

            var chestSlots = new List<ChestInventoryUiState.SlotState>(container.SlotCount);
            for (var i = 0; i < container.SlotCount; i++)
            {
                var itemId = container.GetSlotItemId(i);
                chestSlots.Add(new ChestInventoryUiState.SlotState(i, itemId, !string.IsNullOrWhiteSpace(itemId)));
            }

            var runtime = _inventoryController.Runtime;
            var playerSlots = new List<ChestInventoryUiState.SlotState>(runtime.BackpackCapacity);
            for (var i = 0; i < runtime.BackpackCapacity; i++)
            {
                var itemId = i < runtime.BackpackItemIds.Count ? runtime.BackpackItemIds[i] : null;
                playerSlots.Add(new ChestInventoryUiState.SlotState(i, itemId, !string.IsNullOrWhiteSpace(itemId)));
            }

            _viewBinder.Render(ChestInventoryUiState.Create(true, chestSlots, playerSlots));
        }

        private void ResolveInputSource()
        {
            if (_inputSource == null && _inputSourceBehaviour is IPlayerInputSource direct)
            {
                _inputSource = direct;
            }

            if (_inputSource == null)
            {
                _inputSource = Reloader.Core.DependencyResolutionGuard.FindInterface<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
            }
        }

        private static string ToLocator(string container)
        {
            if (string.Equals(container, "backpack", StringComparison.Ordinal))
            {
                return "backpack";
            }

            if (string.Equals(container, "container", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(StorageUiSession.ActiveContainerId))
            {
                return "container:" + StorageUiSession.ActiveContainerId;
            }

            return null;
        }
    }
}
