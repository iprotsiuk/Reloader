using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.NPCs.Tests.PlayMode
{
    public sealed class HumanoidBloodControllerPlayModeTests
    {
        private const string DefaultCatalogPath = "Assets/_Project/NPCs/Data/BloodVfxCatalog.asset";
        private const string NpcFoundationPrefabPath = "Assets/_Project/NPCs/Prefabs/NpcFoundation.prefab";
        private const string ProjectOwnedPrefabRoot = "Assets/_Project/NPCs/Prefabs/VFX/";
        private const string ProjectOwnedMaterialRoot = "Assets/_Project/NPCs/Materials/VFX/";
        private const string ThirdPartyBloodRoot = "Assets/HIVEMIND/RealisticBloodVFX/";

        [UnityTest]
        public IEnumerator NonLethalLightImpact_SpawnsLightImpactEffect()
        {
            var fixture = CreateRuntimeFixture();
            try
            {
                var hitZone = CreateHitZone(fixture.Root, "ArmZone", HumanoidBodyZone.ArmL);

                ApplyDamage(fixture.Receiver, hitZone, deliveredEnergyJoules: 100f);
                yield return null;

                Assert.That(fixture.Controller.LastRequestedEffectKind, Is.EqualTo(BloodEffectKind.LightImpact));
                AssertSpawnedPrefab(fixture.Controller.LastSpawnedEffect, fixture.LightPrefab, hitZone.transform.position);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [UnityTest]
        public IEnumerator NonLethalSeriousImpact_SpawnsHeavyImpactEffect()
        {
            var fixture = CreateRuntimeFixture();
            try
            {
                var hitZone = CreateHitZone(fixture.Root, "LegZone", HumanoidBodyZone.LegL);

                ApplyDamage(fixture.Receiver, hitZone, deliveredEnergyJoules: 3500f);
                yield return null;

                Assert.That(fixture.Receiver.IsDead, Is.False);
                Assert.That(fixture.Controller.LastRequestedEffectKind, Is.EqualTo(BloodEffectKind.HeavyImpact));
                AssertSpawnedPrefab(fixture.Controller.LastSpawnedEffect, fixture.HeavyPrefab, hitZone.transform.position);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [UnityTest]
        public IEnumerator NonLethalNeckImpact_SpawnsNeckImpactEffect()
        {
            var fixture = CreateRuntimeFixture();
            try
            {
                var hitZone = CreateHitZone(fixture.Root, "NeckZone", HumanoidBodyZone.Neck);

                ApplyDamage(fixture.Receiver, hitZone, deliveredEnergyJoules: 100f);
                yield return null;

                Assert.That(fixture.Receiver.IsDead, Is.False);
                Assert.That(fixture.Controller.LastRequestedEffectKind, Is.EqualTo(BloodEffectKind.NeckImpact));
                AssertSpawnedPrefab(fixture.Controller.LastSpawnedEffect, fixture.NeckPrefab, hitZone.transform.position);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [UnityTest]
        public IEnumerator Death_SpawnsDeathPuddleOnce()
        {
            var fixture = CreateRuntimeFixture();
            try
            {
                var hitZone = CreateHitZone(fixture.Root, "TorsoZone", HumanoidBodyZone.Torso);

                ApplyDamage(fixture.Receiver, hitZone, deliveredEnergyJoules: 1800f);
                yield return null;

                ApplyDamage(fixture.Receiver, hitZone, deliveredEnergyJoules: 1800f);
                yield return null;

                Assert.That(fixture.Receiver.IsDead, Is.True);
                Assert.That(fixture.Controller.DeathPuddleSpawnCount, Is.EqualTo(1));
                Assert.That(fixture.Controller.LastRequestedEffectKind, Is.EqualTo(BloodEffectKind.DeathPuddle));
                AssertSpawnedPrefab(fixture.Controller.LastSpawnedEffect, fixture.DeathPrefab, hitZone.transform.position);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [UnityTest]
        public IEnumerator NpcFoundationGameplayInstance_RequestsAndSpawnsBloodThroughDefaultCatalog()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NpcFoundationPrefabPath);
            var expectedCatalog = AssetDatabase.LoadAssetAtPath<BloodVfxCatalog>(DefaultCatalogPath);
            GameObject instance = null;

            try
            {
                Assert.That(prefab, Is.Not.Null, $"Expected NpcFoundation prefab at {NpcFoundationPrefabPath}.");
                Assert.That(expectedCatalog, Is.Not.Null, $"Expected default blood catalog at {DefaultCatalogPath}.");

                instance = UnityEngine.Object.Instantiate(prefab);
                instance.name = "NpcFoundationBloodGameplayInstance";

                var receiver = instance.GetComponent<HumanoidDamageReceiver>();
                var controller = instance.GetComponent<HumanoidBloodController>();
                var hitbox = instance.GetComponentInChildren<BodyZoneHitbox>(includeInactive: true);

                Assert.That(receiver, Is.Not.Null, "Expected gameplay NPC prefab instance to have HumanoidDamageReceiver.");
                Assert.That(controller, Is.Not.Null, "Expected gameplay NPC prefab instance to have HumanoidBloodController.");
                Assert.That(controller.enabled, Is.True, "Expected gameplay NPC blood controller to be enabled.");
                Assert.That(hitbox, Is.Not.Null, "Expected gameplay NPC prefab instance to have an authored body-zone hitbox.");

                ApplyDamage(receiver, hitbox.gameObject, deliveredEnergyJoules: 100f);
                yield return null;

                Assert.That(controller.HasRequestedEffect, Is.True);
                Assert.That(controller.LastSpawnedEffect, Is.Not.Null,
                    "Expected gameplay NPC prefab instance to spawn blood through its configured default catalog.");

                var catalogPrefab = expectedCatalog.GetRequiredDefaultPrefab(controller.LastRequestedEffectKind);
                Assert.That(catalogPrefab, Is.Not.Null);
                AssertSpawnedPrefab(controller.LastSpawnedEffect, catalogPrefab, hitbox.transform.position);
            }
            finally
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        }

        [Test]
        public void Catalog_WithMissingOptionalOverrides_FallsBackSoftlyToRequiredDefaults()
        {
            var fixture = CreateRuntimeFixture();
            try
            {
                Assert.DoesNotThrow(() =>
                {
                    Assert.That(fixture.Catalog.TryGetPrefab(BloodEffectKind.LightImpact, out var lightPrefab), Is.True);
                    Assert.That(lightPrefab, Is.SameAs(fixture.LightPrefab));

                    Assert.That(fixture.Catalog.TryGetPrefab(BloodEffectKind.HeavyImpact, out var heavyPrefab), Is.True);
                    Assert.That(heavyPrefab, Is.SameAs(fixture.HeavyPrefab));

                    Assert.That(fixture.Catalog.TryGetPrefab(BloodEffectKind.NeckImpact, out var neckPrefab), Is.True);
                    Assert.That(neckPrefab, Is.SameAs(fixture.NeckPrefab));

                    Assert.That(fixture.Catalog.TryGetPrefab(BloodEffectKind.DeathPuddle, out var deathPrefab), Is.True);
                    Assert.That(deathPrefab, Is.SameAs(fixture.DeathPrefab));
                });
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void Catalog_WithMissingProjectOwnedRequiredDefault_FailsValidation()
        {
            var catalog = ScriptableObject.CreateInstance<BloodVfxCatalog>();
            var lightPrefab = CreateEffectPrefab("BloodLightRequiredOnly");
            try
            {
                catalog.ConfigureRequiredDefaultsForTests(
                    lightPrefab,
                    heavyImpactDefaultPrefab: null,
                    neckImpactDefaultPrefab: null,
                    deathPuddleDefaultPrefab: null);

                Assert.That(catalog.ValidateRequiredDefaults(out var error), Is.False);
                Assert.That(error, Does.Contain(nameof(BloodEffectKind.HeavyImpact)));
                Assert.That(error, Does.Contain(nameof(BloodEffectKind.NeckImpact)));
                Assert.That(error, Does.Contain(nameof(BloodEffectKind.DeathPuddle)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lightPrefab);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void DefaultCatalog_UsesProjectOwnedRedReadablePrefabDefaults()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BloodVfxCatalog>(DefaultCatalogPath);
            Assert.That(catalog, Is.Not.Null, $"Expected default blood catalog at {DefaultCatalogPath}.");
            Assert.That(catalog.ValidateRequiredDefaults(out var validationError), Is.True, validationError);

            foreach (BloodEffectKind kind in Enum.GetValues(typeof(BloodEffectKind)))
            {
                var prefab = catalog.GetRequiredDefaultPrefab(kind);
                Assert.That(prefab, Is.Not.Null, $"Expected a required default prefab for {kind}.");

                var prefabPath = AssetDatabase.GetAssetPath(prefab);
                Assert.That(prefabPath, Does.StartWith(ProjectOwnedPrefabRoot), $"{kind} default prefab must be project-owned.");
                Assert.That(prefabPath, Does.Not.StartWith(ThirdPartyBloodRoot), $"{kind} default prefab must not point at RealisticBloodVFX.");

                var renderers = prefab.GetComponentsInChildren<Renderer>(includeInactive: true);
                Assert.That(renderers, Is.Not.Empty, $"{kind} default prefab should have a visible red placeholder renderer.");

                var foundProjectOwnedRedMaterial = false;
                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    var materials = renderers[rendererIndex].sharedMaterials;
                    for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        var material = materials[materialIndex];
                        if (material == null)
                        {
                            continue;
                        }

                        var materialPath = AssetDatabase.GetAssetPath(material);
                        Assert.That(materialPath, Does.StartWith(ProjectOwnedMaterialRoot), $"{kind} material must be project-owned.");
                        Assert.That(materialPath, Does.Not.StartWith(ThirdPartyBloodRoot), $"{kind} material must not point at RealisticBloodVFX.");

                        var color = material.HasProperty("_BaseColor")
                            ? material.GetColor("_BaseColor")
                            : material.color;
                        if (color.r >= 0.45f && color.g <= 0.16f && color.b <= 0.16f && color.a >= 0.5f)
                        {
                            foundProjectOwnedRedMaterial = true;
                        }
                    }
                }

                Assert.That(foundProjectOwnedRedMaterial, Is.True, $"{kind} default should use a readable red project-owned material.");
            }
        }

        private static BloodRuntimeFixture CreateRuntimeFixture()
        {
            var fixture = new BloodRuntimeFixture();
            fixture.LightPrefab = CreateEffectPrefab("BloodLightImpact_TestPrefab");
            fixture.HeavyPrefab = CreateEffectPrefab("BloodHeavyImpact_TestPrefab");
            fixture.NeckPrefab = CreateEffectPrefab("BloodNeckImpact_TestPrefab");
            fixture.DeathPrefab = CreateEffectPrefab("BloodDeathPuddle_TestPrefab");

            fixture.Catalog = ScriptableObject.CreateInstance<BloodVfxCatalog>();
            fixture.Catalog.ConfigureRequiredDefaultsForTests(
                fixture.LightPrefab,
                fixture.HeavyPrefab,
                fixture.NeckPrefab,
                fixture.DeathPrefab);

            fixture.Root = new GameObject("BloodControllerNpcRoot");
            fixture.Root.AddComponent<HumanoidHitboxRig>();
            fixture.Receiver = fixture.Root.AddComponent<HumanoidDamageReceiver>();
            fixture.Controller = fixture.Root.AddComponent<HumanoidBloodController>();
            fixture.Controller.SetCatalogForTests(fixture.Catalog);
            return fixture;
        }

        private static GameObject CreateHitZone(GameObject root, string name, HumanoidBodyZone zone)
        {
            var hitZone = new GameObject(name);
            hitZone.transform.SetParent(root.transform, false);
            hitZone.transform.position = new Vector3(1f, 2f, 3f);
            hitZone.AddComponent<SphereCollider>();
            hitZone.AddComponent<BodyZoneHitbox>().Configure(zone);
            return hitZone;
        }

        private static GameObject CreateEffectPrefab(string name)
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            prefab.name = name;
            var renderer = prefab.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = new Color(0.75f, 0.02f, 0.01f, 1f)
            };
            prefab.SetActive(false);
            return prefab;
        }

        private static void ApplyDamage(HumanoidDamageReceiver receiver, GameObject hitObject, float deliveredEnergyJoules)
        {
            var payloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            Assert.That(payloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");
            var payload = Activator.CreateInstance(
                payloadType!,
                "weapon-kar98k",
                hitObject.transform.position,
                Vector3.back,
                1f,
                hitObject,
                hitObject.transform.position + (Vector3.back * 25f),
                Vector3.forward,
                0f,
                0f,
                deliveredEnergyJoules);

            var method = receiver.GetType().GetMethod(
                "ApplyDamage",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { payloadType! },
                modifiers: null);
            Assert.That(method, Is.Not.Null, "Expected HumanoidDamageReceiver.ApplyDamage to exist.");
            method!.Invoke(receiver, new[] { payload });
        }

        private static void AssertSpawnedPrefab(GameObject spawned, GameObject prefab, Vector3 expectedPosition)
        {
            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.name, Does.StartWith(prefab.name));
            Assert.That(Vector3.Distance(spawned.transform.position, expectedPosition), Is.LessThan(0.001f));
            Assert.That(spawned.activeInHierarchy, Is.True);
        }

        private static Type ResolveType(string fullName, string assemblyName)
        {
            var direct = Type.GetType($"{fullName}, {assemblyName}", throwOnError: false);
            if (direct != null)
            {
                return direct;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType(fullName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private sealed class BloodRuntimeFixture
        {
            public GameObject Root;
            public HumanoidDamageReceiver Receiver;
            public HumanoidBloodController Controller;
            public BloodVfxCatalog Catalog;
            public GameObject LightPrefab;
            public GameObject HeavyPrefab;
            public GameObject NeckPrefab;
            public GameObject DeathPrefab;

            public void Destroy()
            {
                UnityEngine.Object.DestroyImmediate(Root);
                UnityEngine.Object.DestroyImmediate(LightPrefab);
                UnityEngine.Object.DestroyImmediate(HeavyPrefab);
                UnityEngine.Object.DestroyImmediate(NeckPrefab);
                UnityEngine.Object.DestroyImmediate(DeathPrefab);
                UnityEngine.Object.DestroyImmediate(Catalog);

                foreach (var controller in UnityEngine.Object.FindObjectsByType<HumanoidBloodController>(FindObjectsSortMode.None))
                {
                    UnityEngine.Object.DestroyImmediate(controller.gameObject);
                }
            }
        }
    }
}
