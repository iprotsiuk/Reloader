using UnityEngine;

namespace Reloader.Game.Weapons
{
    [CreateAssetMenu(fileName = "OpticDefinition", menuName = "Reloader/Game/Weapons/Optic Definition")]
    public sealed class OpticDefinition : ScriptableObject
    {
        public const float MinSupportedMagnification = 1f;
        public const float MaxSupportedMagnification = 40f;

        [System.Serializable]
        public struct ScopeRenderProfile
        {
            [SerializeField] private int _renderTextureResolution;
            [SerializeField] private float _scopeCameraFov;

            public int RenderTextureResolution => Mathf.Clamp(_renderTextureResolution, 128, 4096);
            public float ScopeCameraFov => Mathf.Clamp(_scopeCameraFov, 1f, 60f);
        }

        [SerializeField] private string _opticId = string.Empty;
        [SerializeField] private OpticCategory _category = OpticCategory.RedDot;
        [SerializeField] private GameObject _opticPrefab;
        [SerializeField] private bool _isVariableZoom;
        [SerializeField] private float _magnificationMin = 1f;
        [SerializeField] private float _magnificationMax = 1f;
        [SerializeField, Min(0.01f)] private float _magnificationStep = 0.25f;
        [SerializeField] private AdsVisualMode _visualModePolicy = AdsVisualMode.Auto;
        [SerializeField, Min(0f)] private float _eyeReliefBackOffset;
        [SerializeField] private Sprite _reticleUiSprite;
        [SerializeField] private ScopeReticleDefinition _scopeReticleDefinition;
        [Header("Scope Adjustment")]
        [SerializeField, Min(0.001f)] private float _mradPerClick = 0.1f;
        [SerializeField] private int _minWindageClicks = -20;
        [SerializeField] private int _maxWindageClicks = 20;
        [SerializeField] private int _minElevationClicks = -20;
        [SerializeField] private int _maxElevationClicks = 20;
        [Header("PiP Calibration")]
        [SerializeField] private Vector2 _mechanicalZeroOffsetMrad = Vector2.zero;
        [SerializeField, Min(0.01f)] private float _projectionCalibrationMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float _compositeReticleScale = 1f;
        [SerializeField] private Vector2 _compositeReticleOffset = Vector2.zero;
        [SerializeField] private bool _hasScopeRenderProfile;
        [SerializeField] private ScopeRenderProfile _scopeRenderProfile;

        public string OpticId => string.IsNullOrWhiteSpace(_opticId) ? string.Empty : _opticId;
        public OpticCategory Category => _category;
        public GameObject OpticPrefab => _opticPrefab;
        public bool IsVariableZoom => _isVariableZoom;
        public float MagnificationMin => ClampMagnitude(_magnificationMin);
        public float MagnificationMax => _isVariableZoom
            ? Mathf.Clamp(_magnificationMax, MagnificationMin, MaxSupportedMagnification)
            : MagnificationMin;
        public float MagnificationStep => _isVariableZoom
            ? Mathf.Clamp(_magnificationStep, 0.01f, MagnificationMax - MagnificationMin + 0.01f)
            : 0f;
        public AdsVisualMode VisualModePolicy => _visualModePolicy;
        public float EyeReliefBackOffset => Mathf.Max(0f, _eyeReliefBackOffset);
        public Sprite ReticleUiSprite => _reticleUiSprite;
        public ScopeReticleDefinition ScopeReticleDefinition => _scopeReticleDefinition;
        public float MradPerClick => Mathf.Max(0.001f, _mradPerClick);
        public int MinWindageClicks => Mathf.Min(_minWindageClicks, _maxWindageClicks);
        public int MaxWindageClicks => Mathf.Max(_minWindageClicks, _maxWindageClicks);
        public int MinElevationClicks => Mathf.Min(_minElevationClicks, _maxElevationClicks);
        public int MaxElevationClicks => Mathf.Max(_minElevationClicks, _maxElevationClicks);
        public Vector2 MechanicalZeroOffsetMrad => _mechanicalZeroOffsetMrad;
        public float ProjectionCalibrationMultiplier => Mathf.Max(0.01f, _projectionCalibrationMultiplier);
        public float CompositeReticleScale => Mathf.Max(0.01f, _compositeReticleScale);
        public Vector2 CompositeReticleOffset => _compositeReticleOffset;
        public bool HasScopeRenderProfile => _hasScopeRenderProfile;
        public ScopeRenderProfile RenderProfile => _scopeRenderProfile;

        public float ClampMagnification(float value)
        {
            return Mathf.Clamp(value, MagnificationMin, MagnificationMax);
        }

        public float SnapMagnification(float value)
        {
            if (!IsVariableZoom)
            {
                return MagnificationMin;
            }

            var clamped = ClampMagnification(value);
            var step = MagnificationStep;
            var stepsFromMin = Mathf.Round((clamped - MagnificationMin) / step);
            return ClampMagnification(MagnificationMin + (stepsFromMin * step));
        }

        private static float ClampMagnitude(float value)
        {
            return Mathf.Clamp(value, MinSupportedMagnification, MaxSupportedMagnification);
        }
    }
}
