using System.Collections.Generic;
using UnityEngine;

namespace Reloader.Weapons.Data
{
    [System.Serializable]
    public struct WeaponAttachmentCompatibility
    {
        [SerializeField] private WeaponAttachmentSlotType _slotType;
        [SerializeField] private string[] _compatibleAttachmentItemIds;

        public WeaponAttachmentSlotType SlotType => _slotType;

        public IReadOnlyList<string> CompatibleAttachmentItemIds =>
            _compatibleAttachmentItemIds ?? System.Array.Empty<string>();

        public static WeaponAttachmentCompatibility Create(WeaponAttachmentSlotType slotType, IReadOnlyList<string> compatibleAttachmentItemIds)
        {
            var copy = compatibleAttachmentItemIds == null
                ? System.Array.Empty<string>()
                : new List<string>(compatibleAttachmentItemIds).ToArray();

            return new WeaponAttachmentCompatibility
            {
                _slotType = slotType,
                _compatibleAttachmentItemIds = copy
            };
        }
    }
}
