using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.PlayerDevice.Runtime;
using Reloader.PlayerDevice.World;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.World;
using UnityEngine;

namespace Reloader.PlayerDevice.Tests.PlayMode
{
    public class PlayerDeviceGroupSessionPlayModeTests
    {
        private const string SelectedTargetId = "target.session.alpha";

        [Test]
        public void IngestProjectileImpact_SelectedTarget_StoresTargetLocalSampleWithAuthoritativeDistance_AndRecomputesMetrics()
        {
            var state = new PlayerDeviceRuntimeState();
            state.SetSelectedTargetBinding(new DeviceTargetBinding(SelectedTargetId, "Session Target", 100f));

            var controller = new PlayerDeviceController(state, null, DeviceAttachmentCatalog.Empty);
            AssertIngestMethodExists(controller);

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var childColliderHost = new GameObject("HitChild");
            childColliderHost.transform.SetParent(target.transform, worldPositionStays: false);
            childColliderHost.AddComponent<BoxCollider>();

            target.transform.position = new Vector3(3f, 1f, -2f);
            target.transform.rotation = Quaternion.Euler(15f, 40f, 5f);

            var metrics = target.AddComponent<DummyTargetRangeMetrics>();
            metrics.Configure(SelectedTargetId, "Session Target", 275f);

            var localFirst = new Vector3(0.12f, -0.08f, 0f);
            var localSecond = new Vector3(-0.03f, 0.14f, 0f);
            var firstPoint = target.transform.TransformPoint(localFirst);
            var secondPoint = target.transform.TransformPoint(localSecond);

            Ingest(controller, firstPoint, childColliderHost);

            Assert.That(state.ActiveGroupSession.ShotCount, Is.EqualTo(1));
            var firstSample = state.ActiveGroupSession.ShotSamples[0];
            Assert.That(firstSample.TargetPlanePointMeters.x, Is.EqualTo(localFirst.x).Within(1e-4f));
            Assert.That(firstSample.TargetPlanePointMeters.y, Is.EqualTo(localFirst.y).Within(1e-4f));
            Assert.That(firstSample.DistanceMeters, Is.EqualTo(275f).Within(1e-4f));

            var metricsAfterFirst = ReadActiveMetrics(controller);
            Assert.That(metricsAfterFirst.ShotCount, Is.EqualTo(1));
            Assert.That(metricsAfterFirst.ValidShotCount, Is.EqualTo(1));
            Assert.That(metricsAfterFirst.IsMoaAvailable, Is.False);

            Ingest(controller, secondPoint, childColliderHost);

            Assert.That(state.ActiveGroupSession.ShotCount, Is.EqualTo(2));
            var metricsAfterSecond = ReadActiveMetrics(controller);
            var expected = DeviceGroupMetricsCalculator.Calculate(state.ActiveGroupSession.ShotSamples.Select(
                sample => new DeviceGroupMetricsCalculator.ShotSample(sample.TargetPlanePointMeters, sample.DistanceMeters)));

            Assert.That(metricsAfterSecond.ShotCount, Is.EqualTo(expected.ShotCount));
            Assert.That(metricsAfterSecond.ValidShotCount, Is.EqualTo(expected.ValidShotCount));
            Assert.That(metricsAfterSecond.IsMoaAvailable, Is.True);
            Assert.That(metricsAfterSecond.AngularSpreadRadians, Is.EqualTo(expected.AngularSpreadRadians).Within(1e-8));
            Assert.That(metricsAfterSecond.Moa, Is.EqualTo(expected.Moa).Within(1e-6));

            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(childColliderHost);
        }

        [Test]
        public void IngestProjectileImpact_WithSourcePoint_UsesDynamicImpactDistance()
        {
            var state = new PlayerDeviceRuntimeState();
            state.SetSelectedTargetBinding(new DeviceTargetBinding(SelectedTargetId, "Session Target", 100f));

            var controller = new PlayerDeviceController(state, null, DeviceAttachmentCatalog.Empty);
            AssertIngestMethodExists(controller);

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var metrics = target.AddComponent<DummyTargetRangeMetrics>();
            metrics.Configure(SelectedTargetId, "Session Target", 999f);

            var sourcePoint = new Vector3(0f, 0f, 0f);
            var impactPoint = new Vector3(0f, 0f, 8.25f);
            var expectedDistance = Vector3.Distance(sourcePoint, impactPoint);

            Ingest(controller, impactPoint, target, sourcePoint);

            Assert.That(state.ActiveGroupSession.ShotCount, Is.EqualTo(1));
            var firstSample = state.ActiveGroupSession.ShotSamples[0];
            Assert.That(firstSample.DistanceMeters, Is.EqualTo(expectedDistance).Within(1e-4f));
            Assert.That(firstSample.DistanceMeters, Is.Not.EqualTo(999f).Within(1e-3f));

            UnityEngine.Object.DestroyImmediate(target);
        }

        [Test]
        public void ApplyDamage_WithoutSourcePoint_FallsBackToAuthoritativeTargetDistance()
        {
            var state = new PlayerDeviceRuntimeState();
            state.SetSelectedTargetBinding(new DeviceTargetBinding(SelectedTargetId, "Session Target", 100f));
            var controller = new PlayerDeviceController(state, null, DeviceAttachmentCatalog.Empty);

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var damageable = target.AddComponent<DummyTargetDamageable>();
            var metrics = target.AddComponent<DummyTargetRangeMetrics>();
            metrics.Configure(SelectedTargetId, "Session Target", 275f);

            var impactPoint = new Vector3(0f, 0f, 8.25f);
            var payloadWithoutSource = new ProjectileImpactPayload("weapon-rifle-01", impactPoint, Vector3.forward, 20f, target);
            Assert.That(payloadWithoutSource.SourcePoint.HasValue, Is.False);
            damageable.ApplyDamage(payloadWithoutSource);

            Assert.That(state.ActiveGroupSession.ShotCount, Is.EqualTo(1));
            var firstSample = state.ActiveGroupSession.ShotSamples[0];
            Assert.That(firstSample.DistanceMeters, Is.EqualTo(275f).Within(1e-4f));

            UnityEngine.Object.DestroyImmediate(target);
            controller.UnregisterAsActiveInstance();
        }

        [Test]
        public void ApplyDamage_DuplicateImpactPayload_SameTargetAndPoint_CountsSingleShot()
        {
            var state = new PlayerDeviceRuntimeState();
            state.SetSelectedTargetBinding(new DeviceTargetBinding(SelectedTargetId, "Session Target", 100f));
            var controller = new PlayerDeviceController(state, null, DeviceAttachmentCatalog.Empty);

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var damageable = target.AddComponent<DummyTargetDamageable>();
            var metrics = target.AddComponent<DummyTargetRangeMetrics>();
            metrics.Configure(SelectedTargetId, "Session Target", 120f);

            var impactPoint = target.transform.position + new Vector3(0.04f, -0.02f, 0.5f);
            var sourcePoint = impactPoint + (Vector3.back * 25f);
            var payload = new ProjectileImpactPayload("weapon-rifle-01", impactPoint, Vector3.forward, 20f, target, sourcePoint);

            damageable.ApplyDamage(payload);
            damageable.ApplyDamage(payload);

            Assert.That(state.ActiveGroupSession.ShotCount, Is.EqualTo(1), "Duplicate callbacks for one physical impact must not double-count MOA shots.");

            UnityEngine.Object.DestroyImmediate(target);
            controller.UnregisterAsActiveInstance();
        }

        [Test]
        public void IngestProjectileImpact_NonSelectedTarget_DoesNotAppendSample()
        {
            var state = new PlayerDeviceRuntimeState();
            state.SetSelectedTargetBinding(new DeviceTargetBinding(SelectedTargetId, "Session Target", 100f));

            var controller = new PlayerDeviceController(state, null, DeviceAttachmentCatalog.Empty);
            AssertIngestMethodExists(controller);

            var otherTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var metrics = otherTarget.AddComponent<DummyTargetRangeMetrics>();
            metrics.Configure("target.other", "Other", 150f);

            var hitPoint = otherTarget.transform.position + Vector3.up;
            Ingest(controller, hitPoint, otherTarget);

            Assert.That(state.ActiveGroupSession.ShotCount, Is.EqualTo(0));
            var activeMetrics = ReadActiveMetrics(controller);
            Assert.That(activeMetrics.ShotCount, Is.EqualTo(0));
            Assert.That(activeMetrics.IsMoaAvailable, Is.False);

            UnityEngine.Object.DestroyImmediate(otherTarget);
        }

        [Test]
        public void ClearCurrentGroup_ClearsPersistentTargetMarkers()
        {
            var state = new PlayerDeviceRuntimeState();
            state.SetSelectedTargetBinding(new DeviceTargetBinding(SelectedTargetId, "Session Target", 90f));

            var controller = new PlayerDeviceController(state, null, DeviceAttachmentCatalog.Empty);

            var target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var damageable = target.AddComponent<DummyTargetDamageable>();
            var metrics = target.AddComponent<DummyTargetRangeMetrics>();
            metrics.Configure(SelectedTargetId, "Session Target", 90f);

            var markerPrefab = new GameObject("MarkerPrefab");
            markerPrefab.AddComponent<TargetImpactMarker>();
            SetPrivateField(typeof(DummyTargetDamageable), damageable, "_impactMarkerPrefab", markerPrefab);

            var markerA = UnityEngine.Object.Instantiate(markerPrefab, target.transform);
            var markerB = UnityEngine.Object.Instantiate(markerPrefab, target.transform);
            markerA.transform.position = target.transform.position + Vector3.up;
            markerB.transform.position = target.transform.position + (Vector3.up * 0.5f);
            Assert.That(target.transform.childCount, Is.EqualTo(2));

            controller.ClearCurrentGroup();
            Assert.That(target.transform.childCount, Is.EqualTo(0), "Clearing a group should clear persistent target hit markers.");

            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(markerPrefab);
        }

        [Test]
        public void ClearCurrentGroup_ResetsActiveCalculations()
        {
            var state = new PlayerDeviceRuntimeState();
            state.SetSelectedTargetBinding(new DeviceTargetBinding(SelectedTargetId, "Session Target", 100f));
            state.AddShotSampleToActiveGroup(new DeviceShotSample(new Vector2(0f, 0f), 100f));
            state.AddShotSampleToActiveGroup(new DeviceShotSample(new Vector2(0.01f, 0f), 100f));

            var controller = new PlayerDeviceController(state, null, DeviceAttachmentCatalog.Empty);
            AssertIngestMethodExists(controller);

            // Force a compute path so we can assert ClearCurrentGroup resets it.
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var metrics = target.AddComponent<DummyTargetRangeMetrics>();
            metrics.Configure(SelectedTargetId, "Session Target", 100f);
            var hitPoint = target.transform.TransformPoint(new Vector3(0.05f, 0.03f, 0f));
            Ingest(controller, hitPoint, target);

            var beforeClear = ReadActiveMetrics(controller);
            Assert.That(beforeClear.ShotCount, Is.GreaterThanOrEqualTo(2));

            controller.ClearCurrentGroup();

            Assert.That(state.ActiveGroupSession.ShotCount, Is.EqualTo(0));
            var afterClear = ReadActiveMetrics(controller);
            Assert.That(afterClear.ShotCount, Is.EqualTo(0));
            Assert.That(afterClear.ValidShotCount, Is.EqualTo(0));
            Assert.That(afterClear.IsMoaAvailable, Is.False);
            Assert.That(afterClear.Moa, Is.EqualTo(0d));

            UnityEngine.Object.DestroyImmediate(target);
        }

        private static void AssertIngestMethodExists(PlayerDeviceController controller)
        {
            var method = controller.GetType().GetMethod("IngestImpact", new[] { typeof(Vector3), typeof(GameObject) });
            Assert.That(method, Is.Not.Null, "Expected public IngestImpact on PlayerDeviceController.");
        }

        private static void Ingest(PlayerDeviceController controller, Vector3 point, GameObject hitObject)
        {
            var method = controller.GetType().GetMethod("IngestImpact", new[] { typeof(Vector3), typeof(GameObject) });
            Assert.That(method, Is.Not.Null, "Expected public IngestImpact on PlayerDeviceController.");
            method.Invoke(controller, new object[] { point, hitObject });
        }

        private static void Ingest(PlayerDeviceController controller, Vector3 point, GameObject hitObject, Vector3 sourcePoint)
        {
            var method = controller.GetType().GetMethod("IngestImpact", new[] { typeof(Vector3), typeof(GameObject), typeof(Vector3?) });
            Assert.That(method, Is.Not.Null, "Expected IngestImpact overload with sourcePoint on PlayerDeviceController.");
            method.Invoke(controller, new object[] { point, hitObject, (Vector3?)sourcePoint });
        }

        private static DeviceGroupMetrics ReadActiveMetrics(PlayerDeviceController controller)
        {
            var property = controller.GetType().GetProperty("ActiveGroupMetrics", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, "Expected ActiveGroupMetrics property on PlayerDeviceController.");

            var value = property.GetValue(controller);
            Assert.That(value, Is.Not.Null);
            return (DeviceGroupMetrics)value;
        }

        private static void SetPrivateField(Type type, object target, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {type?.Name}.");
            field.SetValue(target, value);
        }
    }
}
