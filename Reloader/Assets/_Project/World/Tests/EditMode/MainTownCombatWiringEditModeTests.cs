using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
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
                AssertPlayerCameraDefaultsOwnership(playerRoot!, "MainTown scene PlayerRoot");
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
                AssertPlayerCameraDefaultsOwnership(prefabRoot, "PlayerRoot_MainTown prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [Test]
        public void MainTownCombatWiring_ResolveWeaponViewParent_UsesExplicitWeaponPresentationRoot_AndDoesNotCreateFallback()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerRoot.transform, false);

            var weaponPresentationRoot = new GameObject("WeaponPresentationRoot").transform;
            weaponPresentationRoot.SetParent(cameraPivot, false);
            weaponPresentationRoot.localPosition = Vector3.zero;
            weaponPresentationRoot.localRotation = Quaternion.identity;
            weaponPresentationRoot.localScale = Vector3.one;

            var legacyIkHandGun = new GameObject("ik_hand_gun").transform;
            legacyIkHandGun.SetParent(cameraPivot, false);
            legacyIkHandGun.gameObject.layer = 17;

            try
            {
                var resolveWeaponViewParent = typeof(MainTownCombatWiring).GetMethod(
                    "ResolveWeaponViewParent",
                    BindingFlags.Static | BindingFlags.NonPublic);

                Assert.That(resolveWeaponViewParent, Is.Not.Null, "Expected MainTownCombatWiring.ResolveWeaponViewParent to exist.");

                var resolvedParent = resolveWeaponViewParent!.Invoke(null, new object[] { cameraPivot }) as Transform;

                Assert.That(resolvedParent, Is.Not.Null);
                Assert.That(resolvedParent.name, Is.EqualTo("WeaponPresentationRoot"));
                Assert.That(resolvedParent.parent, Is.EqualTo(cameraPivot));
                Assert.That(cameraPivot.Find("WeaponPresentationRoot"), Is.SameAs(resolvedParent));
                Assert.That(resolvedParent.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(resolvedParent.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(resolvedParent.localScale, Is.EqualTo(Vector3.one));
                Assert.That(resolvedParent.gameObject.layer, Is.EqualTo(weaponPresentationRoot.gameObject.layer));
                Assert.That(resolvedParent, Is.Not.SameAs(legacyIkHandGun));
                Assert.That(cameraPivot.Find("LegacyWeaponPresentationRoot"), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerRoot);
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

        private static void AssertPlayerCameraDefaultsOwnership(GameObject playerRoot, string context)
        {
            var cameraDefaults = playerRoot.GetComponent("PlayerCameraDefaults");
            Assert.That(cameraDefaults, Is.Not.Null, $"{context} should include PlayerCameraDefaults.");

            var serialized = new SerializedObject(cameraDefaults);
            var cameraPivot = serialized.FindProperty("_cameraPivot")?.objectReferenceValue as Transform;
            var playerArmsRoot = serialized.FindProperty("_playerArmsRoot")?.objectReferenceValue as Transform;
            var playerArmsAnimator = serialized.FindProperty("_playerArmsAnimator")?.objectReferenceValue as Animator;
            var viewmodelCameraParent = serialized.FindProperty("_viewmodelCameraParent")?.objectReferenceValue as Transform;
            var weaponPresentationRoot = serialized.FindProperty("_weaponPresentationRoot")?.objectReferenceValue as Transform;

            Assert.That(serialized.FindProperty("_mainCamera")?.objectReferenceValue, Is.Not.Null, $"{context} should serialize the main camera.");
            Assert.That(serialized.FindProperty("_cameraFollowTarget")?.objectReferenceValue, Is.Not.Null, $"{context} should serialize the camera follow target.");
            Assert.That(serialized.FindProperty("_cameraLookTarget")?.objectReferenceValue, Is.Not.Null, $"{context} should serialize the camera look target.");
            Assert.That(viewmodelCameraParent, Is.Not.Null, $"{context} should serialize the viewmodel camera parent.");
            Assert.That(cameraPivot, Is.Not.Null, $"{context} should serialize the camera pivot.");
            Assert.That(playerArmsRoot, Is.Not.Null, $"{context} should serialize the player arms root.");
            Assert.That(playerArmsAnimator, Is.Not.Null, $"{context} should serialize the player arms animator.");
            Assert.That(weaponPresentationRoot, Is.Not.Null, $"{context} should serialize the weapon presentation root.");
            Assert.That(cameraPivot!.parent, Is.SameAs(playerRoot.transform), $"{context} should keep CameraPivot under PlayerRoot.");
            Assert.That(playerArmsRoot!.parent, Is.SameAs(cameraPivot), $"{context} should keep PlayerArms under CameraPivot.");
            Assert.That(viewmodelCameraParent, Is.SameAs(cameraPivot), $"{context} should keep ViewmodelCameraParent on the CameraPivot contract.");
            Assert.That(weaponPresentationRoot!.parent, Is.SameAs(cameraPivot), $"{context} should keep WeaponPresentationRoot under CameraPivot.");
            Assert.That(playerRoot.transform.Find("CameraPivot/PlayerArms"), Is.SameAs(playerArmsRoot), $"{context} should keep PlayerArms as a sibling branch of WeaponPresentationRoot.");
            Assert.That(playerRoot.transform.Find("CameraPivot/WeaponPresentationRoot"), Is.SameAs(weaponPresentationRoot), $"{context} should keep WeaponPresentationRoot as the live mount root.");
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
