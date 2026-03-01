using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.Core.Persistence
{
    public sealed class WorldObjectStateApplyService
    {
        public int ApplyForScene(
            Scene scene,
            WorldObjectStateStore stateStore,
            WorldScenePolicyRegistry policyRegistry,
            Func<Scene, WorldObjectStateRecord, bool> runtimeSpawnRestorer = null)
        {
            if (stateStore == null)
            {
                throw new ArgumentNullException(nameof(stateStore));
            }

            if (policyRegistry == null)
            {
                throw new ArgumentNullException(nameof(policyRegistry));
            }

            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(scene.path))
            {
                return 0;
            }

            var policy = policyRegistry.ResolvePolicy(scene.path);
            var appliedCount = 0;
            var existingObjectIds = new HashSet<string>(StringComparer.Ordinal);
            var roots = scene.GetRootGameObjects();
            for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                var root = roots[rootIndex];
                if (root == null)
                {
                    continue;
                }

                var identities = root.GetComponentsInChildren<WorldObjectIdentity>(true);
                for (var identityIndex = 0; identityIndex < identities.Length; identityIndex++)
                {
                    var identity = identities[identityIndex];
                    if (identity == null)
                    {
                        continue;
                    }

                    existingObjectIds.Add(identity.ObjectId);

                    if (!stateStore.TryGet(scene.path, identity.ObjectId, out var savedRecord))
                    {
                        continue;
                    }

                    ApplySavedState(identity, savedRecord, policy);
                    appliedCount++;
                }
            }

            if (runtimeSpawnRestorer != null && policy.TrackSpawnedObjects)
            {
                var snapshot = stateStore.Snapshot();
                for (var i = 0; i < snapshot.Count; i++)
                {
                    var key = snapshot[i].Key;
                    if (!string.Equals(key.scenePath, scene.path, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var savedRecord = snapshot[i].Value;
                    if (!CanRestoreRuntimeSpawnedRecord(savedRecord, policy))
                    {
                        continue;
                    }

                    if (existingObjectIds.Contains(savedRecord.ObjectId))
                    {
                        continue;
                    }

                    if (runtimeSpawnRestorer(scene, savedRecord))
                    {
                        existingObjectIds.Add(savedRecord.ObjectId);
                        appliedCount++;
                    }
                }
            }

            return appliedCount;
        }

        private static bool CanRestoreRuntimeSpawnedRecord(WorldObjectStateRecord savedRecord, WorldScenePersistencePolicy policy)
        {
            if (savedRecord == null)
            {
                return false;
            }

            if (savedRecord.Consumed && policy.TrackConsumed)
            {
                return false;
            }

            if (savedRecord.Destroyed && policy.TrackDestroyed)
            {
                return false;
            }

            var itemDefinitionId = !string.IsNullOrWhiteSpace(savedRecord.ItemDefinitionId)
                ? savedRecord.ItemDefinitionId
                : savedRecord.ItemInstanceId;
            return !string.IsNullOrWhiteSpace(itemDefinitionId);
        }

        private static void ApplySavedState(WorldObjectIdentity identity, WorldObjectStateRecord savedRecord, WorldScenePersistencePolicy policy)
        {
            if (savedRecord == null)
            {
                return;
            }

            if (policy.TrackTransforms && savedRecord.HasTransformOverride)
            {
                var targetTransform = identity.transform;
                targetTransform.SetPositionAndRotation(savedRecord.Position, savedRecord.Rotation);
            }

            var shouldDeactivateForConsumed = policy.TrackConsumed && savedRecord.Consumed;
            var shouldDeactivateForDestroyed = policy.TrackDestroyed && savedRecord.Destroyed;
            if (shouldDeactivateForConsumed || shouldDeactivateForDestroyed)
            {
                identity.gameObject.SetActive(false);
            }
        }
    }
}
