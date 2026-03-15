using NUnit.Framework;
using Reloader.World.Editor;
using System.Reflection;
using System;
using UnityEditor;
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

        [Test]
        public void MainTownScene_ContainsTerrainGeneratorComponent_OnWorldShell()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected MainTownWorldShell in MainTown.");

                var generatorType = Type.GetType("Reloader.World.MainTownTerrainGenerator, Reloader.World");
                Assert.That(generatorType, Is.Not.Null, "Expected a MainTownTerrainGenerator type in the world runtime assembly.");

                var generator = worldShell!.GetComponent(generatorType!);
                Assert.That(generator, Is.Not.Null, "Expected MainTownWorldShell to host the scene terrain generator component.");
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
        public void MainTownTerrainGenerator_RegenerateInEditor_RecreatesMainTownTerrainAndCollider()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected MainTownWorldShell in MainTown.");

                var generator = worldShell!.GetComponent<MainTownTerrainGenerator>();
                Assert.That(generator, Is.Not.Null, "Expected MainTownWorldShell to host MainTownTerrainGenerator.");

                var existingTerrainRoot = FindChild(worldShell.transform, "MainTownTerrain");
                if (existingTerrainRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingTerrainRoot.gameObject);
                }

                var regenerateMethod = typeof(MainTownTerrainGenerator).GetMethod(
                    "RegenerateInEditor",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert.That(regenerateMethod, Is.Not.Null, "Expected the generator to expose RegenerateInEditor for in-scene authoring.");

                regenerateMethod!.Invoke(generator, null);

                var terrainRoot = FindChild(worldShell.transform, "MainTownTerrain");
                Assert.That(terrainRoot, Is.Not.Null, "Expected generator to recreate MainTownTerrain.");

                var terrain = terrainRoot!.GetComponent<Terrain>();
                var terrainCollider = terrainRoot.GetComponent<TerrainCollider>();
                Assert.That(terrain, Is.Not.Null, "Expected Terrain component after regeneration.");
                Assert.That(terrainCollider, Is.Not.Null, "Expected TerrainCollider after regeneration.");
                Assert.That(terrainCollider!.terrainData, Is.SameAs(terrain!.terrainData), "Expected collider to share the generated terrain data.");

                var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/_Project/World/Terrain/MainTown/MainTownTerrainData.asset");
                Assert.That(terrainData, Is.Not.Null, "Expected regenerated MainTownTerrainData asset at the MainTown terrain path.");

                MainTownTerrainBootstrap.ApplyLoadedMainTownIslandPass(worldShell.transform, terrain);
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
        public void MainTownTerrainGenerator_RegenerateInEditor_BuildsFiveKilometerSplitIslandTerrain()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected MainTownWorldShell in MainTown.");

                var generator = worldShell!.GetComponent<MainTownTerrainGenerator>();
                Assert.That(generator, Is.Not.Null, "Expected MainTownWorldShell to host MainTownTerrainGenerator.");

                generator!.RegenerateInEditor();

                var terrainRoot = FindChild(worldShell.transform, "MainTownTerrain");
                Assert.That(terrainRoot, Is.Not.Null, "Expected generator to leave MainTownTerrain in the scene.");

                var terrain = terrainRoot!.GetComponent<Terrain>();
                Assert.That(terrain, Is.Not.Null, "Expected Terrain on MainTownTerrain.");

                var terrainData = terrain!.terrainData;
                Assert.That(terrainData.size.x, Is.EqualTo(5000f).Within(0.01f), "Expected 5km terrain width.");
                Assert.That(terrainData.size.z, Is.EqualTo(5000f).Within(0.01f), "Expected 5km terrain depth.");
                Assert.That(terrainData.size.y, Is.GreaterThanOrEqualTo(900f), "Expected a taller vertical range so generated mountains are not capped too low.");

                var layerNames = Array.ConvertAll(terrainData.terrainLayers, layer => layer != null ? layer.name : string.Empty);
                CollectionAssert.Contains(layerNames, "MainTown_Sand", "Expected a dedicated beach sand terrain layer.");
                CollectionAssert.Contains(layerNames, "MainTown_Grass", "Expected a grass terrain layer.");
                CollectionAssert.Contains(layerNames, "MainTown_Stone", "Expected a rock terrain layer.");

                Assert.That(SampleTerrainHeight(terrain, new Vector3(-1350f, 0f, -950f)), Is.GreaterThan(45f), "Expected northwest island above water.");
                Assert.That(SampleTerrainHeight(terrain, new Vector3(1450f, 0f, -950f)), Is.GreaterThan(35f), "Expected northeast island above water.");
                Assert.That(SampleTerrainHeight(terrain, new Vector3(0f, 0f, 1250f)), Is.GreaterThan(45f), "Expected the southern island above water.");
                Assert.That(SampleTerrainHeight(terrain, new Vector3(0f, 0f, 0f)), Is.LessThan(24f), "Expected the main cross-island river channel to sit near water level.");
                Assert.That(SampleTerrainHeight(terrain, new Vector3(900f, 0f, 1100f)), Is.LessThan(24f), "Expected the south branch of the T channel to sit near water level.");
                Assert.That(SampleTerrainHeight(terrain, new Vector3(900f, 0f, -1100f)), Is.GreaterThan(35f), "Expected the branch channel not to continue north and create a fourth island.");
                Assert.That(GetTerrainHeightRange(terrain), Is.GreaterThan(240f), "Expected significant relief for mountains and cliffs.");

                MainTownTerrainBootstrap.ApplyLoadedMainTownIslandPass(worldShell.transform, terrain);
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
        public void MainTownTerrainGenerator_HasCustomEditorInspector()
        {
            var editorType = Type.GetType("Reloader.World.Editor.MainTownTerrainGeneratorEditor, Reloader.World.Editor");
            Assert.That(editorType, Is.Not.Null, "Expected a custom editor for the terrain generator so regeneration is clickable in the Inspector.");
            Assert.That(typeof(UnityEditor.Editor).IsAssignableFrom(editorType), Is.True, "Expected the terrain generator editor to derive from UnityEditor.Editor.");
        }

        [Test]
        public void MainTownTerrainGenerator_RegenerateInEditor_RerollsSeedAndChangesTerrain()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected MainTownWorldShell in MainTown.");

                var generator = worldShell!.GetComponent<MainTownTerrainGenerator>();
                Assert.That(generator, Is.Not.Null, "Expected MainTownWorldShell to host MainTownTerrainGenerator.");

                var initialSeed = GetPrivateField<int>(generator!, "seed");
                generator.RegenerateInEditor();

                var terrain = FindChild(worldShell.transform, "MainTownTerrain")!.GetComponent<Terrain>();
                Assert.That(terrain, Is.Not.Null, "Expected terrain after first regeneration.");
                var firstHeight = SampleTerrainHeight(terrain!, new Vector3(620f, 0f, -1220f));

                generator.RegenerateInEditor();

                var rerolledSeed = GetPrivateField<int>(generator!, "seed");
                var secondHeight = SampleTerrainHeight(terrain!, new Vector3(620f, 0f, -1220f));

                Assert.That(rerolledSeed, Is.Not.EqualTo(initialSeed), "Expected regenerate to reroll the seed by default.");
                Assert.That(Mathf.Abs(secondHeight - firstHeight), Is.GreaterThan(5f), "Expected a reroll to materially change the terrain shape.");

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
        public void MainTownTerrainGenerator_RegenerateInEditor_BuildsDistinctAndBroadMountainProfiles()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected MainTownWorldShell in MainTown.");

                var generator = worldShell!.GetComponent<MainTownTerrainGenerator>();
                Assert.That(generator, Is.Not.Null, "Expected MainTownWorldShell to host MainTownTerrainGenerator.");

                generator!.RegenerateInEditor();

                var terrain = FindChild(worldShell.transform, "MainTownTerrain")!.GetComponent<Terrain>();
                Assert.That(terrain, Is.Not.Null, "Expected terrain after regeneration.");

                var northWestProfile = SampleHeightProfile(terrain!, new Vector3(-1750f, 0f, -900f), new Vector3(-850f, 0f, -900f), 24);
                var northEastProfile = SampleHeightProfile(terrain!, new Vector3(850f, 0f, -900f), new Vector3(1750f, 0f, -900f), 24);

                var northWestRoughness = CalculateAverageSecondDifference(northWestProfile);
                var northEastRoughness = CalculateAverageSecondDifference(northEastProfile);
                var profileDifference = CalculateAverageNormalizedProfileDifference(northWestProfile, northEastProfile);

                Assert.That(northWestRoughness, Is.LessThan(18f), $"Expected northwest mountain profile to be broad rather than serrated, but roughness was {northWestRoughness:F2}.");
                Assert.That(northEastRoughness, Is.LessThan(18f), $"Expected northeast mountain profile to be broad rather than serrated, but roughness was {northEastRoughness:F2}.");
                Assert.That(profileDifference, Is.GreaterThan(0.12f), $"Expected the north island mountain profiles to differ materially, but normalized difference was only {profileDifference:F2}.");

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
        public void MainTownTerrainGenerator_RegenerateInEditor_PreservesInspectorTuningValues()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected MainTownWorldShell in MainTown.");

                var generator = worldShell!.GetComponent<MainTownTerrainGenerator>();
                Assert.That(generator, Is.Not.Null, "Expected MainTownWorldShell to host MainTownTerrainGenerator.");

                SetPrivateField(generator!, "terrainWidthMeters", 4321f);
                SetPrivateField(generator!, "terrainHeightMeters", 777f);
                SetPrivateField(generator!, "mountainAmplitudeMeters", 222f);
                SetPrivateField(generator!, "mainRiverHalfWidthMeters", 123f);

                generator.RegenerateInEditor();

                Assert.That(GetPrivateField<float>(generator!, "terrainWidthMeters"), Is.EqualTo(4321f).Within(0.01f), "Expected regenerate to preserve tuned terrain width.");
                Assert.That(GetPrivateField<float>(generator!, "terrainHeightMeters"), Is.EqualTo(777f).Within(0.01f), "Expected regenerate to preserve tuned terrain height.");
                Assert.That(GetPrivateField<float>(generator!, "mountainAmplitudeMeters"), Is.EqualTo(222f).Within(0.01f), "Expected regenerate to preserve tuned mountain amplitude.");
                Assert.That(GetPrivateField<float>(generator!, "mainRiverHalfWidthMeters"), Is.EqualTo(123f).Within(0.01f), "Expected regenerate to preserve tuned river width.");
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

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {target.GetType().Name}.");
            return (T)field!.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {target.GetType().Name}.");
            field!.SetValue(target, value);
        }

        private static float[] SampleHeightProfile(Terrain terrain, Vector3 worldStart, Vector3 worldEnd, int sampleCount)
        {
            var profile = new float[sampleCount];
            for (var index = 0; index < sampleCount; index++)
            {
                var t = sampleCount == 1 ? 0f : index / (float)(sampleCount - 1);
                var worldPoint = Vector3.Lerp(worldStart, worldEnd, t);
                profile[index] = SampleTerrainHeight(terrain, worldPoint);
            }

            return profile;
        }

        private static float CalculateAverageSecondDifference(float[] values)
        {
            var total = 0f;
            for (var index = 1; index < values.Length - 1; index++)
            {
                total += Mathf.Abs(values[index + 1] - (2f * values[index]) + values[index - 1]);
            }

            return total / Mathf.Max(1, values.Length - 2);
        }

        private static float CalculateAverageNormalizedProfileDifference(float[] a, float[] b)
        {
            var normalizedA = NormalizeProfile(a);
            var normalizedB = NormalizeProfile(b);
            var total = 0f;
            for (var index = 0; index < normalizedA.Length; index++)
            {
                total += Mathf.Abs(normalizedA[index] - normalizedB[index]);
            }

            return total / normalizedA.Length;
        }

        private static float[] NormalizeProfile(float[] values)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            foreach (var value in values)
            {
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }

            var range = Mathf.Max(1f, max - min);
            var normalized = new float[values.Length];
            for (var index = 0; index < values.Length; index++)
            {
                normalized[index] = (values[index] - min) / range;
            }

            return normalized;
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
