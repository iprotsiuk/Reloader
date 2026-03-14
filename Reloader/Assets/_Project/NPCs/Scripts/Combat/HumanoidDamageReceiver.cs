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

        [SerializeField] private HumanoidHitboxRig _hitboxRig;
        [SerializeField] private HumanoidBodyZone _defaultZone = HumanoidBodyZone.Torso;

        private static bool s_playerDeviceLookupAttempted;
        private static PropertyInfo s_playerDeviceActiveInstanceProperty;
        private static MethodInfo s_playerDeviceIngestImpactMethod;

        private bool _isDead;

        public event Action ResultResolved;
        public event Action LethalResolved;
        public event Action Died;

        public HumanoidBodyZone LastZone { get; private set; } = HumanoidBodyZone.Torso;
        public HumanoidImpactResolutionResult LastResult { get; private set; }
        public ProjectileImpactPayload LastPayload { get; private set; }
        public bool HasLastResult { get; private set; }
        public bool IsDead => _isDead;
        public HumanoidHitboxRig HitboxRig => _hitboxRig;

        private void Awake()
        {
            ResolveRig();
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

        public void ResetRuntime()
        {
            _isDead = false;
            HasLastResult = false;
            LastZone = _defaultZone;
            LastResult = default;
            LastPayload = default;
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
            if (!result.IsLethal)
            {
                return;
            }

            LethalResolved?.Invoke();
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            Died?.Invoke();
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
            if (explicitEnergy <= 0f &&
                payload.ImpactSpeedMetersPerSecond > 0f &&
                payload.ProjectileMassGrains > 0f)
            {
                explicitEnergy = HumanoidImpactResolution.ComputeDeliveredEnergyJoules(
                    payload.ImpactSpeedMetersPerSecond,
                    payload.ProjectileMassGrains);
            }

            var damageDerivedEnergy = Mathf.Max(0f, payload.Damage) * 100f;
            return Mathf.Max(explicitEnergy, damageDerivedEnergy);
        }

        private void ResolveRig()
        {
            if (_hitboxRig == null)
            {
                _hitboxRig = GetComponent<HumanoidHitboxRig>();
            }
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
