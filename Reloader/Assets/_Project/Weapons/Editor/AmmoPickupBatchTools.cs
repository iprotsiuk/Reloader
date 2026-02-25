#if UNITY_EDITOR
using Reloader.Weapons.World;
using UnityEditor;
using UnityEngine;

namespace Reloader.Weapons.Editor
{
    public static class AmmoPickupBatchTools
    {
        [MenuItem("Reloader/Weapons/Add Ammo Stack Pickup To Selected Prefabs")]
        public static void AddAmmoStackPickupToSelectedPrefabs()
        {
            var selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                Debug.LogWarning("No assets selected. Select one or more ammo prefab assets in the Project window.");
                return;
            }

            var processed = 0;
            var updated = 0;
            var skipped = 0;

            for (var i = 0; i < selectedObjects.Length; i++)
            {
                var assetPath = AssetDatabase.GetAssetPath(selectedObjects[i]);
                if (string.IsNullOrWhiteSpace(assetPath) || !assetPath.EndsWith(".prefab"))
                {
                    skipped++;
                    continue;
                }

                var prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
                if (prefabRoot == null)
                {
                    skipped++;
                    continue;
                }

                processed++;
                var changed = false;

                if (prefabRoot.GetComponent<AmmoStackPickupTarget>() == null)
                {
                    prefabRoot.AddComponent<AmmoStackPickupTarget>();
                    changed = true;
                }

                if (prefabRoot.GetComponentInChildren<Collider>(true) == null)
                {
                    prefabRoot.AddComponent<BoxCollider>();
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                    updated++;
                }
                else
                {
                    skipped++;
                }

                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Ammo pickup batch complete. Processed: {processed}, Updated: {updated}, Skipped: {skipped}");
        }

        [MenuItem("Reloader/Weapons/Add Ammo Stack Pickup To Selected Prefabs", true)]
        private static bool ValidateAddAmmoStackPickupToSelectedPrefabs()
        {
            var selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < selectedObjects.Length; i++)
            {
                var assetPath = AssetDatabase.GetAssetPath(selectedObjects[i]);
                if (!string.IsNullOrWhiteSpace(assetPath) && assetPath.EndsWith(".prefab"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
