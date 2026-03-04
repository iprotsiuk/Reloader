#if UNITY_EDITOR
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Editor
{
    public static class WorldSceneTemplateScaffolds
    {
        private const string StarterRiflePath = "Assets/_Project/Weapons/Data/Weapons/StarterRifle.asset";
        private const string ProjectilePrefabPath = "Assets/_Project/Weapons/Prefabs/WeaponProjectile.prefab";

        [MenuItem("Reloader/World/Templates/Apply TownHub Scaffold To Active Scene")]
        public static void ApplyTownHubScaffoldToActiveScene()
        {
            ApplyBaselineScaffold("TownHub");
        }

        [MenuItem("Reloader/World/Templates/Apply ActivityInstance Scaffold To Active Scene")]
        public static void ApplyActivityInstanceScaffoldToActiveScene()
        {
            ApplyBaselineScaffold("ActivityInstance");
        }

        private static void ApplyBaselineScaffold(string scaffoldName)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError($"Cannot apply {scaffoldName} scaffold: no active scene.");
                return;
            }

            var changed = false;
            var createdCount = 0;

            var playerRoot = EnsureRootObject(scene, "PlayerRoot", ref changed, ref createdCount);
            var cameraPivot = EnsureChild(playerRoot.transform, "CameraPivot", new Vector3(0f, 1.8f, 0f), ref changed, ref createdCount);
            if (cameraPivot.localPosition != new Vector3(0f, 1.8f, 0f))
            {
                cameraPivot.localPosition = new Vector3(0f, 1.8f, 0f);
                changed = true;
            }
            var lookTarget = EnsureChild(cameraPivot, "CameraLookTarget", new Vector3(0f, 0f, 10f), ref changed, ref createdCount);
            var muzzle = EnsureChild(cameraPivot, "WeaponMuzzle", new Vector3(0f, -0.08f, 0.45f), ref changed, ref createdCount);
            var weaponRegistry = EnsureWeaponRegistry(scene, ref changed, ref createdCount);
            EnsureRegistryDefinitions(weaponRegistry, ref changed);
            EnsurePlayerCombatWiring(playerRoot, cameraPivot, lookTarget, muzzle, weaponRegistry, ref changed, ref createdCount);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log($"{scaffoldName} scaffold applied to scene '{scene.path}'. Created objects/components: {createdCount}.");
        }

        private static GameObject EnsureRootObject(Scene scene, string name, ref bool changed, ref int createdCount)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name)
                {
                    return roots[i];
                }
            }

            var gameObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            changed = true;
            createdCount++;
            return gameObject;
        }

        private static Transform EnsureChild(
            Transform parent,
            string childName,
            Vector3 localPosition,
            ref bool changed,
            ref int createdCount)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            var gameObject = new GameObject(childName);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;
            changed = true;
            createdCount++;
            return gameObject.transform;
        }

        private static WeaponRegistry EnsureWeaponRegistry(Scene scene, ref bool changed, ref int createdCount)
        {
            var registry = Object.FindFirstObjectByType<WeaponRegistry>();
            if (registry == null || registry.gameObject.scene != scene)
            {
                var registryObject = EnsureRootObject(scene, "WeaponRegistry", ref changed, ref createdCount);
                registry = registryObject.GetComponent<WeaponRegistry>();
                if (registry == null)
                {
                    registry = Undo.AddComponent<WeaponRegistry>(registryObject);
                    changed = true;
                    createdCount++;
                }

                return registry;
            }

            return registry;
        }

        private static void EnsureRegistryDefinitions(WeaponRegistry weaponRegistry, ref bool changed)
        {
            if (weaponRegistry == null)
            {
                return;
            }

            var starterRifle = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(StarterRiflePath);
            if (starterRifle == null)
            {
                return;
            }

            var so = new SerializedObject(weaponRegistry);
            var definitions = so.FindProperty("_definitions");
            if (definitions == null)
            {
                return;
            }

            if (definitions.arraySize == 1 && definitions.GetArrayElementAtIndex(0).objectReferenceValue == starterRifle)
            {
                return;
            }

            definitions.arraySize = 1;
            definitions.GetArrayElementAtIndex(0).objectReferenceValue = starterRifle;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(weaponRegistry);
            changed = true;
        }

        private static void EnsurePlayerCombatWiring(
            GameObject playerRoot,
            Transform cameraPivot,
            Transform lookTarget,
            Transform muzzle,
            WeaponRegistry weaponRegistry,
            ref bool changed,
            ref int createdCount)
        {
            var inputReader = GetOrAddComponent<PlayerInputReader>(playerRoot, ref changed, ref createdCount);
            var inventoryController = GetOrAddComponent<PlayerInventoryController>(playerRoot, ref changed, ref createdCount);
            var cameraDefaults = GetOrAddComponent<PlayerCameraDefaults>(playerRoot, ref changed, ref createdCount);
            var weaponController = GetOrAddComponent<PlayerWeaponController>(playerRoot, ref changed, ref createdCount);

            var camera = cameraPivot != null ? cameraPivot.GetComponentInChildren<Camera>(true) : null;
            if (camera != null)
            {
                var cameraTransform = camera.transform;
                cameraTransform.SetParent(cameraPivot, false);
                cameraTransform.localPosition = Vector3.zero;
                cameraTransform.localRotation = Quaternion.identity;
            }

            var cameraDefaultsSo = new SerializedObject(cameraDefaults);
            cameraDefaultsSo.FindProperty("_mainCamera").objectReferenceValue = camera;
            cameraDefaultsSo.FindProperty("_cameraFollowTarget").objectReferenceValue = cameraPivot;
            cameraDefaultsSo.FindProperty("_cameraLookTarget").objectReferenceValue = lookTarget;
            cameraDefaultsSo.ApplyModifiedPropertiesWithoutUndo();

            var lookController = playerRoot.GetComponent<PlayerLookController>();
            if (lookController != null)
            {
                var lookSo = new SerializedObject(lookController);
                lookSo.FindProperty("_cameraDefaults").objectReferenceValue = cameraDefaults;
                lookSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var projectile = AssetDatabase.LoadAssetAtPath<WeaponProjectile>(ProjectilePrefabPath);
            var weaponSo = new SerializedObject(weaponController);
            weaponSo.FindProperty("_inputSourceBehaviour").objectReferenceValue = inputReader;
            weaponSo.FindProperty("_inventoryController").objectReferenceValue = inventoryController;
            weaponSo.FindProperty("_weaponRegistry").objectReferenceValue = weaponRegistry;
            weaponSo.FindProperty("_muzzleTransform").objectReferenceValue = muzzle;
            weaponSo.FindProperty("_cameraDefaults").objectReferenceValue = cameraDefaults;
            weaponSo.FindProperty("_projectilePrefab").objectReferenceValue = projectile;
            weaponSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(cameraDefaults);
            EditorUtility.SetDirty(weaponController);
            EditorUtility.SetDirty(playerRoot);
        }

        private static T GetOrAddComponent<T>(GameObject gameObject, ref bool changed, ref int createdCount) where T : Component
        {
            var existing = gameObject.GetComponent<T>();
            if (existing != null)
            {
                return existing;
            }

            changed = true;
            createdCount++;
            return Undo.AddComponent<T>(gameObject);
        }
    }
}
#endif
