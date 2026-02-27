using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.Core.Persistence
{
    public sealed class WorldObjectStateApplyService
    {
        public int ApplyForScene(Scene scene, WorldObjectStateStore stateStore, WorldScenePolicyRegistry policyRegistry)
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

                    if (!stateStore.TryGet(scene.path, identity.ObjectId, out var savedRecord))
                    {
                        continue;
                    }

                    ApplySavedState(identity, savedRecord, policy);
                    appliedCount++;
                }
            }

            return appliedCount;
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
