using NUnit.Framework;
using Reloader.Core.Items;
using Reloader.Inventory;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public class ItemDefinitionStackRulesTests
    {
        [Test]
        public void MaxStack_ClampsToAtLeastOne()
        {
            var definition = ScriptableObject.CreateInstance<ItemDefinition>();
            definition.SetValuesForTests("item-1", maxStack: 0);

            Assert.That(definition.MaxStack, Is.EqualTo(1));

            Object.DestroyImmediate(definition);
        }

        [Test]
        public void MaxStack_AllowsPerItemValues()
        {
            var smallCaliber = ScriptableObject.CreateInstance<ItemDefinition>();
            smallCaliber.SetValuesForTests("ammo-22lr", maxStack: 500);

            var largeCaliber = ScriptableObject.CreateInstance<ItemDefinition>();
            largeCaliber.SetValuesForTests("ammo-50bmg", maxStack: 40);

            Assert.That(smallCaliber.MaxStack, Is.EqualTo(500));
            Assert.That(largeCaliber.MaxStack, Is.EqualTo(40));

            Object.DestroyImmediate(smallCaliber);
            Object.DestroyImmediate(largeCaliber);
        }

        [Test]
        public void SpawnQuantity_ClampsToDefinitionMaxStack()
        {
            var definition = ScriptableObject.CreateInstance<ItemDefinition>();
            definition.SetValuesForTests(
                "ammo-50bmg",
                stackPolicy: ItemStackPolicy.StackByDefinition,
                maxStack: 40);

            var spawn = ScriptableObject.CreateInstance<ItemSpawnDefinition>();
            spawn.SetValuesForTests(definition, quantity: 75);

            Assert.That(spawn.Quantity, Is.EqualTo(40));

            Object.DestroyImmediate(spawn);
            Object.DestroyImmediate(definition);
        }
    }
}
