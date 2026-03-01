using System;
using UnityEngine;

namespace Reloader.Reloading.World
{
    public sealed class TemporaryReloadingSuppliesSpawner : MonoBehaviour
    {
        [SerializeField] private bool _spawnOnStart = true;
        [SerializeField] private float _forwardDistance = 2.25f;
        [SerializeField] private float _sideSpacing = 0.8f;
        [SerializeField] private float _upOffset = 0.1f;

        private static readonly string[] PickupPrefabNames =
        {
            "Casing_308_Pickup",
            "Bullet_308_147_FMJ_Pickup",
            "PrimerBox_LR_PMCBronze_Pickup",
            "PowderCan_PMCBronze_308_Pickup"
        };

        private static bool s_spawnedForSession;
        private static bool s_bootstrapped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (s_bootstrapped)
            {
                return;
            }

            if (FindFirstObjectByType<TemporaryReloadingSuppliesSpawner>(FindObjectsInactive.Include) != null)
            {
                s_bootstrapped = true;
                return;
            }

            var gameObject = new GameObject(nameof(TemporaryReloadingSuppliesSpawner));
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<TemporaryReloadingSuppliesSpawner>();
            s_bootstrapped = true;
        }

        private void Start()
        {
            if (!_spawnOnStart || s_spawnedForSession)
            {
                return;
            }

            if (!TryResolvePlayerRoot(out var playerRoot))
            {
                return;
            }

            var basePosition = playerRoot.position + playerRoot.forward * _forwardDistance + Vector3.up * _upOffset;
            var right = playerRoot.right;

            for (var i = 0; i < PickupPrefabNames.Length; i++)
            {
                var prefab = FindPrefabAsset(PickupPrefabNames[i]);
                if (prefab == null)
                {
                    continue;
                }

                var offsetIndex = i - (PickupPrefabNames.Length - 1) * 0.5f;
                var spawnPosition = basePosition + right * (offsetIndex * _sideSpacing);
                Instantiate(prefab, spawnPosition, Quaternion.identity);
            }

            s_spawnedForSession = true;
        }

        private static bool TryResolvePlayerRoot(out Transform playerRoot)
        {
            playerRoot = null;
            var byName = GameObject.Find("PlayerRoot");
            if (byName != null)
            {
                playerRoot = byName.transform;
                return true;
            }

            return false;
        }

        private static GameObject FindPrefabAsset(string prefabName)
        {
            if (string.IsNullOrWhiteSpace(prefabName))
            {
                return null;
            }

            var allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (var i = 0; i < allGameObjects.Length; i++)
            {
                var candidate = allGameObjects[i];
                if (candidate == null)
                {
                    continue;
                }

                // Prefab assets are not part of a loaded scene and can be instantiated directly.
                if (candidate.scene.IsValid())
                {
                    continue;
                }

                if (!string.Equals(candidate.name, prefabName, StringComparison.Ordinal))
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }
    }
}
