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

        [Test]
        public void PrepareForSave_WhenPopulationDefinitionProvidesSlots_AssignsOneOccupantPerSlot()
        {
            var definitionType = ResolveRequiredType(DefinitionTypeName);
            var poolType = ResolveRequiredType(PoolTypeName);
            var slotType = ResolveRequiredType(SlotTypeName);

            var go = new GameObject("CivilianPopulationRuntimeBridge");
            var bridge = go.AddComponent<CivilianPopulationRuntimeBridge>();

            try
            {
                ConfigureBridge(
                    bridge,
                    populationDefinition: CreateDefinition(definitionType, poolType, slotType),
                    idPrefix: "citizen.mainTown",
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

        private static object CreateDefinition(Type definitionType, Type poolType, Type slotType)
        {
            var definition = ScriptableObject.CreateInstance(definitionType);

            var quarryPool = Activator.CreateInstance(poolType);
            var townsfolkPool = Activator.CreateInstance(poolType);

            SetProperty(quarryPool, "PoolId", "quarry_workers");
            SetProperty(townsfolkPool, "PoolId", "townsfolk");

            var quarrySlots = Array.CreateInstance(slotType, 1);
            quarrySlots.SetValue(CreateSlot(slotType, "quarry.worker.001", "quarry_workers", "quarry", "spawn.quarry.a"), 0);
            SetProperty(quarryPool, "Slots", quarrySlots);

            var townSlots = Array.CreateInstance(slotType, 1);
            townSlots.SetValue(CreateSlot(slotType, "townsfolk.001", "townsfolk", "downtown", "spawn.mainstreet.a"), 0);
            SetProperty(townsfolkPool, "Slots", townSlots);

            var pools = Array.CreateInstance(poolType, 2);
            pools.SetValue(quarryPool, 0);
            pools.SetValue(townsfolkPool, 1);
            SetProperty(definition, "Pools", pools);

            return definition;
        }

        private static object CreateSlot(Type slotType, string slotId, string poolId, string areaTag, string spawnAnchorId)
        {
            var slot = Activator.CreateInstance(slotType);
            SetProperty(slot, "PopulationSlotId", slotId);
            SetProperty(slot, "PoolId", poolId);
            SetProperty(slot, "AreaTag", areaTag);
            SetProperty(slot, "SpawnAnchorId", spawnAnchorId);
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
            CivilianAppearanceLibrary library)
        {
            var type = typeof(CivilianPopulationRuntimeBridge);
            SetPrivateField(type, bridge, "_populationDefinition", populationDefinition);
            SetPrivateField(type, bridge, "_civilianIdPrefix", idPrefix);
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
    }
}
