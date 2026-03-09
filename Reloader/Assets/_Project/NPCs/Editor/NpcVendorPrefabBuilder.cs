using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Capabilities;
using Reloader.NPCs.Data;
using Reloader.NPCs.World;
using Reloader.Player;
using Reloader.World.Editor;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reloader.NPCs.Editor
{
    public static class NpcVendorPrefabBuilder
    {
        private const bool IncludeOptionalRoleCapabilities = false;
        private const string VendorPrefabPath = "Assets/_Project/NPCs/Prefabs/ShopVendor.prefab";
        private const string PlayerInteractorPrefabPath = "Assets/_Project/NPCs/Prefabs/PlayerShopVendorInteractor.prefab";
        private const string NpcFoundationPrefabPath = "Assets/_Project/NPCs/Prefabs/NpcFoundation.prefab";
        private const string RolePrefabFolderPath = "Assets/_Project/NPCs/Prefabs/Roles";
        private const string ReloadingCatalogPath = "Assets/_Project/Economy/Data/ReloadingStore_DefaultCatalog.asset";
        private const string AmmoCatalogPath = "Assets/_Project/Economy/Data/AmmoStore_DefaultCatalog.asset";
        private const string WeaponCatalogPath = "Assets/_Project/Economy/Data/WeaponStore_DefaultCatalog.asset";
        private const string FrontDeskDialogueAssetPath = "Assets/_Project/NPCs/Data/Definitions/Dialogue_FrontDeskClerk.asset";
        private const string PoliceStopDialogueAssetPath = "Assets/_Project/NPCs/Data/Definitions/Dialogue_PoliceStop.asset";

        private static readonly RolePrefabConfig[] RolePrefabConfigs =
        {
            RolePrefabConfig.NonVendor(
                NpcRoleKind.Police,
                "Police",
                new[] { NpcCapabilityKind.LawEnforcementInteraction }),
            RolePrefabConfig.NonVendor(NpcRoleKind.GameWarden, "GameWarden"),
            RolePrefabConfig.Vendor(NpcRoleKind.WeaponVendor, "WeaponVendor", "vendor-weapon-store"),
            RolePrefabConfig.Vendor(NpcRoleKind.AmmoVendor, "AmmoVendor", "vendor-ammo-store"),
            RolePrefabConfig.Vendor(NpcRoleKind.ReloadingSuppliesVendor, "ReloadingSuppliesVendor", "vendor-reloading-store"),
            RolePrefabConfig.NonVendor(
                NpcRoleKind.FrontDeskClerk,
                "FrontDeskClerk",
                new[] { NpcCapabilityKind.FrontDeskInteraction, NpcCapabilityKind.EntryFeeInteraction, NpcCapabilityKind.Dialogue }),
            RolePrefabConfig.NonVendor(
                NpcRoleKind.RangeSafetyOfficer,
                "RangeSafetyOfficer",
                new[] { NpcCapabilityKind.EntryFeeInteraction, NpcCapabilityKind.Dialogue }),
            RolePrefabConfig.NonVendor(NpcRoleKind.Competitor, "Competitor"),
            RolePrefabConfig.NonVendor(
                NpcRoleKind.CompetitionOrganizer,
                "CompetitionOrganizer",
                new[] { NpcCapabilityKind.FrontDeskInteraction, NpcCapabilityKind.EntryFeeInteraction, NpcCapabilityKind.Dialogue }),
            RolePrefabConfig.NonVendor(
                NpcRoleKind.BankWorker,
                "BankWorker",
                new[] { NpcCapabilityKind.FrontDeskInteraction, NpcCapabilityKind.Dialogue }),
            RolePrefabConfig.NonVendor(
                NpcRoleKind.PostWorker,
                "PostWorker",
                new[] { NpcCapabilityKind.FrontDeskInteraction, NpcCapabilityKind.Dialogue })
        };

        private static readonly CapabilityComponentBinding[] ManagedRoleCapabilityBindings =
        {
            CapabilityComponentBinding.Create(NpcCapabilityKind.Dialogue, typeof(DialogueCapability)),
            CapabilityComponentBinding.Create(NpcCapabilityKind.LawEnforcementInteraction, typeof(LawEnforcementInteractionCapability)),
            CapabilityComponentBinding.Create(NpcCapabilityKind.FrontDeskInteraction, typeof(FrontDeskInteractionCapability)),
            CapabilityComponentBinding.Create(NpcCapabilityKind.EntryFeeInteraction, typeof(EntryFeeInteractionCapability))
        };

        [MenuItem("Reloader/NPCs/Rebuild Vendor Prefabs")]
        public static void RebuildAll()
        {
            BuildShopVendorPrefab();
            BuildPlayerInteractorPrefab();
            Debug.Log("NPC vendor prefabs rebuilt.");
        }

        [MenuItem("Reloader/NPCs/Foundation/Rebuild Base NPC Foundation Prefab")]
        public static void RebuildBaseNpcFoundationPrefab()
        {
            BuildBaseNpcFoundationPrefab();
            Debug.Log("Base NPC foundation prefab rebuilt.");
        }

        [MenuItem("Reloader/NPCs/Foundation/Create Role Prefab Variants")]
        public static void CreateRolePrefabVariants()
        {
            EnsureFolderExists(RolePrefabFolderPath);
            var basePrefab = EnsureBaseFoundationPrefabAsset();

            for (var i = 0; i < RolePrefabConfigs.Length; i++)
            {
                BuildRoleVariantPrefab(basePrefab, RolePrefabConfigs[i]);
            }

            ValidateRolePrefabVariants();
            Debug.Log($"NPC role prefabs rebuilt ({RolePrefabConfigs.Length} variants).");
        }

        [MenuItem("Reloader/NPCs/Foundation/Rebuild NPC Foundation + Role Variants")]
        public static void RebuildNpcFoundationAndRoleVariants()
        {
            BuildBaseNpcFoundationPrefab();
            CreateRolePrefabVariants();
            Debug.Log("NPC foundation and role prefabs rebuilt.");
        }

        [MenuItem("Reloader/NPCs/Foundation/Validate Role Prefab Variants")]
        public static void ValidateRolePrefabVariants()
        {
            var errorCount = 0;
            var warningCount = 0;

            for (var i = 0; i < RolePrefabConfigs.Length; i++)
            {
                var config = RolePrefabConfigs[i];
                var prefabPath = GetRolePrefabPath(config);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                ValidateRolePrefab(prefab, prefabPath, config, ref errorCount, ref warningCount);
            }

            if (errorCount > 0 || warningCount > 0)
            {
                Debug.LogWarning(
                    $"NPC role prefab validation finished with {errorCount} error(s) and {warningCount} warning(s).");
                return;
            }

            Debug.Log($"NPC role prefab validation passed ({RolePrefabConfigs.Length} variants).");
        }

        [MenuItem("Reloader/NPCs/Auto-Wire Vendor Interaction In Active Scene")]
        public static void AutoWireVendorInteractionInActiveScene()
        {
            var sceneDirty = false;

            var interactorRoot = FindOrCreateInteractorRoot();
            if (interactorRoot != null)
            {
                sceneDirty |= WireInteractorInScene(interactorRoot);
            }

            sceneDirty |= WireEconomyInScene();

            if (sceneDirty)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("Vendor interaction auto-wiring applied to active scene.");
            }
            else
            {
                Debug.Log("Vendor interaction auto-wiring found nothing to change.");
            }
        }

        public static void BuildShopVendorPrefab()
        {
            var root = BuildNpcFoundationRoot("ShopVendor");
            try
            {
                EnsureVendorComponents(root, "vendor-reloading-store");
                ApplySeededAppearance(root, "vendor.vendor-reloading-store");
                PrefabUtility.SaveAsPrefabAsset(root, VendorPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildBaseNpcFoundationPrefab()
        {
            var root = BuildNpcFoundationRoot("NpcFoundation");
            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, NpcFoundationPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject EnsureBaseFoundationPrefabAsset()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NpcFoundationPrefabPath);
            if (prefab != null)
            {
                return prefab;
            }

            BuildBaseNpcFoundationPrefab();
            return AssetDatabase.LoadAssetAtPath<GameObject>(NpcFoundationPrefabPath);
        }

        private static GameObject BuildNpcFoundationRoot(string rootName)
        {
            var root = new GameObject(rootName);
            root.AddComponent<NpcAgent>();
            root.AddComponent<MainTownNpcAppearanceApplicator>();

            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = Vector3.one;

            var collider = body.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.height = 1.8f;
            collider.radius = 0.35f;

            BuildStyleVisualRoot(root.transform);
            ApplySeededAppearance(root, rootName);

            return root;
        }

        private static void BuildRoleVariantPrefab(GameObject basePrefab, RolePrefabConfig config)
        {
            if (basePrefab == null)
            {
                Debug.LogError($"Cannot build role variant '{config.Role}': missing base prefab at '{NpcFoundationPrefabPath}'.");
                return;
            }

            var root = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
            if (root == null)
            {
                root = UnityEngine.Object.Instantiate(basePrefab);
            }

            try
            {
                root.name = $"Npc_{config.PrefabName}";
                if (config.IsVendor)
                {
                    EnsureVendorComponents(root, config.VendorId);
                }
                else
                {
                    RemoveVendorComponents(root);
                }

                EnsureRoleCapabilities(root, config);
                ApplyRoleAuthoredDefinitions(root, config);
                var appearanceSeedKey = config.IsVendor && !string.IsNullOrWhiteSpace(config.VendorId)
                    ? $"vendor.{config.VendorId}"
                    : config.PrefabName;
                ApplySeededAppearance(root, appearanceSeedKey);
                var prefabPath = GetRolePrefabPath(config);
                var errorCount = 0;
                var warningCount = 0;
                ValidateRolePrefab(root, prefabPath, config, ref errorCount, ref warningCount);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static string GetRolePrefabPath(RolePrefabConfig config)
        {
            return $"{RolePrefabFolderPath}/Npc_{config.PrefabName}.prefab";
        }

        private static void EnsureVendorComponents(GameObject root, string vendorId)
        {
            if (root == null)
            {
                return;
            }

            var target = root.GetComponent<ShopVendorTarget>();
            if (target == null)
            {
                target = root.AddComponent<ShopVendorTarget>();
            }

            var targetSerialized = new SerializedObject(target);
            var vendorIdProperty = targetSerialized.FindProperty("_vendorId");
            if (vendorIdProperty != null)
            {
                vendorIdProperty.stringValue = vendorId;
            }

            targetSerialized.ApplyModifiedPropertiesWithoutUndo();

            if (root.GetComponent<VendorTradeCapability>() == null)
            {
                root.AddComponent<VendorTradeCapability>();
            }
        }

        private static void EnsureRoleCapabilities(GameObject root, RolePrefabConfig config)
        {
            if (root == null)
            {
                return;
            }

            for (var i = 0; i < ManagedRoleCapabilityBindings.Length; i++)
            {
                var binding = ManagedRoleCapabilityBindings[i];
                var shouldExist = config.RequiresCapability(binding.Kind)
                    || (IncludeOptionalRoleCapabilities && config.SupportsOptionalCapability(binding.Kind));
                var existing = root.GetComponent(binding.ComponentType);

                if (shouldExist && existing == null)
                {
                    root.AddComponent(binding.ComponentType);
                    continue;
                }

                if (!shouldExist && existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing);
                }
            }
        }

        private static void ApplyRoleAuthoredDefinitions(GameObject root, RolePrefabConfig config)
        {
            if (root == null)
            {
                return;
            }

            if (config.Role == NpcRoleKind.FrontDeskClerk)
            {
                var capability = root.GetComponent<DialogueCapability>();
                if (capability != null)
                {
                    AssignDialogueDefinition(capability, FrontDeskDialogueAssetPath);
                }
            }

            if (config.Role == NpcRoleKind.Police)
            {
                var capability = root.GetComponent<LawEnforcementInteractionCapability>();
                if (capability != null)
                {
                    AssignDialogueDefinition(capability, PoliceStopDialogueAssetPath);
                }
            }
        }

        private static void AssignDialogueDefinition(Component capability, string assetPath)
        {
            if (capability == null || string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            var definition = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(assetPath);
            if (definition == null)
            {
                Debug.LogWarning($"Missing dialogue definition asset at '{assetPath}' for '{capability.GetType().Name}'.");
                return;
            }

            var serialized = new SerializedObject(capability);
            var definitionProperty = serialized.FindProperty("_definition");
            if (definitionProperty == null)
            {
                Debug.LogWarning($"Capability '{capability.GetType().Name}' is missing '_definition' for authored dialogue binding.");
                return;
            }

            definitionProperty.objectReferenceValue = definition;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ValidateRolePrefab(
            GameObject prefab,
            string prefabPath,
            RolePrefabConfig config,
            ref int errorCount,
            ref int warningCount)
        {
            if (prefab == null)
            {
                Debug.LogError($"Missing role prefab asset for '{config.Role}' at '{prefabPath}'.");
                errorCount++;
                return;
            }

            if (prefab.GetComponent<NpcAgent>() == null)
            {
                Debug.LogError($"Role prefab '{prefabPath}' is missing required {nameof(NpcAgent)}.");
                errorCount++;
            }

            for (var i = 0; i < ManagedRoleCapabilityBindings.Length; i++)
            {
                var binding = ManagedRoleCapabilityBindings[i];
                var hasCapability = prefab.GetComponent(binding.ComponentType) != null;
                var required = config.RequiresCapability(binding.Kind);
                var optional = config.SupportsOptionalCapability(binding.Kind);
                var expected = required || (IncludeOptionalRoleCapabilities && optional);

                if (required && !hasCapability)
                {
                    Debug.LogError(
                        $"Role prefab '{prefabPath}' missing required capability '{binding.Kind}' ({binding.ComponentType.Name}).");
                    errorCount++;
                    continue;
                }

                if (!expected && hasCapability)
                {
                    Debug.LogWarning(
                        $"Role prefab '{prefabPath}' has unexpected capability '{binding.Kind}' ({binding.ComponentType.Name}).");
                    warningCount++;
                }
            }

            var hasVendorTarget = prefab.GetComponent<ShopVendorTarget>() != null;
            var hasVendorCapability = prefab.GetComponent<VendorTradeCapability>() != null;
            if (config.IsVendor)
            {
                if (!hasVendorTarget || !hasVendorCapability)
                {
                    Debug.LogError(
                        $"Vendor role prefab '{prefabPath}' must include both {nameof(ShopVendorTarget)} and {nameof(VendorTradeCapability)}.");
                    errorCount++;
                }
            }
            else if (hasVendorTarget || hasVendorCapability)
            {
                Debug.LogWarning(
                    $"Non-vendor role prefab '{prefabPath}' still has vendor wiring components.");
                warningCount++;
            }
        }

        private static void RemoveVendorComponents(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var vendorCapability = root.GetComponent<VendorTradeCapability>();
            if (vendorCapability != null)
            {
                UnityEngine.Object.DestroyImmediate(vendorCapability);
            }

            var vendorTarget = root.GetComponent<ShopVendorTarget>();
            if (vendorTarget != null)
            {
                UnityEngine.Object.DestroyImmediate(vendorTarget);
            }
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var segments = folderPath.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = segments[i];
                var candidate = $"{current}/{next}";
                if (!AssetDatabase.IsValidFolder(candidate))
                {
                    AssetDatabase.CreateFolder(current, next);
                }

                current = candidate;
            }
        }

        private static void BuildStyleVisualRoot(Transform parent)
        {
            var existing = parent.Find("VisualRoot");
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(parent, false);

            BuildStyleRig(StyleCrowdReviewGender.Male, visualRoot.transform, "StyleMaleRoot");
            BuildStyleRig(StyleCrowdReviewGender.Female, visualRoot.transform, "StyleFemaleRoot");
        }

        private static void BuildStyleRig(StyleCrowdReviewGender gender, Transform parent, string instanceName)
        {
            var modelPath = StyleCrowdReviewBuilder.GetModelAssetPath(gender);
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                Debug.LogWarning($"STYLE rig not found at '{modelPath}'. Creating fallback capsule for '{instanceName}'.");
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fallback.name = instanceName;
                fallback.transform.SetParent(parent, false);
                fallback.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                fallback.transform.localScale = new Vector3(0.85f, 1f, 0.85f);
                return;
            }

            var model = PrefabUtility.InstantiatePrefab(modelAsset, parent) as GameObject;
            model ??= UnityEngine.Object.Instantiate(modelAsset, parent);
            model.name = instanceName;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            foreach (var childCollider in model.GetComponentsInChildren<Collider>(true))
            {
                childCollider.enabled = false;
            }

            AssignStyleMaterials(model.transform, gender);
            SetStyleRigInactive(model.transform);
        }

        private static void AssignStyleMaterials(Transform rigRoot, StyleCrowdReviewGender gender)
        {
            if (rigRoot == null)
            {
                return;
            }

            foreach (Transform child in rigRoot)
            {
                var renderer = child.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                var materialPath = StyleCrowdReviewBuilder.GetExternalMaterialPath(gender, child.name);
                if (string.IsNullOrWhiteSpace(materialPath))
                {
                    continue;
                }

                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static void SetStyleRigInactive(Transform rigRoot)
        {
            if (rigRoot == null)
            {
                return;
            }

            foreach (Transform child in rigRoot)
            {
                child.gameObject.SetActive(false);
            }
        }

        private static void ApplySeededAppearance(GameObject root, string seedKey)
        {
            if (root == null)
            {
                return;
            }

            var applicator = root.GetComponent<MainTownNpcAppearanceApplicator>();
            if (applicator == null)
            {
                return;
            }

            applicator.ApplySeededAppearance(seedKey);
        }

        public static void BuildPlayerInteractorPrefab()
        {
            var root = new GameObject("PlayerShopVendorInteractor");
            try
            {
                var vendorResolver = root.AddComponent<PlayerShopVendorResolver>();
                var vendorController = root.AddComponent<PlayerShopVendorController>();
                var npcResolver = root.AddComponent<PlayerNpcResolver>();
                var npcController = root.AddComponent<PlayerNpcInteractionController>();
                WireVendorInteractorReferences(vendorController, vendorResolver);
                WireNpcInteractorReferences(npcController, npcResolver);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerInteractorPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void WireVendorInteractorReferences(PlayerShopVendorController controller, PlayerShopVendorResolver resolver)
        {
            if (controller == null || resolver == null)
            {
                return;
            }

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("_resolverBehaviour").objectReferenceValue = resolver;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireNpcInteractorReferences(PlayerNpcInteractionController controller, PlayerNpcResolver resolver)
        {
            if (controller == null || resolver == null)
            {
                return;
            }

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("_resolverBehaviour").objectReferenceValue = resolver;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject FindOrCreateInteractorRoot()
        {
            var existingControllers = UnityEngine.Object.FindObjectsByType<PlayerShopVendorController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var existingController = ResolvePreferredInteractorController(existingControllers);
            if (existingController != null)
            {
                RemoveDuplicateInteractorControllers(existingControllers, existingController);
                return existingController.gameObject;
            }

            var existingNpcController = UnityEngine.Object.FindFirstObjectByType<PlayerNpcInteractionController>(FindObjectsInactive.Include);
            if (existingNpcController != null)
            {
                var candidateRoot = existingNpcController.gameObject;
                var hasVendorController = candidateRoot.GetComponent<PlayerShopVendorController>() != null;
                var hasVendorResolver = candidateRoot.GetComponent<PlayerShopVendorResolver>() != null;
                if (hasVendorController || hasVendorResolver)
                {
                    return candidateRoot;
                }
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerInteractorPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Cannot auto-wire vendor interaction: missing prefab at '{PlayerInteractorPrefabPath}'.");
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            return instance;
        }

        private static PlayerShopVendorController ResolvePreferredInteractorController(PlayerShopVendorController[] controllers)
        {
            if (controllers == null || controllers.Length == 0)
            {
                return null;
            }

            var playerInput = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>(FindObjectsInactive.Include);
            if (playerInput != null)
            {
                var playerTransform = playerInput.transform;
                for (var i = 0; i < controllers.Length; i++)
                {
                    var controller = controllers[i];
                    if (controller != null && controller.transform.parent == playerTransform)
                    {
                        return controller;
                    }
                }
            }

            for (var i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null)
                {
                    return controllers[i];
                }
            }

            return null;
        }

        private static void RemoveDuplicateInteractorControllers(PlayerShopVendorController[] controllers, PlayerShopVendorController keep)
        {
            if (controllers == null || keep == null)
            {
                return;
            }

            for (var i = 0; i < controllers.Length; i++)
            {
                var controller = controllers[i];
                if (controller == null || controller == keep)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(controller.gameObject);
            }
        }

        private static bool WireInteractorInScene(GameObject interactorRoot)
        {
            var changed = false;
            var vendorController = interactorRoot.GetComponent<PlayerShopVendorController>();
            var vendorResolver = interactorRoot.GetComponent<PlayerShopVendorResolver>();
            var npcController = interactorRoot.GetComponent<PlayerNpcInteractionController>();
            var npcResolver = interactorRoot.GetComponent<PlayerNpcResolver>();
            if (vendorController == null && npcController == null)
            {
                return false;
            }

            var playerInput = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>(FindObjectsInactive.Include);
            var playerTransform = playerInput != null ? playerInput.transform : null;
            if (playerTransform != null && interactorRoot.transform.parent != playerTransform)
            {
                Undo.SetTransformParent(interactorRoot.transform, playerTransform, "Parent Vendor Interactor To Player");
                interactorRoot.transform.localPosition = Vector3.zero;
                interactorRoot.transform.localRotation = Quaternion.identity;
                interactorRoot.transform.localScale = Vector3.one;
                changed = true;
            }

            if (vendorController != null && vendorResolver != null)
            {
                var controllerSerialized = new SerializedObject(vendorController);
                var inputProp = controllerSerialized.FindProperty("_inputSourceBehaviour");
                var resolverProp = controllerSerialized.FindProperty("_resolverBehaviour");
                if (inputProp != null && inputProp.objectReferenceValue != playerInput)
                {
                    inputProp.objectReferenceValue = playerInput;
                    changed = true;
                }

                if (resolverProp != null && resolverProp.objectReferenceValue != vendorResolver)
                {
                    resolverProp.objectReferenceValue = vendorResolver;
                    changed = true;
                }

                controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            if (npcController != null && npcResolver != null)
            {
                var controllerSerialized = new SerializedObject(npcController);
                var inputProp = controllerSerialized.FindProperty("_inputSourceBehaviour");
                var resolverProp = controllerSerialized.FindProperty("_resolverBehaviour");
                if (inputProp != null && inputProp.objectReferenceValue != playerInput)
                {
                    inputProp.objectReferenceValue = playerInput;
                    changed = true;
                }

                if (resolverProp != null && resolverProp.objectReferenceValue != npcResolver)
                {
                    resolverProp.objectReferenceValue = npcResolver;
                    changed = true;
                }

                controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var sceneCamera = Camera.main;
            if (sceneCamera == null)
            {
                sceneCamera = UnityEngine.Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            }

            if (vendorResolver != null)
            {
                var resolverSerialized = new SerializedObject(vendorResolver);
                var playerCameraProp = resolverSerialized.FindProperty("_playerCamera");
                if (playerCameraProp != null && playerCameraProp.objectReferenceValue != sceneCamera)
                {
                    playerCameraProp.objectReferenceValue = sceneCamera;
                    changed = true;
                }

                resolverSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            if (npcResolver != null)
            {
                var resolverSerialized = new SerializedObject(npcResolver);
                var playerCameraProp = resolverSerialized.FindProperty("_playerCamera");
                if (playerCameraProp != null && playerCameraProp.objectReferenceValue != sceneCamera)
                {
                    playerCameraProp.objectReferenceValue = sceneCamera;
                    changed = true;
                }

                resolverSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return changed;
        }

        private static bool WireEconomyInScene()
        {
            var changed = false;
            var economyController = UnityEngine.Object.FindFirstObjectByType<Reloader.Economy.EconomyController>(FindObjectsInactive.Include);
            if (economyController == null)
            {
                return false;
            }

            var reloadingCatalog = AssetDatabase.LoadAssetAtPath<Reloader.Economy.ShopCatalogDefinition>(ReloadingCatalogPath);
            var ammoCatalog = AssetDatabase.LoadAssetAtPath<Reloader.Economy.ShopCatalogDefinition>(AmmoCatalogPath);
            var weaponCatalog = AssetDatabase.LoadAssetAtPath<Reloader.Economy.ShopCatalogDefinition>(WeaponCatalogPath);
            if (reloadingCatalog == null || ammoCatalog == null || weaponCatalog == null)
            {
                return false;
            }

            var serialized = new SerializedObject(economyController);
            var defaultVendorIdProp = serialized.FindProperty("_defaultVendorId");
            var defaultVendorCatalogProp = serialized.FindProperty("_defaultVendorCatalog");
            if (defaultVendorIdProp != null && defaultVendorIdProp.stringValue != "vendor-reloading-store")
            {
                defaultVendorIdProp.stringValue = "vendor-reloading-store";
                changed = true;
            }

            if (defaultVendorCatalogProp != null && defaultVendorCatalogProp.objectReferenceValue != reloadingCatalog)
            {
                defaultVendorCatalogProp.objectReferenceValue = reloadingCatalog;
                changed = true;
            }

            var vendorsProp = serialized.FindProperty("_vendors");
            if (vendorsProp != null)
            {
                changed |= EnsureRoleVendorCatalogMappings(vendorsProp, reloadingCatalog, ammoCatalog, weaponCatalog);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }

        private static bool EnsureRoleVendorCatalogMappings(
            SerializedProperty vendorsProp,
            Reloader.Economy.ShopCatalogDefinition reloadingCatalog,
            Reloader.Economy.ShopCatalogDefinition ammoCatalog,
            Reloader.Economy.ShopCatalogDefinition weaponCatalog)
        {
            var changed = false;

            for (var i = 0; i < RolePrefabConfigs.Length; i++)
            {
                var config = RolePrefabConfigs[i];
                if (!config.IsVendor || string.IsNullOrWhiteSpace(config.VendorId))
                {
                    continue;
                }

                var index = FindVendorEntryIndex(vendorsProp, config.VendorId);
                var expectedCatalog = ResolveCatalogForVendor(config.VendorId, reloadingCatalog, ammoCatalog, weaponCatalog);
                if (index < 0)
                {
                    index = vendorsProp.arraySize;
                    vendorsProp.arraySize += 1;
                    var created = vendorsProp.GetArrayElementAtIndex(index);
                    created.FindPropertyRelative("_vendorId").stringValue = config.VendorId;
                    created.FindPropertyRelative("_catalog").objectReferenceValue = expectedCatalog;
                    changed = true;
                    continue;
                }

                var existing = vendorsProp.GetArrayElementAtIndex(index);
                var existingCatalog = existing.FindPropertyRelative("_catalog");
                if (existingCatalog != null && existingCatalog.objectReferenceValue != expectedCatalog)
                {
                    existingCatalog.objectReferenceValue = expectedCatalog;
                    changed = true;
                }
            }

            return changed;
        }

        private static Reloader.Economy.ShopCatalogDefinition ResolveCatalogForVendor(
            string vendorId,
            Reloader.Economy.ShopCatalogDefinition reloadingCatalog,
            Reloader.Economy.ShopCatalogDefinition ammoCatalog,
            Reloader.Economy.ShopCatalogDefinition weaponCatalog)
        {
            if (string.Equals(vendorId, "vendor-ammo-store", global::System.StringComparison.Ordinal))
            {
                return ammoCatalog;
            }

            if (string.Equals(vendorId, "vendor-weapon-store", global::System.StringComparison.Ordinal))
            {
                return weaponCatalog;
            }

            return reloadingCatalog;
        }

        private static int FindVendorEntryIndex(SerializedProperty vendorsProp, string vendorId)
        {
            for (var i = 0; i < vendorsProp.arraySize; i++)
            {
                var entry = vendorsProp.GetArrayElementAtIndex(i);
                var entryVendorId = entry.FindPropertyRelative("_vendorId");
                if (entryVendorId == null || !string.Equals(entryVendorId.stringValue, vendorId, global::System.StringComparison.Ordinal))
                {
                    continue;
                }

                return i;
            }

            return -1;
        }

        private readonly struct RolePrefabConfig
        {
            public RolePrefabConfig(
                NpcRoleKind role,
                string prefabName,
                bool isVendor,
                string vendorId,
                NpcCapabilityKind[] requiredCapabilities,
                NpcCapabilityKind[] optionalCapabilities)
            {
                Role = role;
                PrefabName = prefabName;
                IsVendor = isVendor;
                VendorId = vendorId;
                RequiredCapabilities = requiredCapabilities ?? Array.Empty<NpcCapabilityKind>();
                OptionalCapabilities = optionalCapabilities ?? Array.Empty<NpcCapabilityKind>();
            }

            public NpcRoleKind Role { get; }

            public string PrefabName { get; }

            public bool IsVendor { get; }

            public string VendorId { get; }

            public NpcCapabilityKind[] RequiredCapabilities { get; }

            public NpcCapabilityKind[] OptionalCapabilities { get; }

            public bool RequiresCapability(NpcCapabilityKind kind)
            {
                return Contains(RequiredCapabilities, kind);
            }

            public bool SupportsOptionalCapability(NpcCapabilityKind kind)
            {
                return Contains(OptionalCapabilities, kind);
            }

            public static RolePrefabConfig NonVendor(
                NpcRoleKind role,
                string prefabName,
                NpcCapabilityKind[] requiredCapabilities = null,
                NpcCapabilityKind[] optionalCapabilities = null)
            {
                return new RolePrefabConfig(
                    role,
                    prefabName,
                    false,
                    string.Empty,
                    requiredCapabilities,
                    optionalCapabilities);
            }

            public static RolePrefabConfig Vendor(
                NpcRoleKind role,
                string prefabName,
                string vendorId,
                NpcCapabilityKind[] requiredCapabilities = null,
                NpcCapabilityKind[] optionalCapabilities = null)
            {
                return new RolePrefabConfig(
                    role,
                    prefabName,
                    true,
                    vendorId,
                    requiredCapabilities,
                    optionalCapabilities);
            }

            private static bool Contains(NpcCapabilityKind[] items, NpcCapabilityKind kind)
            {
                for (var i = 0; i < items.Length; i++)
                {
                    if (items[i] == kind)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private readonly struct CapabilityComponentBinding
        {
            public CapabilityComponentBinding(NpcCapabilityKind kind, Type componentType)
            {
                Kind = kind;
                ComponentType = componentType;
            }

            public NpcCapabilityKind Kind { get; }

            public Type ComponentType { get; }

            public static CapabilityComponentBinding Create(NpcCapabilityKind kind, Type componentType)
            {
                return new CapabilityComponentBinding(kind, componentType);
            }
        }
    }
}
