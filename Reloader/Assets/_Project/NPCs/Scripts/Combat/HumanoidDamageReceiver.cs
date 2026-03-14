using System;
using Reloader.Weapons.Ballistics;
using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [DisallowMultipleComponent]
    public sealed class HumanoidDamageReceiver : MonoBehaviour, IDamageable
    {
        [SerializeField] private HumanoidHitboxRig _hitboxRig;
        [SerializeField] private HumanoidBodyZone _defaultZone = HumanoidBodyZone.Torso;

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
    }
}
