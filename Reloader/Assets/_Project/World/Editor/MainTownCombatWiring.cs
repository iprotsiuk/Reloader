#if UNITY_EDITOR
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Player.Viewmodel;
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
    public static class MainTownCombatWiring
    {
        private const string TargetScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string StarterRiflePath = "Assets/_Project/Weapons/Data/Weapons/StarterRifle.asset";
        private const string ProjectilePrefabPath = "Assets/_Project/Weapons/Prefabs/WeaponProjectile.prefab";

        [MenuItem("Reloader/World/Wire MainTown Combat Setup")]
        public static void WireMainTownCombatSetup()
        {
            var starterRifle = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(StarterRiflePath);
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<WeaponProjectile>(ProjectilePrefabPath);
            if (starterRifle == null || projectilePrefab == null)
            {
                Debug.LogError("MainTown combat wiring failed: missing StarterRifle asset or WeaponProjectile prefab.");
                return;
            }

            var scene = ResolveTargetScene();
            if (!scene.IsValid() || scene.path != TargetScenePath)
            {
                Debug.LogError($"MainTown combat wiring failed: could not open target scene '{TargetScenePath}'.");
                return;
            }

            if (!WireScene(starterRifle, projectilePrefab))
            {
                Debug.LogError("MainTown combat wiring failed: required PlayerRoot/CameraPivot setup not found.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"MainTown combat wiring complete: {TargetScenePath}");
        }

        private static Scene ResolveTargetScene()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.path == TargetScenePath)
            {
                return activeScene;
            }

            return EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        }

        private static bool WireScene(WeaponDefinition starterRifle, WeaponProjectile projectilePrefab)
        {
            var playerRoot = GameObject.Find("PlayerRoot");
            if (playerRoot == null)
            {
                return false;
            }

            var playerTransform = playerRoot.transform;
            var cameraPivot = EnsureChild(playerTransform, "CameraPivot", Vector3.zero);
            if (cameraPivot == null)
            {
                return false;
            }

            var lookTarget = EnsureChild(cameraPivot, "CameraLookTarget", new Vector3(0f, 0f, 10f));
            var muzzle = EnsureChild(cameraPivot, "WeaponMuzzle", new Vector3(0f, -0.08f, 0.45f));

            var registry = Object.FindFirstObjectByType<WeaponRegistry>();
            if (registry == null)
            {
                var registryGo = new GameObject("WeaponRegistry");
                registry = registryGo.AddComponent<WeaponRegistry>();
            }

            var registrySo = new SerializedObject(registry);
            var definitionsProp = registrySo.FindProperty("_definitions");
            definitionsProp.arraySize = 1;
            definitionsProp.GetArrayElementAtIndex(0).objectReferenceValue = starterRifle;
            registrySo.ApplyModifiedPropertiesWithoutUndo();

            var inputReader = playerRoot.GetComponent<PlayerInputReader>();
            var inventoryController = playerRoot.GetComponent<PlayerInventoryController>();
            var characterController = playerRoot.GetComponent<CharacterController>();
            var lookController = playerRoot.GetComponent<PlayerLookController>();

            var weaponController = GetOrAddComponent<PlayerWeaponController>(playerRoot);
            var cameraDefaults = GetOrAddComponent<PlayerCameraDefaults>(playerRoot);
            var animatorDriver = GetOrAddComponent<FpsViewmodelAnimatorDriver>(playerRoot);
            var viewmodelAdapter = GetOrAddComponent<ViewmodelAnimationAdapter>(playerRoot);

            var mainCamera = cameraPivot.Find("Camera")?.GetComponent<Camera>();
            var armsAnimator = cameraPivot.GetComponentInChildren<Animator>(true);

            var weaponSo = new SerializedObject(weaponController);
            weaponSo.FindProperty("_inputSourceBehaviour").objectReferenceValue = inputReader;
            weaponSo.FindProperty("_inventoryController").objectReferenceValue = inventoryController;
            weaponSo.FindProperty("_weaponRegistry").objectReferenceValue = registry;
            weaponSo.FindProperty("_muzzleTransform").objectReferenceValue = muzzle;
            weaponSo.FindProperty("_cameraDefaults").objectReferenceValue = cameraDefaults;
            weaponSo.FindProperty("_projectilePrefab").objectReferenceValue = projectilePrefab;
            weaponSo.FindProperty("_reloadDurationSeconds").floatValue = 0.35f;
            weaponSo.FindProperty("_scopeZoomSmoothTime").floatValue = 0.08f;
            weaponSo.FindProperty("_scopeZoomStep").floatValue = 0.5f;
            weaponSo.ApplyModifiedPropertiesWithoutUndo();

            if (lookController != null)
            {
                var lookSo = new SerializedObject(lookController);
                lookSo.FindProperty("_cameraDefaults").objectReferenceValue = cameraDefaults;
                lookSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(lookController);
            }

            var cameraDefaultsSo = new SerializedObject(cameraDefaults);
            cameraDefaultsSo.FindProperty("_mainCamera").objectReferenceValue = mainCamera;
            cameraDefaultsSo.FindProperty("_cameraFollowTarget").objectReferenceValue = cameraPivot;
            cameraDefaultsSo.FindProperty("_cameraLookTarget").objectReferenceValue = lookTarget;
            cameraDefaultsSo.ApplyModifiedPropertiesWithoutUndo();

            animatorDriver.Configure(armsAnimator, characterController);
            viewmodelAdapter.Configure(armsAnimator);

            EditorUtility.SetDirty(registry);
            EditorUtility.SetDirty(weaponController);
            EditorUtility.SetDirty(cameraDefaults);
            EditorUtility.SetDirty(animatorDriver);
            EditorUtility.SetDirty(viewmodelAdapter);
            EditorUtility.SetDirty(playerRoot);
            EditorUtility.SetDirty(cameraPivot.gameObject);
            return true;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(target);
        }

        private static Transform EnsureChild(Transform parent, string childName, Vector3 localPosition)
        {
            var child = parent.Find(childName);
            if (child == null)
            {
                var go = new GameObject(childName);
                go.transform.SetParent(parent, false);
                child = go.transform;
            }

            child.localPosition = localPosition;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            return child;
        }
    }
}
#endif
