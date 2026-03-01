using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Reloader.Core.Persistence
{
    [Serializable]
    public sealed class WorldObjectStateRecord
    {
        [JsonProperty("objectId")]
        public string ObjectId { get; set; } = string.Empty;

        [JsonProperty("consumed")]
        public bool Consumed { get; set; }

        [JsonProperty("destroyed")]
        public bool Destroyed { get; set; }

        [JsonProperty("hasTransformOverride")]
        public bool HasTransformOverride { get; set; }

        [JsonProperty("position")]
        public Vector3 Position { get; set; }

        [JsonProperty("rotation")]
        public Quaternion Rotation { get; set; } = Quaternion.identity;

        [JsonProperty("lastUpdatedDay")]
        public int LastUpdatedDay { get; set; }

        [JsonProperty("itemInstanceId")]
        public string ItemInstanceId { get; set; } = string.Empty;

        [JsonProperty("itemDefinitionId")]
        public string ItemDefinitionId { get; set; } = string.Empty;

        [JsonProperty("stackQuantity")]
        public int StackQuantity { get; set; } = 1;

    }
}
