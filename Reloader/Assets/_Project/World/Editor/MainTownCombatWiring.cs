#if UNITY_EDITOR
using System.Collections.Generic;
using Reloader.Inventory;using Reloader.Contracts.Runtime;

using Reloader.Player;
using Reloader.Player.Viewmodel;
using Reloader.Core.Items;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using Reloader.Weapons.World;
using Reloader.Weapons.Animations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Editor
{
    public static class MainTownCombatWiring
    {
        private static readonly Vector3 ArmsLocalPosition = new Vector3(0f, -0.027f, 0.1f);
        private static readonly Vector3 ArmsLocalRotation = Vector3.zero;
        private static readonly Vector3 ArmsLocalScale = new Vector3(0.42f, 0.42f, 0.42f);
        private static readonly Vector3 CameraPivotLocalPosition = new Vector3(0f, 1.8f, 0f);
        private const string TargetScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string StarterRiflePath = "Assets/_Project/Weapons/Data/Weapons/StarterRifle.asset";
        private const string StarterPistolPath = "Assets/_Project/Weapons/Data/Weapons/StarterPistol.asset";
        private const string RifleItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Rifle_308_Starter.asset";
        private const string PistolItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Pistol_9x19_Starter.asset";
        private const string Ammo308ItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Cartridge_308_147_FMJ_PMC_Bronze.asset";
        private const string Ammo9x19ItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Ammo_Factory_9x19_124_FMJ.asset";
        private const string Kar98kScopeItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Kar98k_Scope_Remote_A.asset";
        private const string Kar98kMuzzleItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Kar98k_Muzzle_Device_C.asset";
        private const string ProjectilePrefabPath = "Assets/_Project/Weapons/Prefabs/WeaponProjectile.prefab";
        private const string RifleViewPrefabPath = "Assets/_Project/Weapons/Prefabs/RifleView.prefab";
        private const string PistolViewPrefabPath = "Assets/_Project/Weapons/Prefabs/PistolView.prefab";
        private const string PackCharacterControllerPath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Animators/Character/AC_LPSP_PCH.controller";
        private const string PackRifleOverridePath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Animators/Character/OC_LPSP_PCH_AR_01.overrideController";
        private const string PackPistolOverridePath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Animators/Character/OC_LPSP_PCH_Handgun_03.overrideController";
        private const string WeaponAnimationProfilePath = "Assets/_Project/Weapons/Data/AnimationProfiles/PlayerWeaponAnimatorOverrideProfile.asset";

        [MenuItem("Reloader/World/Wire MainTown Combat Setup")]
        public static void WireMainTownCombatSetup()
        {
            var starterRifle = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(StarterRiflePath);
            var starterPistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(StarterPistolPath);
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<WeaponProjectile>(ProjectilePrefabPath);
            if (starterRifle == null || starterPistol == null || projectilePrefab == null)
            {
                Debug.LogError("MainTown combat wiring failed: missing StarterRifle asset, StarterPistol asset, or WeaponProjectile prefab.");
                return;
            }

            var scene = ResolveTargetScene();
            if (!scene.IsValid() || scene.path != TargetScenePath)
            {
                Debug.LogError($"MainTown combat wiring failed: could not open target scene '{TargetScenePath}'.");
                return;
            }

            if (!WireScene(starterRifle, starterPistol, projectilePrefab))
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

        private static bool WireScene(WeaponDefinition starterRifle, WeaponDefinition starterPistol, WeaponProjectile projectilePrefab)
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

            cameraPivot.localPosition = CameraPivotLocalPosition;

            var playerArms = EnsureChild(cameraPivot, "PlayerArms", ArmsLocalPosition);
            if (playerArms != null)
            {
                playerArms.localEulerAngles = ArmsLocalRotation;
                playerArms.localScale = ArmsLocalScale;
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
            definitionsProp.arraySize = 2;
            definitionsProp.GetArrayElementAtIndex(0).objectReferenceValue = starterRifle;
            definitionsProp.GetArrayElementAtIndex(1).objectReferenceValue = starterPistol;
            registrySo.ApplyModifiedPropertiesWithoutUndo();

            var inputReader = playerRoot.GetComponent<PlayerInputReader>();
            var inventoryController = playerRoot.GetComponent<PlayerInventoryController>();
            var characterController = playerRoot.GetComponent<CharacterController>();
            var lookController = playerRoot.GetComponent<PlayerLookController>();

            var weaponController = GetOrAddComponent<PlayerWeaponController>(playerRoot);
            var poseTuningHelper = GetOrAddComponent<WeaponViewPoseTuningHelper>(playerRoot);
            var animationBinder = GetOrAddComponent<PlayerWeaponAnimationBinder>(playerRoot);
            var cameraDefaults = GetOrAddComponent<PlayerCameraDefaults>(playerRoot);
            var animatorDriver = GetOrAddComponent<FpsViewmodelAnimatorDriver>(playerRoot);
            var viewmodelAdapter = GetOrAddComponent<ViewmodelAnimationAdapter>(playerRoot);

            if (inventoryController != null)
            {
                var inventorySo = new SerializedObject(inventoryController);
                var itemDefinitions = inventorySo.FindProperty("_itemDefinitionRegistry");
                if (itemDefinitions != null)
                {
                    var rifleItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>(RifleItemDefinitionPath);
                    var pistolItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>(PistolItemDefinitionPath);
                    var ammo308Item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(Ammo308ItemDefinitionPath);
                    var ammo9x19Item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(Ammo9x19ItemDefinitionPath);
                    var kar98kScopeItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>(Kar98kScopeItemDefinitionPath);
                    var kar98kMuzzleItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>(Kar98kMuzzleItemDefinitionPath);
                    var values = new List<ItemDefinition>();
                    if (rifleItem != null) values.Add(rifleItem);
                    if (pistolItem != null) values.Add(pistolItem);
                    if (ammo308Item != null) values.Add(ammo308Item);
                    if (ammo9x19Item != null) values.Add(ammo9x19Item);
                    if (kar98kScopeItem != null) values.Add(kar98kScopeItem);
                    if (kar98kMuzzleItem != null) values.Add(kar98kMuzzleItem);
                    itemDefinitions.arraySize = values.Count;
                    for (var i = 0; i < values.Count; i++)
                    {
                        itemDefinitions.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                    }

                    inventorySo.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else
            {
                Debug.LogWarning("MainTown combat wiring: PlayerInventoryController missing on PlayerRoot; skipped inventory registry wiring.");
            }

            var mainCamera =
                cameraPivot.Find("Camera")?.GetComponent<Camera>() ??
                cameraPivot.Find("Main Camera")?.GetComponent<Camera>() ??
                Camera.main;
            if (mainCamera != null)
            {
                var cameraTransform = mainCamera.transform;
                cameraTransform.SetParent(cameraPivot, false);
                cameraTransform.localPosition = Vector3.zero;
                cameraTransform.localRotation = Quaternion.identity;
            }
            var armsAnimator = cameraPivot.GetComponentInChildren<Animator>(true);
            var rifleViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RifleViewPrefabPath);
            var pistolViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PistolViewPrefabPath);
            var packController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PackCharacterControllerPath);
            var rifleAnimationOverride = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PackRifleOverridePath);
            var pistolAnimationOverride = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PackPistolOverridePath);
            var weaponAnimationProfile = LoadOrCreateAnimationProfile(packController, rifleAnimationOverride, pistolAnimationOverride);

            var weaponSo = new SerializedObject(weaponController);
            weaponSo.FindProperty("_inputSourceBehaviour").objectReferenceValue = inputReader;
            weaponSo.FindProperty("_inventoryController").objectReferenceValue = inventoryController;
            weaponSo.FindProperty("_weaponRegistry").objectReferenceValue = registry;
            weaponSo.FindProperty("_muzzleTransform").objectReferenceValue = muzzle;
            weaponSo.FindProperty("_cameraDefaults").objectReferenceValue = cameraDefaults;
            weaponSo.FindProperty("_projectilePrefab").objectReferenceValue = projectilePrefab;
            weaponSo.FindProperty("_packAnimator").objectReferenceValue = armsAnimator;
            var weaponViewParent = weaponSo.FindProperty("_weaponViewParent");
            if (weaponViewParent != null)
            {
                weaponViewParent.objectReferenceValue = ResolveWeaponViewParent(armsAnimator);
            }

            var weaponViewPrefabs = weaponSo.FindProperty("_weaponViewPrefabs");
            if (weaponViewPrefabs != null)
            {
                weaponViewPrefabs.arraySize = 0;
                if (rifleViewPrefab != null)
                {
                    var index = weaponViewPrefabs.arraySize;
                    weaponViewPrefabs.InsertArrayElementAtIndex(index);
                    var element = weaponViewPrefabs.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("_itemId").stringValue = "weapon-kar98k";
                    element.FindPropertyRelative("_viewPrefab").objectReferenceValue = rifleViewPrefab;
                }

                if (pistolViewPrefab != null)
                {
                    var index = weaponViewPrefabs.arraySize;
                    weaponViewPrefabs.InsertArrayElementAtIndex(index);
                    var element = weaponViewPrefabs.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("_itemId").stringValue = "weapon-canik-tp9";
                    element.FindPropertyRelative("_viewPrefab").objectReferenceValue = pistolViewPrefab;
                }
            }

            var packConfig = weaponSo.FindProperty("_packPresentationConfig");
            if (packConfig != null)
            {
                SetStringPropertyIfPresent(packConfig, "_aimBoolParameter", "Aim");
                SetStringPropertyIfPresent(packConfig, "_reloadBoolParameter", "Reloading");
                SetStringPropertyIfPresent(packConfig, "_reloadStateName", "Layer Actions.Reload");
                SetStringPropertyIfPresent(packConfig, "_fireStateName", "Layer Actions.Fire");
            }
            weaponSo.ApplyModifiedPropertiesWithoutUndo();

            if (poseTuningHelper != null)
            {
                var tuningSo = new SerializedObject(poseTuningHelper);
                SetObjectReferencePropertyIfPresent(tuningSo, "_weaponController", weaponController);
                SetStringPropertyIfPresent(tuningSo, "_targetWeaponItemId", "weapon-kar98k");
                SetBoolPropertyIfPresent(tuningSo, "_enabledInPlayMode", true);
                SetBoolPropertyIfPresent(tuningSo, "_seedOffsetsFromCurrentPoseOnEquip", false);
                if (ShouldSeedDefaultWeaponPoseTuning(tuningSo))
                {
                    SetVector3PropertyIfPresent(tuningSo, "_hipLocalPosition", new Vector3(0.015f, 0.15f, 0.005f));
                    SetVector3PropertyIfPresent(tuningSo, "_hipLocalEuler", Vector3.zero);
                    SetVector3PropertyIfPresent(tuningSo, "_adsLocalPosition", new Vector3(0f, 0.2f, 0.05f));
                    SetVector3PropertyIfPresent(tuningSo, "_adsLocalEuler", Vector3.zero);
                    SetFloatPropertyIfPresent(tuningSo, "_blendSpeed", 24f);
                    SetVector3PropertyIfPresent(tuningSo, "_rifleLocalEulerOffset", new Vector3(90f, 0f, 0f));
                }

                tuningSo.ApplyModifiedPropertiesWithoutUndo();
            }

            if (packController != null && armsAnimator != null)
            {
                armsAnimator.runtimeAnimatorController = packController;
            }

            animationBinder.Configure(armsAnimator, weaponAnimationProfile);

            WireContractTargetsToRuntimeProvider();
            CleanupStarterWorldObjects();

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
            EditorUtility.SetDirty(animationBinder);
            EditorUtility.SetDirty(playerRoot);
            EditorUtility.SetDirty(cameraPivot.gameObject);
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

        private static void SetStringPropertyIfPresent(SerializedProperty parent, string propertyName, string value)
        {
            if (parent == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            var property = parent.FindPropertyRelative(propertyName);
            if (property != null)
            {
                property.stringValue = value ?? string.Empty;
            }
        }

        private static void SetStringPropertyIfPresent(SerializedObject so, string propertyName, string value)
        {
            if (so == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            var property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value ?? string.Empty;
            }
        }

        private static void SetBoolPropertyIfPresent(SerializedObject so, string propertyName, bool value)
        {
            if (so == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            var property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetFloatPropertyIfPresent(SerializedObject so, string propertyName, float value)
        {
            if (so == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            var property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetVector3PropertyIfPresent(SerializedObject so, string propertyName, Vector3 value)
        {
            if (so == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            var property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.vector3Value = value;
            }
        }

        private static bool ShouldSeedDefaultWeaponPoseTuning(SerializedObject so)
        {
            if (so == null)
            {
                return false;
            }

            var hipLocalPosition = so.FindProperty("_hipLocalPosition");
            var hipLocalEuler = so.FindProperty("_hipLocalEuler");
            var adsLocalPosition = so.FindProperty("_adsLocalPosition");
            var adsLocalEuler = so.FindProperty("_adsLocalEuler");
            var rifleLocalEulerOffset = so.FindProperty("_rifleLocalEulerOffset");
            var blendSpeed = so.FindProperty("_blendSpeed");

            if (hipLocalPosition == null
                || hipLocalEuler == null
                || adsLocalPosition == null
                || adsLocalEuler == null
                || rifleLocalEulerOffset == null
                || blendSpeed == null)
            {
                return false;
            }

            return hipLocalPosition.vector3Value.sqrMagnitude <= 0.000001f
                && hipLocalEuler.vector3Value.sqrMagnitude <= 0.000001f
                && adsLocalPosition.vector3Value.sqrMagnitude <= 0.000001f
                && adsLocalEuler.vector3Value.sqrMagnitude <= 0.000001f
                && rifleLocalEulerOffset.vector3Value.sqrMagnitude <= 0.000001f
                && IsApproximatelyUnseededBlendSpeed(blendSpeed.floatValue);
        }

        private static bool IsApproximatelyUnseededBlendSpeed(float value)
        {
            return Mathf.Abs(value) <= 0.000001f
                || Mathf.Abs(value - 24f) <= 0.000001f;
        }

        private static void SetObjectReferencePropertyIfPresent(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            if (so == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            var property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
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

        private static void CleanupStarterWorldObjects()
        {
            CleanupOrphanPickupVisuals();
            CleanupDeprecatedStarterPickups();
        }

        private static void CleanupDeprecatedStarterPickups()
        {
            CleanupPickupObjectByName("WeaponSpawn_RifleStarter_Exported");
            CleanupPickupObjectByName("WeaponSpawn_RifleStarter");
            CleanupPickupObjectByName("WeaponSpawn_RifleStarter_LPSP");
            CleanupPickupObjectByName("WeaponSpawn_PistolStarter_LPSP");
            CleanupPickupObjectByName("AmmoSpawn_308_LPSP");
            CleanupPickupObjectByName("AmmoSpawn_9x19_LPSP");
            CleanupPickupObjectByName("AmmoSpawn_Cartridge308");
            CleanupPickupObjectByName("AmmoSpawn_Cartridge308_Exported");
            CleanupPickupObjectByName("AmmoSpawn_Bullet308");
            CleanupPickupObjectByName("AmmoSpawn_Bullet308_Exported");
            CleanupPickupObjectByName("AmmoBox_100R_308");
            CleanupPickupObjectByName("AmmoBox_100R_308_Exported");
            CleanupPickupObjectByName("AttachmentSpawn_Kar98kScope");
            CleanupPickupObjectByName("AttachmentSpawn_Kar98kMuzzle");
        }

        private static void CleanupOrphanPickupVisuals()
        {
            CleanupOrphanPickupVisualByName("WWII_Recon_A_PreSet");
            CleanupOrphanPickupVisualByName("WWII_Recon_A");
            CleanupOrphanPickupVisualByName("WWII_Optic_Remote_Range_A");
            CleanupOrphanPickupVisualByName("WWII_Muzzle_Device_C");
        }

        private static void CleanupOrphanPickupVisualByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return;
            }

            var objects = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < objects.Length; i++)
            {
                var transform = objects[i];
                if (transform == null || transform.name != objectName)
                {
                    continue;
                }

                if (transform.GetComponentInParent<DefinitionPickupTarget>() != null)
                {
                    continue;
                }

                Object.DestroyImmediate(transform.gameObject);
            }
        }

        private static void CleanupPickupObjectByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return;
            }

            var pickup = GameObject.Find(objectName);
            if (pickup != null)
            {
                Object.DestroyImmediate(pickup);
            }
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


private static void WireContractTargetsToRuntimeProvider()
        {
            var provider = Object.FindFirstObjectByType<StaticContractRuntimeProvider>();
            var targets = Object.FindObjectsByType<ContractTargetDamageable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                if (target == null)
                {
                    continue;
                }

                var serializedObject = new SerializedObject(target);
                var sinkProperty = serializedObject.FindProperty("_eliminationSinkBehaviour");
                if (sinkProperty == null)
                {
                    continue;
                }

                sinkProperty.objectReferenceValue = provider;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
        }
}
}
#endif
