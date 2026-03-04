using System.Collections.Generic;
using System.IO;
using Reloader.Core.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reloader.UI.Editor
{
    public static class ItemIconSceneWiring
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string IndoorRangeScenePath = "Assets/_Project/World/Scenes/IndoorRangeInstance.unity";
        private const string MainWorldScenePath = "Assets/Scenes/MainWorld.unity";
        private const string CatalogPath = "Assets/_Project/UI/Data/ItemIconCatalog.asset";
        private static readonly string[] CandidateWorldScenePaths =
        {
            BootstrapScenePath,
            MainTownScenePath,
            IndoorRangeScenePath,
            MainWorldScenePath
        };

        [MenuItem("Tools/Reloader/Wire Item Icon Provider In World Scenes")]
        public static void WireMainWorldMenu()
        {
            WireCandidateWorldScenes();
        }

        public static void WireMainWorldScene()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemIconCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"ItemIconSceneWiring: missing catalog at {CatalogPath}.");
                return;
            }

            WireScene(MainWorldScenePath, catalog);
        }

        public static IReadOnlyList<string> GetCandidateWorldScenePaths() => CandidateWorldScenePaths;

        private static void WireCandidateWorldScenes()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemIconCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"ItemIconSceneWiring: missing catalog at {CatalogPath}.");
                return;
            }

            var wiredScenes = 0;
            for (var i = 0; i < CandidateWorldScenePaths.Length; i++)
            {
                if (WireScene(CandidateWorldScenePaths[i], catalog))
                {
                    wiredScenes++;
                }
            }

            AssetDatabase.SaveAssets();
            if (wiredScenes == 0)
            {
                Debug.LogWarning("ItemIconSceneWiring: no candidate world scenes were found to wire.");
                return;
            }

            Debug.Log($"ItemIconSceneWiring: wired ItemIconCatalogProvider in {wiredScenes} scene(s).");
        }

        private static bool WireScene(string scenePath, ItemIconCatalog catalog)
        {
            if (!File.Exists(scenePath))
            {
                return false;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"ItemIconSceneWiring: failed to open scene at {scenePath}.");
                return false;
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
            return true;
        }
    }
}
