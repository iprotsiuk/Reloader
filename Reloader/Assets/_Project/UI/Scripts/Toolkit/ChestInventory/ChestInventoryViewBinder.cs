using System;
using System.Collections.Generic;
using Reloader.Core.UI;
using Reloader.UI;
using Reloader.UI.Toolkit;
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
        private VisualElement _tooltip;
        private Label _tooltipTitle;
        private Label _tooltipSpecs;
        private InventoryItemTooltipPresenter _tooltipPresenter;
        private readonly List<VisualElement> _chestSlots = new List<VisualElement>();
        private readonly List<VisualElement> _playerSlots = new List<VisualElement>();
        private string[] _chestItemIds = Array.Empty<string>();
        private string[] _playerItemIds = Array.Empty<string>();

        private string _dragSourceContainer;
        private int _dragSourceIndex = -1;
        private string _dragSourceItemId;
        private string _dragTargetContainer;
        private int _dragTargetIndex = -1;
        private string _hoverContainer;
        private int _hoverIndex = -1;
        private string _hoveredTooltipItemId;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root, int chestSlotCount, int playerSlotCount)
        {
            _panel = root?.Q<VisualElement>("chest__panel");
            _chestGrid = root?.Q<VisualElement>("chest__left-grid");
            _playerGrid = root?.Q<VisualElement>("chest__right-grid");
            _tooltip = root?.Q<VisualElement>("chest__tooltip");
            _tooltipTitle = root?.Q<Label>("chest__tooltip-title");
            _tooltipSpecs = root?.Q<Label>("chest__tooltip-specs");
            if (_tooltip == null && _panel != null)
            {
                _tooltip = new VisualElement { name = "chest__tooltip" };
                _tooltip.AddToClassList("chest__tooltip");
                _tooltip.pickingMode = PickingMode.Ignore;
                _tooltipTitle = new Label { name = "chest__tooltip-title" };
                _tooltipTitle.AddToClassList("chest__tooltip-title");
                _tooltipSpecs = new Label { name = "chest__tooltip-specs" };
                _tooltipSpecs.AddToClassList("chest__tooltip-specs");
                _tooltip.Add(_tooltipTitle);
                _tooltip.Add(_tooltipSpecs);
                _panel.Add(_tooltip);
            }
            _tooltipPresenter = InventoryItemTooltipPresenter.CreateOrBind(root, _panel ?? root, "chest");

            BuildSlots(_chestGrid, _chestSlots, chestSlotCount, "container");
            BuildSlots(_playerGrid, _playerSlots, playerSlotCount, "backpack");
            _chestItemIds = new string[Math.Max(0, chestSlotCount)];
            _playerItemIds = new string[Math.Max(0, playerSlotCount)];
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
            if (!chestState.IsOpen)
            {
                HideTooltip();
                return;
            }

            RevalidateHoveredTooltip();
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
                slot.RegisterCallback<PointerEnterEvent>(evt => TryPointerEnter(capturedContainer, capturedIndex, evt.position));
                slot.RegisterCallback<PointerMoveEvent>(evt => TryPointerMove(capturedContainer, capturedIndex, evt.position));
                slot.RegisterCallback<PointerLeaveEvent>(_ => TryPointerLeave());
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
                SetItemId(container, i, itemId);

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
            if (label != null)
            {
                label.RemoveFromHierarchy();
            }

            icon.style.backgroundImage = ItemIconResolver.ResolveBackground(itemId);
            icon.EnableInClassList("is-missing", false);
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

        private void TryPointerEnter(string container, int index, Vector2 panelPosition)
        {
            _hoverContainer = container;
            _hoverIndex = index;
            ShowTooltip(container, index, panelPosition);

            if (string.IsNullOrWhiteSpace(_dragSourceItemId))
            {
                return;
            }

            _dragTargetContainer = container;
            _dragTargetIndex = index;
        }

        private void TryPointerMove(string container, int index, Vector2 panelPosition)
        {
            _hoverContainer = container;
            _hoverIndex = index;
            ShowTooltip(container, index, panelPosition);
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

        private void TryPointerLeave()
        {
            _hoverContainer = null;
            _hoverIndex = -1;
            _hoveredTooltipItemId = null;
            HideTooltip();
        }

        private string ResolveItemId(string container, int index)
        {
            var source = string.Equals(container, "container", StringComparison.Ordinal)
                ? _chestItemIds
                : _playerItemIds;
            if (index < 0 || index >= source.Length)
            {
                return null;
            }

            return source[index];
        }

        private void SetItemId(string container, int index, string itemId)
        {
            var source = string.Equals(container, "container", StringComparison.Ordinal)
                ? _chestItemIds
                : _playerItemIds;
            if (index < 0 || index >= source.Length)
            {
                return;
            }

            source[index] = itemId ?? string.Empty;
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
            HideTooltip();
        }

        private void ShowTooltip(string container, int index, Vector2 panelPosition)
        {
            var itemId = ResolveItemId(container, index);
            if (string.IsNullOrWhiteSpace(itemId))
            {
                _hoveredTooltipItemId = null;
                HideTooltip();
                return;
            }

            if (_tooltipPresenter != null)
            {
                _hoveredTooltipItemId = _tooltipPresenter.TryShowItem(itemId, 1, 1, panelPosition)
                    ? itemId
                    : null;
                return;
            }

            _hoveredTooltipItemId = null;
        }

        private void RevalidateHoveredTooltip()
        {
            if (string.IsNullOrWhiteSpace(_hoveredTooltipItemId))
            {
                return;
            }

            var currentItemId = ResolveItemId(_hoverContainer, _hoverIndex);
            if (string.Equals(currentItemId, _hoveredTooltipItemId, StringComparison.Ordinal))
            {
                return;
            }

            _hoveredTooltipItemId = null;
            HideTooltip();
        }

        private void HideTooltip()
        {
            if (_tooltipPresenter != null)
            {
                _tooltipPresenter.Hide();
            }
            if (_tooltip != null)
            {
                _tooltip.style.display = DisplayStyle.None;
            }
        }
    }
}
