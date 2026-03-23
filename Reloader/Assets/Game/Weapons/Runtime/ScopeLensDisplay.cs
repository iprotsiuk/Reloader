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

        private MaterialPropertyBlock _propertyBlock;
        private Material[] _originalSharedMaterials;
        private Material _runtimeDisplayMaterial;
        private bool _displayMaterialApplied;
        private bool _capturedOriginalRendererEnabled;
        private bool _originalRendererEnabled;

        public Texture CurrentTexture { get; private set; }
        public Renderer TargetRenderer => _targetRenderer != null ? _targetRenderer : (_targetRenderer = GetComponent<Renderer>());
        public Renderer ApertureRenderer => _apertureRenderer;
        public bool IsUsingProxySurface => false;

        private void Awake()
        {
            _targetRenderer ??= GetComponent<Renderer>();
            CaptureOriginalRendererState();
            if (_targetRenderer != null)
            {
                _targetRenderer.enabled = false;
            }
        }

        private void OnDestroy()
        {
            RestoreOriginalMaterials();
            RestoreOriginalRendererState();

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
                RestoreOriginalMaterials();
                _propertyBlock.Clear();
                _targetRenderer.SetPropertyBlock(_propertyBlock);
                _targetRenderer.enabled = false;
                CurrentTexture = null;
                return true;
            }

            if (!EnsureDisplayMaterial())
            {
                return false;
            }

            ApplyTextureToRenderer(_targetRenderer, texture);
            _targetRenderer.enabled = true;
            _targetRenderer.SetPropertyBlock(_propertyBlock);
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

            CaptureOriginalRendererState();
            _originalSharedMaterials = _targetRenderer.sharedMaterials;
            _runtimeDisplayMaterial ??= CreateDisplayMaterial(ResolveSourceMaterial());
            if (_runtimeDisplayMaterial == null)
            {
                return false;
            }

            var materialCount = _originalSharedMaterials != null && _originalSharedMaterials.Length > 0
                ? _originalSharedMaterials.Length
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

            if (_originalSharedMaterials != null)
            {
                for (var i = 0; i < _originalSharedMaterials.Length; i++)
                {
                    if (_originalSharedMaterials[i] != null)
                    {
                        return _originalSharedMaterials[i];
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

        private void RestoreOriginalMaterials()
        {
            if (!_displayMaterialApplied || _targetRenderer == null)
            {
                return;
            }

            if (_originalSharedMaterials != null && _originalSharedMaterials.Length > 0)
            {
                _targetRenderer.sharedMaterials = _originalSharedMaterials;
            }

            _displayMaterialApplied = false;
        }

        private void CaptureOriginalRendererState()
        {
            if (_capturedOriginalRendererEnabled || _targetRenderer == null)
            {
                return;
            }

            _originalRendererEnabled = _targetRenderer.enabled;
            _capturedOriginalRendererEnabled = true;
        }

        private void RestoreOriginalRendererState()
        {
            if (!_capturedOriginalRendererEnabled || _targetRenderer == null)
            {
                return;
            }

            _targetRenderer.enabled = _originalRendererEnabled;
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
