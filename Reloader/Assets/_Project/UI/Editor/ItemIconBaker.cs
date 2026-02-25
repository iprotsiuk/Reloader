using System;
using System.Collections.Generic;
using System.IO;
using Reloader.Core.UI;
using Reloader.Economy;
using Reloader.Weapons.Data;
using UnityEditor;
using UnityEngine;

namespace Reloader.UI.Editor
{
    public static class ItemIconBaker
    {
        private const string OutputFolder = "Assets/_Project/UI/Sprites/Items";
        private const string CatalogPath = "Assets/_Project/UI/Data/ItemIconCatalog.asset";
        private const int IconSize = 256;
        private const int CropPaddingPixels = 16;
        private const byte AlphaThreshold = 8;

        [MenuItem("Tools/Reloader/Regenerate Item Icons")]
        public static void RegenerateItemIcons()
        {
            var sources = CollectIconSources();
            if (sources.Count == 0)
            {
                Debug.LogWarning("ItemIconBaker: no icon sources were found.");
                return;
            }

            EnsureFolder(OutputFolder);

            var generated = 0;
            var skipped = 0;
            var failed = 0;
            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (!TryBakeIcon(source, out var error))
                {
                    failed++;
                    Debug.LogWarning($"ItemIconBaker: failed to bake '{source.ItemId}' from '{source.SourcePath}'. {error}");
                    continue;
                }

                generated++;
            }

            AssetDatabase.Refresh();
            BuildCatalogFromFolder();
            skipped = sources.Count - generated - failed;
            Debug.Log($"ItemIconBaker: generated={generated}, skipped={Mathf.Max(0, skipped)}, failed={failed}.");
        }

        private static List<IconSource> CollectIconSources()
        {
            var sourcesById = new Dictionary<string, IconSource>(StringComparer.Ordinal);

            var weaponGuids = AssetDatabase.FindAssets("t:WeaponDefinition", new[] { "Assets/_Project" });
            for (var i = 0; i < weaponGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(weaponGuids[i]);
                var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
                if (definition == null || string.IsNullOrWhiteSpace(definition.ItemId) || definition.IconSourcePrefab == null)
                {
                    continue;
                }

                sourcesById[definition.ItemId] = new IconSource(definition.ItemId, definition.IconSourcePrefab, path);
            }

            var shopGuids = AssetDatabase.FindAssets("t:ShopCatalogDefinition", new[] { "Assets/_Project" });
            for (var i = 0; i < shopGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(shopGuids[i]);
                var definition = AssetDatabase.LoadAssetAtPath<ShopCatalogDefinition>(path);
                if (definition == null || definition.Items == null)
                {
                    continue;
                }

                var items = definition.Items;
                for (var itemIndex = 0; itemIndex < items.Count; itemIndex++)
                {
                    var item = items[itemIndex];
                    if (item == null || string.IsNullOrWhiteSpace(item.ItemId) || item.IconSourcePrefab == null)
                    {
                        continue;
                    }

                    sourcesById[item.ItemId] = new IconSource(item.ItemId, item.IconSourcePrefab, path);
                }
            }

            return new List<IconSource>(sourcesById.Values);
        }

        private static bool TryBakeIcon(IconSource source, out string error)
        {
            error = null;
            GameObject instance = null;
            Camera camera = null;
            Light keyLight = null;
            var renderTexture = new RenderTexture(IconSize, IconSize, 24, RenderTextureFormat.ARGB32);
            var outputPath = $"{OutputFolder}/{source.ItemId}.png";
            var outputAbsolutePath = ToAbsolutePath(outputPath);

            try
            {
                instance = PrefabUtility.InstantiatePrefab(source.Prefab) as GameObject;
                if (instance == null)
                {
                    error = "prefab instantiation returned null";
                    return false;
                }

                instance.hideFlags = HideFlags.HideAndDontSave;
                var bounds = TryComputeBounds(instance, out var computedBounds)
                    ? computedBounds
                    : new Bounds(Vector3.zero, Vector3.one);

                var cameraGo = new GameObject($"ItemIconCamera_{source.ItemId}");
                cameraGo.hideFlags = HideFlags.HideAndDontSave;
                camera = cameraGo.AddComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.targetTexture = renderTexture;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;

                var lightGo = new GameObject($"ItemIconLight_{source.ItemId}");
                lightGo.hideFlags = HideFlags.HideAndDontSave;
                keyLight = lightGo.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.intensity = 1.15f;
                keyLight.color = Color.white;
                lightGo.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

                PositionCamera(camera.transform, bounds);
                camera.Render();

                var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
                var previousActive = RenderTexture.active;
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, IconSize, IconSize), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previousActive;

                var finalTexture = TightCropToSquare(texture);
                var png = finalTexture.EncodeToPNG();
                File.WriteAllBytes(outputAbsolutePath, png);
                UnityEngine.Object.DestroyImmediate(finalTexture);
                UnityEngine.Object.DestroyImmediate(texture);

                ConfigureTextureImporter(outputPath);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }

                if (camera != null)
                {
                    UnityEngine.Object.DestroyImmediate(camera.gameObject);
                }

                if (keyLight != null)
                {
                    UnityEngine.Object.DestroyImmediate(keyLight.gameObject);
                }

                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static bool TryComputeBounds(GameObject root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            var hasBounds = false;
            bounds = default;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                bounds = new Bounds(root.transform.position, Vector3.one);
            }

            return hasBounds;
        }

        private static void PositionCamera(Transform cameraTransform, Bounds bounds)
        {
            var direction = new Vector3(-1f, 0.85f, -1f).normalized;
            var center = bounds.center;
            var distance = Mathf.Max(bounds.extents.magnitude * 3.5f, 2f);

            cameraTransform.position = center - (direction * distance);
            cameraTransform.LookAt(center);

            var cam = cameraTransform.GetComponent<Camera>();
            if (cam == null)
            {
                return;
            }

            var size = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 1.35f;
            cam.orthographicSize = Mathf.Max(0.1f, size);
        }

        private static Texture2D TightCropToSquare(Texture2D source)
        {
            if (source == null)
            {
                return source;
            }

            var pixels = source.GetPixels32();
            var width = source.width;
            var height = source.height;
            var minX = width;
            var minY = height;
            var maxX = -1;
            var maxY = -1;

            for (var y = 0; y < height; y++)
            {
                var rowOffset = y * width;
                for (var x = 0; x < width; x++)
                {
                    if (pixels[rowOffset + x].a <= AlphaThreshold)
                    {
                        continue;
                    }

                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return source;
            }

            minX = Mathf.Max(0, minX - CropPaddingPixels);
            minY = Mathf.Max(0, minY - CropPaddingPixels);
            maxX = Mathf.Min(width - 1, maxX + CropPaddingPixels);
            maxY = Mathf.Min(height - 1, maxY + CropPaddingPixels);

            var cropWidth = maxX - minX + 1;
            var cropHeight = maxY - minY + 1;
            var cropPixels = source.GetPixels(minX, minY, cropWidth, cropHeight);

            var output = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            var fill = new Color[IconSize * IconSize];
            for (var i = 0; i < fill.Length; i++)
            {
                fill[i] = clear;
            }

            output.SetPixels(fill);

            var targetSize = Mathf.Max(cropWidth, cropHeight);
            var offsetX = (targetSize - cropWidth) / 2;
            var offsetY = (targetSize - cropHeight) / 2;
            var square = new Texture2D(targetSize, targetSize, TextureFormat.RGBA32, false);
            var squareFill = new Color[targetSize * targetSize];
            for (var i = 0; i < squareFill.Length; i++)
            {
                squareFill[i] = clear;
            }

            square.SetPixels(squareFill);
            square.SetPixels(offsetX, offsetY, cropWidth, cropHeight, cropPixels);
            square.Apply(false, false);

            for (var y = 0; y < IconSize; y++)
            {
                for (var x = 0; x < IconSize; x++)
                {
                    var u = x / (float)(IconSize - 1);
                    var v = y / (float)(IconSize - 1);
                    output.SetPixel(x, y, square.GetPixelBilinear(u, v));
                }
            }

            output.Apply(false, false);
            UnityEngine.Object.DestroyImmediate(square);
            return output;
        }

        private static void BuildCatalogFromFolder()
        {
            EnsureFolder("Assets/_Project/UI/Data");

            var catalog = AssetDatabase.LoadAssetAtPath<ItemIconCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ItemIconCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { OutputFolder });
            var entries = new List<ItemIconCatalog.Entry>();
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    continue;
                }

                var itemId = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    continue;
                }

                entries.Add(new ItemIconCatalog.Entry(itemId, sprite));
            }

            catalog.ReplaceEntriesForEditor(entries);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureTextureImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            var absolutePath = ToAbsolutePath(assetFolderPath);
            Directory.CreateDirectory(absolutePath);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return assetPath;
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            return Path.Combine(projectRoot, assetPath);
        }

        private readonly struct IconSource
        {
            public IconSource(string itemId, GameObject prefab, string sourcePath)
            {
                ItemId = itemId;
                Prefab = prefab;
                SourcePath = sourcePath;
            }

            public string ItemId { get; }
            public GameObject Prefab { get; }
            public string SourcePath { get; }
        }
    }
}
