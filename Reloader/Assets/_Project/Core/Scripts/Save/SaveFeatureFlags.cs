using System;
using Newtonsoft.Json;

namespace Reloader.Core.Save
{
    [Serializable]
    public sealed class SaveFeatureFlags
    {
        [JsonProperty("npcStateEnabled")]
        public bool NpcStateEnabled { get; set; }

        [JsonProperty("huntingStateEnabled")]
        public bool HuntingStateEnabled { get; set; }

        [JsonProperty("lawStateEnabled")]
        public bool LawStateEnabled { get; set; }
    }
}
