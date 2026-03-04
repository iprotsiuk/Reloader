#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.Tools.Editor
{
    public static class FPArmsTuningSceneBuilder
    {
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string TuningScenePath = "Assets/_Project/World/Scenes/FPArmsTuning/FPArmsTuning.unity";

        [MenuItem("Reloader/Tools/FP Arms/Sync PlayerRoot From MainTown")]
        public static void SyncPlayerRootFromMainTown()
        {
            var tuningScene = EditorSceneManager.OpenScene(TuningScenePath, OpenSceneMode.Single);
            if (!tuningScene.IsValid())
            {
                Debug.LogError($"Could not open tuning scene: {TuningScenePath}");
                return;
            }

            var existingRoot = GameObject.Find("PlayerRoot");
            if (existingRoot != null)
            {
                Object.DestroyImmediate(existingRoot);
            }

            var mainTownScene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);
            if (!mainTownScene.IsValid())
            {
                Debug.LogError($"Could not open source scene: {MainTownScenePath}");
                return;
            }

            var sourceRoot = FindRootInScene(mainTownScene, "PlayerRoot");
            if (sourceRoot == null)
            {
                EditorSceneManager.CloseScene(mainTownScene, true);
                Debug.LogError("PlayerRoot not found in MainTown.");
                return;
            }

            var clonedRoot = Object.Instantiate(sourceRoot);
            clonedRoot.name = "PlayerRoot";
            clonedRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(clonedRoot, tuningScene);

            // Close source scene without saving any accidental changes.
            EditorSceneManager.CloseScene(mainTownScene, true);

            EditorSceneManager.MarkSceneDirty(tuningScene);
            EditorSceneManager.SaveScene(tuningScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("FPArmsTuning: PlayerRoot synced from MainTown.");
        }

        private static GameObject FindRootInScene(Scene scene, string rootName)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root != null && root.name == rootName)
                {
                    return root;
                }
            }

            return null;
        }
    }
}
#endif
