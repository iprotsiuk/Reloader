using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Reloader.Core.Save.Modules
{
    public sealed class CivilianPopulationModule : ISaveDomainModule
    {
        [Serializable]
        private sealed class CivilianPopulationPayload
        {
            [JsonProperty("civilians")]
            public List<CivilianPopulationRecord> Civilians { get; set; } = new List<CivilianPopulationRecord>();

            [JsonProperty("pendingReplacements")]
            public List<CivilianPopulationReplacementRecord> PendingReplacements { get; set; } =
                new List<CivilianPopulationReplacementRecord>();
        }

        public string ModuleKey => "CivilianPopulation";
        public int ModuleVersion => 1;

        public List<CivilianPopulationRecord> Civilians { get; } = new List<CivilianPopulationRecord>();
        public List<CivilianPopulationReplacementRecord> PendingReplacements { get; } =
            new List<CivilianPopulationReplacementRecord>();

        public string CaptureModuleStateJson()
        {
            return JsonConvert.SerializeObject(new CivilianPopulationPayload
            {
                Civilians = Civilians.Select(CloneRecord).ToList(),
                PendingReplacements = PendingReplacements.Select(CloneReplacement).ToList()
            });
        }

        public void RestoreModuleStateFromJson(string payloadJson)
        {
            var payload = JsonConvert.DeserializeObject<CivilianPopulationPayload>(payloadJson) ?? new CivilianPopulationPayload();

            Civilians.Clear();
            if (payload.Civilians != null)
            {
                for (var i = 0; i < payload.Civilians.Count; i++)
                {
                    var record = payload.Civilians[i];
                    if (record == null || string.IsNullOrWhiteSpace(record.CivilianId))
                    {
                        continue;
                    }

                    Civilians.Add(CloneRecord(record));
                }
            }

            PendingReplacements.Clear();
            if (payload.PendingReplacements != null)
            {
                for (var i = 0; i < payload.PendingReplacements.Count; i++)
                {
                    var record = payload.PendingReplacements[i];
                    if (record == null || string.IsNullOrWhiteSpace(record.VacatedCivilianId))
                    {
                        continue;
                    }

                    PendingReplacements.Add(CloneReplacement(record));
                }
            }
        }

        public void ValidateModuleState()
        {
            var seenCivilianIds = new HashSet<string>(StringComparer.Ordinal);
            var seenAlivePopulationSlotIds = new HashSet<string>(StringComparer.Ordinal);
            var seenPendingReplacementIds = new HashSet<string>(StringComparer.Ordinal);
            var seenPendingReplacementSlotIds = new HashSet<string>(StringComparer.Ordinal);
            var civiliansById = new Dictionary<string, CivilianPopulationRecord>(StringComparer.Ordinal);
            for (var i = 0; i < Civilians.Count; i++)
            {
                var record = Civilians[i];
                if (record == null)
                {
                    throw new InvalidOperationException($"CivilianPopulation civilians[{i}] is null.");
                }

                if (string.IsNullOrWhiteSpace(record.CivilianId))
                {
                    throw new InvalidOperationException($"CivilianPopulation civilians[{i}].civilianId is invalid.");
                }

                if (!seenCivilianIds.Add(record.CivilianId))
                {
                    throw new InvalidOperationException($"CivilianPopulation duplicate civilianId '{record.CivilianId}'.");
                }

                civiliansById.Add(record.CivilianId, record);

                if (record.CreatedAtDay < 0)
                {
                    throw new InvalidOperationException($"CivilianPopulation civilians[{i}].createdAtDay cannot be negative.");
                }

                if (record.RetiredAtDay < -1)
                {
                    throw new InvalidOperationException($"CivilianPopulation civilians[{i}].retiredAtDay cannot be below -1.");
                }

                if (!record.IsAlive && record.RetiredAtDay < 0)
                {
                    throw new InvalidOperationException(
                        $"CivilianPopulation civilians[{i}] must record retiredAtDay when the civilian is dead.");
                }

                ValidateRequiredString(record.PopulationSlotId, $"civilians[{i}].populationSlotId");
                ValidateRequiredString(record.PoolId, $"civilians[{i}].poolId");

                if (record.IsAlive && !seenAlivePopulationSlotIds.Add(record.PopulationSlotId))
                {
                    throw new InvalidOperationException(
                        $"CivilianPopulation duplicate live populationSlotId '{record.PopulationSlotId}'.");
                }

                ValidateRequiredString(record.BaseBodyId, $"civilians[{i}].baseBodyId");
                ValidateRequiredString(record.PresentationType, $"civilians[{i}].presentationType");
                ValidateRequiredString(record.HairId, $"civilians[{i}].hairId");
                ValidateRequiredString(record.HairColorId, $"civilians[{i}].hairColorId");
                ValidateRequiredString(record.BeardId, $"civilians[{i}].beardId");
                ValidateRequiredString(record.OutfitTopId, $"civilians[{i}].outfitTopId");
                ValidateRequiredString(record.OutfitBottomId, $"civilians[{i}].outfitBottomId");
                ValidateRequiredString(record.OuterwearId, $"civilians[{i}].outerwearId");
                ValidateRequiredString(record.SpawnAnchorId, $"civilians[{i}].spawnAnchorId");
                ValidateRequiredString(record.AreaTag, $"civilians[{i}].areaTag");
                ValidateStringList(record.MaterialColorIds, $"civilians[{i}].materialColorIds");
                ValidateStringList(record.GeneratedDescriptionTags, $"civilians[{i}].generatedDescriptionTags");
            }

            for (var i = 0; i < PendingReplacements.Count; i++)
            {
                var record = PendingReplacements[i];
                if (record == null)
                {
                    throw new InvalidOperationException($"CivilianPopulation pendingReplacements[{i}] is null.");
                }

                if (string.IsNullOrWhiteSpace(record.VacatedCivilianId))
                {
                    throw new InvalidOperationException(
                        $"CivilianPopulation pendingReplacements[{i}].vacatedCivilianId is invalid.");
                }

                if (!seenPendingReplacementIds.Add(record.VacatedCivilianId))
                {
                    throw new InvalidOperationException(
                        $"CivilianPopulation duplicate pendingReplacement vacatedCivilianId '{record.VacatedCivilianId}'.");
                }

                if (record.QueuedAtDay < 0)
                {
                    throw new InvalidOperationException(
                        $"CivilianPopulation pendingReplacements[{i}].queuedAtDay cannot be negative.");
                }

                if (string.IsNullOrWhiteSpace(record.SpawnAnchorId))
                {
                    throw new InvalidOperationException(
                        $"CivilianPopulation pendingReplacements[{i}].spawnAnchorId is invalid.");
                }

                if (!civiliansById.TryGetValue(record.VacatedCivilianId, out var vacatedCivilian))
                {
                    throw new InvalidOperationException(
                        $"CivilianPopulation pendingReplacements[{i}] references missing dead civilian '{record.VacatedCivilianId}'.");
                }

                if (vacatedCivilian.IsAlive)
                {
                    throw new InvalidOperationException(
                        $"CivilianPopulation pendingReplacements[{i}] must reference a dead civilian '{record.VacatedCivilianId}'.");
                }

                if (seenAlivePopulationSlotIds.Contains(vacatedCivilian.PopulationSlotId))
                {
                    throw new InvalidOperationException(
                        $"CivilianPopulation pendingReplacements[{i}] targets populationSlotId '{vacatedCivilian.PopulationSlotId}' that already has a live occupant.");
                }

                if (!seenPendingReplacementSlotIds.Add(vacatedCivilian.PopulationSlotId))
                {
                    throw new InvalidOperationException(
                        $"CivilianPopulation duplicate pendingReplacement populationSlotId '{vacatedCivilian.PopulationSlotId}'.");
                }
            }
        }

        private static CivilianPopulationRecord CloneRecord(CivilianPopulationRecord source)
        {
            return new CivilianPopulationRecord
            {
                PopulationSlotId = source.PopulationSlotId ?? string.Empty,
                PoolId = source.PoolId ?? string.Empty,
                CivilianId = source.CivilianId ?? string.Empty,
                IsAlive = source.IsAlive,
                IsContractEligible = source.IsContractEligible,
                IsProtectedFromContracts = source.IsProtectedFromContracts,
                BaseBodyId = source.BaseBodyId ?? string.Empty,
                PresentationType = source.PresentationType ?? string.Empty,
                HairId = source.HairId ?? string.Empty,
                HairColorId = source.HairColorId ?? string.Empty,
                BeardId = source.BeardId ?? string.Empty,
                OutfitTopId = source.OutfitTopId ?? string.Empty,
                OutfitBottomId = source.OutfitBottomId ?? string.Empty,
                OuterwearId = source.OuterwearId ?? string.Empty,
                MaterialColorIds = NormalizeStringList(source.MaterialColorIds),
                GeneratedDescriptionTags = NormalizeStringList(source.GeneratedDescriptionTags),
                SpawnAnchorId = source.SpawnAnchorId ?? string.Empty,
                AreaTag = source.AreaTag ?? string.Empty,
                CreatedAtDay = source.CreatedAtDay,
                RetiredAtDay = source.RetiredAtDay
            };
        }

        private static CivilianPopulationReplacementRecord CloneReplacement(CivilianPopulationReplacementRecord source)
        {
            return new CivilianPopulationReplacementRecord
            {
                VacatedCivilianId = source.VacatedCivilianId ?? string.Empty,
                QueuedAtDay = source.QueuedAtDay,
                SpawnAnchorId = source.SpawnAnchorId ?? string.Empty
            };
        }

        private static List<string> NormalizeStringList(List<string> values)
        {
            if (values == null)
            {
                return new List<string>();
            }

            var normalized = new List<string>(values.Count);
            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    normalized.Add(value);
                }
            }

            return normalized;
        }

        private static void ValidateStringList(List<string> values, string fieldName)
        {
            if (values == null)
            {
                return;
            }

            for (var i = 0; i < values.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(values[i]))
                {
                    throw new InvalidOperationException($"CivilianPopulation {fieldName}[{i}] is invalid.");
                }
            }
        }

        private static void ValidateRequiredString(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"CivilianPopulation {fieldName} is invalid.");
            }
        }
    }
}
