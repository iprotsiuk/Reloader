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

                Assert.That(FindChild(worldShell.transform, "Water_OceanBoundary"), Is.Null, "Expected island pass to leave ocean blockers out of the planning scene.");
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
        public void MainTownTerrainGenerator_RegenerateInEditor_OnRectangularCanvas_LeavesOceanMarginsAroundContainedLandmass()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected MainTownWorldShell in MainTown.");

                var generator = worldShell!.GetComponent<MainTownTerrainGenerator>();
                Assert.That(generator, Is.Not.Null, "Expected MainTownWorldShell to host MainTownTerrainGenerator.");

                SetPrivateField(generator!, "rerollSeedOnRegenerate", false);
                SetPrivateField(generator!, "seed", 24681357);
                SetPrivateField(generator!, "terrainWidthMeters", 3000f);
                SetPrivateField(generator!, "terrainDepthMeters", 4000f);
                SetPrivateField(generator!, "terrainHeightMeters", 1100f);
                SetPrivateField(generator!, "landFootprintMeters", 2200f);
                SetPrivateField(generator!, "satelliteChance", 0f);
                SetPrivateField(generator!, "riverCount", 1);

                generator!.RegenerateInEditor();

                var terrainRoot = FindChild(worldShell.transform, "MainTownTerrain");
                Assert.That(terrainRoot, Is.Not.Null, "Expected generator to leave MainTownTerrain in the scene.");

                var terrain = terrainRoot!.GetComponent<Terrain>();
                Assert.That(terrain, Is.Not.Null, "Expected Terrain on MainTownTerrain.");

                var terrainData = terrain!.terrainData;
                var waterLevel = GetPrivateField<float>(generator!, "waterLevelMeters");
                Assert.That(terrainData.size.x, Is.EqualTo(3000f).Within(0.01f), "Expected regeneration to respect a tuned rectangular terrain width.");
                Assert.That(terrainData.size.z, Is.EqualTo(4000f).Within(0.01f), "Expected regeneration to respect a tuned rectangular terrain depth.");
                Assert.That(terrainData.size.y, Is.GreaterThanOrEqualTo(900f), "Expected enough vertical range for the volcanic island relief.");

                var layerNames = Array.ConvertAll(terrainData.terrainLayers, layer => layer != null ? layer.name : string.Empty);
                CollectionAssert.Contains(layerNames, "MainTown_Sand", "Expected a dedicated beach sand terrain layer.");
                CollectionAssert.Contains(layerNames, "MainTown_Grass", "Expected a grass terrain layer.");
                CollectionAssert.Contains(layerNames, "MainTown_Stone", "Expected a rock terrain layer.");

                var edgeHeight = AverageTerrainHeight(terrain, GetPerimeterSamplePoints(terrain));
                var landCoverage = CalculateLandCoverage(terrain, waterLevel + 8f, 28);
                var peakPoint = FindPeakWorldPoint(terrain);

                Assert.That(edgeHeight, Is.LessThan(waterLevel + 6f), "Expected the outer perimeter to remain ocean so the island does not clip against the terrain bounds.");
                Assert.That(landCoverage, Is.InRange(0.08f, 0.42f), $"Expected a bounded land footprint rather than filling the full rectangular canvas, but coverage was {landCoverage:F2}.");
                Assert.That(Mathf.Abs(peakPoint.x), Is.LessThan(terrainData.size.x * 0.5f - 260f), "Expected the highest relief to sit comfortably inside the terrain bounds.");
                Assert.That(Mathf.Abs(peakPoint.z), Is.LessThan(terrainData.size.z * 0.5f - 260f), "Expected the highest relief to sit comfortably inside the terrain bounds.");
                Assert.That(peakPoint.y, Is.GreaterThan(waterLevel + 180f), "Expected a dominant volcanic landmass rather than a mostly flat planning shell.");
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
        public void MainTownTerrainGenerator_RegenerateInEditor_BuildsBroadNonSerratedVolcanicProfiles()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected MainTownWorldShell in MainTown.");

                var generator = worldShell!.GetComponent<MainTownTerrainGenerator>();
                Assert.That(generator, Is.Not.Null, "Expected MainTownWorldShell to host MainTownTerrainGenerator.");

                SetPrivateField(generator!, "rerollSeedOnRegenerate", false);
                SetPrivateField(generator!, "seed", 1357911);
                SetPrivateField(generator!, "terrainWidthMeters", 3200f);
                SetPrivateField(generator!, "terrainDepthMeters", 4000f);
                SetPrivateField(generator!, "landFootprintMeters", 2400f);
                SetPrivateField(generator!, "satelliteChance", 0f);
                SetPrivateField(generator!, "riverCount", 0);

                generator!.RegenerateInEditor();

                var terrain = FindChild(worldShell.transform, "MainTownTerrain")!.GetComponent<Terrain>();
                Assert.That(terrain, Is.Not.Null, "Expected terrain after regeneration.");

                var peakPoint = FindPeakWorldPoint(terrain!);
                var eastWestProfile = SampleHeightProfile(terrain!, peakPoint + new Vector3(-420f, 0f, 0f), peakPoint + new Vector3(420f, 0f, 0f), 28);
                var northSouthProfile = SampleHeightProfile(terrain!, peakPoint + new Vector3(0f, 0f, -420f), peakPoint + new Vector3(0f, 0f, 420f), 28);

                var eastWestRoughness = CalculateAverageSecondDifference(eastWestProfile);
                var northSouthRoughness = CalculateAverageSecondDifference(northSouthProfile);
                var eastWestRange = CalculateProfileRange(eastWestProfile);
                var northSouthRange = CalculateProfileRange(northSouthProfile);

                Assert.That(eastWestRange, Is.GreaterThan(150f), $"Expected the dominant island to contain substantial east-west volcanic relief, but range was only {eastWestRange:F2}.");
                Assert.That(northSouthRange, Is.GreaterThan(150f), $"Expected the dominant island to contain substantial north-south volcanic relief, but range was only {northSouthRange:F2}.");
                Assert.That(eastWestRoughness, Is.LessThan(16f), $"Expected the east-west mountain profile to stay broad rather than serrated, but roughness was {eastWestRoughness:F2}.");
                Assert.That(northSouthRoughness, Is.LessThan(16f), $"Expected the north-south mountain profile to stay broad rather than serrated, but roughness was {northSouthRoughness:F2}.");

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
                SetPrivateField(generator!, "terrainDepthMeters", 3456f);
                SetPrivateField(generator!, "terrainHeightMeters", 777f);
                SetPrivateField(generator!, "landFootprintMeters", 2100f);
                SetPrivateField(generator!, "mountainAmplitudeMeters", 222f);
                SetPrivateField(generator!, "satelliteChance", 0.2f);
                SetPrivateField(generator!, "riverCount", 2);

                generator.RegenerateInEditor();

                Assert.That(GetPrivateField<float>(generator!, "terrainWidthMeters"), Is.EqualTo(4321f).Within(0.01f), "Expected regenerate to preserve tuned terrain width.");
                Assert.That(GetPrivateField<float>(generator!, "terrainDepthMeters"), Is.EqualTo(3456f).Within(0.01f), "Expected regenerate to preserve tuned terrain depth.");
                Assert.That(GetPrivateField<float>(generator!, "terrainHeightMeters"), Is.EqualTo(777f).Within(0.01f), "Expected regenerate to preserve tuned terrain height.");
                Assert.That(GetPrivateField<float>(generator!, "landFootprintMeters"), Is.EqualTo(2100f).Within(0.01f), "Expected regenerate to preserve tuned land footprint.");
                Assert.That(GetPrivateField<float>(generator!, "mountainAmplitudeMeters"), Is.EqualTo(222f).Within(0.01f), "Expected regenerate to preserve tuned mountain amplitude.");
                Assert.That(GetPrivateField<float>(generator!, "satelliteChance"), Is.EqualTo(0.2f).Within(0.001f), "Expected regenerate to preserve tuned satellite chance.");
                Assert.That(GetPrivateField<int>(generator!, "riverCount"), Is.EqualTo(2), "Expected regenerate to preserve tuned river count.");
            }
            finally
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                var terrain = worldShell != null
                    ? FindChild(worldShell.transform, "MainTownTerrain")?.GetComponent<Terrain>()
                    : null;
                if (worldShell != null && terrain != null)
                {
                    MainTownTerrainBootstrap.ApplyLoadedMainTownIslandPass(worldShell.transform, terrain);
                }

                EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        [Test]
        public void MainTownTerrainGenerator_RepaintTerrainLayersInEditor_UsesWaterHeightAndSteepnessRules()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected MainTownWorldShell in MainTown.");

                var generator = worldShell!.GetComponent<MainTownTerrainGenerator>();
                Assert.That(generator, Is.Not.Null, "Expected MainTownWorldShell to host MainTownTerrainGenerator.");

                SetPrivateField(generator!, "rerollSeedOnRegenerate", false);
                SetPrivateField(generator!, "seed", 424242);
                SetPrivateField(generator!, "terrainWidthMeters", 3000f);
                SetPrivateField(generator!, "terrainDepthMeters", 4000f);
                SetPrivateField(generator!, "terrainHeightMeters", 1100f);
                generator.RegenerateInEditor();

                var terrain = FindChild(worldShell.transform, "MainTownTerrain")!.GetComponent<Terrain>();
                Assert.That(terrain, Is.Not.Null, "Expected terrain after regeneration.");

                ApplyLayerClassificationFixture(terrain!);
                generator.RepaintTerrainLayersInEditor();

                Assert.That(GetDominantTerrainLayer(terrain!, 0.16f, 0.16f), Is.EqualTo(0), "Expected low flat shoreline terrain to paint as sand.");
                Assert.That(GetDominantTerrainLayer(terrain!, 0.5f, 0.5f), Is.EqualTo(1), "Expected moderate inland terrain to paint as grass.");
                Assert.That(GetDominantTerrainLayer(terrain!, 0.72f, 0.5f), Is.EqualTo(2), "Expected steep high terrain to paint as rock.");
            }
            finally
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                var terrain = worldShell != null
                    ? FindChild(worldShell.transform, "MainTownTerrain")?.GetComponent<Terrain>()
                    : null;
                if (worldShell != null && terrain != null)
                {
                    MainTownTerrainBootstrap.ApplyLoadedMainTownIslandPass(worldShell.transform, terrain);
                }

                EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        [Test]
        public void MainTownTerrainGenerator_RegenerateInEditor_DetailPassAddsLocalMountainVariation()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected MainTownWorldShell in MainTown.");

                var generator = worldShell!.GetComponent<MainTownTerrainGenerator>();
                Assert.That(generator, Is.Not.Null, "Expected MainTownWorldShell to host MainTownTerrainGenerator.");

                SetPrivateField(generator!, "rerollSeedOnRegenerate", false);
                SetPrivateField(generator!, "seed", 556677);
                SetPrivateField(generator!, "terrainWidthMeters", 3200f);
                SetPrivateField(generator!, "terrainDepthMeters", 4000f);
                SetPrivateField(generator!, "landFootprintMeters", 2400f);
                SetPrivateField(generator!, "satelliteChance", 0f);
                SetPrivateField(generator!, "riverCount", 0);
                SetPrivateField(generator!, "detailAmplitudeMeters", 0f);
                SetPrivateField(generator!, "pitAmplitudeMeters", 0f);
                SetPrivateField(generator!, "cliffDetailStrengthMeters", 0f);

                generator.RegenerateInEditor();

                var terrain = FindChild(worldShell.transform, "MainTownTerrain")!.GetComponent<Terrain>();
                Assert.That(terrain, Is.Not.Null, "Expected terrain after regeneration.");

                var peakPoint = FindPeakWorldPoint(terrain!);
                var flankStart = peakPoint + new Vector3(-260f, 0f, 80f);
                var flankEnd = peakPoint + new Vector3(160f, 0f, 240f);
                var baselineProfile = SampleHeightProfile(terrain!, flankStart, flankEnd, 64);
                var baselineRoughness = CalculateAverageSecondDifference(baselineProfile);

                SetPrivateField(generator!, "detailAmplitudeMeters", 24f);
                SetPrivateField(generator!, "pitAmplitudeMeters", 12f);
                SetPrivateField(generator!, "cliffDetailStrengthMeters", 42f);

                generator.RegenerateInEditor();

                var detailProfile = SampleHeightProfile(terrain!, flankStart, flankEnd, 64);
                var detailRoughness = CalculateAverageSecondDifference(detailProfile);

                Assert.That(detailRoughness, Is.GreaterThan(baselineRoughness + 1.2f), $"Expected detail pass to increase local mountain roughness, but roughness only moved from {baselineRoughness:F2} to {detailRoughness:F2}.");
            }
            finally
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                var terrain = worldShell != null
                    ? FindChild(worldShell.transform, "MainTownTerrain")?.GetComponent<Terrain>()
                    : null;
                if (worldShell != null && terrain != null)
                {
                    MainTownTerrainBootstrap.ApplyLoadedMainTownIslandPass(worldShell.transform, terrain);
                }

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

        private static float CalculateProfileRange(float[] values)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            foreach (var value in values)
            {
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }

            return max - min;
        }

        private static float CalculateLandCoverage(Terrain terrain, float landHeightThreshold, int sampleResolution)
        {
            var landSamples = 0;
            var totalSamples = 0;

            for (var z = 0; z < sampleResolution; z++)
            {
                var normalizedZ = z / (float)(sampleResolution - 1);
                for (var x = 0; x < sampleResolution; x++)
                {
                    var normalizedX = x / (float)(sampleResolution - 1);
                    if (terrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ) > landHeightThreshold)
                    {
                        landSamples++;
                    }

                    totalSamples++;
                }
            }

            return landSamples / (float)totalSamples;
        }

        private static Vector3 FindPeakWorldPoint(Terrain terrain)
        {
            var heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
            var maxHeight = float.MinValue;
            var maxX = 0;
            var maxZ = 0;

            for (var z = 0; z < heights.GetLength(0); z++)
            {
                for (var x = 0; x < heights.GetLength(1); x++)
                {
                    var height = heights[z, x] * terrain.terrainData.size.y;
                    if (height <= maxHeight)
                    {
                        continue;
                    }

                    maxHeight = height;
                    maxX = x;
                    maxZ = z;
                }
            }

            var normalizedX = maxX / (float)(terrain.terrainData.heightmapResolution - 1);
            var normalizedZ = maxZ / (float)(terrain.terrainData.heightmapResolution - 1);
            return new Vector3(
                terrain.transform.position.x + normalizedX * terrain.terrainData.size.x,
                maxHeight,
                terrain.transform.position.z + normalizedZ * terrain.terrainData.size.z);
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

        private static void ApplyLayerClassificationFixture(Terrain terrain)
        {
            var resolution = terrain.terrainData.heightmapResolution;
            var heights = new float[resolution, resolution];
            var shorelineHeight = 24f / terrain.terrainData.size.y;
            var grassHeight = 90f / terrain.terrainData.size.y;
            var rockHeight = 320f / terrain.terrainData.size.y;

            for (var z = 0; z < resolution; z++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var normalizedX = x / (float)(resolution - 1);
                    float height;
                    if (normalizedX < 0.28f)
                    {
                        height = shorelineHeight;
                    }
                    else if (normalizedX < 0.64f)
                    {
                        height = grassHeight;
                    }
                    else if (normalizedX < 0.76f)
                    {
                        var t = Mathf.InverseLerp(0.64f, 0.76f, normalizedX);
                        height = Mathf.Lerp(grassHeight, rockHeight, t);
                    }
                    else
                    {
                        height = rockHeight;
                    }

                    heights[z, x] = height;
                }
            }

            terrain.terrainData.SetHeights(0, 0, heights);
        }

        private static int GetDominantTerrainLayer(Terrain terrain, float normalizedX, float normalizedZ)
        {
            var alphamapWidth = terrain.terrainData.alphamapWidth;
            var alphamapHeight = terrain.terrainData.alphamapHeight;
            var x = Mathf.Clamp(Mathf.RoundToInt(normalizedX * (alphamapWidth - 1)), 0, alphamapWidth - 1);
            var z = Mathf.Clamp(Mathf.RoundToInt(normalizedZ * (alphamapHeight - 1)), 0, alphamapHeight - 1);
            var weights = terrain.terrainData.GetAlphamaps(x, z, 1, 1);

            var bestIndex = 0;
            var bestWeight = weights[0, 0, 0];
            for (var index = 1; index < weights.GetLength(2); index++)
            {
                if (weights[0, 0, index] <= bestWeight)
                {
                    continue;
                }

                bestWeight = weights[0, 0, index];
                bestIndex = index;
            }

            return bestIndex;
        }
    }
}
