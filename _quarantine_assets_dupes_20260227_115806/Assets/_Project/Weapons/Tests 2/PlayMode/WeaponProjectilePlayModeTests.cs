using System.Collections;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Weapons.Ballistics;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class WeaponProjectilePlayModeTests
    {
        [UnityTest]
        public IEnumerator Projectile_AppliesDamageAndRaisesHitEvent_OnCollision()
        {
            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = Vector3.zero;
            projectileGo.transform.forward = Vector3.forward;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = new Vector3(0f, 0f, 5f);
            target.transform.localScale = new Vector3(1f, 1f, 1f);
            var receiver = target.AddComponent<TestDamageable>();

            string hitItemId = null;
            float hitDamage = 0f;
            GameEvents.OnProjectileHit += OnProjectileHit;
            void OnProjectileHit(string itemId, Vector3 _, float damage)
            {
                hitItemId = itemId;
                hitDamage = damage;
            }

            projectile.Initialize("weapon-rifle-01", Vector3.forward, speed: 120f, gravityMultiplier: 0f, damage: 33f, lifetimeSeconds: 3f);

            var elapsed = 0f;
            while (receiver.HitCount == 0 && elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            GameEvents.OnProjectileHit -= OnProjectileHit;

            Assert.That(receiver.HitCount, Is.EqualTo(1));
            Assert.That(receiver.LastDamage, Is.EqualTo(33f));
            Assert.That(hitItemId, Is.EqualTo("weapon-rifle-01"));
            Assert.That(hitDamage, Is.EqualTo(33f));

            Object.Destroy(projectileGo);
            Object.Destroy(target);
        }

        [UnityTest]
        public IEnumerator Projectile_MovesAndDrops_WithGravity()
        {
            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = new Vector3(0f, 3f, 0f);
            projectileGo.transform.forward = Vector3.forward;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();
            projectile.Initialize("weapon-rifle-01", Vector3.forward, speed: 15f, gravityMultiplier: 1f, damage: 10f, lifetimeSeconds: 2f);

            var start = projectileGo.transform.position;
            yield return null;
            yield return null;
            var end = projectileGo.transform.position;

            Assert.That(end.z, Is.GreaterThan(start.z));
            Assert.That(end.y, Is.LessThan(start.y));

            Object.Destroy(projectileGo);
        }

        [UnityTest]
        public IEnumerator Projectile_Despawned_AfterLifetimeExpires()
        {
            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = Vector3.zero;
            projectileGo.transform.forward = Vector3.forward;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();
            projectile.Initialize("weapon-rifle-01", Vector3.forward, speed: 5f, gravityMultiplier: 0f, damage: 1f, lifetimeSeconds: 0.05f);

            yield return new WaitForSeconds(0.1f);

            Assert.That(projectile == null || projectile.Equals(null), Is.True);
        }

        private sealed class TestDamageable : MonoBehaviour, IDamageable
        {
            public int HitCount { get; private set; }
            public float LastDamage { get; private set; }

            public void ApplyDamage(ProjectileImpactPayload payload)
            {
                HitCount++;
                LastDamage = payload.Damage;
            }
        }
    }
}
