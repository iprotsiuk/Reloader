#if UNITY_EDITOR
using System.Collections.Generic;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Player.Viewmodel;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using Reloader.Weapons.World;
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

        [MenuItem("Reloader/Weapons/Wire Weapons In MainWorld Scenes")]
        public static void WireWeaponsInMainWorldScenes()
        {
            var starterRifle = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(StarterRiflePath);
            var starterPistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(StarterPistolPath);
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<WeaponProjectile>(ProjectilePrefabPath);
            if (starterRifle == null || projectilePrefab == null)
            {
                Debug.LogError("Missing StarterRifle asset or WeaponProjectile prefab. Build starter content first.");
                return;
            }
            if (starterPistol == null)
            {
                Debug.LogWarning("StarterPistol asset not found. WeaponRegistry will be wired with rifle only.");
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
            resolverSo.ApplyModifiedPropertiesWithoutUndo();

            var registry = Object.FindFirstObjectByType<WeaponRegistry>();
            if (registry == null)
            {
                var registryGo = new GameObject("WeaponRegistry");
                registry = registryGo.AddComponent<WeaponRegistry>();
            }

            var registrySo = new SerializedObject(registry);
            var definitionsProp = registrySo.FindProperty("_definitions");
            definitionsProp.arraySize = starterPistol != null ? 2 : 1;
            definitionsProp.GetArrayElementAtIndex(0).objectReferenceValue = starterRifle;
            if (starterPistol != null)
            {
                definitionsProp.GetArrayElementAtIndex(1).objectReferenceValue = starterPistol;
            }
            registrySo.ApplyModifiedPropertiesWithoutUndo();

            var weaponController = playerRoot.GetComponent<PlayerWeaponController>();
            if (weaponController == null)
            {
                weaponController = Undo.AddComponent<PlayerWeaponController>(playerRoot);
            }

            var muzzle = EnsureMuzzle(playerRoot.transform);

            var weaponSo = new SerializedObject(weaponController);
            weaponSo.FindProperty("_inputSourceBehaviour").objectReferenceValue = inputReader;
            weaponSo.FindProperty("_inventoryController").objectReferenceValue = inventoryController;
            weaponSo.FindProperty("_weaponRegistry").objectReferenceValue = registry;
            weaponSo.FindProperty("_projectilePrefab").objectReferenceValue = projectilePrefab;
            weaponSo.FindProperty("_muzzleTransform").objectReferenceValue = muzzle;
            weaponSo.ApplyModifiedPropertiesWithoutUndo();

            var animator = playerRoot.GetComponentInChildren<Animator>(true);
            var viewmodelAdapter = playerRoot.GetComponent<ViewmodelAnimationAdapter>();
            if (viewmodelAdapter == null)
            {
                viewmodelAdapter = Undo.AddComponent<ViewmodelAnimationAdapter>(playerRoot);
            }
            viewmodelAdapter.Configure(animator);

            EditorUtility.SetDirty(inventoryController);
            EditorUtility.SetDirty(registry);
            EditorUtility.SetDirty(weaponController);
            EditorUtility.SetDirty(viewmodelAdapter);
            EditorUtility.SetDirty(playerRoot);
            return true;
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
                pivot.transform.localPosition = new Vector3(0f, 1.65f, 0f);
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
