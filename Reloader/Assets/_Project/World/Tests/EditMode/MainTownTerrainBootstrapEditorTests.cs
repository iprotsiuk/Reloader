using NUnit.Framework;
using Reloader.World.Editor;
using System.Reflection;
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

                // Restore the shared terrain asset to the authored island state so later tests
                // do not inherit the temporary flattened heightmap in memory.
                MainTownTerrainBootstrap.ApplyLoadedMainTownIslandPass(worldShell.transform, terrain!);
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

        [Test]
        public void ApplyLoadedMainTownIslandPass_RaisesIslandAndCreatesHorizonOcean()
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

                InvokeIslandPass(worldShell.transform, terrain!);

                var terrainData = terrain!.terrainData;
                Assert.That(terrainData.size.y, Is.GreaterThanOrEqualTo(500f), "Expected island pass to reserve enough vertical range for later mountain sculpting.");

                var interiorHeight = AverageTerrainHeight(
                    terrain,
                    new Vector3(0f, 0f, 0f),
                    new Vector3(-250f, 0f, 120f),
                    new Vector3(220f, 0f, -180f));
                var edgeHeight = AverageTerrainHeight(terrain, GetPerimeterSamplePoints(terrain));

                Assert.That(interiorHeight, Is.GreaterThanOrEqualTo(35f), "Expected usable island land to sit well above sea level.");
                Assert.That(edgeHeight, Is.LessThan(interiorHeight - 8f), "Expected the outer perimeter to fall toward the ocean instead of staying flat.");

                var oceanRoot = FindChild(worldShell.transform, "Water_OceanHorizon");
                Assert.That(oceanRoot, Is.Not.Null, "Expected island pass to create a horizon ocean root.");
                Assert.That(oceanRoot!.gameObject.activeSelf, Is.True, "Expected horizon ocean root to be active.");
                Assert.That(oceanRoot.GetComponentsInChildren<Collider>(true), Is.Empty, "Expected ocean presentation to avoid gameplay colliders.");

                var oceanSurface = FindChild(oceanRoot, "OceanSurface");
                Assert.That(oceanSurface, Is.Not.Null, "Expected a dedicated horizon ocean surface.");
                var oceanRenderer = oceanSurface!.GetComponent<Renderer>();
                Assert.That(oceanRenderer, Is.Not.Null, "Expected OceanSurface renderer.");
                AssertOceanMaterialIsOpaque(oceanRenderer!);

                var oceanBoundary = FindChild(worldShell.transform, "Water_OceanBoundary");
                Assert.That(oceanBoundary, Is.Not.Null, "Expected island pass to create invisible ocean blockers.");
                Assert.That(oceanBoundary!.GetComponentsInChildren<BoxCollider>(true).Length, Is.GreaterThanOrEqualTo(8), "Expected enough blocker segments to keep the player out of the ocean.");
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

        private static void InvokeIslandPass(Transform worldShell, Terrain terrain)
        {
            var method = typeof(MainTownTerrainBootstrap).GetMethod(
                "ApplyLoadedMainTownIslandPass",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "Expected MainTownTerrainBootstrap to expose ApplyLoadedMainTownIslandPass for one-off island authoring.");

            method!.Invoke(null, new object[] { worldShell, terrain });
        }

        private static float AverageTerrainHeight(Terrain terrain, params Vector3[] worldPoints)
        {
            var total = 0f;
            foreach (var worldPoint in worldPoints)
            {
                total += SampleTerrainHeight(terrain, worldPoint);
            }

            return total / worldPoints.Length;
        }

        private static float SampleTerrainHeight(Terrain terrain, Vector3 worldPoint)
        {
            var terrainPosition = terrain.transform.position;
            var terrainSize = terrain.terrainData.size;
            var normalizedX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, worldPoint.x);
            var normalizedZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, worldPoint.z);
            return terrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
        }

        private static Vector3[] GetPerimeterSamplePoints(Terrain terrain)
        {
            var halfWidth = terrain.terrainData.size.x * 0.5f - 90f;
            var halfDepth = terrain.terrainData.size.z * 0.5f - 90f;
            return new[]
            {
                new Vector3(-halfWidth, 0f, 0f),
                new Vector3(halfWidth, 0f, 0f),
                new Vector3(0f, 0f, -halfDepth),
                new Vector3(0f, 0f, halfDepth),
            };
        }

        private static void AssertOceanMaterialIsOpaque(Renderer renderer)
        {
            var material = renderer.sharedMaterial;
            Assert.That(material, Is.Not.Null, "Expected horizon ocean renderer to reference a material.");

            if (material!.HasProperty("_Surface"))
            {
                Assert.That(material.GetFloat("_Surface"), Is.EqualTo(0f).Within(0.001f), "Expected ocean material to use opaque surface mode.");
            }

            if (material.HasProperty("_BaseColor"))
            {
                Assert.That(material.GetColor("_BaseColor").a, Is.GreaterThanOrEqualTo(0.99f), "Expected ocean base color alpha to stay opaque.");
            }

            if (material.HasProperty("_Color"))
            {
                Assert.That(material.GetColor("_Color").a, Is.GreaterThanOrEqualTo(0.99f), "Expected ocean color alpha to stay opaque.");
            }
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
