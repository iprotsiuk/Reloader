using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.Core.Persistence
{
    public static class WorldObjectPersistenceRuntimeBridge
    {
        private static bool _isInitialized;
        private static WorldObjectStateStore _stateStore = new WorldObjectStateStore();
        private static WorldScenePolicyRegistry _policyRegistry = new WorldScenePolicyRegistry();
        private static ReclaimStorageService _reclaimStorage = new ReclaimStorageService();
        private static WorldCleanupService _cleanupService = new WorldCleanupService();
        private static WorldObjectStateApplyService _applyService = new WorldObjectStateApplyService();
        private static Func<Scene, WorldObjectStateRecord, bool> _runtimeSpawnRestorer;

        public static WorldObjectStateStore StateStore => _stateStore;
        public static ReclaimStorageService ReclaimStorage => _reclaimStorage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForDomainReload()
        {
            ResetForTests();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInitializedBeforeFirstSceneLoad()
        {
            EnsureInitialized();
        }

        public static void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            _isInitialized = true;

            var activeScene = SceneManager.GetActiveScene();
            TryApplyForScene(activeScene);
        }

        public static void ResetForTests()
        {
            if (_isInitialized)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }

            _isInitialized = false;
            _stateStore = new WorldObjectStateStore();
            _policyRegistry = new WorldScenePolicyRegistry();
            _reclaimStorage = new ReclaimStorageService();
            _cleanupService = new WorldCleanupService();
            _applyService = new WorldObjectStateApplyService();
            _runtimeSpawnRestorer = null;
        }

        public static void RegisterScenePolicy(WorldScenePersistencePolicy policy)
        {
            _policyRegistry.Register(policy);
        }

        public static void MarkConsumed(string scenePath, string objectId)
        {
            if (string.IsNullOrWhiteSpace(scenePath) || string.IsNullOrWhiteSpace(objectId))
            {
                return;
            }

            if (_stateStore.TryGet(scenePath, objectId, out var existingRecord) && existingRecord != null)
            {
                existingRecord.Consumed = true;
                _stateStore.Upsert(scenePath, existingRecord);
                return;
            }

            _stateStore.Upsert(scenePath, new WorldObjectStateRecord
            {
                ObjectId = objectId,
                Consumed = true
            });
        }

        public static int ProcessDayBoundary(int previousDay, int currentDay)
        {
            return _cleanupService.CleanupDailyResetForDayChange(previousDay, currentDay, _stateStore, _policyRegistry, _reclaimStorage);
        }

        public static void RegisterRuntimeSpawnRestorer(Func<Scene, WorldObjectStateRecord, bool> restorer)
        {
            _runtimeSpawnRestorer = restorer;
        }

        public static void MarkRuntimeSpawned(
            string scenePath,
            string objectId,
            string itemDefinitionId,
            int quantity,
            Vector3 position,
            Quaternion rotation,
            string runtimeDropInstanceId = null)
        {
            if (string.IsNullOrWhiteSpace(scenePath)
                || string.IsNullOrWhiteSpace(objectId)
                || string.IsNullOrWhiteSpace(itemDefinitionId))
            {
                return;
            }

            _stateStore.TryGet(scenePath, objectId, out var existingRecord);

            var resolvedInstanceId = !string.IsNullOrWhiteSpace(runtimeDropInstanceId)
                ? runtimeDropInstanceId
                : !string.IsNullOrWhiteSpace(existingRecord?.ItemInstanceId)
                    ? existingRecord.ItemInstanceId
                    : BuildFallbackRuntimeDropInstanceId(scenePath, objectId, itemDefinitionId);

            _stateStore.Upsert(scenePath, new WorldObjectStateRecord
            {
                ObjectId = objectId,
                Consumed = existingRecord != null && existingRecord.Consumed,
                Destroyed = existingRecord != null && existingRecord.Destroyed,
                HasTransformOverride = true,
                Position = position,
                Rotation = rotation,
                ItemInstanceId = resolvedInstanceId,
                ItemDefinitionId = itemDefinitionId,
                StackQuantity = Mathf.Max(1, quantity)
            });
        }

        private static string BuildFallbackRuntimeDropInstanceId(string scenePath, string objectId, string itemDefinitionId)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "drop:{0}:{1}:{2}",
                scenePath.Trim(),
                objectId.Trim(),
                itemDefinitionId.Trim());
        }

        private static void OnSceneLoaded(Scene loadedScene, LoadSceneMode mode)
        {
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                return;
            }

            TryApplyForScene(loadedScene);
        }

        private static void TryApplyForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(scene.path))
            {
                return;
            }

            _applyService.ApplyForScene(scene, _stateStore, _policyRegistry, _runtimeSpawnRestorer);
        }
    }
}
