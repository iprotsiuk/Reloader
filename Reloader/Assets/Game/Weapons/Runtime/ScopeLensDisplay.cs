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

        private void Awake()
        {
            if (_targetRenderer == null)
            {
                _targetRenderer = GetComponent<Renderer>();
            }
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
            if (_targetRenderer == null)
            {
                _targetRenderer = GetComponent<Renderer>();
            }

            if (_targetRenderer == null)
            {
                return false;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            if (texture == null)
            {
                RestoreOriginalMaterials();
                _propertyBlock.Clear();
            }
            else
            {
                EnsureDisplayMaterial();
                _targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.Clear();
                _propertyBlock.SetTexture(BaseMapId, texture);
                _propertyBlock.SetTexture(MainTexId, texture);
                _propertyBlock.SetColor(BaseColorId, Color.white);
                _propertyBlock.SetColor(ColorId, Color.white);
            }

            _targetRenderer.SetPropertyBlock(_propertyBlock);
            CurrentTexture = texture;
            return true;
        }

        private void EnsureDisplayMaterial()
        {
            if (_displayMaterialApplied)
            {
                return;
            }

            _originalSharedMaterials = _targetRenderer.sharedMaterials;
            _runtimeDisplayMaterial ??= CreateDisplayMaterial();
            if (_runtimeDisplayMaterial == null)
            {
                return;
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

        private Material CreateDisplayMaterial()
        {
            if (_displayMaterialTemplate != null)
            {
                var material = new Material(_displayMaterialTemplate);
                material.name = $"{_displayMaterialTemplate.name}_ScopeDisplay";
                return material;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Standard");
            if (shader == null)
            {
                return null;
            }

            var runtimeMaterial = new Material(shader)
            {
                name = "RuntimeScopeDisplay"
            };
            if (runtimeMaterial.HasProperty(BaseColorId))
            {
                runtimeMaterial.SetColor(BaseColorId, Color.white);
            }

            if (runtimeMaterial.HasProperty(ColorId))
            {
                runtimeMaterial.SetColor(ColorId, Color.white);
            }

            return runtimeMaterial;
        }
    }
}
