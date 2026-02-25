using System;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.TabInventory
{
    public sealed class TabInventoryViewBinder : IUiViewBinder
    {
        private VisualElement _panel;
        private VisualElement[] _beltSlots = Array.Empty<VisualElement>();
        private VisualElement[] _backpackSlots = Array.Empty<VisualElement>();
        private VisualElement _tooltip;
        private Label _tooltipTitle;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root, int beltSlotCount, int backpackSlotCount)
        {
            _panel = root?.Q<VisualElement>("inventory__panel");
            _tooltip = root?.Q<VisualElement>("inventory__tooltip");
            _tooltipTitle = root?.Q<Label>("inventory__tooltip-title");

            _beltSlots = new VisualElement[Math.Max(0, beltSlotCount)];
            for (var i = 0; i < _beltSlots.Length; i++)
            {
                _beltSlots[i] = root?.Q<VisualElement>($"inventory__belt-slot-{i}");
            }

            _backpackSlots = new VisualElement[Math.Max(0, backpackSlotCount)];
            for (var i = 0; i < _backpackSlots.Length; i++)
            {
                _backpackSlots[i] = root?.Q<VisualElement>($"inventory__backpack-slot-{i}");
            }
        }

        public void Render(UiRenderState state)
        {
            if (state is not TabInventoryUiState inventoryState)
            {
                return;
            }

            if (_panel != null)
            {
                _panel.style.display = inventoryState.IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            }

            ApplyOccupancy(_beltSlots, inventoryState.BeltSlots);
            ApplyOccupancy(_backpackSlots, inventoryState.BackpackSlots);

            if (_tooltip != null)
            {
                _tooltip.style.display = inventoryState.TooltipVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_tooltipTitle != null)
            {
                _tooltipTitle.text = inventoryState.TooltipTitle;
            }
        }

        private static void ApplyOccupancy(VisualElement[] slotElements, System.Collections.Generic.IReadOnlyList<TabInventoryUiState.SlotState> slots)
        {
            var limit = Math.Min(slotElements.Length, slots.Count);
            for (var i = 0; i < limit; i++)
            {
                var element = slotElements[i];
                if (element == null)
                {
                    continue;
                }

                element.EnableInClassList("is-occupied", slots[i].IsOccupied);
            }
        }
    }
}
