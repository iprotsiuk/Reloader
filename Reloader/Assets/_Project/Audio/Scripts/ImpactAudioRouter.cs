using UnityEngine;

namespace Reloader.Audio
{
    public sealed class ImpactAudioRouter : MonoBehaviour
    {
        private const string RuntimeRouterObjectName = "RuntimeImpactAudioRouter";

        [System.Serializable]
        private sealed class TagSurfaceMapping
        {
            [SerializeField] private string _tag = string.Empty;
            [SerializeField] private string _surfaceId = "Default";

            public string Tag => _tag;
            public string SurfaceId => _surfaceId;
        }

        [System.Serializable]
        private sealed class MaterialSurfaceMapping
        {
            [SerializeField] private PhysicsMaterial _material;
            [SerializeField] private string _surfaceId = "Default";

            public PhysicsMaterial Material => _material;
            public string SurfaceId => _surfaceId;
        }

        [SerializeField] private CombatAudioCatalog _catalog;
        [SerializeField] private string _defaultSurfaceId = "Default";
        [SerializeField] private TagSurfaceMapping[] _tagMappings = System.Array.Empty<TagSurfaceMapping>();
        [SerializeField] private MaterialSurfaceMapping[] _materialMappings = System.Array.Empty<MaterialSurfaceMapping>();
        [SerializeField, Range(0f, 1f)] private float _volume = 0.7f;

        public System.Action<string, AudioClip, Vector3> ClipPlayed;

        public static ImpactAudioRouter ResolveOrCreateRuntimeRouter()
        {
            var existing = FindFirstObjectByType<ImpactAudioRouter>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(RuntimeRouterObjectName);
            DontDestroyOnLoad(go);
            return go.AddComponent<ImpactAudioRouter>();
        }

        public void EmitImpact(Vector3 position, Collider hitCollider)
        {
            _catalog = CombatAudioCatalogResolver.Resolve(_catalog);
            if (_catalog == null)
            {
                return;
            }

            var surfaceId = ResolveSurfaceId(hitCollider);
            var clip = _catalog.GetRandomImpactClip(surfaceId);
            if (clip == null)
            {
                return;
            }

            ClipPlayed?.Invoke(surfaceId, clip, position);
            AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(_volume));
        }

        private string ResolveSurfaceId(Collider hitCollider)
        {
            var materialSurface = ResolveSurfaceByMaterial(hitCollider != null ? hitCollider.sharedMaterial : null);
            if (!string.IsNullOrWhiteSpace(materialSurface))
            {
                return materialSurface;
            }

            var tagSurface = ResolveSurfaceByTag(hitCollider != null ? hitCollider.tag : string.Empty);
            if (!string.IsNullOrWhiteSpace(tagSurface))
            {
                return tagSurface;
            }

            return string.IsNullOrWhiteSpace(_defaultSurfaceId) ? "Default" : _defaultSurfaceId;
        }

        private string ResolveSurfaceByTag(string tag)
        {
            for (var i = 0; i < _tagMappings.Length; i++)
            {
                var mapping = _tagMappings[i];
                if (mapping == null)
                {
                    continue;
                }

                if (string.Equals(mapping.Tag, tag, System.StringComparison.OrdinalIgnoreCase))
                {
                    return mapping.SurfaceId;
                }
            }

            return null;
        }

        private string ResolveSurfaceByMaterial(PhysicsMaterial material)
        {
            if (material == null)
            {
                return null;
            }

            for (var i = 0; i < _materialMappings.Length; i++)
            {
                var mapping = _materialMappings[i];
                if (mapping == null || mapping.Material != material)
                {
                    continue;
                }

                return mapping.SurfaceId;
            }

            return null;
        }
    }
}
