using System;
using System.Globalization;
using System.Collections.Generic;
using Reloader.Core.Save;
using Reloader.Core.Save.Modules;
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
        internal static Action FinalizeAfterLoadObserverForTests;

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
            FinalizeAfterLoadObserverForTests = null;
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

        public static void PrepareForSave(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            var module = ResolveModule(moduleRegistrations);
            if (module == null)
            {
                return;
            }

            module.ReplaceState(BuildSceneObjectStateRecords(), BuildReclaimRecords());
        }

        public static void FinalizeAfterLoad(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            var module = ResolveModule(moduleRegistrations);
            if (module == null)
            {
                return;
            }

            RestoreStateStore(module);
            RestoreReclaimStorage(module);
            FinalizeAfterLoadObserverForTests?.Invoke();
            TryApplyForScene(SceneManager.GetActiveScene());
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

            if (IsBackupScenePath(scene.path))
            {
                return;
            }

            _applyService.ApplyForScene(scene, _stateStore, _policyRegistry, _runtimeSpawnRestorer);
        }

        private static bool IsBackupScenePath(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return false;
            }

            var normalizedPath = scenePath.Replace('\\', '/');
            return normalizedPath.IndexOf("Temp/__Backupscenes/", StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedPath.EndsWith(".backup", StringComparison.OrdinalIgnoreCase);
        }

        private static WorldObjectStateModule ResolveModule(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            if (moduleRegistrations == null)
            {
                return null;
            }

            for (var i = 0; i < moduleRegistrations.Count; i++)
            {
                if (moduleRegistrations[i]?.Module is WorldObjectStateModule module)
                {
                    return module;
                }
            }

            return null;
        }

        private static List<WorldObjectStateModule.SceneObjectStateRecord> BuildSceneObjectStateRecords()
        {
            var sceneStates = new Dictionary<string, WorldObjectStateModule.SceneObjectStateRecord>(StringComparer.Ordinal);
            var snapshot = _stateStore.Snapshot();
            for (var i = 0; i < snapshot.Count; i++)
            {
                var entry = snapshot[i];
                var scenePath = entry.Key.scenePath;
                var record = entry.Value;
                if (record == null || string.IsNullOrWhiteSpace(scenePath) || string.IsNullOrWhiteSpace(record.ObjectId))
                {
                    continue;
                }

                if (!sceneStates.TryGetValue(scenePath, out var sceneState))
                {
                    sceneState = new WorldObjectStateModule.SceneObjectStateRecord
                    {
                        ScenePath = scenePath
                    };
                    sceneStates.Add(scenePath, sceneState);
                }

                sceneState.Records.Add(new WorldObjectStateModule.WorldObjectRecord
                {
                    ObjectId = record.ObjectId,
                    Consumed = record.Consumed,
                    Destroyed = record.Destroyed,
                    HasTransformOverride = record.HasTransformOverride,
                    PositionX = record.Position.x,
                    PositionY = record.Position.y,
                    PositionZ = record.Position.z,
                    RotationX = record.Rotation.x,
                    RotationY = record.Rotation.y,
                    RotationZ = record.Rotation.z,
                    RotationW = record.Rotation.w,
                    LastUpdatedDay = record.LastUpdatedDay,
                    ItemInstanceId = record.ItemInstanceId ?? string.Empty,
                    ItemDefinitionId = record.ItemDefinitionId ?? string.Empty,
                    StackQuantity = Mathf.Max(1, record.StackQuantity)
                });
            }

            return new List<WorldObjectStateModule.SceneObjectStateRecord>(sceneStates.Values);
        }

        private static List<WorldObjectStateModule.ReclaimRecord> BuildReclaimRecords()
        {
            var records = new List<WorldObjectStateModule.ReclaimRecord>();
            var snapshot = _reclaimStorage.Snapshot();
            for (var i = 0; i < snapshot.Count; i++)
            {
                var entry = snapshot[i];
                if (entry == null)
                {
                    continue;
                }

                records.Add(new WorldObjectStateModule.ReclaimRecord
                {
                    ScenePath = entry.ScenePath ?? string.Empty,
                    ObjectId = entry.ObjectId ?? string.Empty,
                    ItemInstanceId = entry.ItemInstanceId ?? string.Empty,
                    CleanedOnDay = entry.CleanedOnDay
                });
            }

            return records;
        }

        private static void RestoreStateStore(WorldObjectStateModule module)
        {
            _stateStore = new WorldObjectStateStore();
            if (module == null)
            {
                return;
            }

            for (var i = 0; i < module.SceneObjectStates.Count; i++)
            {
                var sceneState = module.SceneObjectStates[i];
                if (sceneState == null || string.IsNullOrWhiteSpace(sceneState.ScenePath) || sceneState.Records == null)
                {
                    continue;
                }

                for (var j = 0; j < sceneState.Records.Count; j++)
                {
                    var record = sceneState.Records[j];
                    if (record == null || string.IsNullOrWhiteSpace(record.ObjectId))
                    {
                        continue;
                    }

                    _stateStore.Upsert(sceneState.ScenePath, new WorldObjectStateRecord
                    {
                        ObjectId = record.ObjectId,
                        Consumed = record.Consumed,
                        Destroyed = record.Destroyed,
                        HasTransformOverride = record.HasTransformOverride,
                        Position = new Vector3(record.PositionX, record.PositionY, record.PositionZ),
                        Rotation = new Quaternion(record.RotationX, record.RotationY, record.RotationZ, record.RotationW),
                        LastUpdatedDay = record.LastUpdatedDay,
                        ItemInstanceId = record.ItemInstanceId ?? string.Empty,
                        ItemDefinitionId = record.ItemDefinitionId ?? string.Empty,
                        StackQuantity = Mathf.Max(1, record.StackQuantity)
                    });
                }
            }
        }

        private static void RestoreReclaimStorage(WorldObjectStateModule module)
        {
            var entries = new List<ReclaimStorageEntry>();
            if (module != null)
            {
                for (var i = 0; i < module.ReclaimEntries.Count; i++)
                {
                    var entry = module.ReclaimEntries[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    entries.Add(new ReclaimStorageEntry
                    {
                        ScenePath = entry.ScenePath ?? string.Empty,
                        ObjectId = entry.ObjectId ?? string.Empty,
                        ItemInstanceId = entry.ItemInstanceId ?? string.Empty,
                        CleanedOnDay = entry.CleanedOnDay
                    });
                }
            }

            _reclaimStorage.Restore(entries);
        }
    }
}
