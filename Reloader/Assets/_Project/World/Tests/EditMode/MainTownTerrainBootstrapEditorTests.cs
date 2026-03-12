using NUnit.Framework;
using Reloader.World.Editor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Tests.EditMode
{
    public class MainTownTerrainBootstrapEditorTests
    {
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";

        [Test]
        public void FlattenMainTownTerrainToPlanningShell_ResetsTerrainWaterAndForestPresentation()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected MainTownWorldShell in MainTown.");

                var terrainRoot = FindChild(worldShell!.transform, "MainTownTerrain");
                Assert.That(terrainRoot, Is.Not.Null, "Expected MainTownTerrain in MainTown.");

                var terrain = terrainRoot!.GetComponent<Terrain>();
                Assert.That(terrain, Is.Not.Null, "Expected Terrain on MainTownTerrain.");

                MainTownTerrainBootstrap.FlattenLoadedMainTownTerrain(worldShell.transform, terrain!);

                Assert.That(GetTerrainHeightRange(terrain!), Is.LessThanOrEqualTo(0.01f), "Expected flatten command to clear dramatic relief.");
                Assert.That(terrain.terrainData.treeInstances.Length, Is.EqualTo(0), "Expected flatten command to clear terrain trees.");

                var riverRoot = FindChild(worldShell.transform, "Water_RiverChannel");
                var reservoirRoot = FindChild(worldShell.transform, "Water_ReservoirBasin");
                Assert.That(riverRoot == null || !riverRoot.gameObject.activeSelf, Is.True, "Expected flatten command to remove or deactivate river presentation.");
                Assert.That(reservoirRoot == null || !reservoirRoot.gameObject.activeSelf, Is.True, "Expected flatten command to remove or deactivate reservoir presentation.");

                var basinFloor = FindChild(worldShell.transform, "BasinFloor");
                Assert.That(basinFloor, Is.Not.Null, "Expected BasinFloor planning shell object.");
                Assert.That(basinFloor!.gameObject.activeSelf, Is.True, "Expected flatten command to re-enable BasinFloor.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == rootName)
                {
                    return root;
                }
            }

            return null;
        }

        private static Transform FindChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            foreach (var child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static float GetTerrainHeightRange(Terrain terrain)
        {
            var heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
            var min = float.MaxValue;
            var max = float.MinValue;

            for (var z = 0; z < heights.GetLength(0); z++)
            {
                for (var x = 0; x < heights.GetLength(1); x++)
                {
                    var height = heights[z, x] * terrain.terrainData.size.y;
                    if (height < min)
                    {
                        min = height;
                    }

                    if (height > max)
                    {
                        max = height;
                    }
                }
            }

            return max - min;
        }
    }
}
