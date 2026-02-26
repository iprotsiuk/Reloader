using System;
using System.Collections.Generic;
using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Toolkit.BeltHud
{
    public sealed class BeltHudUiState : UiRenderState
    {
        public readonly struct SlotState
        {
            public SlotState(int index, string itemId, bool isOccupied, bool isSelected, int quantity = 1)
            {
                Index = index;
                ItemId = itemId;
                IsOccupied = isOccupied;
                IsSelected = isSelected;
                Quantity = quantity;
            }

            public int Index { get; }
            public string ItemId { get; }
            public bool IsOccupied { get; }
            public bool IsSelected { get; }
            public int Quantity { get; }
        }

        private readonly SlotState[] _slots;

        private BeltHudUiState(SlotState[] slots)
            : base("belt-hud")
        {
            _slots = slots ?? Array.Empty<SlotState>();
        }

        public IReadOnlyList<SlotState> Slots => _slots;

        public static BeltHudUiState Create(IEnumerable<SlotState> slots)
        {
            return new BeltHudUiState(slots == null ? Array.Empty<SlotState>() : new List<SlotState>(slots).ToArray());
        }
    }
}
