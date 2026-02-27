using System;
using Newtonsoft.Json;

namespace Reloader.Core.Persistence
{
    [Serializable]
    public sealed class WorldScenePersistencePolicy
    {
        [JsonProperty("scenePath")]
        public string ScenePath { get; set; } = string.Empty;

        [JsonProperty("mode")]
        public WorldObjectPersistenceMode Mode { get; set; } = WorldObjectPersistenceMode.Persistent;

        [JsonProperty("retentionDays")]
        public int RetentionDays { get; set; }

        [JsonProperty("cleanupRuleSetId")]
        public string CleanupRuleSetId { get; set; } = string.Empty;

        [JsonProperty("trackConsumed")]
        public bool TrackConsumed { get; set; } = true;

        [JsonProperty("trackDestroyed")]
        public bool TrackDestroyed { get; set; } = true;

        [JsonProperty("trackTransforms")]
        public bool TrackTransforms { get; set; } = true;

        [JsonProperty("trackSpawnedObjects")]
        public bool TrackSpawnedObjects { get; set; } = true;
    }
}
