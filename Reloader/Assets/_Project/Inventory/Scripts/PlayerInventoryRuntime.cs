using System;
using System.Collections.Generic;
using Reloader.Core.Events;

namespace Reloader.Inventory
{
    public sealed class PlayerInventoryRuntime
    {
        private sealed class StackSlotState
        {
            public int Quantity;
            public int MaxStack;

            public StackSlotState(int quantity, int maxStack)
            {
                Quantity = Math.Max(1, quantity);
                MaxStack = Math.Max(1, maxStack);
            }

            public StackSlotState Clone()
            {
                return new StackSlotState(Quantity, MaxStack);
            }
        }

        public const int BeltSlotCount = 5;

        public string[] BeltSlotItemIds { get; } = new string[BeltSlotCount];
        public List<string> BackpackItemIds { get; } = new List<string>();

        private readonly Dictionary<string, int> _itemQuantities = new Dictionary<string, int>();
        private readonly Dictionary<string, StackSlotState> _slotStackStates = new Dictionary<string, StackSlotState>();
        private readonly Dictionary<string, int> _itemMaxStacks = new Dictionary<string, int>();

        public int BackpackCapacity { get; private set; }
        public int SelectedBeltIndex { get; private set; } = -1;
        public string SelectedBeltItemId => SelectedBeltIndex >= 0 && SelectedBeltIndex < BeltSlotCount
            ? BeltSlotItemIds[SelectedBeltIndex]
            : null;

        public void SetBackpackCapacity(int capacity)
        {
            BackpackCapacity = Math.Max(0, capacity);
            if (BackpackItemIds.Count <= BackpackCapacity)
            {
                return;
            }

            for (var i = BackpackItemIds.Count - 1; i >= BackpackCapacity; i--)
            {
                RemoveBackpackAt(i);
            }

            RebuildItemQuantities();
        }

        public void SetItemMaxStack(string itemId, int maxStack)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            _itemMaxStacks[itemId] = Math.Max(1, maxStack);
            ClampExistingStacksToMax(itemId, _itemMaxStacks[itemId]);
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

            if (TryFindFirstEmptySlot(out storedArea, out storedIndex))
            {
                SetSlotItem(storedArea, storedIndex, itemId);
                SetSlotStackState(storedArea, storedIndex, new StackSlotState(1, 1));
                RebuildItemQuantities();
                rejectReason = PickupRejectReason.NoSpace;
                return true;
            }

            rejectReason = PickupRejectReason.NoSpace;
            return false;
        }

        public int GetItemQuantity(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            return _itemQuantities.TryGetValue(itemId, out var quantity) ? Math.Max(0, quantity) : 0;
        }

        public int GetSlotQuantity(InventoryArea area, int index)
        {
            if (!TryGetSlotItem(area, index, out var itemId) || string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            return GetOrCreateSlotState(area, index, itemId).Quantity;
        }

        public int GetSlotMaxStack(InventoryArea area, int index)
        {
            if (!TryGetSlotItem(area, index, out var itemId) || string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            return GetOrCreateSlotState(area, index, itemId).MaxStack;
        }

        public bool TryAddStackItem(
            string itemId,
            int quantity,
            out InventoryArea storedArea,
            out int storedIndex,
            out PickupRejectReason rejectReason)
        {
            storedArea = InventoryArea.Belt;
            storedIndex = -1;
            rejectReason = PickupRejectReason.InvalidItem;

            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                return false;
            }

            var maxStack = ResolveItemMaxStack(itemId);
            if (!CanAcceptStackQuantityInternal(itemId, quantity, maxStack))
            {
                rejectReason = PickupRejectReason.NoSpace;
                return false;
            }

            var remaining = quantity;
            var changedAny = false;

            for (var i = 0; i < BeltSlotCount && remaining > 0; i++)
            {
                changedAny |= TryMergeIntoSlot(InventoryArea.Belt, i, itemId, ref remaining, maxStack, ref storedArea, ref storedIndex);
            }

            for (var i = 0; i < BackpackItemIds.Count && remaining > 0; i++)
            {
                changedAny |= TryMergeIntoSlot(InventoryArea.Backpack, i, itemId, ref remaining, maxStack, ref storedArea, ref storedIndex);
            }

            while (remaining > 0)
            {
                if (!TryFindFirstEmptySlot(out var area, out var index))
                {
                    rejectReason = PickupRejectReason.NoSpace;
                    return false;
                }

                var stackQuantity = Math.Min(maxStack, remaining);
                SetSlotItem(area, index, itemId);
                SetSlotStackState(area, index, new StackSlotState(stackQuantity, maxStack));
                if (!changedAny)
                {
                    storedArea = area;
                    storedIndex = index;
                }

                changedAny = true;
                remaining -= stackQuantity;
            }

            RebuildItemQuantities();
            rejectReason = PickupRejectReason.NoSpace;
            return true;
        }

        public bool CanAcceptStackItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            return CanAcceptStackQuantity(itemId, 1);
        }

        public bool CanAcceptStackQuantity(string itemId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                return false;
            }

            return CanAcceptStackQuantityInternal(itemId, quantity, ResolveItemMaxStack(itemId));
        }

        public bool TryRemoveStackItem(string itemId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                return false;
            }

            if (GetItemQuantity(itemId) < quantity)
            {
                return false;
            }

            var remaining = quantity;

            for (var i = 0; i < BeltSlotCount && remaining > 0; i++)
            {
                if (BeltSlotItemIds[i] != itemId)
                {
                    continue;
                }

                RemoveQuantityFromSlot(InventoryArea.Belt, i, ref remaining);
            }

            for (var i = 0; i < BackpackItemIds.Count && remaining > 0;)
            {
                if (BackpackItemIds[i] != itemId)
                {
                    i++;
                    continue;
                }

                var cleared = RemoveQuantityFromSlot(InventoryArea.Backpack, i, ref remaining);
                if (!cleared)
                {
                    i++;
                }
            }

            RebuildItemQuantities();
            return remaining == 0;
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
            if (!TryGetSlotItem(sourceArea, sourceIndex, out var sourceItem) || string.IsNullOrWhiteSpace(sourceItem))
            {
                return false;
            }

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

            if (targetArea == InventoryArea.Belt)
            {
                if (!IsValidBeltIndex(targetIndex))
                {
                    return false;
                }

                if (TryMergeSlots(InventoryArea.Belt, sourceIndex, InventoryArea.Belt, targetIndex))
                {
                    return true;
                }

                SwapSlots(InventoryArea.Belt, sourceIndex, InventoryArea.Belt, targetIndex);
                RebuildItemQuantities();
                return true;
            }

            if (targetArea != InventoryArea.Backpack || targetIndex < 0 || targetIndex > BackpackItemIds.Count || targetIndex >= BackpackCapacity)
            {
                return false;
            }

            if (targetIndex < BackpackItemIds.Count)
            {
                if (TryMergeSlots(InventoryArea.Belt, sourceIndex, InventoryArea.Backpack, targetIndex))
                {
                    return true;
                }

                SwapSlots(InventoryArea.Belt, sourceIndex, InventoryArea.Backpack, targetIndex);
                RebuildItemQuantities();
                return true;
            }

            var sourceItem = BeltSlotItemIds[sourceIndex];
            var sourceState = PopSlotStackState(InventoryArea.Belt, sourceIndex, sourceItem);

            BackpackItemIds.Add(sourceItem);
            BeltSlotItemIds[sourceIndex] = null;

            if (sourceState != null)
            {
                SetSlotStackState(InventoryArea.Backpack, BackpackItemIds.Count - 1, sourceState);
            }

            RebuildItemQuantities();
            return true;
        }

        private bool TryMoveFromBackpack(int sourceIndex, InventoryArea targetArea, int targetIndex)
        {
            if (!IsValidBackpackIndex(sourceIndex))
            {
                return false;
            }

            if (targetArea == InventoryArea.Backpack)
            {
                if (!IsValidBackpackIndex(targetIndex))
                {
                    return false;
                }

                if (TryMergeSlots(InventoryArea.Backpack, sourceIndex, InventoryArea.Backpack, targetIndex))
                {
                    return true;
                }

                SwapSlots(InventoryArea.Backpack, sourceIndex, InventoryArea.Backpack, targetIndex);
                RebuildItemQuantities();
                return true;
            }

            if (targetArea != InventoryArea.Belt || !IsValidBeltIndex(targetIndex))
            {
                return false;
            }

            if (TryMergeSlots(InventoryArea.Backpack, sourceIndex, InventoryArea.Belt, targetIndex))
            {
                return true;
            }

            var sourceItem = BackpackItemIds[sourceIndex];
            var sourceState = PopSlotStackState(InventoryArea.Backpack, sourceIndex, sourceItem);
            var beltItem = BeltSlotItemIds[targetIndex];
            var beltState = PopSlotStackState(InventoryArea.Belt, targetIndex, beltItem);

            BeltSlotItemIds[targetIndex] = sourceItem;

            if (sourceState != null)
            {
                SetSlotStackState(InventoryArea.Belt, targetIndex, sourceState);
            }

            if (string.IsNullOrWhiteSpace(beltItem))
            {
                RemoveBackpackAt(sourceIndex);
            }
            else
            {
                BackpackItemIds[sourceIndex] = beltItem;
                if (beltState != null)
                {
                    SetSlotStackState(InventoryArea.Backpack, sourceIndex, beltState);
                }
            }

            RebuildItemQuantities();
            return true;
        }

        private bool TryMergeSlots(InventoryArea sourceArea, int sourceIndex, InventoryArea targetArea, int targetIndex)
        {
            if (sourceArea == targetArea && sourceIndex == targetIndex)
            {
                return false;
            }

            if (!TryGetSlotItem(sourceArea, sourceIndex, out var sourceItem)
                || !TryGetSlotItem(targetArea, targetIndex, out var targetItem)
                || string.IsNullOrWhiteSpace(sourceItem)
                || string.IsNullOrWhiteSpace(targetItem)
                || !string.Equals(sourceItem, targetItem, StringComparison.Ordinal))
            {
                return false;
            }

            var sourceState = GetOrCreateSlotState(sourceArea, sourceIndex, sourceItem);
            var targetState = GetOrCreateSlotState(targetArea, targetIndex, targetItem);
            if (targetState.Quantity >= targetState.MaxStack)
            {
                return false;
            }

            var available = targetState.MaxStack - targetState.Quantity;
            var merged = Math.Min(sourceState.Quantity, available);
            if (merged <= 0)
            {
                return false;
            }

            targetState.Quantity += merged;
            sourceState.Quantity -= merged;

            if (sourceState.Quantity <= 0)
            {
                if (sourceArea == InventoryArea.Belt)
                {
                    BeltSlotItemIds[sourceIndex] = null;
                    ClearSlotStackState(sourceArea, sourceIndex);
                }
                else
                {
                    RemoveBackpackAt(sourceIndex);
                }
            }
            else
            {
                SetSlotStackState(sourceArea, sourceIndex, sourceState);
            }

            SetSlotStackState(targetArea, targetIndex, targetState);
            RebuildItemQuantities();
            return true;
        }

        private void SwapSlots(InventoryArea firstArea, int firstIndex, InventoryArea secondArea, int secondIndex)
        {
            var firstItem = GetSlotItem(firstArea, firstIndex);
            var secondItem = GetSlotItem(secondArea, secondIndex);

            var firstState = PopSlotStackState(firstArea, firstIndex, firstItem);
            var secondState = PopSlotStackState(secondArea, secondIndex, secondItem);

            SetSlotItem(firstArea, firstIndex, secondItem);
            SetSlotItem(secondArea, secondIndex, firstItem);

            if (secondState != null && !string.IsNullOrWhiteSpace(secondItem))
            {
                SetSlotStackState(firstArea, firstIndex, secondState);
            }

            if (firstState != null && !string.IsNullOrWhiteSpace(firstItem))
            {
                SetSlotStackState(secondArea, secondIndex, firstState);
            }
        }

        private bool RemoveQuantityFromSlot(InventoryArea area, int index, ref int remaining)
        {
            if (!TryGetSlotItem(area, index, out var itemId) || string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            var state = GetOrCreateSlotState(area, index, itemId);
            var removed = Math.Min(remaining, state.Quantity);
            remaining -= removed;
            state.Quantity -= removed;

            if (state.Quantity > 0)
            {
                SetSlotStackState(area, index, state);
                return false;
            }

            if (area == InventoryArea.Belt)
            {
                BeltSlotItemIds[index] = null;
                ClearSlotStackState(area, index);
                return true;
            }

            RemoveBackpackAt(index);
            return true;
        }

        private bool TryMergeIntoSlot(
            InventoryArea area,
            int index,
            string itemId,
            ref int remaining,
            int maxStack,
            ref InventoryArea storedArea,
            ref int storedIndex)
        {
            if (remaining <= 0 || !TryGetSlotItem(area, index, out var slotItem) || !string.Equals(slotItem, itemId, StringComparison.Ordinal))
            {
                return false;
            }

            var state = GetOrCreateSlotState(area, index, itemId);
            var slotMax = Math.Max(1, state.MaxStack);
            if (slotMax != maxStack)
            {
                slotMax = Math.Max(slotMax, maxStack);
                state.MaxStack = slotMax;
            }

            if (state.Quantity >= slotMax)
            {
                return false;
            }

            var merged = Math.Min(slotMax - state.Quantity, remaining);
            state.Quantity += merged;
            remaining -= merged;
            SetSlotStackState(area, index, state);

            if (storedIndex < 0)
            {
                storedArea = area;
                storedIndex = index;
            }

            return merged > 0;
        }

        private bool CanAcceptStackQuantityInternal(string itemId, int quantity, int maxStack)
        {
            var remaining = quantity;

            for (var i = 0; i < BeltSlotCount && remaining > 0; i++)
            {
                if (BeltSlotItemIds[i] != itemId)
                {
                    continue;
                }

                var state = GetOrCreateSlotState(InventoryArea.Belt, i, itemId);
                remaining -= Math.Max(0, state.MaxStack - state.Quantity);
            }

            for (var i = 0; i < BackpackItemIds.Count && remaining > 0; i++)
            {
                if (BackpackItemIds[i] != itemId)
                {
                    continue;
                }

                var state = GetOrCreateSlotState(InventoryArea.Backpack, i, itemId);
                remaining -= Math.Max(0, state.MaxStack - state.Quantity);
            }

            if (remaining <= 0)
            {
                return true;
            }

            var emptySlots = 0;
            for (var i = 0; i < BeltSlotCount; i++)
            {
                if (string.IsNullOrWhiteSpace(BeltSlotItemIds[i]))
                {
                    emptySlots++;
                }
            }

            emptySlots += Math.Max(0, BackpackCapacity - BackpackItemIds.Count);
            var emptyCapacity = (long)emptySlots * Math.Max(1, maxStack);
            return emptyCapacity >= remaining;
        }

        private int ResolveItemMaxStack(string itemId)
        {
            return _itemMaxStacks.TryGetValue(itemId, out var configuredMax)
                ? Math.Max(1, configuredMax)
                : int.MaxValue;
        }

        private void ClampExistingStacksToMax(string itemId, int maxStack)
        {
            for (var i = 0; i < BeltSlotCount; i++)
            {
                if (BeltSlotItemIds[i] != itemId)
                {
                    continue;
                }

                var state = GetOrCreateSlotState(InventoryArea.Belt, i, itemId);
                state.MaxStack = maxStack;
                state.Quantity = Math.Min(state.Quantity, maxStack);
                SetSlotStackState(InventoryArea.Belt, i, state);
            }

            for (var i = 0; i < BackpackItemIds.Count; i++)
            {
                if (BackpackItemIds[i] != itemId)
                {
                    continue;
                }

                var state = GetOrCreateSlotState(InventoryArea.Backpack, i, itemId);
                state.MaxStack = maxStack;
                state.Quantity = Math.Min(state.Quantity, maxStack);
                SetSlotStackState(InventoryArea.Backpack, i, state);
            }

            RebuildItemQuantities();
        }

        private bool TryFindFirstEmptySlot(out InventoryArea area, out int index)
        {
            for (var i = 0; i < BeltSlotCount; i++)
            {
                if (!string.IsNullOrWhiteSpace(BeltSlotItemIds[i]))
                {
                    continue;
                }

                area = InventoryArea.Belt;
                index = i;
                return true;
            }

            if (BackpackItemIds.Count < BackpackCapacity)
            {
                area = InventoryArea.Backpack;
                index = BackpackItemIds.Count;
                return true;
            }

            area = InventoryArea.Belt;
            index = -1;
            return false;
        }

        private bool IsValidBeltIndex(int index)
        {
            return index >= 0 && index < BeltSlotCount;
        }

        private bool IsValidBackpackIndex(int index)
        {
            return index >= 0 && index < BackpackItemIds.Count;
        }

        private bool TryGetSlotItem(InventoryArea area, int index, out string itemId)
        {
            itemId = GetSlotItem(area, index);
            return !string.IsNullOrWhiteSpace(itemId);
        }

        private string GetSlotItem(InventoryArea area, int index)
        {
            if (area == InventoryArea.Belt)
            {
                return IsValidBeltIndex(index) ? BeltSlotItemIds[index] : null;
            }

            if (area == InventoryArea.Backpack)
            {
                return IsValidBackpackIndex(index) ? BackpackItemIds[index] : null;
            }

            return null;
        }

        private void SetSlotItem(InventoryArea area, int index, string itemId)
        {
            if (area == InventoryArea.Belt)
            {
                if (IsValidBeltIndex(index))
                {
                    BeltSlotItemIds[index] = itemId;
                }

                return;
            }

            if (area != InventoryArea.Backpack)
            {
                return;
            }

            if (index == BackpackItemIds.Count)
            {
                BackpackItemIds.Add(itemId);
                return;
            }

            if (IsValidBackpackIndex(index))
            {
                BackpackItemIds[index] = itemId;
            }
        }

        private void RebuildItemQuantities()
        {
            _itemQuantities.Clear();

            for (var i = 0; i < BeltSlotCount; i++)
            {
                RebuildSlotQuantity(InventoryArea.Belt, i);
            }

            for (var i = 0; i < BackpackItemIds.Count; i++)
            {
                RebuildSlotQuantity(InventoryArea.Backpack, i);
            }
        }

        private void RebuildSlotQuantity(InventoryArea area, int index)
        {
            var itemId = GetSlotItem(area, index);
            if (string.IsNullOrWhiteSpace(itemId))
            {
                ClearSlotStackState(area, index);
                return;
            }

            var state = GetOrCreateSlotState(area, index, itemId);
            if (_itemQuantities.TryGetValue(itemId, out var existing))
            {
                _itemQuantities[itemId] = existing + state.Quantity;
                return;
            }

            _itemQuantities[itemId] = state.Quantity;
        }

        private StackSlotState GetOrCreateSlotState(InventoryArea area, int index, string itemId)
        {
            var key = GetSlotKey(area, index);
            if (_slotStackStates.TryGetValue(key, out var existing))
            {
                existing.Quantity = Math.Max(1, existing.Quantity);
                existing.MaxStack = Math.Max(existing.Quantity, Math.Max(1, existing.MaxStack));
                return existing;
            }

            var created = new StackSlotState(1, ResolveItemMaxStack(itemId));
            _slotStackStates[key] = created;
            return created;
        }

        private StackSlotState PopSlotStackState(InventoryArea area, int index, string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                ClearSlotStackState(area, index);
                return null;
            }

            var key = GetSlotKey(area, index);
            if (_slotStackStates.TryGetValue(key, out var existing))
            {
                _slotStackStates.Remove(key);
                return existing.Clone();
            }

            return new StackSlotState(1, ResolveItemMaxStack(itemId));
        }

        private void SetSlotStackState(InventoryArea area, int index, StackSlotState state)
        {
            if (state == null)
            {
                ClearSlotStackState(area, index);
                return;
            }

            _slotStackStates[GetSlotKey(area, index)] = new StackSlotState(state.Quantity, state.MaxStack);
        }

        private void ClearSlotStackState(InventoryArea area, int index)
        {
            _slotStackStates.Remove(GetSlotKey(area, index));
        }

        private void RemoveBackpackAt(int removeIndex)
        {
            if (!IsValidBackpackIndex(removeIndex))
            {
                return;
            }

            BackpackItemIds.RemoveAt(removeIndex);
            _slotStackStates.Remove(GetSlotKey(InventoryArea.Backpack, removeIndex));

            for (var i = removeIndex; i < BackpackItemIds.Count; i++)
            {
                var fromKey = GetSlotKey(InventoryArea.Backpack, i + 1);
                var toKey = GetSlotKey(InventoryArea.Backpack, i);

                if (_slotStackStates.TryGetValue(fromKey, out var state))
                {
                    _slotStackStates[toKey] = state;
                    _slotStackStates.Remove(fromKey);
                }
                else
                {
                    _slotStackStates.Remove(toKey);
                }
            }
        }

        private static string GetSlotKey(InventoryArea area, int index)
        {
            return area == InventoryArea.Belt ? "B:" + index : "P:" + index;
        }
    }
}
