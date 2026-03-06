using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using UnityEngine;

namespace Reloader.Weapons.Runtime
{
    [System.Serializable]
    public struct WeaponViewPoseTuningValues
    {
        public Vector3 HipLocalPosition;
        public Vector3 HipLocalEuler;
        public Vector3 AdsLocalPosition;
        public Vector3 AdsLocalEuler;
        public float BlendSpeed;
        public Vector3 RifleLocalEulerOffset;
        public float ScopedAdsEyeReliefBackOffset;
    }

    public struct WeaponViewPoseTuningRuntimeContext
    {
        public bool HasActiveAttachmentOverride;
        public WeaponAttachmentSlotType SlotType;
        public string AttachmentItemId;
        public WeaponViewPoseTuningValues Values;
        public bool UsesMagnifiedScopedAlignment;
        public float AdsBlendT;
    }

    [DefaultExecutionOrder(10000)]
    public sealed class WeaponViewPoseTuningHelper : MonoBehaviour
    {
        [System.Serializable]
        private struct AttachmentPoseOverride
        {
            [SerializeField] private WeaponAttachmentSlotType _slotType;
            [SerializeField] private string _attachmentItemId;
            [SerializeField] private Vector3 _hipLocalPosition;
            [SerializeField] private Vector3 _hipLocalEuler;
            [SerializeField] private Vector3 _adsLocalPosition;
            [SerializeField] private Vector3 _adsLocalEuler;
            [SerializeField, Min(1f)] private float _blendSpeed;
            [SerializeField] private Vector3 _rifleLocalEulerOffset;
            [SerializeField] private float _scopedAdsEyeReliefBackOffset;

            public WeaponAttachmentSlotType SlotType => _slotType;
            public string AttachmentItemId => string.IsNullOrWhiteSpace(_attachmentItemId) ? string.Empty : _attachmentItemId;
            public Vector3 HipLocalPosition => _hipLocalPosition;
            public Vector3 HipLocalEuler => _hipLocalEuler;
            public Vector3 AdsLocalPosition => _adsLocalPosition;
            public Vector3 AdsLocalEuler => _adsLocalEuler;
            public float BlendSpeed => Mathf.Max(1f, _blendSpeed);
            public Vector3 RifleLocalEulerOffset => _rifleLocalEulerOffset;
            public float ScopedAdsEyeReliefBackOffset => _scopedAdsEyeReliefBackOffset;

            public void SetValues(WeaponViewPoseTuningValues values)
            {
                _hipLocalPosition = values.HipLocalPosition;
                _hipLocalEuler = values.HipLocalEuler;
                _adsLocalPosition = values.AdsLocalPosition;
                _adsLocalEuler = values.AdsLocalEuler;
                _blendSpeed = Mathf.Max(1f, values.BlendSpeed);
                _rifleLocalEulerOffset = values.RifleLocalEulerOffset;
                _scopedAdsEyeReliefBackOffset = values.ScopedAdsEyeReliefBackOffset;
            }
        }

        private struct PoseData
        {
            public Vector3 HipLocalPosition;
            public Vector3 HipLocalEuler;
            public Vector3 AdsLocalPosition;
            public Vector3 AdsLocalEuler;
            public float BlendSpeed;
            public Vector3 RifleLocalEulerOffset;
            public float ScopedAdsEyeReliefBackOffset;
        }

        [Header("References")]
        [SerializeField] private PlayerWeaponController _weaponController;

        [Header("Target")]
        [SerializeField] private string _targetWeaponItemId = "weapon-kar98k";
        [SerializeField] private bool _enabledInPlayMode = true;

        [Header("Base Pose (used for irons / no matching attachment override)")]
        [SerializeField] private Vector3 _hipLocalPosition;
        [SerializeField] private Vector3 _hipLocalEuler;
        [SerializeField] private Vector3 _adsLocalPosition;
        [SerializeField] private Vector3 _adsLocalEuler;
        [SerializeField, Min(1f)] private float _blendSpeed = 24f;
        [SerializeField] private Vector3 _rifleLocalEulerOffset;

        [Header("Attachment Overrides (matched by slot + equipped attachment item id)")]
        [SerializeField] private AttachmentPoseOverride[] _attachmentPoseOverrides = System.Array.Empty<AttachmentPoseOverride>();

        [Header("Debug")]
        [SerializeField] private string _activePoseSource = "Base";
        [SerializeField] private string _activeAttachmentItemId = string.Empty;

        private float _blendT;

        public string TargetWeaponItemId => _targetWeaponItemId;

        private void Awake()
        {
            if (_weaponController == null)
            {
                _weaponController = GetComponent<PlayerWeaponController>();
                if (_weaponController == null)
                {
                    _weaponController = FindFirstObjectByType<PlayerWeaponController>();
                }
            }
        }

        private void Update()
        {
            if (!_enabledInPlayMode || _weaponController == null)
            {
                return;
            }

            var equippedView = _weaponController.EquippedWeaponViewTransform;
            if (!IsTuningTargetEquipped(equippedView))
            {
                _activePoseSource = "Inactive";
                _activeAttachmentItemId = string.Empty;
                _blendT = 0f;
                return;
            }
        }

        private void LateUpdate()
        {
            if (!_enabledInPlayMode || _weaponController == null)
            {
                return;
            }

            var equippedView = _weaponController.EquippedWeaponViewTransform;
            if (!IsTuningTargetEquipped(equippedView))
            {
                return;
            }

            var pose = ResolveActivePose(out var overrideIndex, out var matchedAttachmentItemId);
            ApplyScopedAdsRuntimeTuning(overrideIndex, matchedAttachmentItemId, pose);
            _activeAttachmentItemId = matchedAttachmentItemId;
            _activePoseSource = overrideIndex >= 0
                ? $"AttachmentOverride[{overrideIndex}]"
                : "Base";

            var targetT = ResolveTargetAdsBlendT();
            var step = 1f - Mathf.Exp(-Mathf.Max(1f, pose.BlendSpeed) * Time.deltaTime);
            _blendT = Mathf.Lerp(_blendT, targetT, step);

            var targetPosition = Vector3.Lerp(pose.HipLocalPosition, pose.AdsLocalPosition, _blendT);
            var hipRot = Quaternion.Euler(pose.HipLocalEuler);
            var adsRot = Quaternion.Euler(pose.AdsLocalEuler);
            var targetRotation = Quaternion.Slerp(hipRot, adsRot, _blendT) * Quaternion.Euler(pose.RifleLocalEulerOffset);

            equippedView.localPosition = targetPosition;
            equippedView.localRotation = targetRotation;
        }

        private bool IsTuningTargetEquipped(Transform equippedView)
        {
            if (equippedView == null || _weaponController == null)
            {
                return false;
            }

            var equippedItemId = _weaponController.EquippedItemId;
            if (string.IsNullOrWhiteSpace(_targetWeaponItemId))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(equippedItemId)
                && string.Equals(equippedItemId, _targetWeaponItemId, System.StringComparison.OrdinalIgnoreCase);
        }

        private float ResolveTargetAdsBlendT()
        {
            if (_weaponController == null)
            {
                return 0f;
            }

            if (_weaponController.HasActiveScopedAdsAlignment && _weaponController.HasMagnifiedOpticEquipped())
            {
                return _weaponController.CurrentAdsBlendT;
            }

            return _weaponController.IsAimInputHeld ? 1f : 0f;
        }

        private PoseData ResolveActivePose(out int overrideIndex, out string matchedAttachmentItemId)
        {
            var pose = new PoseData
            {
                HipLocalPosition = _hipLocalPosition,
                HipLocalEuler = _hipLocalEuler,
                AdsLocalPosition = _adsLocalPosition,
                AdsLocalEuler = _adsLocalEuler,
                BlendSpeed = Mathf.Max(1f, _blendSpeed),
                RifleLocalEulerOffset = _rifleLocalEulerOffset,
                ScopedAdsEyeReliefBackOffset = 0f
            };

            overrideIndex = -1;
            matchedAttachmentItemId = string.Empty;

            if (_weaponController == null
                || string.IsNullOrWhiteSpace(_weaponController.EquippedItemId)
                || !_weaponController.TryGetRuntimeState(_weaponController.EquippedItemId, out var state)
                || state == null
                || _attachmentPoseOverrides == null
                || _attachmentPoseOverrides.Length == 0)
            {
                return pose;
            }

            for (var i = 0; i < _attachmentPoseOverrides.Length; i++)
            {
                var candidate = _attachmentPoseOverrides[i];
                if (string.IsNullOrWhiteSpace(candidate.AttachmentItemId))
                {
                    continue;
                }

                var equippedAttachmentItemId = state.GetEquippedAttachmentItemId(candidate.SlotType);
                if (!string.Equals(candidate.AttachmentItemId, equippedAttachmentItemId, System.StringComparison.Ordinal))
                {
                    continue;
                }

                overrideIndex = i;
                matchedAttachmentItemId = equippedAttachmentItemId;
                pose.HipLocalPosition = candidate.HipLocalPosition;
                pose.HipLocalEuler = candidate.HipLocalEuler;
                pose.AdsLocalPosition = candidate.AdsLocalPosition;
                pose.AdsLocalEuler = candidate.AdsLocalEuler;
                pose.BlendSpeed = candidate.BlendSpeed;
                pose.RifleLocalEulerOffset = candidate.RifleLocalEulerOffset;
                pose.ScopedAdsEyeReliefBackOffset = candidate.ScopedAdsEyeReliefBackOffset;
                return pose;
            }

            return pose;
        }

        public WeaponViewPoseTuningValues GetBasePoseValues()
        {
            return new WeaponViewPoseTuningValues
            {
                HipLocalPosition = _hipLocalPosition,
                HipLocalEuler = _hipLocalEuler,
                AdsLocalPosition = _adsLocalPosition,
                AdsLocalEuler = _adsLocalEuler,
                BlendSpeed = Mathf.Max(1f, _blendSpeed),
                RifleLocalEulerOffset = _rifleLocalEulerOffset,
                ScopedAdsEyeReliefBackOffset = 0f
            };
        }

        public void SetBasePoseValues(WeaponViewPoseTuningValues values)
        {
            _hipLocalPosition = values.HipLocalPosition;
            _hipLocalEuler = values.HipLocalEuler;
            _adsLocalPosition = values.AdsLocalPosition;
            _adsLocalEuler = values.AdsLocalEuler;
            _blendSpeed = Mathf.Max(1f, values.BlendSpeed);
            _rifleLocalEulerOffset = values.RifleLocalEulerOffset;
        }

        public bool TryGetAttachmentPoseOverride(
            WeaponAttachmentSlotType slotType,
            string attachmentItemId,
            out WeaponViewPoseTuningValues values)
        {
            var index = FindAttachmentPoseOverrideIndex(slotType, attachmentItemId);
            if (index < 0)
            {
                values = default;
                return false;
            }

            values = CreateValues(_attachmentPoseOverrides[index]);
            return true;
        }

        public bool TrySetAttachmentPoseOverride(
            WeaponAttachmentSlotType slotType,
            string attachmentItemId,
            WeaponViewPoseTuningValues values)
        {
            var index = FindAttachmentPoseOverrideIndex(slotType, attachmentItemId);
            if (index < 0)
            {
                return false;
            }

            var entry = _attachmentPoseOverrides[index];
            entry.SetValues(values);
            _attachmentPoseOverrides[index] = entry;
            return true;
        }

        public bool TryGetActiveAttachmentPoseOverride(
            out WeaponAttachmentSlotType slotType,
            out string attachmentItemId,
            out WeaponViewPoseTuningValues values)
        {
            slotType = default;
            attachmentItemId = string.Empty;
            values = default;

            if (_weaponController == null
                || string.IsNullOrWhiteSpace(_weaponController.EquippedItemId)
                || !_weaponController.TryGetRuntimeState(_weaponController.EquippedItemId, out var state)
                || state == null
                || _attachmentPoseOverrides == null
                || _attachmentPoseOverrides.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < _attachmentPoseOverrides.Length; i++)
            {
                var candidate = _attachmentPoseOverrides[i];
                if (string.IsNullOrWhiteSpace(candidate.AttachmentItemId))
                {
                    continue;
                }

                var equippedAttachmentItemId = state.GetEquippedAttachmentItemId(candidate.SlotType);
                if (!string.Equals(candidate.AttachmentItemId, equippedAttachmentItemId, System.StringComparison.Ordinal))
                {
                    continue;
                }

                slotType = candidate.SlotType;
                attachmentItemId = candidate.AttachmentItemId;
                values = CreateValues(candidate);
                return true;
            }

            return false;
        }

        private int FindAttachmentPoseOverrideIndex(WeaponAttachmentSlotType slotType, string attachmentItemId)
        {
            if (_attachmentPoseOverrides == null || string.IsNullOrWhiteSpace(attachmentItemId))
            {
                return -1;
            }

            for (var i = 0; i < _attachmentPoseOverrides.Length; i++)
            {
                var candidate = _attachmentPoseOverrides[i];
                if (candidate.SlotType != slotType)
                {
                    continue;
                }

                if (!string.Equals(candidate.AttachmentItemId, attachmentItemId, System.StringComparison.Ordinal))
                {
                    continue;
                }

                return i;
            }

            return -1;
        }

        private static WeaponViewPoseTuningValues CreateValues(AttachmentPoseOverride value)
        {
            return new WeaponViewPoseTuningValues
            {
                HipLocalPosition = value.HipLocalPosition,
                HipLocalEuler = value.HipLocalEuler,
                AdsLocalPosition = value.AdsLocalPosition,
                AdsLocalEuler = value.AdsLocalEuler,
                BlendSpeed = value.BlendSpeed,
                RifleLocalEulerOffset = value.RifleLocalEulerOffset,
                ScopedAdsEyeReliefBackOffset = value.ScopedAdsEyeReliefBackOffset
            };
        }

        public bool TryGetRuntimeTuningContext(out WeaponViewPoseTuningRuntimeContext context)
        {
            context = default;
            if (_weaponController == null)
            {
                return false;
            }

            context.AdsBlendT = _blendT;
            context.UsesMagnifiedScopedAlignment = _weaponController.HasActiveScopedAdsAlignment
                && _weaponController.TryGetActiveOpticMagnification(out var minMagnification, out var maxMagnification)
                && (minMagnification > 1.01f || maxMagnification > 1.01f);

            if (TryGetActiveAttachmentPoseOverride(out var slotType, out var attachmentItemId, out var values))
            {
                context.HasActiveAttachmentOverride = true;
                context.SlotType = slotType;
                context.AttachmentItemId = attachmentItemId;
                context.Values = values;
                return true;
            }

            context.HasActiveAttachmentOverride = false;
            context.SlotType = default;
            context.AttachmentItemId = string.Empty;
            context.Values = GetBasePoseValues();
            return IsTuningTargetEquipped(_weaponController.EquippedWeaponViewTransform);
        }

        private void ApplyScopedAdsRuntimeTuning(int overrideIndex, string matchedAttachmentItemId, PoseData pose)
        {
            if (_weaponController == null)
            {
                return;
            }

            if (overrideIndex < 0 || string.IsNullOrWhiteSpace(matchedAttachmentItemId))
            {
                _weaponController.SetScopedAdsPresentationEyeReliefOffset(0f);
                return;
            }

            var candidate = _attachmentPoseOverrides[overrideIndex];
            if (candidate.SlotType != WeaponAttachmentSlotType.Scope)
            {
                _weaponController.SetScopedAdsPresentationEyeReliefOffset(0f);
                return;
            }

            _weaponController.SetScopedAdsPresentationEyeReliefOffset(pose.ScopedAdsEyeReliefBackOffset);
        }
    }
}
