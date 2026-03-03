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
        private const string SourcePistolPrefabPath = "Assets/ThirdParty/Polygon-Mega Weapone Kit/Prefabs/SM_Army_Pistol.prefab";

        [MenuItem("Reloader/Weapons/Build Starter Weapon Content")]
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

            var sourcePistolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePistolPrefabPath);
            if (sourcePistolPrefab == null)
            {
                Debug.LogWarning($"Source pistol prefab not found, building rifle-only starter content: {SourcePistolPrefabPath}");
            }

            BuildProjectilePrefab();
            BuildViewPrefab("RifleView", sourceRiflePrefab, new Vector3(0f, 0.1f, 1f));
            BuildPickupPrefab("RiflePickup", "weapon-rifle-01", sourceRiflePrefab, new Vector3(1.2f, 0.4f, 0.3f));
            BuildDefinitionAsset(
                "StarterRifle.asset",
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

            if (sourcePistolPrefab != null)
            {
                BuildViewPrefab("PistolView", sourcePistolPrefab, new Vector3(0f, 0.08f, 0.55f));
                BuildPickupPrefab("PistolPickup", "weapon-pistol-01", sourcePistolPrefab, new Vector3(0.7f, 0.3f, 0.25f));
                BuildDefinitionAsset(
                    "StarterPistol.asset",
                    "weapon-pistol-01",
                    "Starter Pistol",
                    12,
                    0.13f,
                    95f,
                    1f,
                    22f,
                    90f,
                    1,
                    24,
                    true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(sourcePistolPrefab != null
                ? "Starter rifle + pistol content built under Assets/_Project/Weapons."
                : "Starter rifle content built under Assets/_Project/Weapons (pistol skipped).");
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

        private static void BuildViewPrefab(string prefabName, GameObject sourceWeaponPrefab, Vector3 muzzleLocalPosition)
        {
            var root = new GameObject(prefabName);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(sourceWeaponPrefab);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = muzzleLocalPosition;

            var prefabPath = PrefabsDir + "/" + prefabName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildPickupPrefab(string prefabName, string itemId, GameObject sourceWeaponPrefab, Vector3 colliderSize)
        {
            var root = new GameObject(prefabName);
            var pickup = root.AddComponent<WeaponPickupTarget>();
            pickup.SetItemIdForTests(itemId);

            var collider = root.AddComponent<BoxCollider>();
            collider.size = colliderSize;

            var model = (GameObject)PrefabUtility.InstantiatePrefab(sourceWeaponPrefab);
            model.name = "Visual";
            model.transform.SetParent(root.transform, false);

            var prefabPath = PrefabsDir + "/" + prefabName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildDefinitionAsset(
            string fileName,
            string itemId,
            string displayName,
            int magazineCapacity,
            float fireIntervalSeconds,
            float projectileSpeed,
            float projectileGravityMultiplier,
            float baseDamage,
            float maxRangeMeters,
            int startingMagazineCount,
            int startingReserveCount,
            bool startingChamberLoaded)
        {
            var assetPath = DataDir + "/" + fileName;
            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(assetPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            definition.SetRuntimeValuesForTests(
                itemId,
                displayName,
                magazineCapacity,
                fireIntervalSeconds,
                projectileSpeed,
                projectileGravityMultiplier,
                baseDamage,
                maxRangeMeters,
                startingMagazineCount,
                startingReserveCount,
                startingChamberLoaded);

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
