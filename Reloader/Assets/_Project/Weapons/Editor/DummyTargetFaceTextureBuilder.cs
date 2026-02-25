#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Reloader.Weapons.Editor
{
    public static class DummyTargetFaceTextureBuilder
    {
        private const string TexturesDir = "Assets/_Project/Weapons/Textures";
        private const string TexturePath = TexturesDir + "/DummyTargetFace.png";
        private const string FaceMaterialPath = "Assets/_Project/Weapons/Materials/DummyTarget_Face_URP.mat";

        [MenuItem("Reloader/Weapons/Build Dummy Target Face Texture")]
        public static void BuildAndAssign()
        {
            EnsureFolder(TexturesDir);

            var texture = BuildTexture(1024);
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            File.WriteAllBytes(TexturePath, png);
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = false;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            var targetTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            var faceMaterial = AssetDatabase.LoadAssetAtPath<Material>(FaceMaterialPath);
            if (faceMaterial != null && targetTexture != null)
            {
                if (faceMaterial.HasProperty("_BaseMap"))
                {
                    faceMaterial.SetTexture("_BaseMap", targetTexture);
                }

                if (faceMaterial.HasProperty("_BaseColor"))
                {
                    faceMaterial.SetColor("_BaseColor", Color.white);
                }

                EditorUtility.SetDirty(faceMaterial);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Dummy target face texture generated and assigned: {TexturePath}");
        }

        private static Texture2D BuildTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            var maxR = center * 0.95f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var r = Mathf.Sqrt((dx * dx) + (dy * dy));
                    var t = r / maxR;

                    var color = new Color(0.88f, 0.88f, 0.88f, 1f);
                    if (t <= 1f)
                    {
                        if (t <= 0.09f)
                        {
                            color = new Color(0.92f, 0.1f, 0.1f, 1f);
                        }
                        else if (t <= 0.18f)
                        {
                            color = new Color(0.83f, 0.83f, 0.83f, 1f);
                        }
                        else if (t <= 0.5f)
                        {
                            color = Color.black;
                        }
                    }

                    // Thin concentric guide lines.
                    if (t <= 1f)
                    {
                        const int rings = 12;
                        for (var i = 1; i <= rings; i++)
                        {
                            var ringT = i / (float)rings;
                            if (Mathf.Abs(t - ringT) < 0.0025f)
                            {
                                color = Color.black;
                                break;
                            }
                        }
                    }

                    pixels[(y * size) + x] = color;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var segments = path.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }
}
#endif
