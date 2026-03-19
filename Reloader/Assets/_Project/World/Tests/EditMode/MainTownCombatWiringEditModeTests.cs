using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Tests.EditMode
{
    public class MainTownCombatWiringEditModeTests
    {
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string PlayerRootPrefabPath = "Assets/_Project/Player/Prefabs/PlayerRoot_MainTown.prefab";

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

        [Test]
        public void MainTownScene_PlayerRoot_UsesWeaponHandRigController_AndNoSceneOwnedPoseHelper()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var playerRoot = FindRoot(scene, "PlayerRoot");
                Assert.That(playerRoot, Is.Not.Null, "Expected authored PlayerRoot in MainTown.");
                AssertWeaponHandRigOwner(playerRoot!, "MainTown scene PlayerRoot");
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
        public void PlayerRootMainTownPrefab_UsesWeaponHandRigController_AndNoSceneOwnedPoseHelper()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerRootPrefabPath);

            try
            {
                AssertWeaponHandRigOwner(prefabRoot, "PlayerRoot_MainTown prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
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

        private static void AssertWeaponHandRigOwner(GameObject playerRoot, string context)
        {
            Assert.That(playerRoot, Is.Not.Null, $"{context} should exist.");

            var handRigController = playerRoot.GetComponent("WeaponHandRigController");
            Assert.That(handRigController, Is.Not.Null, $"{context} should include WeaponHandRigController.");
            Assert.That(playerRoot.GetComponent("WeaponViewPoseTuningHelper"), Is.Null, $"{context} should not carry WeaponViewPoseTuningHelper.");

            var serialized = new SerializedObject(handRigController);
            Assert.That(serialized.FindProperty("_enabledInPlayMode")?.boolValue, Is.True, $"{context} should keep the hand rig active in play mode.");
            Assert.That(serialized.FindProperty("_driveLeftHand")?.boolValue, Is.True, $"{context} should keep left-hand rigging enabled.");
            Assert.That(serialized.FindProperty("_driveRightHand")?.boolValue, Is.False, $"{context} should keep right hand animation-authored.");

            var weaponController = playerRoot.GetComponent("PlayerWeaponController");
            Assert.That(weaponController, Is.Not.Null, $"{context} should include PlayerWeaponController.");

            var weaponControllerSerialized = new SerializedObject(weaponController);
            Assert.That(
                weaponControllerSerialized.FindProperty("_allowSceneWideDependencyLookup")?.boolValue,
                Is.False,
                $"{context} should keep scene-wide dependency lookup disabled for the weapon controller.");
        }


        [Test]
        public void MainTownScene_HasAuthoredContractRuntimeAndHumanTargetSlice()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var providerType = Type.GetType("Reloader.Contracts.Runtime.StaticContractRuntimeProvider, Reloader.Core");
                var payoutReceiverType = Type.GetType("Reloader.Economy.EconomyContractPayoutReceiver, Reloader.Economy");

                Assert.That(providerType, Is.Not.Null);
                Assert.That(payoutReceiverType, Is.Not.Null);

                var runtimeRoot = FindRoot(scene, "MainTownContractRuntime");
                Assert.That(runtimeRoot, Is.Not.Null, "Expected an authored contract runtime root in MainTown.");
                var provider = runtimeRoot!.GetComponent(providerType!);
                Assert.That(provider, Is.Not.Null, "Expected StaticContractRuntimeProvider on MainTownContractRuntime.");

                var providerSerializedObject = new SerializedObject((UnityEngine.Object)provider);
                Assert.That(providerSerializedObject.FindProperty("_availableContract")!.objectReferenceValue, Is.Not.Null, "Contract runtime should point at an authored contract asset.");
                Assert.That(providerSerializedObject.FindProperty("_payoutReceiverBehaviour")!.objectReferenceValue, Is.Not.Null, "Contract runtime should point at the payout receiver bridge.");
                Assert.That(providerSerializedObject.FindProperty("_searchDurationSeconds")!.floatValue, Is.EqualTo(30f).Within(0.01f));

                var economyController = FindRoot(scene, "EconomyController");
                Assert.That(economyController, Is.Not.Null);
                Assert.That(economyController!.GetComponent(payoutReceiverType!), Is.Not.Null, "EconomyController should expose the contract payout receiver bridge.");

                var targetRoot = FindRoot(scene, "ContractTarget_Volkov");
                Assert.That(targetRoot, Is.Null, "MainTown should no longer keep the authored ContractTarget_Volkov fixture once procedural civilians own the contract slice.");
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
}
}
