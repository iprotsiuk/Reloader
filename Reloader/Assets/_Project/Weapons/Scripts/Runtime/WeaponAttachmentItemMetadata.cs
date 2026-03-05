using UnityEngine;
using Reloader.Weapons.Data;

namespace Reloader.Weapons.Runtime
{
    [System.Serializable]
    public sealed class WeaponAttachmentItemMetadata
    {
        [SerializeField] private string _attachmentItemId;
        [SerializeField] private WeaponAttachmentSlotType _slotType;

        public string AttachmentItemId => string.IsNullOrWhiteSpace(_attachmentItemId) ? string.Empty : _attachmentItemId;
        public WeaponAttachmentSlotType SlotType => _slotType;

        public static WeaponAttachmentItemMetadata CreateForTests(string attachmentItemId, WeaponAttachmentSlotType slotType)
        {
            return new WeaponAttachmentItemMetadata
            {
                _attachmentItemId = attachmentItemId,
                _slotType = slotType
            };
        }
    }
}
