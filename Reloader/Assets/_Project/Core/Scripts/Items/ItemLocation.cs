using System;

namespace Reloader.Core.Items
{
    [Serializable]
    public struct ItemLocation
    {
        public ItemLocation(ItemOwnerType ownerType, string ownerId, string slotType, int slotIndex)
        {
            OwnerType = ownerType;
            OwnerId = ownerId ?? string.Empty;
            SlotType = slotType ?? string.Empty;
            SlotIndex = slotIndex;
        }

        public ItemOwnerType OwnerType;
        public string OwnerId;
        public string SlotType;
        public int SlotIndex;
    }
}
