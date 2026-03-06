using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.Game.Weapons
{
    [DefaultExecutionOrder(11000)]
    public sealed class WeaponAimAligner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _adsPivot;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private AttachmentManager _attachmentManager;
        [SerializeField] private AdsStateController _adsStateController;

        [Header("Alignment")]
        [SerializeField, Min(0f)] private float _positionLerpSpeed = 24f;
        [SerializeField, Min(0f)] private float _rotationLerpSpeed = 24f;
        [SerializeField, Min(0f)] private float _extraEyeReliefBackOffset;

        [Header("Debug")]
        [SerializeField] private bool _drawDebugGizmos = true;
        [SerializeField, Min(0.01f)] private float _axisLength = 0.08f;
        [SerializeField] private bool _logMissingReferenceWarnings = true;
        [SerializeField, Min(0.1f)] private float _mainCameraRefreshInterval = 1f;

        private Transform _pivotParent;
        private Vector3 _restLocalPosition;
        private Quaternion _restLocalRotation;
        private Transform _cachedMainCameraTransform;
        private float _nextMainCameraRefreshTime;
        private bool _loggedMissingPivot;
        private bool _loggedMissingAnchorSource;
        private float _runtimeEyeReliefBackOffset;

        public float DebugAlignmentErrorDistance { get; private set; }
        public float DebugAlignmentErrorAngleDegrees { get; private set; }
        public float RuntimeEyeReliefBackOffset => _runtimeEyeReliefBackOffset;

        private void Awake()
        {
            RefreshRuntimeCaches();
        }

        private void OnEnable()
        {
            CacheMainCameraTransform();
        }

        public void BindRuntimeReferences(
            Transform adsPivot,
            Transform cameraTransform,
            AttachmentManager attachmentManager,
            AdsStateController adsStateController)
        {
            _adsPivot = adsPivot;
            _cameraTransform = cameraTransform;
            _attachmentManager = attachmentManager;
            _adsStateController = adsStateController;
            RefreshRuntimeCaches();
        }

        public void SetRuntimeEyeReliefBackOffset(float value)
        {
            _runtimeEyeReliefBackOffset = value;
        }

        private void LateUpdate()
        {
            if (_adsPivot == null)
            {
                if (!_loggedMissingPivot && _logMissingReferenceWarnings)
                {
                    _loggedMissingPivot = true;
                    Debug.LogWarning("WeaponAimAligner: AdsPivot is missing.", this);
                }

                return;
            }

            var cameraTx = ResolveCameraTransform();
            var sightAnchor = _attachmentManager != null ? _attachmentManager.GetActiveSightAnchor() : null;
            if (cameraTx == null || sightAnchor == null)
            {
                if (!_loggedMissingAnchorSource && _logMissingReferenceWarnings)
                {
                    _loggedMissingAnchorSource = true;
                    Debug.LogWarning("WeaponAimAligner: Missing camera or sight anchor. Reverting to rest pose.", this);
                }

                LerpBackToRestPose();
                return;
            }

            var adsT = _adsStateController != null ? _adsStateController.AdsT : 0f;

            var deltaMatrix = Matrix4x4.TRS(cameraTx.position, cameraTx.rotation, Vector3.one)
                            * Matrix4x4.TRS(sightAnchor.position, sightAnchor.rotation, Vector3.one).inverse;

            var currentPivotMatrix = Matrix4x4.TRS(_adsPivot.position, _adsPivot.rotation, Vector3.one);
            var targetPivotMatrix = deltaMatrix * currentPivotMatrix;

            var targetWorldPosition = (Vector3)targetPivotMatrix.GetColumn(3);
            var targetWorldRotation = targetPivotMatrix.rotation;

            var opticEyeRelief = 0f;
            var activeOptic = _attachmentManager != null ? _attachmentManager.ActiveOpticDefinition : null;
            if (activeOptic != null)
            {
                opticEyeRelief = activeOptic.EyeReliefBackOffset;
            }

            var totalEyeRelief = opticEyeRelief + _extraEyeReliefBackOffset + _runtimeEyeReliefBackOffset;
            targetWorldPosition += (-sightAnchor.forward) * totalEyeRelief;

            Vector3 targetLocalPosition;
            Quaternion targetLocalRotation;
            if (_pivotParent != null)
            {
                targetLocalPosition = _pivotParent.InverseTransformPoint(targetWorldPosition);
                targetLocalRotation = Quaternion.Inverse(_pivotParent.rotation) * targetWorldRotation;
            }
            else
            {
                targetLocalPosition = targetWorldPosition;
                targetLocalRotation = targetWorldRotation;
            }

            // Scoped ADS timing already comes from AdsStateController.AdsT.
            // Apply the solved pivot pose directly from that blend so the optic
            // is fully aligned when ADS completes instead of continuing to settle.
            _adsPivot.localPosition = Vector3.Lerp(_restLocalPosition, targetLocalPosition, adsT);
            _adsPivot.localRotation = Quaternion.Slerp(_restLocalRotation, targetLocalRotation, adsT);

            DebugAlignmentErrorDistance = Vector3.Distance(sightAnchor.position, cameraTx.position);
            DebugAlignmentErrorAngleDegrees = Quaternion.Angle(sightAnchor.rotation, cameraTx.rotation);
        }

        private void LerpBackToRestPose()
        {
            var positionStep = 1f - Mathf.Exp(-Mathf.Max(0.001f, _positionLerpSpeed) * Time.deltaTime);
            var rotationStep = 1f - Mathf.Exp(-Mathf.Max(0.001f, _rotationLerpSpeed) * Time.deltaTime);
            _adsPivot.localPosition = Vector3.Lerp(_adsPivot.localPosition, _restLocalPosition, positionStep);
            _adsPivot.localRotation = Quaternion.Slerp(_adsPivot.localRotation, _restLocalRotation, rotationStep);
        }

        private Transform ResolveCameraTransform()
        {
            if (_cameraTransform != null)
            {
                return _cameraTransform;
            }

            if (_cachedMainCameraTransform == null || Time.unscaledTime >= _nextMainCameraRefreshTime)
            {
                CacheMainCameraTransform();
                _nextMainCameraRefreshTime = Time.unscaledTime + Mathf.Max(0.1f, _mainCameraRefreshInterval);
            }

            return _cachedMainCameraTransform;
        }

        private void CacheMainCameraTransform()
        {
            var main = Camera.main;
            _cachedMainCameraTransform = main != null ? main.transform : null;
        }

        private void RefreshRuntimeCaches()
        {
            CacheRestPose();
            CacheMainCameraTransform();
            _loggedMissingPivot = false;
            _loggedMissingAnchorSource = false;
        }

        private void CacheRestPose()
        {
            _pivotParent = null;
            _restLocalPosition = Vector3.zero;
            _restLocalRotation = Quaternion.identity;

            if (_adsPivot == null)
            {
                return;
            }

            _pivotParent = _adsPivot.parent;
            _restLocalPosition = _adsPivot.localPosition;
            _restLocalRotation = _adsPivot.localRotation;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_drawDebugGizmos || _attachmentManager == null)
            {
                return;
            }

            var cameraTx = ResolveCameraTransform();
            var sightAnchor = _attachmentManager.GetActiveSightAnchor();
            if (cameraTx == null || sightAnchor == null)
            {
                return;
            }

            var len = Mathf.Max(0.01f, _axisLength);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(cameraTx.position, cameraTx.position + (cameraTx.forward * len));
            Gizmos.DrawLine(cameraTx.position, cameraTx.position + (cameraTx.up * len * 0.75f));

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(sightAnchor.position, sightAnchor.position + (sightAnchor.forward * len));
            Gizmos.DrawLine(sightAnchor.position, sightAnchor.position + (sightAnchor.up * len * 0.75f));

            Gizmos.color = Color.red;
            Gizmos.DrawLine(sightAnchor.position, cameraTx.position);

            var label = "ADS err d=" + DebugAlignmentErrorDistance.ToString("F3") + " a=" + DebugAlignmentErrorAngleDegrees.ToString("F2");
            Handles.Label((sightAnchor.position + cameraTx.position) * 0.5f, label);
        }
#endif
    }
}
