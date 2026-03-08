using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Reloader.World.Tests.PlayMode
{
    public class MainTownPopulationInfrastructurePlayModeTests
    {
        private const string MainTownSceneName = "MainTown";
        private const float SceneSwitchTimeoutSeconds = 8f;

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_HasPopulationDefinitionAndStarterPoolsConfigured()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = root!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");

            var bridgeType = typeof(CivilianPopulationRuntimeBridge);
            var definitionField = bridgeType.GetField("_populationDefinition", BindingFlags.Instance | BindingFlags.NonPublic);
            var libraryField = bridgeType.GetField("_appearanceLibrary", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(definitionField, Is.Not.Null);
            Assert.That(libraryField, Is.Not.Null);

            var definition = definitionField!.GetValue(bridge);
            Assert.That(definition, Is.Not.Null, "Expected MainTown population definition asset to be assigned.");

            var validateMethod = definition!.GetType().GetMethod("Validate", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(validateMethod, Is.Not.Null);
            Assert.DoesNotThrow(() => validateMethod!.Invoke(definition, null));

            var poolsProperty = definition.GetType().GetProperty("Pools", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(poolsProperty, Is.Not.Null);
            var pools = poolsProperty!.GetValue(definition) as System.Array;
            Assert.That(pools, Is.Not.Null);
            Assert.That(pools!.Length, Is.EqualTo(4), "Expected conservative starter pool count for MainTown.");

            var poolIds = pools.Cast<object>()
                .Select(pool => pool.GetType().GetProperty("PoolId", BindingFlags.Instance | BindingFlags.Public)!.GetValue(pool) as string)
                .ToArray();

            CollectionAssert.AreEquivalent(new[] { "townsfolk", "quarry_workers", "hobos", "cops" }, poolIds);

            var library = libraryField!.GetValue(bridge);
            Assert.That(library, Is.Not.Null, "Expected starter appearance library data to be serialized on the bridge.");

            AssertArrayConfigured(library!, "BaseBodyIds");
            AssertArrayConfigured(library!, "PresentationTypes");
            AssertArrayConfigured(library!, "HairIds");
            AssertArrayConfigured(library!, "HairColorIds");
            AssertArrayConfigured(library!, "BeardIds");
            AssertArrayConfigured(library!, "OutfitTopIds");
            AssertArrayConfigured(library!, "OutfitBottomIds");
            AssertArrayConfigured(library!, "OuterwearIds");
        }

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_LoadScene_AutomaticallySeedsAndBuildsStarterPopulation()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = root!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");

            Assert.That(bridge!.Runtime.Civilians.Count, Is.EqualTo(4), "Expected automatic starter population seeding from the authored definition.");

            var spawnedAgents = root.GetComponentsInChildren<NpcAgent>(includeInactive: true);
            Assert.That(spawnedAgents.Length, Is.EqualTo(4), "Expected automatic runtime rebuild to spawn one placeholder civilian per starter slot.");

            var metadata = root.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            Assert.That(metadata.Length, Is.EqualTo(4), "Expected every auto-spawned civilian to carry slot metadata.");

            CollectionAssert.AreEquivalent(
                new[] { "townsfolk.001", "quarry_workers.001", "hobos.001", "cops.001" },
                metadata.Select(component => component.PopulationSlotId).ToArray());
        }

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_RebuildScenePopulation_SpawnsLiveOccupantsAndSkipsDeadSlots()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = root!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");

            bridge!.Runtime.Civilians.Clear();
            bridge.Runtime.PendingReplacements.Clear();
            bridge.Runtime.Civilians.Add(CreateRecord(
                civilianId: "citizen.mainTown.0001",
                populationSlotId: "townsfolk.001",
                poolId: "townsfolk",
                spawnAnchorId: "Anchor_Townsfolk_01",
                areaTag: "maintown.square",
                isAlive: true,
                retiredAtDay: -1));
            bridge.Runtime.Civilians.Add(CreateRecord(
                civilianId: "citizen.mainTown.0002",
                populationSlotId: "cops.001",
                poolId: "cops",
                spawnAnchorId: "Anchor_Cop_01",
                areaTag: "maintown.watch",
                isAlive: false,
                retiredAtDay: 2));

            var bridgeType = typeof(CivilianPopulationRuntimeBridge);
            var rebuildMethod = bridgeType.GetMethod("RebuildScenePopulation", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(rebuildMethod, Is.Not.Null, "Expected a public RebuildScenePopulation() method.");

            Assert.DoesNotThrow(() => rebuildMethod!.Invoke(bridge, null));
            yield return null;

            var spawnedAgents = root.GetComponentsInChildren<NpcAgent>(includeInactive: true);
            Assert.That(spawnedAgents.Length, Is.EqualTo(1), "Expected only live civilian slots to produce runtime NPCs.");

            var metadataType = Type.GetType("Reloader.NPCs.Runtime.MainTownPopulationSpawnedCivilian, Reloader.NPCs", throwOnError: false);
            Assert.That(metadataType, Is.Not.Null, "Expected a runtime component carrying spawned civilian slot metadata.");

            var metadata = spawnedAgents[0].GetComponent(metadataType!);
            Assert.That(metadata, Is.Not.Null, "Expected spawned NPC to carry slot metadata.");
            Assert.That(GetProperty<string>(metadata!, "PopulationSlotId"), Is.EqualTo("townsfolk.001"));
            Assert.That(GetProperty<string>(metadata!, "CivilianId"), Is.EqualTo("citizen.mainTown.0001"));
            Assert.That(GetProperty<string>(metadata!, "PoolId"), Is.EqualTo("townsfolk"));

            var anchor = root.transform.Find("Anchor_Townsfolk_01");
            Assert.That(anchor, Is.Not.Null, "Expected authored spawn anchor to exist.");
            Assert.That(spawnedAgents[0].transform.position, Is.EqualTo(anchor!.position));
        }

        private static void AssertArrayConfigured(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}'.");
            var values = property!.GetValue(instance) as System.Array;
            Assert.That(values, Is.Not.Null, $"Expected '{propertyName}' to be an array.");
            Assert.That(values!.Length, Is.GreaterThan(0), $"Expected '{propertyName}' to have at least one configured value.");
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

            var elapsed = 0f;
            while (elapsed < SceneSwitchTimeoutSeconds)
            {
                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.IsValid() && activeScene.isLoaded && activeScene.name == sceneName)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.Fail($"Timed out waiting for active scene '{sceneName}'.");
        }

        private static CivilianPopulationRecord CreateRecord(
            string civilianId,
            string populationSlotId,
            string poolId,
            string spawnAnchorId,
            string areaTag,
            bool isAlive,
            int retiredAtDay)
        {
            return new CivilianPopulationRecord
            {
                CivilianId = civilianId,
                PopulationSlotId = populationSlotId,
                PoolId = poolId,
                IsAlive = isAlive,
                IsContractEligible = isAlive,
                IsProtectedFromContracts = false,
                BaseBodyId = "body.male.a",
                PresentationType = "masculine",
                HairId = "hair.short.01",
                HairColorId = "hair.black",
                BeardId = "beard.none",
                OutfitTopId = "top.coat.01",
                OutfitBottomId = "bottom.jeans.01",
                OuterwearId = "outer.gray.coat",
                MaterialColorIds = new System.Collections.Generic.List<string> { "color.gray" },
                GeneratedDescriptionTags = new System.Collections.Generic.List<string> { "gray coat" },
                SpawnAnchorId = spawnAnchorId,
                AreaTag = areaTag,
                CreatedAtDay = 0,
                RetiredAtDay = retiredAtDay
            };
        }

        private static T GetProperty<T>(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}'.");
            return (T)property!.GetValue(instance);
        }
    }
}
