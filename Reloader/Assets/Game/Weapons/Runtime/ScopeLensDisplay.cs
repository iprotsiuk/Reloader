using UnityEngine;

namespace Reloader.Game.Weapons
{
    public sealed class ScopeLensDisplay : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        [SerializeField] private Renderer _targetRenderer;

        private MaterialPropertyBlock _propertyBlock;

        public Texture CurrentTexture { get; private set; }

        private void Awake()
        {
            if (_targetRenderer == null)
            {
                _targetRenderer = GetComponent<Renderer>();
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
            _targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetTexture(BaseMapId, texture);
            _propertyBlock.SetTexture(MainTexId, texture);
            _targetRenderer.SetPropertyBlock(_propertyBlock);
            CurrentTexture = texture;
            return true;
        }
    }
}
