using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.DevTools.Data;
using Reloader.DevTools.Runtime;
using Reloader.NPCs.Combat;
using Reloader.NPCs.Generation;
using Reloader.NPCs.Runtime;
using Reloader.Weapons.World;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Reloader.DevTools.Tests.PlayMode
{
    public sealed class DevSpawnNpcCommandPlayModeTests
    {
        [UnityTest]
        public IEnumerator SpawnNpcCommand_SpawnsConfiguredPrefabAtCrosshairHitPoint()
        {
            var cameraGo = new GameObject("MainCamera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.transform.position = Vector3.zero;
            camera.transform.rotation = Quaternion.identity;

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = new Vector3(0f, 0f, 5f);
            target.transform.localScale = Vector3.one;

            var catalog = ScriptableObject.CreateInstance<DevNpcSpawnCatalog>();
            var prefab = new GameObject("NpcPolicePrefab");
            prefab.SetActive(false);
            catalog.SetEntriesForTests(new[]
            {
                new DevNpcSpawnCatalog.Entry("npc.police", "Police Officer", prefab)
            });

            var command = new DevSpawnNpcCommand(new DevNpcSpawnService(catalog), catalog);

            yield return null;

            var executed = command.TryExecute(DevCommandLineParser.Parse("spawn npc npc.police"), out var resultMessage);

            Assert.That(executed, Is.True);
            Assert.That(resultMessage, Does.Contain("npc.police"));

            var spawned = GameObject.Find("NpcPolicePrefab(Clone)");
            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.transform.position, Is.EqualTo(new Vector3(0f, 0f, 4.5f)).Using(Vector3EqualityComparer.Instance));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraGo);
            Object.DestroyImmediate(spawned);
        }

        [UnityTest]
        public IEnumerator SpawnNpcCommand_WithoutCrosshairHit_FallsBackInFrontOfCamera()
        {
            var cameraGo = new GameObject("MainCamera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.transform.position = new Vector3(1f, 2f, 3f);
            camera.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            var catalog = ScriptableObject.CreateInstance<DevNpcSpawnCatalog>();
            var prefab = new GameObject("NpcClerkPrefab");
            prefab.SetActive(false);
            catalog.SetEntriesForTests(new[]
            {
                new DevNpcSpawnCatalog.Entry("npc.front-desk-clerk", "Front Desk Clerk", prefab)
            });

            var command = new DevSpawnNpcCommand(new DevNpcSpawnService(catalog), catalog);

            yield return null;

            var executed = command.TryExecute(DevCommandLineParser.Parse("spawn npc npc.front-desk-clerk"), out var resultMessage);

            Assert.That(executed, Is.True);
            Assert.That(resultMessage, Does.Contain("npc.front-desk-clerk"));

            var spawned = GameObject.Find("NpcClerkPrefab(Clone)");
            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.transform.position, Is.EqualTo(new Vector3(4f, 2f, 3f)).Using(Vector3EqualityComparer.Instance));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(cameraGo);
            Object.DestroyImmediate(spawned);
        }

        [UnityTest]
        public IEnumerator SpawnNpcCommand_RandomSpawnsConfiguredCatalogEntry()
        {
            var cameraGo = new GameObject("MainCamera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.transform.position = Vector3.zero;
            camera.transform.rotation = Quaternion.identity;

            var catalog = ScriptableObject.CreateInstance<DevNpcSpawnCatalog>();
            var prefab = new GameObject("NpcRandomPrefab");
            prefab.SetActive(false);
            catalog.SetEntriesForTests(new[]
            {
                new DevNpcSpawnCatalog.Entry("npc.random-test", "Random Test", prefab)
            });

            var command = new DevSpawnNpcCommand(new DevNpcSpawnService(catalog), catalog);

            yield return null;

            var executed = command.TryExecute(DevCommandLineParser.Parse("spawn npc random"), out var resultMessage);

            Assert.That(executed, Is.True);
            Assert.That(resultMessage, Does.Contain("npc.random-test"));

            var spawned = GameObject.Find("NpcRandomPrefab(Clone)");
            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.transform.position, Is.EqualTo(new Vector3(0f, 0f, 3f)).Using(Vector3EqualityComparer.Instance));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(cameraGo);
            Object.DestroyImmediate(spawned);
        }

        [UnityTest]
        public IEnumerator SpawnNpcCommand_RandomContractSpawnsContractEligibleCivilianThroughBridge()
        {
            var cameraGo = new GameObject("MainCamera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.transform.position = Vector3.zero;
            camera.transform.rotation = Quaternion.identity;

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = new Vector3(0f, 0f, 6f);
            target.transform.localScale = Vector3.one;

            var catalog = ScriptableObject.CreateInstance<DevNpcSpawnCatalog>();
            var prefab = new GameObject("NpcRandomPrefab");
            prefab.SetActive(false);
            catalog.SetEntriesForTests(new[]
            {
                new DevNpcSpawnCatalog.Entry("npc.random-test", "Random Test", prefab)
            });

            var bridgeGo = new GameObject("CivilianPopulationRuntime");
            var bridge = bridgeGo.AddComponent<CivilianPopulationRuntimeBridge>();
            SetPrivateField(typeof(CivilianPopulationRuntimeBridge), bridge, "_npcActorPrefab", prefab);
            SetPrivateField(typeof(CivilianPopulationRuntimeBridge), bridge, "_appearanceLibrary", CreateAppearanceLibrary());

            var context = new DevCommandContext
            {
                SpawnCamera = camera,
                NpcSpawnCatalog = catalog,
                NpcSpawnService = new DevNpcSpawnService(catalog, camera),
                CivilianPopulationRuntimeBridge = bridge
            };

            var command = new DevSpawnNpcCommand();

            yield return null;

            var executed = command.TryExecute(context, DevCommandLineParser.Parse("spawn npc randomContract"), out var resultMessage);

            Assert.That(executed, Is.True);
            Assert.That(resultMessage, Does.Contain("contract-eligible"));

            var spawned = Object.FindFirstObjectByType<MainTownPopulationSpawnedCivilian>();
            Assert.That(spawned, Is.Not.Null, "Expected randomContract to spawn a procedural civilian through the bridge.");
            Assert.That(spawned!.transform.position, Is.EqualTo(new Vector3(0f, 0f, 5.5f)).Using(Vector3EqualityComparer.Instance));
            Assert.That(spawned.GetComponent<ContractTargetDamageable>(), Is.Not.Null);
            Assert.That(spawned.GetComponent<HumanoidDamageReceiver>(), Is.Not.Null);
            Assert.That(spawned.GetComponent<HumanoidRagdollController>(), Is.Not.Null);
            Assert.That(spawned.GetComponent<HumanoidCorpseLootController>(), Is.Not.Null);

            if (spawned != null)
            {
                Object.DestroyImmediate(spawned.gameObject);
            }

            Object.DestroyImmediate(target);
            Object.DestroyImmediate(bridgeGo);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(cameraGo);
        }

        private static CivilianAppearanceLibrary CreateAppearanceLibrary()
        {
            return new CivilianAppearanceLibrary
            {
                BaseBodyIds = new[] { "male.body" },
                PresentationTypes = new[] { "masculine" },
                HairIds = new[] { "hair.short" },
                HairColorIds = new[] { "hair.black" },
                EyebrowIds = new[] { "brous1" },
                BeardIds = new[] { "beard.none" },
                OutfitTopIds = new[] { "tshirt1" },
                OutfitBottomIds = new[] { "pants1" },
                OuterwearIds = new[] { "none" },
                MaterialColorIds = new[] { "style.default" },
                DescriptionTags = new[] { "debug" }
            };
        }

        private static void SetPrivateField(Type type, object target, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {type.Name}.");
            field!.SetValue(target, value);
        }

        private sealed class Vector3EqualityComparer : IEqualityComparer
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
