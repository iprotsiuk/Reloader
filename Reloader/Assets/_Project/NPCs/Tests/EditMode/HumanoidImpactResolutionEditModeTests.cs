using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Combat;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Reloader.NPCs.Tests.EditMode
{
    public sealed class HumanoidImpactResolutionEditModeTests
    {
        [Test]
        public void HumanoidDamageReceiver_NewRuntimeInstanceDefaultsToOneHundredHealth()
        {
            var root = new GameObject("Humanoid");

            try
            {
                var receiver = root.AddComponent<HumanoidDamageReceiver>();
                receiver.ResetRuntime();

                Assert.That(receiver.MaxHealth, Is.EqualTo(100f));
                Assert.That(receiver.CurrentHealth, Is.EqualTo(100f));
                Assert.That(receiver.IsDead, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(HumanoidBodyZone.Head, 199f, false, 99.5f)]
        [TestCase(HumanoidBodyZone.Head, 200f, true, 100f)]
        [TestCase(HumanoidBodyZone.Neck, 199f, false, 99.5f)]
        [TestCase(HumanoidBodyZone.Neck, 200f, true, 100f)]
        [TestCase(HumanoidBodyZone.Torso, 1799f, false, 80.955f)]
        [TestCase(HumanoidBodyZone.Torso, 1800f, true, 100f)]
        [TestCase(HumanoidBodyZone.Pelvis, 3500f, false, 80f)]
        [TestCase(HumanoidBodyZone.ArmL, 3500f, false, 35f)]
        [TestCase(HumanoidBodyZone.ArmR, 3500f, false, 35f)]
        [TestCase(HumanoidBodyZone.LegL, 3500f, false, 45f)]
        [TestCase(HumanoidBodyZone.LegR, 3500f, false, 45f)]
        public void Resolve_UsesExactZoneDamageTable(
            HumanoidBodyZone bodyZone,
            float deliveredEnergyJoules,
            bool expectedIsLethal,
            float expectedRecommendedDamage)
        {
            var result = HumanoidImpactResolution.Resolve(bodyZone, deliveredEnergyJoules);

            Assert.That(result.IsLethal, Is.EqualTo(expectedIsLethal));
            Assert.That(result.RecommendedHealthDamage, Is.EqualTo(expectedRecommendedDamage).Within(0.001f));
            Assert.That(result.EffectiveEnergyJoules, Is.EqualTo(deliveredEnergyJoules).Within(0.001f),
                "Expected damage tuning to use raw delivered impact energy rather than a hidden global zone multiplier.");
        }

        [Test]
        public void ApplyDamage_ConsumesDeliveredEnergyFromPayloadWhenPositive()
        {
            var payloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            GameObject root = null;
            GameObject armZone = null;

            try
            {
                root = new GameObject("Humanoid");
                var receiver = root.AddComponent<HumanoidDamageReceiver>();
                receiver.SetHealthStateForRuntime(100f, 100f);

                armZone = CreateZone(root.transform, HumanoidBodyZone.ArmL);
                InvokeApplyDamage(receiver, CreateImpactPayload(
                    payloadType,
                    hitObject: armZone,
                    deliveredEnergyJoules: 3500f,
                    impactSpeedMetersPerSecond: 1f,
                    projectileMassGrains: 1f,
                    damage: 0f));

                Assert.That(receiver.LastZone, Is.EqualTo(HumanoidBodyZone.ArmL));
                Assert.That(receiver.LastResult.RecommendedHealthDamage, Is.EqualTo(35f).Within(0.001f));
                Assert.That(receiver.CurrentHealth, Is.EqualTo(65f).Within(0.001f));
                Assert.That(receiver.IsDead, Is.False);
            }
            finally
            {
                if (armZone != null)
                {
                    Object.DestroyImmediate(armZone);
                }

                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        [TestCase(HumanoidBodyZone.Head, 200f)]
        [TestCase(HumanoidBodyZone.Neck, 200f)]
        [TestCase(HumanoidBodyZone.Torso, 1800f)]
        public void ApplyDamage_ZoneLethalThresholdsKillFromFullHealth(HumanoidBodyZone bodyZone, float deliveredEnergyJoules)
        {
            var receiver = CreateReceiver(out var root, out var hitObject, bodyZone);

            try
            {
                ApplyZoneHit(receiver, hitObject, deliveredEnergyJoules);

                Assert.That(receiver.IsDead, Is.True);
                Assert.That(receiver.CurrentHealth, Is.EqualTo(0f));
                Assert.That(receiver.LastResult.IsLethal, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(hitObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ApplyDamage_PelvisFullPowerHitDealsEightyAndDoesNotKillFromFullHealth()
        {
            var receiver = CreateReceiver(out var root, out var hitObject, HumanoidBodyZone.Pelvis);

            try
            {
                ApplyZoneHit(receiver, hitObject, 3500f);

                Assert.That(receiver.IsDead, Is.False);
                Assert.That(receiver.CurrentHealth, Is.EqualTo(20f).Within(0.001f));
                Assert.That(receiver.LastResult.RecommendedHealthDamage, Is.EqualTo(80f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(hitObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ApplyDamage_OneArmHitDoesNotKillButRepeatedArmHitsCan()
        {
            var receiver = CreateReceiver(out var root, out var hitObject, HumanoidBodyZone.ArmR);

            try
            {
                ApplyZoneHit(receiver, hitObject, 3500f);
                Assert.That(receiver.IsDead, Is.False);
                Assert.That(receiver.CurrentHealth, Is.EqualTo(65f).Within(0.001f));

                ApplyZoneHit(receiver, hitObject, 3500f);
                Assert.That(receiver.IsDead, Is.False);
                Assert.That(receiver.CurrentHealth, Is.EqualTo(30f).Within(0.001f));

                ApplyZoneHit(receiver, hitObject, 3500f);
                Assert.That(receiver.IsDead, Is.True);
                Assert.That(receiver.CurrentHealth, Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(hitObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ApplyDamage_OneAndTwoLegHitsDoNotKillButThreeCan()
        {
            var receiver = CreateReceiver(out var root, out var hitObject, HumanoidBodyZone.LegL);

            try
            {
                ApplyZoneHit(receiver, hitObject, 3500f);
                Assert.That(receiver.IsDead, Is.False);
                Assert.That(receiver.CurrentHealth, Is.EqualTo(55f).Within(0.001f));

                ApplyZoneHit(receiver, hitObject, 3500f);
                Assert.That(receiver.IsDead, Is.False);
                Assert.That(receiver.CurrentHealth, Is.EqualTo(10f).Within(0.001f));

                ApplyZoneHit(receiver, hitObject, 3500f);
                Assert.That(receiver.IsDead, Is.True);
                Assert.That(receiver.CurrentHealth, Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(hitObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ApplyDamage_DiedFiresOnceWhenAccumulatedDamageReachesZero()
        {
            var receiver = CreateReceiver(out var root, out var hitObject, HumanoidBodyZone.LegR);
            var diedCount = 0;
            receiver.Died += () => diedCount++;

            try
            {
                ApplyZoneHit(receiver, hitObject, 3500f);
                ApplyZoneHit(receiver, hitObject, 3500f);
                Assert.That(diedCount, Is.EqualTo(0));

                ApplyZoneHit(receiver, hitObject, 3500f);
                ApplyZoneHit(receiver, hitObject, 3500f);

                Assert.That(receiver.IsDead, Is.True);
                Assert.That(diedCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(hitObject);
                Object.DestroyImmediate(root);
            }
        }

        private static HumanoidDamageReceiver CreateReceiver(
            out GameObject root,
            out GameObject hitObject,
            HumanoidBodyZone bodyZone)
        {
            root = new GameObject("Humanoid");
            var receiver = root.AddComponent<HumanoidDamageReceiver>();
            receiver.SetHealthStateForRuntime(100f, 100f);
            hitObject = CreateZone(root.transform, bodyZone);
            return receiver;
        }

        private static GameObject CreateZone(Transform parent, HumanoidBodyZone bodyZone)
        {
            var zone = new GameObject(bodyZone.ToString());
            zone.transform.SetParent(parent, false);
            zone.AddComponent<BoxCollider>();
            zone.AddComponent<BodyZoneHitbox>().Configure(bodyZone);
            return zone;
        }

        private static void ApplyZoneHit(
            HumanoidDamageReceiver receiver,
            GameObject hitObject,
            float deliveredEnergyJoules)
        {
            var payloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            InvokeApplyDamage(receiver, CreateImpactPayload(
                payloadType,
                hitObject,
                deliveredEnergyJoules,
                impactSpeedMetersPerSecond: 0f,
                projectileMassGrains: 0f,
                damage: 0f));
        }

        private static Type ResolveType(string typeName, string assemblyName)
        {
            var type = Type.GetType($"{typeName}, {assemblyName}", throwOnError: false);
            Assert.That(type, Is.Not.Null, $"Expected type {typeName} in {assemblyName}.");
            return type;
        }

        private static object CreateImpactPayload(
            Type payloadType,
            GameObject hitObject,
            float deliveredEnergyJoules,
            float impactSpeedMetersPerSecond,
            float projectileMassGrains,
            float damage)
        {
            return Activator.CreateInstance(
                payloadType,
                "weapon-kar98k",
                hitObject.transform.position,
                Vector3.back,
                damage,
                hitObject,
                (Vector3?)Vector3.zero,
                (Vector3?)Vector3.forward,
                impactSpeedMetersPerSecond,
                projectileMassGrains,
                deliveredEnergyJoules);
        }

        private static void InvokeApplyDamage(HumanoidDamageReceiver receiver, object payload)
        {
            var method = typeof(HumanoidDamageReceiver).GetMethod(
                nameof(HumanoidDamageReceiver.ApplyDamage),
                BindingFlags.Instance | BindingFlags.Public);

            Assert.That(method, Is.Not.Null, "Expected HumanoidDamageReceiver.ApplyDamage to exist.");
            method!.Invoke(receiver, new[] { payload });
        }
    }
}
