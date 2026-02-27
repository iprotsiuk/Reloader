using Reloader.Core.Events;
using UnityEngine;

namespace Reloader.Weapons.Ballistics
{
    public sealed class WeaponProjectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 100f;
        [SerializeField] private float _gravityMultiplier = 1f;
        [SerializeField] private float _damage = 20f;
        [SerializeField] private float _lifetimeSeconds = 5f;
        [SerializeField] private LayerMask _hitMask = ~0;

        private Vector3 _velocity;
        private float _remainingLifetime;
        private string _itemId;

        private void Awake()
        {
            _remainingLifetime = _lifetimeSeconds;
            _velocity = transform.forward * _speed;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            _remainingLifetime -= dt;
            if (_remainingLifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            _velocity += Physics.gravity * (_gravityMultiplier * dt);
            var start = transform.position;
            var next = start + (_velocity * dt);
            var delta = next - start;

            if (Physics.Raycast(start, delta.normalized, out var hit, delta.magnitude, _hitMask, QueryTriggerInteraction.Collide))
            {
                transform.position = hit.point;
                var payload = new ProjectileImpactPayload(_itemId, hit.point, hit.normal, _damage, hit.collider.gameObject);
                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                damageable?.ApplyDamage(payload);
                GameEvents.RaiseProjectileHit(_itemId, hit.point, _damage);
                Destroy(gameObject);
                return;
            }

            transform.position = next;
            if (_velocity.sqrMagnitude > 0.0001f)
            {
                transform.forward = _velocity.normalized;
            }
        }

        public void Initialize(string itemId, Vector3 direction, float speed, float gravityMultiplier, float damage, float lifetimeSeconds)
        {
            _itemId = itemId;
            _speed = speed;
            _gravityMultiplier = gravityMultiplier;
            _damage = damage;
            _lifetimeSeconds = lifetimeSeconds;
            _remainingLifetime = lifetimeSeconds;
            _velocity = direction.normalized * speed;
            if (_velocity.sqrMagnitude > 0.0001f)
            {
                transform.forward = _velocity.normalized;
            }
        }
    }
}
