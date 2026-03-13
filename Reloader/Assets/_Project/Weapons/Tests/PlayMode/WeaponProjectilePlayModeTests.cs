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
            projectile.Initialize("weapon-kar98k", Vector3.forward, speed: 120f, gravityMultiplier: 0f, damage: 33f);

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
            projectile.Initialize("weapon-kar98k", Vector3.forward, speed: 120f, gravityMultiplier: 0f, damage: 33f);

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

            projectile.Initialize("weapon-kar98k", Vector3.forward, speed: 120f, gravityMultiplier: 0f, damage: 33f);

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
            Assert.That(hitItemId, Is.EqualTo("weapon-kar98k"));
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
            projectile.Initialize("weapon-kar98k", Vector3.forward, speed: 15f, gravityMultiplier: 1f, damage: 10f);

            var start = projectileGo.transform.position;
            yield return null;
            yield return null;
            var end = projectileGo.transform.position;

            Assert.That(end.z, Is.GreaterThan(start.z));
            Assert.That(end.y, Is.LessThan(start.y));

            Object.Destroy(projectileGo);
        }

        [UnityTest]
        public IEnumerator Projectile_PathObserverReportsExactCurvedSegments()
        {
            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = new Vector3(0f, 3f, 0f);
            projectileGo.transform.forward = Vector3.forward;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();
            var observer = new RecordingPathObserver();
            projectile.SetPathObserver(observer);
            projectile.Initialize("weapon-kar98k", Vector3.forward, speed: 18f, gravityMultiplier: 1f, damage: 10f);

            var elapsed = 0f;
            while (observer.Segments.Count < 2 && elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(observer.Segments.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(observer.Segments[0].StartPoint, Is.EqualTo(new Vector3(0f, 3f, 0f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(observer.Segments[0].EndPoint.z, Is.GreaterThan(observer.Segments[0].StartPoint.z));
            Assert.That(observer.Segments[1].EndPoint.y, Is.LessThan(observer.Segments[0].EndPoint.y));
            Assert.That(observer.TerminalPoint.HasValue, Is.False);

            Object.Destroy(projectileGo);
        }

        [UnityTest]
        public IEnumerator Projectile_Despawned_WhenItFallsBelowWorldFloor()
        {
            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = new Vector3(0f, -499.9f, 0f);
            projectileGo.transform.forward = Vector3.down;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();
            projectile.Initialize("weapon-kar98k", Vector3.down, speed: 10f, gravityMultiplier: 0f, damage: 1f);

            yield return new WaitForSeconds(0.1f);

            Assert.That(projectile == null || projectile.Equals(null), Is.True);
        }

        [UnityTest]
        public IEnumerator Projectile_PathObserverReportsTerminalPointWhenItFallsBelowWorldFloorWithoutHit()
        {
            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = new Vector3(0f, -499.8f, 0f);
            projectileGo.transform.forward = Vector3.down;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();
            var observer = new RecordingPathObserver();
            projectile.SetPathObserver(observer);
            projectile.Initialize("weapon-kar98k", Vector3.down, speed: 30f, gravityMultiplier: 0f, damage: 1f);

            yield return new WaitForSeconds(0.1f);

            Assert.That(observer.TerminalPoint.HasValue, Is.True);
            Assert.That(observer.TerminalDidHit, Is.False);
            Assert.That(observer.Segments.Count, Is.GreaterThan(0));
            Assert.That(observer.TerminalPoint.Value, Is.EqualTo(observer.Segments[^1].EndPoint).Using(Vector3EqualityComparer.Instance));
            Assert.That(observer.TerminalPoint.Value.y, Is.LessThan(-500f));
        }

        [UnityTest]
        public IEnumerator Projectile_PathObserverCompletesWhenProjectileIsDestroyedExternally()
        {
            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = new Vector3(1f, 2f, 3f);
            projectileGo.transform.forward = Vector3.forward;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();
            var observer = new RecordingPathObserver();
            projectile.SetPathObserver(observer);
            projectile.Initialize("weapon-kar98k", Vector3.forward, speed: 30f, gravityMultiplier: 0f, damage: 1f);

            yield return null;

            Object.Destroy(projectileGo);
            yield return null;

            Assert.That(observer.Segments.Count, Is.GreaterThan(0));
            Assert.That(observer.TerminalPoint.HasValue, Is.True, "Destroying a projectile externally should still complete the observed path.");
            Assert.That(observer.TerminalDidHit, Is.False);
            Assert.That(observer.TerminalPoint.Value, Is.EqualTo(observer.Segments[^1].EndPoint).Using(Vector3EqualityComparer.Instance));
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

            projectile.Initialize("weapon-kar98k", Vector3.forward, speed: 120f, gravityMultiplier: 0f, damage: 22f, shooterRoot: shooter.transform);

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
        public IEnumerator Projectile_IgnoresIntermediateTriggerColliders_AndHitsSolidTarget()
        {
            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = new Vector3(0f, 1f, 0f);
            projectileGo.transform.forward = Vector3.forward;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();

            var triggerVolume = GameObject.CreatePrimitive(PrimitiveType.Cube);
            triggerVolume.transform.position = new Vector3(0f, 1f, 2f);
            triggerVolume.transform.localScale = new Vector3(2f, 2f, 0.25f);
            var triggerCollider = triggerVolume.GetComponent<Collider>();
            triggerCollider.isTrigger = true;

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = new Vector3(0f, 1f, 4f);
            target.transform.localScale = new Vector3(1f, 1f, 1f);
            var receiver = target.AddComponent<TestDamageable>();

            projectile.Initialize("weapon-kar98k", Vector3.forward, speed: 120f, gravityMultiplier: 0f, damage: 22f);

            var elapsed = 0f;
            while (receiver.HitCount == 0 && elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(receiver.HitCount, Is.EqualTo(1));
            Assert.That(projectile == null || projectile.Equals(null), Is.True);

            Object.Destroy(projectileGo);
            Object.Destroy(triggerVolume);
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

            projectile.Initialize("weapon-kar98k", Vector3.forward, speed: 90f, gravityMultiplier: 0f, damage: 10f);

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
            low.Initialize("weapon-kar98k", Vector3.forward, speed: 220f, gravityMultiplier: 0f, damage: 10f, ballisticCoefficientG1: 0.2f);

            var highGo = new GameObject("HighBcProjectile");
            var high = highGo.AddComponent<WeaponProjectile>();
            highGo.transform.position = new Vector3(0f, 1f, 0f);
            highGo.transform.forward = Vector3.forward;
            high.Initialize("weapon-kar98k", Vector3.forward, speed: 220f, gravityMultiplier: 0f, damage: 10f, ballisticCoefficientG1: 0.7f);

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

        private sealed class RecordingPathObserver : WeaponProjectile.IPathObserver
        {
            public readonly System.Collections.Generic.List<(Vector3 StartPoint, Vector3 EndPoint)> Segments = new();
            public Vector3? TerminalPoint { get; private set; }
            public bool TerminalDidHit { get; private set; }

            public void RecordSegment(Vector3 startPoint, Vector3 endPoint)
            {
                Segments.Add((startPoint, endPoint));
            }

            public void Complete(Vector3 terminalPoint, bool didHit)
            {
                TerminalPoint = terminalPoint;
                TerminalDidHit = didHit;
            }
        }

        private sealed class Vector3EqualityComparer : System.Collections.IEqualityComparer
        {
            public static readonly Vector3EqualityComparer Instance = new();

            public new bool Equals(object x, object y)
            {
                if (x is not Vector3 left || y is not Vector3 right)
                {
                    return false;
                }

                return Vector3.Distance(left, right) <= 0.01f;
            }

            public int GetHashCode(object obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }
}
