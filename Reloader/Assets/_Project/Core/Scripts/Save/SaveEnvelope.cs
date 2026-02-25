using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Reloader.Core.Save
{
    [Serializable]
    public sealed class SaveEnvelope
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; }

        [JsonProperty("buildVersion")]
        public string BuildVersion { get; set; } = string.Empty;

        [JsonProperty("createdAtUtc")]
        public string CreatedAtUtc { get; set; } = string.Empty;

        [JsonProperty("featureFlags")]
        public SaveFeatureFlags FeatureFlags { get; set; } = new SaveFeatureFlags();

        [JsonProperty("modules")]
        public Dictionary<string, ModuleSaveBlock> Modules { get; set; } = new Dictionary<string, ModuleSaveBlock>();
    }
}
