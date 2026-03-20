using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Reloader.Player;
using Reloader.World.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;
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
            var scenePath = Path.Combine(Application.dataPath, "_Project", "World", "Scenes", "MainTown.unity");
            var sceneText = File.ReadAllText(scenePath);

            Assert.That(sceneText, Does.Contain("m_Name: PlayerRoot"), "Expected authored PlayerRoot in MainTown scene file.");
            Assert.That(sceneText, Does.Contain("m_EditorClassIdentifier: Reloader.Weapons::Reloader.Player.Viewmodel.WeaponHandRigController"), "MainTown scene should keep the hand rig controller authored on PlayerRoot.");
            Assert.That(sceneText, Does.Contain("_handTargetRoot: {fileID: 2662114487113379158}"), "MainTown scene should serialize the explicit WeaponHandRigTargets root.");
            Assert.That(sceneText, Does.Contain("m_Name: WeaponHandRigTargets"), "MainTown scene should author the hand-target root as a first-person child.");
            Assert.That(sceneText, Does.Contain("m_Father: {fileID: 936686685}"), "MainTown scene should keep WeaponHandRigTargets under CameraPivot.");
        }

        [Test]
        public void MainTownCombatWiring_RejectsMissingExplicitHandTargetRoot()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var handRigController = playerRoot.AddComponent<Reloader.Player.Viewmodel.WeaponHandRigController>();
            var cameraPivot = new GameObject("CameraPivot").transform;

            try
            {
                var tryResolveHandTargetRoot = typeof(MainTownCombatWiring).GetMethod(
                    "TryResolveWeaponHandRigTargets",
                    BindingFlags.Static | BindingFlags.NonPublic);

                Assert.That(tryResolveHandTargetRoot, Is.Not.Null, "Expected MainTownCombatWiring.TryResolveWeaponHandRigTargets to exist.");

                var args = new object[] { handRigController, cameraPivot, null };
                var resolved = (bool)tryResolveHandTargetRoot!.Invoke(null, args)!;

                Assert.That(resolved, Is.False,
                    "MainTownCombatWiring should fail closed when the explicit WeaponHandRigController hand-target root is missing.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraPivot.gameObject);
                UnityEngine.Object.DestroyImmediate(playerRoot);
            }
        }

        [Test]
        public void MainTownCombatWiring_DoesNotAddFirstPersonComponents_WhenHandTargetRootMissing()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerRoot.transform, false);

            var playerArmsRoot = new GameObject("PlayerArms").transform;
            playerArmsRoot.SetParent(cameraPivot, false);
            var playerArmsVisual = new GameObject("PlayerArmsVisual");
            playerArmsVisual.transform.SetParent(playerArmsRoot, false);
            var playerArmsAnimator = playerArmsVisual.AddComponent<Animator>();

            var cameraLookTarget = new GameObject("CameraLookTarget").transform;
            cameraLookTarget.SetParent(cameraPivot, false);
            var weaponPresentationRoot = new GameObject("WeaponPresentationRoot").transform;
            weaponPresentationRoot.SetParent(cameraPivot, false);

            var mainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            mainCamera.transform.SetParent(cameraPivot, false);

            var cameraDefaults = playerRoot.AddComponent<PlayerCameraDefaults>();
            SetField(cameraDefaults, "_cameraPivot", cameraPivot);
            SetField(cameraDefaults, "_playerArmsRoot", playerArmsRoot);
            SetField(cameraDefaults, "_playerArmsAnimator", playerArmsAnimator);
            SetField(cameraDefaults, "_cameraLookTarget", cameraLookTarget);
            SetField(cameraDefaults, "_mainCamera", mainCamera);
            SetField(cameraDefaults, "_weaponPresentationRoot", weaponPresentationRoot);

            playerRoot.AddComponent<PlayerInputReader>();
            var playerInventoryControllerType = Type.GetType("Reloader.Inventory.PlayerInventoryController, Reloader.Inventory");
            Assert.That(playerInventoryControllerType, Is.Not.Null, "Expected PlayerInventoryController type to exist.");
            playerRoot.AddComponent(playerInventoryControllerType!);
            playerRoot.AddComponent<CharacterController>();
            playerRoot.AddComponent<PlayerLookController>();
            playerRoot.AddComponent<Reloader.Player.Viewmodel.WeaponHandRigController>();

            var starterRifle = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Project/Weapons/Data/Weapons/StarterRifle.asset");
            var starterPistol = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Project/Weapons/Data/Weapons/StarterPistol.asset");
            var projectilePrefabGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Weapons/Prefabs/WeaponProjectile.prefab");
            var projectilePrefabType = Type.GetType("Reloader.Weapons.Ballistics.WeaponProjectile, Reloader.Weapons");

            Assert.That(starterRifle, Is.Not.Null);
            Assert.That(starterPistol, Is.Not.Null);
            Assert.That(projectilePrefabGo, Is.Not.Null);
            Assert.That(projectilePrefabType, Is.Not.Null, "Expected WeaponProjectile type to exist.");

            var projectilePrefab = projectilePrefabGo!.GetComponent(projectilePrefabType!);
            Assert.That(projectilePrefab, Is.Not.Null, "Expected WeaponProjectile component on the projectile prefab.");

            try
            {
                var wireScene = typeof(MainTownCombatWiring).GetMethod(
                    "WireScene",
                    BindingFlags.Static | BindingFlags.NonPublic);

                Assert.That(wireScene, Is.Not.Null, "Expected MainTownCombatWiring.WireScene to exist.");
                LogAssert.Expect(LogType.Error, "MainTown combat wiring failed: explicit WeaponHandRigTargets root is missing or not parented under CameraPivot.");

                var resolved = (bool)wireScene!.Invoke(null, new object[] { starterRifle, starterPistol, projectilePrefab })!;

                Assert.That(resolved, Is.False,
                    "MainTown combat wiring should fail closed before adding first-person components when the hand-target root is missing.");
                Assert.That(cameraPivot.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(cameraLookTarget.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(playerRoot.GetComponent("PlayerWeaponController"), Is.Null);
                Assert.That(playerRoot.GetComponent("PlayerWeaponAnimationBinder"), Is.Null);
                Assert.That(playerRoot.GetComponent("FpsViewmodelAnimatorDriver"), Is.Null);
                Assert.That(playerRoot.GetComponent("ViewmodelAnimationAdapter"), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerRoot);
            }
        }

        [Test]
        public void MainTownCombatWiring_DoesNotAddPlayerCameraDefaults_WhenMissingFromPlayerRoot()
        {
            var playerRoot = new GameObject("PlayerRoot");

            var starterRifle = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Project/Weapons/Data/Weapons/StarterRifle.asset");
            var starterPistol = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Project/Weapons/Data/Weapons/StarterPistol.asset");
            var projectilePrefabGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Weapons/Prefabs/WeaponProjectile.prefab");
            var projectilePrefabType = Type.GetType("Reloader.Weapons.Ballistics.WeaponProjectile, Reloader.Weapons");

            Assert.That(starterRifle, Is.Not.Null);
            Assert.That(starterPistol, Is.Not.Null);
            Assert.That(projectilePrefabGo, Is.Not.Null);
            Assert.That(projectilePrefabType, Is.Not.Null, "Expected WeaponProjectile type to exist.");

            var projectilePrefab = projectilePrefabGo!.GetComponent(projectilePrefabType!);
            Assert.That(projectilePrefab, Is.Not.Null, "Expected WeaponProjectile component on the projectile prefab.");

            try
            {
                var wireScene = typeof(MainTownCombatWiring).GetMethod(
                    "WireScene",
                    BindingFlags.Static | BindingFlags.NonPublic);

                Assert.That(wireScene, Is.Not.Null, "Expected MainTownCombatWiring.WireScene to exist.");
                LogAssert.Expect(LogType.Error, "MainTown combat wiring failed: PlayerRoot must already have authored PlayerCameraDefaults.");

                var resolved = (bool)wireScene!.Invoke(null, new object[] { starterRifle, starterPistol, projectilePrefab })!;

                Assert.That(resolved, Is.False,
                    "MainTown combat wiring should fail closed when PlayerCameraDefaults is missing.");
                Assert.That(playerRoot.GetComponent<PlayerCameraDefaults>(), Is.Null,
                    "MainTown combat wiring must not add PlayerCameraDefaults as a repair step.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerRoot);
            }
        }

        [Test]
        public void MainTownCombatWiring_DoesNotAddCombatViewmodelComponents_WhenAuthoredBundleMissing()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("PresentationPivot").transform;
            cameraPivot.SetParent(playerRoot.transform, false);

            var playerArmsRoot = new GameObject("AuthoredArmsRoot").transform;
            playerArmsRoot.SetParent(cameraPivot, false);
            var playerArmsVisual = new GameObject("AuthoredArmsVisual");
            playerArmsVisual.transform.SetParent(playerArmsRoot, false);
            var playerArmsAnimator = playerArmsVisual.AddComponent<Animator>();

            var cameraLookTarget = new GameObject("CameraLookTarget").transform;
            cameraLookTarget.SetParent(cameraPivot, false);
            var weaponPresentationRoot = new GameObject("ExplicitWeaponPresentationRoot").transform;
            weaponPresentationRoot.SetParent(cameraPivot, false);

            var mainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            mainCamera.transform.SetParent(cameraPivot, false);

            var cameraDefaults = playerRoot.AddComponent<PlayerCameraDefaults>();
            SetField(cameraDefaults, "_cameraPivot", cameraPivot);
            SetField(cameraDefaults, "_playerArmsRoot", playerArmsRoot);
            SetField(cameraDefaults, "_playerArmsAnimator", playerArmsAnimator);
            SetField(cameraDefaults, "_cameraLookTarget", cameraLookTarget);
            SetField(cameraDefaults, "_mainCamera", mainCamera);
            SetField(cameraDefaults, "_weaponPresentationRoot", weaponPresentationRoot);
            SetField(cameraDefaults, "_viewmodelCameraParent", cameraPivot);

            var handTargetRoot = new GameObject("WeaponHandRigTargets").transform;
            handTargetRoot.SetParent(cameraPivot, false);
            var handRigController = playerRoot.AddComponent<Reloader.Player.Viewmodel.WeaponHandRigController>();
            SetField(handRigController, "_handTargetRoot", handTargetRoot);

            playerRoot.AddComponent<PlayerInputReader>();
            var playerInventoryControllerType = Type.GetType("Reloader.Inventory.PlayerInventoryController, Reloader.Inventory");
            Assert.That(playerInventoryControllerType, Is.Not.Null, "Expected PlayerInventoryController type to exist.");
            playerRoot.AddComponent(playerInventoryControllerType!);
            playerRoot.AddComponent<CharacterController>();
            playerRoot.AddComponent<PlayerLookController>();

            var starterRifle = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Project/Weapons/Data/Weapons/StarterRifle.asset");
            var starterPistol = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Project/Weapons/Data/Weapons/StarterPistol.asset");
            var projectilePrefabGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Weapons/Prefabs/WeaponProjectile.prefab");
            var projectilePrefabType = Type.GetType("Reloader.Weapons.Ballistics.WeaponProjectile, Reloader.Weapons");

            Assert.That(starterRifle, Is.Not.Null);
            Assert.That(starterPistol, Is.Not.Null);
            Assert.That(projectilePrefabGo, Is.Not.Null);
            Assert.That(projectilePrefabType, Is.Not.Null, "Expected WeaponProjectile type to exist.");

            var projectilePrefab = projectilePrefabGo!.GetComponent(projectilePrefabType!);
            Assert.That(projectilePrefab, Is.Not.Null, "Expected WeaponProjectile component on the projectile prefab.");

            try
            {
                var wireScene = typeof(MainTownCombatWiring).GetMethod(
                    "WireScene",
                    BindingFlags.Static | BindingFlags.NonPublic);

                Assert.That(wireScene, Is.Not.Null, "Expected MainTownCombatWiring.WireScene to exist.");
                LogAssert.Expect(LogType.Error,
                    "MainTown combat wiring failed: PlayerRoot must already have authored PlayerWeaponController, PlayerWeaponAnimationBinder, FpsViewmodelAnimatorDriver, and ViewmodelAnimationAdapter.");

                var resolved = (bool)wireScene!.Invoke(null, new object[] { starterRifle, starterPistol, projectilePrefab })!;

                Assert.That(resolved, Is.False,
                    "MainTown combat wiring should fail closed when the authored first-person component bundle is missing.");
                Assert.That(playerRoot.GetComponent("PlayerWeaponController"), Is.Null);
                Assert.That(playerRoot.GetComponent("PlayerWeaponAnimationBinder"), Is.Null);
                Assert.That(playerRoot.GetComponent("FpsViewmodelAnimatorDriver"), Is.Null);
                Assert.That(playerRoot.GetComponent("ViewmodelAnimationAdapter"), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerRoot);
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

            var explicitWeaponPresentationRoot = new GameObject("ExplicitWeaponPresentationRoot").transform;
            explicitWeaponPresentationRoot.SetParent(cameraPivot, false);
            var cameraDefaults = playerRoot.AddComponent<PlayerCameraDefaults>();
            SetField(cameraDefaults, "_cameraPivot", cameraPivot);
            SetField(cameraDefaults, "_weaponPresentationRoot", explicitWeaponPresentationRoot);

            var weaponPresentationRoot = explicitWeaponPresentationRoot;
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

                var resolvedParent = resolveWeaponViewParent!.Invoke(null, new object[] { cameraDefaults }) as Transform;

                Assert.That(resolvedParent, Is.Not.Null);
                Assert.That(resolvedParent, Is.SameAs(explicitWeaponPresentationRoot));
                Assert.That(resolvedParent.parent, Is.EqualTo(cameraPivot));
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

        [Test]
        public void MainTownCombatWiring_ResolveWeaponViewParent_ReturnsNull_WhenExplicitWeaponPresentationRootMissing_EvenIfHierarchyContainsOne()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerRoot.transform, false);

            var legacyNamedWeaponPresentationRoot = new GameObject("WeaponPresentationRoot").transform;
            legacyNamedWeaponPresentationRoot.SetParent(cameraPivot, false);

            var cameraDefaults = playerRoot.AddComponent<PlayerCameraDefaults>();
            SetField(cameraDefaults, "_cameraPivot", cameraPivot);

            try
            {
                var resolveWeaponViewParent = typeof(MainTownCombatWiring).GetMethod(
                    "ResolveWeaponViewParent",
                    BindingFlags.Static | BindingFlags.NonPublic);

                Assert.That(resolveWeaponViewParent, Is.Not.Null, "Expected MainTownCombatWiring.ResolveWeaponViewParent to exist.");

                var resolvedParent = resolveWeaponViewParent!.Invoke(null, new object[] { cameraDefaults }) as Transform;

                Assert.That(resolvedParent, Is.Null,
                    "MainTownCombatWiring should fail closed when the explicit PlayerCameraDefaults contract lacks WeaponPresentationRoot.");
                Assert.That(cameraPivot.Find("WeaponPresentationRoot"), Is.SameAs(legacyNamedWeaponPresentationRoot),
                    "The helper must not recover the root from a hierarchy-name search.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerRoot);
            }
        }

        [Test]
        public void MainTownCombatWiring_TryResolveMainTownDependencies_FailsClosed_WhenExplicitMainCameraMissing_EvenIfCameraMainExists()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerRoot.transform, false);
            var playerArmsRoot = new GameObject("PlayerArms").transform;
            playerArmsRoot.SetParent(cameraPivot, false);
            var playerArmsVisual = new GameObject("PlayerArmsVisual");
            playerArmsVisual.transform.SetParent(playerArmsRoot, false);
            var playerArmsAnimator = playerArmsVisual.AddComponent<Animator>();
            var lookTarget = new GameObject("CameraLookTarget").transform;
            lookTarget.SetParent(cameraPivot, false);
            var weaponPresentationRoot = new GameObject("WeaponPresentationRoot").transform;
            weaponPresentationRoot.SetParent(cameraPivot, false);
            var taggedMainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            taggedMainCamera.tag = "MainCamera";
            taggedMainCamera.transform.SetParent(cameraPivot, false);

            var cameraDefaults = playerRoot.AddComponent<PlayerCameraDefaults>();
            SetField(cameraDefaults, "_cameraPivot", cameraPivot);
            SetField(cameraDefaults, "_playerArmsRoot", playerArmsRoot);
            SetField(cameraDefaults, "_playerArmsAnimator", playerArmsAnimator);
            SetField(cameraDefaults, "_cameraLookTarget", lookTarget);
            SetField(cameraDefaults, "_weaponPresentationRoot", weaponPresentationRoot);

            try
            {
                var tryResolveMainTownDependencies = typeof(MainTownCombatWiring).GetMethod(
                    "TryResolveMainTownDependencies",
                    BindingFlags.Static | BindingFlags.NonPublic);

                Assert.That(tryResolveMainTownDependencies, Is.Not.Null, "Expected MainTownCombatWiring.TryResolveMainTownDependencies to exist.");

                var args = new object[] { cameraDefaults, null, null, null, null, null, null };
                var resolved = (bool)tryResolveMainTownDependencies!.Invoke(null, args)!;

                Assert.That(resolved, Is.False,
                    "MainTownCombatWiring must fail closed when the explicit main camera contract is missing.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerRoot);
            }
        }

        public static void VerifySlice()
        {
            var suite = new MainTownCombatWiringEditModeTests();
            suite.MainTownCombatWiring_ResolveWeaponViewParent_UsesExplicitWeaponPresentationRoot_AndDoesNotCreateFallback();
            suite.MainTownCombatWiring_ResolveWeaponViewParent_ReturnsNull_WhenExplicitWeaponPresentationRootMissing_EvenIfHierarchyContainsOne();
            suite.MainTownCombatWiring_TryResolveMainTownDependencies_FailsClosed_WhenExplicitMainCameraMissing_EvenIfCameraMainExists();
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
            Assert.That(serialized.FindProperty("_handTargetRoot")?.objectReferenceValue, Is.Not.Null, $"{context} should serialize the explicit hand target root.");

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
            Assert.That(playerRoot.transform.Find("CameraPivot/WeaponHandRigTargets"), Is.Not.Null, $"{context} should keep WeaponHandRigTargets under CameraPivot.");
            Assert.That(playerRoot.transform.Find("CameraPivot/PlayerArms"), Is.SameAs(playerArmsRoot), $"{context} should keep PlayerArms as a sibling branch of WeaponPresentationRoot.");
            Assert.That(playerRoot.transform.Find("CameraPivot/WeaponPresentationRoot"), Is.SameAs(weaponPresentationRoot), $"{context} should keep WeaponPresentationRoot as the live mount root.");
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field!.SetValue(instance, value);
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
