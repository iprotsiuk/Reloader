using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Tests.EditMode
{
    public class MainTownLayoutEditModeTests
    {
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";

        [Test]
        public void MainTownScene_HasPlanningShellAndApprovedDistrictRoots()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected a dedicated world shell root for the literal-mile rebuild.");

                var basinFloor = FindChild(worldShell!.transform, "BasinFloor");
                Assert.That(basinFloor, Is.Not.Null, "Expected BasinFloor under MainTownWorldShell.");
                Assert.That(basinFloor!.localScale.x, Is.GreaterThanOrEqualTo(1900f), "Basin floor should now read as a roughly 2km planning-map shell in X.");
                Assert.That(basinFloor.localScale.z, Is.GreaterThanOrEqualTo(1900f), "Basin floor should now read as a roughly 2km planning-map shell in Z.");

                AssertChildExists(worldShell.transform, "District_TownCore");
                AssertChildExists(worldShell.transform, "District_PlayerCompound");
                AssertChildExists(worldShell.transform, "District_ChurchHill");
                AssertChildExists(worldShell.transform, "District_QuarryBasin");
                AssertChildExists(worldShell.transform, "District_ForestBelt");
                AssertChildExists(worldShell.transform, "District_UtilityLandmarks");
                AssertChildExists(worldShell.transform, "District_MotelStrip");
                AssertChildExists(worldShell.transform, "District_IndustrialYard");
                AssertChildExists(worldShell.transform, "District_TrailerPark");
                AssertChildExists(worldShell.transform, "District_ServiceDepot");
                AssertChildExists(worldShell.transform, "District_TruckStop");
                AssertChildExists(worldShell.transform, "District_WaterTreatment");
                AssertChildExists(worldShell.transform, "District_StorageYard");
                AssertChildExists(worldShell.transform, "District_MunicipalBlock");
                AssertChildExists(worldShell.transform, "District_FreightYard");
                AssertChildExists(worldShell.transform, "District_RoadsideMarket");
                AssertChildExists(worldShell.transform, "District_RadioTower");

                AssertChildExists(worldShell.transform, "PerimeterLoopRoad");
                AssertChildExists(worldShell.transform, "MainStreetSpine");
                AssertChildExists(worldShell.transform, "Landmark_Church");
                AssertChildExists(worldShell.transform, "Landmark_WaterTower");
                AssertChildExists(worldShell.transform, "Landmark_QuarryTerraces");
                AssertChildExists(worldShell.transform, "Landmark_PoliceStation");
                AssertChildExists(worldShell.transform, "Landmark_Hospital");
                AssertChildExists(worldShell.transform, "Landmark_GunStore");
                AssertChildExists(worldShell.transform, "Landmark_ReloadingSupply");
                AssertChildExists(worldShell.transform, "Landmark_PlayerHouse");
                AssertChildExists(worldShell.transform, "Landmark_Motel");
                Assert.That(FindChild(worldShell.transform, "PlayerHouse"), Is.Not.Null, "Expected an exact PlayerHouse object for round-trip travel coverage.");

                AssertNamedScaleAtLeast(worldShell.transform, "House_Body", minX: 10f, minY: 4.5f, minZ: 8f);
                AssertNamedScaleAtLeast(worldShell.transform, "Workshop_Body", minX: 8f, minY: 4.5f, minZ: 6f);
                AssertNamedScaleAtLeast(worldShell.transform, "SupplyStore_Body", minX: 10f, minY: 5f, minZ: 8f);
                AssertNamedScaleAtLeast(worldShell.transform, "GunStore_Body", minX: 12f, minY: 5f, minZ: 10f);
                AssertNamedScaleAtLeast(worldShell.transform, "PoliceStation_Body", minX: 16f, minY: 5f, minZ: 10f);
                AssertNamedScaleAtLeast(worldShell.transform, "Hospital_Body", minX: 20f, minY: 6f, minZ: 12f);
                AssertNamedScaleAtLeast(worldShell.transform, "Church_Nave", minX: 12f, minY: 6f, minZ: 20f);
                AssertNamedScaleAtLeast(worldShell.transform, "Church_Tower", minX: 4f, minY: 18f, minZ: 4f);
                AssertNamedScaleAtLeast(worldShell.transform, "WaterTower_Stem", minX: 2f, minY: 20f, minZ: 2f);
                AssertNamedScaleAtLeast(worldShell.transform, "WaterTower_Tank", minX: 10f, minY: 6f, minZ: 10f);
                AssertNamedScaleAtLeast(worldShell.transform, "Motel_Block", minX: 30f, minY: 5f, minZ: 8f);
                AssertNamedScaleAtLeast(worldShell.transform, "MotelLot", minX: 60f, minZ: 30f);
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
        public void MainTownScene_UsesPlanningModeWithoutLandscapePresentationLayer()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected a dedicated world shell root for the literal-mile rebuild.");

                var playerCompound = FindChild(worldShell!.transform, "District_PlayerCompound");
                var townCore = FindChild(worldShell.transform, "District_TownCore");
                var churchHill = FindChild(worldShell.transform, "District_ChurchHill");
                var utilityLandmarks = FindChild(worldShell.transform, "District_UtilityLandmarks");
                var quarryBasin = FindChild(worldShell.transform, "District_QuarryBasin");
                var waterTreatment = FindChild(worldShell.transform, "District_WaterTreatment");
                var storageYard = FindChild(worldShell.transform, "District_StorageYard");
                var municipalBlock = FindChild(worldShell.transform, "District_MunicipalBlock");
                var freightYard = FindChild(worldShell.transform, "District_FreightYard");
                var roadsideMarket = FindChild(worldShell.transform, "District_RoadsideMarket");
                var radioTower = FindChild(worldShell.transform, "District_RadioTower");
                var mainStreet = FindChild(worldShell.transform, "MainStreetSpine");

                AssertScaleYBetween(playerCompound, "HouseShell", 4.5f, 7.5f);
                AssertScaleYBetween(playerCompound, "WorkshopWing", 4.5f, 7.5f);

                AssertScaleYBetween(townCore, "GunStoreBlock", 5f, 8f);
                AssertScaleYBetween(townCore, "ReloadingSupplyBlock", 5f, 8f);
                AssertScaleYBetween(townCore, "TownBlock_A", 4.5f, 8f);
                AssertScaleYBetween(townCore, "TownBlock_B", 4.5f, 8f);
                AssertScaleYBetween(townCore, "PoliceBlock", 8f, 14f);
                AssertScaleYBetween(townCore, "HospitalBlock", 10f, 18f);

                AssertScaleYBetween(churchHill, "ChurchBody", 10f, 18f);
                AssertScaleYBetween(churchHill, "ChurchTowerMass", 30f, 60f);
                AssertScaleYBetween(waterTreatment, "PumpHouse", 8f, 14f);
                AssertScaleYBetween(waterTreatment, "ClarifierTank_A", 6f, 10f);
                AssertScaleYBetween(storageYard, "StorageOffice", 8f, 14f);
                AssertScaleYBetween(storageYard, "LockerRow_A", 6f, 12f);
                AssertScaleYBetween(storageYard, "LockerRow_B", 6f, 12f);
                AssertScaleYBetween(municipalBlock, "TownHallBlock", 8f, 14f);
                AssertScaleYBetween(freightYard, "FreightWarehouse", 8f, 14f);
                AssertScaleYBetween(roadsideMarket, "RoadsideDiner", 6f, 12f);
                AssertScaleYBetween(radioTower, "RadioTowerMass", 26f, 70f);

                AssertScaleYBetween(mainStreet, "MainStreet_EastWest", 0.05f, 0.2f);
                AssertScaleZBetween(mainStreet, "MainStreet_EastWest", 14f, 20f);
                AssertScaleXBetween(mainStreet, "MainStreet_NorthSouth", 14f, 20f);

                AssertScaleXBetween(worldShell.transform, "MarkerPad_IndustrialYard", 120f, 180f);
                AssertScaleXBetween(worldShell.transform, "MarkerPad_TrailerPark", 110f, 170f);
                AssertScaleXBetween(worldShell.transform, "MarkerPad_ServiceDepot", 120f, 180f);
                AssertScaleXBetween(worldShell.transform, "MarkerPad_TruckStop", 120f, 180f);
                AssertScaleXBetween(worldShell.transform, "MarkerPad_WaterTreatment", 110f, 170f);
                AssertScaleXBetween(worldShell.transform, "MarkerPad_StorageYard", 130f, 190f);

                var waterTowerTank = FindChild(utilityLandmarks, "WaterTowerTank");
                Assert.That(waterTowerTank, Is.Not.Null, "Expected WaterTowerTank under District_UtilityLandmarks.");
                Assert.That(waterTowerTank!.localPosition.y, Is.GreaterThanOrEqualTo(20f), "Water tower tank should still sit well above street level for landmark visibility after the island scale-up.");

                AssertChildMissing(worldShell.transform, "Water_RiverWest");
                AssertChildMissing(worldShell.transform, "Water_RiverCentral");
                AssertChildMissing(worldShell.transform, "Water_ReservoirNorth");
                AssertChildMissing(worldShell.transform, "Landmark_RiverBridge");
                AssertChildMissing(worldShell.transform, "MountainRim");
                AssertChildMissing(worldShell.transform, "ForestDensityLayer_West");
                AssertChildMissing(worldShell.transform, "ForestDensityLayer_SouthEast");
                AssertChildMissing(worldShell.transform, "ForestDensityLayer_NorthEast");
                AssertChildMissing(worldShell.transform, "ForestGapCluster_West");
                AssertChildMissing(worldShell.transform, "ForestGapCluster_East");
                AssertChildMissing(worldShell.transform, "ForestGapCluster_North");

                Assert.That(TryGetLargestLocalScale(worldShell.transform, "ForestTree_01", out _), Is.False, "Expected planning mode to remove forest tree presentation objects.");
                Assert.That(TryGetLargestLocalScale(worldShell.transform, "Ridge_North", out _), Is.False, "Expected planning mode to remove ridge presentation objects.");
                Assert.That(TryGetLargestLocalScale(worldShell.transform, "PlayerOverlookHill", out _), Is.False, "Expected planning mode to remove hill presentation objects.");
                Assert.That(TryGetLargestLocalScale(worldShell.transform, "WaterTowerHill", out _), Is.False, "Expected planning mode to remove hill presentation objects.");
                Assert.That(TryGetLargestLocalScale(worldShell.transform, "ChurchSlope", out _), Is.False, "Expected planning mode to remove slope presentation objects.");
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
        public void MainTownScene_HasRoadNetworkForPlanningPass()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected a dedicated world shell root for the literal-mile rebuild.");

                var perimeterLoopRoad = FindChild(worldShell!.transform, "PerimeterLoopRoad");
                var roadMainStreet = FindChild(worldShell.transform, "Road_MainStreet");
                var roadNorth = FindChild(worldShell.transform, "Road_North");
                var roadSouth = FindChild(worldShell.transform, "Road_South");
                var roadEast = FindChild(worldShell.transform, "Road_East");
                var roadWest = FindChild(worldShell.transform, "Road_West");
                Assert.That(perimeterLoopRoad, Is.Not.Null, "Expected PerimeterLoopRoad before connector authoring.");
                Assert.That(roadMainStreet, Is.Not.Null, "Expected authored MainStreet road coverage in the planning shell.");
                Assert.That(roadNorth, Is.Not.Null, "Expected northbound road coverage in the planning shell.");
                Assert.That(roadSouth, Is.Not.Null, "Expected southbound road coverage in the planning shell.");
                Assert.That(roadEast, Is.Not.Null, "Expected eastbound road coverage in the planning shell.");
                Assert.That(roadWest, Is.Not.Null, "Expected westbound road coverage in the planning shell.");

                AssertRoadUsesReplacementSurface(worldShell.transform, "MainStreet_EastWest", expectDirt: false);
                AssertRoadUsesReplacementSurface(worldShell.transform, "MainStreet_NorthSouth", expectDirt: false);
                AssertRoadUsesReplacementSurface(worldShell.transform, "Road_MainStreet", expectDirt: false);
                AssertRoadUsesReplacementSurface(worldShell.transform, "Road_North", expectDirt: false);
                AssertRoadUsesReplacementSurface(worldShell.transform, "Road_South", expectDirt: false);
                AssertRoadUsesReplacementSurface(worldShell.transform, "Road_East", expectDirt: false);
                AssertRoadUsesReplacementSurface(worldShell.transform, "Road_West", expectDirt: false);
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
        public void MainTownScene_HasPaintReadyTerrainBootstrap()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                var worldShell = FindRoot(scene, "MainTownWorldShell");
                Assert.That(worldShell, Is.Not.Null, "Expected a dedicated world shell root for the literal-mile rebuild.");

                var terrainRoot = FindChild(worldShell!.transform, "MainTownTerrain");
                Assert.That(terrainRoot, Is.Not.Null, "Expected MainTownTerrain under MainTownWorldShell for Terrain Tools paint workflows.");

                var terrain = terrainRoot!.GetComponent<Terrain>();
                var terrainCollider = terrainRoot.GetComponent<TerrainCollider>();
                Assert.That(terrain, Is.Not.Null, "Expected a Terrain component on MainTownTerrain.");
                Assert.That(terrainCollider, Is.Not.Null, "Expected a TerrainCollider component on MainTownTerrain.");
                Assert.That(terrain!.terrainData, Is.Not.Null, "Expected TerrainData on MainTownTerrain.");
                Assert.That(terrainCollider!.terrainData, Is.SameAs(terrain.terrainData), "Expected TerrainCollider to reference the same TerrainData as Terrain.");

                var terrainData = terrain.terrainData;
                Assert.That(terrainData.size.x, Is.GreaterThanOrEqualTo(2900f), "Expected terrain width to expand by roughly 50% so the island footprint can breathe.");
                Assert.That(terrainData.size.z, Is.GreaterThanOrEqualTo(2900f), "Expected terrain depth to expand by roughly 50% so the island footprint can breathe.");
                Assert.That(terrainData.size.y, Is.GreaterThanOrEqualTo(100f), "Expected enough terrain height range for future sculpting.");
                Assert.That(terrainData.terrainLayers.Length, Is.GreaterThanOrEqualTo(4), "Expected at least four starter terrain layers for grass, dirt, road, and quarry paint.");
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
        public void MainTownScene_KeepsAuthoredContentAboveIslandTerrain()
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

                AssertRootContentClearsTerrain(terrain!, FindChild(worldShell.transform, "Landmark_PlayerHouse"), "Landmark_PlayerHouse");
                AssertRootContentClearsTerrain(terrain, FindChild(worldShell.transform, "GunStoreBlock"), "GunStoreBlock");
                AssertRootContentClearsTerrain(terrain, FindChild(worldShell.transform, "ReloadingSupplyBlock"), "ReloadingSupplyBlock");
                AssertRootContentClearsTerrain(terrain, FindChild(worldShell.transform, "PoliceBlock"), "PoliceBlock");
                AssertRootContentClearsTerrain(terrain, FindChild(worldShell.transform, "HospitalBlock"), "HospitalBlock");
                AssertRootContentClearsTerrain(terrain, FindChild(worldShell.transform, "ChurchBody"), "ChurchBody");
                AssertRootContentClearsTerrain(terrain, FindChild(worldShell.transform, "Landmark_QuarryTerraces"), "Landmark_QuarryTerraces");

                AssertRootContentClearsTerrain(terrain, FindRoot(scene, "PlayerRoot")?.transform, "PlayerRoot");
                AssertRootContentClearsTerrain(terrain, FindRoot(scene, "ReloadingWorkbench")?.transform, "ReloadingWorkbench");
                AssertRootContentClearsTerrain(terrain, FindRoot(scene, "StorageChest")?.transform, "StorageChest");
                AssertRootContentClearsTerrain(terrain, FindRoot(scene, "AmmoVendor")?.transform, "AmmoVendor");
                AssertRootContentClearsTerrain(terrain, FindRoot(scene, "WeaponVendor")?.transform, "WeaponVendor");
                AssertRootContentClearsTerrain(terrain, FindRoot(scene, "ReloadingVendor_House")?.transform, "ReloadingVendor_House");
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

        private static void AssertScaleYBetween(Transform parent, string childName, float min, float max)
        {
            var child = FindChild(parent, childName);
            Assert.That(child, Is.Not.Null, $"Expected child '{childName}' under '{parent?.name}'.");
            Assert.That(child!.localScale.y, Is.InRange(min, max), $"Expected '{childName}' height to be between {min}m and {max}m for believable blockout scale.");
        }

        private static void AssertScaleXBetween(Transform parent, string childName, float min, float max)
        {
            var child = FindChild(parent, childName);
            Assert.That(child, Is.Not.Null, $"Expected child '{childName}' under '{parent?.name}'.");
            Assert.That(child!.localScale.x, Is.InRange(min, max), $"Expected '{childName}' X scale to be between {min}m and {max}m.");
        }

        private static void AssertScaleZBetween(Transform parent, string childName, float min, float max)
        {
            var child = FindChild(parent, childName);
            Assert.That(child, Is.Not.Null, $"Expected child '{childName}' under '{parent?.name}'.");
            Assert.That(child!.localScale.z, Is.InRange(min, max), $"Expected '{childName}' Z scale to be between {min}m and {max}m.");
        }

        private static void AssertThatRendererHeightIsBetween(Transform parent, string childName, float min, float max)
        {
            var child = FindChild(parent, childName);
            Assert.That(child, Is.Not.Null, $"Expected child '{childName}' under '{parent?.name}'.");

            var renderer = child!.GetComponent<Renderer>();
            Assert.That(renderer, Is.Not.Null, $"Expected renderer on '{childName}' so tree height can be evaluated.");
            Assert.That(renderer!.bounds.size.y, Is.InRange(min, max), $"Expected '{childName}' rendered height to be between {min}m and {max}m.");
        }

        private static void AssertChildExists(Transform parent, string childName)
        {
            Assert.That(FindChild(parent, childName), Is.Not.Null, $"Expected child '{childName}' under '{parent.name}'.");
        }

        private static void AssertChildMissing(Transform parent, string childName)
        {
            Assert.That(FindChild(parent, childName), Is.Null, $"Expected child '{childName}' to be removed from '{parent.name}'.");
        }

        private static void AssertRoadUsesReplacementSurface(Transform parent, string childName, bool expectDirt)
        {
            var child = FindChild(parent, childName);
            Assert.That(child, Is.Not.Null, $"Expected road child '{childName}' under '{parent?.name}'.");

            var meshFilter = child!.GetComponent<MeshFilter>();
            Assert.That(meshFilter, Is.Not.Null, $"Expected MeshFilter on road '{childName}'.");
            Assert.That(meshFilter!.sharedMesh, Is.Not.Null, $"Expected shared mesh on road '{childName}'.");
            Assert.That(meshFilter.sharedMesh!.name, Is.Not.EqualTo("Cube"), $"Expected '{childName}' to stop using the cube blockout mesh.");

            var renderer = child.GetComponent<MeshRenderer>();
            Assert.That(renderer, Is.Not.Null, $"Expected MeshRenderer on road '{childName}'.");
            Assert.That(renderer!.sharedMaterial, Is.Not.Null, $"Expected shared material on road '{childName}'.");

            var materialPath = AssetDatabase.GetAssetPath(renderer.sharedMaterial);
            Assert.That(materialPath, Does.Not.EndWith("sptp_Main_Mat.mat"), $"Expected '{childName}' to stop using the atlas road material.");

            if (expectDirt)
            {
                Assert.That(materialPath, Does.Contain("/dirt material.mat"), $"Expected '{childName}' to use the dirt road material.");
            }
            else
            {
                Assert.That(materialPath, Does.Contain("/road material.mat"), $"Expected '{childName}' to use the paved road material.");
            }
        }

        private static void AssertNamedScaleAtLeast(Transform parent, string childName, float minX = 0f, float minY = 0f, float minZ = 0f)
        {
            Assert.That(TryGetLargestLocalScale(parent, childName, out var scale), Is.True, $"Expected at least one '{childName}' blockout object.");

            if (minX > 0f)
            {
                Assert.That(scale.x, Is.GreaterThanOrEqualTo(minX), $"Expected '{childName}' scale.x >= {minX}.");
            }

            if (minY > 0f)
            {
                Assert.That(scale.y, Is.GreaterThanOrEqualTo(minY), $"Expected '{childName}' scale.y >= {minY}.");
            }

            if (minZ > 0f)
            {
                Assert.That(scale.z, Is.GreaterThanOrEqualTo(minZ), $"Expected '{childName}' scale.z >= {minZ}.");
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

        private static void AssertRootContentClearsTerrain(Terrain terrain, Transform root, string rootName)
        {
            Assert.That(root, Is.Not.Null, $"Expected '{rootName}' root to exist.");

            var boundsFound = TryGetAuthoredContentBounds(root!, out var bounds);
            Assert.That(boundsFound, Is.True, $"Expected '{rootName}' to provide renderer/collider bounds for terrain clearance validation.");

            var terrainHeight = SampleTerrainHeight(terrain, bounds.center);
            Assert.That(
                bounds.min.y,
                Is.GreaterThanOrEqualTo(terrainHeight - 2.5f),
                $"Expected '{rootName}' content to sit on or above the island terrain.");
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
    }
}
