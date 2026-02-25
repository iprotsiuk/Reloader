using System;
using Newtonsoft.Json;

namespace Reloader.Core.Save.Modules
{
    public sealed class CoreWorldModule : ISaveDomainModule
    {
        [Serializable]
        private sealed class CoreWorldPayload
        {
            [JsonProperty("dayCount")]
            public int DayCount { get; set; }

            [JsonProperty("timeOfDay")]
            public float TimeOfDay { get; set; }
        }

        public string ModuleKey => "CoreWorld";
        public int ModuleVersion => 1;

        public int DayCount { get; set; }
        public float TimeOfDay { get; set; }

        public string CaptureModuleStateJson()
        {
            var payload = new CoreWorldPayload
            {
                DayCount = DayCount,
                TimeOfDay = TimeOfDay
            };
            return JsonConvert.SerializeObject(payload);
        }

        public void RestoreModuleStateFromJson(string payloadJson)
        {
            var payload = JsonConvert.DeserializeObject<CoreWorldPayload>(payloadJson);
            if (payload == null)
            {
                throw new InvalidOperationException("CoreWorld payload is null.");
            }

            DayCount = payload.DayCount;
            TimeOfDay = payload.TimeOfDay;
        }

        public void ValidateModuleState()
        {
            if (DayCount < 0)
            {
                throw new InvalidOperationException("CoreWorld.DayCount cannot be negative.");
            }

            if (TimeOfDay < 0f || TimeOfDay >= 24f)
            {
                throw new InvalidOperationException("CoreWorld.TimeOfDay must be in [0, 24).");
            }
        }
    }
}
