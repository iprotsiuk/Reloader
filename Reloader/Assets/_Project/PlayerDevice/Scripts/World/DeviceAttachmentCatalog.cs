using System;
using System.Collections.Generic;
using Reloader.Core.Items;
using Reloader.PlayerDevice.Runtime;

namespace Reloader.PlayerDevice.World
{
    public sealed class DeviceAttachmentCatalog
    {
        public readonly struct Entry
        {
            public Entry(ItemDefinition itemDefinition, DeviceAttachmentType attachmentType)
            {
                ItemDefinition = itemDefinition;
                AttachmentType = attachmentType;
            }

            public ItemDefinition ItemDefinition { get; }

            public DeviceAttachmentType AttachmentType { get; }
        }

        private readonly Dictionary<string, DeviceAttachmentType> _attachmentByItemId;
        private readonly Dictionary<DeviceAttachmentType, string> _itemIdByAttachment;

        private DeviceAttachmentCatalog(
            Dictionary<string, DeviceAttachmentType> attachmentByItemId,
            Dictionary<DeviceAttachmentType, string> itemIdByAttachment)
        {
            _attachmentByItemId = attachmentByItemId;
            _itemIdByAttachment = itemIdByAttachment;
        }

        public static DeviceAttachmentCatalog Empty { get; } = new DeviceAttachmentCatalog(
            new Dictionary<string, DeviceAttachmentType>(StringComparer.Ordinal),
            new Dictionary<DeviceAttachmentType, string>());

        public static DeviceAttachmentCatalog FromDefinitions(IEnumerable<Entry> entries)
        {
            var attachmentByItemId = new Dictionary<string, DeviceAttachmentType>(StringComparer.Ordinal);
            var itemIdByAttachment = new Dictionary<DeviceAttachmentType, string>();
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (entry.ItemDefinition == null || string.IsNullOrWhiteSpace(entry.ItemDefinition.DefinitionId))
                    {
                        continue;
                    }

                    if (entry.AttachmentType == DeviceAttachmentType.None)
                    {
                        continue;
                    }

                    attachmentByItemId[entry.ItemDefinition.DefinitionId] = entry.AttachmentType;
                    itemIdByAttachment[entry.AttachmentType] = entry.ItemDefinition.DefinitionId;
                }
            }

            return new DeviceAttachmentCatalog(attachmentByItemId, itemIdByAttachment);
        }

        public bool TryGetAttachmentType(string itemId, out DeviceAttachmentType attachmentType)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                attachmentType = DeviceAttachmentType.None;
                return false;
            }

            return _attachmentByItemId.TryGetValue(itemId, out attachmentType);
        }

        public bool TryGetItemId(DeviceAttachmentType attachmentType, out string itemId)
        {
            if (attachmentType == DeviceAttachmentType.None)
            {
                itemId = null;
                return false;
            }

            return _itemIdByAttachment.TryGetValue(attachmentType, out itemId);
        }
    }
}
