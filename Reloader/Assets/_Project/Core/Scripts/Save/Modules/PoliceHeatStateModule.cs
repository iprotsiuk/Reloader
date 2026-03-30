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

            [JsonProperty("wantedLevel")]
            public int? WantedLevel { get; set; }

            [JsonProperty("isPlayerIdentified")]
            public bool? IsPlayerIdentified { get; set; }

            [JsonProperty("identificationProgressSeconds")]
            public float? IdentificationProgressSeconds { get; set; }
        }

        public string ModuleKey => "PoliceHeatState";
        public int ModuleVersion => 1;

        public PoliceHeatLevel HeatLevel { get; set; } = PoliceHeatLevel.Clear;
        public CrimeType LastCrimeType { get; set; } = CrimeType.Murder;
        public float SearchTimeRemainingSeconds { get; set; }
        public bool HasLineOfSightToPlayer { get; set; }
        public int WantedLevel { get; set; }
        public bool IsPlayerIdentified { get; set; }
        public float IdentificationProgressSeconds { get; set; }

        public PoliceHeatState CurrentState
        {
            get => new PoliceHeatState(
                HeatLevel,
                LastCrimeType,
                SearchTimeRemainingSeconds,
                HasLineOfSightToPlayer,
                WantedLevel,
                IsPlayerIdentified,
                IdentificationProgressSeconds);
            set
            {
                HeatLevel = value.Level;
                LastCrimeType = value.LastCrimeType;
                SearchTimeRemainingSeconds = value.SearchTimeRemainingSeconds;
                HasLineOfSightToPlayer = value.HasLineOfSightToPlayer;
                WantedLevel = value.WantedLevel;
                IsPlayerIdentified = value.IsPlayerIdentified;
                IdentificationProgressSeconds = value.IdentificationProgressSeconds;
            }
        }

        public string CaptureModuleStateJson()
        {
            return JsonConvert.SerializeObject(new PoliceHeatStatePayload
            {
                Level = HeatLevel,
                LastCrimeType = LastCrimeType,
                SearchTimeRemainingSeconds = SearchTimeRemainingSeconds,
                HasLineOfSightToPlayer = HasLineOfSightToPlayer,
                WantedLevel = WantedLevel,
                IsPlayerIdentified = IsPlayerIdentified,
                IdentificationProgressSeconds = IdentificationProgressSeconds
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
                WantedLevel = 0;
                IsPlayerIdentified = false;
                IdentificationProgressSeconds = 0f;
                return;
            }

            HeatLevel = payload.Level;
            LastCrimeType = payload.LastCrimeType;
            SearchTimeRemainingSeconds = payload.SearchTimeRemainingSeconds;
            HasLineOfSightToPlayer = payload.HasLineOfSightToPlayer;
            WantedLevel = payload.WantedLevel ?? DeriveWantedLevel(payload.Level, payload.LastCrimeType);
            IsPlayerIdentified = payload.IsPlayerIdentified ?? DeriveIdentificationState(payload.Level);
            IdentificationProgressSeconds = payload.IdentificationProgressSeconds ?? 0f;
        }

        public void ValidateModuleState()
        {
            if (SearchTimeRemainingSeconds < 0f)
            {
                throw new InvalidOperationException("PoliceHeatState search timer cannot be negative.");
            }

            if (WantedLevel < 0)
            {
                throw new InvalidOperationException("PoliceHeatState wanted level cannot be negative.");
            }

            if (IdentificationProgressSeconds < 0f)
            {
                throw new InvalidOperationException("PoliceHeatState identification progress cannot be negative.");
            }
        }

        private static int DeriveWantedLevel(PoliceHeatLevel heatLevel, CrimeType lastCrimeType)
        {
            if (heatLevel == PoliceHeatLevel.Clear)
            {
                return 0;
            }

            return lastCrimeType switch
            {
                CrimeType.Murder => 3,
                CrimeType.AttemptedMurder => 3,
                CrimeType.Resisting => 2,
                CrimeType.Fleeing => 2,
                _ => 1
            };
        }

        private static bool DeriveIdentificationState(PoliceHeatLevel heatLevel)
        {
            return heatLevel == PoliceHeatLevel.ActivePursuit || heatLevel == PoliceHeatLevel.Search;
        }
    }
}
