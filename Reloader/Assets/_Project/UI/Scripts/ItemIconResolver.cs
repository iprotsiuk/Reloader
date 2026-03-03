using System;
using System.Collections.Generic;
using Reloader.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI
{
    internal static class ItemIconResolver
    {
        private const int MaxGeneratedIconCacheSize = 128;
        private static readonly Dictionary<string, Texture2D> GeneratedByItemId = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private static readonly LinkedList<string> GeneratedCacheOrder = new LinkedList<string>();
        private static readonly Dictionary<string, LinkedListNode<string>> GeneratedCacheNodes = new Dictionary<string, LinkedListNode<string>>(StringComparer.Ordinal);
        private static Texture2D _defaultTexture;

        public static StyleBackground ResolveBackground(string itemId)
        {
            if (!string.IsNullOrWhiteSpace(itemId)
                && ItemIconCatalogProvider.Catalog != null
                && ItemIconCatalogProvider.Catalog.TryGetIcon(itemId, out var sprite)
                && sprite != null)
            {
                return new StyleBackground(sprite);
            }

            if (string.IsNullOrWhiteSpace(itemId))
            {
                return new StyleBackground(GetDefaultTexture());
            }

            return new StyleBackground(GetOrCreateGeneratedTexture(itemId));
        }

        private static Texture2D GetOrCreateGeneratedTexture(string itemId)
        {
            if (GeneratedByItemId.TryGetValue(itemId, out var cached))
            {
                if (cached != null)
                {
                    MarkGeneratedCacheAsMostRecent(itemId);
                    return cached;
                }

                // Stale null entry, likely after external destruction. Clean tracking and regenerate.
                GeneratedByItemId.Remove(itemId);
                RemoveGeneratedCacheTracking(itemId);
            }

            var generated = CreateGeneratedTexture(itemId);
            AddGeneratedTextureToCache(itemId, generated);
            return generated;
        }

        private static void AddGeneratedTextureToCache(string itemId, Texture2D texture)
        {
            GeneratedByItemId[itemId] = texture;
            MarkGeneratedCacheAsMostRecent(itemId);
            EvictGeneratedCacheIfNeeded();
        }

        private static void MarkGeneratedCacheAsMostRecent(string itemId)
        {
            if (GeneratedCacheNodes.TryGetValue(itemId, out var node)
                && node != null
                && node.List == GeneratedCacheOrder)
            {
                if (node != GeneratedCacheOrder.Last)
                {
                    GeneratedCacheOrder.Remove(node);
                    GeneratedCacheOrder.AddLast(node);
                }

                return;
            }

            GeneratedCacheNodes[itemId] = GeneratedCacheOrder.AddLast(itemId);
        }

        private static void EvictGeneratedCacheIfNeeded()
        {
            while (GeneratedByItemId.Count > MaxGeneratedIconCacheSize && GeneratedCacheOrder.First != null)
            {
                var oldestItemId = GeneratedCacheOrder.First.Value;
                GeneratedCacheOrder.RemoveFirst();
                GeneratedCacheNodes.Remove(oldestItemId);

                if (!GeneratedByItemId.TryGetValue(oldestItemId, out var texture))
                {
                    continue;
                }

                GeneratedByItemId.Remove(oldestItemId);
                DestroyTexture(texture);
            }
        }

        private static void RemoveGeneratedCacheTracking(string itemId)
        {
            if (!GeneratedCacheNodes.TryGetValue(itemId, out var node))
            {
                return;
            }

            if (node != null && node.List == GeneratedCacheOrder)
            {
                GeneratedCacheOrder.Remove(node);
            }

            GeneratedCacheNodes.Remove(itemId);
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(texture);
                return;
            }

            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static Texture2D CreateGeneratedTexture(string itemId)
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "GeneratedItemIcon_" + itemId
            };

            var hash = itemId.GetHashCode();
            var hue = Mathf.Abs(hash % 997) / 997f;
            var baseColor = Color.HSVToRGB(hue, 0.35f, 0.78f);
            var accent = Color.HSVToRGB((hue + 0.18f) % 1f, 0.48f, 0.95f);
            var dark = Color.Lerp(baseColor, Color.black, 0.35f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = x / (float)(size - 1);
                    var v = y / (float)(size - 1);
                    var radial = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 1.2f);
                    var checker = ((x / 4) + (y / 4)) % 2 == 0 ? 0.08f : -0.04f;
                    var color = Color.Lerp(dark, baseColor, radial) + new Color(checker, checker, checker, 0f);
                    texture.SetPixel(x, y, color);
                }
            }

            for (var x = 0; x < size; x++)
            {
                texture.SetPixel(x, 0, accent);
                texture.SetPixel(x, size - 1, accent);
            }

            for (var y = 0; y < size; y++)
            {
                texture.SetPixel(0, y, accent);
                texture.SetPixel(size - 1, y, accent);
            }

            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return texture;
        }

        private static Texture2D GetDefaultTexture()
        {
            if (_defaultTexture != null)
            {
                return _defaultTexture;
            }

            const int size = 32;
            _defaultTexture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "DefaultItemIcon"
            };

            var background = new Color(0.24f, 0.28f, 0.34f, 1f);
            var mark = new Color(0.82f, 0.86f, 0.91f, 1f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    _defaultTexture.SetPixel(x, y, background);
                }
            }

            for (var i = 7; i < 25; i++)
            {
                _defaultTexture.SetPixel(i, i, mark);
                _defaultTexture.SetPixel(size - 1 - i, i, mark);
            }

            _defaultTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return _defaultTexture;
        }
    }
}
