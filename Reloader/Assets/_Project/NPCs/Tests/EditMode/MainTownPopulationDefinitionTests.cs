using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class MainTownPopulationDefinitionTests
    {
        private const string DefinitionTypeName = "Reloader.NPCs.Generation.MainTownPopulationDefinition, Reloader.NPCs";
        private const string PoolTypeName = "Reloader.NPCs.Generation.MainTownPopulationPoolDefinition, Reloader.NPCs";
        private const string SlotTypeName = "Reloader.NPCs.Generation.MainTownPopulationSlotDefinition, Reloader.NPCs";

        [Test]
        public void Validate_WhenPoolsAreEmpty_ThrowsArgumentException()
        {
            var definitionType = ResolveRequiredType(DefinitionTypeName);
            var poolType = ResolveRequiredType(PoolTypeName);

            var definition = ScriptableObject.CreateInstance(definitionType);
            SetProperty(definition, "Pools", Array.CreateInstance(poolType, 0));

            var validate = definitionType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(validate, Is.Not.Null, "Expected a public Validate() method on MainTownPopulationDefinition.");

            var ex = Assert.Throws<TargetInvocationException>(() => validate.Invoke(definition, null));
            Assert.That(ex?.InnerException, Is.TypeOf<ArgumentException>());
            Assert.That(ex?.InnerException?.Message, Does.Contain("at least one pool"));

            UnityEngine.Object.DestroyImmediate(definition);
        }

        [Test]
        public void Validate_WhenPopulationSlotIdsDuplicate_ThrowsArgumentException()
        {
            var definitionType = ResolveRequiredType(DefinitionTypeName);
            var poolType = ResolveRequiredType(PoolTypeName);
            var slotType = ResolveRequiredType(SlotTypeName);

            var definition = ScriptableObject.CreateInstance(definitionType);
            var pool = Activator.CreateInstance(poolType);
            var slotA = CreateSlot(slotType, "quarry.worker.001", "quarry_workers", "quarry", "spawn.quarry.a", false);
            var slotB = CreateSlot(slotType, "quarry.worker.001", "quarry_workers", "quarry", "spawn.quarry.b", false);

            SetProperty(pool, "PoolId", "quarry_workers");
            SetProperty(pool, "Slots", Array.CreateInstance(slotType, 2));
            var slots = (Array)GetProperty<object>(pool, "Slots");
            slots.SetValue(slotA, 0);
            slots.SetValue(slotB, 1);

            SetProperty(definition, "Pools", Array.CreateInstance(poolType, 1));
            var pools = (Array)GetProperty<object>(definition, "Pools");
            pools.SetValue(pool, 0);

            var validate = definitionType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(validate, Is.Not.Null, "Expected a public Validate() method on MainTownPopulationDefinition.");

            var ex = Assert.Throws<TargetInvocationException>(() => validate.Invoke(definition, null));
            Assert.That(ex?.InnerException, Is.TypeOf<ArgumentException>());
            Assert.That(ex?.InnerException?.Message, Does.Contain("populationSlotId"));

            UnityEngine.Object.DestroyImmediate(definition);
        }

        [Test]
        public void Validate_WhenVendorPoolIsProtected_AcceptsStableVendorSlots()
        {
            var definitionType = ResolveRequiredType(DefinitionTypeName);
            var poolType = ResolveRequiredType(PoolTypeName);
            var slotType = ResolveRequiredType(SlotTypeName);

            var definition = ScriptableObject.CreateInstance(definitionType);
            var vendorPool = Activator.CreateInstance(poolType);
            var vendorSlot = CreateSlot(slotType, "vendor.weapon.001", "vendors", "market", "spawn.vendor.weapon", true);

            SetProperty(vendorPool, "PoolId", "vendors");
            SetProperty(vendorPool, "Slots", Array.CreateInstance(slotType, 1));
            var slots = (Array)GetProperty<object>(vendorPool, "Slots");
            slots.SetValue(vendorSlot, 0);

            SetProperty(definition, "Pools", Array.CreateInstance(poolType, 1));
            var pools = (Array)GetProperty<object>(definition, "Pools");
            pools.SetValue(vendorPool, 0);

            var validate = definitionType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(validate, Is.Not.Null, "Expected a public Validate() method on MainTownPopulationDefinition.");

            Assert.DoesNotThrow(() => validate.Invoke(definition, null));

            UnityEngine.Object.DestroyImmediate(definition);
        }

        private static object CreateSlot(Type slotType, string slotId, string poolId, string areaTag, string spawnAnchorId, bool isProtected)
        {
            var slot = Activator.CreateInstance(slotType);
            SetProperty(slot, "PopulationSlotId", slotId);
            SetProperty(slot, "PoolId", poolId);
            SetProperty(slot, "AreaTag", areaTag);
            SetProperty(slot, "SpawnAnchorId", spawnAnchorId);
            SetProperty(slot, "IsProtectedFromContracts", isProtected);
            return slot;
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

        private static T GetProperty<T>(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}' on '{instance.GetType().FullName}'.");
            return (T)property.GetValue(instance);
        }
    }
}
