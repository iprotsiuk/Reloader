using System;
using UnityEngine;

namespace Reloader.World
{
    [ExecuteAlways]
    public sealed class MainTownTerrainGenerator : MonoBehaviour
    {
#if UNITY_EDITOR
        private const string TerrainRootName = "MainTownTerrain";
        private const string TerrainFolderPath = "Assets/_Project/World/Terrain/MainTown";
        private const string TerrainDataPath = TerrainFolderPath + "/MainTownTerrainData.asset";
        private const string OceanRootName = "Water_OceanHorizon";
        private const string OceanSurfaceName = "OceanSurface";
        private const string OceanMaterialPath = TerrainFolderPath + "/MainTown_Ocean.mat";
        private const string SandTexturePath = "Assets/ThirdParty/Polygon-Mega Survival Forest/Textures/TerrainTextures/Sand.PNG";
        private const string GrassTexturePath = "Assets/ThirdParty/Polygon-Mega Survival Forest/Textures/TerrainTextures/Grass.PNG";
        private const string RockTexturePath = "Assets/ThirdParty/Polygon-Mega Survival Forest/Textures/TerrainTextures/Stones.PNG";

        [SerializeField] private int seed = 1337;
        [SerializeField] private bool rerollSeedOnRegenerate = true;
        [SerializeField] private float waterLevelMeters = 20f;
        [SerializeField] private float terrainWidthMeters = 3000f;
        [SerializeField] private float terrainDepthMeters = 4000f;
        [SerializeField] private float terrainHeightMeters = 1100f;
        [SerializeField] private int heightmapResolution = 1025;
        [SerializeField] private int alphamapResolution = 1024;
        [SerializeField] private int baseMapResolution = 1024;
        [SerializeField] private int detailResolution = 1024;
        [SerializeField] private int detailResolutionPerPatch = 16;
        [SerializeField] private float landFootprintMeters = 2350f;
        [SerializeField] private float shorelineFalloffMeters = 340f;
        [SerializeField] [Range(0.4f, 1f)] private float landCoverage = 0.82f;
        [SerializeField] [Range(0f, 1f)] private float coastlineBreakupStrength = 0.45f;
        [SerializeField] [Range(0f, 1f)] private float satelliteChance = 0.35f;
        [SerializeField] [Range(0.1f, 0.6f)] private float satelliteSizeRatio = 0.28f;
        [SerializeField] private float hillsAmplitudeMeters = 90f;
        [SerializeField] private float mountainAmplitudeMeters = 430f;
        [SerializeField] private float cliffSharpening = 1.45f;
        [SerializeField] [Range(0, 3)] private int riverCount = 1;
        [SerializeField] private float riverHalfWidthMeters = 95f;
        [SerializeField] private float riverDepthMeters = 115f;
        [SerializeField] private float riverMeanderMeters = 220f;
        [SerializeField] private float beachBlendMeters = 10f;
        [SerializeField] private float detailAmplitudeMeters = 18f;
        [SerializeField] private float detailNoiseFrequency = 0.0032f;
        [SerializeField] private float pitAmplitudeMeters = 10f;
        [SerializeField] private float pitNoiseFrequency = 0.0044f;
        [SerializeField] private float cliffDetailStrengthMeters = 34f;
        [SerializeField] private float cliffDetailFrequency = 0.0038f;
        [SerializeField] private bool autoRepaintLayersOnHeightChange = true;
        [SerializeField] private float sandBandOffsetMeters = 4f;
        [SerializeField] private float sandBandWidthMeters = 16f;
        [SerializeField] private float sandMaxSlopeDegrees = 26f;
        [SerializeField] private float rockSlopeStartDegrees = 34f;
        [SerializeField] private float rockSlopeFullDegrees = 55f;
        [SerializeField] private float rockHeightStartMeters = 125f;
        [SerializeField] private float rockHeightFullMeters = 240f;

        private bool isRepaintingLayers;

        private readonly struct IslandLayout
        {
            public readonly Vector2 MainCenter;
            public readonly Vector2 MainRadii;
            public readonly float MainAngleRadians;
            public readonly Vector2 PeakCenter;
            public readonly float RidgeAngleRadians;
            public readonly bool HasSatellite;
            public readonly Vector2 SatelliteCenter;
            public readonly Vector2 SatelliteRadii;
            public readonly float SatelliteAngleRadians;
            public readonly RiverLayout[] Rivers;

            public IslandLayout(
                Vector2 mainCenter,
                Vector2 mainRadii,
                float mainAngleRadians,
                Vector2 peakCenter,
                float ridgeAngleRadians,
                bool hasSatellite,
                Vector2 satelliteCenter,
                Vector2 satelliteRadii,
                float satelliteAngleRadians,
                RiverLayout[] rivers)
            {
                MainCenter = mainCenter;
                MainRadii = mainRadii;
                MainAngleRadians = mainAngleRadians;
                PeakCenter = peakCenter;
                RidgeAngleRadians = ridgeAngleRadians;
                HasSatellite = hasSatellite;
                SatelliteCenter = satelliteCenter;
                SatelliteRadii = satelliteRadii;
                SatelliteAngleRadians = satelliteAngleRadians;
                Rivers = rivers;
            }
        }

        private readonly struct RiverLayout
        {
            public readonly Vector2 Start;
            public readonly Vector2 Mid;
            public readonly Vector2 End;
            public readonly float WidthMeters;
            public readonly float DepthMeters;

            public RiverLayout(Vector2 start, Vector2 mid, Vector2 end, float widthMeters, float depthMeters)
            {
                Start = start;
                Mid = mid;
                End = end;
                WidthMeters = widthMeters;
                DepthMeters = depthMeters;
            }
        }

        [ContextMenu("Regenerate Terrain")]
        private void RegenerateTerrainContextMenu()
        {
            RegenerateInEditor();
        }

        [ContextMenu("Repaint Terrain Layers")]
        private void RepaintTerrainLayersContextMenu()
        {
            RepaintTerrainLayersInEditor();
        }

        private void OnEnable()
        {
            TerrainCallbacks.heightmapChanged += HandleTerrainHeightmapChanged;
        }

        private void OnDisable()
        {
            TerrainCallbacks.heightmapChanged -= HandleTerrainHeightmapChanged;
        }

        public void RegenerateInEditor()
        {
            if (rerollSeedOnRegenerate)
            {
                seed = unchecked(seed * 1664525 + 1013904223);
            }

            EnsureFolderPath(TerrainFolderPath);

            var terrainData = LoadOrCreateTerrainData();
            ConfigureTerrainData(terrainData);
            terrainData.terrainLayers = EnsureTerrainLayers();

            var terrainObject = LoadOrCreateTerrainObject(terrainData);
            var terrain = terrainObject.GetComponent<Terrain>();
            var terrainCollider = terrainObject.GetComponent<TerrainCollider>();

            terrain.terrainData = terrainData;
            terrain.drawInstanced = true;
            terrain.basemapDistance = 2000f;
            terrainCollider.terrainData = terrainData;

            SculptTerrain(terrain);
            PaintTerrainLayers(terrain);
            EnsureOceanSurface();

            UnityEditor.EditorUtility.SetDirty(terrainData);
            UnityEditor.EditorUtility.SetDirty(terrain);
            UnityEditor.EditorUtility.SetDirty(terrainCollider);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        public void RepaintTerrainLayersInEditor()
        {
            if (!TryGetOwnedTerrain(out var terrain))
            {
                return;
            }

            PaintTerrainLayers(terrain);
            UnityEditor.EditorUtility.SetDirty(terrain.terrainData);
            UnityEditor.EditorUtility.SetDirty(terrain);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        private TerrainData LoadOrCreateTerrainData()
        {
            var terrainData = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (terrainData != null)
            {
                return terrainData;
            }

            terrainData = new TerrainData();
            UnityEditor.AssetDatabase.CreateAsset(terrainData, TerrainDataPath);
            return terrainData;
        }

        private void ConfigureTerrainData(TerrainData terrainData)
        {
            terrainData.heightmapResolution = heightmapResolution;
            terrainData.alphamapResolution = alphamapResolution;
            terrainData.baseMapResolution = baseMapResolution;
            terrainData.SetDetailResolution(detailResolution, detailResolutionPerPatch);
            terrainData.size = new Vector3(terrainWidthMeters, terrainHeightMeters, terrainDepthMeters);
        }

        private TerrainLayer[] EnsureTerrainLayers()
        {
            return new[]
            {
                EnsureTerrainLayer("MainTown_Sand", SandTexturePath, null, new Vector2(24f, 24f)),
                EnsureTerrainLayer("MainTown_Grass", GrassTexturePath, null, new Vector2(30f, 30f)),
                EnsureTerrainLayer("MainTown_Stone", RockTexturePath, null, new Vector2(22f, 22f)),
            };
        }

        private static TerrainLayer EnsureTerrainLayer(string assetName, string diffuseTexturePath, string normalTexturePath, Vector2 tileSize)
        {
            var assetPath = $"{TerrainFolderPath}/{assetName}.terrainlayer";
            var terrainLayer = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainLayer>(assetPath);
            if (terrainLayer == null)
            {
                terrainLayer = new TerrainLayer();
                UnityEditor.AssetDatabase.CreateAsset(terrainLayer, assetPath);
            }

            terrainLayer.name = assetName;
            terrainLayer.diffuseTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(diffuseTexturePath);
            terrainLayer.normalMapTexture = string.IsNullOrWhiteSpace(normalTexturePath)
                ? null
                : UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(normalTexturePath);
            terrainLayer.maskMapTexture = null;
            terrainLayer.tileSize = tileSize;
            terrainLayer.tileOffset = Vector2.zero;
            terrainLayer.metallic = 0f;
            terrainLayer.smoothness = 0.05f;
            UnityEditor.EditorUtility.SetDirty(terrainLayer);
            return terrainLayer;
        }

        private GameObject LoadOrCreateTerrainObject(TerrainData terrainData)
        {
            var terrainTransform = transform.GetComponentsInChildren<Transform>(true);
            foreach (var child in terrainTransform)
            {
                if (child.name == TerrainRootName)
                {
                    var existing = child.gameObject;
                    existing.transform.SetParent(transform, false);
                    existing.transform.localPosition = new Vector3(-terrainWidthMeters * 0.5f, 0f, -terrainDepthMeters * 0.5f);
                    existing.transform.localRotation = Quaternion.identity;
                    existing.transform.localScale = Vector3.one;
                    if (existing.GetComponent<TerrainCollider>() == null)
                    {
                        existing.AddComponent<TerrainCollider>();
                    }

                    return existing;
                }
            }

            var terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = TerrainRootName;
            terrainObject.transform.SetParent(transform, false);
            terrainObject.transform.localPosition = new Vector3(-terrainWidthMeters * 0.5f, 0f, -terrainDepthMeters * 0.5f);
            terrainObject.transform.localRotation = Quaternion.identity;
            terrainObject.transform.localScale = Vector3.one;
            return terrainObject;
        }

        private void SculptTerrain(Terrain terrain)
        {
            var terrainData = terrain.terrainData;
            var resolution = terrainData.heightmapResolution;
            var heights = new float[resolution, resolution];
            var layout = BuildLayout();

            for (var z = 0; z < resolution; z++)
            {
                var normalizedZ = z / (float)(resolution - 1);
                var worldZ = normalizedZ * terrainDepthMeters - terrainDepthMeters * 0.5f;

                for (var x = 0; x < resolution; x++)
                {
                    var normalizedX = x / (float)(resolution - 1);
                    var worldX = normalizedX * terrainWidthMeters - terrainWidthMeters * 0.5f;
                    var heightMeters = CalculateTerrainHeightMeters(worldX, worldZ, layout);
                    heights[z, x] = Mathf.Clamp01(heightMeters / terrainHeightMeters);
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }

        private void PaintTerrainLayers(Terrain terrain)
        {
            isRepaintingLayers = true;

            var terrainData = terrain.terrainData;
            var resolution = terrainData.alphamapResolution;
            var alphamaps = new float[resolution, resolution, terrainData.terrainLayers.Length];

            try
            {
                for (var z = 0; z < resolution; z++)
                {
                    var normalizedZ = z / (float)(resolution - 1);
                    for (var x = 0; x < resolution; x++)
                    {
                        var normalizedX = x / (float)(resolution - 1);
                        var height = terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
                        var steepness = terrainData.GetSteepness(normalizedX, normalizedZ);

                        var sandWeight = Mathf.Clamp01(1f - Mathf.Abs(height - (waterLevelMeters + sandBandOffsetMeters)) / sandBandWidthMeters)
                            * Mathf.Clamp01(1f - (steepness / Mathf.Max(1f, sandMaxSlopeDegrees)));
                        var rockWeight = Mathf.Max(
                            Mathf.InverseLerp(rockSlopeStartDegrees, rockSlopeFullDegrees, steepness),
                            Mathf.InverseLerp(waterLevelMeters + rockHeightStartMeters, waterLevelMeters + rockHeightFullMeters, height));
                        var grassWeight = Mathf.Max(0.05f, 1f - Mathf.Max(sandWeight, rockWeight));

                        var total = sandWeight + grassWeight + rockWeight;
                        alphamaps[z, x, 0] = sandWeight / total;
                        alphamaps[z, x, 1] = grassWeight / total;
                        alphamaps[z, x, 2] = rockWeight / total;
                    }
                }

                terrainData.SetAlphamaps(0, 0, alphamaps);
            }
            finally
            {
                isRepaintingLayers = false;
            }
        }

        private IslandLayout BuildLayout()
        {
            var halfWidth = terrainWidthMeters * 0.5f;
            var halfDepth = terrainDepthMeters * 0.5f;
            var clampedFootprint = Mathf.Clamp(landFootprintMeters, 900f, Mathf.Min(terrainWidthMeters, terrainDepthMeters) - 320f);
            var mainBaseRadius = clampedFootprint * 0.5f;
            var mainRadiusX = Mathf.Clamp(mainBaseRadius * Mathf.Lerp(0.92f, 1.2f, Hash01(11f)), 420f, halfWidth - 180f);
            var mainRadiusZ = Mathf.Clamp(mainBaseRadius * Mathf.Lerp(0.74f, 1.08f, Hash01(13f)), 420f, halfDepth - 180f);
            var mainAngle = HashSigned01(17f) * 0.95f;
            var oceanMargin = Mathf.Max(180f, shorelineFalloffMeters * 0.45f + 40f);
            var mainCenter = new Vector2(
                ChooseCenterCoordinate(halfWidth, mainRadiusX, oceanMargin, 19f),
                ChooseCenterCoordinate(halfDepth, mainRadiusZ, oceanMargin, 23f));

            var peakOffset = Rotate(
                new Vector2(HashSigned01(29f) * mainRadiusX * 0.22f, HashSigned01(31f) * mainRadiusZ * 0.18f),
                mainAngle);
            var peakCenter = mainCenter + peakOffset;
            var ridgeAngle = mainAngle + HashSigned01(37f) * 0.75f;

            var hasSatellite = Hash01(41f) < satelliteChance;
            var satelliteCenter = Vector2.zero;
            var satelliteRadii = Vector2.zero;
            var satelliteAngle = 0f;

            if (hasSatellite)
            {
                var satelliteBaseRadius = mainBaseRadius * satelliteSizeRatio * Mathf.Lerp(0.82f, 1.18f, Hash01(43f));
                satelliteRadii = new Vector2(
                    Mathf.Clamp(satelliteBaseRadius * Mathf.Lerp(1.05f, 1.22f, Hash01(47f)), 140f, halfWidth - oceanMargin - 40f),
                    Mathf.Clamp(satelliteBaseRadius * Mathf.Lerp(0.8f, 1.08f, Hash01(53f)), 120f, halfDepth - oceanMargin - 40f));
                satelliteAngle = HashSigned01(59f) * 1.3f;

                var offsetAngle = Hash01(61f) * Mathf.PI * 2f;
                var offsetDirection = new Vector2(Mathf.Cos(offsetAngle), Mathf.Sin(offsetAngle));
                var separation = Mathf.Max(mainRadiusX, mainRadiusZ) + Mathf.Max(satelliteRadii.x, satelliteRadii.y) + Mathf.Lerp(180f, 420f, Hash01(67f));
                satelliteCenter = ClampInsideCanvas(mainCenter + offsetDirection * separation, satelliteRadii, oceanMargin);
            }

            var rivers = new RiverLayout[Mathf.Clamp(riverCount, 0, 3)];
            for (var index = 0; index < rivers.Length; index++)
            {
                var directionAngle = Hash01(83f + index * 19f) * Mathf.PI * 2f;
                var direction = new Vector2(Mathf.Cos(directionAngle), Mathf.Sin(directionAngle));
                var start = peakCenter + direction * Mathf.Lerp(40f, 110f, Hash01(89f + index * 7f));
                var reach = Mathf.Max(mainRadiusX, mainRadiusZ) + Mathf.Lerp(120f, 260f, Hash01(97f + index * 13f));
                var end = ClampInsideCanvas(mainCenter + direction * reach, new Vector2(40f, 40f), oceanMargin * 0.6f);
                var mid = Vector2.Lerp(start, end, 0.52f)
                    + Rotate(direction, Mathf.PI * 0.5f) * HashSigned01(101f + index * 17f) * riverMeanderMeters;
                var width = riverHalfWidthMeters * Mathf.Lerp(0.85f, 1.18f, Hash01(109f + index * 11f));
                var depth = riverDepthMeters * Mathf.Lerp(0.82f, 1.14f, Hash01(113f + index * 11f));
                rivers[index] = new RiverLayout(start, mid, end, width, depth);
            }

            return new IslandLayout(
                mainCenter,
                new Vector2(mainRadiusX, mainRadiusZ),
                mainAngle,
                peakCenter,
                ridgeAngle,
                hasSatellite,
                satelliteCenter,
                satelliteRadii,
                satelliteAngle,
                rivers);
        }

        private float CalculateTerrainHeightMeters(float worldX, float worldZ, IslandLayout layout)
        {
            var warpX = worldX
                + 240f * coastlineBreakupStrength * (SampleNoise(worldX, worldZ, 0.00055f, 11f) - 0.5f)
                + 90f * coastlineBreakupStrength * (SampleNoise(worldX, worldZ, 0.0013f, 29f) - 0.5f);
            var warpZ = worldZ
                + 240f * coastlineBreakupStrength * (SampleNoise(worldX, worldZ, 0.00055f, 47f) - 0.5f)
                + 90f * coastlineBreakupStrength * (SampleNoise(worldX, worldZ, 0.0013f, 61f) - 0.5f);

            var mainMask = EvaluateIslandMask(warpX, warpZ, layout.MainCenter, layout.MainRadii, layout.MainAngleRadians, 71f);
            var satelliteMask = layout.HasSatellite
                ? EvaluateIslandMask(warpX, warpZ, layout.SatelliteCenter, layout.SatelliteRadii, layout.SatelliteAngleRadians, 79f)
                : 0f;
            var landMask = Mathf.Max(mainMask, satelliteMask);

            var oceanShelf = waterLevelMeters - 38f + (SampleFractalNoise(warpX - 280f, warpZ + 360f, 0.0007f, 3, 2f, 0.58f) - 0.5f) * 14f;
            var continental = Mathf.Lerp(oceanShelf, waterLevelMeters + 24f, landMask);
            var hills = (SampleFractalNoise(warpX + 620f, warpZ - 430f, 0.0009f, 4, 2f, 0.58f) - 0.48f) * hillsAmplitudeMeters * landMask;

            var mainPeakMask = EvaluateGaussian(worldX, worldZ, layout.PeakCenter, new Vector2(layout.MainRadii.x * 0.33f, layout.MainRadii.y * 0.29f), layout.RidgeAngleRadians - 0.2f);
            var ridgeMask = EvaluateGaussian(worldX, worldZ, layout.MainCenter, new Vector2(layout.MainRadii.x * 0.72f, layout.MainRadii.y * 0.18f), layout.RidgeAngleRadians);
            var mountainNoise = 0.7f + 0.55f * SmoothRange01(0.42f, 0.88f, SampleFractalNoise(warpX - 540f, warpZ + 170f, 0.00085f, 4, 2.05f, 0.56f));
            var mountains = mainMask * mountainNoise * ((mainPeakMask * mountainAmplitudeMeters * 0.72f) + (ridgeMask * mountainAmplitudeMeters * 0.42f));

            var satellitePeak = layout.HasSatellite
                ? EvaluateGaussian(worldX, worldZ, layout.SatelliteCenter, layout.SatelliteRadii * 0.42f, layout.SatelliteAngleRadians) * satelliteMask * mountainAmplitudeMeters * 0.26f
                : 0f;

            var cliffNoise = SmoothRange01(0.62f, 0.9f, SampleRidgedNoise(warpX + 210f, warpZ - 160f, 0.00125f, 3, 2f, 0.55f));
            var cliffRegions = Mathf.Max(mainPeakMask * 0.75f, ridgeMask * 0.68f);
            var cliffAccents = cliffRegions * cliffNoise * (52f * cliffSharpening);

            var height = continental + hills + mountains + satellitePeak + cliffAccents;

            for (var index = 0; index < layout.Rivers.Length; index++)
            {
                var river = layout.Rivers[index];
                var distance = DistanceToPolyline(new Vector2(worldX, worldZ), river.Start, river.Mid, river.End);
                var riverMask = Gaussian1D(distance, river.WidthMeters) * mainMask;
                height -= riverMask * river.DepthMeters;
                height = Mathf.Lerp(height, waterLevelMeters + 4f, riverMask * 0.45f);
            }

            var smoothingTarget = continental + hills * 0.72f + mountains * 0.92f + satellitePeak * 0.9f;
            height = Mathf.Lerp(height, smoothingTarget, 0.12f);

            var inlandMask = SmoothRange01(waterLevelMeters + 10f, waterLevelMeters + 75f, height) * landMask;
            var detailNoise = (SampleFractalNoise(warpX - 180f, warpZ + 260f, detailNoiseFrequency, 4, 2.1f, 0.54f) - 0.5f) * 2f;
            var billowNoise = (SampleBillowNoise(warpX + 410f, warpZ - 290f, detailNoiseFrequency * 1.35f, 3, 2f, 0.56f) - 0.5f) * 2f;
            var terrainDetail = (detailNoise + (billowNoise * 0.65f)) * detailAmplitudeMeters * inlandMask;

            var pitMask = SmoothRange01(0.58f, 0.9f, SampleRidgedNoise(warpX - 320f, warpZ + 520f, pitNoiseFrequency, 3, 2.15f, 0.52f));
            var pits = pitMask * pitAmplitudeMeters * inlandMask * Mathf.Clamp01(1f - (cliffRegions * 0.45f));

            var cliffHeightMask = SmoothRange01(waterLevelMeters + 70f, waterLevelMeters + 210f, height);
            var cliffMask = cliffRegions * cliffHeightMask;
            var cliffMicroNoise = SmoothRange01(0.44f, 0.88f, SampleRidgedNoise(warpX + 140f, warpZ - 190f, cliffDetailFrequency, 4, 2.05f, 0.54f));
            var cliffBreakNoise = (SampleBillowNoise(warpX - 540f, warpZ + 90f, cliffDetailFrequency * 1.22f, 2, 2f, 0.58f) - 0.5f) * 2f;
            var cliffMicroBreaks = ((cliffMicroNoise * 0.75f) + (cliffBreakNoise * 0.25f)) * cliffDetailStrengthMeters * cliffMask * cliffSharpening;

            height += terrainDetail;
            height -= pits;
            height += cliffMicroBreaks;

            var beachTarget = waterLevelMeters + 5f;
            var beachMask = Mathf.Clamp01(1f - Mathf.Abs(height - beachTarget) / beachBlendMeters);
            height = Mathf.Lerp(height, beachTarget + (SampleNoise(worldX, worldZ, 0.0048f, 123f) - 0.5f) * 1.5f, beachMask * 0.32f);

            return Mathf.Clamp(height, 0f, terrainHeightMeters - 5f);
        }

        private void EnsureOceanSurface()
        {
            var oceanRoot = FindOrCreateChild(OceanRootName);
            var oceanSurface = FindOrCreatePrimitive(oceanRoot, OceanSurfaceName, PrimitiveType.Cube);
            oceanSurface.transform.localPosition = new Vector3(0f, waterLevelMeters, 0f);
            oceanSurface.transform.localScale = new Vector3(40000f, 0.25f, 40000f);

            var renderer = oceanSurface.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = EnsureOceanMaterial();
            }

            var collider = oceanSurface.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        private Material EnsureOceanMaterial()
        {
            var material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(OceanMaterialPath);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = "MainTown_Ocean",
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", new Color(0.16f, 0.42f, 0.57f, 1f));
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
            }

            UnityEditor.AssetDatabase.CreateAsset(material, OceanMaterialPath);
            return material;
        }

        private Transform FindOrCreateChild(string childName)
        {
            foreach (Transform child in transform)
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            var childObject = new GameObject(childName);
            childObject.transform.SetParent(transform, false);
            return childObject.transform;
        }

        private static GameObject FindOrCreatePrimitive(Transform parent, string childName, PrimitiveType primitiveType)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child.gameObject;
                }
            }

            var childObject = GameObject.CreatePrimitive(primitiveType);
            childObject.name = childName;
            childObject.transform.SetParent(parent, false);
            return childObject;
        }

        private bool TryGetOwnedTerrain(out Terrain terrain)
        {
            terrain = null;
            foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != TerrainRootName)
                {
                    continue;
                }

                terrain = child.GetComponent<Terrain>();
                return terrain != null;
            }

            return false;
        }

        private void HandleTerrainHeightmapChanged(Terrain terrain, RectInt heightRegion, bool synched)
        {
            if (!autoRepaintLayersOnHeightChange || isRepaintingLayers || terrain == null)
            {
                return;
            }

            if (!TryGetOwnedTerrain(out var ownedTerrain) || ownedTerrain != terrain)
            {
                return;
            }

            RepaintTerrainLayersInEditor();
        }

        private float SampleNoise(float worldX, float worldZ, float frequency, float offset)
        {
            var seedOffsetX = HashToOffset(seed * 0.173f + offset * 13.1f);
            var seedOffsetZ = HashToOffset(seed * 0.217f + offset * 17.9f);
            return Mathf.PerlinNoise(worldX * frequency + offset + seedOffsetX, worldZ * frequency + offset + seedOffsetZ);
        }

        private float SampleFractalNoise(float worldX, float worldZ, float baseFrequency, int octaves, float lacunarity, float persistence)
        {
            var amplitude = 1f;
            var totalAmplitude = 0f;
            var total = 0f;
            var frequency = baseFrequency;

            for (var octave = 0; octave < octaves; octave++)
            {
                total += SampleNoise(worldX, worldZ, frequency, octave * 37f) * amplitude;
                totalAmplitude += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / Mathf.Max(totalAmplitude, Mathf.Epsilon);
        }

        private float SampleRidgedNoise(float worldX, float worldZ, float baseFrequency, int octaves, float lacunarity, float persistence)
        {
            var amplitude = 1f;
            var totalAmplitude = 0f;
            var total = 0f;
            var frequency = baseFrequency;

            for (var octave = 0; octave < octaves; octave++)
            {
                var noise = SampleNoise(worldX, worldZ, frequency, octave * 53f);
                var ridge = 1f - Mathf.Abs(noise * 2f - 1f);
                total += ridge * ridge * amplitude;
                totalAmplitude += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / Mathf.Max(totalAmplitude, Mathf.Epsilon);
        }

        private float SampleBillowNoise(float worldX, float worldZ, float baseFrequency, int octaves, float lacunarity, float persistence)
        {
            var amplitude = 1f;
            var totalAmplitude = 0f;
            var total = 0f;
            var frequency = baseFrequency;

            for (var octave = 0; octave < octaves; octave++)
            {
                var noise = SampleNoise(worldX, worldZ, frequency, octave * 71f);
                var billow = Mathf.Abs((noise * 2f) - 1f);
                total += billow * amplitude;
                totalAmplitude += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / Mathf.Max(totalAmplitude, Mathf.Epsilon);
        }

        private float EvaluateIslandMask(float worldX, float worldZ, Vector2 center, Vector2 radii, float angleRadians, float noiseOffset)
        {
            var localPoint = Rotate(new Vector2(worldX, worldZ) - center, -angleRadians);
            var breakupMeters = 180f * coastlineBreakupStrength;
            localPoint.x += (SampleNoise(worldX + center.x, worldZ + center.y, 0.0011f, noiseOffset) - 0.5f) * breakupMeters;
            localPoint.y += (SampleNoise(worldX - center.x, worldZ - center.y, 0.0011f, noiseOffset + 13f) - 0.5f) * breakupMeters;

            var coverageScale = Mathf.Lerp(0.62f, 0.94f, landCoverage);
            var normalizedX = localPoint.x / Mathf.Max(1f, radii.x * coverageScale);
            var normalizedZ = localPoint.y / Mathf.Max(1f, radii.y * coverageScale);
            var distance = Mathf.Sqrt((normalizedX * normalizedX) + (normalizedZ * normalizedZ));
            var shorelineNoise = (SampleFractalNoise(worldX, worldZ, 0.00115f, 3, 2f, 0.55f) - 0.5f) * coastlineBreakupStrength * 0.34f;
            var falloff = Mathf.Max(0.08f, shorelineFalloffMeters / Mathf.Max(120f, Mathf.Min(radii.x, radii.y)));
            return 1f - SmoothRange01(1f + shorelineNoise, 1f + falloff + shorelineNoise, distance);
        }

        private static float EvaluateGaussian(float worldX, float worldZ, Vector2 center, Vector2 radii, float angleRadians)
        {
            var localPoint = Rotate(new Vector2(worldX, worldZ) - center, -angleRadians);
            var normalizedX = localPoint.x / Mathf.Max(1f, radii.x);
            var normalizedZ = localPoint.y / Mathf.Max(1f, radii.y);
            return Mathf.Exp(-((normalizedX * normalizedX) + (normalizedZ * normalizedZ)));
        }

        private float ChooseCenterCoordinate(float halfExtent, float radius, float oceanMargin, float salt)
        {
            var usableRange = Mathf.Max(0f, halfExtent - radius - oceanMargin);
            return HashSigned01(salt) * usableRange * 0.92f;
        }

        private Vector2 ClampInsideCanvas(Vector2 point, Vector2 radii, float oceanMargin)
        {
            var maxX = Mathf.Max(0f, terrainWidthMeters * 0.5f - radii.x - oceanMargin);
            var maxZ = Mathf.Max(0f, terrainDepthMeters * 0.5f - radii.y - oceanMargin);
            return new Vector2(Mathf.Clamp(point.x, -maxX, maxX), Mathf.Clamp(point.y, -maxZ, maxZ));
        }

        private float Hash01(float salt)
        {
            var hashed = Mathf.Sin((seed * 0.01371f) + salt * 127.1f) * 43758.5453f;
            return hashed - Mathf.Floor(hashed);
        }

        private float HashSigned01(float salt)
        {
            return (Hash01(salt) * 2f) - 1f;
        }

        private static Vector2 Rotate(Vector2 point, float radians)
        {
            var cosine = Mathf.Cos(radians);
            var sine = Mathf.Sin(radians);
            return new Vector2(
                (point.x * cosine) - (point.y * sine),
                (point.x * sine) + (point.y * cosine));
        }

        private static float DistanceToPolyline(Vector2 point, Vector2 start, Vector2 mid, Vector2 end)
        {
            return Mathf.Min(DistanceToSegment(point, start, mid), DistanceToSegment(point, mid, end));
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, start);
            }

            var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return Vector2.Distance(point, start + (segment * t));
        }

        private static float SmoothRange01(float min, float max, float value)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(min, max, value));
        }

        private static float Gaussian1D(float distance, float radius)
        {
            return Mathf.Exp(-(distance * distance) / (2f * radius * radius));
        }

        private static float HashToOffset(float value)
        {
            var hashed = Mathf.Sin(value * 12.9898f) * 43758.5453f;
            return Mathf.Abs(hashed - Mathf.Floor(hashed)) * 10000f;
        }

        private static void EnsureFolderPath(string folderPath)
        {
            var parts = folderPath.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = $"{current}/{parts[index]}";
                if (!UnityEditor.AssetDatabase.IsValidFolder(next))
                {
                    UnityEditor.AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
#endif
    }
}
