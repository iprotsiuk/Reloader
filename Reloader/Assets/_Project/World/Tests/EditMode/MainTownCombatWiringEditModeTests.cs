using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Reloader.Weapons.Runtime;
using Reloader.World.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Tests.EditMode
{
    public class MainTownCombatWiringEditModeTests
    {
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";

        private static readonly MethodInfo ShouldSeedDefaultWeaponPoseTuningMethod =
            typeof(MainTownCombatWiring).GetMethod(
                "ShouldSeedDefaultWeaponPoseTuning",
                BindingFlags.NonPublic | BindingFlags.Static);

        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("PoseTuningRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }

        [Test]
        public void ShouldSeedDefaultWeaponPoseTuning_WhenHelperHasFreshDefaultBlendSpeed_ReturnsTrue()
        {
            var helper = _root.AddComponent<WeaponViewPoseTuningHelper>();
            var serializedObject = new SerializedObject(helper);

            var shouldSeed = InvokeShouldSeedDefaultWeaponPoseTuning(serializedObject);

            Assert.That(shouldSeed, Is.True);
        }

        [Test]
        public void ShouldSeedDefaultWeaponPoseTuning_WhenHelperHasAuthoredPose_ReturnsFalse()
        {
            var helper = _root.AddComponent<WeaponViewPoseTuningHelper>();
            var serializedObject = new SerializedObject(helper);
            serializedObject.FindProperty("_adsLocalPosition").vector3Value = new Vector3(0f, 0.2f, 0.05f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var shouldSeed = InvokeShouldSeedDefaultWeaponPoseTuning(serializedObject);

            Assert.That(shouldSeed, Is.False);
        }

        [Test]
        public void MainTownScene_RemovesStarterFloorPickups_InFavorOfVendorAndChestAuthority()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                Assert.That(FindRoot(scene, "WeaponSpawn_RifleStarter_LPSP"), Is.Null,
                    "MainTown should not keep a floor-spawned Kar98k pickup.");
                Assert.That(FindRoot(scene, "WeaponSpawn_RifleStarter"), Is.Null,
                    "Legacy duplicate rifle pickup should stay removed.");
                Assert.That(FindRoot(scene, "WeaponSpawn_RifleStarter_Exported"), Is.Null,
                    "Exported duplicate rifle pickup should stay removed.");
                Assert.That(FindRoot(scene, "WeaponSpawn_PistolStarter_LPSP"), Is.Null,
                    "MainTown should not keep a floor-spawned pistol pickup.");
                Assert.That(FindRoot(scene, "AmmoSpawn_308_LPSP"), Is.Null,
                    ".308 starter ammo pickup should stay removed from MainTown.");
                Assert.That(FindRoot(scene, "AmmoSpawn_9x19_LPSP"), Is.Null,
                    "9x19 starter ammo pickup should stay removed from MainTown.");
                Assert.That(FindRoot(scene, "AmmoSpawn_Cartridge308"), Is.Null,
                    "Legacy loose .308 cartridge pickup should stay removed from MainTown.");
                Assert.That(FindRoot(scene, "AmmoSpawn_Cartridge308_Exported"), Is.Null,
                    "Exported loose .308 cartridge pickup should stay removed from MainTown.");
                Assert.That(FindRoot(scene, "AmmoSpawn_Bullet308"), Is.Null,
                    "Legacy loose .308 bullet pickup should stay removed from MainTown.");
                Assert.That(FindRoot(scene, "AmmoSpawn_Bullet308_Exported"), Is.Null,
                    "Exported loose .308 bullet pickup should stay removed from MainTown.");
                Assert.That(FindRoot(scene, "AmmoBox_100R_308"), Is.Null,
                    "Legacy .308 ammo box floor spawn should stay removed from MainTown.");
                Assert.That(FindRoot(scene, "AmmoBox_100R_308_Exported"), Is.Null,
                    "Exported .308 ammo box floor spawn should stay removed from MainTown.");
                Assert.That(FindRoot(scene, "AttachmentSpawn_Kar98kScope"), Is.Null,
                    "Kar98k scope pickup should move to the vendor/chest authority path.");
                Assert.That(FindRoot(scene, "AttachmentSpawn_Kar98kMuzzle"), Is.Null,
                    "Kar98k muzzle pickup should move to the vendor/chest authority path.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        [Test]
        public void MainTownScene_SeedsStorageChest_WithRifleAndCanikStarterLoadout()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var storageChest = FindRoot(scene, "StorageChest");
                Assert.That(storageChest, Is.Not.Null, "MainTown should keep the authored StorageChest root.");

                var seederType = Type.GetType("Reloader.Inventory.WorldStorageContainerSeedLoadout, Reloader.Inventory");
                Assert.That(seederType, Is.Not.Null, "Expected chest starter-loadout seeder type.");

                var seeder = storageChest!.GetComponent(seederType!);
                Assert.That(seeder, Is.Not.Null, "StorageChest should seed the supported starter loadout.");

                var entriesProperty = new SerializedObject(seeder).FindProperty("_entries");
                Assert.That(entriesProperty, Is.Not.Null);

                var itemIds = new List<string>();
                var ammo308Quantity = 0;
                for (var i = 0; i < entriesProperty.arraySize; i++)
                {
                    var element = entriesProperty.GetArrayElementAtIndex(i);
                    var definition = element.FindPropertyRelative("_itemDefinition").objectReferenceValue;
                    Assert.That(definition, Is.Not.Null, "Seed entries should point to real item definitions.");

                    var itemId = new SerializedObject(definition).FindProperty("_definitionId")!.stringValue;
                    itemIds.Add(itemId);
                    if (itemId == "ammo-factory-308-147-fmj")
                    {
                        ammo308Quantity = element.FindPropertyRelative("_quantity").intValue;
                    }
                }

                CollectionAssert.AreEquivalent(
                    new[]
                    {
                        "weapon-kar98k",
                        "weapon-canik-tp9",
                        "att-kar98k-scope-remote-a",
                        "att-kar98k-muzzle-device-c",
                        "ammo-factory-308-147-fmj"
                    },
                    itemIds);
                Assert.That(ammo308Quantity, Is.EqualTo(50), "Starter chest should seed exactly 50 rounds of .308.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        private static bool InvokeShouldSeedDefaultWeaponPoseTuning(SerializedObject serializedObject)
        {
            Assert.That(ShouldSeedDefaultWeaponPoseTuningMethod, Is.Not.Null);
            return (bool)ShouldSeedDefaultWeaponPoseTuningMethod.Invoke(null, new object[] { serializedObject });
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == rootName)
                {
                    return root;
                }
            }

            return null;
        }
    }
}
