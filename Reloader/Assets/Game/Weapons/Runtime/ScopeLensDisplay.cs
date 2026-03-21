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

        private MaterialPropertyBlock _propertyBlock;
        private Material[] _originalSharedMaterials;
        private Material _runtimeDisplayMaterial;
        private bool _displayMaterialApplied;

        public Texture CurrentTexture { get; private set; }
        public Renderer TargetRenderer => _targetRenderer != null ? _targetRenderer : (_targetRenderer = GetComponent<Renderer>());
        public bool IsUsingProxySurface => false;

        private void Awake()
        {
            _targetRenderer ??= GetComponent<Renderer>();
        }

        private void OnDestroy()
        {
            RestoreOriginalMaterials();

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
                CurrentTexture = null;
                return true;
            }

            if (!EnsureDisplayMaterial())
            {
                return false;
            }

            ApplyTextureToMaterial(texture);
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

            _originalSharedMaterials = _targetRenderer.sharedMaterials;
            var runtimeShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (runtimeShader == null)
            {
                return false;
            }

            _runtimeDisplayMaterial ??= new Material(runtimeShader)
            {
                name = $"{_targetRenderer.sharedMaterial?.name ?? _targetRenderer.name}_RuntimeInstance"
            };

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

        private void ApplyTextureToMaterial(Texture texture)
        {
            if (_runtimeDisplayMaterial == null)
            {
                return;
            }

            _runtimeDisplayMaterial.SetTexture(BaseMapId, texture);
            _runtimeDisplayMaterial.SetTexture(MainTexId, texture);
            _runtimeDisplayMaterial.SetColor(BaseColorId, Color.white);
            _runtimeDisplayMaterial.SetColor(ColorId, Color.white);
        }
    }
}
