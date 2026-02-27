using System;
using System.Collections.Generic;
using Reloader.Core.Events;

namespace Reloader.Inventory
{
    public sealed class PlayerInventoryRuntime
    {
        public const int BeltSlotCount = 5;

        public string[] BeltSlotItemIds { get; } = new string[BeltSlotCount];
        public List<string> BackpackItemIds { get; } = new List<string>();
        public int BackpackCapacity { get; private set; }
        public int SelectedBeltIndex { get; private set; } = -1;
        public string SelectedBeltItemId => SelectedBeltIndex >= 0 && SelectedBeltIndex < BeltSlotCount
            ? BeltSlotItemIds[SelectedBeltIndex]
            : null;

        public void SetBackpackCapacity(int capacity)
        {
            BackpackCapacity = Math.Max(0, capacity);
            if (BackpackItemIds.Count > BackpackCapacity)
            {
                BackpackItemIds.RemoveRange(BackpackCapacity, BackpackItemIds.Count - BackpackCapacity);
            }
        }

        public bool TryStoreItem(
            string itemId,
            out InventoryArea storedArea,
            out int storedIndex,
            out PickupRejectReason rejectReason)
        {
            storedArea = InventoryArea.Belt;
            storedIndex = -1;
            rejectReason = PickupRejectReason.InvalidItem;

            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            for (var i = 0; i < BeltSlotCount; i++)
            {
                if (!string.IsNullOrWhiteSpace(BeltSlotItemIds[i]))
                {
                    continue;
                }

                BeltSlotItemIds[i] = itemId;
                storedArea = InventoryArea.Belt;
                storedIndex = i;
                rejectReason = PickupRejectReason.NoSpace;
                return true;
            }

            if (BackpackItemIds.Count < BackpackCapacity)
            {
                BackpackItemIds.Add(itemId);
                storedArea = InventoryArea.Backpack;
                storedIndex = BackpackItemIds.Count - 1;
                rejectReason = PickupRejectReason.NoSpace;
                return true;
            }

            rejectReason = PickupRejectReason.NoSpace;
            return false;
        }

        public void SelectBeltSlot(int beltIndex)
        {
            if (beltIndex < 0 || beltIndex >= BeltSlotCount)
            {
                return;
            }

            if (SelectedBeltIndex == beltIndex)
            {
                return;
            }

            SelectedBeltIndex = beltIndex;
        }

        public bool TryMoveItem(InventoryArea sourceArea, int sourceIndex, InventoryArea targetArea, int targetIndex)
        {
            if (sourceArea == InventoryArea.Belt)
            {
                return TryMoveFromBelt(sourceIndex, targetArea, targetIndex);
            }

            if (sourceArea == InventoryArea.Backpack)
            {
                return TryMoveFromBackpack(sourceIndex, targetArea, targetIndex);
            }

            return false;
        }

        private bool TryMoveFromBelt(int sourceIndex, InventoryArea targetArea, int targetIndex)
        {
            if (!IsValidBeltIndex(sourceIndex))
            {
                return false;
            }

            var sourceItem = BeltSlotItemIds[sourceIndex];
            if (string.IsNullOrWhiteSpace(sourceItem))
            {
                return false;
            }

            if (targetArea == InventoryArea.Belt)
            {
                if (!IsValidBeltIndex(targetIndex))
                {
                    return false;
                }

                var targetItem = BeltSlotItemIds[targetIndex];
                BeltSlotItemIds[targetIndex] = sourceItem;
                BeltSlotItemIds[sourceIndex] = targetItem;
                return true;
            }

            if (targetArea != InventoryArea.Backpack || targetIndex < 0 || targetIndex >= BackpackCapacity)
            {
                return false;
            }

            if (targetIndex < BackpackItemIds.Count)
            {
                var targetItem = BackpackItemIds[targetIndex];
                BackpackItemIds[targetIndex] = sourceItem;
                BeltSlotItemIds[sourceIndex] = targetItem;
                return true;
            }

            if (targetIndex == BackpackItemIds.Count && BackpackItemIds.Count < BackpackCapacity)
            {
                BackpackItemIds.Add(sourceItem);
                BeltSlotItemIds[sourceIndex] = null;
                return true;
            }

            return false;
        }

        private bool TryMoveFromBackpack(int sourceIndex, InventoryArea targetArea, int targetIndex)
        {
            if (!IsValidBackpackIndex(sourceIndex))
            {
                return false;
            }

            var sourceItem = BackpackItemIds[sourceIndex];
            if (string.IsNullOrWhiteSpace(sourceItem))
            {
                return false;
            }

            if (targetArea == InventoryArea.Backpack)
            {
                if (!IsValidBackpackIndex(targetIndex))
                {
                    return false;
                }

                var targetItem = BackpackItemIds[targetIndex];
                BackpackItemIds[targetIndex] = sourceItem;
                BackpackItemIds[sourceIndex] = targetItem;
                return true;
            }

            if (targetArea != InventoryArea.Belt || !IsValidBeltIndex(targetIndex))
            {
                return false;
            }

            var beltItem = BeltSlotItemIds[targetIndex];
            BeltSlotItemIds[targetIndex] = sourceItem;

            if (string.IsNullOrWhiteSpace(beltItem))
            {
                BackpackItemIds.RemoveAt(sourceIndex);
            }
            else
            {
                BackpackItemIds[sourceIndex] = beltItem;
            }

            return true;
        }

        private static bool IsValidBeltIndex(int index)
        {
            return index >= 0 && index < BeltSlotCount;
        }

        private bool IsValidBackpackIndex(int index)
        {
            return index >= 0 && index < BackpackItemIds.Count;
        }
    }
}
