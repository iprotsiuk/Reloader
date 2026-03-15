using System;
using UnityEngine;

namespace Reloader.World
{
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
        [SerializeField] private float terrainWidthMeters = 5000f;
        [SerializeField] private float terrainDepthMeters = 5000f;
        [SerializeField] private float terrainHeightMeters = 1100f;
        [SerializeField] private int heightmapResolution = 1025;
        [SerializeField] private int alphamapResolution = 1024;
        [SerializeField] private int baseMapResolution = 1024;
        [SerializeField] private int detailResolution = 1024;
        [SerializeField] private int detailResolutionPerPatch = 16;
        [SerializeField] private float shorelineRadiusMeters = 2050f;
        [SerializeField] private float shorelineFalloffMeters = 520f;
        [SerializeField] private float hillsAmplitudeMeters = 90f;
        [SerializeField] private float mountainAmplitudeMeters = 430f;
        [SerializeField] private float cliffSharpening = 1.45f;
        [SerializeField] private float mainRiverHalfWidthMeters = 210f;
        [SerializeField] private float mainRiverDepthMeters = 140f;
        [SerializeField] private float secondaryRiverHalfWidthMeters = 165f;
        [SerializeField] private float secondaryRiverDepthMeters = 120f;
        [SerializeField] private float beachBlendMeters = 10f;

        [ContextMenu("Regenerate Terrain")]
        private void RegenerateTerrainContextMenu()
        {
            RegenerateInEditor();
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

            for (var z = 0; z < resolution; z++)
            {
                var normalizedZ = z / (float)(resolution - 1);
                var worldZ = normalizedZ * terrainDepthMeters - terrainDepthMeters * 0.5f;

                for (var x = 0; x < resolution; x++)
                {
                    var normalizedX = x / (float)(resolution - 1);
                    var worldX = normalizedX * terrainWidthMeters - terrainWidthMeters * 0.5f;
                    var heightMeters = CalculateTerrainHeightMeters(worldX, worldZ);
                    heights[z, x] = Mathf.Clamp01(heightMeters / terrainHeightMeters);
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }

        private void PaintTerrainLayers(Terrain terrain)
        {
            var terrainData = terrain.terrainData;
            var resolution = terrainData.alphamapResolution;
            var alphamaps = new float[resolution, resolution, terrainData.terrainLayers.Length];

            for (var z = 0; z < resolution; z++)
            {
                var normalizedZ = z / (float)(resolution - 1);
                for (var x = 0; x < resolution; x++)
                {
                    var normalizedX = x / (float)(resolution - 1);
                    var height = terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
                    var steepness = terrainData.GetSteepness(normalizedX, normalizedZ);

                    var sandWeight = Mathf.Clamp01(1f - Mathf.Abs(height - (waterLevelMeters + 4f)) / 16f) * Mathf.Clamp01(1f - steepness / 26f);
                    var rockWeight = Mathf.Max(
                        Mathf.InverseLerp(34f, 55f, steepness),
                        Mathf.InverseLerp(waterLevelMeters + 125f, waterLevelMeters + 240f, height));
                    var grassWeight = Mathf.Max(0.05f, 1f - Mathf.Max(sandWeight, rockWeight));

                    var total = sandWeight + grassWeight + rockWeight;
                    alphamaps[z, x, 0] = sandWeight / total;
                    alphamaps[z, x, 1] = grassWeight / total;
                    alphamaps[z, x, 2] = rockWeight / total;
                }
            }

            terrainData.SetAlphamaps(0, 0, alphamaps);
        }

        private float CalculateTerrainHeightMeters(float worldX, float worldZ)
        {
            var warpX = worldX
                + 420f * (SampleNoise(worldX, worldZ, 0.00038f, 11f) - 0.5f)
                + 150f * (SampleNoise(worldX, worldZ, 0.00095f, 29f) - 0.5f);
            var warpZ = worldZ
                + 420f * (SampleNoise(worldX, worldZ, 0.00038f, 47f) - 0.5f)
                + 150f * (SampleNoise(worldX, worldZ, 0.00095f, 61f) - 0.5f);

            var radialDistance = Mathf.Sqrt(warpX * warpX + warpZ * warpZ);
            var shorelineNoise = 320f * (SampleNoise(warpX, warpZ, 0.00055f, 83f) - 0.5f);
            var islandMask = 1f - SmoothRange01(shorelineRadiusMeters + shorelineNoise, shorelineRadiusMeters + shorelineFalloffMeters + shorelineNoise, radialDistance);

            var continental = (SampleFractalNoise(warpX, warpZ, 0.00045f, 5, 2f, 0.52f) - 0.5f) * 55f * islandMask;
            var rolling = (SampleFractalNoise(warpX + 620f, warpZ - 430f, 0.00105f, 4, 2f, 0.58f) - 0.5f) * hillsAmplitudeMeters * islandMask;
            var baseHeight = Mathf.Lerp(waterLevelMeters - 30f, waterLevelMeters + 30f, islandMask) + continental + rolling;

            var northWestMask = Gaussian(worldX, worldZ, -1325f, -920f, 980f, 760f);
            var northEastMask = Gaussian(worldX, worldZ, 1380f, -900f, 980f, 760f);
            var southMask = Gaussian(worldX, worldZ, 120f, 1180f, 1550f, 980f);

            var northWestShape = SmoothRange01(0.46f, 0.76f, SampleFractalNoise(warpX - 860f, warpZ + 210f, 0.00092f, 4, 2.1f, 0.55f));
            var northEastShape = SmoothRange01(0.5f, 0.8f, SampleFractalNoise(warpX + 1180f, warpZ + 510f, 0.00088f, 4, 2.1f, 0.55f));
            var southShape = SmoothRange01(0.48f, 0.78f, SampleFractalNoise(warpX - 260f, warpZ - 980f, 0.00082f, 4, 2.05f, 0.57f));

            var broadMountains =
                northWestMask * northWestShape * (mountainAmplitudeMeters * 0.72f) +
                northEastMask * northEastShape * (mountainAmplitudeMeters * 0.58f) +
                southMask * southShape * mountainAmplitudeMeters;

            var northWestCliffs = northWestMask * SmoothRange01(0.76f, 0.92f, SampleRidgedNoise(warpX - 420f, warpZ + 180f, 0.00092f, 3, 2f, 0.55f)) * (96f * cliffSharpening);
            var northEastCliffs = northEastMask * SmoothRange01(0.8f, 0.94f, SampleRidgedNoise(warpX + 510f, warpZ + 90f, 0.0009f, 3, 2f, 0.55f)) * (72f * cliffSharpening);
            var southCliffs = southMask * SmoothRange01(0.78f, 0.93f, SampleRidgedNoise(warpX - 180f, warpZ - 520f, 0.00086f, 3, 2f, 0.57f)) * (84f * cliffSharpening);
            var cliffAccents = (northWestCliffs + northEastCliffs + southCliffs) * SmoothRange01(waterLevelMeters + 90f, waterLevelMeters + 240f, baseHeight + broadMountains);

            var mainRiverCenterZ = 40f * Mathf.Sin(worldX * 0.0032f);
            var mainRiverDistance = Mathf.Abs(worldZ - mainRiverCenterZ);
            var mainRiverMask = Gaussian1D(mainRiverDistance, mainRiverHalfWidthMeters);

            var secondaryRiverCenterX = 900f + 60f * Mathf.Sin(worldZ * 0.0027f);
            var secondaryRiverDistance = Mathf.Abs(worldX - secondaryRiverCenterX);
            var southBranchGate = SmoothRange01(mainRiverCenterZ + 120f, mainRiverCenterZ + 360f, worldZ);
            var secondaryRiverMask = Gaussian1D(secondaryRiverDistance, secondaryRiverHalfWidthMeters) * southBranchGate;

            var riverMask = Mathf.Max(mainRiverMask, secondaryRiverMask);

            var height = baseHeight + broadMountains + cliffAccents;
            height -= mainRiverMask * mainRiverDepthMeters;
            height -= secondaryRiverMask * secondaryRiverDepthMeters;
            height = Mathf.Lerp(height, waterLevelMeters + 2f, riverMask * 0.72f);

            var smoothingTarget = baseHeight + broadMountains * 0.88f + (SampleFractalNoise(warpX + 340f, warpZ - 140f, 0.0007f, 3, 2f, 0.6f) - 0.5f) * 18f;
            height = Mathf.Lerp(height, smoothingTarget, 0.18f);

            var beachTarget = waterLevelMeters + 5f;
            var beachMask = Mathf.Clamp01(1f - Mathf.Abs(height - beachTarget) / beachBlendMeters);
            height = Mathf.Lerp(height, beachTarget + (SampleNoise(worldX, worldZ, 0.005f, 123f) - 0.5f) * 1.5f, beachMask * 0.35f);

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

        private static float SmoothRange01(float min, float max, float value)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(min, max, value));
        }

        private static float Gaussian(float x, float z, float centerX, float centerZ, float radiusX, float radiusZ)
        {
            var normalizedX = (x - centerX) / radiusX;
            var normalizedZ = (z - centerZ) / radiusZ;
            return Mathf.Exp(-(normalizedX * normalizedX + normalizedZ * normalizedZ));
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
