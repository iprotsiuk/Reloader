using UnityEngine;
using System;

namespace Reloader.Game.Weapons
{
    public sealed class AdsStateController : MonoBehaviour
    {
        private const float MinMagnification = 1f;
        private const float MaxMagnification = 40f;

        [Header("References")]
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private Camera _viewmodelCamera;
        [SerializeField] private AttachmentManager _attachmentManager;
        [SerializeField] private ScopeMaskController _scopeMaskController;
        [SerializeField] private RenderTextureScopeController _renderTextureScopeController;
        [SerializeField] private ScopeAdjustmentTooltipOverlay _scopeAdjustmentTooltipOverlay;
        [SerializeField] private PeripheralScopeEffects _peripheralScopeEffects;
        [SerializeField] private WeaponDefinition _weaponDefinition;

        [Header("Input")]
        [SerializeField] private string _adsButton = "Fire2";
        [SerializeField] private KeyCode _adsKey = KeyCode.Mouse1;
        [SerializeField] private bool _zoomOnlyWhileAds = true;
        [SerializeField] private bool _useLegacyInput = true;
        [SerializeField] private bool _allowExternalAdsControl = true;
        [SerializeField] private bool _allowExternalZoomControl = true;

        [Header("Fallback Tuning")]
        [SerializeField, Min(0.01f)] private float _fallbackAdsInTime = 0.12f;
        [SerializeField, Min(0.01f)] private float _fallbackAdsOutTime = 0.1f;
        [SerializeField, Min(0.01f)] private float _fallbackZoomStep = 0.25f;
        [SerializeField, Min(0.01f)] private float _magnificationLerpSpeed = 14f;
        [SerializeField, Min(0.1f)] private float _worldFovLerpSpeed = 18f;
        [SerializeField, Range(1f, 45f)] private float _minimumWorldFov = 8f;

        [Header("Response Curves (x = magnification)")]
        [SerializeField] private AnimationCurve _sensitivityScaleByMagnification = new AnimationCurve(
            new Keyframe(1f, 1f),
            new Keyframe(4f, 0.55f),
            new Keyframe(10f, 0.35f),
            new Keyframe(25f, 0.18f),
            new Keyframe(40f, 0.1f));

        [SerializeField] private AnimationCurve _swayScaleByMagnification = new AnimationCurve(
            new Keyframe(1f, 1f),
            new Keyframe(4f, 0.6f),
            new Keyframe(10f, 0.4f),
            new Keyframe(25f, 0.2f),
            new Keyframe(40f, 0.12f));

        [Header("Debug")]
        [SerializeField] private bool _logDebugState;
        [SerializeField] private bool _logInputWarnings;

        private bool _isAdsHeld;
        private bool _maskLatch;
        private bool _loggedInputWarning;
        private bool _legacyInputUnavailable;
        private bool _adsButtonUnavailable;
        private bool _capturedRuntimeCameraDefaults;
        private float _baseWorldFov = 75f;
        private float _baseViewmodelFov = 60f;
        private float _targetMagnification = 1f;
        private float _nextDebugLogTime;
        private int _externalAdsSetFrame = -1;
        private int _externalMagnificationSetFrame = -1;
        private bool _externalAdsControlActive;
        private bool _externalZoomControlActive;
        private bool _hasLegacyAdsSample;
        private bool _lastLegacyAdsHeld;
        private OpticDefinition _lastMaskOpticDefinition;
        private AttachmentManager _subscribedAttachmentManager;
        private AdsVisualMode _lastMaskPolicy = AdsVisualMode.Auto;

        public bool IsAdsActive => _isAdsHeld;
        public float AdsT { get; private set; }
        public float CurrentMagnification { get; private set; } = 1f;
        public float CurrentSensitivityScale { get; private set; } = 1f;
        public float CurrentSwayScale { get; private set; } = 1f;
        public float TargetWorldFov { get; private set; }

        private void Awake()
        {
            if (_worldCamera == null)
            {
                _worldCamera = Camera.main;
            }

            if (_worldCamera != null)
            {
                _baseWorldFov = _weaponDefinition != null ? _weaponDefinition.DefaultWorldFov : _worldCamera.fieldOfView;
                _worldCamera.fieldOfView = _baseWorldFov;
            }

            if (_viewmodelCamera != null)
            {
                _baseViewmodelFov = _weaponDefinition != null ? _weaponDefinition.DefaultViewmodelFov : _viewmodelCamera.fieldOfView;
                _viewmodelCamera.fieldOfView = _baseViewmodelFov;
            }

            CurrentMagnification = ResolveDefaultMagnification();
            _targetMagnification = CurrentMagnification;
            TargetWorldFov = _baseWorldFov;
            SubscribeAttachmentManagerEvents();
        }

        private void Update()
        {
            EnsureAttachmentManagerSubscription();
            EnsureRuntimeCameraDefaults();
            TickInput();
            TickAdsBlend();
            TickMagnification();
            TickFov();
            TickScaling();
            TickVisualMode();
        }

        private void OnDisable()
        {
            UnsubscribeAttachmentManagerEvents();
            ResetRuntimeState();
            ApplyDisabledVisualState();
        }

        private void OnDestroy()
        {
            UnsubscribeAttachmentManagerEvents();
        }

        public void SetAdsHeld(bool held)
        {
            if (!_allowExternalAdsControl)
            {
                return;
            }

            _isAdsHeld = held;
            _externalAdsSetFrame = Time.frameCount;
            _externalAdsControlActive = true;
        }

        public void SetMagnification(float magnification)
        {
            if (!_allowExternalZoomControl)
            {
                return;
            }

            _targetMagnification = ResolveClampedMagnification(magnification);
            _externalMagnificationSetFrame = Time.frameCount;
            _externalZoomControlActive = true;
        }

        public void SetWeaponDefinition(WeaponDefinition weaponDefinition)
        {
            _weaponDefinition = weaponDefinition;
            if (_weaponDefinition != null)
            {
                _baseWorldFov = _weaponDefinition.DefaultWorldFov;
                _baseViewmodelFov = _weaponDefinition.DefaultViewmodelFov;
            }
        }

        public void SetLegacyInputEnabled(bool enabled)
        {
            _useLegacyInput = enabled;
        }

        public void BindRuntimeReferences(
            Camera worldCamera,
            Camera viewmodelCamera,
            AttachmentManager attachmentManager,
            RenderTextureScopeController renderTextureScopeController = null,
            ScopeMaskController scopeMaskController = null,
            ScopeAdjustmentTooltipOverlay scopeAdjustmentTooltipOverlay = null,
            PeripheralScopeEffects peripheralScopeEffects = null)
        {
            _worldCamera = worldCamera;
            _viewmodelCamera = viewmodelCamera;
            _attachmentManager = attachmentManager;
            _renderTextureScopeController = renderTextureScopeController;
            _scopeMaskController = scopeMaskController;
            _scopeAdjustmentTooltipOverlay = scopeAdjustmentTooltipOverlay;
            _peripheralScopeEffects = peripheralScopeEffects;
            _capturedRuntimeCameraDefaults = false;

            EnsureAttachmentManagerSubscription();
            EnsureRuntimeCameraDefaults();
        }

        public void RefreshRuntimeBindings()
        {
            EnsureAttachmentManagerSubscription();
            EnsureRuntimeCameraDefaults();
            TickVisualMode();
        }

        public bool ApplyScopeAdjustmentInput(int windageClicks, int elevationClicks)
        {
            var optic = ResolveActiveOptic();
            if (!_isAdsHeld || !UsesScopedPip(optic))
            {
                return false;
            }

            var controller = ResolveActiveScopeAdjustmentController();
            if (controller == null)
            {
                return false;
            }

            controller.AdjustWindageClicks(windageClicks);
            controller.AdjustElevationClicks(elevationClicks);
            UpdateScopeAdjustmentTooltip(true, controller);
            return true;
        }

        private void ResetRuntimeState()
        {
            _isAdsHeld = false;
            AdsT = 0f;
            _targetMagnification = 1f;
            CurrentMagnification = 1f;
            CurrentSensitivityScale = 1f;
            CurrentSwayScale = 1f;
            _maskLatch = false;
            _externalAdsControlActive = false;
            _externalZoomControlActive = false;
            _externalAdsSetFrame = -1;
            _externalMagnificationSetFrame = -1;
            _hasLegacyAdsSample = false;
            _lastLegacyAdsHeld = false;
            _lastMaskOpticDefinition = null;
            _lastMaskPolicy = AdsVisualMode.Auto;
            _capturedRuntimeCameraDefaults = false;
        }

        private void ApplyDisabledVisualState()
        {
            if (_worldCamera != null)
            {
                _worldCamera.fieldOfView = _baseWorldFov;
            }

            if (_viewmodelCamera != null)
            {
                _viewmodelCamera.fieldOfView = _baseViewmodelFov;
            }

            if (_scopeMaskController != null)
            {
                _scopeMaskController.SetState(false, 1f, 0f);
            }

            if (_renderTextureScopeController != null)
            {
                _renderTextureScopeController.SetScopeActive(false, null, null, _baseWorldFov, 1f, 0, 0);
            }

            if (_peripheralScopeEffects != null)
            {
                _peripheralScopeEffects.SetState(false, 0f);
            }

            if (_scopeAdjustmentTooltipOverlay != null)
            {
                _scopeAdjustmentTooltipOverlay.SetState(false, 0, 0);
            }
        }

        private void TickInput()
        {
            var externalAdsThisFrame = _externalAdsSetFrame == Time.frameCount;
            var externalMagThisFrame = _externalMagnificationSetFrame == Time.frameCount;

            if (!_useLegacyInput || _legacyInputUnavailable)
            {
                return;
            }

            var legacyHeld = SafeGetKey(_adsKey);
            if (!_adsButtonUnavailable && !string.IsNullOrWhiteSpace(_adsButton))
            {
                legacyHeld |= SafeGetButton(_adsButton);
            }

            var legacyAdsEdgeChanged = _hasLegacyAdsSample && legacyHeld != _lastLegacyAdsHeld;
            _hasLegacyAdsSample = true;
            _lastLegacyAdsHeld = legacyHeld;

            if (!externalAdsThisFrame && _externalAdsControlActive && legacyAdsEdgeChanged)
            {
                _externalAdsControlActive = false;
            }

            if (!externalAdsThisFrame && !_externalAdsControlActive)
            {
                _isAdsHeld = legacyHeld;
            }

            if (_zoomOnlyWhileAds && !_isAdsHeld)
            {
                return;
            }

            var scroll = SafeGetMouseScrollY();
            if (!externalMagThisFrame && _externalZoomControlActive && Mathf.Abs(scroll) > 0.001f)
            {
                _externalZoomControlActive = false;
            }

            if (externalMagThisFrame || _externalZoomControlActive)
            {
                return;
            }

            if (Mathf.Abs(scroll) <= 0.001f)
            {
                return;
            }

            var optic = ResolveActiveOptic();
            var step = _fallbackZoomStep;
            if (optic != null && optic.IsVariableZoom)
            {
                step = Mathf.Max(0.01f, optic.MagnificationStep);
            }

            _targetMagnification = ResolveClampedMagnification(_targetMagnification + (scroll * step));
        }

        private void TickAdsBlend()
        {
            var target = _isAdsHeld ? 1f : 0f;
            var inTime = _weaponDefinition != null ? _weaponDefinition.AdsInTime : _fallbackAdsInTime;
            var outTime = _weaponDefinition != null ? _weaponDefinition.AdsOutTime : _fallbackAdsOutTime;

            var stepPerSecond = target > AdsT ? 1f / Mathf.Max(0.01f, inTime) : 1f / Mathf.Max(0.01f, outTime);
            AdsT = Mathf.MoveTowards(AdsT, target, stepPerSecond * Time.deltaTime);
        }

        private void TickMagnification()
        {
            var optic = ResolveActiveOptic();
            _targetMagnification = ResolveClampedMagnification(ResolveRequestedMagnification(optic, _targetMagnification));

            var t = 1f - Mathf.Exp(-Mathf.Max(0.01f, _magnificationLerpSpeed) * Time.deltaTime);
            CurrentMagnification = Mathf.Lerp(CurrentMagnification, _targetMagnification, t);
        }

        private void TickFov()
        {
            if (_worldCamera == null)
            {
                return;
            }

            var optic = ResolveActiveOptic();
            var usesScopedPip = UsesScopedPip(optic);
            var adsFov = usesScopedPip
                ? _baseWorldFov
                : Mathf.Clamp(_baseWorldFov / Mathf.Max(MinMagnification, CurrentMagnification), _minimumWorldFov, _baseWorldFov);
            TargetWorldFov = Mathf.Lerp(_baseWorldFov, adsFov, AdsT);
            if (usesScopedPip)
            {
                _worldCamera.fieldOfView = _baseWorldFov;
            }
            else
            {
                var worldLerp = 1f - Mathf.Exp(-Mathf.Max(0.01f, _worldFovLerpSpeed) * Time.deltaTime);
                _worldCamera.fieldOfView = Mathf.Lerp(_worldCamera.fieldOfView, TargetWorldFov, worldLerp);
            }

            if (_viewmodelCamera != null)
            {
                _viewmodelCamera.fieldOfView = _baseViewmodelFov;
            }
        }

        private void TickScaling()
        {
            var baseSensitivity = _weaponDefinition != null ? _weaponDefinition.BaseAdsSensitivityScale : 1f;
            var baseSway = _weaponDefinition != null ? _weaponDefinition.BaseAdsSwayScale : 1f;

            var sensitivityCurve = Mathf.Max(0.01f, _sensitivityScaleByMagnification.Evaluate(CurrentMagnification));
            var swayCurve = Mathf.Max(0.01f, _swayScaleByMagnification.Evaluate(CurrentMagnification));

            var targetAdsSensitivity = baseSensitivity * sensitivityCurve;
            var targetAdsSway = baseSway * swayCurve;
            CurrentSensitivityScale = Mathf.Lerp(1f, targetAdsSensitivity, AdsT);
            CurrentSwayScale = Mathf.Lerp(1f, targetAdsSway, AdsT);
        }

        private void TickVisualMode()
        {
            var optic = ResolveActiveOptic();
            var policy = ResolveVisualModePolicy(optic);
            if (!ReferenceEquals(_lastMaskOpticDefinition, optic) || _lastMaskPolicy != policy)
            {
                UpdateMaskContext(optic, policy, CurrentMagnification);
            }

            var useMask = ResolveMaskMode(policy, CurrentMagnification);
            var adsVisible = AdsT > 0.01f;

            if (_scopeMaskController != null)
            {
                _scopeMaskController.SetReticleSprite(optic != null ? optic.ReticleUiSprite : null);
                _scopeMaskController.SetState(adsVisible && useMask, CurrentMagnification, AdsT);
            }

            if (_renderTextureScopeController != null)
            {
                var usePip = adsVisible && policy == AdsVisualMode.RenderTexturePiP;
                var scopeReferenceFov = _baseWorldFov;
                var scopeMagnification = Mathf.Max(MinMagnification, CurrentMagnification);
                var activeOpticInstance = _attachmentManager != null ? _attachmentManager.ActiveOpticInstance : null;
                var activeAdjustmentController = ResolveActiveScopeAdjustmentController();
                var windageClicks = activeAdjustmentController != null ? activeAdjustmentController.CurrentWindageClicks : 0;
                var elevationClicks = activeAdjustmentController != null ? activeAdjustmentController.CurrentElevationClicks : 0;
                _renderTextureScopeController.SetScopeActive(
                    usePip,
                    optic,
                    activeOpticInstance,
                    scopeReferenceFov,
                    scopeMagnification,
                    windageClicks,
                    elevationClicks);

                if (_peripheralScopeEffects != null)
                {
                    _peripheralScopeEffects.SetState(usePip, AdsT);
                }
            }

            UpdateScopeAdjustmentTooltip(
                _isAdsHeld && policy == AdsVisualMode.RenderTexturePiP,
                ResolveActiveScopeAdjustmentController());

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_logDebugState && Time.unscaledTime >= _nextDebugLogTime)
            {
                _nextDebugLogTime = Time.unscaledTime + 0.2f;
                Debug.Log($"[ADS] t={AdsT:F2} mag={CurrentMagnification:F2} sens={CurrentSensitivityScale:F2} sway={CurrentSwayScale:F2} mask={useMask}", this);
            }
#endif
        }

        private void SubscribeAttachmentManagerEvents()
        {
            if (_attachmentManager == null || ReferenceEquals(_subscribedAttachmentManager, _attachmentManager))
            {
                return;
            }

            UnsubscribeAttachmentManagerEvents();
            _attachmentManager.ActiveOpticChanged += HandleActiveOpticChanged;
            _subscribedAttachmentManager = _attachmentManager;
            HandleActiveOpticChanged(_subscribedAttachmentManager.ActiveOpticDefinition);
        }

        private void UnsubscribeAttachmentManagerEvents()
        {
            if (_subscribedAttachmentManager == null)
            {
                return;
            }

            _subscribedAttachmentManager.ActiveOpticChanged -= HandleActiveOpticChanged;
            _subscribedAttachmentManager = null;
        }

        private void EnsureAttachmentManagerSubscription()
        {
            if (ReferenceEquals(_subscribedAttachmentManager, _attachmentManager))
            {
                return;
            }

            SubscribeAttachmentManagerEvents();
        }

        private void HandleActiveOpticChanged(OpticDefinition optic)
        {
            _targetMagnification = ResolveRequestedMagnification(optic, _targetMagnification);
            CurrentMagnification = ResolveClampedMagnification(CurrentMagnification);
            _targetMagnification = ResolveClampedMagnification(_targetMagnification);
            UpdateMaskContext(optic, ResolveVisualModePolicy(optic), CurrentMagnification);

            if (_scopeMaskController != null)
            {
                _scopeMaskController.SetReticleSprite(optic != null ? optic.ReticleUiSprite : null);
            }
        }

        private void EnsureRuntimeCameraDefaults()
        {
            if (_capturedRuntimeCameraDefaults || _weaponDefinition != null)
            {
                return;
            }

            if (_worldCamera != null)
            {
                _baseWorldFov = Mathf.Clamp(_worldCamera.fieldOfView, 1f, 179f);
            }

            if (_viewmodelCamera != null)
            {
                _baseViewmodelFov = Mathf.Clamp(_viewmodelCamera.fieldOfView, 1f, 179f);
            }

            _capturedRuntimeCameraDefaults = _worldCamera != null || _viewmodelCamera != null;
        }

        private void ResetMaskLatchForContext(AdsVisualMode policy, float magnification)
        {
            if (policy == AdsVisualMode.Mask)
            {
                _maskLatch = true;
                return;
            }

            if (policy == AdsVisualMode.RenderTexturePiP)
            {
                _maskLatch = false;
                return;
            }

            // Deterministic Auto initialization on optic/policy swap.
            _maskLatch = magnification >= 4f;
        }

        private bool ResolveMaskMode(AdsVisualMode policy, float magnification)
        {
            if (policy == AdsVisualMode.Mask)
            {
                return true;
            }

            if (policy == AdsVisualMode.RenderTexturePiP)
            {
                return false;
            }

            if (magnification <= 2f)
            {
                _maskLatch = false;
            }
            else if (magnification >= 4f)
            {
                _maskLatch = true;
            }

            return _maskLatch;
        }

        private OpticDefinition ResolveActiveOptic()
        {
            return _attachmentManager != null ? _attachmentManager.ActiveOpticDefinition : null;
        }

        private ScopeAdjustmentController ResolveActiveScopeAdjustmentController()
        {
            return _attachmentManager != null ? _attachmentManager.ActiveScopeAdjustmentController : null;
        }

        private static AdsVisualMode ResolveVisualModePolicy(OpticDefinition optic)
        {
            return optic != null ? optic.VisualModePolicy : AdsVisualMode.Auto;
        }

        private static float ResolveRequestedMagnification(OpticDefinition optic, float requestedMagnification)
        {
            if (optic == null)
            {
                return 1f;
            }

            if (!optic.IsVariableZoom)
            {
                return optic.MagnificationMin;
            }

            return optic.SnapMagnification(requestedMagnification);
        }

        private void UpdateMaskContext(OpticDefinition optic, AdsVisualMode policy, float magnification)
        {
            ResetMaskLatchForContext(policy, magnification);
            _lastMaskOpticDefinition = optic;
            _lastMaskPolicy = policy;
        }

        private static bool UsesScopedPip(OpticDefinition optic)
        {
            return optic != null && optic.VisualModePolicy == AdsVisualMode.RenderTexturePiP;
        }

        private void UpdateScopeAdjustmentTooltip(bool isVisible, ScopeAdjustmentController controller)
        {
            if (_scopeAdjustmentTooltipOverlay == null)
            {
                return;
            }

            if (!isVisible || controller == null)
            {
                _scopeAdjustmentTooltipOverlay.SetState(false, 0, 0);
                return;
            }

            _scopeAdjustmentTooltipOverlay.SetState(true, controller.CurrentWindageClicks, controller.CurrentElevationClicks);
        }

        private bool SafeGetKey(KeyCode key)
        {
            if (_legacyInputUnavailable)
            {
                return false;
            }

            try
            {
                return Input.GetKey(key);
            }
            catch (InvalidOperationException)
            {
                _legacyInputUnavailable = true;
                LogInputWarningOnce();
                return false;
            }
        }

        private bool SafeGetButton(string buttonName)
        {
            if (_legacyInputUnavailable || _adsButtonUnavailable)
            {
                return false;
            }

            try
            {
                return Input.GetButton(buttonName);
            }
            catch (InvalidOperationException)
            {
                _legacyInputUnavailable = true;
                LogInputWarningOnce();
                return false;
            }
            catch (ArgumentException)
            {
                _adsButtonUnavailable = true;
                return false;
            }
        }

        private float SafeGetMouseScrollY()
        {
            if (_legacyInputUnavailable)
            {
                return 0f;
            }

            try
            {
                return Input.mouseScrollDelta.y;
            }
            catch (InvalidOperationException)
            {
                _legacyInputUnavailable = true;
                LogInputWarningOnce();
                return 0f;
            }
        }

        private void LogInputWarningOnce()
        {
            if (_loggedInputWarning || !_logInputWarnings)
            {
                return;
            }

            _loggedInputWarning = true;
            Debug.LogWarning("AdsStateController: Legacy Input API unavailable. Bind ADS/zoom through SetAdsHeld/SetMagnification integration path.", this);
        }

        private float ResolveDefaultMagnification()
        {
            var optic = ResolveActiveOptic();
            if (optic == null)
            {
                return 1f;
            }

            return ResolveClampedMagnification(optic.MagnificationMin);
        }

        private float ResolveClampedMagnification(float requested)
        {
            var optic = ResolveActiveOptic();
            if (optic == null)
            {
                return Mathf.Clamp(requested, 1f, 1f);
            }

            return Mathf.Clamp(optic.ClampMagnification(requested), MinMagnification, MaxMagnification);
        }
    }
}
