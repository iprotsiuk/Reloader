using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Reloader.Core.Save;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Generation;
using Reloader.NPCs.Runtime;
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

        private static Transform CreateAnchor(Transform parent, string name, Vector3 position)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = position;
            return anchor;
        }
    }
}
