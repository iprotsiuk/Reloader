using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reloader.Core.Save.IO;

namespace Reloader.Core.Save
{
    public sealed class SaveCoordinator
    {
        private readonly SaveFileRepository _fileRepository;
        private readonly List<SaveModuleRegistration> _moduleRegistrations;
        private readonly int _currentSchemaVersion;

        public SaveCoordinator(
            SaveFileRepository fileRepository,
            IEnumerable<SaveModuleRegistration> moduleRegistrations,
            int currentSchemaVersion = 1)
        {
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _moduleRegistrations = (moduleRegistrations ?? throw new ArgumentNullException(nameof(moduleRegistrations)))
                .OrderBy(x => x.Order)
                .ToList();
            _currentSchemaVersion = currentSchemaVersion;

            if (_moduleRegistrations.Count == 0)
            {
                throw new ArgumentException("At least one save module registration is required.", nameof(moduleRegistrations));
            }

            if (_moduleRegistrations.GroupBy(x => x.Module.ModuleKey).Any(group => group.Count() > 1))
            {
                throw new InvalidOperationException("Duplicate module keys detected in save module registrations.");
            }
        }

        public SaveEnvelope CaptureEnvelope(string buildVersion)
        {
            SaveRuntimeBridgeRegistry.PrepareForSave(_moduleRegistrations);

            var envelope = new SaveEnvelope
            {
                SchemaVersion = _currentSchemaVersion,
                BuildVersion = string.IsNullOrWhiteSpace(buildVersion) ? "0.0.0-unknown" : buildVersion,
                CreatedAtUtc = DateTime.UtcNow.ToString("O"),
                Modules = new Dictionary<string, ModuleSaveBlock>()
            };

            foreach (var registration in _moduleRegistrations)
            {
                envelope.Modules[registration.Module.ModuleKey] = new ModuleSaveBlock
                {
                    ModuleVersion = registration.Module.ModuleVersion,
                    PayloadJson = registration.Module.CaptureModuleStateJson() ?? "{}"
                };
            }

            return envelope;
        }

        public void Save(string absolutePath, string buildVersion)
        {
            var envelope = CaptureEnvelope(buildVersion);
            _fileRepository.WriteEnvelope(absolutePath, envelope);
        }

        public void Load(string absolutePath)
        {
            var envelope = _fileRepository.ReadEnvelope(absolutePath);

            if (envelope.SchemaVersion != _currentSchemaVersion)
            {
                throw new InvalidDataException(
                    $"Save schema {envelope.SchemaVersion} does not match runtime schema {_currentSchemaVersion}.");
            }

            ValidateRequiredModulesPresent(envelope);
            ValidateAllPayloadsAreWellFormedJson(envelope);

            // Load is transactional: if restore/validation fails, roll back module state.
            var transaction = SaveLoadTransaction.Capture(_moduleRegistrations);
            try
            {
                RestoreModules(envelope);
                ValidateRestoredState();
                SaveRuntimeBridgeRegistry.FinalizeAfterLoad(_moduleRegistrations);
            }
            catch (Exception restoreEx)
            {
                transaction.RollbackOrThrow(restoreEx);
                throw;
            }
        }

        private void ValidateRequiredModulesPresent(SaveEnvelope envelope)
        {
            var missing = _moduleRegistrations
                .Where(x => !envelope.Modules.ContainsKey(x.Module.ModuleKey))
                .Select(x => x.Module.ModuleKey)
                .ToArray();

            if (missing.Length > 0)
            {
                throw new InvalidDataException("Missing required module block(s): " + string.Join(", ", missing));
            }
        }

        private void ValidateAllPayloadsAreWellFormedJson(SaveEnvelope envelope)
        {
            foreach (var registration in _moduleRegistrations)
            {
                var key = registration.Module.ModuleKey;
                var block = envelope.Modules[key];
                var payloadJson = block.PayloadJson ?? string.Empty;

                try
                {
                    JToken.Parse(payloadJson);
                }
                catch (JsonException ex)
                {
                    throw new InvalidDataException($"Corrupted payload JSON for module '{key}'.", ex);
                }
            }
        }

        private void RestoreModules(SaveEnvelope envelope)
        {
            foreach (var registration in _moduleRegistrations)
            {
                var block = envelope.Modules[registration.Module.ModuleKey];
                registration.Module.RestoreModuleStateFromJson(block.PayloadJson);
            }
        }

        private void ValidateRestoredState()
        {
            foreach (var registration in _moduleRegistrations)
            {
                registration.Module.ValidateModuleState();
            }
        }
    }
}
