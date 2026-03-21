using UnityEngine;
using UnityEngine.Rendering;
using Reloader.Game.Weapons.Rendering;

namespace Reloader.Game.Weapons
{
    public sealed class RenderTextureScopeController : MonoBehaviour
    {
        private const int DefaultPipResolutionPercent = 100;
        private const int MinPipResolutionPercent = 10;
        private const int MaxPipResolutionPercent = 400;
        private const float MaxProjectionAxisOffset = 0.45f;
        private const int DefaultScopeRenderTextureResolution = 1024;
        private const int MinimumAdaptiveScopeRenderTextureResolution = 256;
        private const int MaximumAdaptiveScopeRenderTextureResolution = 8192;
        private const float NearSquareReticleAspectTolerance = 0.01f;

        [SerializeField] private Camera _scopeCamera;
        [SerializeField] private Camera _apertureCamera;
        [SerializeField] private Behaviour[] _expensiveScopeBehaviours;
        [Header("Inspector Calibration Overrides")]
        [SerializeField] private bool _useInspectorCalibrationOverrides;
        [SerializeField, Min(0.001f)] private float _inspectorMradPerClick = 0.1f;
        [SerializeField] private Vector2 _inspectorMechanicalZeroOffsetMrad = Vector2.zero;
        [SerializeField, Min(0.01f)] private float _inspectorProjectionCalibrationMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float _inspectorCompositeReticleScale = 1f;
        [SerializeField] private Vector2 _inspectorCompositeReticleOffset = Vector2.zero;
        private float _defaultScopeCameraFov = 20f;
        private bool _lastIsActive;
        private float _lastAppliedFov = -1f;
        private bool _initialized;
        private RenderTexture _scopeRenderTexture;
        private ScopeLensDisplay _lastLensDisplay;
        private ScopeReticleController _lastReticleController;
        private GameObject _lastMissingLensDisplayOpticInstance;
        private GameObject _lastMissingScopeCameraOpticInstance;
        private int _lastResolution = -1;
        private float _lastMagnification = -1f;
        private int _lastWindageClicks;
        private int _lastElevationClicks;
        private Vector2 _lastEffectiveAdjustmentMrad;
        private float _lastProjectionCalibrationMultiplier = -1f;
        private float _lastMradPerClick = -1f;
        private Vector2 _lastMechanicalZeroOffsetMrad;
        private float _lastCompositeReticleScale = -1f;
        private Vector2 _lastCompositeReticleOffset;
        private Sprite _currentCompositeReticleSprite;
        private float _currentCompositeReticleScale = 1f;
        private Vector2 _currentCompositeReticleOffset;
        private Vector2 _currentCompositeReticleDrawScale = Vector2.one;
        private bool _isCompositeReticleActive;
        private Material _compositeReticleMaterial;
        private Vector2 _currentEffectiveAdjustmentMrad;
        private float _currentProjectionCalibrationMultiplier = 1f;
        private float _currentMradPerClick = 0.1f;
        private Vector2 _currentMechanicalZeroOffsetMrad;
        private int _scopedPipResolutionPercent = DefaultPipResolutionPercent;

        public bool IsCompositeReticleActive => _isCompositeReticleActive;
        public Sprite CurrentCompositeReticleSprite => _currentCompositeReticleSprite;
        public float CurrentCompositeReticleScale => _currentCompositeReticleScale;
        public Vector2 CurrentCompositeReticleOffset => _currentCompositeReticleOffset;
        public Vector2 CurrentCompositeReticleDrawScale => _currentCompositeReticleDrawScale;
        public Vector2 CurrentEffectiveAdjustmentMrad => _currentEffectiveAdjustmentMrad;
        public float CurrentProjectionCalibrationMultiplier => _currentProjectionCalibrationMultiplier;
        public float CurrentMradPerClick => _currentMradPerClick;
        public Vector2 CurrentMechanicalZeroOffsetMrad => _currentMechanicalZeroOffsetMrad;
        public int ScopedPipResolutionPercent => _scopedPipResolutionPercent;

        private void Awake()
        {
            if (_scopeCamera != null)
            {
                _defaultScopeCameraFov = _scopeCamera.fieldOfView;
            }

            ApplyState(false, _defaultScopeCameraFov, Vector2.zero, 1f, 0.1f, Vector2.zero);
        }

        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += HandleEndCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= HandleEndCameraRendering;
            DisableCompositeReticle();
            ResetCurrentCalibrationState();
        }

        private void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering -= HandleEndCameraRendering;
            ReleaseCompositeReticleMaterial();
            ReleaseRenderTexture();
        }

        public void SetScopeCamera(Camera scopeCamera)
        {
            if (ReferenceEquals(_scopeCamera, scopeCamera))
            {
                if (_scopeCamera != null)
                {
                    _defaultScopeCameraFov = _scopeCamera.fieldOfView;
                }

                return;
            }

            if (_scopeCamera != null)
            {
                FailClosedScopePresentation();
            }

            _scopeCamera = scopeCamera;
            if (_scopeCamera != null)
            {
                _defaultScopeCameraFov = _scopeCamera.fieldOfView;
            }
        }

        public void SetApertureCamera(Camera apertureCamera)
        {
            _apertureCamera = apertureCamera;
        }

        public void SetScopedPipResolutionPercent(int pipResolutionPercent)
        {
            _scopedPipResolutionPercent = Mathf.Clamp(pipResolutionPercent, MinPipResolutionPercent, MaxPipResolutionPercent);
        }

        public void SetScopeActive(
            bool isActive,
            OpticDefinition optic,
            GameObject activeOpticInstance,
            float referenceFieldOfView,
            float magnification,
            int windageClicks,
            int elevationClicks)
        {
            var requestedFov = ResolveRequestedFov(isActive, optic, referenceFieldOfView, magnification);
            var requestedResolution = ResolveRequestedResolution(optic, requestedFov);
            var lensDisplay = ResolveLensDisplay(activeOpticInstance);
            var reticleController = ResolveReticleController(activeOpticInstance);
            var requiresPipPresentation = isActive && optic != null && optic.VisualModePolicy == AdsVisualMode.RenderTexturePiP;
            var missingScopeCamera = requiresPipPresentation && _scopeCamera == null;
            var missingLensDisplay = requiresPipPresentation && lensDisplay == null;
            var mradPerClick = ResolveMradPerClick(optic);
            var mechanicalZeroOffsetMrad = ResolveMechanicalZeroOffsetMrad(optic);
            var effectiveAdjustmentMrad = ResolveEffectiveAdjustmentMrad(mechanicalZeroOffsetMrad, mradPerClick, windageClicks, elevationClicks);
            var projectionCalibrationMultiplier = ResolveProjectionCalibrationMultiplier(optic);
            var compositeReticleScale = ResolveCompositeReticleScale(optic);
            var compositeReticleOffset = ResolveCompositeReticleOffset(optic);
            var effectiveIsActive = isActive && !missingScopeCamera && !missingLensDisplay;
            var renderTextureStateMatches = !effectiveIsActive || ScopeRenderTextureMatches(requestedResolution);
            var scopeCameraStateMatches = ScopeCameraStateMatches(effectiveIsActive, requestedFov);
            var lensDisplayStateMatches = !effectiveIsActive || (lensDisplay != null && ReferenceEquals(lensDisplay.CurrentTexture, _scopeRenderTexture));

            UpdatePeripheralBlurAperture(effectiveIsActive, lensDisplay);

            if (_initialized
                && _lastIsActive == effectiveIsActive
                && Mathf.Approximately(_lastAppliedFov, requestedFov)
                && _lastResolution == requestedResolution
                && Mathf.Approximately(_lastMagnification, magnification)
                && _lastWindageClicks == windageClicks
                && _lastElevationClicks == elevationClicks
                && Approximately(_lastEffectiveAdjustmentMrad, effectiveAdjustmentMrad)
                && Mathf.Approximately(_lastProjectionCalibrationMultiplier, projectionCalibrationMultiplier)
                && Mathf.Approximately(_lastMradPerClick, mradPerClick)
                && Approximately(_lastMechanicalZeroOffsetMrad, mechanicalZeroOffsetMrad)
                && Mathf.Approximately(_lastCompositeReticleScale, compositeReticleScale)
                && Approximately(_lastCompositeReticleOffset, compositeReticleOffset)
                && ReferenceEquals(_lastMissingScopeCameraOpticInstance, missingScopeCamera ? activeOpticInstance : null)
                && ReferenceEquals(_lastLensDisplay, lensDisplay)
                && ReferenceEquals(_lastReticleController, reticleController)
                && ReferenceEquals(_lastMissingLensDisplayOpticInstance, missingLensDisplay ? activeOpticInstance : null)
                && renderTextureStateMatches
                && scopeCameraStateMatches
                && lensDisplayStateMatches)
            {
                return;
            }

            if (missingScopeCamera && !ReferenceEquals(_lastMissingScopeCameraOpticInstance, activeOpticInstance))
            {
                Debug.LogWarning("RenderTextureScopeController: Active scoped optic is missing a scope camera binding.", this);
            }

            if (missingLensDisplay && !ReferenceEquals(_lastMissingLensDisplayOpticInstance, activeOpticInstance))
            {
                Debug.LogWarning("RenderTextureScopeController: Active scoped optic is missing an authored optic-root ScopeLensDisplay binding.", this);
            }

            if (effectiveIsActive)
            {
                EnsureRenderTexture(requestedResolution);
                effectiveIsActive = _scopeRenderTexture != null && _scopeRenderTexture.IsCreated();
            }

            if (!effectiveIsActive)
            {
                ReleaseRenderTexture();
            }

            BindLensDisplay(effectiveIsActive, lensDisplay);
            BindReticle(effectiveIsActive, reticleController, optic, magnification, compositeReticleScale, compositeReticleOffset);
            ApplyState(
                effectiveIsActive,
                requestedFov,
                effectiveAdjustmentMrad,
                projectionCalibrationMultiplier,
                mradPerClick,
                mechanicalZeroOffsetMrad);
            _lastIsActive = effectiveIsActive;
            _lastAppliedFov = requestedFov;
            _lastResolution = requestedResolution;
            _lastMagnification = magnification;
            _lastWindageClicks = windageClicks;
            _lastElevationClicks = elevationClicks;
            _lastEffectiveAdjustmentMrad = effectiveAdjustmentMrad;
            _lastProjectionCalibrationMultiplier = projectionCalibrationMultiplier;
            _lastMradPerClick = mradPerClick;
            _lastMechanicalZeroOffsetMrad = mechanicalZeroOffsetMrad;
            _lastCompositeReticleScale = compositeReticleScale;
            _lastCompositeReticleOffset = compositeReticleOffset;
            _lastLensDisplay = lensDisplay;
            _lastReticleController = reticleController;
            _lastMissingScopeCameraOpticInstance = missingScopeCamera ? activeOpticInstance : null;
            _lastMissingLensDisplayOpticInstance = missingLensDisplay ? activeOpticInstance : null;
            _initialized = true;
        }

        private void ApplyState(
            bool isActive,
            float requestedFov,
            Vector2 effectiveAdjustmentMrad,
            float projectionCalibrationMultiplier,
            float mradPerClick,
            Vector2 mechanicalZeroOffsetMrad)
        {
            if (_scopeCamera != null)
            {
                _scopeCamera.fieldOfView = isActive ? requestedFov : _defaultScopeCameraFov;
                _scopeCamera.targetTexture = isActive ? _scopeRenderTexture : null;
                ApplyProjectionOffset(isActive, effectiveAdjustmentMrad, projectionCalibrationMultiplier);
                _scopeCamera.enabled = isActive;
            }

            _currentEffectiveAdjustmentMrad = isActive ? effectiveAdjustmentMrad : Vector2.zero;
            _currentProjectionCalibrationMultiplier = isActive ? projectionCalibrationMultiplier : 1f;
            _currentMradPerClick = isActive ? mradPerClick : 0.1f;
            _currentMechanicalZeroOffsetMrad = isActive ? mechanicalZeroOffsetMrad : Vector2.zero;

            if (_expensiveScopeBehaviours != null)
            {
                for (var i = 0; i < _expensiveScopeBehaviours.Length; i++)
                {
                    if (_expensiveScopeBehaviours[i] != null)
                    {
                        _expensiveScopeBehaviours[i].enabled = isActive;
                    }
                }
            }
        }

        private float ResolveRequestedFov(bool isActive, OpticDefinition optic, float referenceFieldOfView, float magnification)
        {
            if (!isActive || optic == null)
            {
                return _defaultScopeCameraFov;
            }

            if (optic.HasScopeRenderProfile)
            {
                return optic.RenderProfile.ScopeCameraFov;
            }

            return MagnificationToFieldOfView(referenceFieldOfView, magnification);
        }

        private int ResolveRequestedResolution(OpticDefinition optic, float requestedFov)
        {
            if (optic != null && optic.HasScopeRenderProfile)
            {
                return optic.RenderProfile.RenderTextureResolution;
            }

            if (optic == null || optic.VisualModePolicy != AdsVisualMode.RenderTexturePiP)
            {
                return DefaultScopeRenderTextureResolution;
            }

            return ResolveAdaptiveResolution(ResolveAdaptiveResolutionBaseline(), _scopedPipResolutionPercent);
        }

        private ScopeLensDisplay ResolveLensDisplay(GameObject activeOpticInstance)
        {
            if (activeOpticInstance == null)
            {
                return null;
            }

            return FindDirectChildComponent<ScopeLensDisplay>(activeOpticInstance.transform);
        }

        private ScopeReticleController ResolveReticleController(GameObject activeOpticInstance)
        {
            if (activeOpticInstance == null)
            {
                return null;
            }

            return activeOpticInstance.GetComponentInChildren<ScopeReticleController>(true);
        }

        private void UpdatePeripheralBlurAperture(bool isActive, ScopeLensDisplay lensDisplay)
        {
            if (!isActive)
            {
                PeripheralScopeBlurRuntimeState.ClearAperture();
                return;
            }

            if (!TryResolveLensViewportRectNormalized(_apertureCamera, lensDisplay, out var viewportRect))
            {
                PeripheralScopeBlurRuntimeState.ClearAperture();
                return;
            }

            PeripheralScopeBlurRuntimeState.UpdateAperture(
                viewportRect.center.x,
                viewportRect.center.y,
                viewportRect.width,
                viewportRect.height,
                0.04f);
        }

        private static T FindDirectChildComponent<T>(Transform root) where T : Component
        {
            if (root == null)
            {
                return null;
            }

            var componentOnRoot = root.GetComponent<T>();
            if (componentOnRoot != null)
            {
                return componentOnRoot;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var component = root.GetChild(i).GetComponent<T>();
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static bool TryResolveLensViewportRectNormalized(Camera apertureCamera, ScopeLensDisplay lensDisplay, out Rect viewportRect)
        {
            viewportRect = default;
            var targetRenderer = lensDisplay != null ? lensDisplay.TargetRenderer : null;
            if (apertureCamera == null || targetRenderer == null)
            {
                return false;
            }

            var bounds = targetRenderer.bounds;
            if (bounds.size.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            var min = bounds.min;
            var max = bounds.max;
            var corners = new[]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };

            var minX = float.PositiveInfinity;
            var minY = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var maxY = float.NegativeInfinity;
            var resolvedVisiblePoint = false;

            for (var i = 0; i < corners.Length; i++)
            {
                var viewportPoint = apertureCamera.WorldToViewportPoint(corners[i]);
                if (viewportPoint.z <= 0f)
                {
                    continue;
                }

                resolvedVisiblePoint = true;
                minX = Mathf.Min(minX, viewportPoint.x);
                minY = Mathf.Min(minY, viewportPoint.y);
                maxX = Mathf.Max(maxX, viewportPoint.x);
                maxY = Mathf.Max(maxY, viewportPoint.y);
            }

            if (!resolvedVisiblePoint)
            {
                return false;
            }

            minX = Mathf.Clamp01(minX);
            minY = Mathf.Clamp01(minY);
            maxX = Mathf.Clamp01(maxX);
            maxY = Mathf.Clamp01(maxY);
            if (maxX <= minX || maxY <= minY)
            {
                return false;
            }

            viewportRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return true;
        }

        private void EnsureRenderTexture(int resolution)
        {
            var safeResolution = Mathf.Clamp(
                resolution,
                MinimumAdaptiveScopeRenderTextureResolution,
                MaximumAdaptiveScopeRenderTextureResolution);
            if (_scopeRenderTexture != null
                && _scopeRenderTexture.width == safeResolution
                && _scopeRenderTexture.height == safeResolution)
            {
                return;
            }

            ReleaseRenderTexture();
            _scopeRenderTexture = new RenderTexture(safeResolution, safeResolution, 24, RenderTextureFormat.ARGB32)
            {
                name = $"ScopeRT_{safeResolution}"
            };
            _scopeRenderTexture.Create();
        }

        private bool ScopeRenderTextureMatches(int resolution)
        {
            return _scopeRenderTexture != null
                && _scopeRenderTexture.width == resolution
                && _scopeRenderTexture.height == resolution
                && _scopeRenderTexture.IsCreated();
        }

        private bool ScopeCameraStateMatches(bool isActive, float requestedFov)
        {
            if (_scopeCamera == null)
            {
                return !isActive;
            }

            var expectedTargetTexture = isActive ? _scopeRenderTexture : null;
            var expectedFieldOfView = isActive ? requestedFov : _defaultScopeCameraFov;
            return _scopeCamera.enabled == isActive
                && ReferenceEquals(_scopeCamera.targetTexture, expectedTargetTexture)
                && Mathf.Approximately(_scopeCamera.fieldOfView, expectedFieldOfView);
        }

        private static int ResolveAdaptiveResolution(int nativeSquareBaseline, int pipResolutionPercent)
        {
            var scopedPipResolutionPercent = Mathf.Clamp(pipResolutionPercent, MinPipResolutionPercent, MaxPipResolutionPercent);
            var scopedResolutionScale = scopedPipResolutionPercent / 100f;

            var baseline = nativeSquareBaseline;
            if (baseline <= 0)
            {
                baseline = DefaultScopeRenderTextureResolution;
            }

            var scaledResolution = Mathf.CeilToInt(baseline * scopedResolutionScale);
            return Mathf.Clamp(
                scaledResolution,
                MinimumAdaptiveScopeRenderTextureResolution,
                MaximumAdaptiveScopeRenderTextureResolution);
        }

        private static int ResolveAdaptiveResolutionBaseline()
        {
            var nativeSquareBaseline = Mathf.Max(Screen.width, Screen.height);
            if (nativeSquareBaseline <= 0)
            {
                return DefaultScopeRenderTextureResolution;
            }

            return nativeSquareBaseline;
        }

        private void BindLensDisplay(bool isActive, ScopeLensDisplay lensDisplay)
        {
            if (!isActive)
            {
                if (_lastLensDisplay != null)
                {
                    _lastLensDisplay.TrySetTexture(null);
                }

                return;
            }

            if (lensDisplay == null)
            {
                Debug.LogWarning("RenderTextureScopeController: Active scoped optic is missing a ScopeLensDisplay binding.", this);
                return;
            }

            if (_scopeRenderTexture == null)
            {
                lensDisplay.TrySetTexture(null);
                return;
            }

            lensDisplay.TrySetTexture(_scopeRenderTexture);
        }

        private void BindReticle(
            bool isActive,
            ScopeReticleController reticleController,
            OpticDefinition optic,
            float magnification,
            float compositeReticleScale,
            Vector2 compositeReticleOffset)
        {
            if (!isActive)
            {
                ClearReticleController(_lastReticleController);
                DisableCompositeReticle();
                return;
            }

            var reticleDefinition = optic != null ? optic.ScopeReticleDefinition : null;
            if (optic == null || reticleDefinition == null)
            {
                Debug.LogWarning("RenderTextureScopeController: Active scoped optic is missing a ScopeReticleDefinition binding.", this);
            }

            if (optic != null && optic.VisualModePolicy == AdsVisualMode.RenderTexturePiP)
            {
                if (_lastReticleController != null && !ReferenceEquals(_lastReticleController, reticleController))
                {
                    ClearReticleController(_lastReticleController);
                }

                ClearReticleController(reticleController);
                EnableCompositeReticle(reticleDefinition, magnification, compositeReticleScale, compositeReticleOffset);
                return;
            }

            DisableCompositeReticle();
            if (reticleController == null)
            {
                Debug.LogWarning("RenderTextureScopeController: Active scoped optic is missing a ScopeReticleController binding.", this);
                return;
            }

            reticleController.ApplyReticle(reticleDefinition, magnification);
        }

        private void ReleaseRenderTexture()
        {
            if (_scopeRenderTexture == null)
            {
                return;
            }

            if (_scopeCamera != null && ReferenceEquals(_scopeCamera.targetTexture, _scopeRenderTexture))
            {
                _scopeCamera.enabled = false;
                _scopeCamera.targetTexture = null;
            }

            if (_scopeRenderTexture.IsCreated())
            {
                _scopeRenderTexture.Release();
            }

            DestroyRuntimeObject(_scopeRenderTexture);
            _scopeRenderTexture = null;
        }

        private void FailClosedScopePresentation()
        {
            UpdatePeripheralBlurAperture(false, null);
            BindLensDisplay(false, null);
            BindReticle(false, null, null, 1f, 1f, Vector2.zero);
            ReleaseRenderTexture();
            ApplyState(false, _defaultScopeCameraFov, Vector2.zero, 1f, 0.1f, Vector2.zero);
            _lastIsActive = false;
            _lastMissingScopeCameraOpticInstance = null;
            _lastMissingLensDisplayOpticInstance = null;
        }

        private void ApplyProjectionOffset(bool isActive, Vector2 effectiveAdjustmentMrad, float projectionCalibrationMultiplier)
        {
            if (_scopeCamera == null)
            {
                return;
            }

            _scopeCamera.ResetProjectionMatrix();
            if (!isActive)
            {
                return;
            }

            var xOffset = Mathf.Clamp(
                -ConvertMradToProjectionOffset(effectiveAdjustmentMrad.x * projectionCalibrationMultiplier, horizontal: true),
                -MaxProjectionAxisOffset,
                MaxProjectionAxisOffset);
            var yOffset = Mathf.Clamp(
                -ConvertMradToProjectionOffset(effectiveAdjustmentMrad.y * projectionCalibrationMultiplier, horizontal: false),
                -MaxProjectionAxisOffset,
                MaxProjectionAxisOffset);
            if (Mathf.Approximately(xOffset, 0f) && Mathf.Approximately(yOffset, 0f))
            {
                return;
            }

            var projection = _scopeCamera.projectionMatrix;
            projection.m02 += xOffset;
            projection.m12 += yOffset;
            _scopeCamera.projectionMatrix = projection;
        }

        private static float MagnificationToFieldOfView(float referenceFieldOfView, float magnification)
        {
            var safeReferenceFov = Mathf.Clamp(referenceFieldOfView, 1f, 179f);
            var safeMagnification = Mathf.Max(1f, magnification);
            var referenceHalfAngle = safeReferenceFov * 0.5f * Mathf.Deg2Rad;
            var zoomedHalfAngle = Mathf.Atan(Mathf.Tan(referenceHalfAngle) / safeMagnification);
            return Mathf.Clamp(zoomedHalfAngle * 2f * Mathf.Rad2Deg, 1f, safeReferenceFov);
        }

        private void HandleEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!_isCompositeReticleActive || camera != _scopeCamera || _scopeRenderTexture == null || _currentCompositeReticleSprite == null)
            {
                return;
            }

            var compositeMaterial = EnsureCompositeReticleMaterial();
            if (compositeMaterial == null)
            {
                return;
            }

            var spriteTexture = _currentCompositeReticleSprite.texture;
            if (spriteTexture == null)
            {
                return;
            }

            var previousActive = RenderTexture.active;
            RenderTexture.active = _scopeRenderTexture;

            GL.PushMatrix();
            GL.LoadPixelMatrix(0f, _scopeRenderTexture.width, _scopeRenderTexture.height, 0f);

            var width = _scopeRenderTexture.width * _currentCompositeReticleDrawScale.x;
            var height = _scopeRenderTexture.height * _currentCompositeReticleDrawScale.y;
            var destination = ResolveCompositeReticleDestination(
                _scopeRenderTexture.width,
                new Vector2(
                    width / _scopeRenderTexture.width,
                    height / _scopeRenderTexture.height),
                _currentCompositeReticleOffset);
            var textureRect = _currentCompositeReticleSprite.textureRect;
            var source = new Rect(
                textureRect.x / spriteTexture.width,
                textureRect.y / spriteTexture.height,
                textureRect.width / spriteTexture.width,
                textureRect.height / spriteTexture.height);
            Graphics.DrawTexture(destination, spriteTexture, source, 0, 0, 0, 0, Color.white, compositeMaterial);

            GL.PopMatrix();
            RenderTexture.active = previousActive;
        }

        private void EnableCompositeReticle(
            ScopeReticleDefinition reticleDefinition,
            float magnification,
            float compositeReticleScale,
            Vector2 compositeReticleOffset)
        {
            _currentCompositeReticleSprite = reticleDefinition != null ? reticleDefinition.ReticleSprite : null;
            _currentCompositeReticleScale = compositeReticleScale * ResolveReticleScale(reticleDefinition, magnification);
            _currentCompositeReticleOffset = ResolveEffectiveCompositeReticleOffset(reticleDefinition, magnification, compositeReticleOffset);
            _currentCompositeReticleDrawScale = ResolveCompositeReticleDrawScale(_currentCompositeReticleSprite, _currentCompositeReticleScale);
            _isCompositeReticleActive = _currentCompositeReticleSprite != null;
        }

        private void DisableCompositeReticle()
        {
            _currentCompositeReticleSprite = null;
            _currentCompositeReticleScale = 1f;
            _currentCompositeReticleOffset = Vector2.zero;
            _currentCompositeReticleDrawScale = Vector2.one;
            _isCompositeReticleActive = false;
        }

        private static void ClearReticleController(ScopeReticleController reticleController)
        {
            if (reticleController == null)
            {
                return;
            }

            reticleController.Clear();
        }

        private static float ResolveReticleScale(ScopeReticleDefinition reticleDefinition, float magnification)
        {
            if (reticleDefinition == null || reticleDefinition.Mode == ScopeReticleMode.Sfp)
            {
                return 1f;
            }

            return Mathf.Max(0.01f, Mathf.Max(1f, magnification) / reticleDefinition.ReferenceMagnification);
        }

        private static Vector2 ResolveCompositeReticleDrawScale(Sprite reticleSprite, float compositeReticleScale)
        {
            var safeScale = Mathf.Max(0.01f, compositeReticleScale);
            if (reticleSprite == null)
            {
                return new Vector2(safeScale, safeScale);
            }

            var textureRect = reticleSprite.textureRect;
            var width = Mathf.Max(0.0001f, textureRect.width);
            var height = Mathf.Max(0.0001f, textureRect.height);
            if (Mathf.Abs(1f - (width / height)) <= NearSquareReticleAspectTolerance)
            {
                return new Vector2(safeScale, safeScale);
            }

            if (width >= height)
            {
                return new Vector2(safeScale, safeScale * (height / width));
            }

            return new Vector2(safeScale * (width / height), safeScale);
        }

        private static Vector2 ResolveEffectiveCompositeReticleOffset(
            ScopeReticleDefinition reticleDefinition,
            float magnification,
            Vector2 compositeReticleOffset)
        {
            if (reticleDefinition == null || reticleDefinition.Mode == ScopeReticleMode.Sfp)
            {
                return compositeReticleOffset;
            }

            return compositeReticleOffset * ResolveReticleScale(reticleDefinition, magnification);
        }

        private static Rect ResolveCompositeReticleDestination(
            int renderTextureResolution,
            Vector2 compositeReticleDrawScale,
            Vector2 compositeReticleOffset)
        {
            var safeResolution = Mathf.Max(1, renderTextureResolution);
            var width = Mathf.Max(1f, Mathf.Round(safeResolution * compositeReticleDrawScale.x));
            var height = Mathf.Max(1f, Mathf.Round(safeResolution * compositeReticleDrawScale.y));
            var offsetPixels = new Vector2(
                compositeReticleOffset.x * safeResolution,
                -compositeReticleOffset.y * safeResolution);

            var x = Mathf.Round(((safeResolution - width) * 0.5f) + offsetPixels.x);
            var y = Mathf.Round(((safeResolution - height) * 0.5f) + offsetPixels.y);
            return new Rect(x, y, width, height);
        }

        private Material EnsureCompositeReticleMaterial()
        {
            if (_compositeReticleMaterial != null)
            {
                return _compositeReticleMaterial;
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning("RenderTextureScopeController: Unable to find Sprites/Default shader for PiP reticle compositing.", this);
                return null;
            }

            _compositeReticleMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return _compositeReticleMaterial;
        }

        private static Vector2 ResolveEffectiveAdjustmentMrad(
            Vector2 mechanicalZeroOffsetMrad,
            float mradPerClick,
            int windageClicks,
            int elevationClicks)
        {
            return new Vector2(
                mechanicalZeroOffsetMrad.x + (windageClicks * mradPerClick),
                mechanicalZeroOffsetMrad.y + (elevationClicks * mradPerClick));
        }

        private float ResolveMradPerClick(OpticDefinition optic)
        {
            if (_useInspectorCalibrationOverrides)
            {
                return Mathf.Max(0.001f, _inspectorMradPerClick);
            }

            return optic != null ? optic.MradPerClick : 0.1f;
        }

        private Vector2 ResolveMechanicalZeroOffsetMrad(OpticDefinition optic)
        {
            if (_useInspectorCalibrationOverrides)
            {
                return _inspectorMechanicalZeroOffsetMrad;
            }

            return optic != null ? optic.MechanicalZeroOffsetMrad : Vector2.zero;
        }

        private float ResolveProjectionCalibrationMultiplier(OpticDefinition optic)
        {
            if (_useInspectorCalibrationOverrides)
            {
                return Mathf.Max(0.01f, _inspectorProjectionCalibrationMultiplier);
            }

            return optic != null ? optic.ProjectionCalibrationMultiplier : 1f;
        }

        private float ResolveCompositeReticleScale(OpticDefinition optic)
        {
            if (_useInspectorCalibrationOverrides)
            {
                return Mathf.Max(0.01f, _inspectorCompositeReticleScale);
            }

            return optic != null ? optic.CompositeReticleScale : 1f;
        }

        private Vector2 ResolveCompositeReticleOffset(OpticDefinition optic)
        {
            if (_useInspectorCalibrationOverrides)
            {
                return _inspectorCompositeReticleOffset;
            }

            return optic != null ? optic.CompositeReticleOffset : Vector2.zero;
        }

        private float ConvertMradToProjectionOffset(float mrad, bool horizontal)
        {
            if (_scopeCamera == null)
            {
                return 0f;
            }

            var verticalHalfAngle = Mathf.Max(0.0001f, _scopeCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var aspect = Mathf.Max(0.0001f, _scopeCamera.aspect);
            var denominator = Mathf.Tan(verticalHalfAngle) * (horizontal ? aspect : 1f);
            var angleRadians = mrad * 0.001f;
            return Mathf.Tan(angleRadians) / Mathf.Max(0.0001f, denominator);
        }

        private void ResetCurrentCalibrationState()
        {
            _currentEffectiveAdjustmentMrad = Vector2.zero;
            _currentProjectionCalibrationMultiplier = 1f;
            _currentMradPerClick = 0.1f;
            _currentMechanicalZeroOffsetMrad = Vector2.zero;
            _lastEffectiveAdjustmentMrad = Vector2.zero;
            _lastProjectionCalibrationMultiplier = -1f;
            _lastMradPerClick = -1f;
            _lastMechanicalZeroOffsetMrad = Vector2.zero;
            _lastCompositeReticleScale = -1f;
            _lastCompositeReticleOffset = Vector2.zero;
        }

        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return Mathf.Approximately(left.x, right.x) && Mathf.Approximately(left.y, right.y);
        }

        private void ReleaseCompositeReticleMaterial()
        {
            if (_compositeReticleMaterial == null)
            {
                return;
            }

            DestroyRuntimeObject(_compositeReticleMaterial);
            _compositeReticleMaterial = null;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object runtimeObject)
        {
            if (runtimeObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeObject);
                return;
            }

            DestroyImmediate(runtimeObject);
        }
    }
}
