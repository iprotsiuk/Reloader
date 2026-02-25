using Reloader.Core.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reloader.UI.Editor
{
    public static class ItemIconSceneWiring
    {
        private const string MainWorldScenePath = "Assets/Scenes/MainWorld.unity";
        private const string CatalogPath = "Assets/_Project/UI/Data/ItemIconCatalog.asset";

        [MenuItem("Tools/Reloader/Wire Item Icon Provider In MainWorld")]
        public static void WireMainWorldMenu()
        {
            WireMainWorldScene();
        }

        public static void WireMainWorldScene()
        {
            var scene = EditorSceneManager.OpenScene(MainWorldScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"ItemIconSceneWiring: failed to open scene at {MainWorldScenePath}.");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<ItemIconCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"ItemIconSceneWiring: missing catalog at {CatalogPath}.");
                return;
            }

            var provider = Object.FindFirstObjectByType<ItemIconCatalogProvider>(FindObjectsInactive.Include);
            if (provider == null)
            {
                var providerGo = new GameObject("ItemIconCatalogProvider");
                provider = providerGo.AddComponent<ItemIconCatalogProvider>();
            }

            var so = new SerializedObject(provider);
            so.FindProperty("_itemIconCatalog").objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(provider);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("ItemIconSceneWiring: MainWorld now has ItemIconCatalogProvider wired.");
        }
    }
}
