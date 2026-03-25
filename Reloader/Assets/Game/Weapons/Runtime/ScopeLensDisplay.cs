using UnityEngine;

namespace Reloader.Game.Weapons
{
    public sealed class ScopeLensDisplay : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer _targetRenderer;
        [SerializeField] private Material _displayMaterialTemplate;
        [SerializeField] private Renderer _apertureRenderer;
        [SerializeField] private Renderer _hipLensRenderer;

        private MaterialPropertyBlock _propertyBlock;
        private Material[] _originalTargetSharedMaterials;
        private Material[] _originalHipLensSharedMaterials;
        private Material _runtimeDisplayMaterial;
        private bool _displayMaterialApplied;
        private bool _capturedOriginalTargetRendererEnabled;
        private bool _capturedOriginalHipLensRendererEnabled;
        private bool _originalTargetRendererEnabled;
        private bool _originalHipLensRendererEnabled;

        public Texture CurrentTexture { get; private set; }
        public Renderer TargetRenderer => _targetRenderer != null ? _targetRenderer : (_targetRenderer = GetComponent<Renderer>());
        public Renderer ApertureRenderer => _apertureRenderer;
        public Renderer HipLensRenderer => _hipLensRenderer;
        public bool IsUsingProxySurface => false;

        private void Awake()
        {
            _targetRenderer ??= GetComponent<Renderer>();
            CaptureOriginalTargetRendererState();
            CaptureOriginalHipLensRendererState();

            if (CurrentTexture == null)
            {
                ApplyHipVisualState();
            }
        }

        private void OnDestroy()
        {
            RestoreOriginalTargetMaterials();
            RestoreOriginalHipLensMaterials();
            RestoreOriginalTargetRendererState();
            RestoreOriginalHipLensRendererState();

            if (_runtimeDisplayMaterial != null)
            {
                Destroy(_runtimeDisplayMaterial);
                _runtimeDisplayMaterial = null;
            }
        }

        public bool TrySetTexture(Texture texture)
        {
            _targetRenderer ??= GetComponent<Renderer>();
            if (_targetRenderer == null)
            {
                return false;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            if (texture == null)
            {
                RestoreOriginalTargetMaterials();
                RestoreOriginalHipLensMaterials();
                _propertyBlock.Clear();
                _targetRenderer.SetPropertyBlock(_propertyBlock);
                ApplyHipVisualState();
                CurrentTexture = null;
                return true;
            }

            if (!EnsureDisplayMaterial())
            {
                return false;
            }

            ApplyTextureToRenderer(_targetRenderer, texture);
            ApplyPipVisualState();
            CurrentTexture = texture;
            return true;
        }

        private bool EnsureDisplayMaterial()
        {
            if (_displayMaterialApplied && _runtimeDisplayMaterial != null)
            {
                return true;
            }

            if (_targetRenderer == null)
            {
                return false;
            }

            CaptureOriginalTargetRendererState();
            _originalTargetSharedMaterials = _targetRenderer.sharedMaterials;
            _runtimeDisplayMaterial ??= CreateDisplayMaterial(ResolveSourceMaterial());
            if (_runtimeDisplayMaterial == null)
            {
                return false;
            }

            var materialCount = _originalTargetSharedMaterials != null && _originalTargetSharedMaterials.Length > 0
                ? _originalTargetSharedMaterials.Length
                : 1;
            var displayMaterials = new Material[materialCount];
            for (var i = 0; i < materialCount; i++)
            {
                displayMaterials[i] = _runtimeDisplayMaterial;
            }

            _targetRenderer.sharedMaterials = displayMaterials;
            _displayMaterialApplied = true;
            return true;
        }

        private Material ResolveSourceMaterial()
        {
            if (_displayMaterialTemplate != null)
            {
                return _displayMaterialTemplate;
            }

            if (_originalTargetSharedMaterials != null)
            {
                for (var i = 0; i < _originalTargetSharedMaterials.Length; i++)
                {
                    if (_originalTargetSharedMaterials[i] != null)
                    {
                        return _originalTargetSharedMaterials[i];
                    }
                }
            }

            return _targetRenderer != null ? _targetRenderer.sharedMaterial : null;
        }

        private static Material CreateDisplayMaterial(Material sourceMaterial)
        {
            if (sourceMaterial == null)
            {
                return null;
            }

            return new Material(sourceMaterial)
            {
                name = $"{sourceMaterial.name}_RuntimeInstance"
            };
        }

        private void RestoreOriginalTargetMaterials()
        {
            if (!_displayMaterialApplied || _targetRenderer == null)
            {
                return;
            }

            if (_originalTargetSharedMaterials != null && _originalTargetSharedMaterials.Length > 0)
            {
                _targetRenderer.sharedMaterials = _originalTargetSharedMaterials;
            }

            _displayMaterialApplied = false;
        }

        private void RestoreOriginalHipLensMaterials()
        {
            if (_hipLensRenderer == null)
            {
                return;
            }

            if (_originalHipLensSharedMaterials != null && _originalHipLensSharedMaterials.Length > 0)
            {
                _hipLensRenderer.sharedMaterials = _originalHipLensSharedMaterials;
            }
        }

        private void CaptureOriginalTargetRendererState()
        {
            if (_capturedOriginalTargetRendererEnabled || _targetRenderer == null)
            {
                return;
            }

            _originalTargetRendererEnabled = _targetRenderer.enabled;
            _capturedOriginalTargetRendererEnabled = true;
        }

        private void CaptureOriginalHipLensRendererState()
        {
            if (_capturedOriginalHipLensRendererEnabled || _hipLensRenderer == null)
            {
                return;
            }

            _originalHipLensRendererEnabled = _hipLensRenderer.enabled;
            _originalHipLensSharedMaterials = _hipLensRenderer.sharedMaterials;
            _capturedOriginalHipLensRendererEnabled = true;
        }

        private void RestoreOriginalTargetRendererState()
        {
            if (!_capturedOriginalTargetRendererEnabled || _targetRenderer == null)
            {
                return;
            }

            _targetRenderer.enabled = _originalTargetRendererEnabled;
        }

        private void RestoreOriginalHipLensRendererState()
        {
            if (!_capturedOriginalHipLensRendererEnabled || _hipLensRenderer == null)
            {
                return;
            }

            _hipLensRenderer.enabled = _originalHipLensRendererEnabled;
        }

        private void ApplyHipVisualState()
        {
            if (_targetRenderer != null)
            {
                _targetRenderer.enabled = false;
            }

            if (_hipLensRenderer != null)
            {
                _hipLensRenderer.enabled = true;
            }
        }

        private void ApplyPipVisualState()
        {
            if (_targetRenderer != null)
            {
                _targetRenderer.enabled = true;
                _targetRenderer.SetPropertyBlock(_propertyBlock);
            }

            if (_hipLensRenderer != null)
            {
                _hipLensRenderer.enabled = false;
            }
        }

        private void ApplyTextureToRenderer(Renderer renderer, Texture texture)
        {
            if (renderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.Clear();
            _propertyBlock.SetTexture(BaseMapId, texture);
            _propertyBlock.SetTexture(MainTexId, texture);
            _propertyBlock.SetColor(BaseColorId, Color.white);
            _propertyBlock.SetColor(ColorId, Color.white);
            renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
