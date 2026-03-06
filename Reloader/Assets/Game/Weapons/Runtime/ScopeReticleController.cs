using UnityEngine;

namespace Reloader.Game.Weapons
{
    public sealed class ScopeReticleController : MonoBehaviour
    {
        [SerializeField] private Transform _reticleRoot;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public ScopeReticleDefinition CurrentReticleDefinition { get; private set; }
        public float CurrentScale { get; private set; } = 1f;

        private void Awake()
        {
            if (_reticleRoot == null)
            {
                _reticleRoot = transform;
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public void ApplyReticle(ScopeReticleDefinition reticleDefinition, float magnification)
        {
            if (_reticleRoot == null)
            {
                _reticleRoot = transform;
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            CurrentReticleDefinition = reticleDefinition;
            CurrentScale = ResolveScale(reticleDefinition, magnification);
            _reticleRoot.localScale = Vector3.one * CurrentScale;
            ApplySprite(reticleDefinition);
        }

        public void Clear()
        {
            CurrentReticleDefinition = null;
            CurrentScale = 1f;
            if (_reticleRoot == null)
            {
                _reticleRoot = transform;
            }

            _reticleRoot.localScale = Vector3.one;
            ApplySprite(null);
        }

        private static float ResolveScale(ScopeReticleDefinition reticleDefinition, float magnification)
        {
            if (reticleDefinition == null)
            {
                return 1f;
            }

            if (reticleDefinition.Mode == ScopeReticleMode.Sfp)
            {
                return 1f;
            }

            return Mathf.Max(0.01f, Mathf.Max(1f, magnification) / reticleDefinition.ReferenceMagnification);
        }

        private void ApplySprite(ScopeReticleDefinition reticleDefinition)
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            var sprite = reticleDefinition != null ? reticleDefinition.ReticleSprite : null;
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.enabled = sprite != null;
        }
    }
}
