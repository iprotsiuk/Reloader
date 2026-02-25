using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Weapons.Ballistics;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class DummyTargetDamageablePlayModeTests
    {
        [UnityTest]
        public IEnumerator ApplyDamage_SpawnsImpactMarker_AtHitPoint()
        {
            var dummyType = System.Type.GetType("Reloader.Weapons.World.DummyTargetDamageable, Reloader.Weapons");
            Assert.That(dummyType, Is.Not.Null, "DummyTargetDamageable type should exist.");

            var markerType = System.Type.GetType("Reloader.Weapons.World.TargetImpactMarker, Reloader.Weapons");
            Assert.That(markerType, Is.Not.Null, "TargetImpactMarker type should exist.");

            var target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            target.name = "RoundDummyTarget";
            var damageable = target.AddComponent(dummyType);

            var markerPrefab = new GameObject("MarkerPrefab");
            markerPrefab.AddComponent(markerType);
            SetPrivateField(dummyType, damageable, "_impactMarkerPrefab", markerPrefab);

            var hitPoint = target.transform.position + Vector3.up;
            var payload = new ProjectileImpactPayload("weapon-rifle-01", hitPoint, Vector3.forward, 20f, target);
            InvokeApplyDamage(dummyType, damageable, payload);

            yield return null;

            Assert.That(target.transform.childCount, Is.EqualTo(1));
            var marker = target.transform.GetChild(0);
            Assert.That(Vector3.Distance(marker.position, hitPoint), Is.LessThan(0.02f));

            Object.Destroy(target);
            Object.Destroy(markerPrefab);
        }

        [UnityTest]
        public IEnumerator ImpactMarkers_Persist_UntilSceneReset()
        {
            var dummyType = System.Type.GetType("Reloader.Weapons.World.DummyTargetDamageable, Reloader.Weapons");
            Assert.That(dummyType, Is.Not.Null, "DummyTargetDamageable type should exist.");

            var markerType = System.Type.GetType("Reloader.Weapons.World.TargetImpactMarker, Reloader.Weapons");
            Assert.That(markerType, Is.Not.Null, "TargetImpactMarker type should exist.");

            var target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var damageable = target.AddComponent(dummyType);

            var markerPrefab = new GameObject("MarkerPrefab");
            markerPrefab.AddComponent(markerType);
            SetPrivateField(dummyType, damageable, "_impactMarkerPrefab", markerPrefab);

            InvokeApplyDamage(dummyType, damageable, new ProjectileImpactPayload("weapon-rifle-01", target.transform.position + Vector3.up, Vector3.forward, 20f, target));
            InvokeApplyDamage(dummyType, damageable, new ProjectileImpactPayload("weapon-rifle-01", target.transform.position + (Vector3.up * 0.7f), Vector3.right, 20f, target));

            yield return new WaitForSeconds(0.2f);
            Assert.That(target.transform.childCount, Is.EqualTo(2));

            Object.Destroy(target);
            Object.Destroy(markerPrefab);
        }

        private static void SetPrivateField(System.Type type, object target, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {type?.Name}.");
            field.SetValue(target, value);
        }

        private static void InvokeApplyDamage(System.Type type, object target, ProjectileImpactPayload payload)
        {
            var method = type.GetMethod("ApplyDamage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, $"Expected ApplyDamage on {type?.Name}.");
            method.Invoke(target, new object[] { payload });
        }
    }
}
