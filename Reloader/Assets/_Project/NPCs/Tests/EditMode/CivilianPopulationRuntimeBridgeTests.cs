using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Reloader.Core.Runtime;
using Reloader.Core.Save;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Generation;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Capabilities;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class CivilianPopulationRuntimeBridgeTests
    {
        [Test]
        public void PrepareForSave_WhenRuntimeAndModuleAreEmpty_SeedsInitialRoster()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();

            try
            {
                ConfigureBridge(
                    bridge,
                    initialPopulationCount: 3,
                    idPrefix: "citizen.mainTown",
                    spawnAnchorIds: new[] { "spawn.busstop.a" },
                    library: CreateLibrary());

                var module = new CivilianPopulationModule();
                bridge.PrepareForSave(new[] { new SaveModuleRegistration(1, module) });

                Assert.That(module.Civilians.Count, Is.EqualTo(3));
                Assert.That(module.PendingReplacements.Count, Is.EqualTo(0));
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(3));
                Assert.That(module.Civilians.Select(record => record.CivilianId), Is.EqualTo(new[]
                {
                    "citizen.mainTown.0001",
                    "citizen.mainTown.0002",
                    "citizen.mainTown.0003"
                }));
                Assert.That(module.Civilians.All(record => record.SpawnAnchorId == "spawn.busstop.a"), Is.True);
                Assert.That(module.Civilians.Select(record => record.PopulationSlotId), Is.EqualTo(new[]
                {
                    "seeded.maintown.0001",
                    "seeded.maintown.0002",
                    "seeded.maintown.0003"
                }));
                Assert.That(module.Civilians.All(record => record.PoolId == "townsfolk"), Is.True);
                Assert.That(module.Civilians.All(record => record.AreaTag == "maintown"), Is.True);
                Assert.That(module.Civilians.All(record => record.IsProtectedFromContracts == false), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FinalizeAfterLoad_RestoresRuntimeFromModule()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();

            try
            {
                var module = new CivilianPopulationModule();
                module.Civilians.Add(new CivilianPopulationRecord
                {
                    PopulationSlotId = "quarry.worker.004",
                    PoolId = "quarry_workers",
                    CivilianId = "citizen.mainTown.0042",
                    IsAlive = true,
                    IsContractEligible = false,
                    IsProtectedFromContracts = true,
                    BaseBodyId = "body.female.a",
                    PresentationType = "feminine",
                    HairId = "hair.long.01",
                    HairColorId = "hair.red",
                    BeardId = "beard.none",
                    OutfitTopId = "top.jacket.01",
                    OutfitBottomId = "bottom.jeans.01",
                    OuterwearId = "outer.red.jacket",
                    MaterialColorIds = new List<string> { "color.red" },
                    GeneratedDescriptionTags = new List<string> { "red jacket" },
                    SpawnAnchorId = "spawn.busstop.b",
                    AreaTag = "quarry",
                    CreatedAtDay = 6,
                    RetiredAtDay = -1
                });
                module.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0004",
                    QueuedAtDay = 7,
                    SpawnAnchorId = "spawn.busstop.b"
                });

                bridge.FinalizeAfterLoad(new[] { new SaveModuleRegistration(1, module) });

                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(1));
                Assert.That(bridge.Runtime.Civilians[0].PopulationSlotId, Is.EqualTo("quarry.worker.004"));
                Assert.That(bridge.Runtime.Civilians[0].PoolId, Is.EqualTo("quarry_workers"));
                Assert.That(bridge.Runtime.Civilians[0].CivilianId, Is.EqualTo("citizen.mainTown.0042"));
                Assert.That(bridge.Runtime.Civilians[0].IsContractEligible, Is.False);
                Assert.That(bridge.Runtime.Civilians[0].IsProtectedFromContracts, Is.True);
                Assert.That(bridge.Runtime.Civilians[0].GeneratedDescriptionTags, Is.EqualTo(new[] { "red jacket" }));
                Assert.That(bridge.Runtime.Civilians[0].AreaTag, Is.EqualTo("quarry"));
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(1));
                Assert.That(bridge.Runtime.PendingReplacements[0].VacatedCivilianId, Is.EqualTo("citizen.mainTown.0004"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FinalizeAfterLoad_RebuildsScenePopulationFromLoadedModuleAndClearsPriorSpawnedObjects()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();
            CreateAnchor(go.transform, "Anchor_A", new Vector3(1f, 0f, 0f));
            var anchorB = CreateAnchor(go.transform, "Anchor_B", new Vector3(4f, 0f, 0f));

            try
            {
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.stale",
                    PopulationSlotId = "stale.001",
                    PoolId = "townsfolk",
                    SpawnAnchorId = "Anchor_A",
                    AreaTag = "maintown.old",
                    IsAlive = true
                });

                bridge.RebuildScenePopulation();
                Assert.That(go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true).Length, Is.EqualTo(1));

                var module = new CivilianPopulationModule();
                module.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0042",
                    PopulationSlotId = "cops.001",
                    PoolId = "cops",
                    SpawnAnchorId = "Anchor_B",
                    AreaTag = "maintown.watch",
                    IsAlive = true
                });
                module.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0043",
                    PopulationSlotId = "hobos.001",
                    PoolId = "hobos",
                    SpawnAnchorId = "Anchor_A",
                    AreaTag = "maintown.alley",
                    IsAlive = false
                });

                bridge.FinalizeAfterLoad(new[] { new SaveModuleRegistration(1, module) });

                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(2));

                var spawned = go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
                Assert.That(spawned.Length, Is.EqualTo(1), "Expected only live loaded civilians to rebuild into scene placeholders.");
                Assert.That(spawned[0].CivilianId, Is.EqualTo("citizen.mainTown.0042"));
                Assert.That(spawned[0].PopulationSlotId, Is.EqualTo("cops.001"));
                Assert.That(spawned[0].PoolId, Is.EqualTo("cops"));
                Assert.That(spawned[0].transform.position, Is.EqualTo(anchorB.position));
                Assert.That(go.transform.Find("Civilian_citizen.mainTown.stale"), Is.Null, "Expected stale spawned civilians to be cleared during load rebuild.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RebuildScenePopulation_WhenActorPrefabIsAssigned_InstantiatesActorPrefabWithPopulationMetadata()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();
            CreateAnchor(go.transform, "Anchor_A", new Vector3(1f, 0f, 0f));
            var actorPrefab = new GameObject("NpcActorPrefab");
            actorPrefab.AddComponent<NpcAgent>();
            actorPrefab.AddComponent<CapsuleCollider>();
            new GameObject("VisualRoot").transform.SetParent(actorPrefab.transform, false);

            try
            {
                ConfigureBridge(
                    bridge,
                    initialPopulationCount: 0,
                    idPrefix: "citizen.mainTown",
                    spawnAnchorIds: System.Array.Empty<string>(),
                    library: CreateLibrary());
                SetPrivateField(typeof(CivilianPopulationRuntimeBridge), bridge, "_npcActorPrefab", actorPrefab);

                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0042",
                    PopulationSlotId = "townsfolk.004",
                    PoolId = "townsfolk",
                    SpawnAnchorId = "Anchor_A",
                    AreaTag = "maintown.square",
                    IsAlive = true
                });

                bridge.RebuildScenePopulation();

                var spawned = go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
                Assert.That(spawned.Length, Is.EqualTo(1));
                Assert.That(spawned[0].CivilianId, Is.EqualTo("citizen.mainTown.0042"));
                Assert.That(spawned[0].GetComponent<NpcAgent>(), Is.Not.Null);
                Assert.That(spawned[0].GetComponent<AmbientCitizenCapability>(), Is.Not.Null);
                Assert.That(spawned[0].transform.Find("VisualRoot"), Is.Not.Null, "Expected prefab-backed civilian spawn to keep the actor visual hierarchy.");
            }
            finally
            {
                Object.DestroyImmediate(actorPrefab);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FinalizeAfterLoad_WhenMondayRefreshWindowHasArrived_ExecutesReplacementUsingCoreWorldState()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();
            CreateAnchor(go.transform, "Anchor_Townsfolk_01", new Vector3(2f, 0f, 0f));

            try
            {
                ConfigureBridge(
                    bridge,
                    initialPopulationCount: 0,
                    idPrefix: "citizen.mainTown",
                    spawnAnchorIds: System.Array.Empty<string>(),
                    library: CreateLibrary());

                var coreWorldModule = new CoreWorldModule
                {
                    DayCount = 14,
                    TimeOfDay = 8f
                };

                var populationModule = new CivilianPopulationModule();
                populationModule.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0007",
                    PopulationSlotId = "townsfolk.001",
                    PoolId = "townsfolk",
                    SpawnAnchorId = "Anchor_Townsfolk_01",
                    AreaTag = "maintown.square",
                    IsAlive = false,
                    IsContractEligible = false,
                    IsProtectedFromContracts = false,
                    BaseBodyId = "body.male.a",
                    PresentationType = "masculine",
                    HairId = "hair.short.01",
                    HairColorId = "hair.black",
                    BeardId = "beard.none",
                    OutfitTopId = "top.coat.01",
                    OutfitBottomId = "bottom.jeans.01",
                    OuterwearId = "outer.gray.coat",
                    MaterialColorIds = new List<string> { "color.gray" },
                    GeneratedDescriptionTags = new List<string> { "gray coat" },
                    CreatedAtDay = 2,
                    RetiredAtDay = 9
                });
                populationModule.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0007",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_01"
                });

                bridge.FinalizeAfterLoad(new[]
                {
                    new SaveModuleRegistration(0, coreWorldModule),
                    new SaveModuleRegistration(1, populationModule)
                });

                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0), "Expected matured debt to execute during load finalization.");
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(2), "Expected dead historical civilian plus newly loaded replacement.");

                var replacement = bridge.Runtime.Civilians[1];
                Assert.That(replacement.CivilianId, Is.EqualTo("citizen.mainTown.0008"));
                Assert.That(replacement.PopulationSlotId, Is.EqualTo("townsfolk.001"));
                Assert.That(replacement.SpawnAnchorId, Is.EqualTo("Anchor_Townsfolk_01"));
                Assert.That(replacement.CreatedAtDay, Is.EqualTo(14));
                Assert.That(replacement.IsAlive, Is.True);

                var spawned = go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
                Assert.That(spawned.Length, Is.EqualTo(1), "Expected finalized load to rebuild one live placeholder after replacement execution.");
                Assert.That(spawned[0].CivilianId, Is.EqualTo("citizen.mainTown.0008"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void WorldStateChanged_WhenMondayMorningThresholdIsCrossed_ExecutesReplacementWithoutReload()
        {
            var worldGo = new GameObject("CoreWorldController");
            worldGo.AddComponent<CoreWorldController>();

            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();
            CreateAnchor(go.transform, "Anchor_Townsfolk_01", new Vector3(2f, 0f, 0f));

            try
            {
                ConfigureBridge(
                    bridge,
                    initialPopulationCount: 0,
                    idPrefix: "citizen.mainTown",
                    spawnAnchorIds: System.Array.Empty<string>(),
                    library: CreateLibrary());

                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0007",
                    PopulationSlotId = "townsfolk.001",
                    PoolId = "townsfolk",
                    SpawnAnchorId = "Anchor_Townsfolk_01",
                    AreaTag = "maintown.square",
                    IsAlive = false,
                    IsContractEligible = false,
                    IsProtectedFromContracts = false,
                    BaseBodyId = "body.male.a",
                    PresentationType = "masculine",
                    HairId = "hair.short.01",
                    HairColorId = "hair.black",
                    BeardId = "beard.none",
                    OutfitTopId = "top.coat.01",
                    OutfitBottomId = "bottom.jeans.01",
                    OuterwearId = "outer.gray.coat",
                    MaterialColorIds = new List<string> { "color.gray" },
                    GeneratedDescriptionTags = new List<string> { "gray coat" },
                    CreatedAtDay = 2,
                    RetiredAtDay = 9
                });
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0007",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_01"
                });

                var controller = worldGo.GetComponent<CoreWorldController>();
                controller.SetWorldState(14, 7.5f);
                bridge.SetCoreWorldController(controller);
                controller.SetWorldState(14, 7.75f);

                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(1), "Expected pre-refresh Monday time changes to leave replacement debt untouched.");
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(1), "Expected no replacement until Monday 08:00 arrives.");

                controller.SetWorldState(14, 8f);

                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0), "Expected Monday 08:00 to execute matured replacement debt.");
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(2), "Expected same-session replacement to add a new live civilian.");

                var replacement = bridge.Runtime.Civilians[1];
                Assert.That(replacement.CivilianId, Is.EqualTo("citizen.mainTown.0008"));
                Assert.That(replacement.PopulationSlotId, Is.EqualTo("townsfolk.001"));
                Assert.That(replacement.CreatedAtDay, Is.EqualTo(14));

                var spawned = go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
                Assert.That(spawned.Length, Is.EqualTo(1), "Expected world-state change to rebuild one live placeholder.");
                Assert.That(spawned[0].CivilianId, Is.EqualTo("citizen.mainTown.0008"));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(worldGo);
            }
        }

        [Test]
        public void PrepareForSave_WhenRuntimeIsEmpty_PreservesLoadedModuleData()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();

            try
            {
                var module = new CivilianPopulationModule();
                module.Civilians.Add(new CivilianPopulationRecord
                {
                    PopulationSlotId = "townsfolk.021",
                    PoolId = "townsfolk",
                    CivilianId = "citizen.mainTown.0042",
                    IsAlive = true,
                    IsContractEligible = false,
                    IsProtectedFromContracts = false,
                    BaseBodyId = "body.female.a",
                    PresentationType = "feminine",
                    HairId = "hair.long.01",
                    HairColorId = "hair.red",
                    BeardId = "beard.none",
                    OutfitTopId = "top.jacket.01",
                    OutfitBottomId = "bottom.jeans.01",
                    OuterwearId = "outer.red.jacket",
                    MaterialColorIds = new List<string> { "color.red" },
                    GeneratedDescriptionTags = new List<string> { "red jacket" },
                    SpawnAnchorId = "spawn.busstop.b",
                    AreaTag = "downtown",
                    CreatedAtDay = 6,
                    RetiredAtDay = -1
                });
                module.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0004",
                    QueuedAtDay = 7,
                    SpawnAnchorId = "spawn.busstop.b"
                });

                bridge.PrepareForSave(new[] { new SaveModuleRegistration(1, module) });

                Assert.That(module.Civilians.Count, Is.EqualTo(1));
                Assert.That(module.Civilians[0].PopulationSlotId, Is.EqualTo("townsfolk.021"));
                Assert.That(module.Civilians[0].PoolId, Is.EqualTo("townsfolk"));
                Assert.That(module.Civilians[0].CivilianId, Is.EqualTo("citizen.mainTown.0042"));
                Assert.That(module.PendingReplacements.Count, Is.EqualTo(1));
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(1));
                Assert.That(bridge.Runtime.Civilians[0].PopulationSlotId, Is.EqualTo("townsfolk.021"));
                Assert.That(bridge.Runtime.Civilians[0].CivilianId, Is.EqualTo("citizen.mainTown.0042"));
                Assert.That(bridge.Runtime.Civilians[0].AreaTag, Is.EqualTo("downtown"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TryRetireCivilian_MarksCitizenDeadAndQueuesReplacement()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();

            try
            {
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0007",
                    IsAlive = true,
                    IsContractEligible = true,
                    SpawnAnchorId = "spawn.busstop.c",
                    CreatedAtDay = 2,
                    RetiredAtDay = -1
                });

                Assert.That(bridge.TryRetireCivilian("citizen.mainTown.0007", retiredAtDay: 9), Is.True);

                Assert.That(bridge.Runtime.Civilians[0].IsAlive, Is.False);
                Assert.That(bridge.Runtime.Civilians[0].IsContractEligible, Is.False);
                Assert.That(bridge.Runtime.Civilians[0].RetiredAtDay, Is.EqualTo(9));
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(1));
                Assert.That(bridge.Runtime.PendingReplacements[0].VacatedCivilianId, Is.EqualTo("citizen.mainTown.0007"));
                Assert.That(bridge.Runtime.PendingReplacements[0].QueuedAtDay, Is.EqualTo(9));
                Assert.That(bridge.Runtime.PendingReplacements[0].SpawnAnchorId, Is.EqualTo("spawn.busstop.c"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TryRetireCivilian_WhenCalledAgain_DoesNotDuplicateReplacementDebt()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();

            try
            {
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0008",
                    IsAlive = true,
                    IsContractEligible = true,
                    SpawnAnchorId = "spawn.busstop.a",
                    CreatedAtDay = 2,
                    RetiredAtDay = -1
                });

                Assert.That(bridge.TryRetireCivilian("citizen.mainTown.0008", retiredAtDay: 10), Is.True);
                Assert.That(bridge.TryRetireCivilian("citizen.mainTown.0008", retiredAtDay: 11), Is.False);

                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(1));
                Assert.That(bridge.Runtime.Civilians[0].RetiredAtDay, Is.EqualTo(10));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExecutePendingReplacements_WhenMondayRefreshWindowHasArrived_ReplacesDeadCivilianInSameSlot()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();
            CreateAnchor(go.transform, "Anchor_Townsfolk_01", new Vector3(2f, 0f, 0f));

            try
            {
                ConfigureBridge(
                    bridge,
                    initialPopulationCount: 0,
                    idPrefix: "citizen.mainTown",
                    spawnAnchorIds: System.Array.Empty<string>(),
                    library: CreateLibrary());

                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0007",
                    PopulationSlotId = "townsfolk.001",
                    PoolId = "townsfolk",
                    SpawnAnchorId = "Anchor_Townsfolk_01",
                    AreaTag = "maintown.square",
                    IsAlive = false,
                    IsContractEligible = false,
                    IsProtectedFromContracts = false,
                    BaseBodyId = "body.male.a",
                    PresentationType = "masculine",
                    HairId = "hair.short.01",
                    HairColorId = "hair.black",
                    BeardId = "beard.none",
                    OutfitTopId = "top.coat.01",
                    OutfitBottomId = "bottom.jeans.01",
                    OuterwearId = "outer.gray.coat",
                    MaterialColorIds = new List<string> { "color.gray" },
                    GeneratedDescriptionTags = new List<string> { "gray coat" },
                    CreatedAtDay = 2,
                    RetiredAtDay = 9
                });
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0007",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_01"
                });

                var replacedCount = InvokeExecutePendingReplacements(bridge, currentDay: 14, currentTimeOfDay: 8f);

                Assert.That(replacedCount, Is.EqualTo(1), "Expected one matured replacement to execute.");
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0), "Expected replacement debt to clear after execution.");
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(2), "Expected original dead civilian to remain in history and a new live civilian to be added.");

                var replacement = bridge.Runtime.Civilians[1];
                Assert.That(replacement.CivilianId, Is.EqualTo("citizen.mainTown.0008"));
                Assert.That(replacement.CivilianId, Is.Not.EqualTo("citizen.mainTown.0007"));
                Assert.That(replacement.PopulationSlotId, Is.EqualTo("townsfolk.001"));
                Assert.That(replacement.PoolId, Is.EqualTo("townsfolk"));
                Assert.That(replacement.SpawnAnchorId, Is.EqualTo("Anchor_Townsfolk_01"));
                Assert.That(replacement.AreaTag, Is.EqualTo("maintown.square"));
                Assert.That(replacement.CreatedAtDay, Is.EqualTo(14));
                Assert.That(replacement.RetiredAtDay, Is.EqualTo(-1));
                Assert.That(replacement.IsAlive, Is.True);
                Assert.That(replacement.IsContractEligible, Is.True);

                var spawned = go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
                Assert.That(spawned.Length, Is.EqualTo(1), "Expected replacement execution to rebuild scene placeholders.");
                Assert.That(spawned[0].CivilianId, Is.EqualTo("citizen.mainTown.0008"));
                Assert.That(spawned[0].PopulationSlotId, Is.EqualTo("townsfolk.001"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExecutePendingReplacements_WhenMondayRefreshWindowHasNotArrived_DoesNotReplaceEarly()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();

            try
            {
                ConfigureBridge(
                    bridge,
                    initialPopulationCount: 0,
                    idPrefix: "citizen.mainTown",
                    spawnAnchorIds: System.Array.Empty<string>(),
                    library: CreateLibrary());

                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0010",
                    PopulationSlotId = "cops.001",
                    PoolId = "cops",
                    SpawnAnchorId = "Anchor_Cop_01",
                    AreaTag = "maintown.watch",
                    IsAlive = false,
                    IsContractEligible = false,
                    IsProtectedFromContracts = true,
                    BaseBodyId = "body.male.a",
                    PresentationType = "masculine",
                    HairId = "hair.short.01",
                    HairColorId = "hair.black",
                    BeardId = "beard.none",
                    OutfitTopId = "top.coat.01",
                    OutfitBottomId = "bottom.jeans.01",
                    OuterwearId = "outer.gray.coat",
                    MaterialColorIds = new List<string> { "color.gray" },
                    GeneratedDescriptionTags = new List<string> { "gray coat" },
                    CreatedAtDay = 3,
                    RetiredAtDay = 10
                });
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0010",
                    QueuedAtDay = 14,
                    SpawnAnchorId = "Anchor_Cop_01"
                });

                var replacedCount = InvokeExecutePendingReplacements(bridge, currentDay: 14, currentTimeOfDay: 7.99f);

                Assert.That(replacedCount, Is.EqualTo(0));
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(1));
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExecutePendingReplacements_WhenVacancyWasQueuedOnMonday_WaitsUntilFollowingMondayRefresh()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();

            try
            {
                ConfigureBridge(
                    bridge,
                    initialPopulationCount: 0,
                    idPrefix: "citizen.mainTown",
                    spawnAnchorIds: System.Array.Empty<string>(),
                    library: CreateLibrary());

                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0011",
                    PopulationSlotId = "townsfolk.002",
                    PoolId = "townsfolk",
                    SpawnAnchorId = "Anchor_Townsfolk_02",
                    AreaTag = "maintown.square",
                    IsAlive = false,
                    IsContractEligible = false,
                    IsProtectedFromContracts = false,
                    BaseBodyId = "body.male.a",
                    PresentationType = "masculine",
                    HairId = "hair.short.01",
                    HairColorId = "hair.black",
                    BeardId = "beard.none",
                    OutfitTopId = "top.coat.01",
                    OutfitBottomId = "bottom.jeans.01",
                    OuterwearId = "outer.gray.coat",
                    MaterialColorIds = new List<string> { "color.gray" },
                    GeneratedDescriptionTags = new List<string> { "gray coat" },
                    CreatedAtDay = 3,
                    RetiredAtDay = 14
                });
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0011",
                    QueuedAtDay = 14,
                    SpawnAnchorId = "Anchor_Townsfolk_02"
                });

                var replacedCount = InvokeExecutePendingReplacements(bridge, currentDay: 14, currentTimeOfDay: 8f);

                Assert.That(replacedCount, Is.EqualTo(0), "Expected Monday-queued vacancies to wait until the following Monday because no time-of-death is persisted.");
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(1));
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExecutePendingReplacements_WhenDuplicateDebtsReachMondayRefresh_SpawnsOnlyOneReplacement()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();
            CreateAnchor(go.transform, "Anchor_Townsfolk_01", new Vector3(2f, 0f, 0f));

            try
            {
                ConfigureBridge(
                    bridge,
                    initialPopulationCount: 0,
                    idPrefix: "citizen.mainTown",
                    spawnAnchorIds: System.Array.Empty<string>(),
                    library: CreateLibrary());

                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0007",
                    PopulationSlotId = "townsfolk.001",
                    PoolId = "townsfolk",
                    SpawnAnchorId = "Anchor_Townsfolk_01",
                    AreaTag = "maintown.square",
                    IsAlive = false,
                    IsContractEligible = false,
                    IsProtectedFromContracts = false,
                    BaseBodyId = "body.male.a",
                    PresentationType = "masculine",
                    HairId = "hair.short.01",
                    HairColorId = "hair.black",
                    BeardId = "beard.none",
                    OutfitTopId = "top.coat.01",
                    OutfitBottomId = "bottom.jeans.01",
                    OuterwearId = "outer.gray.coat",
                    MaterialColorIds = new List<string> { "color.gray" },
                    GeneratedDescriptionTags = new List<string> { "gray coat" },
                    CreatedAtDay = 2,
                    RetiredAtDay = 9
                });
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0007",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_01"
                });
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0007",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_01"
                });

                var replacedCount = InvokeExecutePendingReplacements(bridge, currentDay: 14, currentTimeOfDay: 8f);

                Assert.That(replacedCount, Is.EqualTo(1), "Expected duplicate matured debt to produce only one replacement.");
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0), "Expected duplicate debt entries to be cleared after execution.");
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(2), "Expected one dead historical civilian plus one live replacement.");
                Assert.That(bridge.Runtime.Civilians[1].CivilianId, Is.EqualTo("citizen.mainTown.0008"));

                var spawned = go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
                Assert.That(spawned.Length, Is.EqualTo(1), "Expected scene rebuild to keep one live occupant for the slot.");
                Assert.That(spawned[0].CivilianId, Is.EqualTo("citizen.mainTown.0008"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExecutePendingReplacements_WhenMaturedDebtReferencesMissingCivilian_PurgesDebtWithoutSpawning()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();

            try
            {
                ConfigureBridge(
                    bridge,
                    initialPopulationCount: 0,
                    idPrefix: "citizen.mainTown",
                    spawnAnchorIds: System.Array.Empty<string>(),
                    library: CreateLibrary());

                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.4040",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_01"
                });

                var replacedCount = InvokeExecutePendingReplacements(bridge, currentDay: 14, currentTimeOfDay: 8f);

                Assert.That(replacedCount, Is.EqualTo(0));
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0), "Expected matured debt with no dead civilian target to be purged.");
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(0));
                Assert.That(go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true).Length, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExecutePendingReplacements_WhenMaturedDebtReferencesAliveCivilian_PurgesDebtWithoutSpawning()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();

            try
            {
                ConfigureBridge(
                    bridge,
                    initialPopulationCount: 0,
                    idPrefix: "citizen.mainTown",
                    spawnAnchorIds: System.Array.Empty<string>(),
                    library: CreateLibrary());

                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0012",
                    PopulationSlotId = "townsfolk.003",
                    PoolId = "townsfolk",
                    SpawnAnchorId = "Anchor_Townsfolk_03",
                    AreaTag = "maintown.square",
                    IsAlive = true,
                    IsContractEligible = true,
                    IsProtectedFromContracts = false,
                    BaseBodyId = "body.male.a",
                    PresentationType = "masculine",
                    HairId = "hair.short.01",
                    HairColorId = "hair.black",
                    BeardId = "beard.none",
                    OutfitTopId = "top.coat.01",
                    OutfitBottomId = "bottom.jeans.01",
                    OuterwearId = "outer.gray.coat",
                    MaterialColorIds = new List<string> { "color.gray" },
                    GeneratedDescriptionTags = new List<string> { "gray coat" },
                    CreatedAtDay = 6,
                    RetiredAtDay = -1
                });
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0012",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_03"
                });

                var replacedCount = InvokeExecutePendingReplacements(bridge, currentDay: 14, currentTimeOfDay: 8f);

                Assert.That(replacedCount, Is.EqualTo(0));
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0), "Expected matured debt targeting an alive civilian to be purged.");
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(1));
                Assert.That(go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true).Length, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExecutePendingReplacements_WhenMaturedDebtTargetsOccupiedSlot_PurgesDebtWithoutSpawning()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();

            try
            {
                ConfigureBridge(
                    bridge,
                    initialPopulationCount: 0,
                    idPrefix: "citizen.mainTown",
                    spawnAnchorIds: System.Array.Empty<string>(),
                    library: CreateLibrary());

                bridge.Runtime.Civilians.Add(CreateCivilianRecord(
                    civilianId: "citizen.mainTown.0013",
                    populationSlotId: "townsfolk.004",
                    spawnAnchorId: "Anchor_Townsfolk_04",
                    isAlive: false,
                    retiredAtDay: 5));
                bridge.Runtime.Civilians.Add(CreateCivilianRecord(
                    civilianId: "citizen.mainTown.0014",
                    populationSlotId: "townsfolk.004",
                    spawnAnchorId: "Anchor_Townsfolk_04",
                    isAlive: true,
                    retiredAtDay: -1));
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0013",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_04"
                });

                var replacedCount = InvokeExecutePendingReplacements(bridge, currentDay: 14, currentTimeOfDay: 8f);

                Assert.That(replacedCount, Is.EqualTo(0));
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0), "Expected matured debt for an occupied slot to be purged.");
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExecutePendingReplacements_WhenMultipleMaturedDebtsTargetSameSlot_SpawnsOnlyOneReplacement()
        {
            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();
            CreateAnchor(go.transform, "Anchor_Townsfolk_05", new Vector3(3f, 0f, 0f));

            try
            {
                ConfigureBridge(
                    bridge,
                    initialPopulationCount: 0,
                    idPrefix: "citizen.mainTown",
                    spawnAnchorIds: System.Array.Empty<string>(),
                    library: CreateLibrary());

                bridge.Runtime.Civilians.Add(CreateCivilianRecord(
                    civilianId: "citizen.mainTown.0015",
                    populationSlotId: "townsfolk.005",
                    spawnAnchorId: "Anchor_Townsfolk_05",
                    isAlive: false,
                    retiredAtDay: 5));
                bridge.Runtime.Civilians.Add(CreateCivilianRecord(
                    civilianId: "citizen.mainTown.0016",
                    populationSlotId: "townsfolk.005",
                    spawnAnchorId: "Anchor_Townsfolk_05",
                    isAlive: false,
                    retiredAtDay: 6));
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0015",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_05"
                });
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0016",
                    QueuedAtDay = 10,
                    SpawnAnchorId = "Anchor_Townsfolk_05"
                });

                var replacedCount = InvokeExecutePendingReplacements(bridge, currentDay: 14, currentTimeOfDay: 8f);

                Assert.That(replacedCount, Is.EqualTo(1), "Expected only one replacement for a slot even if malformed debt exists for multiple historical civilians.");
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0), "Expected malformed same-slot debts to be cleared after execution.");
                Assert.That(bridge.Runtime.Civilians.Count(record => record.IsAlive && record.PopulationSlotId == "townsfolk.005"), Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static CivilianAppearanceLibrary CreateLibrary()
        {
            return new CivilianAppearanceLibrary
            {
                BaseBodyIds = new[] { "body.male.a" },
                PresentationTypes = new[] { "masculine" },
                HairIds = new[] { "hair.short.01" },
                HairColorIds = new[] { "hair.black" },
                BeardIds = new[] { "beard.none" },
                OutfitTopIds = new[] { "top.coat.01" },
                OutfitBottomIds = new[] { "bottom.jeans.01" },
                OuterwearIds = new[] { "outer.gray.coat" },
                MaterialColorIds = new[] { "color.gray" },
                DescriptionTags = new[] { "gray coat" }
            };
        }

        private static CivilianPopulationRecord CreateCivilianRecord(
            string civilianId,
            string populationSlotId,
            string spawnAnchorId,
            bool isAlive,
            int retiredAtDay)
        {
            return new CivilianPopulationRecord
            {
                CivilianId = civilianId,
                PopulationSlotId = populationSlotId,
                PoolId = "townsfolk",
                SpawnAnchorId = spawnAnchorId,
                AreaTag = "maintown.square",
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
                MaterialColorIds = new List<string> { "color.gray" },
                GeneratedDescriptionTags = new List<string> { "gray coat" },
                CreatedAtDay = 4,
                RetiredAtDay = retiredAtDay
            };
        }

        private static void ConfigureBridge(
            CivilianPopulationRuntimeBridge bridge,
            int initialPopulationCount,
            string idPrefix,
            string[] spawnAnchorIds,
            CivilianAppearanceLibrary library)
        {
            var type = typeof(CivilianPopulationRuntimeBridge);
            SetPrivateField(type, bridge, "_initialPopulationCount", initialPopulationCount);
            SetPrivateField(type, bridge, "_civilianIdPrefix", idPrefix);
            SetPrivateField(type, bridge, "_spawnAnchorIds", spawnAnchorIds);
            SetPrivateField(type, bridge, "_appearanceLibrary", library);
        }

        private static void SetPrivateField(System.Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on '{type.FullName}'.");
            field.SetValue(instance, value);
        }

        private static int InvokeExecutePendingReplacements(CivilianPopulationRuntimeBridge bridge, int currentDay, float currentTimeOfDay)
        {
            var method = typeof(CivilianPopulationRuntimeBridge).GetMethod(
                "ExecutePendingReplacements",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            Assert.That(method, Is.Not.Null, "Expected a public ExecutePendingReplacements(int currentDay, float currentTimeOfDay) method.");
            return (int)method.Invoke(bridge, new object[] { currentDay, currentTimeOfDay });
        }

        private static Transform CreateAnchor(Transform parent, string name, Vector3 position)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = position;
            return anchor;
        }
    }
}
