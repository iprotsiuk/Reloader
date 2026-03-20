using System;
using Reloader.Weapons.Data;
using UnityEngine;

namespace Reloader.Weapons.Runtime
{
    [DisallowMultipleComponent]
    public sealed class WeaponViewAttachmentMounts : MonoBehaviour
    {
        [Serializable]
        private struct AttachmentSlotMount
        {
            [SerializeField] private WeaponAttachmentSlotType _slotType;
            [SerializeField] private Transform _slotTransform;

            public WeaponAttachmentSlotType SlotType => _slotType;
            public Transform SlotTransform => _slotTransform;
        }

        [Header("Reference Points")]
        [SerializeField] private Transform _adsPivot;
        [SerializeField] private Transform _ironSightAnchor;
        [SerializeField] private Transform _muzzleTransform;
        [SerializeField] private Transform _magazineSocket;
        [SerializeField] private Transform _magazineDropSocket;

        [Header("Scoped Pose")]
        [SerializeField] private bool _hasScopedPoseAuthoring;
        [SerializeField] private Vector3 _scopedHipLocalPosition;
        [SerializeField] private Vector3 _scopedAdsLocalPosition;

        [Header("Attachment Slots")]
        [SerializeField] private AttachmentSlotMount[] _attachmentSlots = Array.Empty<AttachmentSlotMount>();

        public Transform AdsPivot => _adsPivot;
        public Transform IronSightAnchor => _ironSightAnchor;
        public Transform MuzzleTransform => _muzzleTransform;
        public Transform MagazineSocket => _magazineSocket;
        public Transform MagazineDropSocket => _magazineDropSocket;
        public bool HasScopedPoseAuthoring => _hasScopedPoseAuthoring;

        public bool TryGetScopedPoseAuthoring(out Vector3 hipLocalPosition, out Vector3 adsLocalPosition)
        {
            if (!_hasScopedPoseAuthoring)
            {
                hipLocalPosition = default;
                adsLocalPosition = default;
                return false;
            }

            hipLocalPosition = _scopedHipLocalPosition;
            adsLocalPosition = _scopedAdsLocalPosition;
            return true;
        }

        public bool TryGetAttachmentSlot(WeaponAttachmentSlotType slotType, out Transform slotTransform)
        {
            if (_attachmentSlots != null)
            {
                for (var i = 0; i < _attachmentSlots.Length; i++)
                {
                    var slot = _attachmentSlots[i];
                    if (slot.SlotType != slotType || slot.SlotTransform == null)
                    {
                        continue;
                    }

                    slotTransform = slot.SlotTransform;
                    return true;
                }
            }

            slotTransform = null;
            return false;
        }
    }
}
