using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [DisallowMultipleComponent]
    public sealed class HumanoidBloodController : MonoBehaviour
    {
        [SerializeField] private HumanoidDamageReceiver _damageReceiver;
        [SerializeField] private BloodVfxCatalog _catalog;
        [SerializeField] private Transform _spawnParent;

        private bool _deathPuddleSpawned;

        public bool HasRequestedEffect { get; private set; }
        public BloodEffectKind LastRequestedEffectKind { get; private set; }
        public GameObject LastSpawnedEffect { get; private set; }
        public int DeathPuddleSpawnCount { get; private set; }

        private void Reset()
        {
            ResolveDependencies();
        }

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void SetCatalogForTests(BloodVfxCatalog catalog)
        {
            ConfigureCatalog(catalog);
        }

        public void ConfigureCatalog(BloodVfxCatalog catalog)
        {
            _catalog = catalog;
        }

        public void ResetRuntime()
        {
            _deathPuddleSpawned = false;
            HasRequestedEffect = false;
            LastRequestedEffectKind = default;
            LastSpawnedEffect = null;
            DeathPuddleSpawnCount = 0;
        }

        private void ResolveDependencies()
        {
            _damageReceiver ??= GetComponent<HumanoidDamageReceiver>();
        }

        private void Subscribe()
        {
            if (_damageReceiver == null)
            {
                return;
            }

            Unsubscribe();
            _damageReceiver.ResultResolved += HandleResultResolved;
            _damageReceiver.Died += HandleDied;
        }

        private void Unsubscribe()
        {
            if (_damageReceiver == null)
            {
                return;
            }

            _damageReceiver.ResultResolved -= HandleResultResolved;
            _damageReceiver.Died -= HandleDied;
        }

        private void HandleResultResolved()
        {
            if (_damageReceiver == null || !_damageReceiver.HasLastResult || _deathPuddleSpawned)
            {
                return;
            }

            var kind = ResolveImpactKind(_damageReceiver.LastZone, _damageReceiver.LastResult);
            Spawn(kind, _damageReceiver.LastPayload.Point, _damageReceiver.LastPayload.Normal);
        }

        private void HandleDied()
        {
            if (_damageReceiver == null || _deathPuddleSpawned)
            {
                return;
            }

            _deathPuddleSpawned = true;
            DeathPuddleSpawnCount++;
            var point = _damageReceiver.HasLastResult ? _damageReceiver.LastPayload.Point : transform.position;
            var normal = _damageReceiver.HasLastResult ? _damageReceiver.LastPayload.Normal : Vector3.up;
            Spawn(BloodEffectKind.DeathPuddle, point, normal);
        }

        private static BloodEffectKind ResolveImpactKind(HumanoidBodyZone zone, HumanoidImpactResolutionResult result)
        {
            if (zone == HumanoidBodyZone.Neck)
            {
                return BloodEffectKind.NeckImpact;
            }

            return result.Severity >= HumanoidImpactSeverity.Serious
                ? BloodEffectKind.HeavyImpact
                : BloodEffectKind.LightImpact;
        }

        private void Spawn(BloodEffectKind kind, Vector3 point, Vector3 normal)
        {
            HasRequestedEffect = true;
            LastRequestedEffectKind = kind;

            if (_catalog == null || !_catalog.TryGetPrefab(kind, out var prefab) || prefab == null)
            {
                return;
            }

            var rotation = ResolveRotation(normal);
            var instance = _spawnParent != null
                ? Instantiate(prefab, point, rotation, _spawnParent)
                : Instantiate(prefab, point, rotation);
            instance.SetActive(true);
            LastSpawnedEffect = instance;
        }

        private static Quaternion ResolveRotation(Vector3 normal)
        {
            return normal.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(normal.normalized, Vector3.up)
                : Quaternion.identity;
        }
    }
}
