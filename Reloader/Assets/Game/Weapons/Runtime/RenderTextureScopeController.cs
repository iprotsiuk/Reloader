using UnityEngine;
using UnityEngine.Rendering;

namespace Reloader.Game.Weapons
{
    public sealed class RenderTextureScopeController : MonoBehaviour
    {
        private const float MaxProjectionAxisOffset = 0.45f;

        [SerializeField] private Camera _scopeCamera;
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

        public bool IsCompositeReticleActive => _isCompositeReticleActive;
        public Sprite CurrentCompositeReticleSprite => _currentCompositeReticleSprite;
        public float CurrentCompositeReticleScale => _currentCompositeReticleScale;
        public Vector2 CurrentCompositeReticleOffset => _currentCompositeReticleOffset;
        public Vector2 CurrentCompositeReticleDrawScale => _currentCompositeReticleDrawScale;
        public Vector2 CurrentEffectiveAdjustmentMrad => _currentEffectiveAdjustmentMrad;
        public float CurrentProjectionCalibrationMultiplier => _currentProjectionCalibrationMultiplier;
        public float CurrentMradPerClick => _currentMradPerClick;
        public Vector2 CurrentMechanicalZeroOffsetMrad => _currentMechanicalZeroOffsetMrad;

        public void BindRuntimeReferences(Camera scopeCamera, Behaviour[] expensiveScopeBehaviours = null)
        {
            _scopeCamera = scopeCamera;
            if (expensiveScopeBehaviours != null)
            {
                _expensiveScopeBehaviours = expensiveScopeBehaviours;
            }

            _defaultScopeCameraFov = _scopeCamera != null ? _scopeCamera.fieldOfView : 20f;
            ApplyState(false, _defaultScopeCameraFov, Vector2.zero, 1f, 0.1f, Vector2.zero);
        }

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
            var requestedResolution = ResolveRequestedResolution(optic);
            var lensDisplay = ResolveLensDisplay(activeOpticInstance);
            var reticleController = ResolveReticleController(activeOpticInstance);
            var mradPerClick = ResolveMradPerClick(optic);
            var mechanicalZeroOffsetMrad = ResolveMechanicalZeroOffsetMrad(optic);
            var effectiveAdjustmentMrad = ResolveEffectiveAdjustmentMrad(mechanicalZeroOffsetMrad, mradPerClick, windageClicks, elevationClicks);
            var projectionCalibrationMultiplier = ResolveProjectionCalibrationMultiplier(optic);
            var compositeReticleScale = ResolveCompositeReticleScale(optic);
            var compositeReticleOffset = ResolveCompositeReticleOffset(optic);
            var renderTextureStateMatches = !isActive || ScopeRenderTextureMatches(requestedResolution);
            var scopeCameraStateMatches = ScopeCameraStateMatches(isActive, requestedFov);
            var lensDisplayStateMatches = !isActive || (lensDisplay != null && ReferenceEquals(lensDisplay.CurrentTexture, _scopeRenderTexture));

            if (_initialized
                && _lastIsActive == isActive
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
                && ReferenceEquals(_lastLensDisplay, lensDisplay)
                && ReferenceEquals(_lastReticleController, reticleController)
                && renderTextureStateMatches
                && scopeCameraStateMatches
                && lensDisplayStateMatches)
            {
                return;
            }

            if (isActive)
            {
                EnsureRenderTexture(requestedResolution);
            }
            else
            {
                ReleaseRenderTexture();
            }

            BindLensDisplay(isActive, lensDisplay);
            BindReticle(isActive, reticleController, optic, magnification, compositeReticleScale, compositeReticleOffset);
            ApplyState(
                isActive,
                requestedFov,
                effectiveAdjustmentMrad,
                projectionCalibrationMultiplier,
                mradPerClick,
                mechanicalZeroOffsetMrad);
            _lastIsActive = isActive;
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

        private int ResolveRequestedResolution(OpticDefinition optic)
        {
            if (optic != null && optic.HasScopeRenderProfile)
            {
                return optic.RenderProfile.RenderTextureResolution;
            }

            return 1024;
        }

        private ScopeLensDisplay ResolveLensDisplay(GameObject activeOpticInstance)
        {
            if (activeOpticInstance == null)
            {
                return null;
            }

            return activeOpticInstance.GetComponentInChildren<ScopeLensDisplay>(true);
        }

        private ScopeReticleController ResolveReticleController(GameObject activeOpticInstance)
        {
            if (activeOpticInstance == null)
            {
                return null;
            }

            return activeOpticInstance.GetComponentInChildren<ScopeReticleController>(true);
        }

        private void EnsureRenderTexture(int resolution)
        {
            var safeResolution = Mathf.Clamp(resolution, 128, 4096);
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

            if (_scopeRenderTexture.IsCreated())
            {
                _scopeRenderTexture.Release();
            }

            Destroy(_scopeRenderTexture);
            _scopeRenderTexture = null;
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
            var offsetPixels = new Vector2(
                _currentCompositeReticleOffset.x * _scopeRenderTexture.width,
                -_currentCompositeReticleOffset.y * _scopeRenderTexture.height);
            var destination = new Rect(
                ((_scopeRenderTexture.width - width) * 0.5f) + offsetPixels.x,
                ((_scopeRenderTexture.height - height) * 0.5f) + offsetPixels.y,
                width,
                height);
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
            _currentCompositeReticleOffset = compositeReticleOffset;
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
            if (width >= height)
            {
                return new Vector2(safeScale, safeScale * (height / width));
            }

            return new Vector2(safeScale * (width / height), safeScale);
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

            Destroy(_compositeReticleMaterial);
            _compositeReticleMaterial = null;
        }
    }
}
