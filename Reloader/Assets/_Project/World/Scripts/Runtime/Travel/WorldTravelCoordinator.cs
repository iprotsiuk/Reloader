using System.Collections.Generic;
using System;
using System.Reflection;
using Reloader.World.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Travel
{
    public static class WorldTravelCoordinator
    {
        private static string _pendingSceneName;
        private static string _pendingEntryPointId;
        private static bool _isSubscribedToSceneLoaded;
        private static float _travelSuppressedUntilRealtime;
        private static Dictionary<string, int> _pendingInventoryQuantities = new();

        public static string LastResolvedEntryPointId { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _pendingSceneName = null;
            _pendingEntryPointId = null;
            LastResolvedEntryPointId = null;
            _isSubscribedToSceneLoaded = false;
            _travelSuppressedUntilRealtime = 0f;
            _pendingInventoryQuantities = new Dictionary<string, int>();
        }

        public static bool TryTravel(TravelContext context)
        {
            if (context == null)
            {
                return false;
            }

            if (Time.realtimeSinceStartup < _travelSuppressedUntilRealtime)
            {
                return false;
            }

            try
            {
                context.Validate();
            }
            catch (ArgumentException ex)
            {
                Debug.LogWarning($"Travel context validation failed: {ex.Message}");
                return false;
            }

            return TryLoadSceneAtEntry(context.DestinationSceneName, context.DestinationEntryPointId);
        }

        public static bool TryLoadSceneAtEntry(string sceneName, string entryPointId)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || string.IsNullOrWhiteSpace(entryPointId))
            {
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"Travel scene '{sceneName}' is not available in build settings.");
                return false;
            }

            EnsureSubscribed();
            CaptureInventorySnapshotForTravel();
            _pendingSceneName = sceneName.Trim();
            _pendingEntryPointId = entryPointId.Trim();
            LastResolvedEntryPointId = null;
            try
            {
                SceneManager.LoadScene(_pendingSceneName, LoadSceneMode.Single);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load travel scene '{_pendingSceneName}': {ex.Message}");
                _pendingSceneName = null;
                _pendingEntryPointId = null;
                return false;
            }
        }

        private static void EnsureSubscribed()
        {
            if (_isSubscribedToSceneLoaded)
            {
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            _isSubscribedToSceneLoaded = true;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrWhiteSpace(_pendingSceneName) || string.IsNullOrWhiteSpace(_pendingEntryPointId))
            {
                return;
            }

            if (!IsMatchingPendingScene(scene, _pendingSceneName))
            {
                return;
            }

            var candidates = new List<SceneEntryPoint>();
            var allEntryPoints = UnityEngine.Object.FindObjectsByType<SceneEntryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < allEntryPoints.Length; i++)
            {
                var entryPoint = allEntryPoints[i];
                if (entryPoint != null && entryPoint.gameObject.scene == scene)
                {
                    candidates.Add(entryPoint);
                }
            }

            if (SceneEntryPoint.TryFindById(candidates, _pendingEntryPointId, out var resolvedEntryPoint))
            {
                LastResolvedEntryPointId = resolvedEntryPoint.EntryPointId;
                RepositionPlayerToEntryPoint(scene, resolvedEntryPoint.transform);
            }
            else
            {
                Debug.LogWarning($"Travel entry point '{_pendingEntryPointId}' was not found in scene '{scene.name}'.");
            }

            _pendingSceneName = null;
            _pendingEntryPointId = null;
            _travelSuppressedUntilRealtime = Time.realtimeSinceStartup + 1f;
        }

        private static bool IsMatchingPendingScene(Scene loadedScene, string pendingSceneIdentifier)
        {
            if (string.IsNullOrWhiteSpace(pendingSceneIdentifier))
            {
                return false;
            }

            var pending = pendingSceneIdentifier.Trim();
            if (string.Equals(loadedScene.name, pending, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var loadedScenePath = loadedScene.path;
            if (string.IsNullOrWhiteSpace(loadedScenePath))
            {
                return false;
            }

            var normalizedPending = pending.Replace('\\', '/');
            return string.Equals(loadedScenePath, normalizedPending, StringComparison.OrdinalIgnoreCase);
        }

        private static void RepositionPlayerToEntryPoint(Scene scene, Transform entryPointTransform)
        {
            if (entryPointTransform == null)
            {
                return;
            }

            var activeScenePlayerRoot = FindPlayerRootInScene(scene);

            if (activeScenePlayerRoot != null)
            {
                ApplyInventorySnapshotAfterTravel(activeScenePlayerRoot);
                activeScenePlayerRoot.position = entryPointTransform.position;
                activeScenePlayerRoot.rotation = entryPointTransform.rotation;
            }

            if (PersistentPlayerRoot.Instance != null)
            {
                var persistentRootTransform = PersistentPlayerRoot.Instance.transform;
                persistentRootTransform.position = entryPointTransform.position;
                persistentRootTransform.rotation = entryPointTransform.rotation;
            }
        }

        private static void CaptureInventorySnapshotForTravel()
        {
            _pendingInventoryQuantities.Clear();

            var activeScene = SceneManager.GetActiveScene();
            var playerRoot = FindPlayerRootInScene(activeScene);
            if (playerRoot == null)
            {
                return;
            }

            var inventoryController = playerRoot.GetComponent("PlayerInventoryController");
            if (inventoryController == null)
            {
                return;
            }

            var runtime = GetRuntimeFromInventoryController(inventoryController);
            if (runtime == null)
            {
                return;
            }

            var itemIds = new HashSet<string>(StringComparer.Ordinal);
            CollectItemIdsFromRuntimeCollection(runtime, "BeltSlotItemIds", itemIds);
            CollectItemIdsFromRuntimeCollection(runtime, "BackpackItemIds", itemIds);

            var getItemQuantity = runtime.GetType().GetMethod("GetItemQuantity", BindingFlags.Instance | BindingFlags.Public);
            if (getItemQuantity == null)
            {
                return;
            }

            foreach (var itemId in itemIds)
            {
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    continue;
                }

                var quantity = (int)getItemQuantity.Invoke(runtime, new object[] { itemId });
                if (quantity > 0)
                {
                    _pendingInventoryQuantities[itemId] = quantity;
                }
            }
        }

        private static void ApplyInventorySnapshotAfterTravel(Transform playerRootTransform)
        {
            if (_pendingInventoryQuantities == null || _pendingInventoryQuantities.Count == 0 || playerRootTransform == null)
            {
                return;
            }

            var inventoryController = playerRootTransform.GetComponent("PlayerInventoryController");
            if (inventoryController == null)
            {
                _pendingInventoryQuantities.Clear();
                return;
            }

            var runtime = GetRuntimeFromInventoryController(inventoryController);
            if (runtime == null)
            {
                _pendingInventoryQuantities.Clear();
                return;
            }

            var getItemQuantity = runtime.GetType().GetMethod("GetItemQuantity", BindingFlags.Instance | BindingFlags.Public);
            var tryAddStackItem = runtime.GetType().GetMethod("TryAddStackItem", BindingFlags.Instance | BindingFlags.Public);
            var tryStoreItem = runtime.GetType().GetMethod("TryStoreItem", BindingFlags.Instance | BindingFlags.Public);
            if (getItemQuantity == null || (tryAddStackItem == null && tryStoreItem == null))
            {
                _pendingInventoryQuantities.Clear();
                return;
            }

            foreach (var pair in _pendingInventoryQuantities)
            {
                var itemId = pair.Key;
                var targetQuantity = pair.Value;
                if (string.IsNullOrWhiteSpace(itemId) || targetQuantity <= 0)
                {
                    continue;
                }

                var currentQuantity = (int)getItemQuantity.Invoke(runtime, new object[] { itemId });
                var missingQuantity = targetQuantity - currentQuantity;
                if (missingQuantity <= 0)
                {
                    continue;
                }

                if (tryAddStackItem != null)
                {
                    var addArgs = new object[] { itemId, missingQuantity, null, null, null };
                    var added = (bool)tryAddStackItem.Invoke(runtime, addArgs);
                    if (added)
                    {
                        continue;
                    }
                }

                if (tryStoreItem == null)
                {
                    continue;
                }

                for (var i = 0; i < missingQuantity; i++)
                {
                    var storeArgs = new object[] { itemId, null, null, null };
                    var stored = (bool)tryStoreItem.Invoke(runtime, storeArgs);
                    if (!stored)
                    {
                        break;
                    }
                }
            }

            _pendingInventoryQuantities.Clear();
        }

        private static object GetRuntimeFromInventoryController(Component inventoryController)
        {
            if (inventoryController == null)
            {
                return null;
            }

            var runtimeProperty = inventoryController.GetType().GetProperty("Runtime", BindingFlags.Instance | BindingFlags.Public);
            return runtimeProperty?.GetValue(inventoryController);
        }

        private static void CollectItemIdsFromRuntimeCollection(object runtime, string propertyName, ISet<string> sink)
        {
            if (runtime == null || sink == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            var property = runtime.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                return;
            }

            var enumerable = property.GetValue(runtime) as System.Collections.IEnumerable;
            if (enumerable == null)
            {
                return;
            }

            foreach (var entry in enumerable)
            {
                var itemId = entry as string;
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    sink.Add(itemId);
                }
            }
        }

        private static Transform FindPlayerRootInScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            var rootObjects = scene.GetRootGameObjects();
            for (var i = 0; i < rootObjects.Length; i++)
            {
                var root = rootObjects[i];
                if (root != null && root.name == "PlayerRoot")
                {
                    return root.transform;
                }
            }

            return null;
        }
    }
}
