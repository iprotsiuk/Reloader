using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Items;
using UnityEditor;
using UnityEngine;

namespace Reloader.Inventory.Tests.PlayMode
{
    public class WorldStorageContainerSeedLoadoutPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            StorageRuntimeBridge.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            StorageRuntimeBridge.ResetForTests();
        }

        [Test]
        public void EnsureSeeded_WhenContainerIsEmpty_FillsConfiguredStarterLoadout()
        {
            var seederType = ResolveSeederType();
            var host = new GameObject("StorageChest");
            var container = host.AddComponent<WorldStorageContainer>();
            var seeder = host.AddComponent(seederType);

            var rifle = ScriptableObject.CreateInstance<ItemDefinition>();
            rifle.SetValuesForTests("weapon-kar98k", displayName: "Kar98k", maxStack: 1);

            var ammo = ScriptableObject.CreateInstance<ItemDefinition>();
            ammo.SetValuesForTests(
                "ammo-factory-308-147-fmj",
                displayName: "PMC Bronze .308 147gr FMJ Cartridge",
                stackPolicy: ItemStackPolicy.StackByDefinition,
                maxStack: 999);

            try
            {
                ConfigureSeeder(seeder, container, (rifle, 1), (ammo, 50));
                InvokeEnsureSeeded(seederType, seeder);

                var runtime = container.EnsureRegistered();
                Assert.That(runtime.GetSlotItemId(0), Is.EqualTo("weapon-kar98k"));
                Assert.That(runtime.TryGetSlotStack(1, out var ammoStack), Is.True);
                Assert.That(ammoStack, Is.Not.Null);
                Assert.That(ammoStack!.ItemId, Is.EqualTo("ammo-factory-308-147-fmj"));
                Assert.That(ammoStack.Quantity, Is.EqualTo(50));
                Assert.That(ammoStack.MaxStack, Is.EqualTo(999));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rifle);
                UnityEngine.Object.DestroyImmediate(ammo);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void EnsureSeeded_WhenContainerAlreadyContainsItems_DoesNotOverwritePlayerStorage()
        {
            var seederType = ResolveSeederType();
            var host = new GameObject("StorageChest");
            var container = host.AddComponent<WorldStorageContainer>();
            var seeder = host.AddComponent(seederType);

            var rifle = ScriptableObject.CreateInstance<ItemDefinition>();
            rifle.SetValuesForTests("weapon-kar98k", displayName: "Kar98k", maxStack: 1);

            try
            {
                ConfigureSeeder(seeder, container, (rifle, 1));

                var runtime = container.EnsureRegistered();
                runtime.TrySetSlotStack(0, new ItemStackState("item-player-custom", 1, 1));

                InvokeEnsureSeeded(seederType, seeder);

                Assert.That(runtime.GetSlotItemId(0), Is.EqualTo("item-player-custom"));
                Assert.That(runtime.GetSlotItemId(1), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rifle);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static Type ResolveSeederType()
        {
            var seederType = Type.GetType("Reloader.Inventory.WorldStorageContainerSeedLoadout, Reloader.Inventory");
            Assert.That(seederType, Is.Not.Null, "Expected WorldStorageContainerSeedLoadout type.");
            return seederType!;
        }

        private static void ConfigureSeeder(Component seeder, WorldStorageContainer container, params (ItemDefinition definition, int quantity)[] entries)
        {
            var serializedObject = new SerializedObject(seeder);
            serializedObject.FindProperty("_container").objectReferenceValue = container;

            var entriesProperty = serializedObject.FindProperty("_entries");
            entriesProperty.arraySize = entries.Length;
            for (var i = 0; i < entries.Length; i++)
            {
                var element = entriesProperty.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_itemDefinition").objectReferenceValue = entries[i].definition;
                element.FindPropertyRelative("_quantity").intValue = entries[i].quantity;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var seededField = seeder.GetType().GetField("_seedAttempted", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(seededField, Is.Not.Null, "Expected _seedAttempted field for authored Awake guard.");
            seededField!.SetValue(seeder, false);
        }

        private static void InvokeEnsureSeeded(Type seederType, Component seeder)
        {
            var method = seederType.GetMethod("EnsureSeeded", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, "Expected public EnsureSeeded method.");
            method!.Invoke(seeder, null);
        }
    }
}
