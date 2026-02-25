#if UNITY_EDITOR
using System.IO;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Data;
using Reloader.Weapons.World;
using UnityEditor;
using UnityEngine;

namespace Reloader.Weapons.Editor
{
    public static class WeaponsContentBuilder
    {
        private const string WeaponsRoot = "Assets/_Project/Weapons";
        private const string PrefabsDir = WeaponsRoot + "/Prefabs";
        private const string DataDir = WeaponsRoot + "/Data/Weapons";
        private const string SourceRiflePrefabPath = "Assets/ThirdParty/Polygon-Mega Weapone Kit/Prefabs/SM_Army_Sniper_Rifle.prefab";

        [MenuItem("Reloader/Weapons/Build Starter Rifle Content")]
        public static void BuildStarterRifleContent()
        {
            EnsureDir(PrefabsDir);
            EnsureDir(DataDir);

            var sourceRiflePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceRiflePrefabPath);
            if (sourceRiflePrefab == null)
            {
                Debug.LogError($"Source rifle prefab not found: {SourceRiflePrefabPath}");
                return;
            }

            BuildProjectilePrefab();
            BuildRifleViewPrefab(sourceRiflePrefab);
            BuildRiflePickupPrefab(sourceRiflePrefab);
            BuildRifleDefinitionAsset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Starter rifle content built under Assets/_Project/Weapons.");
        }

        private static void BuildProjectilePrefab()
        {
            var go = new GameObject("WeaponProjectile");
            go.AddComponent<SphereCollider>().isTrigger = true;
            go.AddComponent<WeaponProjectile>();

            var prefabPath = PrefabsDir + "/WeaponProjectile.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        private static void BuildRifleViewPrefab(GameObject sourceRiflePrefab)
        {
            var root = new GameObject("RifleView");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(sourceRiflePrefab);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.1f, 1f);

            var prefabPath = PrefabsDir + "/RifleView.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildRiflePickupPrefab(GameObject sourceRiflePrefab)
        {
            var root = new GameObject("RiflePickup");
            var pickup = root.AddComponent<WeaponPickupTarget>();
            pickup.SetItemIdForTests("weapon-rifle-01");

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.2f, 0.4f, 0.3f);

            var model = (GameObject)PrefabUtility.InstantiatePrefab(sourceRiflePrefab);
            model.name = "Visual";
            model.transform.SetParent(root.transform, false);

            var prefabPath = PrefabsDir + "/RiflePickup.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildRifleDefinitionAsset()
        {
            var assetPath = DataDir + "/StarterRifle.asset";
            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(assetPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            definition.SetRuntimeValuesForTests(
                "weapon-rifle-01",
                "Starter Rifle",
                5,
                0.2f,
                120f,
                1f,
                35f,
                220f,
                0,
                0,
                false);

            EditorUtility.SetDirty(definition);
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
