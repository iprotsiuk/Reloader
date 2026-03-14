using Reloader.Weapons.Ballistics;
using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [DisallowMultipleComponent]
    public sealed class BodyZoneHitbox : MonoBehaviour, IDamageable
    {
        [SerializeField] private HumanoidBodyZone _bodyZone = HumanoidBodyZone.Torso;
        [SerializeField] private HumanoidHitboxRig _ownerRig;

        public HumanoidBodyZone BodyZone => _bodyZone;
        public HumanoidHitboxRig OwnerRig => _ownerRig;

        private void Reset()
        {
            ResolveOwnerRig();
        }

        private void Awake()
        {
            ResolveOwnerRig();
        }

        private void OnEnable()
        {
            ResolveOwnerRig();
            _ownerRig?.RegisterHitbox(this);
        }

        private void OnDisable()
        {
            _ownerRig?.UnregisterHitbox(this);
        }

        public void Configure(HumanoidBodyZone bodyZone)
        {
            var previousRig = _ownerRig;
            var previousZone = _bodyZone;
            _bodyZone = bodyZone;
            ResolveOwnerRig();
            RebindRegistration(previousRig, previousZone);
        }

        public void Configure(HumanoidHitboxRig ownerRig, HumanoidBodyZone bodyZone)
        {
            var previousRig = _ownerRig;
            var previousZone = _bodyZone;
            _ownerRig = ownerRig;
            _bodyZone = bodyZone;
            ResolveOwnerRig();
            RebindRegistration(previousRig, previousZone);
        }

        public void ApplyDamage(ProjectileImpactPayload payload)
        {
            var receiver = ResolveDamageReceiver();
            if (receiver != null)
            {
                receiver.ApplyDamage(payload);
                return;
            }

            ForwardToFallbackDamageable(payload);
        }

        private HumanoidDamageReceiver ResolveDamageReceiver()
        {
            ResolveOwnerRig();
            if (_ownerRig != null && _ownerRig.TryGetDamageReceiver(out var receiverFromRig))
            {
                return receiverFromRig;
            }

            return GetComponentInParent<HumanoidDamageReceiver>();
        }

        private void ResolveOwnerRig()
        {
            if (_ownerRig != null)
            {
                return;
            }

            _ownerRig = GetComponentInParent<HumanoidHitboxRig>();
        }

        private void ForwardToFallbackDamageable(ProjectileImpactPayload payload)
        {
            var behaviours = GetComponentsInParent<MonoBehaviour>(includeInactive: true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || ReferenceEquals(behaviour, this))
                {
                    continue;
                }

                if (behaviour is IDamageable damageable)
                {
                    damageable.ApplyDamage(payload);
                    return;
                }
            }
        }

        private void RebindRegistration(HumanoidHitboxRig previousRig, HumanoidBodyZone previousZone)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (previousRig != null)
            {
                previousRig.UnregisterHitbox(this, previousZone);
            }

            _ownerRig?.RegisterHitbox(this);
        }
    }
}
