using System;
using Newtonsoft.Json;
using Reloader.Core.Events;

namespace Reloader.Core.Save.Modules
{
    public sealed class PoliceHeatStateModule : ISaveDomainModule
    {
        [Serializable]
        private sealed class PoliceHeatStatePayload
        {
            [JsonProperty("level")]
            public PoliceHeatLevel Level { get; set; } = PoliceHeatLevel.Clear;

            [JsonProperty("lastCrimeType")]
            public CrimeType LastCrimeType { get; set; } = CrimeType.Murder;

            [JsonProperty("searchTimeRemainingSeconds")]
            public float SearchTimeRemainingSeconds { get; set; }

            [JsonProperty("hasLineOfSightToPlayer")]
            public bool HasLineOfSightToPlayer { get; set; }
        }

        public string ModuleKey => "PoliceHeatState";
        public int ModuleVersion => 1;

        public PoliceHeatLevel HeatLevel { get; set; } = PoliceHeatLevel.Clear;
        public CrimeType LastCrimeType { get; set; } = CrimeType.Murder;
        public float SearchTimeRemainingSeconds { get; set; }
        public bool HasLineOfSightToPlayer { get; set; }

        public PoliceHeatState CurrentState
        {
            get => new PoliceHeatState(HeatLevel, LastCrimeType, SearchTimeRemainingSeconds, HasLineOfSightToPlayer);
            set
            {
                HeatLevel = value.Level;
                LastCrimeType = value.LastCrimeType;
                SearchTimeRemainingSeconds = value.SearchTimeRemainingSeconds;
                HasLineOfSightToPlayer = value.HasLineOfSightToPlayer;
            }
        }

        public string CaptureModuleStateJson()
        {
            return JsonConvert.SerializeObject(new PoliceHeatStatePayload
            {
                Level = HeatLevel,
                LastCrimeType = LastCrimeType,
                SearchTimeRemainingSeconds = SearchTimeRemainingSeconds,
                HasLineOfSightToPlayer = HasLineOfSightToPlayer
            });
        }

        public void RestoreModuleStateFromJson(string payloadJson)
        {
            var payload = JsonConvert.DeserializeObject<PoliceHeatStatePayload>(payloadJson);
            if (payload == null)
            {
                HeatLevel = PoliceHeatLevel.Clear;
                LastCrimeType = CrimeType.Murder;
                SearchTimeRemainingSeconds = 0f;
                HasLineOfSightToPlayer = false;
                return;
            }

            HeatLevel = payload.Level;
            LastCrimeType = payload.LastCrimeType;
            SearchTimeRemainingSeconds = payload.SearchTimeRemainingSeconds;
            HasLineOfSightToPlayer = payload.HasLineOfSightToPlayer;
        }

        public void ValidateModuleState()
        {
            if (SearchTimeRemainingSeconds < 0f)
            {
                throw new InvalidOperationException("PoliceHeatState search timer cannot be negative.");
            }
        }
    }
}
