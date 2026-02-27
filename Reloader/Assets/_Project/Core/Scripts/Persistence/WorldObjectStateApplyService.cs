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
            if (policy.Mode != WorldObjectPersistenceMode.Persistent)
            {
                return 0;
            }

            var appliedCount = 0;
            var identities = UnityEngine.Object.FindObjectsByType<WorldObjectIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < identities.Length; i++)
            {
                var identity = identities[i];
                if (identity == null || identity.gameObject.scene.handle != scene.handle)
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
