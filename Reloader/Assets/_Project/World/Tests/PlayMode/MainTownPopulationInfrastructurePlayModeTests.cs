using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Runtime;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;
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
            var actorPrefabField = bridgeType.GetField("_npcActorPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(definitionField, Is.Not.Null);
            Assert.That(libraryField, Is.Not.Null);
            Assert.That(actorPrefabField, Is.Not.Null);

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

            var actorPrefab = actorPrefabField!.GetValue(bridge) as GameObject;
            Assert.That(actorPrefab, Is.Not.Null, "Expected MainTown to assign an authored NPC actor prefab to the population bridge.");
            Assert.That(actorPrefab!.GetComponent<NpcAgent>(), Is.Not.Null, "Expected the assigned actor prefab to carry the NPC foundation contract.");

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
            Assert.That(metadata.All(component => component.transform.Find("Body/NpcModel") != null), Is.True,
                "Expected starter civilians to instantiate the authored NPC actor prefab instead of ad-hoc shell objects.");
            Assert.That(metadata.Select(component => component.transform.position).Distinct().Count(), Is.EqualTo(4),
                "Expected authored population slot anchors to occupy distinct scene positions.");

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

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_RebuildScenePopulation_SameFrameLookupsOnlySeeReplacementCivilian()
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

            bridge.RebuildScenePopulation();
            yield return null;

            Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.0001", out _), Is.True,
                "Expected the initial rebuild to resolve the first civilian.");

            bridge.Runtime.Civilians.Clear();
            bridge.Runtime.Civilians.Add(CreateRecord(
                civilianId: "citizen.mainTown.0002",
                populationSlotId: "cops.001",
                poolId: "cops",
                spawnAnchorId: "Anchor_Cop_01",
                areaTag: "maintown.watch",
                isAlive: true,
                retiredAtDay: -1));

            bridge.RebuildScenePopulation();

            var spawned = root.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            Assert.That(spawned.Select(component => component.CivilianId).ToArray(), Is.EqualTo(new[] { "citizen.mainTown.0002" }),
                "Expected same-frame bridge children to exclude civilians scheduled for deferred destruction.");
            Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.0001", out _), Is.False,
                "Expected same-frame lookups to stop resolving the prior civilian after rebuild.");
            Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.0002", out var resolved), Is.True,
                "Expected same-frame lookups to resolve the replacement civilian immediately after rebuild.");
            Assert.That(resolved!.CivilianId, Is.EqualTo("citizen.mainTown.0002"));
        }

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_SaveCoordinatorLoad_RebuildsAuthoredSceneFromLoadedPopulationModule()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = root!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");

            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var repository = new SaveFileRepository();
            var tempDir = Path.Combine(Path.GetTempPath(), "reloader-maintown-population-tests-" + Guid.NewGuid().ToString("N"));
            var savePath = Path.Combine(tempDir, "maintown-population-load.json");
            Directory.CreateDirectory(tempDir);

            try
            {
                var envelope = coordinator.CaptureEnvelope("0.6.0-dev");
                var module = new CivilianPopulationModule();
                module.Civilians.Add(CreateRecord(
                    civilianId: "citizen.mainTown.9001",
                    populationSlotId: "cops.001",
                    poolId: "cops",
                    spawnAnchorId: "Anchor_Cop_01",
                    areaTag: "maintown.watch",
                    isAlive: true,
                    retiredAtDay: -1));
                module.Civilians.Add(CreateRecord(
                    civilianId: "citizen.mainTown.9002",
                    populationSlotId: "hobos.001",
                    poolId: "hobos",
                    spawnAnchorId: "Anchor_Hobo_01",
                    areaTag: "maintown.alley",
                    isAlive: false,
                    retiredAtDay: 4));

                envelope.Modules["CivilianPopulation"] = new ModuleSaveBlock
                {
                    ModuleVersion = 2,
                    PayloadJson = module.CaptureModuleStateJson()
                };

                repository.WriteEnvelope(savePath, envelope);

                Assert.That(root.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true).Length, Is.EqualTo(4));

                coordinator.Load(savePath);
                yield return null;

                Assert.That(bridge!.Runtime.Civilians.Count, Is.EqualTo(2), "Expected runtime civilians to reflect the loaded save module.");

                var loadedCivilian = bridge.Runtime.Civilians.Single(record => record.CivilianId == "citizen.mainTown.9001");
                Assert.That(loadedCivilian.FirstName, Is.EqualTo("Orson"));
                Assert.That(loadedCivilian.LastName, Is.EqualTo("Vale"));

                var spawned = root.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
                Assert.That(spawned.Length, Is.EqualTo(1), "Expected only living civilians from the loaded save to rebuild into the scene.");
                Assert.That(spawned[0].CivilianId, Is.EqualTo("citizen.mainTown.9001"));
                Assert.That(spawned[0].PopulationSlotId, Is.EqualTo("cops.001"));
                Assert.That(spawned[0].PoolId, Is.EqualTo("cops"));

                var anchor = root.transform.Find("Anchor_Cop_01");
                Assert.That(anchor, Is.Not.Null, "Expected authored anchor for loaded civilian.");
                Assert.That(spawned[0].transform.position, Is.EqualTo(anchor!.position));
                Assert.That(root.transform.Find("Civilian_citizen.mainTown.0001"), Is.Null, "Expected starter civilians to be cleared when a save load rebuild runs.");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_ExecutePendingReplacements_AfterMondayRefresh_RebuildsStableSlotWithNewCivilian()
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
                civilianId: "citizen.mainTown.0007",
                populationSlotId: "townsfolk.001",
                poolId: "townsfolk",
                spawnAnchorId: "Anchor_Townsfolk_01",
                areaTag: "maintown.square",
                isAlive: false,
                retiredAtDay: 9));
            bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
            {
                VacatedCivilianId = "citizen.mainTown.0007",
                QueuedAtDay = 9,
                SpawnAnchorId = "Anchor_Townsfolk_01"
            });

            var replacedCount = bridge.ExecutePendingReplacements(currentDay: 14, currentTimeOfDay: 8f);
            yield return null;

            Assert.That(replacedCount, Is.EqualTo(1));
            Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));

            var replacement = bridge.Runtime.Civilians.Single(record => record.CivilianId == "citizen.mainTown.0008");
            Assert.That(replacement.PopulationSlotId, Is.EqualTo("townsfolk.001"));
            Assert.That(replacement.PoolId, Is.EqualTo("townsfolk"));
            Assert.That(replacement.SpawnAnchorId, Is.EqualTo("Anchor_Townsfolk_01"));
            Assert.That(replacement.CreatedAtDay, Is.EqualTo(14));
            Assert.That(replacement.FirstName, Is.Not.Empty);
            Assert.That(replacement.LastName, Is.Not.Empty);
            Assert.That(
                string.Concat(replacement.FirstName, " ", replacement.LastName),
                Is.Not.EqualTo("Derek Mullen"),
                "Expected Monday replacement to become a different persistent person, not a cloned identity.");

            var spawned = root.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            Assert.That(spawned.Length, Is.EqualTo(1));
            Assert.That(spawned[0].CivilianId, Is.EqualTo("citizen.mainTown.0008"));
            Assert.That(spawned[0].PopulationSlotId, Is.EqualTo("townsfolk.001"));

            var anchor = root.transform.Find("Anchor_Townsfolk_01");
            Assert.That(anchor, Is.Not.Null);
            Assert.That(spawned[0].transform.position, Is.EqualTo(anchor!.position));
        }

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_WorldStateChanged_ExecutesReplacementWhenMondayRefreshTimeArrives()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = root!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");

            var coreWorldController = UnityEngine.Object.FindFirstObjectByType<CoreWorldController>(FindObjectsInactive.Include);
            Assert.That(coreWorldController, Is.Not.Null, "Expected CoreWorldController in MainTown.");

            bridge!.Runtime.Civilians.Clear();
            bridge.Runtime.PendingReplacements.Clear();
            bridge.Runtime.Civilians.Add(CreateRecord(
                civilianId: "citizen.mainTown.0007",
                populationSlotId: "townsfolk.001",
                poolId: "townsfolk",
                spawnAnchorId: "Anchor_Townsfolk_01",
                areaTag: "maintown.square",
                isAlive: false,
                retiredAtDay: 9));
            bridge.SetCoreWorldController(coreWorldController!);

            bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
            {
                VacatedCivilianId = "citizen.mainTown.0007",
                QueuedAtDay = 9,
                SpawnAnchorId = "Anchor_Townsfolk_01"
            });

            coreWorldController!.SetWorldState(14, 7.5f);
            yield return null;
            coreWorldController.SetWorldState(14, 7.75f);
            yield return null;

            Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(1), "Expected pre-refresh Monday world updates to keep replacement debt pending.");
            Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(1), "Expected no replacement until Monday 08:00 arrives.");

            coreWorldController.SetWorldState(14, 8f);
            yield return null;

            Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));

            var replacement = bridge.Runtime.Civilians.Single(record => record.CivilianId == "citizen.mainTown.0008");
            Assert.That(replacement.PopulationSlotId, Is.EqualTo("townsfolk.001"));
            Assert.That(replacement.SpawnAnchorId, Is.EqualTo("Anchor_Townsfolk_01"));
            Assert.That(replacement.CreatedAtDay, Is.EqualTo(14));

            var spawned = root.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            Assert.That(spawned.Length, Is.EqualTo(1));
            Assert.That(spawned[0].CivilianId, Is.EqualTo("citizen.mainTown.0008"));
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
                FirstName = spawnAnchorId == "Anchor_Cop_01" ? "Orson" : "Derek",
                LastName = spawnAnchorId == "Anchor_Cop_01" ? "Vale" : "Mullen",
                Nickname = spawnAnchorId == "Anchor_Hobo_01" ? "Tincan" : string.Empty,
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
