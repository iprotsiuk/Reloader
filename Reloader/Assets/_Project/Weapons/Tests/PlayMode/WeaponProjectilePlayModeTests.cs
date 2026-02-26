using System.Collections;
using NUnit.Framework;
using Reloader.Core.Runtime;
using Reloader.Weapons.Ballistics;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class WeaponProjectilePlayModeTests
    {
        private IGameEventsRuntimeHub _runtimeEventsBeforeEachTest;

        [SetUp]
        public void SetUp()
        {
            _runtimeEventsBeforeEachTest = RuntimeKernelBootstrapper.Events;
        }

        [TearDown]
        public void TearDown()
        {
            RuntimeKernelBootstrapper.Events = _runtimeEventsBeforeEachTest;
        }

        [UnityTest]
        public IEnumerator Configure_InjectedWeaponEvents_RaisesProjectileHitThroughInjectedPortOnly()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var fallbackRuntimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = fallbackRuntimeEvents;

            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = Vector3.zero;
            projectileGo.transform.forward = Vector3.forward;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = new Vector3(0f, 0f, 5f);
            target.transform.localScale = new Vector3(1f, 1f, 1f);
            var receiver = target.AddComponent<TestDamageable>();

            var injectedEvents = new DefaultRuntimeEvents();
            var injectedHitRaised = 0;
            injectedEvents.OnProjectileHit += (_, _, _) => injectedHitRaised++;

            var fallbackHitRaised = 0;
            fallbackRuntimeEvents.OnProjectileHit += HandleFallbackProjectileHit;
            void HandleFallbackProjectileHit(string _, Vector3 __, float ___) => fallbackHitRaised++;

            projectile.Configure(injectedEvents);
            projectile.Initialize("weapon-rifle-01", Vector3.forward, speed: 120f, gravityMultiplier: 0f, damage: 33f, lifetimeSeconds: 3f);

            var elapsed = 0f;
            while (receiver.HitCount == 0 && elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            fallbackRuntimeEvents.OnProjectileHit -= HandleFallbackProjectileHit;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;

            Assert.That(receiver.HitCount, Is.EqualTo(1));
            Assert.That(injectedHitRaised, Is.EqualTo(1));
            Assert.That(fallbackHitRaised, Is.EqualTo(0));

            Object.Destroy(projectileGo);
            Object.Destroy(target);
        }

        [UnityTest]
        public IEnumerator Configure_WithoutInjectedWeaponEvents_UsesCurrentRuntimeHubAfterSwap()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var initialRuntimeEvents = new DefaultRuntimeEvents();
            var replacementRuntimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = initialRuntimeEvents;

            var initialHits = 0;
            var replacementHits = 0;
            initialRuntimeEvents.OnProjectileHit += (_, _, _) => initialHits++;
            replacementRuntimeEvents.OnProjectileHit += (_, _, _) => replacementHits++;

            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = Vector3.zero;
            projectileGo.transform.forward = Vector3.forward;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = new Vector3(0f, 0f, 5f);
            target.transform.localScale = new Vector3(1f, 1f, 1f);
            target.AddComponent<TestDamageable>();

            projectile.Configure();
            projectile.Initialize("weapon-rifle-01", Vector3.forward, speed: 120f, gravityMultiplier: 0f, damage: 33f, lifetimeSeconds: 3f);

            RuntimeKernelBootstrapper.Events = replacementRuntimeEvents;
            yield return null;

            var elapsed = 0f;
            while (replacementHits == 0 && elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;

            Assert.That(initialHits, Is.EqualTo(0));
            Assert.That(replacementHits, Is.EqualTo(1));

            Object.Destroy(projectileGo);
            Object.Destroy(target);
        }

        [UnityTest]
        public IEnumerator Projectile_AppliesDamageAndRaisesHitEvent_OnCollision()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

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
            runtimeEvents.OnProjectileHit += OnProjectileHit;
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

            runtimeEvents.OnProjectileHit -= OnProjectileHit;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;

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

        [UnityTest]
        public IEnumerator Projectile_IgnoresShooterColliders_AndHitsTarget()
        {
            var shooter = new GameObject("Shooter");
            var shooterCollider = shooter.AddComponent<CapsuleCollider>();
            shooterCollider.center = new Vector3(0f, 1f, 0f);
            shooterCollider.height = 2f;
            shooterCollider.radius = 0.35f;

            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = new Vector3(0f, 1f, 0f);
            projectileGo.transform.forward = Vector3.forward;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = new Vector3(0f, 1f, 4f);
            var receiver = target.AddComponent<TestDamageable>();

            projectile.Initialize("weapon-rifle-01", Vector3.forward, speed: 120f, gravityMultiplier: 0f, damage: 22f, lifetimeSeconds: 2f, shooterRoot: shooter.transform);

            var elapsed = 0f;
            while (receiver.HitCount == 0 && elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(receiver.HitCount, Is.EqualTo(1));
            Assert.That(receiver.LastDamage, Is.EqualTo(22f));

            Object.Destroy(shooter);
            Object.Destroy(projectileGo);
            Object.Destroy(target);
        }

        [UnityTest]
        public IEnumerator Projectile_SpawnsImpactVfx_OnAnySurfaceCollision()
        {
            var baselineParticles = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length;

            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = new Vector3(0f, 1f, 0f);
            projectileGo.transform.forward = Vector3.forward;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 1f, 3f);
            wall.transform.localScale = new Vector3(2f, 2f, 0.25f);

            projectile.Initialize("weapon-rifle-01", Vector3.forward, speed: 90f, gravityMultiplier: 0f, damage: 10f, lifetimeSeconds: 2f);

            var elapsed = 0f;
            var hasExtraParticleSystem = false;
            while (!hasExtraParticleSystem && elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                hasExtraParticleSystem = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length > baselineParticles;
                yield return null;
            }

            Assert.That(hasExtraParticleSystem, Is.True);

            Object.Destroy(projectileGo);
            Object.Destroy(wall);
        }

        [UnityTest]
        public IEnumerator Projectile_WithHigherBc_RetainsVelocityLonger()
        {
            var lowGo = new GameObject("LowBcProjectile");
            var low = lowGo.AddComponent<WeaponProjectile>();
            lowGo.transform.position = Vector3.zero;
            lowGo.transform.forward = Vector3.forward;
            low.Initialize("weapon-rifle-01", Vector3.forward, speed: 220f, gravityMultiplier: 0f, damage: 10f, lifetimeSeconds: 2f, ballisticCoefficientG1: 0.2f);

            var highGo = new GameObject("HighBcProjectile");
            var high = highGo.AddComponent<WeaponProjectile>();
            highGo.transform.position = new Vector3(0f, 1f, 0f);
            highGo.transform.forward = Vector3.forward;
            high.Initialize("weapon-rifle-01", Vector3.forward, speed: 220f, gravityMultiplier: 0f, damage: 10f, lifetimeSeconds: 2f, ballisticCoefficientG1: 0.7f);

            yield return new WaitForSeconds(0.4f);

            Assert.That(high.CurrentSpeedMetersPerSecond, Is.GreaterThan(low.CurrentSpeedMetersPerSecond));
            Assert.That(highGo.transform.position.z, Is.GreaterThan(lowGo.transform.position.z));

            Object.Destroy(lowGo);
            Object.Destroy(highGo);
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
