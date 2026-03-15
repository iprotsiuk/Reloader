using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Runtime;
using UnityEngine;
using static Reloader.NPCs.Tests.EditMode.CivilianPopulationRuntimeBridgeTestSupport;

namespace Reloader.NPCs.Tests.EditMode
{
    public class CivilianPopulationRuntimeBridgeReplacementTests
    {
        [Test]
        public void TryRetireCivilian_MarksCitizenDeadAndQueuesReplacement()
        {
            var (go, bridge) = CreateBridgeObject();

            try
            {
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0007",
                    FirstName = "Pavel",
                    LastName = "Dobrev",
                    IsAlive = true,
                    IsContractEligible = true,
                    SpawnAnchorId = "spawn.busstop.c",
                    CreatedAtDay = 2,
                    RetiredAtDay = -1
                });

                Assert.That(bridge.TryRetireCivilian("citizen.mainTown.0007", 9), Is.True);
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
            var (go, bridge) = CreateBridgeObject();

            try
            {
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0008",
                    FirstName = "Nadia",
                    LastName = "Petrov",
                    IsAlive = true,
                    IsContractEligible = true,
                    SpawnAnchorId = "spawn.busstop.a",
                    CreatedAtDay = 2,
                    RetiredAtDay = -1
                });

                Assert.That(bridge.TryRetireCivilian("citizen.mainTown.0008", 10), Is.True);
                Assert.That(bridge.TryRetireCivilian("citizen.mainTown.0008", 11), Is.False);
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

                var replacedCount = InvokeExecutePendingReplacements(bridge, 14, 8f);

                Assert.That(replacedCount, Is.EqualTo(1));
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(2));

                var replacement = bridge.Runtime.Civilians[1];
                Assert.That(replacement.CivilianId, Is.EqualTo("citizen.mainTown.0008"));
                Assert.That(replacement.PopulationSlotId, Is.EqualTo("townsfolk.001"));
                Assert.That(replacement.SpawnAnchorId, Is.EqualTo("Anchor_Townsfolk_01"));
                Assert.That(replacement.AreaTag, Is.EqualTo("maintown.square"));
                Assert.That(replacement.CreatedAtDay, Is.EqualTo(14));
                Assert.That(replacement.RetiredAtDay, Is.EqualTo(-1));
                Assert.That(replacement.IsAlive, Is.True);
                Assert.That(replacement.IsContractEligible, Is.True);

                var spawned = go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(true).Single();
                Assert.That(spawned.CivilianId, Is.EqualTo("citizen.mainTown.0008"));
                Assert.That(spawned.PopulationSlotId, Is.EqualTo("townsfolk.001"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExecutePendingReplacements_WhenReplacementSeedCollidesWithLiveRoster_RerollsToUniquePublicName()
        {
            var (go, bridge) = CreateBridgeObject();
            CreateAnchor(go.transform, "Anchor_Townsfolk_12", new Vector3(12f, 0f, 0f));
            CreateAnchor(go.transform, "Anchor_Townsfolk_77", new Vector3(77f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary());
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0012",
                    FirstName = "Ivan",
                    LastName = "Novak",
                    PopulationSlotId = "townsfolk.012",
                    PoolId = "townsfolk",
                    SpawnAnchorId = "Anchor_Townsfolk_12",
                    AreaTag = "maintown.square",
                    IsAlive = true,
                    IsContractEligible = true,
                    IsProtectedFromContracts = false
                });
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0076", "townsfolk.077", "Anchor_Townsfolk_77", false, 9));
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0076",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_77"
                });

                Assert.That(InvokeExecutePendingReplacements(bridge, 14, 8f), Is.EqualTo(1));

                var replacement = bridge.Runtime.Civilians.Single(record => record.CivilianId == "citizen.mainTown.0077");
                Assert.That(string.Concat(replacement.FirstName, " ", replacement.LastName), Is.Not.EqualTo("Ivan Novak"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExecutePendingReplacements_WhenMondayRefreshWindowHasNotArrived_DoesNotReplaceEarly()
        {
            var (go, bridge) = CreateBridgeObject();

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary());
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0010",
                    FirstName = "Martin",
                    LastName = "Kolar",
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

                Assert.That(InvokeExecutePendingReplacements(bridge, 14, 7.99f), Is.EqualTo(0));
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
            var (go, bridge) = CreateBridgeObject();

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary());
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0011",
                    FirstName = "Maksim",
                    LastName = "Volkov",
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

                Assert.That(InvokeExecutePendingReplacements(bridge, 14, 8f), Is.EqualTo(0));
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
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0007",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_01"
                });

                Assert.That(InvokeExecutePendingReplacements(bridge, 14, 8f), Is.EqualTo(1));
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(2));
                Assert.That(bridge.Runtime.Civilians[1].CivilianId, Is.EqualTo("citizen.mainTown.0008"));
                Assert.That(go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(true).Single().CivilianId, Is.EqualTo("citizen.mainTown.0008"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExecutePendingReplacements_WhenMaturedDebtReferencesMissingCivilian_PurgesDebtWithoutSpawning()
        {
            var (go, bridge) = CreateBridgeObject();

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary());
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.4040",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_01"
                });

                Assert.That(InvokeExecutePendingReplacements(bridge, 14, 8f), Is.EqualTo(0));
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(0));
                Assert.That(go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(true).Length, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExecutePendingReplacements_WhenMaturedDebtReferencesAliveCivilian_PurgesDebtWithoutSpawning()
        {
            var (go, bridge) = CreateBridgeObject();

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary());
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.0012",
                    FirstName = "Leon",
                    LastName = "Hale",
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

                Assert.That(InvokeExecutePendingReplacements(bridge, 14, 8f), Is.EqualTo(0));
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(1));
                Assert.That(go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(true).Length, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExecutePendingReplacements_WhenMaturedDebtTargetsOccupiedSlot_PurgesDebtWithoutSpawning()
        {
            var (go, bridge) = CreateBridgeObject();

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary());
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0013", "townsfolk.004", "Anchor_Townsfolk_04", false, 5));
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0014", "townsfolk.004", "Anchor_Townsfolk_04", true, -1));
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0013",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_04"
                });

                Assert.That(InvokeExecutePendingReplacements(bridge, 14, 8f), Is.EqualTo(0));
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));
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
            var (go, bridge) = CreateBridgeObject();
            CreateAnchor(go.transform, "Anchor_Townsfolk_05", new Vector3(3f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary());
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0015", "townsfolk.005", "Anchor_Townsfolk_05", false, 5));
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0016", "townsfolk.005", "Anchor_Townsfolk_05", false, 6));
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

                Assert.That(InvokeExecutePendingReplacements(bridge, 14, 8f), Is.EqualTo(1));
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));
                Assert.That(bridge.Runtime.Civilians.Count(record => record.IsAlive && record.PopulationSlotId == "townsfolk.005"), Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ExecutePendingReplacements_WhenDebtAnchorDiffersFromVacatedCivilian_UsesVacatedCivilianAnchor()
        {
            var (go, bridge) = CreateBridgeObject();
            CreateAnchor(go.transform, "Anchor_Townsfolk_06", new Vector3(6f, 0f, 0f));
            CreateAnchor(go.transform, "Anchor_Townsfolk_Drift", new Vector3(12f, 0f, 0f));

            try
            {
                ConfigureBridge(bridge, 0, "citizen.mainTown", System.Array.Empty<string>(), CreateLibrary());
                bridge.Runtime.Civilians.Add(CreateCivilianRecord("citizen.mainTown.0080", "townsfolk.080", "Anchor_Townsfolk_06", false, 9));
                bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                {
                    VacatedCivilianId = "citizen.mainTown.0080",
                    QueuedAtDay = 9,
                    SpawnAnchorId = "Anchor_Townsfolk_Drift"
                });

                Assert.That(InvokeExecutePendingReplacements(bridge, 14, 8f), Is.EqualTo(1));
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));
                Assert.That(bridge.Runtime.Civilians[1].SpawnAnchorId, Is.EqualTo("Anchor_Townsfolk_06"));
                Assert.That(go.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(true).Single().transform.position, Is.EqualTo(new Vector3(6f, 0f, 0f)));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
