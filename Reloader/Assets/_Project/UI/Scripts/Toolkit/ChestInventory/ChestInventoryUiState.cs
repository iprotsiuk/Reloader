using System.Collections.Generic;
using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Toolkit.ChestInventory
{
    public sealed class ChestInventoryUiState : UiRenderState
    {
        public readonly struct SlotState
        {
            public SlotState(int index, string itemId, bool occupied)
            {
                Index = index;
                ItemId = itemId;
                Occupied = occupied;
            }

            public int Index { get; }
            public string ItemId { get; }
            public bool Occupied { get; }
        }

        private ChestInventoryUiState(bool isOpen, IReadOnlyList<SlotState> chestSlots, IReadOnlyList<SlotState> playerSlots)
            : base("chest-inventory")
        {
            IsOpen = isOpen;
            ChestSlots = chestSlots;
            PlayerSlots = playerSlots;
        }

        public bool IsOpen { get; }
        public IReadOnlyList<SlotState> ChestSlots { get; }
        public IReadOnlyList<SlotState> PlayerSlots { get; }

        public static ChestInventoryUiState Create(bool isOpen, IReadOnlyList<SlotState> chestSlots, IReadOnlyList<SlotState> playerSlots)
        {
            return new ChestInventoryUiState(isOpen, chestSlots, playerSlots);
        }
    }
}
