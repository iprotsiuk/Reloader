using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Reloader.Core.Save.Modules
{
    [Serializable]
    public sealed class CivilianPopulationRecord
    {
        [JsonProperty("civilianId")]
        public string CivilianId { get; set; } = string.Empty;

        [JsonProperty("isAlive")]
        public bool IsAlive { get; set; } = true;

        [JsonProperty("isContractEligible")]
        public bool IsContractEligible { get; set; } = true;

        [JsonProperty("baseBodyId")]
        public string BaseBodyId { get; set; } = string.Empty;

        [JsonProperty("presentationType")]
        public string PresentationType { get; set; } = string.Empty;

        [JsonProperty("hairId")]
        public string HairId { get; set; } = string.Empty;

        [JsonProperty("hairColorId")]
        public string HairColorId { get; set; } = string.Empty;

        [JsonProperty("beardId")]
        public string BeardId { get; set; } = string.Empty;

        [JsonProperty("outfitTopId")]
        public string OutfitTopId { get; set; } = string.Empty;

        [JsonProperty("outfitBottomId")]
        public string OutfitBottomId { get; set; } = string.Empty;

        [JsonProperty("outerwearId")]
        public string OuterwearId { get; set; } = string.Empty;

        [JsonProperty("materialColorIds")]
        public List<string> MaterialColorIds { get; set; } = new List<string>();

        [JsonProperty("generatedDescriptionTags")]
        public List<string> GeneratedDescriptionTags { get; set; } = new List<string>();

        [JsonProperty("spawnAnchorId")]
        public string SpawnAnchorId { get; set; } = string.Empty;

        [JsonProperty("createdAtDay")]
        public int CreatedAtDay { get; set; }

        [JsonProperty("retiredAtDay")]
        public int RetiredAtDay { get; set; } = -1;
    }
}
