using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Reloader.Core.Runtime;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Runtime;
using UnityEngine;
using static Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTestSupport;

namespace Reloader.NPCs.Tests.EditMode
{
    public class CivilianPopulationRuntimeBridgeSaveLoadTests
    {
        [Test]
        public void PrepareForSave_WhenRuntimeAndModuleAreEmpty_SeedsInitialRoster()
        {
            var (go, bridge) = CreateBridgeObject();

            try
            {
                ConfigureBridge(bridge, 3, "citizen.mainTown", new[] { "spawn.busstop.a" }, CreateLibrary());

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
        public void PrepareForSave_WhenSeedingInitialRoster_AssignsUniquePublicNamesAcrossLiveCivilians()
        {
            var (go, bridge) = CreateBridgeObject();

            try
            {
                ConfigureBridge(bridge, 80, "citizen.mainTown", new[] { "spawn.busstop.a" }, CreateLibrary());

                var module = new CivilianPopulationModule();
                bridge.PrepareForSave(new[] { new SaveModuleRegistration(1, module) });

                var displayNames = module.Civilians
                    .Where(record => record.IsAlive)
                    .Select(record => string.Concat(record.FirstName, " ", record.LastName))
                    .ToArray();

                Assert.That(displayNames.Length, Is.EqualTo(80));
                Assert.That(displayNames.Distinct().Count(), Is.EqualTo(displayNames.Length));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FinalizeAfterLoad_RestoresRuntimeFromModule()
        {
            var (go, bridge) = CreateBridgeObject();

            try
            {
                var module = new CivilianPopulationModule();
                module.Civilians.Add(new CivilianPopulationRecord
                {
                    PopulationSlotId = "quarry.worker.004",
                    PoolId = "quarry_workers",
                    CivilianId = "citizen.mainTown.0042",
                    FirstName = "Ilona",
                    LastName = "Sidorov",
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
                module.OfferRotationSeed = 17;

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
                Assert.That(bridge.Runtime.OfferRotationSeed, Is.EqualTo(17));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FinalizeAfterLoad_WhenStyleAppearanceIdsAreInvalid_NormalizesEyebrowsAndPants()
        {
            var (go, bridge) = CreateBridgeObject();

            try
            {
                var module = new CivilianPopulationModule();
                module.Civilians.Add(new CivilianPopulationRecord
                {
                    PopulationSlotId = "townsfolk.legacy.001",
                    PoolId = "townsfolk",
                    CivilianId = "citizen.legacy.001",
                    FirstName = "Ilona",
                    LastName = "Sidorov",
                    IsAlive = true,
                    IsContractEligible = false,
                    IsProtectedFromContracts = true,
                    BaseBodyId = "female.body",
                    PresentationType = "feminine",
                    HairId = "hair.long",
                    HairColorId = "hair.red",
                    EyebrowId = string.Empty,
                    BeardId = "beard.none",
                    OutfitTopId = "tshirt2",
                    OutfitBottomId = "legacy.bottom.invalid",
                    OuterwearId = "none",
                    MaterialColorIds = new List<string> { "style.variant.a" },
                    GeneratedDescriptionTags = new List<string> { "legacy" },
                    SpawnAnchorId = "spawn.busstop.b",
                    AreaTag = "maintown",
                    CreatedAtDay = 6,
                    RetiredAtDay = -1
                });

                bridge.FinalizeAfterLoad(new[] { new SaveModuleRegistration(1, module) });

                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(1));
                Assert.That(bridge.Runtime.Civilians[0].EyebrowId, Is.EqualTo("brous1"));
                Assert.That(bridge.Runtime.Civilians[0].OutfitBottomId, Is.EqualTo("pants1"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PrepareForSave_WhenRuntimeStyleAppearanceIdsAreInvalid_NormalizesEyebrowsAndPantsIntoModule()
        {
            var (go, bridge) = CreateBridgeObject();

            try
            {
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    PopulationSlotId = "townsfolk.legacy.002",
                    PoolId = "townsfolk",
                    CivilianId = "citizen.legacy.002",
                    FirstName = "Vera",
                    LastName = "Petrov",
                    IsAlive = true,
                    IsContractEligible = true,
                    BaseBodyId = "female.body",
                    PresentationType = "feminine",
                    HairId = "hair.long",
                    HairColorId = "hair.red",
                    EyebrowId = string.Empty,
                    BeardId = "beard.none",
                    OutfitTopId = "tshirt2",
                    OutfitBottomId = "legacy.bottom.invalid",
                    OuterwearId = "none",
                    MaterialColorIds = new List<string> { "style.variant.a" },
                    GeneratedDescriptionTags = new List<string> { "legacy" },
                    SpawnAnchorId = "spawn.busstop.b",
                    AreaTag = "maintown",
                    CreatedAtDay = 6,
                    RetiredAtDay = -1
                });

                var module = new CivilianPopulationModule();
                bridge.PrepareForSave(new[] { new SaveModuleRegistration(1, module) });

                Assert.That(module.Civilians.Count, Is.EqualTo(1));
                Assert.That(module.Civilians[0].EyebrowId, Is.EqualTo("brous1"));
                Assert.That(module.Civilians[0].OutfitBottomId, Is.EqualTo("pants1"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PrepareForSave_WhenRuntimeCivilianIsNonStyleMasculine_PreservesOriginalEyebrowAndBottomIds()
        {
            var (go, bridge) = CreateBridgeObject();

            try
            {
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    PopulationSlotId = "townsfolk.legacy.003",
                    PoolId = "townsfolk",
                    CivilianId = "citizen.legacy.003",
                    FirstName = "Marek",
                    LastName = "Sidorov",
                    IsAlive = true,
                    IsContractEligible = true,
                    BaseBodyId = "body.male.a",
                    PresentationType = "masculine",
                    HairId = "hair.short.01",
                    HairColorId = "hair.black",
                    EyebrowId = "brows.arch.01",
                    BeardId = "beard.none",
                    OutfitTopId = "top.coat.01",
                    OutfitBottomId = "bottom.jeans.01",
                    OuterwearId = "outer.gray.coat",
                    MaterialColorIds = new List<string> { "color.gray" },
                    GeneratedDescriptionTags = new List<string> { "legacy" },
                    SpawnAnchorId = "spawn.busstop.a",
                    AreaTag = "downtown",
                    CreatedAtDay = 6,
                    RetiredAtDay = -1
                });

                var module = new CivilianPopulationModule();
                bridge.PrepareForSave(new[] { new SaveModuleRegistration(1, module) });

                Assert.That(module.Civilians.Count, Is.EqualTo(1));
                Assert.That(module.Civilians[0].EyebrowId, Is.EqualTo("brows.arch.01"));
                Assert.That(module.Civilians[0].OutfitBottomId, Is.EqualTo("bottom.jeans.01"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FinalizeAfterLoad_RebuildsScenePopulationFromLoadedModuleAndClearsPriorSpawnedObjects()
        {
            var (go, bridge) = CreateBridgeObject();
            CreateAnchor(go.transform, "Anchor_A", new Vector3(1f, 0f, 0f));
            var anchorB = CreateAnchor(go.transform, "Anchor_B", new Vector3(4f, 0f, 0f));

            try
            {
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.stale",
                    FirstName = "Derek",
                    LastName = "Mullen",
                    PopulationSlotId = "stale.001",
                    PoolId = "townsfolk",
                    SpawnAnchorId = "Anchor_A",
                    AreaTag = "maintown.old",
                    IsAlive = true
                });

                bridge.RebuildScenePopulation();
                Assert.That(go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(true).Length, Is.EqualTo(1));

                var module = new CivilianPopulationModule();
                module.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0042",
                    FirstName = "Maksim",
                    LastName = "Volkov",
                    PopulationSlotId = "cops.001",
                    PoolId = "cops",
                    SpawnAnchorId = "Anchor_B",
                    AreaTag = "maintown.watch",
                    IsAlive = true
                });
                module.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0043",
                    FirstName = "Vera",
                    LastName = "Petrov",
                    PopulationSlotId = "hobos.001",
                    PoolId = "hobos",
                    SpawnAnchorId = "Anchor_A",
                    AreaTag = "maintown.alley",
                    IsAlive = false
                });

                bridge.FinalizeAfterLoad(new[] { new SaveModuleRegistration(1, module) });

                var spawned = go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(true);
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(2));
                Assert.That(spawned.Length, Is.EqualTo(1));
                Assert.That(spawned[0].CivilianId, Is.EqualTo("citizen.mainTown.0042"));
                Assert.That(spawned[0].PopulationSlotId, Is.EqualTo("cops.001"));
                Assert.That(spawned[0].PoolId, Is.EqualTo("cops"));
                Assert.That(spawned[0].transform.position, Is.EqualTo(anchorB.position));
                Assert.That(go.transform.Find("Civilian_citizen.mainTown.stale"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FinalizeAfterLoad_WhenMondayRefreshWindowHasArrived_ExecutesReplacementUsingCoreWorldState()
        {
            var (go, bridge) = CreateBridgeObject();
            CreateAnchor(go.transform, "Anchor_Townsfolk_01", new Vector3(2f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary());

                var coreWorldModule = new CoreWorldModule
                {
                    DayCount = 14,
                    TimeOfDay = 8f
                };
                var populationModule = new CivilianPopulationModule();
                populationModule.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0007", "townsfolk.001", "Anchor_Townsfolk_01", false, 9));
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

                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(2));
                Assert.That(bridge.Runtime.Civilians[1].CivilianId, Is.EqualTo("citizen.mainTown.0008"));
                Assert.That(bridge.Runtime.Civilians[1].PopulationSlotId, Is.EqualTo("townsfolk.001"));
                Assert.That(bridge.Runtime.Civilians[1].SpawnAnchorId, Is.EqualTo("Anchor_Townsfolk_01"));
                Assert.That(bridge.Runtime.Civilians[1].CreatedAtDay, Is.EqualTo(14));
                Assert.That(bridge.Runtime.Civilians[1].IsAlive, Is.True);
                Assert.That(go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(true).Single().CivilianId, Is.EqualTo("citizen.mainTown.0008"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SaveCoordinator_Load_WhenLiveWorldClockWasSaved_ExecutesReplacementUsingSavedCoreWorldTime()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "reloader-coreworld-population-bridge-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var savePath = Path.Combine(tempDir, "slot01.json");

            var worldGo = new GameObject("CoreWorldController");
            var controller = worldGo.AddComponent<CoreWorldController>();
            var (go, bridge) = CreateBridgeObject();
            CreateAnchor(go.transform, "Anchor_Townsfolk_01", new Vector3(2f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary());

                controller.SetWorldState(14, 8f);
                bridge.SetCoreWorldController(controller);
                SaveRuntimeBridgeRegistry.Register(controller);
                SaveRuntimeBridgeRegistry.Register(bridge);

                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0007", "townsfolk.001", "Anchor_Townsfolk_01", false, 9));
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0007",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_01"
                });

                var coordinator = new SaveCoordinator(
                    new SaveFileRepository(),
                    new SaveModuleRegistration[]
                    {
                        new(0, new CoreWorldModule()),
                        new(1, new CivilianPopulationModule())
                    },
                    currentSchemaVersion: 9);

                coordinator.Save(savePath, "0.8.0-dev");

                controller.SetWorldState(0, 0f);
                bridge.Runtime.PendingReplacements.Clear();
                bridge.Runtime.Civilians.Clear();

                coordinator.Load(savePath);

                var snapshot = controller.CaptureSnapshot();
                Assert.That(snapshot.DayCount, Is.EqualTo(14));
                Assert.That(snapshot.TimeOfDay, Is.EqualTo(8f).Within(0.001f));
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(2));
                Assert.That(bridge.Runtime.Civilians[1].CivilianId, Is.EqualTo("citizen.mainTown.0008"));
            }
            finally
            {
                SaveRuntimeBridgeRegistry.Unregister(bridge);
                SaveRuntimeBridgeRegistry.Unregister(controller);
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(worldGo);
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public void WorldStateChanged_WhenMondayMorningThresholdIsCrossed_ExecutesReplacementWithoutReload()
        {
            var worldGo = new GameObject("CoreWorldController");
            var controller = worldGo.AddComponent<CoreWorldController>();
            var (go, bridge) = CreateBridgeObject();
            CreateAnchor(go.transform, "Anchor_Townsfolk_01", new Vector3(2f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary());
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0007", "townsfolk.001", "Anchor_Townsfolk_01", false, 9));
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0007",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_01"
                });

                controller.SetWorldState(14, 7.5f);
                bridge.SetCoreWorldController(controller);
                controller.SetWorldState(14, 7.75f);

                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(1));
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(1));

                controller.SetWorldState(14, 8f);

                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(2));
                Assert.That(bridge.Runtime.Civilians[1].CivilianId, Is.EqualTo("citizen.mainTown.0008"));
                Assert.That(bridge.Runtime.Civilians[1].PopulationSlotId, Is.EqualTo("townsfolk.001"));
                Assert.That(bridge.Runtime.Civilians[1].CreatedAtDay, Is.EqualTo(14));
                Assert.That(go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(true).Single().CivilianId, Is.EqualTo("citizen.mainTown.0008"));
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
            var (go, bridge) = CreateBridgeObject();

            try
            {
                var module = new CivilianPopulationModule();
                module.Civilians.Add(new CivilianPopulationRecord
                {
                    PopulationSlotId = "townsfolk.021",
                    PoolId = "townsfolk",
                    CivilianId = "citizen.mainTown.0042",
                    FirstName = "Ilona",
                    LastName = "Sidorov",
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
    }
}
