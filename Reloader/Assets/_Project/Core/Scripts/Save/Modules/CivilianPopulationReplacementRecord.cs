using System;
using Newtonsoft.Json;

namespace Reloader.Core.Save.Modules
{
    [Serializable]
    public sealed class CivilianPopulationReplacementRecord
    {
        [JsonProperty("vacatedCivilianId")]
        public string VacatedCivilianId { get; set; } = string.Empty;

        [JsonProperty("queuedAtDay")]
        public int QueuedAtDay { get; set; }

        [JsonProperty("spawnAnchorId")]
        public string SpawnAnchorId { get; set; } = string.Empty;
    }
}
