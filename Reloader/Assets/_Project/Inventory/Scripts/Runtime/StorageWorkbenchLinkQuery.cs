using System;
using System.Collections.Generic;

namespace Reloader.Inventory
{
    public static class StorageWorkbenchLinkQuery
    {
        public readonly struct LinkedContainerItemRow
        {
            public LinkedContainerItemRow(string containerId, int slotIndex, ItemStackState stack)
            {
                ContainerId = containerId;
                SlotIndex = slotIndex;
                Stack = stack;
            }

            public string ContainerId { get; }
            public int SlotIndex { get; }
            public ItemStackState Stack { get; }
        }

        public static IEnumerable<LinkedContainerItemRow> EnumerateLinkedItems(
            IEnumerable<string> linkedContainerIds,
            Func<string, InventoryContainerState> resolveContainerState)
        {
            if (linkedContainerIds == null || resolveContainerState == null)
            {
                yield break;
            }

            foreach (var containerId in linkedContainerIds)
            {
                if (string.IsNullOrWhiteSpace(containerId))
                {
                    continue;
                }

                var state = resolveContainerState(containerId);
                if (state == null)
                {
                    continue;
                }

                for (var slotIndex = 0; slotIndex < state.SlotCount; slotIndex++)
                {
                    if (!state.TryGetSlot(slotIndex, out var stack) || stack == null)
                    {
                        continue;
                    }

                    yield return new LinkedContainerItemRow(containerId, slotIndex, stack);
                }
            }
        }
    }
}
