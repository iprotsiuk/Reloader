using System;
using Newtonsoft.Json;

namespace Reloader.Core.Save.Modules
{
    public sealed class PlayerStateModule : ISaveDomainModule
    {
        [Serializable]
        private sealed class PlayerStatePayload
        {
            [JsonProperty("currentScenePath")]
            public string CurrentScenePath { get; set; } = string.Empty;

            [JsonProperty("currentAnchorId")]
            public string CurrentAnchorId { get; set; } = string.Empty;

            [JsonProperty("positionX")]
            public float PositionX { get; set; }

            [JsonProperty("positionY")]
            public float PositionY { get; set; }

            [JsonProperty("positionZ")]
            public float PositionZ { get; set; }

            [JsonProperty("rotationX")]
            public float RotationX { get; set; }

            [JsonProperty("rotationY")]
            public float RotationY { get; set; }

            [JsonProperty("rotationZ")]
            public float RotationZ { get; set; }

            [JsonProperty("rotationW")]
            public float RotationW { get; set; } = 1f;

            [JsonProperty("selectedBeltSlotIndex")]
            public int SelectedBeltSlotIndex { get; set; } = -1;

            [JsonProperty("recoveryReasonId")]
            public string RecoveryReasonId { get; set; } = string.Empty;

            [JsonProperty("recoveryScenePath")]
            public string RecoveryScenePath { get; set; } = string.Empty;

            [JsonProperty("recoveryAnchorId")]
            public string RecoveryAnchorId { get; set; } = string.Empty;

            [JsonProperty("currentHealth")]
            public float CurrentHealth { get; set; }

            [JsonProperty("maxHealth")]
            public float MaxHealth { get; set; }
        }

        public const int BeltSlotCount = 5;

        public string ModuleKey => "PlayerState";
        public int ModuleVersion => 2;

        public string CurrentScenePath { get; set; } = string.Empty;
        public string CurrentAnchorId { get; set; } = string.Empty;
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float RotationX { get; set; }
        public float RotationY { get; set; }
        public float RotationZ { get; set; }
        public float RotationW { get; set; } = 1f;
        public int SelectedBeltSlotIndex { get; set; } = -1;
        public string RecoveryReasonId { get; set; } = string.Empty;
        public string RecoveryScenePath { get; set; } = string.Empty;
        public string RecoveryAnchorId { get; set; } = string.Empty;
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; set; }

        public string CaptureModuleStateJson()
        {
            return JsonConvert.SerializeObject(new PlayerStatePayload
            {
                CurrentScenePath = CurrentScenePath ?? string.Empty,
                CurrentAnchorId = CurrentAnchorId ?? string.Empty,
                PositionX = PositionX,
                PositionY = PositionY,
                PositionZ = PositionZ,
                RotationX = RotationX,
                RotationY = RotationY,
                RotationZ = RotationZ,
                RotationW = RotationW,
                SelectedBeltSlotIndex = SelectedBeltSlotIndex,
                RecoveryReasonId = RecoveryReasonId ?? string.Empty,
                RecoveryScenePath = RecoveryScenePath ?? string.Empty,
                RecoveryAnchorId = RecoveryAnchorId ?? string.Empty,
                CurrentHealth = CurrentHealth,
                MaxHealth = MaxHealth
            });
        }

        public void RestoreModuleStateFromJson(string payloadJson)
        {
            var payload = JsonConvert.DeserializeObject<PlayerStatePayload>(payloadJson);
            if (payload == null)
            {
                CurrentScenePath = string.Empty;
                CurrentAnchorId = string.Empty;
                PositionX = 0f;
                PositionY = 0f;
                PositionZ = 0f;
                RotationX = 0f;
                RotationY = 0f;
                RotationZ = 0f;
                RotationW = 1f;
                SelectedBeltSlotIndex = -1;
                RecoveryReasonId = string.Empty;
                RecoveryScenePath = string.Empty;
                RecoveryAnchorId = string.Empty;
                CurrentHealth = 0f;
                MaxHealth = 0f;
                return;
            }

            CurrentScenePath = payload.CurrentScenePath ?? string.Empty;
            CurrentAnchorId = payload.CurrentAnchorId ?? string.Empty;
            PositionX = payload.PositionX;
            PositionY = payload.PositionY;
            PositionZ = payload.PositionZ;
            RotationX = payload.RotationX;
            RotationY = payload.RotationY;
            RotationZ = payload.RotationZ;
            RotationW = payload.RotationW;
            SelectedBeltSlotIndex = payload.SelectedBeltSlotIndex;
            RecoveryReasonId = payload.RecoveryReasonId ?? string.Empty;
            RecoveryScenePath = payload.RecoveryScenePath ?? string.Empty;
            RecoveryAnchorId = payload.RecoveryAnchorId ?? string.Empty;
            CurrentHealth = payload.CurrentHealth;
            MaxHealth = payload.MaxHealth;
        }

        public void ValidateModuleState()
        {
            SaveValidation.EnsureRequiredString(CurrentScenePath, "PlayerState CurrentScenePath is required.");
            SaveValidation.EnsureRequiredString(CurrentAnchorId, "PlayerState CurrentAnchorId is required.");

            if (SelectedBeltSlotIndex < -1 || SelectedBeltSlotIndex >= BeltSlotCount)
            {
                throw new InvalidOperationException("PlayerState SelectedBeltSlotIndex is out of range.");
            }

            if (!string.IsNullOrWhiteSpace(RecoveryReasonId))
            {
                SaveValidation.EnsureRequiredString(RecoveryScenePath, "PlayerState RecoveryScenePath is required when RecoveryReasonId is set.");
                SaveValidation.EnsureRequiredString(RecoveryAnchorId, "PlayerState RecoveryAnchorId is required when RecoveryReasonId is set.");
            }

            SaveValidation.Ensure(CurrentHealth >= 0f, "PlayerState CurrentHealth must be non-negative.");
            SaveValidation.Ensure(MaxHealth >= 0f, "PlayerState MaxHealth must be non-negative.");
            if (MaxHealth > 0f)
            {
                SaveValidation.Ensure(CurrentHealth <= MaxHealth, "PlayerState CurrentHealth cannot exceed MaxHealth.");
            }
        }
    }
}
