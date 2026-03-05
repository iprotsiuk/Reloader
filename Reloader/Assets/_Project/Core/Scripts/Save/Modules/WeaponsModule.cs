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
        public sealed class AttachmentStateRecord
        {
            [JsonProperty("slotType")]
            public int SlotType { get; set; }

            [JsonProperty("attachmentItemId")]
            public string AttachmentItemId { get; set; }
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

            [JsonProperty("magCapacity")]
            public int MagCapacity { get; set; }

            [JsonProperty("reserveCount")]
            public int ReserveCount { get; set; }

            [JsonProperty("chamberRound")]
            public AmmoBallisticRecord ChamberRound { get; set; }

            [JsonProperty("magazineRounds")]
            public List<AmmoBallisticRecord> MagazineRounds { get; set; } = new List<AmmoBallisticRecord>();

            [JsonProperty("attachments")]
            public List<AttachmentStateRecord> Attachments { get; set; } = new List<AttachmentStateRecord>();
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
                if (state == null)
                {
                    throw new InvalidOperationException($"Weapons payload contains invalid item ID at index {i}.");
                }

                SaveValidation.EnsureRequiredString(state.ItemId, $"Weapons payload contains invalid item ID at index {i}.");
                SaveValidation.EnsureNonNegative(state.MagCount, $"Weapon '{state.ItemId}' has negative mag count.");
                SaveValidation.EnsureNonNegative(state.MagCapacity, $"Weapon '{state.ItemId}' has invalid mag capacity '{state.MagCapacity}'.");

                var effectiveMagCapacity = state.MagCapacity > 0
                    ? state.MagCapacity
                    : Math.Max(state.MagCount, state.MagazineRounds?.Count ?? 0);
                SaveValidation.Ensure(
                    state.MagCount <= effectiveMagCapacity,
                    $"Weapon '{state.ItemId}' mag count '{state.MagCount}' exceeds capacity '{effectiveMagCapacity}'.");

                SaveValidation.EnsureNonNegative(state.ReserveCount, $"Weapon '{state.ItemId}' has negative reserve count.");
                SaveValidation.Ensure(!state.ChamberLoaded || state.ChamberRound != null, $"Weapon '{state.ItemId}' is marked chamberLoaded but has no chamberRound.");
                SaveValidation.Ensure(state.ChamberLoaded || state.ChamberRound == null, $"Weapon '{state.ItemId}' has chamberRound payload while chamberLoaded is false.");

                if (state.ChamberRound != null)
                {
                    ValidateAmmoRecord(state.ItemId, "chamber", state.ChamberRound);
                }

                if (state.MagazineRounds == null)
                {
                    state.MagazineRounds = new List<AmmoBallisticRecord>();
                }

                SaveValidation.EnsureCountMatch(
                    state.MagCount,
                    state.MagazineRounds.Count,
                    $"Weapon '{state.ItemId}' magazine round payload count '{state.MagazineRounds.Count}' does not match magCount '{state.MagCount}'.");

                for (var j = 0; j < state.MagazineRounds.Count; j++)
                {
                    ValidateAmmoRecord(state.ItemId, $"magazine[{j}]", state.MagazineRounds[j]);
                }

                if (state.Attachments == null)
                {
                    state.Attachments = new List<AttachmentStateRecord>();
                }

                var seenSlots = new HashSet<int>();
                for (var j = 0; j < state.Attachments.Count; j++)
                {
                    var attachment = state.Attachments[j];
                    SaveValidation.Ensure(attachment != null, $"Weapon '{state.ItemId}' contains null attachment entry at index {j}.");
                    SaveValidation.EnsureRequiredString(
                        attachment.AttachmentItemId,
                        $"Weapon '{state.ItemId}' attachment entry at index {j} has invalid attachment item ID.");
                    SaveValidation.Ensure(
                        seenSlots.Add(attachment.SlotType),
                        $"Weapon '{state.ItemId}' contains duplicate attachment slot '{attachment.SlotType}'.");
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
