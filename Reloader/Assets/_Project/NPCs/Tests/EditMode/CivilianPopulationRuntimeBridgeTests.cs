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
                    CivilianId = "citizen.mainTown.0042",
                    IsAlive = true,
                    IsContractEligible = false,
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
                Assert.That(bridge.Runtime.Civilians[0].CivilianId, Is.EqualTo("citizen.mainTown.0042"));
                Assert.That(bridge.Runtime.Civilians[0].IsContractEligible, Is.False);
                Assert.That(bridge.Runtime.Civilians[0].GeneratedDescriptionTags, Is.EqualTo(new[] { "red jacket" }));
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(1));
                Assert.That(bridge.Runtime.PendingReplacements[0].VacatedCivilianId, Is.EqualTo("citizen.mainTown.0004"));
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
    }
}
