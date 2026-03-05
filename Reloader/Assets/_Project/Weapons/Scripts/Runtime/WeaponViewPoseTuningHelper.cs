using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using UnityEngine;

namespace Reloader.Weapons.Runtime
{
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

            public WeaponAttachmentSlotType SlotType => _slotType;
            public string AttachmentItemId => string.IsNullOrWhiteSpace(_attachmentItemId) ? string.Empty : _attachmentItemId;
            public Vector3 HipLocalPosition => _hipLocalPosition;
            public Vector3 HipLocalEuler => _hipLocalEuler;
            public Vector3 AdsLocalPosition => _adsLocalPosition;
            public Vector3 AdsLocalEuler => _adsLocalEuler;
            public float BlendSpeed => Mathf.Max(1f, _blendSpeed);
            public Vector3 RifleLocalEulerOffset => _rifleLocalEulerOffset;

            public void SeedFromTransform(Transform equippedView)
            {
                if (equippedView == null)
                {
                    return;
                }

                _hipLocalPosition = equippedView.localPosition;
                _hipLocalEuler = equippedView.localEulerAngles;
                _adsLocalPosition = equippedView.localPosition;
                _adsLocalEuler = equippedView.localEulerAngles;
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
        [SerializeField] private bool _seedOffsetsFromCurrentPoseOnEquip;

        [Header("Attachment Overrides (matched by slot + equipped attachment item id)")]
        [SerializeField] private AttachmentPoseOverride[] _attachmentPoseOverrides = System.Array.Empty<AttachmentPoseOverride>();

        [Header("Debug")]
        [SerializeField] private string _activePoseSource = "Base";
        [SerializeField] private string _activeAttachmentItemId = string.Empty;

        private bool _initializedFromCurrentPose;
        private bool _appliedKar98kDefaultPose;
        private Transform _lastEquippedView;
        private float _blendT;

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
                _initializedFromCurrentPose = false;
                _appliedKar98kDefaultPose = false;
                _lastEquippedView = null;
                _activePoseSource = "Inactive";
                _activeAttachmentItemId = string.Empty;
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

            if (equippedView != _lastEquippedView)
            {
                _lastEquippedView = equippedView;
                _initializedFromCurrentPose = false;
                _appliedKar98kDefaultPose = false;
            }

            if (!_appliedKar98kDefaultPose)
            {
                TryApplyKar98kDefaults();
                _appliedKar98kDefaultPose = true;
            }

            var pose = ResolveActivePose(out var overrideIndex, out var matchedAttachmentItemId);
            _activeAttachmentItemId = matchedAttachmentItemId;
            _activePoseSource = overrideIndex >= 0
                ? $"AttachmentOverride[{overrideIndex}]"
                : "Base";

            if (_seedOffsetsFromCurrentPoseOnEquip && !_initializedFromCurrentPose)
            {
                SeedResolvedPoseFromCurrentTransform(equippedView, overrideIndex);
                pose = ResolveActivePose(out overrideIndex, out matchedAttachmentItemId);
                _activeAttachmentItemId = matchedAttachmentItemId;
                _activePoseSource = overrideIndex >= 0
                    ? $"AttachmentOverride[{overrideIndex}]"
                    : "Base";
                _initializedFromCurrentPose = true;
            }

            var targetT = _weaponController.IsAimInputHeld ? 1f : 0f;
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

        private void TryApplyKar98kDefaults()
        {
            if (!string.Equals(_targetWeaponItemId, "weapon-kar98k", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!IsNearZero(_hipLocalPosition)
                || !IsNearZero(_hipLocalEuler)
                || !IsNearZero(_adsLocalPosition)
                || !IsNearZero(_adsLocalEuler)
                || !IsNearZero(_rifleLocalEulerOffset))
            {
                return;
            }

            _hipLocalPosition = new Vector3(0.015f, 0.15f, 0.005f);
            _hipLocalEuler = Vector3.zero;
            _adsLocalPosition = new Vector3(0f, 0.2f, 0.05f);
            _adsLocalEuler = Vector3.zero;
            _blendSpeed = 24f;
            _rifleLocalEulerOffset = new Vector3(90f, 0f, 0f);
        }

        private static bool IsNearZero(Vector3 value)
        {
            return value.sqrMagnitude <= 0.000001f;
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
                RifleLocalEulerOffset = _rifleLocalEulerOffset
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
                return pose;
            }

            return pose;
        }

        private void SeedResolvedPoseFromCurrentTransform(Transform equippedView, int overrideIndex)
        {
            if (equippedView == null)
            {
                return;
            }

            if (overrideIndex >= 0 && _attachmentPoseOverrides != null && overrideIndex < _attachmentPoseOverrides.Length)
            {
                var overridePose = _attachmentPoseOverrides[overrideIndex];
                overridePose.SeedFromTransform(equippedView);
                _attachmentPoseOverrides[overrideIndex] = overridePose;
                return;
            }

            _hipLocalPosition = equippedView.localPosition;
            _hipLocalEuler = equippedView.localEulerAngles;
            _adsLocalPosition = equippedView.localPosition;
            _adsLocalEuler = equippedView.localEulerAngles;
        }
    }
}
