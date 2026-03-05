using UnityEngine;

namespace Reloader.Game.Weapons
{
    public sealed class ScopeReticleController : MonoBehaviour
    {
        [SerializeField] private Transform _reticleRoot;

        public ScopeReticleDefinition CurrentReticleDefinition { get; private set; }
        public float CurrentScale { get; private set; } = 1f;

        private void Awake()
        {
            if (_reticleRoot == null)
            {
                _reticleRoot = transform;
            }
        }

        public void ApplyReticle(ScopeReticleDefinition reticleDefinition, float magnification)
        {
            if (_reticleRoot == null)
            {
                _reticleRoot = transform;
            }

            CurrentReticleDefinition = reticleDefinition;
            CurrentScale = ResolveScale(reticleDefinition, magnification);
            _reticleRoot.localScale = Vector3.one * CurrentScale;
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
    }
}
