using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Combat;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.NPCs.Tests.PlayMode
{
    public class HumanoidBloodPrefabSpawnPlayModeTests
    {
        [UnityTest]
        public IEnumerator ImpactEffect_WithLoopingParticlePrefab_ReconfiguresSpawnedInstanceToOneShot()
        {
            GameObject npcRoot = null;
            GameObject zoneObject = null;
            GameObject effectPrefab = null;
            BloodVfxCatalog catalog = null;
            try
            {
                npcRoot = new GameObject("NpcRoot");
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                var controller = npcRoot.AddComponent<HumanoidBloodController>();

                zoneObject = new GameObject("TorsoZone");
                zoneObject.transform.SetParent(npcRoot.transform, false);
                zoneObject.AddComponent<BoxCollider>();
                zoneObject.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Torso);

                effectPrefab = new GameObject("ImpactFxPrefab");
                var particleSystem = effectPrefab.AddComponent<ParticleSystem>();
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particleSystem.main;
                main.playOnAwake = false;
                main.loop = true;
                main.duration = 1f;
                main.startLifetime = 0.4f;

                catalog = ScriptableObject.CreateInstance<BloodVfxCatalog>();
                AssignCatalogEntry(catalog, BloodEffectKind.TorsoImpact, effectPrefab);
                AssignCatalog(controller, catalog);

                yield return null;

                InvokeApplyDamage(receiver, CreateImpactPayload(
                    itemId: "weapon-kar98k",
                    point: zoneObject.transform.position,
                    normal: Vector3.up,
                    damage: 1f,
                    hitObject: zoneObject,
                    sourcePoint: zoneObject.transform.position + (Vector3.back * 10f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 120f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 100f));

                yield return null;

                var spawnedSystem = FindSpawnedParticleSystem(effectPrefab, zoneObject.transform.position);
                Assert.That(spawnedSystem, Is.Not.Null, "Expected HumanoidBloodController to instantiate the configured blood VFX prefab.");
                Assert.That(spawnedSystem!.main.loop, Is.False,
                    "Expected looping package blood prefabs to be reconfigured to one-shot runtime effects instead of looping forever.");
            }
            finally
            {
                if (catalog != null)
                {
                    UnityEngine.Object.Destroy(catalog);
                }

                if (effectPrefab != null)
                {
                    UnityEngine.Object.Destroy(effectPrefab);
                }

                if (zoneObject != null)
                {
                    UnityEngine.Object.Destroy(zoneObject);
                }

                if (npcRoot != null)
                {
                    UnityEngine.Object.Destroy(npcRoot);
                }
            }
        }

        private static ParticleSystem FindSpawnedParticleSystem(GameObject prefab, Vector3 expectedPosition)
        {
            var systems = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            for (var i = 0; i < systems.Length; i++)
            {
                var system = systems[i];
                if (system == null || system.gameObject == prefab)
                {
                    continue;
                }

                if (!string.Equals(system.gameObject.name, $"{prefab.name}(Clone)", StringComparison.Ordinal))
                {
                    continue;
                }

                if ((system.transform.position - expectedPosition).sqrMagnitude > 0.0001f)
                {
                    continue;
                }

                return system;
            }

            return null;
        }

        private static void AssignCatalog(HumanoidBloodController controller, BloodVfxCatalog catalog)
        {
            var field = typeof(HumanoidBloodController).GetField("_catalog", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Expected HumanoidBloodController to define a private _catalog field.");
            field!.SetValue(controller, catalog);
        }

        private static void AssignCatalogEntry(BloodVfxCatalog catalog, BloodEffectKind kind, GameObject prefab)
        {
            var catalogType = typeof(BloodVfxCatalog);
            var entryType = catalogType.GetNestedType("BloodEffectEntry", BindingFlags.NonPublic);
            Assert.That(entryType, Is.Not.Null, "Expected private BloodEffectEntry struct on BloodVfxCatalog.");

            var entry = Activator.CreateInstance(entryType!);
            entryType!.GetField("Kind", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(entry, kind);
            entryType.GetField("Prefab", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(entry, prefab);

            var entries = Array.CreateInstance(entryType, 1);
            entries.SetValue(entry, 0);

            var field = catalogType.GetField("_effectEntries", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Expected BloodVfxCatalog to define a private _effectEntries field.");
            field!.SetValue(catalog, entries);
        }

        private static object CreateImpactPayload(
            string itemId,
            Vector3 point,
            Vector3 normal,
            float damage,
            GameObject hitObject,
            Vector3? sourcePoint,
            Vector3? direction,
            float impactSpeedMetersPerSecond,
            float projectileMassGrains,
            float deliveredEnergyJoules)
        {
            var payloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            Assert.That(payloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

            return Activator.CreateInstance(
                payloadType!,
                itemId,
                point,
                normal,
                damage,
                hitObject,
                sourcePoint,
                direction,
                impactSpeedMetersPerSecond,
                projectileMassGrains,
                deliveredEnergyJoules);
        }

        private static void InvokeApplyDamage(HumanoidDamageReceiver receiver, object payload)
        {
            var method = typeof(HumanoidDamageReceiver).GetMethod(
                "ApplyDamage",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { payload.GetType() },
                modifiers: null);
            Assert.That(method, Is.Not.Null, "Expected HumanoidDamageReceiver.ApplyDamage to exist.");
            method!.Invoke(receiver, new[] { payload });
        }

        private static Type ResolveType(string fullName, string assemblyName)
        {
            var type = Type.GetType($"{fullName}, {assemblyName}", throwOnError: false);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
