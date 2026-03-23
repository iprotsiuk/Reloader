using UnityEngine;
using System;
using System.Reflection;
using Reloader.Game.Weapons.Rendering;

namespace Reloader.Game.Weapons
{
    public sealed class AdsStateController : MonoBehaviour
    {
        private const float MinMagnification = 1f;
        private const float MaxMagnification = 40f;
        private const int FallbackScopedPipResolutionPercent = 100;
        private const int FallbackPeripheralBlurPercent = 50;
        private const int ScopedOpticsSettingsMinPipResolutionPercent = 10;
        private const int ScopedOpticsSettingsMaxPipResolutionPercent = 400;
        private const int ScopedOpticsSettingsMinPeripheralBlurPercent = 0;
        private const int ScopedOpticsSettingsMaxPeripheralBlurPercent = 100;
        private const string ScopedOpticsSettingsSourceTypeName = "Reloader.UI.Toolkit.EscMenu.IScopedOpticsSettingsSource";
        private const string ScopedOpticsSettingsSnapshotTypeName = "Reloader.UI.Toolkit.EscMenu.ScopedOpticsSettingsSnapshot";
        private const string ScopedOpticsSettingsContractTypeName = "Reloader.UI.Toolkit.EscMenu.ScopedOpticsSettings";
        private const string ShotCameraGameplayStateTypeName = "Reloader.Player.ShotCameraGameplayState";
        private const string ScopedPipResolutionPlayerPrefKey = "esc-menu.scoped-pip-resolution-percent";
        private const string PeripheralBlurPlayerPrefKey = "esc-menu.peripheral-blur-percent";
        private const int ScopedOpticsSettingsSourceInitialRetryFrameInterval = 30;
        private const int ScopedOpticsSettingsSourceMaxRetryFrameInterval = 600;

        private static PropertyInfo s_shotCameraIsActiveProperty;

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

        [SerializeField] private AnimationCurve _pipPrecisionScaleByMagnification = new AnimationCurve(
            new Keyframe(1f, 1f),
            new Keyframe(4f, 0.4f),
            new Keyframe(10f, 0.12f),
            new Keyframe(25f, 0.04f),
            new Keyframe(40f, 0.03f));

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
        private object _scopedOpticsSettingsSource;
        private Type _scopedOpticsSettingsSourceType;
        private Type _scopedOpticsSettingsSnapshotType;
        private Type _scopedOpticsSettingsContractType;
        private bool _hasResolvedScopedOpticsSettingsSource;
        private int _nextScopedOpticsSettingsSourceLookupFrame;
        private int _scopedOpticsSettingsSourceRetryFrameInterval = ScopedOpticsSettingsSourceInitialRetryFrameInterval;

        public bool IsAdsActive => _isAdsHeld;
        public float AdsT { get; private set; }
        public float CurrentMagnification { get; private set; } = 1f;
        public float CurrentSensitivityScale { get; private set; } = 1f;
        public float CurrentPipPrecisionScale { get; private set; } = 1f;
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
            _isAdsHeld = false;
            AdsT = 0f;
            _targetMagnification = 1f;
            CurrentMagnification = 1f;
            CurrentSensitivityScale = 1f;
            CurrentPipPrecisionScale = 1f;
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
            _nextScopedOpticsSettingsSourceLookupFrame = 0;
            _scopedOpticsSettingsSourceRetryFrameInterval = ScopedOpticsSettingsSourceInitialRetryFrameInterval;

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
            else
            {
                PeripheralScopeBlurRuntimeState.Reset();
            }

            ScopedPeripheralWorldRenderScaleRuntime.Reset();

            if (_scopeAdjustmentTooltipOverlay != null)
            {
                _scopeAdjustmentTooltipOverlay.SetState(false, 0, 0);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeAttachmentManagerEvents();
            PeripheralScopeBlurRuntimeState.Reset();
            ScopedPeripheralWorldRenderScaleRuntime.Reset();
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

        public void BindRuntimeReferences(
            Camera worldCamera,
            Camera viewmodelCamera,
            AttachmentManager attachmentManager,
            RenderTextureScopeController renderTextureScopeController,
            PeripheralScopeEffects peripheralScopeEffects,
            ScopeAdjustmentTooltipOverlay scopeAdjustmentTooltipOverlay)
        {
            _worldCamera = worldCamera;
            _viewmodelCamera = viewmodelCamera;
            _renderTextureScopeController = renderTextureScopeController;
            _peripheralScopeEffects = peripheralScopeEffects;
            _scopeAdjustmentTooltipOverlay = scopeAdjustmentTooltipOverlay;

            if (ReferenceEquals(_attachmentManager, attachmentManager))
            {
                return;
            }

            UnsubscribeAttachmentManagerEvents();
            _attachmentManager = attachmentManager;
            SubscribeAttachmentManagerEvents();
        }

        public void SetUseLegacyInput(bool useLegacyInput)
        {
            _useLegacyInput = useLegacyInput;
        }

        public void RefreshVisualMode()
        {
            TickVisualMode();
        }

        public bool ApplyScopeAdjustmentInput(int windageClicks, int elevationClicks)
        {
            var optic = _attachmentManager != null ? _attachmentManager.ActiveOpticDefinition : null;
            if (!_isAdsHeld || !UsesScopedPip(optic))
            {
                return false;
            }

            var controller = _attachmentManager != null ? _attachmentManager.ActiveScopeAdjustmentController : null;
            if (controller == null)
            {
                return false;
            }

            controller.AdjustWindageClicks(windageClicks);
            controller.AdjustElevationClicks(elevationClicks);
            UpdateScopeAdjustmentTooltip(true, controller);
            return true;
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

            var optic = _attachmentManager != null ? _attachmentManager.ActiveOpticDefinition : null;
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
            var optic = _attachmentManager != null ? _attachmentManager.ActiveOpticDefinition : null;
            if (optic == null)
            {
                _targetMagnification = 1f;
            }
            else if (!optic.IsVariableZoom)
            {
                _targetMagnification = optic.MagnificationMin;
            }
            else
            {
                _targetMagnification = optic.SnapMagnification(_targetMagnification);
            }

            _targetMagnification = ResolveClampedMagnification(_targetMagnification);

            var t = 1f - Mathf.Exp(-Mathf.Max(0.01f, _magnificationLerpSpeed) * Time.deltaTime);
            CurrentMagnification = Mathf.Lerp(CurrentMagnification, _targetMagnification, t);
        }

        private void TickFov()
        {
            if (_worldCamera == null)
            {
                return;
            }

            var optic = _attachmentManager != null ? _attachmentManager.ActiveOpticDefinition : null;
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
            var optic = _attachmentManager != null ? _attachmentManager.ActiveOpticDefinition : null;
            var usePipPrecision = UsesScopedPip(optic);
            var baseSensitivity = _weaponDefinition != null ? _weaponDefinition.BaseAdsSensitivityScale : 1f;
            var baseSway = _weaponDefinition != null ? _weaponDefinition.BaseAdsSwayScale : 1f;

            var sensitivityCurve = Mathf.Max(0.01f, _sensitivityScaleByMagnification.Evaluate(CurrentMagnification));
            var pipPrecisionCurve = Mathf.Max(0.01f, _pipPrecisionScaleByMagnification.Evaluate(CurrentMagnification));
            var swayCurve = Mathf.Max(0.01f, _swayScaleByMagnification.Evaluate(CurrentMagnification));

            var targetAdsSensitivity = baseSensitivity * sensitivityCurve;
            // PiP scopes keep the gameplay FOV fixed, so they need a dedicated precision path.
            var targetPipPrecision = usePipPrecision ? pipPrecisionCurve : 1f;
            var targetAdsSway = baseSway * swayCurve;
            CurrentSensitivityScale = Mathf.Lerp(1f, targetAdsSensitivity, AdsT);
            CurrentPipPrecisionScale = Mathf.Lerp(1f, targetPipPrecision, AdsT);
            CurrentSwayScale = Mathf.Lerp(1f, targetAdsSway, AdsT);
        }

        private void TickVisualMode()
        {
            var optic = _attachmentManager != null ? _attachmentManager.ActiveOpticDefinition : null;
            var policy = optic != null ? optic.VisualModePolicy : AdsVisualMode.Auto;
            if (!ReferenceEquals(_lastMaskOpticDefinition, optic) || _lastMaskPolicy != policy)
            {
                ResetMaskLatchForContext(policy, CurrentMagnification);
                _lastMaskOpticDefinition = optic;
                _lastMaskPolicy = policy;
            }

            var useMask = ResolveMaskMode(policy, CurrentMagnification);
            var adsVisible = AdsT > 0.01f;
            var scopedOpticsSettings = ResolveScopedOpticsSettings();
            var usePip = adsVisible
                && !IsShotCameraActive()
                && policy == AdsVisualMode.RenderTexturePiP;
            var pipResolutionMin = ResolveScopedOpticsMinPipResolutionPercent();
            var pipResolutionMax = ResolveScopedOpticsMaxPipResolutionPercent();
            var peripheralBlurMin = ResolveScopedOpticsMinPeripheralBlurPercent();
            var peripheralBlurMax = ResolveScopedOpticsMaxPeripheralBlurPercent();
            var pipResolutionPercent = Mathf.Clamp(
                scopedOpticsSettings.PipResolutionPercent,
                pipResolutionMin,
                pipResolutionMax);
            var peripheralBlurPercent = Mathf.Clamp(
                scopedOpticsSettings.PeripheralBlurPercent,
                peripheralBlurMin,
                peripheralBlurMax);
            var normalizedPeripheralBlur = peripheralBlurPercent / 100f;

            if (_scopeMaskController != null)
            {
                _scopeMaskController.SetReticleSprite(optic != null ? optic.ReticleUiSprite : null);
                _scopeMaskController.SetState(adsVisible && useMask, CurrentMagnification, AdsT);
            }

            if (_peripheralScopeEffects != null)
            {
                _peripheralScopeEffects.SetState(usePip, AdsT, normalizedPeripheralBlur);
            }
            else
            {
                PeripheralScopeBlurRuntimeState.Reset();
            }

            ScopedPeripheralWorldRenderScaleRuntime.Apply(usePip, normalizedPeripheralBlur);

            if (_renderTextureScopeController != null)
            {
                _renderTextureScopeController.SetScopedPipResolutionPercent(pipResolutionPercent);
                _renderTextureScopeController.SetApertureCamera(_viewmodelCamera != null ? _viewmodelCamera : _worldCamera);
                var scopeReferenceFov = _baseWorldFov;
                var scopeMagnification = Mathf.Max(MinMagnification, CurrentMagnification);
                var activeOpticInstance = _attachmentManager != null ? _attachmentManager.ActiveOpticInstance : null;
                var activeAdjustmentController = _attachmentManager != null ? _attachmentManager.ActiveScopeAdjustmentController : null;
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
            }

            UpdateScopeAdjustmentTooltip(
                _isAdsHeld && policy == AdsVisualMode.RenderTexturePiP,
                _attachmentManager != null ? _attachmentManager.ActiveScopeAdjustmentController : null);

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
            if (optic == null)
            {
                _targetMagnification = 1f;
            }
            else if (!optic.IsVariableZoom)
            {
                _targetMagnification = optic.MagnificationMin;
            }
            else
            {
                _targetMagnification = optic.ClampMagnification(_targetMagnification);
            }

            CurrentMagnification = ResolveClampedMagnification(CurrentMagnification);
            _targetMagnification = ResolveClampedMagnification(_targetMagnification);

            var policy = optic != null ? optic.VisualModePolicy : AdsVisualMode.Auto;
            ResetMaskLatchForContext(policy, CurrentMagnification);
            _lastMaskOpticDefinition = optic;
            _lastMaskPolicy = policy;

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

        private bool UsesScopedPip(OpticDefinition optic)
        {
            return optic != null && optic.VisualModePolicy == AdsVisualMode.RenderTexturePiP;
        }

        private ScopedOpticsSettings ResolveScopedOpticsSettings()
        {
            if (_hasResolvedScopedOpticsSettingsSource == false)
            {
                _scopedOpticsSettingsSourceType = ResolveType(ScopedOpticsSettingsSourceTypeName);
                _scopedOpticsSettingsSnapshotType = ResolveType(ScopedOpticsSettingsSnapshotTypeName);
                _scopedOpticsSettingsContractType = ResolveType(ScopedOpticsSettingsContractTypeName);
                _scopedOpticsSettingsSource = ResolveScopedOpticsSettingsSource(_scopedOpticsSettingsSourceType);
                _hasResolvedScopedOpticsSettingsSource = true;
                ScheduleScopedOpticsSettingsSourceLookupRetry(_scopedOpticsSettingsSource);
            }
            else if (_scopedOpticsSettingsSourceType != null
                && Time.frameCount >= _nextScopedOpticsSettingsSourceLookupFrame)
            {
                _scopedOpticsSettingsSource = ResolveScopedOpticsSettingsSource(_scopedOpticsSettingsSourceType);
                ScheduleScopedOpticsSettingsSourceLookupRetry(_scopedOpticsSettingsSource);
            }

            var minPip = ResolveScopedOpticsMinPipResolutionPercent();
            var maxPip = ResolveScopedOpticsMaxPipResolutionPercent();
            var minBlur = ResolveScopedOpticsMinPeripheralBlurPercent();
            var maxBlur = ResolveScopedOpticsMaxPeripheralBlurPercent();
            var source = _scopedOpticsSettingsSource;
            var fallback = new ScopedOpticsSettings(
                ReadIntegerSettingFromPlayerPrefs(ScopedPipResolutionPlayerPrefKey, FallbackScopedPipResolutionPercent, minPip, maxPip),
                ReadIntegerSettingFromPlayerPrefs(PeripheralBlurPlayerPrefKey, FallbackPeripheralBlurPercent, minBlur, maxBlur));

            if (source == null || _scopedOpticsSettingsSourceType == null)
            {
                return fallback;
            }

            var settingsFromSource = TryGetSettingsFromSource(
                source,
                _scopedOpticsSettingsSourceType,
                _scopedOpticsSettingsSnapshotType,
                fallback);

            if (settingsFromSource != null)
            {
                return settingsFromSource.Value;
            }

            var pipPercent = ReadIntegerSettingFromSource(
                source,
                _scopedOpticsSettingsSourceType,
                "GetScopedPipResolutionPercent",
                fallback.PipResolutionPercent);
            var blurPercent = ReadIntegerSettingFromSource(
                source,
                _scopedOpticsSettingsSourceType,
                "GetPeripheralBlurPercent",
                fallback.PeripheralBlurPercent);

            return new ScopedOpticsSettings(
                Mathf.Clamp(pipPercent, minPip, maxPip),
                Mathf.Clamp(blurPercent, minBlur, maxBlur));
        }

        private void ScheduleScopedOpticsSettingsSourceLookupRetry(object source)
        {
            if (source != null)
            {
                _nextScopedOpticsSettingsSourceLookupFrame = Time.frameCount + ScopedOpticsSettingsSourceInitialRetryFrameInterval;
                _scopedOpticsSettingsSourceRetryFrameInterval = ScopedOpticsSettingsSourceInitialRetryFrameInterval;
                return;
            }

            _nextScopedOpticsSettingsSourceLookupFrame = Time.frameCount + _scopedOpticsSettingsSourceRetryFrameInterval;
            _scopedOpticsSettingsSourceRetryFrameInterval = Mathf.Min(
                _scopedOpticsSettingsSourceRetryFrameInterval * 2,
                ScopedOpticsSettingsSourceMaxRetryFrameInterval);
        }

        private object ResolveScopedOpticsSettingsSource(Type sourceType)
        {
            if (sourceType == null)
            {
                return null;
            }

            var settingsObjects = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            if (settingsObjects == null || settingsObjects.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < settingsObjects.Length; i++)
            {
                var candidate = settingsObjects[i];
                if (candidate == null)
                {
                    continue;
                }

                if (sourceType.IsInstanceOfType(candidate))
                {
                    return candidate;
                }

                var providerFromMember = ResolveScopedOpticsSettingsSourceFromMembers(candidate, sourceType);
                if (providerFromMember != null)
                {
                    return providerFromMember;
                }
            }

            return null;
        }

        private static object ResolveScopedOpticsSettingsSourceFromMembers(object host, Type sourceType)
        {
            if (host == null || sourceType == null)
            {
                return null;
            }

            var hostType = host.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fields = hostType.GetFields(flags);
            for (var i = 0; i < fields.Length; i++)
            {
                object value;
                try
                {
                    value = fields[i].GetValue(host);
                }
                catch
                {
                    continue;
                }

                if (value != null && sourceType.IsInstanceOfType(value))
                {
                    return value;
                }
            }

            var properties = hostType.GetProperties(flags);
            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                if (!property.CanRead || property.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                object value;
                try
                {
                    value = property.GetValue(host, null);
                }
                catch
                {
                    continue;
                }

                if (value != null && sourceType.IsInstanceOfType(value))
                {
                    return value;
                }
            }

            return null;
        }

        private ScopedOpticsSettings? TryGetSettingsFromSource(
            object source,
            Type sourceType,
            Type snapshotType,
            ScopedOpticsSettings fallback)
        {
            if (snapshotType == null)
            {
                return null;
            }

            var snapshotMethod = sourceType.GetMethod("GetScopedOpticsSettingsSnapshot", BindingFlags.Instance | BindingFlags.Public);
            if (snapshotMethod == null || snapshotMethod.ReturnType != snapshotType || snapshotMethod.GetParameters().Length != 0)
            {
                return null;
            }

            object snapshot;
            try
            {
                snapshot = snapshotMethod.Invoke(source, null);
            }
            catch (TargetInvocationException ex)
            {
                Debug.LogWarning($"AdsStateController: Unable to read scoped optics snapshot from source contract. {ex.InnerException?.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AdsStateController: Unable to read scoped optics snapshot from source contract. {ex.Message}");
                return null;
            }

            if (snapshot == null)
            {
                return null;
            }

            var pipProperty = snapshotType.GetProperty("PipResolutionPercent", BindingFlags.Instance | BindingFlags.Public);
            var blurProperty = snapshotType.GetProperty("PeripheralBlurPercent", BindingFlags.Instance | BindingFlags.Public);
            if (pipProperty == null || blurProperty == null)
            {
                return null;
            }

            try
            {
                var pipPercent = Convert.ToInt32(pipProperty.GetValue(snapshot, null));
                var blurPercent = Convert.ToInt32(blurProperty.GetValue(snapshot, null));
                return new ScopedOpticsSettings(
                    Mathf.Clamp(
                        pipPercent,
                        ResolveScopedOpticsMinPipResolutionPercent(),
                        ResolveScopedOpticsMaxPipResolutionPercent()),
                    Mathf.Clamp(
                        blurPercent,
                        ResolveScopedOpticsMinPeripheralBlurPercent(),
                        ResolveScopedOpticsMaxPeripheralBlurPercent()));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AdsStateController: Unable to parse scoped optics snapshot values. {ex.Message}");
                return null;
            }
        }

        private int ReadIntegerSettingFromSource(object source, Type sourceType, string methodName, int fallback)
        {
            try
            {
                var method = sourceType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
                if (method == null || method.ReturnType != typeof(int) || method.GetParameters().Length != 0)
                {
                    return fallback;
                }

                return Convert.ToInt32(method.Invoke(source, null));
            }
            catch (TargetInvocationException ex)
            {
                Debug.LogWarning($"AdsStateController: Failed to read {methodName} from scoped optics source. {ex.InnerException?.Message}");
                return fallback;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AdsStateController: Failed to read {methodName} from scoped optics source. {ex.Message}");
                return fallback;
            }
        }

        private static int ReadIntegerSettingFromPlayerPrefs(string key, int fallback, int minValue, int maxValue)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(key, fallback), minValue, maxValue);
        }

        private static Type ResolveType(string fullyQualifiedTypeName)
        {
            var type = Type.GetType(fullyQualifiedTypeName);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullyQualifiedTypeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static bool IsShotCameraActive()
        {
            if (s_shotCameraIsActiveProperty == null)
            {
                s_shotCameraIsActiveProperty = ResolveType(ShotCameraGameplayStateTypeName)?.GetProperty(
                    "IsActive",
                    BindingFlags.Public | BindingFlags.Static);
            }

            return s_shotCameraIsActiveProperty?.GetValue(null) is true;
        }

        private int ResolveScopedOpticsMinPipResolutionPercent()
        {
            return _scopedOpticsSettingsContractType != null
                ? ReadContractInt(_scopedOpticsSettingsContractType, "MinPipResolutionPercent", ScopedOpticsSettingsMinPipResolutionPercent)
                : ScopedOpticsSettingsMinPipResolutionPercent;
        }

        private int ResolveScopedOpticsMaxPipResolutionPercent()
        {
            return _scopedOpticsSettingsContractType != null
                ? ReadContractInt(_scopedOpticsSettingsContractType, "MaxPipResolutionPercent", ScopedOpticsSettingsMaxPipResolutionPercent)
                : ScopedOpticsSettingsMaxPipResolutionPercent;
        }

        private int ResolveScopedOpticsMinPeripheralBlurPercent()
        {
            return _scopedOpticsSettingsContractType != null
                ? ReadContractInt(_scopedOpticsSettingsContractType, "MinPeripheralBlurPercent", ScopedOpticsSettingsMinPeripheralBlurPercent)
                : ScopedOpticsSettingsMinPeripheralBlurPercent;
        }

        private int ResolveScopedOpticsMaxPeripheralBlurPercent()
        {
            return _scopedOpticsSettingsContractType != null
                ? ReadContractInt(_scopedOpticsSettingsContractType, "MaxPeripheralBlurPercent", ScopedOpticsSettingsMaxPeripheralBlurPercent)
                : ScopedOpticsSettingsMaxPeripheralBlurPercent;
        }

        private static int ReadContractInt(Type contractType, string fieldName, int fallback)
        {
            try
            {
                var field = contractType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
                return field == null ? fallback : Convert.ToInt32(field.GetValue(null));
            }
            catch (Exception)
            {
                return fallback;
            }
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
            var optic = _attachmentManager != null ? _attachmentManager.ActiveOpticDefinition : null;
            if (optic == null)
            {
                return 1f;
            }

            return ResolveClampedMagnification(optic.MagnificationMin);
        }

        private float ResolveClampedMagnification(float requested)
        {
            var optic = _attachmentManager != null ? _attachmentManager.ActiveOpticDefinition : null;
            if (optic == null)
            {
                return Mathf.Clamp(requested, 1f, 1f);
            }

            return Mathf.Clamp(optic.ClampMagnification(requested), MinMagnification, MaxMagnification);
        }

        private readonly struct ScopedOpticsSettings
        {
            public ScopedOpticsSettings(int pipResolutionPercent, int peripheralBlurPercent)
            {
                PipResolutionPercent = pipResolutionPercent;
                PeripheralBlurPercent = peripheralBlurPercent;
            }

            public int PipResolutionPercent { get; }
            public int PeripheralBlurPercent { get; }
        }
    }
}
