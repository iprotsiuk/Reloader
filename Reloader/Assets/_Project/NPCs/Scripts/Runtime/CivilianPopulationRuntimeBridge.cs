using System;
using System.Collections.Generic;
using Reloader.Core.Save;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Generation;
using UnityEngine;

namespace Reloader.NPCs.Runtime
{
    public sealed class CivilianPopulationRuntimeBridge : MonoBehaviour, ISaveRuntimeBridge
    {
        [SerializeField] private CivilianAppearanceLibrary _appearanceLibrary;
        [SerializeField] private int _initialPopulationCount;
        [SerializeField] private string _civilianIdPrefix = "citizen.mainTown";
        [SerializeField] private string[] _spawnAnchorIds = Array.Empty<string>();

        private readonly CivilianPopulationRuntimeState _runtime = new CivilianPopulationRuntimeState();
        private readonly CivilianAppearanceGenerator _generator = new CivilianAppearanceGenerator();

        public CivilianPopulationRuntimeState Runtime => _runtime;

        private void OnEnable()
        {
            SaveRuntimeBridgeRegistry.Register(this);
        }

        private void OnDisable()
        {
            SaveRuntimeBridgeRegistry.Unregister(this);
        }

        public void PrepareForSave(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            var module = ResolveModule(moduleRegistrations);
            if (module == null)
            {
                return;
            }

            SeedInitialRosterIfNeeded(module);
            CopyRuntimeToModule(module);
        }

        public void FinalizeAfterLoad(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            var module = ResolveModule(moduleRegistrations);
            if (module == null)
            {
                return;
            }

            CopyModuleToRuntime(module);
        }

        public bool TryRetireCivilian(string civilianId, int retiredAtDay)
        {
            if (string.IsNullOrWhiteSpace(civilianId))
            {
                return false;
            }

            for (var i = 0; i < _runtime.Civilians.Count; i++)
            {
                var record = _runtime.Civilians[i];
                if (record == null || !string.Equals(record.CivilianId, civilianId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!record.IsAlive)
                {
                    return false;
                }

                record.IsAlive = false;
                record.IsContractEligible = false;
                record.RetiredAtDay = Math.Max(0, retiredAtDay);

                if (!HasPendingReplacement(civilianId))
                {
                    _runtime.PendingReplacements.Add(new CivilianPopulationReplacementRecord
                    {
                        VacatedCivilianId = record.CivilianId,
                        QueuedAtDay = record.RetiredAtDay,
                        SpawnAnchorId = record.SpawnAnchorId ?? string.Empty
                    });
                }

                return true;
            }

            return false;
        }

        private void SeedInitialRosterIfNeeded(CivilianPopulationModule module)
        {
            if (_runtime.Civilians.Count > 0 || module.Civilians.Count > 0)
            {
                return;
            }

            if (_appearanceLibrary == null || _initialPopulationCount <= 0)
            {
                return;
            }

            var anchorIds = NormalizeSpawnAnchors();
            if (anchorIds.Count == 0)
            {
                return;
            }

            var idPrefix = string.IsNullOrWhiteSpace(_civilianIdPrefix) ? "citizen.mainTown" : _civilianIdPrefix.Trim();
            for (var i = 0; i < _initialPopulationCount; i++)
            {
                var civilianId = $"{idPrefix}.{i + 1:0000}";
                var spawnAnchorId = anchorIds[i % anchorIds.Count];
                var seed = i + 1;
                _runtime.Civilians.Add(_generator.GenerateRecord(
                    _appearanceLibrary,
                    civilianId,
                    createdAtDay: 0,
                    spawnAnchorId,
                    seed,
                    isContractEligible: true));
            }
        }

        private void CopyRuntimeToModule(CivilianPopulationModule module)
        {
            module.Civilians.Clear();
            for (var i = 0; i < _runtime.Civilians.Count; i++)
            {
                module.Civilians.Add(CloneRecord(_runtime.Civilians[i]));
            }

            module.PendingReplacements.Clear();
            for (var i = 0; i < _runtime.PendingReplacements.Count; i++)
            {
                module.PendingReplacements.Add(CloneReplacement(_runtime.PendingReplacements[i]));
            }
        }

        private void CopyModuleToRuntime(CivilianPopulationModule module)
        {
            _runtime.Civilians.Clear();
            for (var i = 0; i < module.Civilians.Count; i++)
            {
                _runtime.Civilians.Add(CloneRecord(module.Civilians[i]));
            }

            _runtime.PendingReplacements.Clear();
            for (var i = 0; i < module.PendingReplacements.Count; i++)
            {
                _runtime.PendingReplacements.Add(CloneReplacement(module.PendingReplacements[i]));
            }
        }

        private List<string> NormalizeSpawnAnchors()
        {
            var anchors = new List<string>();
            if (_spawnAnchorIds == null)
            {
                return anchors;
            }

            for (var i = 0; i < _spawnAnchorIds.Length; i++)
            {
                var value = _spawnAnchorIds[i];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    anchors.Add(value.Trim());
                }
            }

            return anchors;
        }

        private static CivilianPopulationModule ResolveModule(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            if (moduleRegistrations == null)
            {
                return null;
            }

            for (var i = 0; i < moduleRegistrations.Count; i++)
            {
                if (moduleRegistrations[i]?.Module is CivilianPopulationModule module)
                {
                    return module;
                }
            }

            return null;
        }

        private static CivilianPopulationRecord CloneRecord(CivilianPopulationRecord source)
        {
            return new CivilianPopulationRecord
            {
                CivilianId = source?.CivilianId ?? string.Empty,
                IsAlive = source != null && source.IsAlive,
                IsContractEligible = source != null && source.IsContractEligible,
                BaseBodyId = source?.BaseBodyId ?? string.Empty,
                PresentationType = source?.PresentationType ?? string.Empty,
                HairId = source?.HairId ?? string.Empty,
                HairColorId = source?.HairColorId ?? string.Empty,
                BeardId = source?.BeardId ?? string.Empty,
                OutfitTopId = source?.OutfitTopId ?? string.Empty,
                OutfitBottomId = source?.OutfitBottomId ?? string.Empty,
                OuterwearId = source?.OuterwearId ?? string.Empty,
                MaterialColorIds = source?.MaterialColorIds != null ? new List<string>(source.MaterialColorIds) : new List<string>(),
                GeneratedDescriptionTags = source?.GeneratedDescriptionTags != null ? new List<string>(source.GeneratedDescriptionTags) : new List<string>(),
                SpawnAnchorId = source?.SpawnAnchorId ?? string.Empty,
                CreatedAtDay = source?.CreatedAtDay ?? 0,
                RetiredAtDay = source?.RetiredAtDay ?? -1
            };
        }

        private static CivilianPopulationReplacementRecord CloneReplacement(CivilianPopulationReplacementRecord source)
        {
            return new CivilianPopulationReplacementRecord
            {
                VacatedCivilianId = source?.VacatedCivilianId ?? string.Empty,
                QueuedAtDay = source?.QueuedAtDay ?? 0,
                SpawnAnchorId = source?.SpawnAnchorId ?? string.Empty
            };
        }

        private bool HasPendingReplacement(string civilianId)
        {
            for (var i = 0; i < _runtime.PendingReplacements.Count; i++)
            {
                var replacement = _runtime.PendingReplacements[i];
                if (replacement != null && string.Equals(replacement.VacatedCivilianId, civilianId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class CivilianPopulationRuntimeState
    {
        public List<CivilianPopulationRecord> Civilians { get; } = new List<CivilianPopulationRecord>();
        public List<CivilianPopulationReplacementRecord> PendingReplacements { get; } =
            new List<CivilianPopulationReplacementRecord>();
    }
}
