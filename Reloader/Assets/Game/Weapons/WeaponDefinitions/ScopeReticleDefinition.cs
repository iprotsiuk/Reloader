using UnityEngine;

namespace Reloader.Game.Weapons
{
    public enum ScopeReticleMode
    {
        Ffp = 0,
        Sfp = 1
    }

    [CreateAssetMenu(fileName = "ScopeReticleDefinition", menuName = "Reloader/Game/Weapons/Scope Reticle Definition")]
    public sealed class ScopeReticleDefinition : ScriptableObject
    {
        [SerializeField] private ScopeReticleMode _mode;
        [SerializeField, Min(1f)] private float _referenceMagnification = 4f;
        [SerializeField] private Sprite _reticleSprite;

        public ScopeReticleMode Mode => _mode;
        public float ReferenceMagnification => Mathf.Max(1f, _referenceMagnification);
        public Sprite ReticleSprite => _reticleSprite;
    }
}
