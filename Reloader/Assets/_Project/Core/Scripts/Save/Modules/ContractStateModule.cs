using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Reloader.Contracts.Runtime;

namespace Reloader.Core.Save.Modules
{
    public sealed class ContractStateModule : ISaveDomainModule
    {
        [Serializable]
        private sealed class ContractStatePayload
        {
            [JsonProperty("contractId")]
            public string ContractId { get; set; } = string.Empty;

            [JsonProperty("targetId")]
            public string TargetId { get; set; } = string.Empty;

            [JsonProperty("distanceBand")]
            public float DistanceBand { get; set; }

            [JsonProperty("payout")]
            public int Payout { get; set; }

            [JsonProperty("generatedContractIds")]
            public List<string> GeneratedContractIds { get; set; } = new List<string>();

            [JsonProperty("completedContractIds")]
            public List<string> CompletedContractIds { get; set; } = new List<string>();
        }

        public string ModuleKey => "ContractState";
        public int ModuleVersion => 1;

        public string ActiveContractId { get; set; } = string.Empty;
        public string ActiveTargetId { get; set; } = string.Empty;
        public float ActiveDistanceBand { get; set; }
        public int ActivePayout { get; set; }
        public List<string> GeneratedContractIds { get; } = new List<string>();
        public List<string> CompletedContractIds { get; } = new List<string>();

        public AssassinationContractRuntimeState ActiveContract
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ActiveContractId) || string.IsNullOrWhiteSpace(ActiveTargetId))
                {
                    return null;
                }

                return new AssassinationContractRuntimeState(
                    ActiveContractId,
                    ActiveTargetId,
                    ActiveDistanceBand,
                    ActivePayout);
            }
            set
            {
                if (value == null)
                {
                    ActiveContractId = string.Empty;
                    ActiveTargetId = string.Empty;
                    ActiveDistanceBand = 0f;
                    ActivePayout = 0;
                    return;
                }

                ActiveContractId = value.ContractId;
                ActiveTargetId = value.TargetId;
                ActiveDistanceBand = value.DistanceBand;
                ActivePayout = value.Payout;
            }
        }

        public string CaptureModuleStateJson()
        {
            return JsonConvert.SerializeObject(new ContractStatePayload
            {
                ContractId = ActiveContractId ?? string.Empty,
                TargetId = ActiveTargetId ?? string.Empty,
                DistanceBand = ActiveDistanceBand,
                Payout = ActivePayout,
                GeneratedContractIds = new List<string>(GeneratedContractIds),
                CompletedContractIds = new List<string>(CompletedContractIds)
            });
        }

        public void RestoreModuleStateFromJson(string payloadJson)
        {
            var payload = JsonConvert.DeserializeObject<ContractStatePayload>(payloadJson);
            ActiveContractId = payload?.ContractId ?? string.Empty;
            ActiveTargetId = payload?.TargetId ?? string.Empty;
            ActiveDistanceBand = payload?.DistanceBand ?? 0f;
            ActivePayout = payload?.Payout ?? 0;

            GeneratedContractIds.Clear();
            if (payload?.GeneratedContractIds != null)
            {
                GeneratedContractIds.AddRange(payload.GeneratedContractIds.Where(id => !string.IsNullOrWhiteSpace(id)));
            }

            CompletedContractIds.Clear();
            if (payload?.CompletedContractIds != null)
            {
                CompletedContractIds.AddRange(payload.CompletedContractIds.Where(id => !string.IsNullOrWhiteSpace(id)));
            }
        }

        public void ValidateModuleState()
        {
            if (!string.IsNullOrEmpty(ActiveContractId) && string.IsNullOrWhiteSpace(ActiveContractId))
            {
                throw new InvalidOperationException("ContractState requires a non-empty contractId.");
            }

            if (!string.IsNullOrEmpty(ActiveTargetId) && string.IsNullOrWhiteSpace(ActiveTargetId))
            {
                throw new InvalidOperationException("ContractState requires a non-empty targetId.");
            }

            if (ActiveDistanceBand < 0f)
            {
                throw new InvalidOperationException("ContractState distanceBand cannot be negative.");
            }

            if (ActivePayout < 0)
            {
                throw new InvalidOperationException("ContractState payout cannot be negative.");
            }

            ValidateIdList(GeneratedContractIds, "generatedContractIds");
            ValidateIdList(CompletedContractIds, "completedContractIds");
        }

        private static void ValidateIdList(List<string> ids, string fieldName)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(ids[i]))
                {
                    throw new InvalidOperationException($"ContractState {fieldName}[{i}] is invalid.");
                }
            }
        }
    }
}
