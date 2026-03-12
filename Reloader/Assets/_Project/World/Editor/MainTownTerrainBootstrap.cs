using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Editor
{
    public static class MainTownTerrainBootstrap
    {
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string WorldShellName = "MainTownWorldShell";
        private const string TerrainRootName = "MainTownTerrain";
        private const string TerrainFolderPath = "Assets/_Project/World/Terrain/MainTown";
        private const string TerrainDataPath = TerrainFolderPath + "/MainTownTerrainData.asset";
        private const string WaterMaterialPath = "Assets/ThirdParty/Polygon-Mega Survival Forest/Materials/Water.mat";
        private const string BlueMaterialPath = "Assets/ThirdParty/Polygon-Mega Survival Forest/Materials/Blue.mat";

        private static readonly string[] TreePrefabPaths =
        {
            "Assets/ThirdParty/Polygon-Mega Survival Forest/Prefabs/SM_Pine.prefab",
        };

        private static readonly LayerSpec[] TerrainLayerSpecs =
        {
            new(
                "MainTown_Grass",
                "Assets/ThirdParty/Polygon-Mega Survival Forest/Textures/TerrainTextures/Grass.PNG",
                null,
                new Vector2(30f, 30f)),
            new(
                "MainTown_Dirt",
                "Assets/EasyRoads3D/textures/roads/dirtRoad_A.tga",
                "Assets/EasyRoads3D/textures/roads/dirtRoad_N.tga",
                new Vector2(18f, 18f)),
            new(
                "MainTown_Road",
                "Assets/EasyRoads3D/textures/roads/road2Lane_A.tga",
                "Assets/EasyRoads3D/textures/roads/road_N.png",
                new Vector2(14f, 14f)),
            new(
                "MainTown_Stone",
                "Assets/ThirdParty/Polygon-Mega Survival Forest/Textures/TerrainTextures/Stones.PNG",
                null,
                new Vector2(22f, 22f)),
        };

        [MenuItem("Tools/Reloader/World/Bootstrap MainTown Terrain")]
        public static void BootstrapMainTownTerrain()
        {
            EnsureFolderPath(TerrainFolderPath);

            var scene = EnsureMainTownSceneLoaded();
            var worldShell = FindRoot(scene, WorldShellName);
            if (worldShell == null)
            {
                Debug.LogError($"Terrain bootstrap aborted. Root '{WorldShellName}' was not found in MainTown.");
                return;
            }

            var terrainData = LoadOrCreateTerrainData();
            ConfigureTerrainData(terrainData);
            terrainData.terrainLayers = EnsureTerrainLayers();

            var terrainObject = LoadOrCreateTerrainObject(worldShell.transform, terrainData);
            var terrain = terrainObject.GetComponent<Terrain>();
            var terrainCollider = terrainObject.GetComponent<TerrainCollider>();

            terrain.terrainData = terrainData;
            terrain.drawInstanced = true;
            terrainCollider.terrainData = terrainData;

            ResetTerrainHeights(terrainData);
            ApplyStarterPaint(worldShell.transform, terrain);
            DisablePlanningFloor(worldShell.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = terrainObject;

            Debug.Log($"MainTown terrain bootstrap complete. Seeded {terrainData.terrainLayers.Length} terrain layers on '{TerrainRootName}'.");
        }

        [MenuItem("Tools/Reloader/World/Apply MainTown Dramatic Terrain Redesign")]
        public static void ApplyMainTownDramaticTerrainRedesign()
        {
            EnsureFolderPath(TerrainFolderPath);

            var scene = EnsureMainTownSceneLoaded();
            var worldShell = FindRoot(scene, WorldShellName);
            if (worldShell == null)
            {
                Debug.LogError($"Dramatic terrain redesign aborted. Root '{WorldShellName}' was not found in MainTown.");
                return;
            }

            var terrainData = LoadOrCreateTerrainData();
            ConfigureTerrainData(terrainData);
            terrainData.terrainLayers = EnsureTerrainLayers();

            var terrainObject = LoadOrCreateTerrainObject(worldShell.transform, terrainData);
            var terrain = terrainObject.GetComponent<Terrain>();
            var terrainCollider = terrainObject.GetComponent<TerrainCollider>();
            terrain.terrainData = terrainData;
            terrain.drawInstanced = true;
            terrainCollider.terrainData = terrainData;

            ResetNeutralBasePaint(terrain);
            SculptDramaticTerrain(terrain);
            ApplyRegionalForestTreeInstances(terrain);
            EnsureWaterPresentation(worldShell.transform);
            DisablePlanningFloor(worldShell.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = terrainObject;

            Debug.Log($"MainTown dramatic terrain redesign complete. Trees: {terrainData.treeInstances.Length}.");
        }

        [MenuItem("Tools/Reloader/World/Flatten MainTown Terrain To Planning Shell")]
        public static void FlattenMainTownTerrainToPlanningShell()
        {
            var scene = EnsureMainTownSceneLoaded();
            var worldShell = FindRoot(scene, WorldShellName);
            if (worldShell == null)
            {
                Debug.LogError($"Flatten terrain aborted. Root '{WorldShellName}' was not found in MainTown.");
                return;
            }

            var terrainTransform = worldShell.transform.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == TerrainRootName);
            if (terrainTransform == null)
            {
                Debug.LogError($"Flatten terrain aborted. '{TerrainRootName}' was not found in MainTown.");
                return;
            }

            var terrain = terrainTransform.GetComponent<Terrain>();
            if (terrain == null)
            {
                Debug.LogError($"Flatten terrain aborted. '{TerrainRootName}' has no Terrain component.");
                return;
            }

            FlattenLoadedMainTownTerrain(worldShell.transform, terrain);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = terrain.gameObject;

            Debug.Log("MainTown terrain flattened back to planning-shell state.");
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

        private static void EnsureFolderPath(string folderPath)
        {
            var parts = folderPath.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static TerrainData LoadOrCreateTerrainData()
        {
            var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (terrainData != null)
            {
                return terrainData;
            }

            terrainData = new TerrainData();
            AssetDatabase.CreateAsset(terrainData, TerrainDataPath);
            return terrainData;
        }

        private static void ConfigureTerrainData(TerrainData terrainData)
        {
            terrainData.heightmapResolution = 513;
            terrainData.alphamapResolution = 1024;
            terrainData.baseMapResolution = 1024;
            terrainData.SetDetailResolution(1024, 16);
            terrainData.size = new Vector3(2000f, 120f, 2000f);
        }

        private static TerrainLayer[] EnsureTerrainLayers()
        {
            var terrainLayers = new TerrainLayer[TerrainLayerSpecs.Length];
            for (var index = 0; index < TerrainLayerSpecs.Length; index++)
            {
                var spec = TerrainLayerSpecs[index];
                var path = $"{TerrainFolderPath}/{spec.AssetName}.terrainlayer";
                var terrainLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                if (terrainLayer == null)
                {
                    terrainLayer = new TerrainLayer();
                    AssetDatabase.CreateAsset(terrainLayer, path);
                }

                ApplyLayerSpec(terrainLayer, spec);
                terrainLayers[index] = terrainLayer;
            }

            return terrainLayers;
        }

        private static void ApplyLayerSpec(TerrainLayer terrainLayer, LayerSpec spec)
        {
            var diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.DiffuseTexturePath);
            if (diffuseTexture == null)
            {
                Debug.LogError($"Terrain bootstrap could not load diffuse texture '{spec.DiffuseTexturePath}' for layer '{spec.AssetName}'.");
                return;
            }

            terrainLayer.diffuseTexture = diffuseTexture;
            terrainLayer.normalMapTexture = string.IsNullOrWhiteSpace(spec.NormalTexturePath)
                ? null
                : AssetDatabase.LoadAssetAtPath<Texture2D>(spec.NormalTexturePath);
            terrainLayer.maskMapTexture = null;
            terrainLayer.tileSize = spec.TileSize;
            terrainLayer.tileOffset = Vector2.zero;
            terrainLayer.metallic = 0f;
            terrainLayer.smoothness = 0.05f;
            EditorUtility.SetDirty(terrainLayer);
        }

        private static GameObject LoadOrCreateTerrainObject(Transform worldShell, TerrainData terrainData)
        {
            var terrainTransform = worldShell.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == TerrainRootName);

            GameObject terrainObject;
            if (terrainTransform != null)
            {
                terrainObject = terrainTransform.gameObject;
            }
            else
            {
                terrainObject = Terrain.CreateTerrainGameObject(terrainData);
                terrainObject.name = TerrainRootName;
            }

            if (terrainObject.transform.parent != worldShell)
            {
                terrainObject.transform.SetParent(worldShell, false);
            }

            terrainObject.transform.localPosition = new Vector3(-1000f, 0f, -1000f);
            terrainObject.transform.localRotation = Quaternion.identity;
            terrainObject.transform.localScale = Vector3.one;

            if (terrainObject.GetComponent<TerrainCollider>() == null)
            {
                terrainObject.AddComponent<TerrainCollider>();
            }

            return terrainObject;
        }

        private static void ResetTerrainHeights(TerrainData terrainData)
        {
            var resolution = terrainData.heightmapResolution;
            var heights = new float[resolution, resolution];
            terrainData.SetHeights(0, 0, heights);
        }

        public static void FlattenLoadedMainTownTerrain(Transform worldShell, Terrain terrain)
        {
            ResetTerrainHeights(terrain.terrainData);
            ResetNeutralBasePaint(terrain);
            terrain.terrainData.SetTreeInstances(Array.Empty<TreeInstance>(), true);
            RemoveWaterPresentation(worldShell);
            EnablePlanningFloor(worldShell);
        }

        private static void SculptDramaticTerrain(Terrain terrain)
        {
            var terrainData = terrain.terrainData;
            var resolution = terrainData.heightmapResolution;
            var heights = new float[resolution, resolution];

            for (var z = 0; z < resolution; z++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var normalizedX = x / (float)(resolution - 1);
                    var normalizedZ = z / (float)(resolution - 1);
                    var worldX = normalizedX * terrainData.size.x - 1000f;
                    var worldZ = normalizedZ * terrainData.size.z - 1000f;
                    heights[z, x] = CalculateDramaticHeight(worldX, worldZ);
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }

        private static float CalculateDramaticHeight(float worldX, float worldZ)
        {
            var radialDistance = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(20f, 10f));
            var outerRing = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(520f, 980f, radialDistance));

            var height = 0.12f;
            height += outerRing * outerRing * 0.24f;
            height += outerRing * 0.02f * (
                Mathf.Sin(worldX * 0.0085f) * Mathf.Cos(worldZ * 0.0065f) +
                0.55f * Mathf.Sin((worldX + worldZ) * 0.011f));

            height -= 0.035f * Gaussian(worldX, worldZ, 80f, 40f, 340f, 280f);
            height += 0.055f * Gaussian(worldX, worldZ, 200f, 180f, 150f, 135f);
            height += 0.05f * Gaussian(worldX, worldZ, 540f, -40f, 210f, 190f);
            height += 0.11f * Gaussian(worldX, worldZ, 760f, -760f, 220f, 220f);
            height += 0.085f * Gaussian(worldX, worldZ, -760f, 760f, 230f, 230f);
            height += 0.07f * Gaussian(worldX, worldZ, -800f, -620f, 280f, 220f);

            var riverDistance = DistanceToSegment(worldX, worldZ, -650f, 860f, -260f, -930f);
            height -= 0.095f * Mathf.Exp(-(riverDistance * riverDistance) / (2f * 85f * 85f));
            height -= 0.03f * Mathf.Exp(-(riverDistance * riverDistance) / (2f * 190f * 190f));
            height -= 0.07f * Gaussian(worldX, worldZ, -620f, 520f, 220f, 170f);
            height -= 0.12f * Gaussian(worldX, worldZ, 420f, -260f, 180f, 145f);
            height += 0.05f * (Gaussian(worldX, worldZ, 420f, -260f, 320f, 250f) - Gaussian(worldX, worldZ, 420f, -260f, 220f, 170f));

            return Mathf.Clamp(height, 0.02f, 0.5f);
        }

        private static float Gaussian(float x, float z, float centerX, float centerZ, float radiusX, float radiusZ)
        {
            var normalizedX = (x - centerX) / radiusX;
            var normalizedZ = (z - centerZ) / radiusZ;
            return Mathf.Exp(-(normalizedX * normalizedX + normalizedZ * normalizedZ));
        }

        private static float DistanceToSegment(float pointX, float pointZ, float startX, float startZ, float endX, float endZ)
        {
            var start = new Vector2(startX, startZ);
            var end = new Vector2(endX, endZ);
            var point = new Vector2(pointX, pointZ);
            var segment = end - start;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, start);
            }

            var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            var projection = start + segment * t;
            return Vector2.Distance(point, projection);
        }

        private static void ApplyStarterPaint(Transform worldShell, Terrain terrain)
        {
            var terrainData = terrain.terrainData;
            var resolution = terrainData.alphamapResolution;
            var layerCount = terrainData.terrainLayers.Length;
            var alphamaps = new float[resolution, resolution, layerCount];

            FillLayer(alphamaps, 0);

            foreach (var roadRenderer in FindRoadRenderers(worldShell))
            {
                PaintLayerWithinBounds(terrain, alphamaps, roadRenderer.bounds, IsDirtRoute(roadRenderer.name) ? 1 : 2, 4f);
            }

            var quarryBounds = CalculateDistrictBounds(worldShell, "District_QuarryBasin");
            if (quarryBounds.HasValue)
            {
                PaintLayerWithinBounds(terrain, alphamaps, quarryBounds.Value, 3, 8f);
            }

            terrainData.SetAlphamaps(0, 0, alphamaps);
        }

        private static void ResetNeutralBasePaint(Terrain terrain)
        {
            var terrainData = terrain.terrainData;
            var resolution = terrainData.alphamapResolution;
            var layerCount = terrainData.terrainLayers.Length;
            var alphamaps = new float[resolution, resolution, layerCount];
            FillLayer(alphamaps, 0);
            terrainData.SetAlphamaps(0, 0, alphamaps);
        }

        private static void FillLayer(float[,,] alphamaps, int layerIndex)
        {
            var height = alphamaps.GetLength(0);
            var width = alphamaps.GetLength(1);
            var layers = alphamaps.GetLength(2);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    for (var layer = 0; layer < layers; layer++)
                    {
                        alphamaps[y, x, layer] = layer == layerIndex ? 1f : 0f;
                    }
                }
            }
        }

        private static IEnumerable<Renderer> FindRoadRenderers(Transform worldShell)
        {
            return worldShell
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.gameObject.name.StartsWith("Road_") || renderer.gameObject.name.StartsWith("MainStreet"))
                .Where(renderer => renderer.gameObject.name is not "PerimeterLoopRoad" and not "MainStreetSpine" and not "ServiceRoads")
                .Where(renderer => renderer.bounds.size.x > 1f || renderer.bounds.size.z > 1f);
        }

        private static bool IsDirtRoute(string routeName)
        {
            return routeName.Contains("Road_Dirt_");
        }

        private static Bounds? CalculateDistrictBounds(Transform worldShell, string districtName)
        {
            var district = worldShell.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == districtName);

            if (district == null)
            {
                return null;
            }

            var renderers = district.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();

            if (renderers.Length == 0)
            {
                return null;
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            bounds.Expand(new Vector3(40f, 0f, 40f));
            return bounds;
        }

        private static void PaintLayerWithinBounds(Terrain terrain, float[,,] alphamaps, Bounds worldBounds, int layerIndex, float padding)
        {
            var terrainData = terrain.terrainData;
            var terrainPosition = terrain.transform.position;
            var terrainSize = terrainData.size;
            var resolution = terrainData.alphamapResolution;

            var minX = Mathf.Clamp01((worldBounds.min.x - padding - terrainPosition.x) / terrainSize.x);
            var maxX = Mathf.Clamp01((worldBounds.max.x + padding - terrainPosition.x) / terrainSize.x);
            var minZ = Mathf.Clamp01((worldBounds.min.z - padding - terrainPosition.z) / terrainSize.z);
            var maxZ = Mathf.Clamp01((worldBounds.max.z + padding - terrainPosition.z) / terrainSize.z);

            var startX = Mathf.Clamp(Mathf.FloorToInt(minX * (resolution - 1)), 0, resolution - 1);
            var endX = Mathf.Clamp(Mathf.CeilToInt(maxX * (resolution - 1)), 0, resolution - 1);
            var startZ = Mathf.Clamp(Mathf.FloorToInt(minZ * (resolution - 1)), 0, resolution - 1);
            var endZ = Mathf.Clamp(Mathf.CeilToInt(maxZ * (resolution - 1)), 0, resolution - 1);

            for (var z = startZ; z <= endZ; z++)
            {
                for (var x = startX; x <= endX; x++)
                {
                    for (var layer = 0; layer < alphamaps.GetLength(2); layer++)
                    {
                        alphamaps[z, x, layer] = layer == layerIndex ? 1f : 0f;
                    }
                }
            }
        }

        private static void DisablePlanningFloor(Transform worldShell)
        {
            var basinFloor = worldShell.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == "BasinFloor");

            if (basinFloor != null)
            {
                basinFloor.gameObject.SetActive(false);
            }
        }

        private static void EnablePlanningFloor(Transform worldShell)
        {
            var basinFloor = worldShell.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == "BasinFloor");

            if (basinFloor != null)
            {
                basinFloor.gameObject.SetActive(true);
            }
        }

        private static void ApplyRegionalForestTreeInstances(Terrain terrain)
        {
            var terrainData = terrain.terrainData;
            var prototypes = LoadTreePrototypes();
            if (prototypes.Length == 0)
            {
                terrainData.SetTreeInstances(Array.Empty<TreeInstance>(), true);
                return;
            }

            terrainData.treePrototypes = prototypes;

            var random = new System.Random(11311);
            var instances = new List<TreeInstance>(900);

            AddForestRegion(terrain, instances, random, new Rect(-930f, -930f, 520f, 1860f), 320);
            AddForestRegion(terrain, instances, random, new Rect(-260f, 430f, 1200f, 470f), 210);
            AddForestRegion(terrain, instances, random, new Rect(280f, -930f, 640f, 640f), 190);
            AddForestRegion(terrain, instances, random, new Rect(-860f, 180f, 460f, 520f), 180);
            AddForestRegion(terrain, instances, random, new Rect(-560f, -900f, 320f, 1800f), 170);

            terrainData.SetTreeInstances(instances.ToArray(), true);
        }

        private static TreePrototype[] LoadTreePrototypes()
        {
            var prefabs = TreePrefabPaths
                .Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path))
                .Where(prefab => prefab != null)
                .ToArray();

            return prefabs
                .Select(prefab => new TreePrototype
                {
                    prefab = prefab,
                    bendFactor = 0.15f,
                })
                .ToArray();
        }

        private static void AddForestRegion(Terrain terrain, List<TreeInstance> instances, System.Random random, Rect worldRect, int count)
        {
            var terrainData = terrain.terrainData;
            var prototypeCount = terrainData.treePrototypes.Length;
            if (prototypeCount == 0)
            {
                return;
            }

            var added = 0;
            var attempts = 0;
            while (added < count && attempts < count * 12)
            {
                attempts++;
                var worldX = Mathf.Lerp(worldRect.xMin, worldRect.xMax, (float)random.NextDouble());
                var worldZ = Mathf.Lerp(worldRect.yMin, worldRect.yMax, (float)random.NextDouble());
                if (IsExcludedForestArea(worldX, worldZ))
                {
                    continue;
                }

                var normalizedX = Mathf.InverseLerp(-1000f, 1000f, worldX);
                var normalizedZ = Mathf.InverseLerp(-1000f, 1000f, worldZ);
                var normalizedY = terrainData.GetInterpolatedHeight(normalizedX, normalizedZ) / terrainData.size.y;
                var steepness = terrainData.GetSteepness(normalizedX, normalizedZ);
                if (steepness > 42f)
                {
                    continue;
                }

                instances.Add(new TreeInstance
                {
                    prototypeIndex = random.Next(0, prototypeCount),
                    position = new Vector3(normalizedX, normalizedY, normalizedZ),
                    widthScale = Mathf.Lerp(0.8f, 1.25f, (float)random.NextDouble()),
                    heightScale = Mathf.Lerp(0.9f, 1.4f, (float)random.NextDouble()),
                    rotation = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble()),
                    color = Color.Lerp(new Color(0.82f, 0.82f, 0.82f), Color.white, (float)random.NextDouble()),
                    lightmapColor = Color.white,
                });
                added++;
            }
        }

        private static bool IsExcludedForestArea(float worldX, float worldZ)
        {
            if (worldX > -260f && worldX < 360f && worldZ > -180f && worldZ < 320f)
            {
                return true;
            }

            if (worldX > 260f && worldX < 560f && worldZ > -420f && worldZ < -100f)
            {
                return true;
            }

            if (worldX > 120f && worldX < 310f && worldZ > 80f && worldZ < 280f)
            {
                return true;
            }

            return false;
        }

        private static void EnsureWaterPresentation(Transform worldShell)
        {
            var waterMaterial = AssetDatabase.LoadAssetAtPath<Material>(WaterMaterialPath) ??
                AssetDatabase.LoadAssetAtPath<Material>(BlueMaterialPath);
            if (waterMaterial == null)
            {
                Debug.LogWarning("MainTown dramatic terrain redesign could not find a reusable water material.");
                return;
            }

            var riverRoot = EnsureChildRoot(worldShell, "Water_RiverChannel", new Vector3(0f, 0f, 0f));
            EnsureWaterSegment(riverRoot, "RiverSegment_A", waterMaterial, new Vector3(-575f, 5.35f, 500f), new Vector3(120f, 0.25f, 180f), 13f);
            EnsureWaterSegment(riverRoot, "RiverSegment_B", waterMaterial, new Vector3(-465f, 5.2f, -10f), new Vector3(110f, 0.25f, 360f), 12f);
            EnsureWaterSegment(riverRoot, "RiverSegment_C", waterMaterial, new Vector3(-345f, 5.05f, -560f), new Vector3(95f, 0.25f, 230f), 11f);

            var reservoirRoot = EnsureChildRoot(worldShell, "Water_ReservoirBasin", new Vector3(0f, 0f, 0f));
            EnsureWaterSegment(reservoirRoot, "ReservoirSurface", waterMaterial, new Vector3(-620f, 5.4f, 520f), new Vector3(250f, 0.25f, 190f), 8f);
        }

        private static void RemoveWaterPresentation(Transform worldShell)
        {
            foreach (var waterRootName in new[] { "Water_RiverChannel", "Water_ReservoirBasin" })
            {
                var waterRoot = worldShell.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(child => child.name == waterRootName);

                if (waterRoot != null)
                {
                    waterRoot.gameObject.SetActive(false);
                }
            }
        }

        private static Transform EnsureChildRoot(Transform parent, string name, Vector3 localPosition)
        {
            var existing = parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == name);
            if (existing != null)
            {
                existing.localPosition = localPosition;
                existing.localRotation = Quaternion.identity;
                existing.localScale = Vector3.one;
                existing.gameObject.SetActive(true);
                return existing;
            }

            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;
            return gameObject.transform;
        }

        private static void EnsureWaterSegment(Transform parent, string name, Material material, Vector3 localPosition, Vector3 localScale, float yRotation)
        {
            var existing = parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == name);
            GameObject gameObject;
            if (existing != null)
            {
                gameObject = existing.gameObject;
            }
            else
            {
                gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gameObject.name = name;
                gameObject.transform.SetParent(parent, false);
                var collider = gameObject.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }

            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            gameObject.transform.localScale = localScale;

            var renderer = gameObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private readonly struct LayerSpec
        {
            public LayerSpec(string assetName, string diffuseTexturePath, string normalTexturePath, Vector2 tileSize)
            {
                AssetName = assetName;
                DiffuseTexturePath = diffuseTexturePath;
                NormalTexturePath = normalTexturePath;
                TileSize = tileSize;
            }

            public string AssetName { get; }
            public string DiffuseTexturePath { get; }
            public string NormalTexturePath { get; }
            public Vector2 TileSize { get; }
        }
    }
}
