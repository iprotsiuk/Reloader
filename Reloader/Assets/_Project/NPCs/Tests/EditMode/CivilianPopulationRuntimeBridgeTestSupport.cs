using System.Collections.Generic;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Events;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Generation;
using Reloader.NPCs.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    internal static class CivilianPopulationRuntimeBridgeTestSupport
    {
        public static (GameObject root, CivilianPopulationRuntimeBridge bridge) CreateBridgeObject(string rootName = "CivilianPopulationRuntimeBridge")
        {
            var root = new GameObject(rootName);
            return (root, root.AddComponent<CivilianPopulationRuntimeBridge>());
        }

        public static (GameObject root, CivilianPopulationRuntimeBridge bridge, GameObject providerRoot, StaticContractRuntimeProvider provider) CreateBridgeWithProvider()
        {
            var (root, bridge) = CreateBridgeObject();
            var providerRoot = new GameObject("StaticContractRuntimeProvider");
            var provider = providerRoot.AddComponent<StaticContractRuntimeProvider>();
            return (root, bridge, providerRoot, provider);
        }

        public static CivilianAppearanceLibrary CreateLibrary()
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

        public static CivilianPopulationRecord CreateCivilianRecord(
            string civilianId,
            string populationSlotId,
            string spawnAnchorId,
            bool isAlive,
            int retiredAtDay)
        {
            return new CivilianPopulationRecord
            {
                CivilianId = civilianId,
                FirstName = "Derek",
                LastName = "Mullen",
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

        public static void ConfigureBridge(
            CivilianPopulationRuntimeBridge bridge,
            int initialPopulationCount,
            string idPrefix,
            string[] spawnAnchorIds,
            CivilianAppearanceLibrary library,
            GameObject npcActorPrefab = null,
            StaticContractRuntimeProvider contractRuntimeProvider = null)
        {
            bridge.ConfigureForTests(
                appearanceLibrary: library,
                initialPopulationCount: initialPopulationCount,
                civilianIdPrefix: idPrefix,
                spawnAnchorIds: spawnAnchorIds,
                npcActorPrefab: npcActorPrefab,
                contractRuntimeProvider: contractRuntimeProvider);
        }

        public static void DestroyProceduralContractDefinition(CivilianPopulationRuntimeBridge bridge)
        {
            bridge.DestroyProceduralContractForTests();
        }

        public static int InvokeExecutePendingReplacements(CivilianPopulationRuntimeBridge bridge, int currentDay, float currentTimeOfDay)
        {
            return bridge.ExecutePendingReplacements(currentDay, currentTimeOfDay);
        }

        public static PoliceHeatLevel GetCurrentHeatLevel(StaticContractRuntimeProvider provider)
        {
            var runtimeField = typeof(StaticContractRuntimeProvider).GetField("_runtime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(runtimeField, Is.Not.Null, "Expected StaticContractRuntimeProvider runtime field for the pre-accept search regression.");
            var runtime = runtimeField!.GetValue(provider) as ContractEscapeResolutionRuntime;
            Assert.That(runtime, Is.Not.Null, "Expected StaticContractRuntimeProvider to initialize its runtime.");
            return runtime!.CurrentHeatState.Level;
        }

        public static Transform CreateAnchor(Transform parent, string name, Vector3 position)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = position;
            return anchor;
        }
    }
}
