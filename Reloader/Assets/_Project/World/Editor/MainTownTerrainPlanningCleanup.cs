using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Editor
{
    internal static class MainTownTerrainPlanningCleanup
    {
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string WorldShellName = "MainTownWorldShell";

        private static readonly HashSet<string> ExactNamesToRemove = new()
        {
            "MountainRim",
            "Landmark_RiverBridge",
            "PlayerOverlookHill",
            "WaterTowerHill",
            "ChurchSlope",
            "NorthValleyApproach",
            "EastValleyApproach",
            "PlayerOverlookApproach",
            "WaterTowerApproach",
            "QuarrySouthApproach",
            "QuarryWestRamp",
            "RidgeNorth_InnerSlope",
            "RidgeSouth_InnerSlope",
            "RidgeEast_InnerSlope",
            "RidgeWest_InnerSlope",
        };

        private static readonly string[] PrefixesToRemove =
        {
            "Water_",
            "ForestTree_",
            "ForestDensityLayer_",
            "ForestGapCluster_",
        };

        [MenuItem("Tools/Reloader/World/Clean MainTown To Terrain Planning Shell")]
        private static void CleanMainTownToTerrainPlanningShell()
        {
            var scene = EnsureMainTownSceneLoaded();
            var worldShell = FindRoot(scene, WorldShellName);
            if (worldShell == null)
            {
                Debug.LogError($"Terrain-planning cleanup aborted. Root '{WorldShellName}' was not found in MainTown.");
                return;
            }

            var targets = FindRemovalTargets(scene, worldShell.transform);
            if (targets.Count == 0)
            {
                Debug.Log("Terrain-planning cleanup found no removable landscape presentation objects.");
                return;
            }

            foreach (var target in targets.OrderByDescending(GetHierarchyDepth))
            {
                Undo.DestroyObjectImmediate(target);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeObject = worldShell;
            Debug.Log($"MainTown terrain-planning cleanup complete. Removed {targets.Count} landscape presentation objects.");
        }

        private static Scene EnsureMainTownSceneLoaded()
        {
            var scene = SceneManager.GetSceneByPath(MainTownScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                return scene;
            }

            return EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Single);
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == rootName);
        }

        private static List<GameObject> FindRemovalTargets(Scene scene, Transform worldShell)
        {
            var allObjects = new List<GameObject>();
            allObjects.AddRange(scene.GetRootGameObjects());
            allObjects.AddRange(worldShell.GetComponentsInChildren<Transform>(true).Select(transform => transform.gameObject));

            return allObjects
                .Where(gameObject => gameObject != null)
                .Distinct()
                .Where(ShouldRemove)
                .ToList();
        }

        private static bool ShouldRemove(GameObject gameObject)
        {
            if (ExactNamesToRemove.Contains(gameObject.name))
            {
                return true;
            }

            foreach (var prefix in PrefixesToRemove)
            {
                if (gameObject.name.StartsWith(prefix))
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetHierarchyDepth(GameObject gameObject)
        {
            var depth = 0;
            var current = gameObject.transform.parent;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }
    }
}
