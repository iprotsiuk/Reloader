using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Runtime;
using Reloader.Player;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Cinematics;
using Reloader.Weapons.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class WeaponProjectilePlayModeTests
    {
        private IGameEventsRuntimeHub _runtimeEventsBeforeEachTest;
        private readonly System.Collections.Generic.HashSet<int> _baselineRootInstanceIds = new();

        [SetUp]
        public void SetUp()
        {
            _runtimeEventsBeforeEachTest = RuntimeKernelBootstrapper.Events;
            _baselineRootInstanceIds.Clear();

            var activeScene = SceneManager.GetActiveScene();
            var roots = activeScene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                _baselineRootInstanceIds.Add(root.GetInstanceID());
            }
        }

        [TearDown]
        public void TearDown()
        {
            RuntimeKernelBootstrapper.Events = _runtimeEventsBeforeEachTest;
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            RuntimeKernelBootstrapper.Events = _runtimeEventsBeforeEachTest;

            var activeScene = SceneManager.GetActiveScene();
            var roots = activeScene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null || _baselineRootInstanceIds.Contains(root.GetInstanceID()))
                {
                    continue;
                }

                Object.Destroy(root);
            }

            yield return null;
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
        public IEnumerator Projectile_ForwardsImpactDirectionAndEnergyDrivingMetadata_IntoHitPayload()
        {
            const float launchSpeedMetersPerSecond = 180f;
            const float gravityMultiplier = 4f;
            const float ballisticCoefficientG1 = 0.2f;
            var expectedProjectileMassGrains = WeaponAmmoDefaults.DefaultProjectileMassGrains;
            const float grainsToKilograms = 0.00006479891f;

            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = new Vector3(0f, 1000f, 0f);
            projectileGo.transform.forward = Vector3.forward;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = new Vector3(0f, 995.7f, 75f);
            target.transform.localScale = new Vector3(2f, 12f, 2f);
            var receiver = target.AddComponent<TestDamageable>();
            receiver.BindProjectile(projectile);

            projectile.Initialize(
                "weapon-kar98k",
                Vector3.forward,
                speed: launchSpeedMetersPerSecond,
                gravityMultiplier: gravityMultiplier,
                damage: 33f,
                ballisticCoefficientG1: ballisticCoefficientG1);

            var elapsed = 0f;
            while (receiver.HitCount == 0 && elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(receiver.HitCount, Is.EqualTo(1));
            Assert.That(receiver.LastProjectileVelocityAtImpact.HasValue, Is.True, "Expected to capture projectile velocity at impact time.");
            Assert.That(receiver.LastPayload.HasValue, Is.True);

            var expectedImpactVelocity = receiver.LastProjectileVelocityAtImpact!.Value;
            var expectedImpactDirection = expectedImpactVelocity.normalized;
            var expectedImpactSpeedMetersPerSecond = expectedImpactVelocity.magnitude;
            Assert.That(Vector3.Dot(expectedImpactDirection, Vector3.forward), Is.LessThan(0.9999f), "Impact direction should differ from launch direction in this setup.");
            Assert.That(expectedImpactSpeedMetersPerSecond, Is.LessThan(launchSpeedMetersPerSecond - 0.5f), "Impact speed should differ from launch speed in this setup.");

            var payload = receiver.LastPayload.Value;
            var payloadDirection = ReadPayloadVector3Property(payload, "Direction");
            var payloadImpactSpeedMetersPerSecond = ReadPayloadFloatProperty(payload, "ImpactSpeedMetersPerSecond");
            var payloadProjectileMassGrains = ReadPayloadFloatProperty(payload, "ProjectileMassGrains");
            var payloadDeliveredEnergyJoules = ReadPayloadFloatProperty(payload, "DeliveredEnergyJoules");
            var expectedDeliveredEnergyJoules = 0.5f
                * (expectedProjectileMassGrains * grainsToKilograms)
                * expectedImpactVelocity.magnitude
                * expectedImpactVelocity.magnitude;

            Assert.That(payloadDirection.sqrMagnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(Vector3.Dot(payloadDirection, expectedImpactDirection), Is.GreaterThan(0.9999f));
            Assert.That(payloadImpactSpeedMetersPerSecond, Is.EqualTo(expectedImpactSpeedMetersPerSecond).Within(0.01f));
            Assert.That(payloadProjectileMassGrains, Is.EqualTo(expectedProjectileMassGrains).Within(0.001f));
            Assert.That(payloadDeliveredEnergyJoules, Is.EqualTo(expectedDeliveredEnergyJoules).Within(0.1f));

            Object.Destroy(projectileGo);
            Object.Destroy(target);
        }

        [Test]
        public void Projectile_UsesHitFractionToReportImpactMetadataAtContactPoint()
        {
            const float launchSpeedMetersPerSecond = 500f;
            const float gravityMultiplier = 0f;
            const float ballisticCoefficientG1 = 0.05f;
            const float projectileMassGrains = 175f;
            const float dragCoefficient = 0.00012f;
            const float hitDistanceMeters = 2.1f;
            const float stepDt = 0.016f;

            var projectileGo = new GameObject("Projectile");
            try
            {
                projectileGo.transform.forward = Vector3.forward;
                var projectile = projectileGo.AddComponent<WeaponProjectile>();
                projectile.Initialize(
                    "weapon-kar98k",
                    Vector3.forward,
                    speed: launchSpeedMetersPerSecond,
                    gravityMultiplier: gravityMultiplier,
                    damage: 33f,
                    ballisticCoefficientG1: ballisticCoefficientG1,
                    projectileMassGrains: projectileMassGrains);

                var resolveImpactVelocityMethod = typeof(WeaponProjectile).GetMethod(
                    "ResolveImpactVelocity",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(resolveImpactVelocityMethod, Is.Not.Null,
                    "Expected contact-point impact metadata helper to exist.");

                var dragFactor = dragCoefficient / ballisticCoefficientG1;
                var stepStartVelocity = Vector3.forward * launchSpeedMetersPerSecond;
                var frameEndSpeedMetersPerSecond = launchSpeedMetersPerSecond
                    - (dragFactor * launchSpeedMetersPerSecond * launchSpeedMetersPerSecond * stepDt);
                var frameEndVelocity = Vector3.forward * frameEndSpeedMetersPerSecond;
                var delta = frameEndVelocity * stepDt;

                var impactVelocity = (Vector3)resolveImpactVelocityMethod!.Invoke(
                    projectile,
                    new object[] { stepStartVelocity, stepDt, delta, hitDistanceMeters })!;

                var payloadImpactSpeedMetersPerSecond = impactVelocity.magnitude;
                var payloadDeliveredEnergyJoules = ImpactEnergyMath.ComputeDeliveredEnergyJoules(
                    payloadImpactSpeedMetersPerSecond,
                    projectileMassGrains);
                var contactDt = stepDt * (hitDistanceMeters / delta.magnitude);
                var expectedImpactSpeedMetersPerSecond = launchSpeedMetersPerSecond
                    - (dragFactor * launchSpeedMetersPerSecond * launchSpeedMetersPerSecond * contactDt);
                var expectedDeliveredEnergyJoules = ImpactEnergyMath.ComputeDeliveredEnergyJoules(
                    expectedImpactSpeedMetersPerSecond,
                    projectileMassGrains);

                Assert.That(payloadImpactSpeedMetersPerSecond, Is.GreaterThan(frameEndSpeedMetersPerSecond + 0.05f),
                    "Expected contact-point metadata to preserve more speed than the fully integrated end-of-frame velocity.");
                Assert.That(payloadImpactSpeedMetersPerSecond,
                    Is.EqualTo(expectedImpactSpeedMetersPerSecond).Within(0.001f));
                Assert.That(payloadDeliveredEnergyJoules,
                    Is.EqualTo(expectedDeliveredEnergyJoules).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(projectileGo);
            }
        }

        private static float ReadPayloadFloatProperty(object payload, string propertyName)
        {
            var property = payload.GetType().GetProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Expected ProjectileImpactPayload.{propertyName}.");
            var value = property!.GetValue(payload);
            Assert.That(value, Is.Not.Null, $"Expected ProjectileImpactPayload.{propertyName} to produce a value.");
            return (float)value!;
        }

        private static Vector3 ReadPayloadVector3Property(object payload, string propertyName)
        {
            var property = payload.GetType().GetProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Expected ProjectileImpactPayload.{propertyName}.");
            var value = property!.GetValue(payload);
            Assert.That(value, Is.Not.Null, $"Expected ProjectileImpactPayload.{propertyName} to produce a value.");
            return (Vector3)value!;
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
        public IEnumerator Projectile_ShotCameraPresentationToggle_UpdatesReadableVisualState()
        {
            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = Vector3.zero;
            projectileGo.transform.forward = Vector3.forward;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();
            yield return null;

            var visual = projectileGo.transform.Find("ProjectileVisual");
            Assert.That(visual, Is.Not.Null, "Expected runtime projectile visuals to exist for shot-camera presentation.");

            var baselineScale = visual!.localScale;
            Assert.That(projectile.IsShotCameraPresentationActive, Is.False);

            projectile.SetShotCameraPresentationActive(true);
            Assert.That(projectile.IsShotCameraPresentationActive, Is.True);
            Assert.That(visual.localScale.x, Is.GreaterThan(baselineScale.x));

            projectile.SetShotCameraPresentationActive(false);
            Assert.That(projectile.IsShotCameraPresentationActive, Is.False);
            Assert.That(visual.localScale, Is.EqualTo(baselineScale).Using(Vector3EqualityComparer.Instance));

            Object.Destroy(projectileGo);
        }

        [UnityTest]
        public IEnumerator ShotCameraRuntime_ProjectileDespawn_RestoresRealtimeAndClearsCinematicCamera()
        {
            GameObject runtimeRoot = null;
            GameObject projectileGo = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                runtimeRoot = new GameObject("ShotCameraRuntimeRoot");
                var runtime = runtimeRoot.AddComponent<ShotCameraRuntime>();
                var settings = new ShotCameraSettings(true, 100f, 0.1f, 0.25f);
                runtime.Configure(null, settings);

                projectileGo = new GameObject("Projectile");
                projectileGo.transform.position = new Vector3(0f, -499.8f, 0f);
                projectileGo.transform.forward = Vector3.down;
                var projectile = projectileGo.AddComponent<WeaponProjectile>();
                projectile.Initialize("weapon-kar98k", Vector3.down, speed: 30f, gravityMultiplier: 0f, damage: 1f);

                var request = new ShotCameraRequest(
                    projectile,
                    projectileGo.transform.position,
                    projectileGo.transform.position + (Vector3.down * 120f),
                    120f,
                    settings);
                Assert.That(runtime.TryRegisterShot(request), Is.True);
                Assert.That(FindShotRenderCamera(), Is.Not.Null, "Expected shot cam registration to create a temporary render camera.");
                Assert.That(ShotCameraGameplayState.PresentationCamera, Is.SameAs(FindShotRenderCamera()), "Expected projectile shot cam to publish the temporary render camera as the current presentation camera.");

                var lingerStartTime = Time.unscaledTime;
                var elapsed = 0f;
                while (runtime.IsShotActive && elapsed < 3f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.That(runtime.IsShotActive, Is.False, "Expected projectile despawn termination to end shot cam automatically.");
                Assert.That(runtime.HasActiveCinematicCamera, Is.False, "Expected projectile despawn termination to clear the temporary cinematic camera.");
                Assert.That(Time.unscaledTime - lingerStartTime, Is.GreaterThanOrEqualTo(0.9f), "Expected miss/despawn termination to linger at the terminal point before restoring the player camera.");
                Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f));
                Assert.That(FindShotRenderCamera(), Is.Null, "Expected the temporary shot camera to be removed after projectile despawn.");
                Assert.That(ShotCameraGameplayState.PresentationCamera, Is.Null, "Expected shot-cam teardown to clear the current presentation camera.");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
                {
                    Object.Destroy(projectile.gameObject);
                }

                foreach (var cinematicCamera in FindAllCinemachineCameras())
                {
                    Object.Destroy(cinematicCamera.gameObject);
                }

                if (projectileGo != null)
                {
                    Object.Destroy(projectileGo);
                }

                if (runtimeRoot != null)
                {
                    Object.Destroy(runtimeRoot);
                }
            }
        }

        [UnityTest]
        public IEnumerator ShotCameraRuntime_ProjectileDespawn_LingersForMissBeforeRestore()
        {
            GameObject runtimeRoot = null;
            GameObject projectileGo = null;
            GameObject cameraGo = null;
            var previousTimeScale = Time.timeScale;
            var previousFixedDeltaTime = Time.fixedDeltaTime;

            try
            {
                Time.timeScale = 1f;
                runtimeRoot = new GameObject("ShotCameraRuntimeRoot");
                var runtime = runtimeRoot.AddComponent<ShotCameraRuntime>();
                var settings = new ShotCameraSettings(true, 100f, 0.1f, 0.25f);
                runtime.Configure(null, settings);

                cameraGo = new GameObject("WorldCamera");
                var worldCamera = cameraGo.AddComponent<Camera>();
                worldCamera.tag = "MainCamera";

                projectileGo = new GameObject("Projectile");
                projectileGo.transform.position = new Vector3(0f, -499.8f, 0f);
                projectileGo.transform.forward = Vector3.down;
                var projectile = projectileGo.AddComponent<WeaponProjectile>();
                projectile.Initialize("weapon-kar98k", Vector3.down, speed: 30f, gravityMultiplier: 0f, damage: 1f);

                var request = new ShotCameraRequest(
                    projectile,
                    projectileGo.transform.position,
                    projectileGo.transform.position + (Vector3.down * 120f),
                    120f,
                    settings);
                Assert.That(runtime.TryRegisterShot(request), Is.True);
                Assert.That(runtime.IsShotActive, Is.True);

                yield return new WaitForSecondsRealtime(0.5f);
                Assert.That(runtime.IsShotActive, Is.True, "Expected missed shots to hold the impact camera briefly before restoring gameplay.");

                yield return new WaitForSecondsRealtime(0.8f);
                Assert.That(runtime.IsShotActive, Is.False, "Expected missed-shot linger to end after roughly one second.");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Time.fixedDeltaTime = previousFixedDeltaTime;

                foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
                {
                    Object.Destroy(projectile.gameObject);
                }

                foreach (var cinematicCamera in FindAllCinemachineCameras())
                {
                    Object.Destroy(cinematicCamera.gameObject);
                }

                if (cameraGo != null)
                {
                    Object.Destroy(cameraGo);
                }

                if (projectileGo != null)
                {
                    Object.Destroy(projectileGo);
                }

                if (runtimeRoot != null)
                {
                    Object.Destroy(runtimeRoot);
                }
            }
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
            shooter.transform.position = new Vector3(0f, 999f, 0f);
            var shooterCollider = shooter.AddComponent<CapsuleCollider>();
            shooterCollider.center = new Vector3(0f, 1f, 0f);
            shooterCollider.height = 2f;
            shooterCollider.radius = 0.35f;

            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = new Vector3(0f, 1000f, 0f);
            projectileGo.transform.forward = Vector3.forward;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = new Vector3(0f, 1000f, 4f);
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
            projectileGo.transform.position = new Vector3(0f, 1000f, 0f);
            projectileGo.transform.forward = Vector3.forward;
            var projectile = projectileGo.AddComponent<WeaponProjectile>();

            var triggerVolume = GameObject.CreatePrimitive(PrimitiveType.Cube);
            triggerVolume.transform.position = new Vector3(0f, 1000f, 2f);
            triggerVolume.transform.localScale = new Vector3(2f, 2f, 0.25f);
            var triggerCollider = triggerVolume.GetComponent<Collider>();
            triggerCollider.isTrigger = true;

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = new Vector3(0f, 1000f, 4f);
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
            yield return null;
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
            lowGo.transform.position = new Vector3(0f, 1000f, 0f);
            lowGo.transform.forward = Vector3.forward;
            low.Initialize("weapon-kar98k", Vector3.forward, speed: 220f, gravityMultiplier: 0f, damage: 10f, ballisticCoefficientG1: 0.2f);

            var highGo = new GameObject("HighBcProjectile");
            var high = highGo.AddComponent<WeaponProjectile>();
            highGo.transform.position = new Vector3(0f, 1001f, 0f);
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
            private static readonly FieldInfo ProjectileVelocityField =
                typeof(WeaponProjectile).GetField("_velocity", BindingFlags.Instance | BindingFlags.NonPublic);

            private WeaponProjectile _boundProjectile;
            public int HitCount { get; private set; }
            public float LastDamage { get; private set; }
            public ProjectileImpactPayload? LastPayload { get; private set; }
            public Vector3? LastProjectileVelocityAtImpact { get; private set; }

            public void BindProjectile(WeaponProjectile projectile)
            {
                _boundProjectile = projectile;
            }

            public void ApplyDamage(ProjectileImpactPayload payload)
            {
                HitCount++;
                LastDamage = payload.Damage;
                LastPayload = payload;
                LastProjectileVelocityAtImpact = CaptureProjectileVelocityAtImpact();
            }

            private Vector3? CaptureProjectileVelocityAtImpact()
            {
                if (ProjectileVelocityField == null)
                {
                    return null;
                }

                if (_boundProjectile == null || _boundProjectile.Equals(null))
                {
                    return null;
                }

                var value = ProjectileVelocityField.GetValue(_boundProjectile);
                if (value is Vector3 velocity)
                {
                    return velocity;
                }

                return null;
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

        private static Camera FindShotRenderCamera()
        {
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera != null && camera.name == "ShotCameraRuntime_Camera")
                {
                    return camera;
                }
            }

            return null;
        }

        private static System.Collections.Generic.List<MonoBehaviour> FindAllCinemachineCameras()
        {
            var matches = new System.Collections.Generic.List<MonoBehaviour>();
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour != null && behaviour.GetType().FullName == "Unity.Cinemachine.CinemachineCamera")
                {
                    matches.Add(behaviour);
                }
            }

            return matches;
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
