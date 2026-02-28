using System;

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

            if (!targetContainer.TrySetSlotItemId(targetIndex, sourceItemId))
            {
                return false;
            }

            player.BackpackItemIds.RemoveAt(sourceIndex);
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

            var sourceItemId = sourceContainer.GetSlotItemId(sourceIndex);
            if (string.IsNullOrWhiteSpace(sourceItemId))
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

            player.BackpackItemIds.Insert(insertAt, sourceItemId);
            sourceContainer.TrySetSlotItemId(sourceIndex, null);
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
