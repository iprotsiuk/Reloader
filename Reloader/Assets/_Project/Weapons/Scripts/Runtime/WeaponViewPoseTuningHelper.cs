using Reloader.Weapons.Controllers;
using UnityEngine;

namespace Reloader.Weapons.Runtime
{
    [DefaultExecutionOrder(10000)]
    public sealed class WeaponViewPoseTuningHelper : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerWeaponController _weaponController;

        [Header("Target")]
        [SerializeField] private string _targetWeaponItemId = "weapon-kar98k";
        [SerializeField] private bool _enabledInPlayMode = true;

        [Header("Offsets (local to equipped weapon root)")]
        [SerializeField] private Vector3 _hipLocalPosition;
        [SerializeField] private Vector3 _hipLocalEuler;
        [SerializeField] private Vector3 _adsLocalPosition;
        [SerializeField] private Vector3 _adsLocalEuler;
        [SerializeField, Min(1f)] private float _blendSpeed = 24f;
        [SerializeField] private Vector3 _rifleLocalEulerOffset;
        [SerializeField] private bool _seedOffsetsFromCurrentPoseOnEquip;

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

            if (_seedOffsetsFromCurrentPoseOnEquip && !_initializedFromCurrentPose)
            {
                _hipLocalPosition = equippedView.localPosition;
                _hipLocalEuler = equippedView.localEulerAngles;
                _adsLocalPosition = equippedView.localPosition;
                _adsLocalEuler = equippedView.localEulerAngles;
                _initializedFromCurrentPose = true;
            }

            var targetT = _weaponController.IsAimInputHeld ? 1f : 0f;
            var step = 1f - Mathf.Exp(-Mathf.Max(1f, _blendSpeed) * Time.deltaTime);
            _blendT = Mathf.Lerp(_blendT, targetT, step);

            var targetPosition = Vector3.Lerp(_hipLocalPosition, _adsLocalPosition, _blendT);
            var hipRot = Quaternion.Euler(_hipLocalEuler);
            var adsRot = Quaternion.Euler(_adsLocalEuler);
            var targetRotation = Quaternion.Slerp(hipRot, adsRot, _blendT) * Quaternion.Euler(_rifleLocalEulerOffset);

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

    }
}
