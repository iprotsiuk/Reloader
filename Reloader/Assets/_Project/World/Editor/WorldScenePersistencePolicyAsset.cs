#if UNITY_EDITOR
using Reloader.Core.Persistence;
using UnityEngine;

namespace Reloader.World.Editor
{
    [CreateAssetMenu(
        fileName = "WorldScenePersistencePolicy",
        menuName = "Reloader/World/Scene Persistence Policy")]
    public sealed class WorldScenePersistencePolicyAsset : ScriptableObject
    {
        [SerializeField] private string _scenePath = string.Empty;
        [SerializeField] private WorldObjectPersistenceMode _mode = WorldObjectPersistenceMode.Persistent;
        [SerializeField] private int _retentionDays;
        [SerializeField] private string _cleanupRuleSetId = string.Empty;
        [SerializeField] private bool _trackConsumed = true;
        [SerializeField] private bool _trackDestroyed = true;
        [SerializeField] private bool _trackTransforms = true;
        [SerializeField] private bool _trackSpawnedObjects = true;

        public WorldScenePersistencePolicy ToPolicy()
        {
            return new WorldScenePersistencePolicy
            {
                ScenePath = _scenePath?.Trim() ?? string.Empty,
                Mode = _mode,
                RetentionDays = _retentionDays,
                CleanupRuleSetId = _cleanupRuleSetId ?? string.Empty,
                TrackConsumed = _trackConsumed,
                TrackDestroyed = _trackDestroyed,
                TrackTransforms = _trackTransforms,
                TrackSpawnedObjects = _trackSpawnedObjects
            };
        }
    }
}
#endif
