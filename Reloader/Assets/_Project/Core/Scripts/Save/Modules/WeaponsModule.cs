using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Reloader.Core.Save.Modules
{
    public sealed class WeaponsModule : ISaveDomainModule
    {
        [Serializable]
        public sealed class AmmoBallisticRecord
        {
            [JsonProperty("cartridgeId")]
            public string CartridgeId { get; set; }

            [JsonProperty("displayName")]
            public string DisplayName { get; set; }

            [JsonProperty("ammoItemId")]
            public string AmmoItemId { get; set; }

            [JsonProperty("ammoSource")]
            public int AmmoSource { get; set; }

            [JsonProperty("muzzleVelocityFps")]
            public float MuzzleVelocityFps { get; set; }

            [JsonProperty("velocityStdDevFps")]
            public float VelocityStdDevFps { get; set; }

            [JsonProperty("projectileMassGrains")]
            public float ProjectileMassGrains { get; set; }

            [JsonProperty("ballisticCoefficientG1")]
            public float BallisticCoefficientG1 { get; set; }

            [JsonProperty("dispersionMoa")]
            public float DispersionMoa { get; set; }
        }

        [Serializable]
        public sealed class WeaponStateRecord
        {
            [JsonProperty("itemId")]
            public string ItemId { get; set; }

            [JsonProperty("chamberLoaded")]
            public bool ChamberLoaded { get; set; }

            [JsonProperty("magCount")]
            public int MagCount { get; set; }

            [JsonProperty("reserveCount")]
            public int ReserveCount { get; set; }

            [JsonProperty("chamberRound")]
            public AmmoBallisticRecord ChamberRound { get; set; }

            [JsonProperty("magazineRounds")]
            public List<AmmoBallisticRecord> MagazineRounds { get; set; } = new List<AmmoBallisticRecord>();
        }

        [Serializable]
        private sealed class WeaponsPayload
        {
            [JsonProperty("weaponStates")]
            public List<WeaponStateRecord> WeaponStates { get; set; } = new List<WeaponStateRecord>();
        }

        public string ModuleKey => "Weapons";
        public int ModuleVersion => 1;
        public List<WeaponStateRecord> WeaponStates { get; } = new List<WeaponStateRecord>();

        public string CaptureModuleStateJson()
        {
            return JsonConvert.SerializeObject(new WeaponsPayload
            {
                WeaponStates = new List<WeaponStateRecord>(WeaponStates)
            });
        }

        public void RestoreModuleStateFromJson(string payloadJson)
        {
            var payload = JsonConvert.DeserializeObject<WeaponsPayload>(payloadJson);
            if (payload == null)
            {
                throw new InvalidOperationException("Weapons payload is null.");
            }

            WeaponStates.Clear();
            if (payload.WeaponStates != null)
            {
                WeaponStates.AddRange(payload.WeaponStates);
            }
        }

        public void ValidateModuleState()
        {
            for (var i = 0; i < WeaponStates.Count; i++)
            {
                var state = WeaponStates[i];
                if (state == null || string.IsNullOrWhiteSpace(state.ItemId))
                {
                    throw new InvalidOperationException($"Weapons payload contains invalid item ID at index {i}.");
                }

                if (state.MagCount < 0)
                {
                    throw new InvalidOperationException($"Weapon '{state.ItemId}' has negative mag count.");
                }

                if (state.ReserveCount < 0)
                {
                    throw new InvalidOperationException($"Weapon '{state.ItemId}' has negative reserve count.");
                }

                if (state.ChamberRound != null)
                {
                    ValidateAmmoRecord(state.ItemId, "chamber", state.ChamberRound);
                }

                if (state.MagazineRounds == null)
                {
                    continue;
                }

                for (var j = 0; j < state.MagazineRounds.Count; j++)
                {
                    ValidateAmmoRecord(state.ItemId, $"magazine[{j}]", state.MagazineRounds[j]);
                }
            }
        }

        private static void ValidateAmmoRecord(string itemId, string context, AmmoBallisticRecord record)
        {
            if (record == null)
            {
                throw new InvalidOperationException($"Weapon '{itemId}' has null ammo record for {context}.");
            }

            if (record.MuzzleVelocityFps <= 0f)
            {
                throw new InvalidOperationException($"Weapon '{itemId}' ammo {context} has invalid muzzleVelocityFps.");
            }
        }
    }
}
