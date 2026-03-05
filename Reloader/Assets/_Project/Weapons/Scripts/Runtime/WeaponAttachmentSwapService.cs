using System;
using System.Collections.Generic;
using Reloader.Inventory;
using Reloader.Weapons.Data;

namespace Reloader.Weapons.Runtime
{
    public static class WeaponAttachmentSwapService
    {
        public static bool TrySwap(
            PlayerInventoryRuntime inventoryRuntime,
            WeaponDefinition weaponDefinition,
            WeaponRuntimeState runtimeState,
            IReadOnlyDictionary<string, WeaponAttachmentSlotType> attachmentSlotByItemId,
            WeaponAttachmentSlotType slotType,
            string newAttachmentItemId)
        {
            if (inventoryRuntime == null
                || weaponDefinition == null
                || runtimeState == null
                || attachmentSlotByItemId == null)
            {
                return false;
            }

            var currentAttachmentItemId = runtimeState.GetEquippedAttachmentItemId(slotType);
            if (string.IsNullOrWhiteSpace(newAttachmentItemId))
            {
                if (string.IsNullOrWhiteSpace(currentAttachmentItemId))
                {
                    return true;
                }

                if (!inventoryRuntime.TryAddStackItem(currentAttachmentItemId, 1, out _, out _, out _))
                {
                    return false;
                }

                runtimeState.SetEquippedAttachmentItemId(slotType, string.Empty);
                return true;
            }

            if (attachmentSlotByItemId.TryGetValue(newAttachmentItemId, out var resolvedSlotType)
                && resolvedSlotType != slotType)
            {
                return false;
            }

            var compatibleIds = weaponDefinition.GetCompatibleAttachmentItemIds(slotType);
            if (!ContainsItemId(compatibleIds, newAttachmentItemId))
            {
                return false;
            }

            if (string.Equals(currentAttachmentItemId, newAttachmentItemId, StringComparison.Ordinal))
            {
                return true;
            }

            if (inventoryRuntime.GetItemQuantity(newAttachmentItemId) <= 0)
            {
                return false;
            }

            if (!inventoryRuntime.TryRemoveStackItem(newAttachmentItemId, 1))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(currentAttachmentItemId))
            {
                if (!inventoryRuntime.TryAddStackItem(currentAttachmentItemId, 1, out _, out _, out _))
                {
                    inventoryRuntime.TryAddStackItem(newAttachmentItemId, 1, out _, out _, out _);
                    return false;
                }
            }

            runtimeState.SetEquippedAttachmentItemId(slotType, newAttachmentItemId);
            return true;
        }

        private static bool ContainsItemId(IReadOnlyList<string> itemIds, string itemId)
        {
            if (itemIds == null || string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            for (var i = 0; i < itemIds.Count; i++)
            {
                if (string.Equals(itemIds[i], itemId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
