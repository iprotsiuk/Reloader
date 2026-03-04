using Reloader.Player;
using UnityEngine;

namespace Reloader.Audio
{
    public sealed class FootstepAudioRouter : MonoBehaviour
    {
        private const string RuntimeRouterObjectName = "RuntimeFootstepAudioRouter";

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
        [SerializeField] private PlayerMover _playerMover;
        [SerializeField] private string _surfaceId = "Default";
        [SerializeField, Min(0.1f)] private float _metersPerStep = 1.6f;
        [SerializeField, Min(0f)] private float _minimumSpeed = 0.1f;
        [SerializeField] private LayerMask _surfaceMask = ~0;
        [SerializeField] private TagSurfaceMapping[] _tagMappings = System.Array.Empty<TagSurfaceMapping>();
        [SerializeField] private MaterialSurfaceMapping[] _materialMappings = System.Array.Empty<MaterialSurfaceMapping>();
        [SerializeField, Range(0f, 1f)] private float _volume = 0.6f;

        private float _distanceAccumulator;
        private PlayerMover _subscribedMover;

        public System.Action<string, AudioClip, Vector3> ClipPlayed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapRuntimeRouter()
        {
            ResolveOrCreateRuntimeRouter();
        }

        public static FootstepAudioRouter ResolveOrCreateRuntimeRouter()
        {
            var existing = FindFirstObjectByType<FootstepAudioRouter>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(RuntimeRouterObjectName);
            DontDestroyOnLoad(go);
            return go.AddComponent<FootstepAudioRouter>();
        }

        private void OnEnable()
        {
            TryBindPlayerMover(forceRebind: true);
        }

        private void OnDisable()
        {
            UnbindPlayerMover();
        }

        private void Update()
        {
            if (_subscribedMover == null || !_subscribedMover.isActiveAndEnabled)
            {
                TryBindPlayerMover(forceRebind: true);
            }
        }

        private void HandleLocomotionFrame(Vector3 worldPosition, Vector3 horizontalVelocity, bool isGrounded, float deltaTime)
        {
            _catalog = CombatAudioCatalogResolver.Resolve(_catalog);
            if (_catalog == null || !isGrounded || deltaTime <= 0f)
            {
                _distanceAccumulator = 0f;
                return;
            }

            var speed = horizontalVelocity.magnitude;
            if (speed < _minimumSpeed)
            {
                _distanceAccumulator = 0f;
                return;
            }

            _distanceAccumulator += speed * deltaTime;
            var stepDistance = Mathf.Max(0.1f, _metersPerStep);
            if (_distanceAccumulator < stepDistance)
            {
                return;
            }

            _distanceAccumulator -= stepDistance;
            var surface = ResolveSurfaceId(worldPosition);
            var clip = _catalog.GetRandomFootstepClip(surface);
            if (clip == null)
            {
                return;
            }

            ClipPlayed?.Invoke(surface, clip, worldPosition);
            AudioSource.PlayClipAtPoint(clip, worldPosition, Mathf.Clamp01(_volume));
        }

        private string ResolveSurfaceId(Vector3 worldPosition)
        {
            var rayOrigin = worldPosition + (Vector3.up * 0.2f);
            if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, 2f, _surfaceMask, QueryTriggerInteraction.Ignore))
            {
                var surfaceFromMaterial = ResolveSurfaceByMaterial(hit.collider != null ? hit.collider.sharedMaterial : null);
                if (!string.IsNullOrWhiteSpace(surfaceFromMaterial))
                {
                    return surfaceFromMaterial;
                }

                var surfaceFromTag = ResolveSurfaceByTag(hit.collider != null ? hit.collider.tag : string.Empty);
                if (!string.IsNullOrWhiteSpace(surfaceFromTag))
                {
                    return surfaceFromTag;
                }
            }

            return string.IsNullOrWhiteSpace(_surfaceId) ? "Default" : _surfaceId;
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

        private void TryBindPlayerMover(bool forceRebind)
        {
            if (!forceRebind && _subscribedMover != null)
            {
                return;
            }

            var resolved = _playerMover != null ? _playerMover : GetComponentInParent<PlayerMover>();
            if (resolved == null)
            {
                resolved = FindFirstObjectByType<PlayerMover>(FindObjectsInactive.Include);
            }

            if (ReferenceEquals(_subscribedMover, resolved))
            {
                return;
            }

            UnbindPlayerMover();

            _playerMover = resolved;
            _subscribedMover = resolved;
            if (_subscribedMover != null)
            {
                _subscribedMover.LocomotionFramePublished += HandleLocomotionFrame;
            }
        }

        private void UnbindPlayerMover()
        {
            if (_subscribedMover != null)
            {
                _subscribedMover.LocomotionFramePublished -= HandleLocomotionFrame;
                _subscribedMover = null;
            }
        }
    }
}
