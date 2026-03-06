#if UNITY_EDITOR
using System.Collections.Generic;
using Reloader.Core.Items;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Player.Viewmodel;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using Reloader.Weapons.World;
using Reloader.Weapons.Animations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reloader.Weapons.Editor
{
    public static class WeaponsSceneWiring
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainWorld.unity",
            "Assets/Scenes/MainWorld_Scaffold.unity",
            "Assets/Scenes/MainWorld_Level02Only.unity"
        };

        private const string StarterRiflePath = "Assets/_Project/Weapons/Data/Weapons/StarterRifle.asset";
        private const string StarterPistolPath = "Assets/_Project/Weapons/Data/Weapons/StarterPistol.asset";
        private const string ProjectilePrefabPath = "Assets/_Project/Weapons/Prefabs/WeaponProjectile.prefab";
        private const string RifleViewPrefabPath = "Assets/_Project/Weapons/Prefabs/RifleView.prefab";
        private const string PistolViewPrefabPath = "Assets/_Project/Weapons/Prefabs/PistolView.prefab";
        private const string PackCharacterControllerPath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Animators/Character/AC_LPSP_PCH.controller";
        private const string PackRifleOverridePath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Animators/Character/OC_LPSP_PCH_AR_01.overrideController";
        private const string PackPistolOverridePath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Animators/Character/OC_LPSP_PCH_Handgun_03.overrideController";
        private const string WeaponAnimationProfilePath = "Assets/_Project/Weapons/Data/AnimationProfiles/PlayerWeaponAnimatorOverrideProfile.asset";
        private const string RifleItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Rifle_308_Starter.asset";
        private const string PistolItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Pistol_9x19_Starter.asset";
        private const string Ammo308ItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Cartridge_308_147_FMJ_PMC_Bronze.asset";
        private const string Ammo9x19ItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Ammo_Factory_9x19_124_FMJ.asset";

        [MenuItem("Reloader/Weapons/Wire Weapons In MainWorld Scenes")]
        public static void WireWeaponsInMainWorldScenes()
        {
            var starterRifle = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(StarterRiflePath);
            var starterPistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(StarterPistolPath);
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<WeaponProjectile>(ProjectilePrefabPath);
            if (starterRifle == null || starterPistol == null || projectilePrefab == null)
            {
                Debug.LogError("Missing StarterRifle asset, StarterPistol asset, or WeaponProjectile prefab. Build supported weapon content first.");
                return;
            }

            var changedCount = 0;
            foreach (var scenePath in ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (!scene.IsValid())
                {
                    Debug.LogWarning($"Scene not valid: {scenePath}");
                    continue;
                }

                if (WireScene(scenePath, starterRifle, starterPistol, projectilePrefab))
                {
                    EditorSceneManager.SaveScene(scene);
                    changedCount++;
                    Debug.Log($"Wired and saved: {scenePath}");
                }
                else
                {
                    Debug.LogWarning($"No player/inventory root found, skipped: {scenePath}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Weapons scene wiring complete. Updated scenes: {changedCount}");
        }

        private static bool WireScene(
            string scenePath,
            WeaponDefinition starterRifle,
            WeaponDefinition starterPistol,
            WeaponProjectile projectilePrefab)
        {
            var inventoryController = Object.FindFirstObjectByType<PlayerInventoryController>();
            var inputReader = Object.FindFirstObjectByType<PlayerInputReader>();
            if (inventoryController == null || inputReader == null)
            {
                return false;
            }

            var playerRoot = inventoryController.gameObject;

            var pickupResolver = playerRoot.GetComponent<PlayerWeaponPickupResolver>();
            if (pickupResolver == null)
            {
                pickupResolver = Undo.AddComponent<PlayerWeaponPickupResolver>(playerRoot);
            }

            var resolverSo = new SerializedObject(inventoryController);
            resolverSo.FindProperty("_pickupTargetResolverBehaviour").objectReferenceValue = pickupResolver;
            if (resolverSo.FindProperty("_inputSourceBehaviour") != null)
            {
                resolverSo.FindProperty("_inputSourceBehaviour").objectReferenceValue = inputReader;
            }

            var itemDefinitions = resolverSo.FindProperty("_itemDefinitionRegistry");
            if (itemDefinitions != null)
            {
                var rifleItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>(RifleItemDefinitionPath);
                var pistolItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>(PistolItemDefinitionPath);
                var ammo308Item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(Ammo308ItemDefinitionPath);
                var ammo9x19Item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(Ammo9x19ItemDefinitionPath);
                var values = new List<ItemDefinition>();
                if (rifleItem != null) values.Add(rifleItem);
                if (pistolItem != null) values.Add(pistolItem);
                if (ammo308Item != null) values.Add(ammo308Item);
                if (ammo9x19Item != null) values.Add(ammo9x19Item);
                itemDefinitions.arraySize = values.Count;
                for (var i = 0; i < values.Count; i++)
                {
                    itemDefinitions.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
            }
            resolverSo.ApplyModifiedPropertiesWithoutUndo();

            var registry = Object.FindFirstObjectByType<WeaponRegistry>();
            if (registry == null)
            {
                var registryGo = new GameObject("WeaponRegistry");
                registry = registryGo.AddComponent<WeaponRegistry>();
            }

            var registrySo = new SerializedObject(registry);
            var definitionsProp = registrySo.FindProperty("_definitions");
            definitionsProp.arraySize = 2;
            definitionsProp.GetArrayElementAtIndex(0).objectReferenceValue = starterRifle;
            definitionsProp.GetArrayElementAtIndex(1).objectReferenceValue = starterPistol;
            registrySo.ApplyModifiedPropertiesWithoutUndo();

            var weaponController = playerRoot.GetComponent<PlayerWeaponController>();
            if (weaponController == null)
            {
                weaponController = Undo.AddComponent<PlayerWeaponController>(playerRoot);
            }
            var animationBinder = playerRoot.GetComponent<PlayerWeaponAnimationBinder>();
            if (animationBinder == null)
            {
                animationBinder = Undo.AddComponent<PlayerWeaponAnimationBinder>(playerRoot);
            }

            var muzzle = EnsureMuzzle(playerRoot.transform);

            var weaponSo = new SerializedObject(weaponController);
            weaponSo.FindProperty("_inputSourceBehaviour").objectReferenceValue = inputReader;
            weaponSo.FindProperty("_inventoryController").objectReferenceValue = inventoryController;
            weaponSo.FindProperty("_weaponRegistry").objectReferenceValue = registry;
            weaponSo.FindProperty("_projectilePrefab").objectReferenceValue = projectilePrefab;
            weaponSo.FindProperty("_muzzleTransform").objectReferenceValue = muzzle;
            var armsAnimator = playerRoot.GetComponentInChildren<Animator>(true);
            if (weaponSo.FindProperty("_packAnimator") != null)
            {
                weaponSo.FindProperty("_packAnimator").objectReferenceValue = armsAnimator;
            }

            if (weaponSo.FindProperty("_weaponViewParent") != null)
            {
                weaponSo.FindProperty("_weaponViewParent").objectReferenceValue = ResolveWeaponViewParent(armsAnimator);
            }

            var rifleViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RifleViewPrefabPath);
            var pistolViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PistolViewPrefabPath);
            var packController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PackCharacterControllerPath);
            var rifleAnimationOverride = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PackRifleOverridePath);
            var pistolAnimationOverride = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PackPistolOverridePath);
            var weaponAnimationProfile = LoadOrCreateAnimationProfile(packController, rifleAnimationOverride, pistolAnimationOverride);
            var viewPrefabs = weaponSo.FindProperty("_weaponViewPrefabs");
            if (viewPrefabs != null)
            {
                viewPrefabs.arraySize = 0;
                if (rifleViewPrefab != null)
                {
                    var index = viewPrefabs.arraySize;
                    viewPrefabs.InsertArrayElementAtIndex(index);
                    var entry = viewPrefabs.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("_itemId").stringValue = "weapon-kar98k";
                    entry.FindPropertyRelative("_viewPrefab").objectReferenceValue = rifleViewPrefab;
                }

                if (pistolViewPrefab != null)
                {
                    var index = viewPrefabs.arraySize;
                    viewPrefabs.InsertArrayElementAtIndex(index);
                    var entry = viewPrefabs.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("_itemId").stringValue = "weapon-canik-tp9";
                    entry.FindPropertyRelative("_viewPrefab").objectReferenceValue = pistolViewPrefab;
                }
            }

            weaponSo.ApplyModifiedPropertiesWithoutUndo();

            if (armsAnimator != null && packController != null)
            {
                armsAnimator.runtimeAnimatorController = packController;
            }
            animationBinder.Configure(armsAnimator, weaponAnimationProfile);

            var viewmodelAdapter = playerRoot.GetComponent<ViewmodelAnimationAdapter>();
            if (viewmodelAdapter == null)
            {
                viewmodelAdapter = Undo.AddComponent<ViewmodelAnimationAdapter>(playerRoot);
            }
            viewmodelAdapter.Configure(armsAnimator);

            EditorUtility.SetDirty(inventoryController);
            EditorUtility.SetDirty(registry);
            EditorUtility.SetDirty(weaponController);
            EditorUtility.SetDirty(animationBinder);
            EditorUtility.SetDirty(viewmodelAdapter);
            EditorUtility.SetDirty(playerRoot);
            return true;
        }

        private static WeaponAnimatorOverrideProfile LoadOrCreateAnimationProfile(
            RuntimeAnimatorController defaultController,
            RuntimeAnimatorController rifleController,
            RuntimeAnimatorController pistolController)
        {
            var profile = AssetDatabase.LoadAssetAtPath<WeaponAnimatorOverrideProfile>(WeaponAnimationProfilePath);
            if (profile == null)
            {
                EnsureAssetFolder("Assets/_Project/Weapons/Data/AnimationProfiles");
                profile = ScriptableObject.CreateInstance<WeaponAnimatorOverrideProfile>();
                AssetDatabase.CreateAsset(profile, WeaponAnimationProfilePath);
            }

            var profileSo = new SerializedObject(profile);
            profileSo.FindProperty("_defaultController").objectReferenceValue = defaultController;
            var entries = profileSo.FindProperty("_entries");
            entries.arraySize = 0;
            if (rifleController != null)
            {
                var index = entries.arraySize;
                entries.InsertArrayElementAtIndex(index);
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("_itemId").stringValue = "weapon-kar98k";
                entry.FindPropertyRelative("_controller").objectReferenceValue = rifleController;
            }

            if (pistolController != null)
            {
                var index = entries.arraySize;
                entries.InsertArrayElementAtIndex(index);
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("_itemId").stringValue = "weapon-canik-tp9";
                entry.FindPropertyRelative("_controller").objectReferenceValue = pistolController;
            }

            profileSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var normalized = folderPath.Replace('\\', '/').Trim('/');
            var parts = normalized.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static Transform ResolveWeaponViewParent(Animator armsAnimator)
        {
            if (armsAnimator == null)
            {
                return null;
            }

            var root = armsAnimator.transform;
            return FindDescendantByName(root, "ik_hand_gun") ?? root;
        }

        private static Transform FindDescendantByName(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            if (root.name == targetName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDescendantByName(root.GetChild(i), targetName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform EnsureMuzzle(Transform playerRoot)
        {
            var existing = playerRoot.Find("CameraPivot/WeaponMuzzle");
            if (existing != null)
            {
                return existing;
            }

            var cameraPivot = playerRoot.Find("CameraPivot");
            if (cameraPivot == null)
            {
                var pivot = new GameObject("CameraPivot");
                pivot.transform.SetParent(playerRoot, false);
                pivot.transform.localPosition = new Vector3(0f, 1.8f, 0f);
                cameraPivot = pivot.transform;
            }

            var muzzleGo = new GameObject("WeaponMuzzle");
            muzzleGo.transform.SetParent(cameraPivot, false);
            muzzleGo.transform.localPosition = new Vector3(0f, -0.08f, 0.45f);
            return muzzleGo.transform;
        }
    }
}
#endif
