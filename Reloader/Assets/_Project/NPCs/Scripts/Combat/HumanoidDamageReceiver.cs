using System;
using System.Reflection;
using Reloader.Weapons.Ballistics;
using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [DisallowMultipleComponent]
    public sealed class HumanoidDamageReceiver : MonoBehaviour, IDamageable
    {
        private const string PlayerDeviceControllerTypeName = "Reloader.PlayerDevice.World.PlayerDeviceController, Reloader.PlayerDevice";
        public const float DefaultMaxHealth = 100f;

        [SerializeField] private HumanoidHitboxRig _hitboxRig;
        [SerializeField] private HumanoidBodyZone _defaultZone = HumanoidBodyZone.Torso;
        [SerializeField] private float _maxHealth = DefaultMaxHealth;

        private static bool s_playerDeviceLookupAttempted;
        private static PropertyInfo s_playerDeviceActiveInstanceProperty;
        private static MethodInfo s_playerDeviceIngestImpactMethod;

        private bool _isDead;
        private float _currentHealth;

        public event Action ResultResolved;
        public event Action LethalResolved;
        public event Action Died;
        public event Action HealthStateChanged;

        public HumanoidBodyZone LastZone { get; private set; } = HumanoidBodyZone.Torso;
        public HumanoidImpactResolutionResult LastResult { get; private set; }
        public ProjectileImpactPayload LastPayload { get; private set; }
        public bool HasLastResult { get; private set; }
        public bool IsDead => _isDead;
        public HumanoidHitboxRig HitboxRig => _hitboxRig;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;

        private void Awake()
        {
            ResolveRig();
            ResetRuntime();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            ResolveRig();
        }

        public void Configure(HumanoidHitboxRig hitboxRig, HumanoidBodyZone defaultZone = HumanoidBodyZone.Torso)
        {
            _hitboxRig = hitboxRig;
            _defaultZone = defaultZone;
        }

        public void SetHealthStateForRuntime(float currentHealth, float maxHealth)
        {
            _maxHealth = Mathf.Max(0.01f, maxHealth);
            _currentHealth = Mathf.Clamp(currentHealth, 0f, _maxHealth);
            _isDead = _currentHealth <= 0f;
            ResetResolvedImpactState();
            HealthStateChanged?.Invoke();
        }

        public void ResetRuntime()
        {
            _isDead = false;
            _maxHealth = Mathf.Max(0.01f, _maxHealth);
            _currentHealth = _maxHealth;
            ResetResolvedImpactState();
            HealthStateChanged?.Invoke();
        }

        public void ApplyDamage(ProjectileImpactPayload payload)
        {
            ResolveRig();
            TryIngestImpact(payload);

            var zone = ResolveHitZone(payload.HitObject);
            var deliveredEnergyJoules = ResolveDeliveredEnergy(payload);
            var result = HumanoidImpactResolution.Resolve(zone, deliveredEnergyJoules);

            LastZone = zone;
            LastResult = result;
            LastPayload = payload;
            HasLastResult = true;

            ResultResolved?.Invoke();
            var healthStateChanged = false;
            if (!_isDead && !result.IsLethal)
            {
                var nextHealth = Mathf.Max(0f, _currentHealth - result.RecommendedHealthDamage);
                healthStateChanged = !Mathf.Approximately(nextHealth, _currentHealth);
                _currentHealth = nextHealth;
            }

            if (!ShouldEnterDeadState(result))
            {
                if (healthStateChanged)
                {
                    HealthStateChanged?.Invoke();
                }

                return;
            }

            LethalResolved?.Invoke();
            if (_isDead)
            {
                if (healthStateChanged)
                {
                    HealthStateChanged?.Invoke();
                }

                return;
            }

            _isDead = true;
            _currentHealth = 0f;
            Died?.Invoke();
            HealthStateChanged?.Invoke();
        }

        private bool ShouldEnterDeadState(HumanoidImpactResolutionResult result)
        {
            if (result.IsLethal)
            {
                return true;
            }

            return !_isDead && _currentHealth <= 0f;
        }

        private HumanoidBodyZone ResolveHitZone(GameObject hitObject)
        {
            if (hitObject == null)
            {
                return _defaultZone;
            }

            if (hitObject.TryGetComponent<BodyZoneHitbox>(out var directHitbox))
            {
                return directHitbox.BodyZone;
            }

            var parentHitbox = hitObject.GetComponentInParent<BodyZoneHitbox>();
            if (parentHitbox != null)
            {
                return parentHitbox.BodyZone;
            }

            if (_hitboxRig != null && _hitboxRig.TryResolveZone(hitObject.transform, out var zone))
            {
                return zone;
            }

            return _defaultZone;
        }

        private static float ResolveDeliveredEnergy(ProjectileImpactPayload payload)
        {
            var explicitEnergy = payload.DeliveredEnergyJoules;
            if (explicitEnergy > 0f)
            {
                return explicitEnergy;
            }

            if (payload.ImpactSpeedMetersPerSecond > 0f &&
                payload.ProjectileMassGrains > 0f)
            {
                return HumanoidImpactResolution.ComputeDeliveredEnergyJoules(
                    payload.ImpactSpeedMetersPerSecond,
                    payload.ProjectileMassGrains);
            }

            return Mathf.Max(0f, payload.Damage) * 100f;
        }

        private void ResolveRig()
        {
            if (_hitboxRig == null)
            {
                _hitboxRig = GetComponent<HumanoidHitboxRig>();
            }
        }

        private void ResetResolvedImpactState()
        {
            HasLastResult = false;
            LastZone = _defaultZone;
            LastResult = default;
            LastPayload = default;
        }

        private static void TryIngestImpact(ProjectileImpactPayload payload)
        {
            EnsurePlayerDeviceReflectionCache();
            if (s_playerDeviceActiveInstanceProperty == null || s_playerDeviceIngestImpactMethod == null)
            {
                return;
            }

            var activeInstance = s_playerDeviceActiveInstanceProperty.GetValue(null);
            if (activeInstance == null)
            {
                return;
            }

            s_playerDeviceIngestImpactMethod.Invoke(
                activeInstance,
                new object[] { payload.Point, payload.HitObject, payload.SourcePoint });
        }

        private static void EnsurePlayerDeviceReflectionCache()
        {
            if (s_playerDeviceLookupAttempted)
            {
                return;
            }

            s_playerDeviceLookupAttempted = true;
            var playerDeviceType = Type.GetType(PlayerDeviceControllerTypeName, throwOnError: false);
            if (playerDeviceType == null)
            {
                return;
            }

            s_playerDeviceActiveInstanceProperty = playerDeviceType.GetProperty(
                "ActiveInstance",
                BindingFlags.Public | BindingFlags.Static);
            s_playerDeviceIngestImpactMethod = playerDeviceType.GetMethod(
                "IngestImpact",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(Vector3), typeof(GameObject), typeof(Vector3?) },
                modifiers: null);
        }
    }
}
