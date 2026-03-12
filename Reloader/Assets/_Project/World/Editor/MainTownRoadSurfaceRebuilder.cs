using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Editor
{
    internal static class MainTownRoadSurfaceRebuilder
    {
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string WorldShellName = "MainTownWorldShell";
        private const string AsphaltPrefabPath = "Assets/ThirdParty/SimplePoly - Town Pack/Prefabs/RoadSegments/sptp_asphalt_01.prefab";
        private const string PavedMaterialPath = "Assets/EasyRoads3D/Resources/Materials/roads/road material.mat";
        private const string DirtMaterialPath = "Assets/EasyRoads3D/Resources/Materials/roads/dirt material.mat";

        [MenuItem("Tools/Reloader/World/Rebuild MainTown Road Surfaces")]
        private static void RebuildMainTownRoadSurfaces()
        {
            var scene = EnsureMainTownSceneLoaded();
            var worldShell = FindRoot(scene, WorldShellName);
            if (worldShell == null)
            {
                Debug.LogError($"Road surface rebuild aborted. Root '{WorldShellName}' was not found in MainTown.");
                return;
            }

            var roadMesh = LoadRoadMesh();
            var pavedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PavedMaterialPath);
            var dirtMaterial = AssetDatabase.LoadAssetAtPath<Material>(DirtMaterialPath);

            if (roadMesh == null || pavedMaterial == null || dirtMaterial == null)
            {
                Debug.LogError("Road surface rebuild aborted. Required mesh or materials could not be loaded.");
                return;
            }

            var guideRoads = FindGuideRoads(worldShell.transform);
            if (guideRoads.Count == 0)
            {
                Debug.LogError("Road surface rebuild aborted. No visual road strips were found.");
                return;
            }

            foreach (var guideRoad in guideRoads)
            {
                ReplaceRoadSurface(guideRoad, roadMesh, pavedMaterial, dirtMaterial);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeObject = worldShell;
            Debug.Log($"MainTown road surface rebuild complete. Updated {guideRoads.Count} road strips.");
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

        private static Mesh LoadRoadMesh()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AsphaltPrefabPath);
            if (prefab == null)
            {
                return null;
            }

            var meshFilter = prefab.GetComponent<MeshFilter>();
            return meshFilter != null ? meshFilter.sharedMesh : null;
        }

        private static List<GameObject> FindGuideRoads(Transform worldShell)
        {
            return worldShell
                .GetComponentsInChildren<Transform>(true)
                .Select(transform => transform.gameObject)
                .Where(IsVisualRoadStrip)
                .OrderBy(gameObject => gameObject.name)
                .ToList();
        }

        private static bool IsVisualRoadStrip(GameObject gameObject)
        {
            if (gameObject.name is "PerimeterLoopRoad" or "MainStreetSpine" or "ServiceRoads")
            {
                return false;
            }

            var matchesRoadNaming =
                gameObject.name.StartsWith("Road_") ||
                gameObject.name.StartsWith("MainStreet") ||
                gameObject.name.StartsWith("Loop_");

            if (!matchesRoadNaming)
            {
                return false;
            }

            return gameObject.GetComponent<MeshFilter>() != null && gameObject.GetComponent<MeshRenderer>() != null;
        }

        private static void ReplaceRoadSurface(GameObject guideRoad, Mesh roadMesh, Material pavedMaterial, Material dirtMaterial)
        {
            var meshFilter = guideRoad.GetComponent<MeshFilter>();
            var renderer = guideRoad.GetComponent<MeshRenderer>();

            if (meshFilter == null || renderer == null)
            {
                return;
            }

            Undo.RecordObject(meshFilter, $"Rebuild {guideRoad.name} road mesh");
            Undo.RecordObject(renderer, $"Rebuild {guideRoad.name} road material");

            meshFilter.sharedMesh = roadMesh;
            renderer.sharedMaterials = new[] { IsDirtRoute(guideRoad.name) ? dirtMaterial : pavedMaterial };
        }

        private static bool IsDirtRoute(string routeName)
        {
            return routeName.Contains("Road_Dirt_");
        }
    }
}
