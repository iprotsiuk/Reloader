using System;

namespace Reloader.Inventory
{
    public static class InventoryTransferEngine
    {
        public static bool TryTransfer(
            InventoryContainerState sourceContainer,
            int sourceIndex,
            InventoryContainerState targetContainer,
            int targetIndex,
            out InventoryTransferResult result)
        {
            result = InventoryTransferResult.None;

            if (sourceContainer == null || !sourceContainer.IsValidIndex(sourceIndex) || !sourceContainer.TryGetSlot(sourceIndex, out var sourceStack))
            {
                result = InventoryTransferResult.InvalidSource;
                return false;
            }

            if (targetContainer == null || !targetContainer.IsValidIndex(targetIndex))
            {
                result = InventoryTransferResult.InvalidTarget;
                return false;
            }

            if (!sourceContainer.Permissions.CanDragOut || !targetContainer.Permissions.CanDropIn)
            {
                result = InventoryTransferResult.PermissionDenied;
                return false;
            }

            if (ReferenceEquals(sourceContainer, targetContainer) && sourceIndex == targetIndex)
            {
                result = InventoryTransferResult.NoChange;
                return false;
            }

            if (!targetContainer.TryGetSlot(targetIndex, out var targetStack))
            {
                targetContainer.TrySetSlot(targetIndex, sourceStack);
                sourceContainer.TryClearSlot(sourceIndex);
                result = InventoryTransferResult.Moved;
                return true;
            }

            var canMerge = sourceContainer.Permissions.CanMerge
                && targetContainer.Permissions.CanMerge
                && string.Equals(sourceStack.ItemId, targetStack.ItemId, StringComparison.Ordinal)
                && targetStack.Quantity < targetStack.MaxStack;

            if (canMerge)
            {
                var availableSpace = targetStack.MaxStack - targetStack.Quantity;
                var mergedAmount = Math.Min(sourceStack.Quantity, availableSpace);
                targetStack.SetQuantity(targetStack.Quantity + mergedAmount);

                if (mergedAmount == sourceStack.Quantity)
                {
                    sourceContainer.TryClearSlot(sourceIndex);
                    result = InventoryTransferResult.MergedFull;
                    return true;
                }

                sourceStack.SetQuantity(sourceStack.Quantity - mergedAmount);
                result = InventoryTransferResult.MergedPartial;
                return true;
            }

            if (!sourceContainer.Permissions.CanReorder || !targetContainer.Permissions.CanReorder)
            {
                result = InventoryTransferResult.PermissionDenied;
                return false;
            }

            sourceContainer.TrySetSlot(sourceIndex, targetStack);
            targetContainer.TrySetSlot(targetIndex, sourceStack);
            result = InventoryTransferResult.Swapped;
            return true;
        }
    }
}
