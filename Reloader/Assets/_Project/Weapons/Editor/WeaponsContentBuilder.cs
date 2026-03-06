#if UNITY_EDITOR
using System.IO;
using Reloader.Core.Items;
using Reloader.Inventory;
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
        private const string InventoryItemsDir = "Assets/_Project/Inventory/Data/Items";
        private const string InventorySpawnsDir = "Assets/_Project/Inventory/Data/Spawns";
        private const string SourceRiflePrefabPath = "Assets/Low Poly Weapon Pack 4_WWII_1/Prefabs/Weapons/WWII_Recon_A.prefab";
        private const string SourcePistolPrefabPath = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Prefabs/Weapons/P_LPSP_WEP_Handgun_03.prefab";
        private static readonly string[] PackWeaponMaterialPaths =
        {
            "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Art/Materials/Weapons/M_WEP_Basic_039.mat",
            "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Art/Materials/Weapons/M_WEP_Camo_001.mat",
            "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Art/Materials/Weapons/M_WEP_CarbonFibre_001.mat",
            "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Art/Materials/Weapons/M_WEP_Steel_Brushed_01.mat",
            "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Art/Materials/Weapons/M_WEP_Basic_006.mat",
            "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Art/Materials/Weapons/M_WEP_Basic_008.mat",
            "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Art/Materials/Weapons/M_WEP_Basic_043.mat",
            "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Art/Materials/Weapons/M_WEP_Basic_005.mat"
        };

        [MenuItem("Reloader/Weapons/Build Starter Weapon Content")]
        public static void BuildStarterRifleContent()
        {
            EnsureDir(PrefabsDir);
            EnsureDir(DataDir);
            EnsureDir(InventoryItemsDir);
            EnsureDir(InventorySpawnsDir);
            EnsurePackWeaponMaterialsCompatible();

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
            BuildViewPrefab("RifleView", sourceRiflePrefab, new Vector3(0f, 0.08f, 0.72f));
            BuildPickupPrefab(
                "RiflePickup",
                "weapon-kar98k",
                sourceRiflePrefab,
                new Vector3(0.06f, 0.29f, 1.45f),
                new Vector3(0f, 0.02f, 0.28f));
            BuildDefinitionAsset(
                "StarterRifle.asset",
                "weapon-kar98k",
                "Kar98k (.308)",
                5,
                0.2f,
                120f,
                1f,
                35f,
                220f,
                0,
                0,
                false,
                "ammo-factory-308-147-fmj");
            EnsureInventoryItemAndSpawn(
                "Rifle_308_Starter",
                "weapon-kar98k",
                ItemCategory.Weapon,
                ItemStackPolicy.NonStackable,
                1,
                sourceRiflePrefab,
                "Kar98k (.308)",
                1);

            if (sourcePistolPrefab != null)
            {
                BuildViewPrefab("PistolView", sourcePistolPrefab, new Vector3(0f, 0.07f, 0.42f));
                BuildPickupPrefab("PistolPickup", "weapon-canik-tp9", sourcePistolPrefab, new Vector3(0.5f, 0.26f, 0.2f));
                BuildDefinitionAsset(
                    "StarterPistol.asset",
                    "weapon-canik-tp9",
                    "Canik TP9 (9mm)",
                    12,
                    0.13f,
                    95f,
                    1f,
                    22f,
                    90f,
                    1,
                    24,
                    true,
                    "ammo-factory-9x19-124-fmj");
                EnsureInventoryItemAndSpawn(
                    "Pistol_9x19_Starter",
                    "weapon-canik-tp9",
                    ItemCategory.Weapon,
                    ItemStackPolicy.NonStackable,
                    1,
                    sourcePistolPrefab,
                    "Canik TP9 (9mm)",
                    1);
            }

            EnsureInventoryItemAndSpawn(
                "Cartridge_308_147_FMJ_PMC_Bronze",
                "ammo-factory-308-147-fmj",
                ItemCategory.Bullet,
                ItemStackPolicy.StackByDefinition,
                999,
                sourceRiflePrefab,
                "Factory .308 147gr FMJ",
                120);
            EnsureInventoryItemAndSpawn(
                "Ammo_Factory_9x19_124_FMJ",
                "ammo-factory-9x19-124-fmj",
                ItemCategory.Bullet,
                ItemStackPolicy.StackByDefinition,
                999,
                sourcePistolPrefab != null ? sourcePistolPrefab : sourceRiflePrefab,
                "Factory 9mm 124gr FMJ",
                90);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(sourcePistolPrefab != null
                ? "Starter rifle + pistol content built under Assets/_Project/Weapons."
                : "Starter rifle content built under Assets/_Project/Weapons (pistol skipped).");
        }

        private static void EnsurePackWeaponMaterialsCompatible()
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                return;
            }

            for (var i = 0; i < PackWeaponMaterialPaths.Length; i++)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(PackWeaponMaterialPaths[i]);
                if (material == null)
                {
                    continue;
                }

                if (material.shader == urpLit)
                {
                    continue;
                }

                var mainTex = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                var color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                var bumpMap = material.HasProperty("_BumpMap") ? material.GetTexture("_BumpMap") : null;

                material.shader = urpLit;

                if (material.HasProperty("_BaseMap") && mainTex != null)
                {
                    material.SetTexture("_BaseMap", mainTex);
                }

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }

                if (material.HasProperty("_BumpMap") && bumpMap != null)
                {
                    material.SetTexture("_BumpMap", bumpMap);
                    if (material.HasProperty("_BumpScale"))
                    {
                        material.SetFloat("_BumpScale", 1f);
                    }
                }

                EditorUtility.SetDirty(material);
            }
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

        private static void BuildPickupPrefab(
            string prefabName,
            string itemId,
            GameObject sourceWeaponPrefab,
            Vector3 colliderSize,
            Vector3? colliderCenter = null)
        {
            var root = new GameObject(prefabName);
            var pickup = root.AddComponent<WeaponPickupTarget>();
            pickup.SetItemIdForTests(itemId);

            var collider = root.AddComponent<BoxCollider>();
            collider.size = colliderSize;
            collider.center = colliderCenter ?? Vector3.zero;

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
            bool startingChamberLoaded,
            string ammoItemId)
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
                startingChamberLoaded,
                ammoItemId: ammoItemId);

            EditorUtility.SetDirty(definition);
        }

        private static void EnsureInventoryItemAndSpawn(
            string baseName,
            string itemId,
            ItemCategory category,
            ItemStackPolicy stackPolicy,
            int maxStack,
            GameObject iconSourcePrefab,
            string displayName,
            int spawnQuantity)
        {
            var itemPath = InventoryItemsDir + "/" + baseName + ".asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item, itemPath);
            }

            item.SetValuesForTests(
                itemId,
                category,
                displayName,
                stackPolicy,
                maxStack,
                iconSourcePrefab);
            EditorUtility.SetDirty(item);

            var spawnPath = InventorySpawnsDir + "/" + baseName + "_Spawn.asset";
            var spawn = AssetDatabase.LoadAssetAtPath<ItemSpawnDefinition>(spawnPath);
            if (spawn == null)
            {
                spawn = ScriptableObject.CreateInstance<ItemSpawnDefinition>();
                AssetDatabase.CreateAsset(spawn, spawnPath);
            }

            spawn.SetValuesForTests(item, spawnQuantity, 1f, 0, "{}");
            EditorUtility.SetDirty(spawn);
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
