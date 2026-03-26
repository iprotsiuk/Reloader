using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Tests.EditMode
{
    public class MainTownLayoutEditModeTests
    {
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";

        [Test]
        public void MainTownScene_HasTerrainShellAndRuntimeAnchors()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected a dedicated world shell root for the authored island terrain.");

                var terrainGeneratorType = Type.GetType("Reloader.World.MainTownTerrainGenerator, Reloader.World");
                Assert.That(terrainGeneratorType, Is.Not.Null, "Expected MainTownTerrainGenerator type in the world runtime assembly.");
                Assert.That(worldShell!.GetComponent(terrainGeneratorType!), Is.Not.Null, "Expected MainTownWorldShell to host the authored terrain generator.");

                var basinFloor = FindChild(worldShell!.transform, "BasinFloor");
                Assert.That(basinFloor, Is.Not.Null, "Expected BasinFloor under MainTownWorldShell.");
                Assert.That(basinFloor!.gameObject.activeSelf, Is.False, "Expected the basin floor to stay hidden in the island scene.");

                Assert.That(FindChild(worldShell.transform, "MainTownTerrain"), Is.Not.Null, "Expected MainTownTerrain under MainTownWorldShell.");
                Assert.That(FindChild(worldShell.transform, "Water_OceanHorizon"), Is.Not.Null, "Expected horizon ocean presentation in the island scene.");

                var oceanBoundary = FindChild(worldShell.transform, "Water_OceanBoundary");
                Assert.That(oceanBoundary, Is.Not.Null, "Expected invisible ocean blockers in the island scene.");
                Assert.That(oceanBoundary!.GetComponentsInChildren<BoxCollider>(true).Length, Is.GreaterThan(0), "Expected the ocean boundary to carry blocker colliders.");

                AssertSceneRootExists(scene, "MainTownContractRuntime");
                AssertSceneRootExists(scene, "MainTownPopulationRuntime");
                AssertSceneRootExists(scene, "MainTownEntry_Spawn");
                AssertSceneRootExists(scene, "MainTownEntry_Return");
                AssertSceneRootExists(scene, "MainTown_SmokeToIndoor_Trigger");
                AssertSceneRootExists(scene, "ReloadingWorkbench");
                AssertSceneRootExists(scene, "StorageChest");
                AssertSceneRootExists(scene, "WeaponRegistry");
                AssertSceneRootExists(scene, "CoreWorldController");

                var populationRuntime = FindRoot(scene, "MainTownPopulationRuntime");
                Assert.That(populationRuntime, Is.Not.Null, "Expected MainTownPopulationRuntime root to remain authored.");
                AssertChildExists(populationRuntime!.transform, "Anchor_Townsfolk_01");
                AssertChildExists(populationRuntime.transform, "Anchor_QuarryWorker_01");
                AssertChildExists(populationRuntime.transform, "Anchor_Hobo_01");
                AssertChildExists(populationRuntime.transform, "Anchor_Cop_01");
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
        public void MainTownScene_UsesIslandTerrainWithoutLegacyLandscapePresentationLayer()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected a dedicated world shell root for the authored island terrain.");

                AssertChildMissing(worldShell!.transform, "District_TownCore");
                AssertChildMissing(worldShell.transform, "District_PlayerCompound");
                AssertChildMissing(worldShell.transform, "District_ChurchHill");
                AssertChildMissing(worldShell.transform, "District_QuarryBasin");
                AssertChildMissing(worldShell.transform, "District_ForestBelt");
                AssertChildMissing(worldShell.transform, "District_UtilityLandmarks");
                AssertChildMissing(worldShell.transform, "District_MotelStrip");
                AssertChildMissing(worldShell.transform, "District_IndustrialYard");
                AssertChildMissing(worldShell.transform, "District_TrailerPark");
                AssertChildMissing(worldShell.transform, "District_ServiceDepot");
                AssertChildMissing(worldShell.transform, "District_TruckStop");
                AssertChildMissing(worldShell.transform, "District_WaterTreatment");
                AssertChildMissing(worldShell.transform, "District_StorageYard");
                AssertChildMissing(worldShell.transform, "District_MunicipalBlock");
                AssertChildMissing(worldShell.transform, "District_FreightYard");
                AssertChildMissing(worldShell.transform, "District_RoadsideMarket");
                AssertChildMissing(worldShell.transform, "District_RadioTower");
                AssertChildMissing(worldShell.transform, "PerimeterLoopRoad");
                AssertChildMissing(worldShell.transform, "MainStreetSpine");
                AssertChildMissing(worldShell.transform, "Road_MainStreet");
                AssertChildMissing(worldShell.transform, "Road_North");
                AssertChildMissing(worldShell.transform, "Road_South");
                AssertChildMissing(worldShell.transform, "Road_East");
                AssertChildMissing(worldShell.transform, "Road_West");
                AssertChildMissing(worldShell.transform, "Landmark_PlayerHouse");
                AssertChildMissing(worldShell.transform, "Landmark_Church");
                AssertChildMissing(worldShell.transform, "Landmark_QuarryTerraces");
                AssertChildMissing(worldShell.transform, "Landmark_PoliceStation");
                AssertChildMissing(worldShell.transform, "Landmark_Hospital");
                AssertChildMissing(worldShell.transform, "Landmark_GunStore");
                AssertChildMissing(worldShell.transform, "Landmark_ReloadingSupply");
                AssertChildMissing(worldShell.transform, "Landmark_Motel");
                AssertChildMissing(worldShell.transform, "MountainRim");
                Assert.That(TryGetLargestLocalScale(worldShell.transform, "ForestTree_01", out _), Is.False, "Expected the island scene to leave legacy forest-tree presentation out of the saved shell.");
                Assert.That(TryGetLargestLocalScale(worldShell.transform, "ForestDensityLayer_West", out _), Is.False, "Expected the island scene to leave legacy forest density layers out of the saved shell.");
                Assert.That(TryGetLargestLocalScale(worldShell.transform, "ForestGapCluster_West", out _), Is.False, "Expected the island scene to leave legacy forest gap clusters out of the saved shell.");
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
        public void MainTownScene_LeavesLegacyRoadBlockoutsOutOfTheIslandScene()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected a dedicated world shell root for the authored island terrain.");

                AssertChildMissing(worldShell!.transform, "PerimeterLoopRoad");
                AssertChildMissing(worldShell.transform, "MainStreetSpine");
                AssertChildMissing(worldShell.transform, "Road_MainStreet");
                AssertChildMissing(worldShell.transform, "Road_North");
                AssertChildMissing(worldShell.transform, "Road_South");
                AssertChildMissing(worldShell.transform, "Road_East");
                AssertChildMissing(worldShell.transform, "Road_West");
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
        public void MainTownScene_HasThreeLayerIslandTerrainBootstrap()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected a dedicated world shell root for the literal-mile rebuild.");

                var terrainGeneratorType = Type.GetType("Reloader.World.MainTownTerrainGenerator, Reloader.World");
                Assert.That(terrainGeneratorType, Is.Not.Null, "Expected MainTownTerrainGenerator type in the world runtime assembly.");
                Assert.That(worldShell!.GetComponent(terrainGeneratorType!), Is.Not.Null, "Expected MainTownWorldShell to keep the authored terrain generator component.");

                var terrainRoot = FindChild(worldShell!.transform, "MainTownTerrain");
                Assert.That(terrainRoot, Is.Not.Null, "Expected MainTownTerrain under MainTownWorldShell.");

                var terrain = terrainRoot!.GetComponent<Terrain>();
                var terrainCollider = terrainRoot.GetComponent<TerrainCollider>();
                Assert.That(terrain, Is.Not.Null, "Expected a Terrain component on MainTownTerrain.");
                Assert.That(terrainCollider, Is.Not.Null, "Expected a TerrainCollider component on MainTownTerrain.");
                Assert.That(terrain!.terrainData, Is.Not.Null, "Expected TerrainData on MainTownTerrain.");
                Assert.That(terrainCollider!.terrainData, Is.SameAs(terrain.terrainData), "Expected TerrainCollider to reference the same TerrainData as Terrain.");

                var terrainData = terrain.terrainData;
                Assert.That(terrainData.size.x, Is.EqualTo(4333f).Within(0.1f), "Expected the island terrain width to match the authored MainTown terrain footprint.");
                Assert.That(terrainData.size.z, Is.EqualTo(5111f).Within(0.1f), "Expected the island terrain depth to match the authored MainTown terrain footprint.");
                Assert.That(terrainData.size.y, Is.EqualTo(1100f).Within(0.1f), "Expected the island terrain height budget to match the authored generator settings.");
                Assert.That(terrainData.terrainLayers.Length, Is.EqualTo(3), "Expected the authored island bootstrap to keep the sand, grass, and stone terrain layers.");

                var terrainLayerNames = new string[terrainData.terrainLayers.Length];
                for (var index = 0; index < terrainData.terrainLayers.Length; index++)
                {
                    terrainLayerNames[index] = terrainData.terrainLayers[index].name;
                }

                CollectionAssert.AreEqual(new[] { "MainTown_Sand", "MainTown_Grass", "MainTown_Stone" }, terrainLayerNames);
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
        public void MainTownScene_UsesLinearFogAtOnePointSixKilometers()
        {
            var originalScene = SceneManager.GetActiveScene();
            var originalFog = RenderSettings.fog;
            var originalFogMode = RenderSettings.fogMode;
            var originalFogStart = RenderSettings.fogStartDistance;
            var originalFogEnd = RenderSettings.fogEndDistance;
            var originalFogColor = RenderSettings.fogColor;
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(scene);

                Assert.That(RenderSettings.fog, Is.True, "Expected MainTown to enable scene fog for the island horizon.");
                Assert.That(RenderSettings.fogMode, Is.EqualTo(FogMode.Linear), "Expected MainTown to use linear distance fog.");
                Assert.That(RenderSettings.fogEndDistance, Is.EqualTo(1600f).Within(1f), "Expected MainTown fog to fade to the horizon at about 1.6km.");
                Assert.That(RenderSettings.fogStartDistance, Is.LessThan(RenderSettings.fogEndDistance), "Expected fog start distance to remain below the fog end distance.");
            }
            finally
            {
                RenderSettings.fog = originalFog;
                RenderSettings.fogMode = originalFogMode;
                RenderSettings.fogStartDistance = originalFogStart;
                RenderSettings.fogEndDistance = originalFogEnd;
                RenderSettings.fogColor = originalFogColor;

                EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        [Test]
        public void MainTownScene_PersistsIslandTerrainPass()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected MainTownWorldShell to exist for terrain sampling.");

                var terrainRoot = FindChild(worldShell!.transform, "MainTownTerrain");
                Assert.That(terrainRoot, Is.Not.Null, "Expected MainTownTerrain for dramatic terrain validation.");

                var terrain = terrainRoot!.GetComponent<Terrain>();
                Assert.That(terrain, Is.Not.Null, "Expected Terrain component on MainTownTerrain.");

                var terrainData = terrain!.terrainData;
                Assert.That(terrainData.size.y, Is.GreaterThanOrEqualTo(500f), "Expected the saved MainTown scene to reserve the agreed 500m vertical budget.");
                Assert.That(GetTerrainHeightRange(terrain), Is.GreaterThanOrEqualTo(18f), "Expected the saved MainTown scene to persist the island relief instead of a flat planning shell.");
                Assert.That(terrainData.treeInstances.Length, Is.EqualTo(0), "Expected the saved MainTown scene to defer forest population to the dramatic terrain authoring tool.");

                var interiorHeight = AverageTerrainHeight(
                    terrain,
                    new Vector3(0f, 0f, 0f),
                    new Vector3(-250f, 0f, 120f),
                    new Vector3(220f, 0f, -180f));
                var edgeHeight = AverageTerrainHeight(terrain, GetPerimeterSamplePoints(terrain));

                Assert.That(interiorHeight, Is.GreaterThanOrEqualTo(35f), "Expected the island interior to remain comfortably above sea level.");
                Assert.That(edgeHeight, Is.LessThan(interiorHeight - 8f), "Expected the saved scene perimeter to taper toward the ocean.");

                var oceanRoot = FindChild(worldShell.transform, "Water_OceanHorizon");
                Assert.That(oceanRoot, Is.Not.Null, "Expected the saved MainTown scene to include horizon ocean presentation.");
                Assert.That(oceanRoot!.gameObject.activeSelf, Is.True, "Expected the horizon ocean root to be active.");
                Assert.That(oceanRoot.GetComponentsInChildren<Collider>(true), Is.Empty, "Expected ocean presentation to avoid gameplay colliders.");

                var oceanSurface = FindChild(oceanRoot, "OceanSurface");
                Assert.That(oceanSurface, Is.Not.Null, "Expected the saved MainTown scene to include a horizon ocean surface.");
                var oceanRenderer = oceanSurface!.GetComponent<Renderer>();
                Assert.That(oceanRenderer, Is.Not.Null, "Expected OceanSurface renderer.");
                AssertOceanMaterialIsOpaque(oceanRenderer!);

                var oceanBoundary = FindChild(worldShell.transform, "Water_OceanBoundary");
                Assert.That(oceanBoundary, Is.Not.Null, "Expected the saved MainTown scene to include invisible ocean blockers.");
                Assert.That(oceanBoundary!.GetComponentsInChildren<BoxCollider>(true).Length, Is.GreaterThanOrEqualTo(8), "Expected enough blocker segments to keep the player out of the ocean.");

                var basinFloor = FindChild(worldShell.transform, "BasinFloor");
                Assert.That(basinFloor, Is.Not.Null, "Expected BasinFloor to remain authored in the planning-shell scene state.");
                Assert.That(basinFloor!.gameObject.activeSelf, Is.False, "Expected the island pass to hide BasinFloor in the saved scene state.");
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
        public void MainTownScene_KeepsRuntimeRootsAboveIslandTerrain()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected MainTownWorldShell to exist for terrain sampling.");

                var terrainRoot = FindChild(worldShell!.transform, "MainTownTerrain");
                Assert.That(terrainRoot, Is.Not.Null, "Expected MainTownTerrain for island terrain validation.");

                var terrain = terrainRoot!.GetComponent<Terrain>();
                Assert.That(terrain, Is.Not.Null, "Expected Terrain component on MainTownTerrain.");

                AssertRootContentClearsTerrain(terrain, FindRoot(scene, "ReloadingWorkbench")?.transform, "ReloadingWorkbench", maxFloatAboveTerrain: 1f);
                AssertRootContentClearsTerrain(terrain, FindRoot(scene, "StorageChest")?.transform, "StorageChest", maxFloatAboveTerrain: 1f);
                AssertRootContentClearsTerrain(terrain, FindRoot(scene, "WeaponVendor")?.transform, "WeaponVendor", maxFloatAboveTerrain: 1f);
                AssertRootContentClearsTerrain(terrain, FindRoot(scene, "AmmoVendor")?.transform, "AmmoVendor", maxFloatAboveTerrain: 1f);
                AssertRootContentClearsTerrain(terrain, FindRoot(scene, "ReloadingVendor_House")?.transform, "ReloadingVendor_House", maxFloatAboveTerrain: 1f);

                var entrySpawn = FindRoot(scene, "MainTownEntry_Spawn");
                Assert.That(entrySpawn, Is.Not.Null, "Expected MainTownEntry_Spawn to remain authored.");
                Assert.That(
                    entrySpawn!.transform.position.y,
                    Is.GreaterThanOrEqualTo(SampleTerrainHeight(terrain, entrySpawn.transform.position) - 1f),
                    "Expected MainTownEntry_Spawn to sit on or above the island terrain.");

                var entryReturn = FindRoot(scene, "MainTownEntry_Return");
                Assert.That(entryReturn, Is.Not.Null, "Expected MainTownEntry_Return to remain authored.");
                Assert.That(
                    entryReturn!.transform.position.y,
                    Is.GreaterThanOrEqualTo(SampleTerrainHeight(terrain, entryReturn.transform.position) - 1f),
                    "Expected MainTownEntry_Return to sit on or above the island terrain.");

                AssertSceneRootExists(scene, "MainTown_SmokeToIndoor_Trigger");
                AssertSceneRootExists(scene, "MainTownContractRuntime");
                AssertSceneRootExists(scene, "MainTownPopulationRuntime");
                AssertSceneRootExists(scene, "WeaponRegistry");

                var populationRuntime = FindRoot(scene, "MainTownPopulationRuntime");
                Assert.That(populationRuntime, Is.Not.Null, "Expected MainTownPopulationRuntime root to remain authored.");
                AssertChildExists(populationRuntime!.transform, "Anchor_Townsfolk_01");
                AssertChildExists(populationRuntime.transform, "Anchor_QuarryWorker_01");
                AssertChildExists(populationRuntime.transform, "Anchor_Hobo_01");
                AssertChildExists(populationRuntime.transform, "Anchor_Cop_01");
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
        public void MainTownScene_DoesNotUseNegativeScaleBoxColliders()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var collider in root.GetComponentsInChildren<BoxCollider>(true))
                    {
                        if (collider == null)
                        {
                            continue;
                        }

                        var lossyScale = collider.transform.lossyScale;
                        Assert.That(lossyScale.x, Is.GreaterThanOrEqualTo(0f), $"Expected non-negative X scale for BoxCollider at '{GetHierarchyPath(collider.transform)}'.");
                        Assert.That(lossyScale.y, Is.GreaterThanOrEqualTo(0f), $"Expected non-negative Y scale for BoxCollider at '{GetHierarchyPath(collider.transform)}'.");
                        Assert.That(lossyScale.z, Is.GreaterThanOrEqualTo(0f), $"Expected non-negative Z scale for BoxCollider at '{GetHierarchyPath(collider.transform)}'.");
                    }
                }
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
        public void MainTownScene_HasAuthoredStartupSupportForSpawnAndReturnSeam()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var supportRoot = FindRoot(scene, "MainTownEntry_StartupSupport");
                Assert.That(supportRoot, Is.Not.Null, "Expected an authored collider pad to support MainTown startup seams.");

                var supportCollider = supportRoot!.GetComponent<BoxCollider>();
                Assert.That(supportCollider, Is.Not.Null, "Expected MainTown startup support to use a BoxCollider.");
                Assert.That(supportCollider!.enabled, Is.True, "Expected MainTown startup support collider to stay enabled.");

                var supportBounds = supportCollider.bounds;
                var spawnEntry = FindRoot(scene, "MainTownEntry_Spawn");
                var returnEntry = FindRoot(scene, "MainTownEntry_Return");
                Assert.That(spawnEntry, Is.Not.Null, "Expected MainTownEntry_Spawn to remain authored.");
                Assert.That(returnEntry, Is.Not.Null, "Expected MainTownEntry_Return to remain authored.");

                Assert.That(
                    supportBounds.Contains(spawnEntry!.transform.position),
                    Is.True,
                    $"Expected startup support bounds to contain MainTownEntry_Spawn. Bounds={supportBounds}, Entry={spawnEntry.transform.position}.");
                Assert.That(
                    supportBounds.Contains(returnEntry!.transform.position),
                    Is.True,
                    $"Expected startup support bounds to contain MainTownEntry_Return. Bounds={supportBounds}, Entry={returnEntry.transform.position}.");
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

        private static Transform AssertChildExists(Transform parent, string childName)
        {
            var child = FindChild(parent, childName);
            Assert.That(child, Is.Not.Null, $"Expected child '{childName}' under '{parent.name}'.");
            return child;
        }

        private static void AssertChildMissing(Transform parent, string childName)
        {
            Assert.That(FindChild(parent, childName), Is.Null, $"Expected child '{childName}' to be removed from '{parent.name}'.");
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

        private static void AssertSceneRootExists(Scene scene, string rootName)
        {
            Assert.That(FindRoot(scene, rootName), Is.Not.Null, $"Expected scene root '{rootName}'.");
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

        private static bool TryGetLargestLocalScale(Transform parent, string childName, out Vector3 largestScale)
        {
            largestScale = Vector3.zero;
            var found = false;
            var largestVolume = -1f;

            if (parent == null)
            {
                return false;
            }

            foreach (var child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != childName)
                {
                    continue;
                }

                var localScale = child.localScale;
                var volume = localScale.x * localScale.y * localScale.z;
                if (found && volume <= largestVolume)
                {
                    continue;
                }

                largestScale = localScale;
                largestVolume = volume;
                found = true;
            }

            return found;
        }

        private static float SampleTerrainHeight(Terrain terrain, Vector3 worldPoint)
        {
            var terrainPosition = terrain.transform.position;
            var terrainSize = terrain.terrainData.size;
            var normalizedX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, worldPoint.x);
            var normalizedZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, worldPoint.z);
            return terrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
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

        private static void AssertRootContentClearsTerrain(Terrain terrain, Transform root, string rootName, float maxFloatAboveTerrain = float.PositiveInfinity)
        {
            Assert.That(root, Is.Not.Null, $"Expected '{rootName}' root to exist.");

            var boundsFound = TryGetAuthoredContentBounds(root!, out var bounds);
            Assert.That(boundsFound, Is.True, $"Expected '{rootName}' to provide renderer/collider bounds for terrain clearance validation.");

            var terrainHeight = SampleTerrainHeight(terrain, bounds.center);
            Assert.That(
                bounds.min.y,
                Is.GreaterThanOrEqualTo(terrainHeight - 2.5f),
                $"Expected '{rootName}' content to sit on or above the island terrain. BoundsMinY={bounds.min.y}, TerrainHeight={terrainHeight}, Center={bounds.center}.");
            Assert.That(
                bounds.min.y,
                Is.LessThanOrEqualTo(terrainHeight + maxFloatAboveTerrain),
                $"Expected '{rootName}' content to stay grounded instead of floating above the island terrain. BoundsMinY={bounds.min.y}, TerrainHeight={terrainHeight}, Center={bounds.center}, MaxFloatAboveTerrain={maxFloatAboveTerrain}.");
        }


        private static bool TryGetAuthoredContentBounds(Transform root, out Bounds combinedBounds)
        {
            combinedBounds = default;
            var found = false;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!found)
                {
                    combinedBounds = renderer.bounds;
                    found = true;
                    continue;
                }

                combinedBounds.Encapsulate(renderer.bounds);
            }

            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (!found)
                {
                    combinedBounds = collider.bounds;
                    found = true;
                    continue;
                }

                combinedBounds.Encapsulate(collider.bounds);
            }

            foreach (var controller in root.GetComponentsInChildren<CharacterController>(true))
            {
                if (!found)
                {
                    combinedBounds = controller.bounds;
                    found = true;
                    continue;
                }

                combinedBounds.Encapsulate(controller.bounds);
            }

            return found;
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

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            var path = transform.name;
            var current = transform.parent;
            while (current != null)
            {
                path = $"{current.name}/{path}";
                current = current.parent;
            }

            return path;
        }
    }
}
