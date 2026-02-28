using System;
using System.Collections.Generic;
using Reloader.Core.UI;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.TabInventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.ChestInventory
{
    public sealed class ChestInventoryViewBinder : IUiViewBinder
    {
        private VisualElement _panel;
        private VisualElement _chestGrid;
        private VisualElement _playerGrid;
        private readonly List<VisualElement> _chestSlots = new List<VisualElement>();
        private readonly List<VisualElement> _playerSlots = new List<VisualElement>();

        private string _dragSourceContainer;
        private int _dragSourceIndex = -1;
        private string _dragSourceItemId;
        private string _dragTargetContainer;
        private int _dragTargetIndex = -1;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root, int chestSlotCount, int playerSlotCount)
        {
            _panel = root?.Q<VisualElement>("chest__panel");
            _chestGrid = root?.Q<VisualElement>("chest__left-grid");
            _playerGrid = root?.Q<VisualElement>("chest__right-grid");

            BuildSlots(_chestGrid, _chestSlots, chestSlotCount, "container");
            BuildSlots(_playerGrid, _playerSlots, playerSlotCount, "backpack");
        }

        public void Render(UiRenderState state)
        {
            if (state is not ChestInventoryUiState chestState)
            {
                return;
            }

            if (_panel != null)
            {
                _panel.style.display = chestState.IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            }

            ApplySlots(_chestSlots, chestState.ChestSlots, "container");
            ApplySlots(_playerSlots, chestState.PlayerSlots, "backpack");
        }

        private void BuildSlots(VisualElement grid, List<VisualElement> target, int count, string container)
        {
            if (grid == null)
            {
                return;
            }

            grid.Clear();
            target.Clear();
            for (var i = 0; i < Math.Max(0, count); i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("chest__slot");
                slot.name = $"chest__slot-{container}-{i}";

                var capturedIndex = i;
                var capturedContainer = container;
                slot.RegisterCallback<PointerDownEvent>(_ => TryPointerDown(capturedContainer, capturedIndex));
                slot.RegisterCallback<PointerEnterEvent>(_ => TryPointerEnter(capturedContainer, capturedIndex));
                slot.RegisterCallback<PointerUpEvent>(_ => TryPointerUp(capturedContainer, capturedIndex));

                grid.Add(slot);
                target.Add(slot);
            }
        }

        private void ApplySlots(List<VisualElement> slots, IReadOnlyList<ChestInventoryUiState.SlotState> states, string container)
        {
            var limit = Math.Min(slots.Count, states?.Count ?? 0);
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null)
                {
                    continue;
                }

                var occupied = i < limit && states[i].Occupied;
                var itemId = occupied ? states[i].ItemId : string.Empty;
                slot.EnableInClassList("is-occupied", occupied);
                slot.tooltip = itemId;

                ApplySlotVisual(slot, itemId, occupied, container, i);

                slot.EnableInClassList("is-drag-source", IsDragSource(container, i));
                slot.EnableInClassList("is-drag-target", IsDragTarget(container, i));
            }
        }

        private static void ApplySlotVisual(VisualElement slot, string itemId, bool occupied, string container, int index)
        {
            var contentName = $"chest__slot-item-{container}-{index}";
            var iconName = $"chest__slot-item-icon-{container}-{index}";
            var labelName = $"chest__slot-label-{container}-{index}";
            var hasVisual = occupied && !string.IsNullOrWhiteSpace(itemId);
            var content = slot.Q<VisualElement>(contentName);

            if (!hasVisual)
            {
                content?.RemoveFromHierarchy();
                return;
            }

            if (content == null)
            {
                content = new VisualElement
                {
                    name = contentName,
                    pickingMode = PickingMode.Ignore
                };
                content.AddToClassList("chest__slot-item");
                slot.Add(content);
            }

            var icon = content.Q<VisualElement>(iconName);
            if (icon == null)
            {
                icon = new VisualElement
                {
                    name = iconName,
                    pickingMode = PickingMode.Ignore
                };
                icon.AddToClassList("chest__slot-item-icon");
                content.Add(icon);
            }

            var label = content.Q<Label>(labelName);
            if (label == null)
            {
                label = new Label
                {
                    name = labelName,
                    pickingMode = PickingMode.Ignore
                };
                label.AddToClassList("chest__slot-label");
                content.Add(label);
            }

            if (ItemIconCatalogProvider.Catalog != null && ItemIconCatalogProvider.Catalog.TryGetIcon(itemId, out var iconSprite))
            {
                icon.style.backgroundImage = new StyleBackground(iconSprite);
                icon.EnableInClassList("is-missing", false);
            }
            else
            {
                icon.style.backgroundImage = new StyleBackground((Texture2D)null);
                icon.EnableInClassList("is-missing", true);
            }

            label.text = ItemDisplayNameFormatter.Format(itemId);
        }

        private void TryPointerDown(string container, int index)
        {
            var itemId = ResolveItemId(container, index);
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            _dragSourceContainer = container;
            _dragSourceIndex = index;
            _dragSourceItemId = itemId;
            _dragTargetContainer = null;
            _dragTargetIndex = -1;
        }

        private void TryPointerEnter(string container, int index)
        {
            if (string.IsNullOrWhiteSpace(_dragSourceItemId))
            {
                return;
            }

            _dragTargetContainer = container;
            _dragTargetIndex = index;
        }

        private void TryPointerUp(string container, int index)
        {
            if (string.IsNullOrWhiteSpace(_dragSourceItemId) || _dragSourceIndex < 0)
            {
                return;
            }

            var targetContainer = !string.IsNullOrWhiteSpace(_dragTargetContainer) ? _dragTargetContainer : container;
            var targetIndex = _dragTargetIndex >= 0 ? _dragTargetIndex : index;
            if (targetIndex < 0)
            {
                return;
            }

            var payload = new TabInventoryDragController.DragIntentPayload(_dragSourceContainer, _dragSourceIndex, targetContainer, targetIndex);
            var key = string.Equals(_dragSourceItemId, ResolveItemId(targetContainer, targetIndex), StringComparison.Ordinal)
                ? "inventory.drag.merge"
                : "inventory.drag.swap";
            IntentRaised?.Invoke(new UiIntent(key, payload));
            ClearDrag();
        }

        private string ResolveItemId(string container, int index)
        {
            List<VisualElement> slots = string.Equals(container, "container", StringComparison.Ordinal)
                ? _chestSlots
                : _playerSlots;
            if (index < 0 || index >= slots.Count)
            {
                return null;
            }

            return slots[index]?.tooltip;
        }

        private bool IsDragSource(string container, int index)
        {
            return string.Equals(_dragSourceContainer, container, StringComparison.Ordinal) && _dragSourceIndex == index;
        }

        private bool IsDragTarget(string container, int index)
        {
            return string.Equals(_dragTargetContainer, container, StringComparison.Ordinal) && _dragTargetIndex == index;
        }

        private void ClearDrag()
        {
            _dragSourceContainer = null;
            _dragSourceIndex = -1;
            _dragSourceItemId = null;
            _dragTargetContainer = null;
            _dragTargetIndex = -1;
        }
    }
}
