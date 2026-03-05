#if UNITY_EDITOR
using System.Collections.Generic;
using Reloader.Inventory;
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
        private const string StarterRifleSpawnPath = "Assets/_Project/Inventory/Data/Spawns/Rifle_308_Starter_Spawn.asset";
        private const string StarterPistolSpawnPath = "Assets/_Project/Inventory/Data/Spawns/Pistol_9x19_Starter_Spawn.asset";
        private const string Ammo308SpawnPath = "Assets/_Project/Inventory/Data/Spawns/Cartridge_308_147_FMJ_PMC_Bronze_Spawn.asset";
        private const string Ammo9x19SpawnPath = "Assets/_Project/Inventory/Data/Spawns/Ammo_Factory_9x19_124_FMJ_Spawn.asset";
        private const string Kar98kScopeSpawnPath = "Assets/_Project/Inventory/Data/Spawns/Kar98k_Scope_Remote_A_Spawn.asset";
        private const string Kar98kMuzzleSpawnPath = "Assets/_Project/Inventory/Data/Spawns/Kar98k_Muzzle_Device_C_Spawn.asset";
        private const string RifleItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Rifle_308_Starter.asset";
        private const string PistolItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Pistol_9x19_Starter.asset";
        private const string Ammo308ItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Cartridge_308_147_FMJ_PMC_Bronze.asset";
        private const string Ammo9x19ItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Ammo_Factory_9x19_124_FMJ.asset";
        private const string Kar98kScopeItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Kar98k_Scope_Remote_A.asset";
        private const string Kar98kMuzzleItemDefinitionPath = "Assets/_Project/Inventory/Data/Items/Kar98k_Muzzle_Device_C.asset";
        private const string ProjectilePrefabPath = "Assets/_Project/Weapons/Prefabs/WeaponProjectile.prefab";
        private const string RifleViewPrefabPath = "Assets/_Project/Weapons/Prefabs/RifleView.prefab";
        private const string Kar98kScopeViewPrefabPath = "Assets/Low Poly Weapon Pack 4_WWII_1/Prefabs/Attachments/WWII_Optic_Remote_Range_A.prefab";
        private const string Kar98kMuzzleViewPrefabPath = "Assets/Low Poly Weapon Pack 4_WWII_1/Prefabs/Attachments/WWII_Muzzle_Device_C.prefab";
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
            definitionsProp.arraySize = starterPistol != null ? 2 : 1;
            definitionsProp.GetArrayElementAtIndex(0).objectReferenceValue = starterRifle;
            if (starterPistol != null)
            {
                definitionsProp.GetArrayElementAtIndex(1).objectReferenceValue = starterPistol;
            }
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
                    element.FindPropertyRelative("_itemId").stringValue = "weapon-pistol-01";
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
                SetVector3PropertyIfPresent(tuningSo, "_hipLocalPosition", new Vector3(0.015f, 0.15f, 0.005f));
                SetVector3PropertyIfPresent(tuningSo, "_hipLocalEuler", Vector3.zero);
                SetVector3PropertyIfPresent(tuningSo, "_adsLocalPosition", new Vector3(0f, 0.2f, 0.05f));
                SetVector3PropertyIfPresent(tuningSo, "_adsLocalEuler", Vector3.zero);
                SetFloatPropertyIfPresent(tuningSo, "_blendSpeed", 24f);
                SetVector3PropertyIfPresent(tuningSo, "_rifleLocalEulerOffset", new Vector3(90f, 0f, 0f));
                tuningSo.ApplyModifiedPropertiesWithoutUndo();
            }

            if (packController != null && armsAnimator != null)
            {
                armsAnimator.runtimeAnimatorController = packController;
            }

            animationBinder.Configure(armsAnimator, weaponAnimationProfile);

            WireStarterPickups(playerTransform);

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
                entry.FindPropertyRelative("_itemId").stringValue = "weapon-pistol-01";
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

        private static void WireStarterPickups(Transform playerRoot)
        {
            if (playerRoot == null)
            {
                return;
            }

            var rifleSpawn = AssetDatabase.LoadAssetAtPath<ItemSpawnDefinition>(StarterRifleSpawnPath);
            var pistolSpawn = AssetDatabase.LoadAssetAtPath<ItemSpawnDefinition>(StarterPistolSpawnPath);
            var ammo308Spawn = AssetDatabase.LoadAssetAtPath<ItemSpawnDefinition>(Ammo308SpawnPath);
            var ammo9x19Spawn = AssetDatabase.LoadAssetAtPath<ItemSpawnDefinition>(Ammo9x19SpawnPath);
            var kar98kScopeSpawn = AssetDatabase.LoadAssetAtPath<ItemSpawnDefinition>(Kar98kScopeSpawnPath);
            var kar98kMuzzleSpawn = AssetDatabase.LoadAssetAtPath<ItemSpawnDefinition>(Kar98kMuzzleSpawnPath);
            CleanupOrphanPickupVisuals();

            var forward = playerRoot.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var basePos = playerRoot.position + (forward * 2.15f) + (Vector3.up * 0.35f);

            EnsureDefinitionPickup("WeaponSpawn_RifleStarter_LPSP", rifleSpawn, basePos + (right * -0.55f), RifleViewPrefabPath);
            EnsureDefinitionPickup("WeaponSpawn_PistolStarter_LPSP", pistolSpawn, basePos + (right * 0.55f));
            EnsureDefinitionPickup("AmmoSpawn_308_LPSP", ammo308Spawn, basePos + (forward * 0.35f) + (right * -0.45f));
            EnsureDefinitionPickup("AmmoSpawn_9x19_LPSP", ammo9x19Spawn, basePos + (forward * 0.35f) + (right * 0.45f));
            EnsureDefinitionPickup("AttachmentSpawn_Kar98kScope", kar98kScopeSpawn, basePos + (forward * 0.6f) + (right * -0.15f), Kar98kScopeViewPrefabPath);
            EnsureDefinitionPickup("AttachmentSpawn_Kar98kMuzzle", kar98kMuzzleSpawn, basePos + (forward * 0.6f) + (right * 0.15f), Kar98kMuzzleViewPrefabPath);
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

        private static void EnsureDefinitionPickup(string name, ItemSpawnDefinition spawnDefinition, Vector3 worldPosition, string forcedVisualPrefabPath = null)
        {
            if (spawnDefinition == null)
            {
                return;
            }

            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
            }

            go.transform.position = worldPosition;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var pickup = go.GetComponent<DefinitionPickupTarget>();
            if (pickup == null)
            {
                pickup = go.AddComponent<DefinitionPickupTarget>();
            }

            var so = new SerializedObject(pickup);
            so.FindProperty("_spawnDefinition").objectReferenceValue = spawnDefinition;
            so.ApplyModifiedPropertiesWithoutUndo();

            SyncPickupVisual(go.transform, pickup, spawnDefinition, forcedVisualPrefabPath);

            if (go.GetComponent<BoxCollider>() == null)
            {
                var box = go.AddComponent<BoxCollider>();
                box.size = new Vector3(0.35f, 0.2f, 0.2f);
            }

            if (go.GetComponent("WorldObjectIdentity") == null)
            {
                var identityType = System.Type.GetType("Reloader.Core.Persistence.WorldObjectIdentity, Reloader.Core");
                if (identityType != null)
                {
                    go.AddComponent(identityType);
                }
            }

            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(pickup);
        }

        private static void SyncPickupVisual(
            Transform pickupRoot,
            DefinitionPickupTarget pickup,
            ItemSpawnDefinition spawnDefinition,
            string forcedVisualPrefabPath = null)
        {
            if (pickupRoot == null || pickup == null)
            {
                return;
            }

            var existingVisual = pickupRoot.Find("Visual");
            if (existingVisual != null)
            {
                Object.DestroyImmediate(existingVisual.gameObject);
            }

            var sourcePrefab = !string.IsNullOrWhiteSpace(forcedVisualPrefabPath)
                ? AssetDatabase.LoadAssetAtPath<GameObject>(forcedVisualPrefabPath)
                : TryResolvePickupIconPrefab(spawnDefinition);

            GameObject visualRoot = null;
            if (sourcePrefab != null)
            {
                visualRoot = TryInstantiateVisualSource(sourcePrefab);
                if (visualRoot == null)
                {
                    visualRoot = TryCreateMeshProxyVisual(sourcePrefab);
                }
            }
            else if (spawnDefinition != null && spawnDefinition.ItemDefinition != null)
            {
                Debug.LogWarning(
                    $"[MainTownCombatWiring] Missing IconSourcePrefab for item '{spawnDefinition.ItemDefinition.DefinitionId}' while syncing pickup visual '{pickupRoot.name}'.",
                    pickupRoot);
            }

            if (sourcePrefab != null && visualRoot == null)
            {
                Debug.LogWarning(
                    $"[MainTownCombatWiring] Failed to instantiate IconSourcePrefab '{sourcePrefab.name}' for item '{spawnDefinition?.ItemDefinition?.DefinitionId ?? "<unknown>"}' on pickup '{pickupRoot.name}'. Falling back to cube.",
                    pickupRoot);
            }

            if (visualRoot == null)
            {
                visualRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var fallbackCollider = visualRoot.GetComponent<Collider>();
                if (fallbackCollider != null)
                {
                    Object.DestroyImmediate(fallbackCollider);
                }

                visualRoot.transform.localScale = Vector3.one * 0.22f;
            }

            visualRoot.name = "Visual";
            visualRoot.transform.SetParent(pickupRoot, false);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;

            StripVisualPhysics(visualRoot);
            NormalizePickupVisualMaterials(visualRoot);

            var pickupSo = new SerializedObject(pickup);
            pickupSo.FindProperty("_visualRoot").objectReferenceValue = visualRoot;
            pickupSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(visualRoot);
        }

        private static GameObject TryInstantiateVisualSource(GameObject sourcePrefab)
        {
            if (sourcePrefab == null)
            {
                return null;
            }

            try
            {
                var prefabInstance = PrefabUtility.InstantiatePrefab(sourcePrefab);
                if (prefabInstance is GameObject prefabGo)
                {
                    return prefabGo;
                }

                if (prefabInstance is Component prefabComponent)
                {
                    return prefabComponent.gameObject;
                }
            }
            catch (System.InvalidCastException)
            {
                // Some imported assets can keep stale object refs in serialized fields.
                // Fallback path below resolves or returns null safely.
            }

            try
            {
                var rawInstance = Object.Instantiate((Object)sourcePrefab);
                if (rawInstance is GameObject go)
                {
                    return go;
                }

                if (rawInstance is Component component)
                {
                    return component.gameObject;
                }
            }
            catch (System.InvalidCastException)
            {
                return null;
            }

            return null;
        }

        private static GameObject TryCreateMeshProxyVisual(GameObject sourcePrefab)
        {
            if (sourcePrefab == null)
            {
                return null;
            }

            var sourceMeshFilter = sourcePrefab.GetComponentInChildren<MeshFilter>(true);
            var sourceMeshRenderer = sourcePrefab.GetComponentInChildren<MeshRenderer>(true);
            if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null || sourceMeshRenderer == null)
            {
                return null;
            }

            var proxy = new GameObject("VisualProxy");
            var meshFilter = proxy.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

            var meshRenderer = proxy.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = sourceMeshRenderer.sharedMaterials;
            return proxy;
        }

        private static void NormalizePickupVisualMaterials(GameObject visualRoot)
        {
            if (visualRoot == null)
            {
                return;
            }

            var fallbackShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (fallbackShader == null)
            {
                return;
            }

            var renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var sharedMaterials = renderer.sharedMaterials;
                var replaced = false;
                for (var m = 0; m < sharedMaterials.Length; m++)
                {
                    var sourceMaterial = sharedMaterials[m];
                    if (sourceMaterial == null)
                    {
                        continue;
                    }

                    var shader = sourceMaterial.shader;
                    var shaderBroken = shader == null
                        || !shader.isSupported
                        || string.Equals(shader.name, "Hidden/InternalErrorShader", System.StringComparison.OrdinalIgnoreCase);
                    var shaderName = shader != null ? shader.name : string.Empty;
                    var requiresPipelineUpgrade =
                        string.Equals(shaderName, "Standard", System.StringComparison.OrdinalIgnoreCase)
                        || shaderName.StartsWith("Legacy Shaders/", System.StringComparison.OrdinalIgnoreCase);
                    if (!shaderBroken && !requiresPipelineUpgrade)
                    {
                        continue;
                    }

                    var replacement = new Material(fallbackShader);
                    if (sourceMaterial.HasProperty("_BaseMap") && replacement.HasProperty("_BaseMap"))
                    {
                        replacement.SetTexture("_BaseMap", sourceMaterial.GetTexture("_BaseMap"));
                    }
                    else if (sourceMaterial.HasProperty("_MainTex"))
                    {
                        var tex = sourceMaterial.GetTexture("_MainTex");
                        if (replacement.HasProperty("_BaseMap"))
                        {
                            replacement.SetTexture("_BaseMap", tex);
                        }
                        else if (replacement.HasProperty("_MainTex"))
                        {
                            replacement.SetTexture("_MainTex", tex);
                        }
                    }

                    if (sourceMaterial.HasProperty("_BaseColor") && replacement.HasProperty("_BaseColor"))
                    {
                        replacement.SetColor("_BaseColor", sourceMaterial.GetColor("_BaseColor"));
                    }
                    else if (sourceMaterial.HasProperty("_Color"))
                    {
                        var color = sourceMaterial.GetColor("_Color");
                        if (replacement.HasProperty("_BaseColor"))
                        {
                            replacement.SetColor("_BaseColor", color);
                        }
                        else if (replacement.HasProperty("_Color"))
                        {
                            replacement.SetColor("_Color", color);
                        }
                    }

                    // Preserve basic transparency behavior for lens-like materials.
                    var sourceQueueTransparent =
                        sourceMaterial.renderQueue >= 3000
                        || (sourceMaterial.HasProperty("_Mode") && sourceMaterial.GetFloat("_Mode") >= 2.5f);
                    if (sourceQueueTransparent)
                    {
                        if (replacement.HasProperty("_Surface"))
                        {
                            replacement.SetFloat("_Surface", 1f);
                        }

                        if (replacement.HasProperty("_Blend"))
                        {
                            replacement.SetFloat("_Blend", 0f);
                        }

                        replacement.renderQueue = 3000;
                    }

                    sharedMaterials[m] = replacement;
                    replaced = true;
                }

                if (replaced)
                {
                    renderer.sharedMaterials = sharedMaterials;
                }
            }
        }

        private static GameObject TryResolvePickupIconPrefab(ItemSpawnDefinition spawnDefinition)
        {
            if (spawnDefinition == null || spawnDefinition.ItemDefinition == null)
            {
                return null;
            }

            var itemDefinition = spawnDefinition.ItemDefinition;
            var itemId = itemDefinition.DefinitionId;

            GameObject iconPrefab = null;
            try
            {
                iconPrefab = itemDefinition.IconSourcePrefab;
            }
            catch (MissingReferenceException)
            {
                iconPrefab = null;
            }

            if (iconPrefab != null)
            {
                return iconPrefab;
            }

            return ResolvePickupIconFallback(itemId);
        }

        private static GameObject ResolvePickupIconFallback(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            return itemId switch
            {
                "weapon-kar98k" => AssetDatabase.LoadAssetAtPath<GameObject>(RifleViewPrefabPath),
                "att-kar98k-scope-remote-a" => AssetDatabase.LoadAssetAtPath<GameObject>(Kar98kScopeViewPrefabPath),
                "att-kar98k-muzzle-device-c" => AssetDatabase.LoadAssetAtPath<GameObject>(Kar98kMuzzleViewPrefabPath),
                _ => null
            };
        }

        private static void StripVisualPhysics(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    Object.DestroyImmediate(colliders[i]);
                }
            }

            var rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (var i = 0; i < rigidbodies.Length; i++)
            {
                if (rigidbodies[i] != null)
                {
                    Object.DestroyImmediate(rigidbodies[i]);
                }
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
    }
}
#endif
