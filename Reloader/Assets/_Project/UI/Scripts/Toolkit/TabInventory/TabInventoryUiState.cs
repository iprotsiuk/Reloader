using System;
using System.Collections.Generic;
using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Toolkit.TabInventory
{
    public sealed class TabInventoryUiState : UiRenderState
    {
        public readonly struct SlotState
        {
            public SlotState(int index, string itemId, bool isOccupied)
            {
                Index = index;
                ItemId = itemId;
                IsOccupied = isOccupied;
            }

            public int Index { get; }
            public string ItemId { get; }
            public bool IsOccupied { get; }
        }

        private readonly SlotState[] _beltSlots;
        private readonly SlotState[] _backpackSlots;

        private TabInventoryUiState(
            bool isOpen,
            SlotState[] beltSlots,
            SlotState[] backpackSlots,
            string tooltipTitle,
            bool tooltipVisible,
            string activeSection)
            : base("tab-inventory")
        {
            IsOpen = isOpen;
            _beltSlots = beltSlots ?? Array.Empty<SlotState>();
            _backpackSlots = backpackSlots ?? Array.Empty<SlotState>();
            TooltipTitle = tooltipTitle ?? string.Empty;
            TooltipVisible = tooltipVisible;
            ActiveSection = string.IsNullOrWhiteSpace(activeSection) ? "inventory" : activeSection;
        }

        public bool IsOpen { get; }
        public IReadOnlyList<SlotState> BeltSlots => _beltSlots;
        public IReadOnlyList<SlotState> BackpackSlots => _backpackSlots;
        public string TooltipTitle { get; }
        public bool TooltipVisible { get; }
        public string ActiveSection { get; }

        public static TabInventoryUiState Create(
            bool isOpen,
            IEnumerable<SlotState> beltSlots,
            IEnumerable<SlotState> backpackSlots,
            string tooltipTitle,
            bool tooltipVisible,
            string activeSection = "inventory")
        {
            var belt = beltSlots == null ? Array.Empty<SlotState>() : new List<SlotState>(beltSlots).ToArray();
            var backpack = backpackSlots == null ? Array.Empty<SlotState>() : new List<SlotState>(backpackSlots).ToArray();
            return new TabInventoryUiState(isOpen, belt, backpack, tooltipTitle, tooltipVisible, activeSection);
        }
    }
}
