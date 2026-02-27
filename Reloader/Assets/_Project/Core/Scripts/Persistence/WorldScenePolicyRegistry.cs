using System;
using System.Collections.Generic;

namespace Reloader.Core.Persistence
{
    public sealed class WorldScenePolicyRegistry
    {
        private readonly Dictionary<string, WorldScenePersistencePolicy> _policiesByScenePath =
            new Dictionary<string, WorldScenePersistencePolicy>(StringComparer.Ordinal);

        private readonly Dictionary<string, WorldScenePersistencePolicy> _fallbackPoliciesByScenePath =
            new Dictionary<string, WorldScenePersistencePolicy>(StringComparer.Ordinal);

        public void Register(WorldScenePersistencePolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            EnsureRequired(policy.ScenePath, nameof(policy.ScenePath));
            _policiesByScenePath[policy.ScenePath] = policy;
        }

        public bool TryGetPolicy(string scenePath, out WorldScenePersistencePolicy policy)
        {
            EnsureRequired(scenePath, nameof(scenePath));

            if (_policiesByScenePath.TryGetValue(scenePath, out policy))
            {
                return true;
            }

            policy = null;
            return false;
        }

        public WorldScenePersistencePolicy ResolvePolicy(string scenePath)
        {
            EnsureRequired(scenePath, nameof(scenePath));

            if (_policiesByScenePath.TryGetValue(scenePath, out var explicitPolicy))
            {
                return explicitPolicy;
            }

            if (_fallbackPoliciesByScenePath.TryGetValue(scenePath, out var fallbackPolicy))
            {
                return fallbackPolicy;
            }

            fallbackPolicy = new WorldScenePersistencePolicy
            {
                ScenePath = scenePath,
                Mode = WorldObjectPersistenceMode.Persistent,
                RetentionDays = 0,
                CleanupRuleSetId = string.Empty,
                TrackConsumed = true,
                TrackDestroyed = true,
                TrackTransforms = true,
                TrackSpawnedObjects = true
            };

            _fallbackPoliciesByScenePath[scenePath] = fallbackPolicy;
            return fallbackPolicy;
        }

        private static void EnsureRequired(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{paramName} cannot be null or whitespace.", paramName);
            }
        }
    }
}
