using System;
using Newtonsoft.Json;

namespace Reloader.Core.Save
{
    [Serializable]
    public sealed class ModuleSaveBlock
    {
        [JsonProperty("moduleVersion")]
        public int ModuleVersion { get; set; }

        [JsonProperty("payloadJson")]
        public string PayloadJson { get; set; } = "{}";
    }
}
