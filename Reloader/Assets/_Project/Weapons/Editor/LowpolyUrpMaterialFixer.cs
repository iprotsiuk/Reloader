#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Reloader.Weapons.Editor
{
    public static class LowpolyUrpMaterialFixer
    {
        private static readonly string[] TargetRoots =
        {
            "Assets/ThirdParty/Polygon-Mega Weapone Kit",
            "Assets/Polygon-Mega Survival",
            "Assets/STYLE - Character Customization Kit"
        };

        private const string DummyMaterialsDir = "Assets/_Project/Weapons/Materials";
        private const string DummyBaseMatPath = DummyMaterialsDir + "/DummyTarget_Base_URP.mat";
        private const string DummyFaceMatPath = DummyMaterialsDir + "/DummyTarget_Face_URP.mat";
        private const string DummyMarkerMatPath = DummyMaterialsDir + "/DummyTarget_Marker_URP.mat";

        [MenuItem("Reloader/Weapons/Fix Lowpoly Materials For URP")]
        public static void FixAll()
        {
            var litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
            {
                Debug.LogError("URP Lit shader not found. Ensure URP package is installed.");
                return;
            }

            var converted = 0;
            for (var i = 0; i < TargetRoots.Length; i++)
            {
                converted += ConvertMaterialsUnderRoot(TargetRoots[i], litShader);
            }

            EnsureDummyTargetMaterials(litShader);
            AssignDummyTargetMaterials();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"URP material fix completed. Converted materials: {converted}.");
        }

        private static int ConvertMaterialsUnderRoot(string root, Shader litShader)
        {
            if (!AssetDatabase.IsValidFolder(root))
            {
                return 0;
            }

            var guids = AssetDatabase.FindAssets("t:Material", new[] { root });
            var converted = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    continue;
                }

                if (!ShouldConvert(mat))
                {
                    continue;
                }

                var srcMainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                var srcMainScale = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex") : Vector2.one;
                var srcMainOffset = mat.HasProperty("_MainTex") ? mat.GetTextureOffset("_MainTex") : Vector2.zero;
                var srcColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                var srcEmission = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
                var srcNormal = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                var srcMetallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
                var srcSmoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0f;

                mat.shader = litShader;

                if (srcMainTex != null && mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", srcMainTex);
                    mat.SetTextureScale("_BaseMap", srcMainScale);
                    mat.SetTextureOffset("_BaseMap", srcMainOffset);
                }

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", srcColor);
                }

                if (srcNormal != null && mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", srcNormal);
                }

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", srcEmission);
                }

                if (mat.HasProperty("_Metallic"))
                {
                    mat.SetFloat("_Metallic", srcMetallic);
                }

                if (mat.HasProperty("_Smoothness"))
                {
                    mat.SetFloat("_Smoothness", srcSmoothness);
                }

                EditorUtility.SetDirty(mat);
                converted++;
            }

            return converted;
        }

        private static bool ShouldConvert(Material mat)
        {
            if (mat == null)
            {
                return false;
            }

            var shader = mat.shader;
            if (shader == null)
            {
                return true;
            }

            var name = shader.name ?? string.Empty;
            if (name.StartsWith("Universal Render Pipeline/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (name.Equals("Standard", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Legacy Shaders/", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Nature/Terrain/Standard", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Mobile/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // If material looks built-in style (MainTex/Color) and not URP, convert it.
            return mat.HasProperty("_MainTex") || mat.HasProperty("_Color");
        }

        private static void EnsureDummyTargetMaterials(Shader litShader)
        {
            EnsureFolder(DummyMaterialsDir);

            CreateOrUpdateMaterial(DummyBaseMatPath, litShader, new Color(0.82f, 0.72f, 0.56f, 1f));
            CreateOrUpdateMaterial(DummyFaceMatPath, litShader, new Color(0.95f, 0.9f, 0.78f, 1f));
            CreateOrUpdateMaterial(DummyMarkerMatPath, litShader, new Color(0.9f, 0.15f, 0.1f, 1f));
        }

        private static void CreateOrUpdateMaterial(string path, Shader shader, Color color)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.shader = shader;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            EditorUtility.SetDirty(mat);
        }

        private static void AssignDummyTargetMaterials()
        {
            var baseMat = AssetDatabase.LoadAssetAtPath<Material>(DummyBaseMatPath);
            var faceMat = AssetDatabase.LoadAssetAtPath<Material>(DummyFaceMatPath);
            var markerMat = AssetDatabase.LoadAssetAtPath<Material>(DummyMarkerMatPath);

            var targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Weapons/Prefabs/RoundDummyTarget.prefab");
            if (targetPrefab != null)
            {
                var rootRenderer = targetPrefab.GetComponent<Renderer>();
                if (rootRenderer != null)
                {
                    rootRenderer.sharedMaterial = baseMat;
                    EditorUtility.SetDirty(rootRenderer);
                }

                var face = targetPrefab.transform.Find("TargetFace");
                if (face != null)
                {
                    var faceRenderer = face.GetComponent<Renderer>();
                    if (faceRenderer != null)
                    {
                        faceRenderer.sharedMaterial = faceMat;
                        EditorUtility.SetDirty(faceRenderer);
                    }
                }

                EditorUtility.SetDirty(targetPrefab);
            }

            var markerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Weapons/Prefabs/TargetImpactMarker.prefab");
            if (markerPrefab != null)
            {
                var markerRenderer = markerPrefab.GetComponent<Renderer>();
                if (markerRenderer != null)
                {
                    markerRenderer.sharedMaterial = markerMat;
                    EditorUtility.SetDirty(markerRenderer);
                }

                EditorUtility.SetDirty(markerPrefab);
            }
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
