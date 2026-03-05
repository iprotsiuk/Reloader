namespace Reloader.UI.Toolkit.TabInventory
{
    public readonly struct TabInventoryAttachmentContextIntentPayload
    {
        public TabInventoryAttachmentContextIntentPayload(string container, int slotIndex, string itemId)
        {
            Container = container;
            SlotIndex = slotIndex;
            ItemId = itemId;
        }

        public string Container { get; }
        public int SlotIndex { get; }
        public string ItemId { get; }
    }
}
