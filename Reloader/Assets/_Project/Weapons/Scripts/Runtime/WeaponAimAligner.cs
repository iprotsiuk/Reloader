using UnityEngine;

namespace Reloader.Weapons.Runtime
{
    [DisallowMultipleComponent]
    public sealed class WeaponAimAligner : MonoBehaviour
    {
        [SerializeField] private bool _logMissingReferenceWarnings;
        [Header("Scoped Recoil")]
        [SerializeField, Min(0f)] private float _scopedRecoilPitchDegrees = 1.1f;
        [SerializeField, Min(0f)] private float _scopedRecoilYawDegrees = 0.18f;
        [SerializeField, Min(0f)] private float _scopedRecoilRollDegrees = 0.08f;
        [SerializeField, Min(0.01f)] private float _scopedRecoilReturnDegreesPerSecond = 8f;

        private Reloader.Game.Weapons.AttachmentManager _attachmentManager;
        private Reloader.Game.Weapons.AdsStateController _adsStateController;
        private Camera _worldCamera;
        private WeaponViewAttachmentMounts _viewMounts;
        private Transform _adsPivot;
        private Vector3 _cachedBaseLocalPosition;
        private Quaternion _cachedBaseLocalRotation = Quaternion.identity;
        private Vector3 _scopedHipLocalPosition;
        private Vector3 _scopedAdsLocalPosition;
        private bool _hasCachedBasePose;
        private bool _hasScopedPoseAuthoring;
        private bool _wasAdsActiveLastFrame;
        private bool _warnedMissingBindings;
        private bool _warnedMissingSightAnchor;
        private Vector3 _scopedRecoilEuler;

        public void BindRuntimeReferences(
            Camera worldCamera,
            Reloader.Game.Weapons.AttachmentManager attachmentManager,
            Reloader.Game.Weapons.AdsStateController adsStateController,
            WeaponViewAttachmentMounts viewMounts)
        {
            _worldCamera = worldCamera;
            _attachmentManager = attachmentManager;
            _adsStateController = adsStateController;
            _viewMounts = viewMounts;
            RefreshViewPivotCache();
        }

        public void ClearRuntimeReferences()
        {
            _worldCamera = null;
            _attachmentManager = null;
            _adsStateController = null;
            _viewMounts = null;
            _adsPivot = null;
            _hasCachedBasePose = false;
            _hasScopedPoseAuthoring = false;
            _wasAdsActiveLastFrame = false;
            _warnedMissingBindings = false;
            _warnedMissingSightAnchor = false;
            _scopedRecoilEuler = Vector3.zero;
        }

        public void ApplyScopedRecoilImpulse()
        {
            if (_adsStateController == null || !_adsStateController.IsAdsActive)
            {
                return;
            }

            _scopedRecoilEuler += new Vector3(
                -_scopedRecoilPitchDegrees,
                _scopedRecoilYawDegrees,
                -_scopedRecoilRollDegrees);
        }

        public void AlignNow()
        {
            if (!HasValidRuntimeBindings())
            {
                return;
            }

            if (!_adsStateController.IsAdsActive)
            {
                if (_wasAdsActiveLastFrame)
                {
                    RefreshViewPivotCache();
                    RestoreCanonicalPose();
                    _wasAdsActiveLastFrame = false;
                    _scopedRecoilEuler = Vector3.zero;
                }

                return;
            }

            _wasAdsActiveLastFrame = true;
            ApplyScopedPose();
        }

        private void LateUpdate()
        {
            AlignNow();
        }

        private bool HasValidRuntimeBindings()
        {
            if (_worldCamera != null
                && _attachmentManager != null
                && _adsStateController != null
                && _viewMounts != null
                && _adsPivot != null)
            {
                return true;
            }

            if (_logMissingReferenceWarnings && _adsStateController != null && _adsStateController.IsAdsActive && !_warnedMissingBindings)
            {
                _warnedMissingBindings = true;
                Debug.LogWarning(
                    "WeaponAimAligner: Missing scoped runtime bindings. Expected world camera, AttachmentManager, AdsStateController, and view mount references.",
                    this);
            }

            return false;
        }

        private void RefreshViewPivotCache()
        {
            _adsPivot = _viewMounts != null ? _viewMounts.AdsPivot : null;
            if (_adsPivot == null)
            {
                _hasCachedBasePose = false;
                _hasScopedPoseAuthoring = false;
                return;
            }

            if (!_hasCachedBasePose)
            {
                _cachedBaseLocalPosition = _adsPivot.localPosition;
                _cachedBaseLocalRotation = _adsPivot.localRotation;
                _hasCachedBasePose = true;
            }

            if (_viewMounts.TryGetScopedPoseAuthoring(out _scopedHipLocalPosition, out _scopedAdsLocalPosition))
            {
                _hasScopedPoseAuthoring = true;
            }
            else
            {
                _hasScopedPoseAuthoring = false;
            }
        }

        private void RestoreCanonicalPose()
        {
            if (_adsPivot == null)
            {
                return;
            }

            if (_hasScopedPoseAuthoring)
            {
                _adsPivot.localPosition = _scopedHipLocalPosition;
            }
            else if (_hasCachedBasePose)
            {
                _adsPivot.localPosition = _cachedBaseLocalPosition;
            }

            if (_hasCachedBasePose)
            {
                _adsPivot.localRotation = _cachedBaseLocalRotation;
            }
        }

        private void ApplyScopedPose()
        {
            RefreshViewPivotCache();
            if (_adsPivot == null)
            {
                return;
            }

            if (_hasScopedPoseAuthoring)
            {
                _adsPivot.localPosition = Vector3.Lerp(_scopedHipLocalPosition, _scopedAdsLocalPosition, Mathf.Clamp01(_adsStateController.AdsT));
            }
            else if (_hasCachedBasePose)
            {
                _adsPivot.localPosition = _cachedBaseLocalPosition;
            }

            if (_hasCachedBasePose)
            {
                _adsPivot.localRotation = _cachedBaseLocalRotation;
            }

            var activeSightAnchor = _attachmentManager.GetActiveSightAnchor();
            if (activeSightAnchor == null)
            {
                if (_logMissingReferenceWarnings && !_warnedMissingSightAnchor)
                {
                    _warnedMissingSightAnchor = true;
                    Debug.LogWarning(
                        "WeaponAimAligner: Scoped alignment is active but AttachmentManager.GetActiveSightAnchor() returned null.",
                        this);
                }

                return;
            }

            var eyeReliefBackOffset = _viewMounts != null ? _viewMounts.ScopedAdsEyeReliefBackOffset : 0f;
            if (eyeReliefBackOffset <= 0f && _attachmentManager.ActiveOpticDefinition != null)
            {
                eyeReliefBackOffset = _attachmentManager.ActiveOpticDefinition.EyeReliefBackOffset;
            }

            var cameraTransform = _worldCamera.transform;
            var targetRotation = cameraTransform.rotation;
            var targetPosition = cameraTransform.position - (cameraTransform.forward * eyeReliefBackOffset);

            var deltaRotation = targetRotation * Quaternion.Inverse(activeSightAnchor.rotation);
            var deltaPosition = targetPosition - (deltaRotation * activeSightAnchor.position);

            var pivotWorldPosition = deltaRotation * _adsPivot.position + deltaPosition;
            var pivotWorldRotation = deltaRotation * _adsPivot.rotation * Quaternion.Euler(_scopedRecoilEuler);
            _adsPivot.SetPositionAndRotation(pivotWorldPosition, pivotWorldRotation);

            AdvanceScopedRecoil(Time.deltaTime);
        }

        private void AdvanceScopedRecoil(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            _scopedRecoilEuler = Vector3.MoveTowards(
                _scopedRecoilEuler,
                Vector3.zero,
                _scopedRecoilReturnDegreesPerSecond * deltaTime);
        }
    }
}
