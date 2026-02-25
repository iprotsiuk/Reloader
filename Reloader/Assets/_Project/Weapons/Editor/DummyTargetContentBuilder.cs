#if UNITY_EDITOR
using Reloader.Weapons.World;
using UnityEditor;
using UnityEngine;

namespace Reloader.Weapons.Editor
{
    public static class DummyTargetContentBuilder
    {
        private const string TargetPrefabPath = "Assets/_Project/Weapons/Prefabs/RoundDummyTarget.prefab";
        private const string MarkerPrefabPath = "Assets/_Project/Weapons/Prefabs/TargetImpactMarker.prefab";
        private const string MaterialsDir = "Assets/_Project/Weapons/Materials";
        private const string DummyBaseMatPath = MaterialsDir + "/DummyTarget_Base_URP.mat";
        private const string DummyFaceMatPath = MaterialsDir + "/DummyTarget_Face_URP.mat";
        private const string DummyMarkerMatPath = MaterialsDir + "/DummyTarget_Marker_URP.mat";

        [MenuItem("Reloader/Weapons/Build Dummy Target Content")]
        public static void BuildDummyTargetContent()
        {
            var markerPrefab = BuildMarkerPrefab();
            BuildTargetPrefab(markerPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dummy target content built.");
        }

        private static GameObject BuildMarkerPrefab()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "TargetImpactMarker";
            root.transform.localScale = new Vector3(0.035f, 0.035f, 0.005f);
            Object.DestroyImmediate(root.GetComponent<Collider>());
            root.AddComponent<TargetImpactMarker>();

            var renderer = root.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetOrCreateMaterial(
                    DummyMarkerMatPath,
                    new Color(0.9f, 0.15f, 0.1f, 1f));
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, MarkerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildTargetPrefab(GameObject markerPrefab)
        {
            EnsureDir(MaterialsDir);
            var root = new GameObject("RoundDummyTarget");
            root.name = "RoundDummyTarget";
            
            var board = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            board.name = "TargetBoard";
            board.transform.SetParent(root.transform, false);
            board.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            board.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            board.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
            Object.DestroyImmediate(board.GetComponent<Collider>());

            var baseRenderer = board.GetComponent<Renderer>();
            if (baseRenderer != null)
            {
                baseRenderer.sharedMaterial = GetOrCreateMaterial(
                    DummyBaseMatPath,
                    new Color(0.82f, 0.72f, 0.56f, 1f));
            }

            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.2f, 0f);
            collider.size = new Vector3(1.8f, 1.8f, 0.18f);

            var bullGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bullGo.name = "TargetFace";
            // Keep face independent from board non-uniform scale to avoid shear/stretch.
            bullGo.transform.SetParent(root.transform, false);
            bullGo.transform.localPosition = new Vector3(0f, 1.2f, 0.091f);
            bullGo.transform.localRotation = Quaternion.identity;
            bullGo.transform.localScale = new Vector3(1.575f, 1.575f, 1f);
            Object.DestroyImmediate(bullGo.GetComponent<Collider>());

            var bullRenderer = bullGo.GetComponent<Renderer>();
            if (bullRenderer != null)
            {
                bullRenderer.sharedMaterial = GetOrCreateMaterial(
                    DummyFaceMatPath,
                    new Color(0.95f, 0.9f, 0.78f, 1f));
            }

            var markersRoot = new GameObject("ImpactMarkers");
            markersRoot.transform.SetParent(root.transform, false);

            var damageable = root.AddComponent<DummyTargetDamageable>();
            var so = new SerializedObject(damageable);
            so.FindProperty("_impactMarkerPrefab").objectReferenceValue = markerPrefab;
            so.FindProperty("_markersRoot").objectReferenceValue = markersRoot.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, TargetPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static Material GetOrCreateMaterial(string path, Color baseColor)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureDir(string path)
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
