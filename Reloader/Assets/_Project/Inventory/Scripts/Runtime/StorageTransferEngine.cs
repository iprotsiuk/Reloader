using System;
using Reloader.Core.Events;

namespace Reloader.Inventory
{
    public static class StorageTransferEngine
    {
        private const string BackpackLocator = "backpack";
        private const string ContainerPrefix = "container:";

        public static bool TryMove(
            PlayerInventoryRuntime player,
            StorageContainerRegistry registry,
            string sourceLocator,
            int sourceIndex,
            string targetLocator,
            int targetIndex)
        {
            if (player == null || registry == null)
            {
                return false;
            }

            if (string.Equals(sourceLocator, BackpackLocator, StringComparison.Ordinal)
                && TryResolveContainer(registry, targetLocator, out var targetContainer))
            {
                return TryMoveBackpackToContainer(player, sourceIndex, targetContainer, targetIndex);
            }

            if (TryResolveContainer(registry, sourceLocator, out var sourceContainer)
                && string.Equals(targetLocator, BackpackLocator, StringComparison.Ordinal))
            {
                return TryMoveContainerToBackpack(player, sourceContainer, sourceIndex, targetIndex);
            }

            return false;
        }

        private static bool TryMoveBackpackToContainer(
            PlayerInventoryRuntime player,
            int sourceIndex,
            StorageContainerRuntime targetContainer,
            int targetIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= player.BackpackItemIds.Count)
            {
                return false;
            }

            var sourceItemId = player.BackpackItemIds[sourceIndex];
            if (string.IsNullOrWhiteSpace(sourceItemId))
            {
                return false;
            }

            if (!targetContainer.IsValidSlot(targetIndex))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(targetContainer.GetSlotItemId(targetIndex)))
            {
                return false;
            }

            var quantity = player.GetSlotQuantity(InventoryArea.Backpack, sourceIndex);
            var maxStack = player.GetSlotMaxStack(InventoryArea.Backpack, sourceIndex);
            if (quantity <= 0 || maxStack <= 0)
            {
                return false;
            }

            if (!player.TryRemoveFromSlot(InventoryArea.Backpack, sourceIndex, out var removedItemId, out var removedQuantity))
            {
                return false;
            }

            if (!targetContainer.TrySetSlotStack(targetIndex, new ItemStackState(removedItemId, removedQuantity, maxStack)))
            {
                return false;
            }

            return true;
        }

        private static bool TryMoveContainerToBackpack(
            PlayerInventoryRuntime player,
            StorageContainerRuntime sourceContainer,
            int sourceIndex,
            int targetIndex)
        {
            if (!sourceContainer.IsValidSlot(sourceIndex))
            {
                return false;
            }

            if (!sourceContainer.TryGetSlotStack(sourceIndex, out var sourceStack) || sourceStack == null)
            {
                return false;
            }

            var insertAt = targetIndex;
            if (insertAt < 0 || insertAt > player.BackpackItemIds.Count)
            {
                insertAt = player.BackpackItemIds.Count;
            }

            if (insertAt >= player.BackpackCapacity)
            {
                return false;
            }

            player.SetItemMaxStack(sourceStack.ItemId, sourceStack.MaxStack);
            if (!player.TryInsertBackpackStack(insertAt, sourceStack.ItemId, sourceStack.Quantity, sourceStack.MaxStack))
            {
                return false;
            }

            sourceContainer.TrySetSlotStack(sourceIndex, null);
            return true;
        }

        private static bool TryResolveContainer(
            StorageContainerRegistry registry,
            string locator,
            out StorageContainerRuntime container)
        {
            container = null;
            if (string.IsNullOrWhiteSpace(locator) || !locator.StartsWith(ContainerPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var containerId = locator.Substring(ContainerPrefix.Length);
            return registry.TryGet(containerId, out container);
        }
    }
}
