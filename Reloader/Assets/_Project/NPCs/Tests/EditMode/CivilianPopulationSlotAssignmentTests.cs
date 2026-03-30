using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Save;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Generation;
using Reloader.NPCs.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class CivilianPopulationSlotAssignmentTests
    {
        private const string DefinitionTypeName = "Reloader.NPCs.Generation.MainTownPopulationDefinition, Reloader.NPCs";
        private const string PoolTypeName = "Reloader.NPCs.Generation.MainTownPopulationPoolDefinition, Reloader.NPCs";
        private const string SlotTypeName = "Reloader.NPCs.Generation.MainTownPopulationSlotDefinition, Reloader.NPCs";
        private const string HabitatTypeName = "Reloader.NPCs.Generation.MainTownPopulationHabitat, Reloader.NPCs";

        [Test]
        public void PrepareForSave_WhenPopulationDefinitionProvidesSlots_AssignsOneOccupantPerSlot()
        {
            var definitionType = ResolveRequiredType(DefinitionTypeName);
            var poolType = ResolveRequiredType(PoolTypeName);
            var slotType = ResolveRequiredType(SlotTypeName);
            var habitatType = ResolveRequiredType(HabitatTypeName);

            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();

            try
            {
                ConfigureBridge(
                    bridge,
                    populationDefinition: CreateDefinition(definitionType, poolType, slotType, habitatType),
                    idPrefix: "citizen.mainTown",
                    spawnHabitat: Enum.Parse(habitatType, "Any"),
                    library: CreateLibrary());

                var module = new CivilianPopulationModule();
                bridge.PrepareForSave(new[] { new SaveModuleRegistration(1, module) });

                Assert.That(module.Civilians.Count, Is.EqualTo(2));
                Assert.That(module.Civilians.Select(record => record.CivilianId), Is.EqualTo(new[]
                {
                    "citizen.mainTown.0001",
                    "citizen.mainTown.0002"
                }));

                Assert.That(module.Civilians.Select(record => GetRecordProperty<string>(record, "PopulationSlotId")), Is.EqualTo(new[]
                {
                    "quarry.worker.001",
                    "townsfolk.001"
                }));
                Assert.That(module.Civilians.Select(record => GetRecordProperty<string>(record, "PoolId")), Is.EqualTo(new[]
                {
                    "quarry_workers",
                    "townsfolk"
                }));
                Assert.That(module.Civilians.Select(record => GetRecordProperty<string>(record, "AreaTag")), Is.EqualTo(new[]
                {
                    "quarry",
                    "downtown"
                }));
                Assert.That(module.Civilians.Select(record => GetRecordProperty<bool>(record, "IsProtectedFromContracts")), Is.EqualTo(new[]
                {
                    false,
                    false
                }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PrepareForSave_WhenBridgeHabitatIsBounded_SeedsOnlyMatchingHabitatSlots()
        {
            var definitionType = ResolveRequiredType(DefinitionTypeName);
            var poolType = ResolveRequiredType(PoolTypeName);
            var slotType = ResolveRequiredType(SlotTypeName);
            var habitatType = ResolveRequiredType(HabitatTypeName);

            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();

            try
            {
                ConfigureBridge(
                    bridge,
                    populationDefinition: CreateDefinition(definitionType, poolType, slotType, habitatType),
                    idPrefix: "citizen.mainTown",
                    spawnHabitat: Enum.Parse(habitatType, "Quarry"),
                    library: CreateLibrary());

                var module = new CivilianPopulationModule();
                bridge.PrepareForSave(new[] { new SaveModuleRegistration(1, module) });

                Assert.That(module.Civilians.Count, Is.EqualTo(1));
                Assert.That(module.Civilians[0].CivilianId, Is.EqualTo("citizen.mainTown.0001"));
                Assert.That(GetRecordProperty<string>(module.Civilians[0], "PopulationSlotId"), Is.EqualTo("quarry.worker.001"));
                Assert.That(GetRecordProperty<string>(module.Civilians[0], "PoolId"), Is.EqualTo("quarry_workers"));
                Assert.That(GetRecordProperty<string>(module.Civilians[0], "AreaTag"), Is.EqualTo("quarry"));
                Assert.That(GetRecordProperty<string>(module.Civilians[0], "SpawnAnchorId"), Is.EqualTo("spawn.quarry.a"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PrepareForSave_WhenDifferentHabitatBridgesSharePopulationDefinition_AssignsStableUniqueCivilianIdsFromGlobalSlotOrder()
        {
            var definitionType = ResolveRequiredType(DefinitionTypeName);
            var poolType = ResolveRequiredType(PoolTypeName);
            var slotType = ResolveRequiredType(SlotTypeName);
            var habitatType = ResolveRequiredType(HabitatTypeName);

            var definition = CreateSplitDefinition(definitionType, poolType, slotType, habitatType);
            var townGo = new GameObject("CivilianPopulationRuntimeBridge_Town");
            var townBridge = townGo.AddComponent<CivilianPopulationRuntimeBridge>();
            var quarryGo = new GameObject("CivilianPopulationRuntimeBridge_Quarry");
            var quarryBridge = quarryGo.AddComponent<CivilianPopulationRuntimeBridge>();

            try
            {
                ConfigureBridge(
                    townBridge,
                    populationDefinition: definition,
                    idPrefix: "citizen.mainTown",
                    spawnHabitat: Enum.Parse(habitatType, "Town"),
                    library: CreateLibrary());
                ConfigureBridge(
                    quarryBridge,
                    populationDefinition: definition,
                    idPrefix: "citizen.mainTown",
                    spawnHabitat: Enum.Parse(habitatType, "Quarry"),
                    library: CreateLibrary());

                var townModule = new CivilianPopulationModule();
                townBridge.PrepareForSave(new[] { new SaveModuleRegistration(1, townModule) });

                var quarryModule = new CivilianPopulationModule();
                quarryBridge.PrepareForSave(new[] { new SaveModuleRegistration(1, quarryModule) });

                Assert.That(townModule.Civilians.Select(record => record.CivilianId), Is.EqualTo(new[]
                {
                    "citizen.mainTown.0001",
                    "citizen.mainTown.0003"
                }));
                Assert.That(quarryModule.Civilians.Select(record => record.CivilianId), Is.EqualTo(new[]
                {
                    "citizen.mainTown.0002"
                }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(townGo);
                UnityEngine.Object.DestroyImmediate(quarryGo);
                UnityEngine.Object.DestroyImmediate((UnityEngine.Object)definition);
            }
        }

        [Test]
        public void ExecutePendingReplacements_WhenDifferentHabitatBridgesSharePopulationDefinition_AssignsUniqueReplacementIds()
        {
            var definitionType = ResolveRequiredType(DefinitionTypeName);
            var poolType = ResolveRequiredType(PoolTypeName);
            var slotType = ResolveRequiredType(SlotTypeName);
            var habitatType = ResolveRequiredType(HabitatTypeName);

            var definition = CreateTwoHabitatDefinition(definitionType, poolType, slotType, habitatType);
            var townGo = new GameObject("CivilianPopulationRuntimeBridge_Town");
            var townBridge = townGo.AddComponent<CivilianPopulationRuntimeBridge>();
            var quarryGo = new GameObject("CivilianPopulationRuntimeBridge_Quarry");
            var quarryBridge = quarryGo.AddComponent<CivilianPopulationRuntimeBridge>();

            try
            {
                ConfigureBridge(
                    townBridge,
                    populationDefinition: definition,
                    idPrefix: "citizen.mainTown",
                    spawnHabitat: Enum.Parse(habitatType, "Town"),
                    library: CreateLibrary());
                ConfigureBridge(
                    quarryBridge,
                    populationDefinition: definition,
                    idPrefix: "citizen.mainTown",
                    spawnHabitat: Enum.Parse(habitatType, "Quarry"),
                    library: CreateLibrary());

                townBridge.PrepareForSave(new[] { new SaveModuleRegistration(1, new CivilianPopulationModule()) });
                quarryBridge.PrepareForSave(new[] { new SaveModuleRegistration(1, new CivilianPopulationModule()) });

                QueueReplacementForOnlyLiveCivilian(townBridge);
                QueueReplacementForOnlyLiveCivilian(quarryBridge);

                Assert.That(townBridge.ExecutePendingReplacements(7, 8f), Is.EqualTo(1));
                Assert.That(quarryBridge.ExecutePendingReplacements(7, 8f), Is.EqualTo(1));

                Assert.That(townBridge.Runtime.Civilians.Single(record => record.IsAlive).CivilianId, Is.EqualTo("citizen.mainTown.0003"));
                Assert.That(quarryBridge.Runtime.Civilians.Single(record => record.IsAlive).CivilianId, Is.EqualTo("citizen.mainTown.0004"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(townGo);
                UnityEngine.Object.DestroyImmediate(quarryGo);
                UnityEngine.Object.DestroyImmediate((UnityEngine.Object)definition);
            }
        }

        private static object CreateDefinition(Type definitionType, Type poolType, Type slotType, Type habitatType)
        {
            var definition = ScriptableObject.CreateInstance(definitionType);

            var quarryPool = Activator.CreateInstance(poolType);
            var townsfolkPool = Activator.CreateInstance(poolType);

            SetProperty(quarryPool, "PoolId", "quarry_workers");
            SetProperty(townsfolkPool, "PoolId", "townsfolk");

            var quarrySlots = Array.CreateInstance(slotType, 1);
            quarrySlots.SetValue(CreateSlot(slotType, habitatType, "quarry.worker.001", "quarry_workers", "quarry", "spawn.quarry.a", "Quarry"), 0);
            SetProperty(quarryPool, "Slots", quarrySlots);

            var townSlots = Array.CreateInstance(slotType, 1);
            townSlots.SetValue(CreateSlot(slotType, habitatType, "townsfolk.001", "townsfolk", "downtown", "spawn.mainstreet.a", "Town"), 0);
            SetProperty(townsfolkPool, "Slots", townSlots);

            var pools = Array.CreateInstance(poolType, 2);
            pools.SetValue(quarryPool, 0);
            pools.SetValue(townsfolkPool, 1);
            SetProperty(definition, "Pools", pools);

            return definition;
        }

        private static object CreateSplitDefinition(Type definitionType, Type poolType, Type slotType, Type habitatType)
        {
            var definition = ScriptableObject.CreateInstance(definitionType);

            var townPool = Activator.CreateInstance(poolType);
            var quarryPool = Activator.CreateInstance(poolType);
            var forestPool = Activator.CreateInstance(poolType);

            SetProperty(townPool, "PoolId", "townsfolk");
            SetProperty(quarryPool, "PoolId", "quarry_workers");
            SetProperty(forestPool, "PoolId", "forest_camp");

            var townSlots = Array.CreateInstance(slotType, 1);
            townSlots.SetValue(CreateSlot(slotType, habitatType, "townsfolk.001", "townsfolk", "downtown", "spawn.mainstreet.a", "Town"), 0);
            SetProperty(townPool, "Slots", townSlots);

            var quarrySlots = Array.CreateInstance(slotType, 1);
            quarrySlots.SetValue(CreateSlot(slotType, habitatType, "quarry.worker.001", "quarry_workers", "quarry", "spawn.quarry.a", "Quarry"), 0);
            SetProperty(quarryPool, "Slots", quarrySlots);

            var forestSlots = Array.CreateInstance(slotType, 1);
            forestSlots.SetValue(CreateSlot(slotType, habitatType, "forest.camp.001", "forest_camp", "forest", "spawn.forest.a", "Town"), 0);
            SetProperty(forestPool, "Slots", forestSlots);

            var pools = Array.CreateInstance(poolType, 3);
            pools.SetValue(townPool, 0);
            pools.SetValue(quarryPool, 1);
            pools.SetValue(forestPool, 2);
            SetProperty(definition, "Pools", pools);

            return definition;
        }

        private static object CreateTwoHabitatDefinition(Type definitionType, Type poolType, Type slotType, Type habitatType)
        {
            var definition = ScriptableObject.CreateInstance(definitionType);

            var townPool = Activator.CreateInstance(poolType);
            var quarryPool = Activator.CreateInstance(poolType);

            SetProperty(townPool, "PoolId", "townsfolk");
            SetProperty(quarryPool, "PoolId", "quarry_workers");

            var townSlots = Array.CreateInstance(slotType, 1);
            townSlots.SetValue(CreateSlot(slotType, habitatType, "townsfolk.001", "townsfolk", "downtown", "spawn.mainstreet.a", "Town"), 0);
            SetProperty(townPool, "Slots", townSlots);

            var quarrySlots = Array.CreateInstance(slotType, 1);
            quarrySlots.SetValue(CreateSlot(slotType, habitatType, "quarry.worker.001", "quarry_workers", "quarry", "spawn.quarry.a", "Quarry"), 0);
            SetProperty(quarryPool, "Slots", quarrySlots);

            var pools = Array.CreateInstance(poolType, 2);
            pools.SetValue(townPool, 0);
            pools.SetValue(quarryPool, 1);
            SetProperty(definition, "Pools", pools);

            return definition;
        }

        private static object CreateSlot(
            Type slotType,
            Type habitatType,
            string slotId,
            string poolId,
            string areaTag,
            string spawnAnchorId,
            string habitatName)
        {
            var slot = Activator.CreateInstance(slotType);
            SetProperty(slot, "PopulationSlotId", slotId);
            SetProperty(slot, "PoolId", poolId);
            SetProperty(slot, "AreaTag", areaTag);
            SetProperty(slot, "SpawnAnchorId", spawnAnchorId);
            SetProperty(slot, "Habitat", Enum.Parse(habitatType, habitatName));
            SetProperty(slot, "IsProtectedFromContracts", false);
            return slot;
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
            object populationDefinition,
            string idPrefix,
            object spawnHabitat,
            CivilianAppearanceLibrary library)
        {
            var type = typeof(CivilianPopulationRuntimeBridge);
            SetPrivateField(type, bridge, "_populationDefinition", populationDefinition);
            SetPrivateField(type, bridge, "_civilianIdPrefix", idPrefix);
            SetPrivateField(type, bridge, "_spawnHabitat", spawnHabitat);
            SetPrivateField(type, bridge, "_appearanceLibrary", library);
        }

        private static void SetPrivateField(Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on '{type.FullName}'.");
            field.SetValue(instance, value);
        }

        private static Type ResolveRequiredType(string assemblyQualifiedName)
        {
            var type = Type.GetType(assemblyQualifiedName, throwOnError: false);
            Assert.That(type, Is.Not.Null, $"Expected type '{assemblyQualifiedName}' to exist.");
            return type;
        }

        private static void SetProperty(object instance, string propertyName, object value)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}' on '{instance.GetType().FullName}'.");
            property.SetValue(instance, value);
        }

        private static T GetRecordProperty<T>(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}' on '{instance.GetType().FullName}'.");
            return (T)property.GetValue(instance);
        }

        private static void QueueReplacementForOnlyLiveCivilian(CivilianPopulationRuntimeBridge bridge)
        {
            var liveCivilian = bridge.Runtime.Civilians.Single(record => record.IsAlive);
            liveCivilian.IsAlive = false;
            liveCivilian.IsContractEligible = false;
            liveCivilian.RetiredAtDay = 0;
            bridge.Runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
            {
                VacatedCivilianId = liveCivilian.CivilianId,
                QueuedAtDay = 0,
                SpawnAnchorId = liveCivilian.SpawnAnchorId
            });
        }
    }
}
