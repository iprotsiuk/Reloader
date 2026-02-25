using Reloader.NPCs.World;
using UnityEditor;
using UnityEngine;

namespace Reloader.NPCs.Editor
{
    public static class NpcVendorPrefabBuilder
    {
        private const string VendorPrefabPath = "Assets/_Project/NPCs/Prefabs/ShopVendor.prefab";
        private const string PlayerInteractorPrefabPath = "Assets/_Project/NPCs/Prefabs/PlayerShopVendorInteractor.prefab";
        private const string VendorModelPath = "Assets/ThirdParty/Lowpoly Animated Men Pack/Man in Long Sleeves/Male_LongSleeve.fbx";

        [MenuItem("Reloader/NPCs/Rebuild Vendor Prefabs")]
        public static void RebuildAll()
        {
            BuildShopVendorPrefab();
            BuildPlayerInteractorPrefab();
            Debug.Log("NPC vendor prefabs rebuilt.");
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
                root.AddComponent<PlayerShopVendorResolver>();
                root.AddComponent<PlayerShopVendorController>();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerInteractorPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
