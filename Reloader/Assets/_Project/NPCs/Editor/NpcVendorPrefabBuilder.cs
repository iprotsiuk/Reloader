using Reloader.NPCs.World;
using Reloader.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reloader.NPCs.Editor
{
    public static class NpcVendorPrefabBuilder
    {
        private const string VendorPrefabPath = "Assets/_Project/NPCs/Prefabs/ShopVendor.prefab";
        private const string PlayerInteractorPrefabPath = "Assets/_Project/NPCs/Prefabs/PlayerShopVendorInteractor.prefab";
        private const string VendorModelPath = "Assets/ThirdParty/Lowpoly Animated Men Pack/Man in Long Sleeves/Male_LongSleeve.fbx";
        private const string DefaultCatalogPath = "Assets/_Project/Economy/Data/ReloadingStore_DefaultCatalog.asset";

        [MenuItem("Reloader/NPCs/Rebuild Vendor Prefabs")]
        public static void RebuildAll()
        {
            BuildShopVendorPrefab();
            BuildPlayerInteractorPrefab();
            Debug.Log("NPC vendor prefabs rebuilt.");
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
            var root = new GameObject("ShopVendor");
            try
            {
                var body = new GameObject("Body");
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;
                body.transform.localScale = Vector3.one;

                var visualModel = TryInstantiateModel(VendorModelPath, body.transform, "VendorModel");
                if (visualModel == null)
                {
                    var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    fallback.name = "VendorModel_Fallback";
                    fallback.transform.SetParent(body.transform, false);
                    fallback.transform.localPosition = new Vector3(0f, 1f, 0f);
                    fallback.transform.localScale = new Vector3(0.85f, 1f, 0.85f);
                }

                var collider = body.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0f, 0.9f, 0f);
                collider.height = 1.8f;
                collider.radius = 0.35f;

                var target = root.AddComponent<ShopVendorTarget>();
                var serialized = new SerializedObject(target);
                serialized.FindProperty("_vendorId").stringValue = "vendor-reloading-store";
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, VendorPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject TryInstantiateModel(string modelPath, Transform parent, string instanceName)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                Debug.LogWarning($"Vendor model not found at '{modelPath}'. Using fallback capsule.");
                return null;
            }

            var model = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            model ??= Object.Instantiate(modelAsset);
            model.name = instanceName;
            model.transform.SetParent(parent, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            foreach (var childCollider in model.GetComponentsInChildren<Collider>(true))
            {
                childCollider.enabled = false;
            }

            return model;
        }

        public static void BuildPlayerInteractorPrefab()
        {
            var root = new GameObject("PlayerShopVendorInteractor");
            try
            {
                var resolver = root.AddComponent<PlayerShopVendorResolver>();
                var controller = root.AddComponent<PlayerShopVendorController>();
                WireInteractorReferences(controller, resolver);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerInteractorPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void WireInteractorReferences(PlayerShopVendorController controller, PlayerShopVendorResolver resolver)
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
            var existingController = Object.FindFirstObjectByType<PlayerShopVendorController>(FindObjectsInactive.Include);
            if (existingController != null)
            {
                return existingController.gameObject;
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

        private static bool WireInteractorInScene(GameObject interactorRoot)
        {
            var changed = false;
            var controller = interactorRoot.GetComponent<PlayerShopVendorController>();
            var resolver = interactorRoot.GetComponent<PlayerShopVendorResolver>();
            if (controller == null || resolver == null)
            {
                return false;
            }

            var playerInput = Object.FindFirstObjectByType<PlayerInputReader>(FindObjectsInactive.Include);
            var playerTransform = playerInput != null ? playerInput.transform : null;
            if (playerTransform != null && interactorRoot.transform.parent != playerTransform)
            {
                Undo.SetTransformParent(interactorRoot.transform, playerTransform, "Parent Vendor Interactor To Player");
                interactorRoot.transform.localPosition = Vector3.zero;
                interactorRoot.transform.localRotation = Quaternion.identity;
                interactorRoot.transform.localScale = Vector3.one;
                changed = true;
            }

            var controllerSerialized = new SerializedObject(controller);
            var inputProp = controllerSerialized.FindProperty("_inputSourceBehaviour");
            var resolverProp = controllerSerialized.FindProperty("_resolverBehaviour");
            if (inputProp != null && inputProp.objectReferenceValue != playerInput)
            {
                inputProp.objectReferenceValue = playerInput;
                changed = true;
            }

            if (resolverProp != null && resolverProp.objectReferenceValue != resolver)
            {
                resolverProp.objectReferenceValue = resolver;
                changed = true;
            }

            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            var sceneCamera = Camera.main;
            if (sceneCamera == null)
            {
                sceneCamera = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            }

            var resolverSerialized = new SerializedObject(resolver);
            var playerCameraProp = resolverSerialized.FindProperty("_playerCamera");
            if (playerCameraProp != null && playerCameraProp.objectReferenceValue != sceneCamera)
            {
                playerCameraProp.objectReferenceValue = sceneCamera;
                changed = true;
            }

            resolverSerialized.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }

        private static bool WireEconomyInScene()
        {
            var changed = false;
            var economyController = Object.FindFirstObjectByType<Reloader.Economy.EconomyController>(FindObjectsInactive.Include);
            if (economyController == null)
            {
                return false;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<Reloader.Economy.ShopCatalogDefinition>(DefaultCatalogPath);
            if (catalog == null)
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

            if (defaultVendorCatalogProp != null && defaultVendorCatalogProp.objectReferenceValue != catalog)
            {
                defaultVendorCatalogProp.objectReferenceValue = catalog;
                changed = true;
            }

            var vendorsProp = serialized.FindProperty("_vendors");
            if (vendorsProp != null && vendorsProp.arraySize == 0)
            {
                vendorsProp.arraySize = 1;
                var first = vendorsProp.GetArrayElementAtIndex(0);
                first.FindPropertyRelative("_vendorId").stringValue = "vendor-reloading-store";
                first.FindPropertyRelative("_catalog").objectReferenceValue = catalog;
                changed = true;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }
    }
}
