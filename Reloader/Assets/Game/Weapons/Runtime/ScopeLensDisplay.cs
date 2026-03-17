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

            EnsureDisplayMaterial();
            ApplyTextureToRenderer(_targetRenderer, texture);
            _targetRenderer.enabled = true;
            _targetRenderer.SetPropertyBlock(_propertyBlock);
            CurrentTexture = texture;
            return true;
        }

        private void EnsureDisplayMaterial()
        {
            if (_displayMaterialApplied || _targetRenderer == null)
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

        private void ApplyTextureToRenderer(Renderer renderer, Texture texture)
        {
            if (renderer == null)
            {
                return;
            }

            _propertyBlock.Clear();
            _propertyBlock.SetTexture(BaseMapId, texture);
            _propertyBlock.SetTexture(MainTexId, texture);
            _propertyBlock.SetColor(BaseColorId, Color.white);
            _propertyBlock.SetColor(ColorId, Color.white);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private Material CreateDisplayMaterial()
        {
            if (_displayMaterialTemplate != null)
            {
                return new Material(_displayMaterialTemplate)
                {
                    name = $"{_displayMaterialTemplate.name}_RuntimeInstance"
                };
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Standard");
            if (shader == null)
            {
                return null;
            }

            return new Material(shader)
            {
                name = "ScopeLensDisplay_RuntimeMaterial"
            };
        }
    }
}
