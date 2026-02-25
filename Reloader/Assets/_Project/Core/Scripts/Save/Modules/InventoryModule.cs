using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Reloader.Core.Save.Modules
{
    public sealed class InventoryModule : ISaveDomainModule
    {
        [Serializable]
        private sealed class InventoryPayload
        {
            [JsonProperty("carriedItemIds")]
            public List<string> CarriedItemIds { get; set; } = new List<string>();

            [JsonProperty("beltSlotItemIds")]
            public List<string> BeltSlotItemIds { get; set; } = new List<string>();

            [JsonProperty("backpackItemIds")]
            public List<string> BackpackItemIds { get; set; } = new List<string>();

            [JsonProperty("backpackCapacity")]
            public int BackpackCapacity { get; set; }

            [JsonProperty("selectedBeltIndex")]
            public int SelectedBeltIndex { get; set; } = -1;
        }

        public string ModuleKey => "Inventory";
        public int ModuleVersion => 1;
        public const int BeltSlotCount = 5;

        public List<string> CarriedItemIds { get; } = new List<string>();
        public List<string> BeltSlotItemIds { get; } = new List<string>(BeltSlotCount);
        public List<string> BackpackItemIds { get; } = new List<string>();
        public int BackpackCapacity { get; set; }
        public int SelectedBeltIndex { get; set; } = -1;

        public string CaptureModuleStateJson()
        {
            var payload = new InventoryPayload
            {
                CarriedItemIds = new List<string>(CarriedItemIds),
                BeltSlotItemIds = GetNormalizedBeltSlotItemIds(),
                BackpackItemIds = new List<string>(BackpackItemIds),
                BackpackCapacity = BackpackCapacity,
                SelectedBeltIndex = SelectedBeltIndex
            };
            return JsonConvert.SerializeObject(payload);
        }

        public void RestoreModuleStateFromJson(string payloadJson)
        {
            var payload = JsonConvert.DeserializeObject<InventoryPayload>(payloadJson);
            if (payload == null)
            {
                throw new InvalidOperationException("Inventory payload is null.");
            }

            CarriedItemIds.Clear();
            if (payload.CarriedItemIds != null)
            {
                CarriedItemIds.AddRange(payload.CarriedItemIds);
            }

            BeltSlotItemIds.Clear();
            if (payload.BeltSlotItemIds != null && payload.BeltSlotItemIds.Count > 0)
            {
                BeltSlotItemIds.AddRange(payload.BeltSlotItemIds);
            }
            else
            {
                for (var i = 0; i < BeltSlotCount; i++)
                {
                    BeltSlotItemIds.Add(null);
                }
            }

            if (BeltSlotItemIds.Count < BeltSlotCount)
            {
                for (var i = BeltSlotItemIds.Count; i < BeltSlotCount; i++)
                {
                    BeltSlotItemIds.Add(null);
                }
            }
            else if (BeltSlotItemIds.Count > BeltSlotCount)
            {
                BeltSlotItemIds.RemoveRange(BeltSlotCount, BeltSlotItemIds.Count - BeltSlotCount);
            }

            BackpackItemIds.Clear();
            if (payload.BackpackItemIds != null)
            {
                BackpackItemIds.AddRange(payload.BackpackItemIds);
            }

            BackpackCapacity = Math.Max(0, payload.BackpackCapacity);
            if (BackpackItemIds.Count > BackpackCapacity)
            {
                BackpackItemIds.RemoveRange(BackpackCapacity, BackpackItemIds.Count - BackpackCapacity);
            }

            SelectedBeltIndex = payload.SelectedBeltIndex;
            if (SelectedBeltIndex < -1 || SelectedBeltIndex >= BeltSlotCount)
            {
                SelectedBeltIndex = -1;
            }
        }

        public void ValidateModuleState()
        {
            for (var i = 0; i < CarriedItemIds.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(CarriedItemIds[i]))
                {
                    throw new InvalidOperationException($"Inventory contains invalid item ID at index {i}.");
                }
            }

            if (BeltSlotItemIds.Count != BeltSlotCount)
            {
                throw new InvalidOperationException($"Inventory belt must have exactly {BeltSlotCount} slots.");
            }

            for (var i = 0; i < BeltSlotItemIds.Count; i++)
            {
                if (BeltSlotItemIds[i] != null && string.IsNullOrWhiteSpace(BeltSlotItemIds[i]))
                {
                    throw new InvalidOperationException($"Inventory belt contains invalid item ID at slot {i}.");
                }
            }

            for (var i = 0; i < BackpackItemIds.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(BackpackItemIds[i]))
                {
                    throw new InvalidOperationException($"Inventory backpack contains invalid item ID at index {i}.");
                }
            }

            if (BackpackCapacity < 0)
            {
                throw new InvalidOperationException("Inventory backpack capacity cannot be negative.");
            }

            if (BackpackItemIds.Count > BackpackCapacity)
            {
                throw new InvalidOperationException("Inventory backpack item count cannot exceed backpack capacity.");
            }

            if (SelectedBeltIndex < -1 || SelectedBeltIndex >= BeltSlotCount)
            {
                throw new InvalidOperationException("Inventory selected belt index is out of range.");
            }
        }

        private List<string> GetNormalizedBeltSlotItemIds()
        {
            var normalized = new List<string>(BeltSlotCount);
            for (var i = 0; i < BeltSlotCount; i++)
            {
                normalized.Add(i < BeltSlotItemIds.Count ? BeltSlotItemIds[i] : null);
            }

            return normalized;
        }
    }
}
