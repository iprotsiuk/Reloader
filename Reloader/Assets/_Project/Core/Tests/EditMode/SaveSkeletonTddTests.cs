using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;
using Reloader.Core.Save.Migrations;

namespace Reloader.Core.Tests.EditMode
{
    public class SaveSkeletonTddTests
    {
        private string _tempDir;
        private string _savePath;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "reloader-save-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _savePath = Path.Combine(_tempDir, "slot01.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Test]
        public void SaveEnvelope_RoundTrip_PreservesSchemaAndModulePayload()
        {
            var envelope = new SaveEnvelope
            {
                SchemaVersion = 1,
                BuildVersion = "0.1.0-dev",
                CreatedAtUtc = "2026-02-23T18:00:00Z",
                FeatureFlags = new SaveFeatureFlags
                {
                    NpcStateEnabled = false,
                    HuntingStateEnabled = false,
                    LawStateEnabled = false
                },
                Modules = new Dictionary<string, ModuleSaveBlock>
                {
                    {
                        "CoreWorld",
                        new ModuleSaveBlock
                        {
                            ModuleVersion = 1,
                            PayloadJson = "{\"day\":3,\"timeOfDay\":15.25}"
                        }
                    }
                }
            };

            var repository = new SaveFileRepository();
            repository.WriteEnvelope(_savePath, envelope);
            var restored = repository.ReadEnvelope(_savePath);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.SchemaVersion, Is.EqualTo(1));
            Assert.That(restored.Modules.ContainsKey("CoreWorld"), Is.True);
            Assert.That(restored.Modules["CoreWorld"].PayloadJson, Is.EqualTo("{\"day\":3,\"timeOfDay\":15.25}"));
        }

        [Test]
        public void MigrationRunner_MigratesToCurrentSchema_WithBaselineNoOpMigration()
        {
            var runner = new MigrationRunner(new ISaveMigration[]
            {
                new SchemaV1ToV1NoOpMigration()
            });

            runner.ValidateConfiguration();

            var input = new SaveEnvelope
            {
                SchemaVersion = 1,
                BuildVersion = "0.1.0-dev",
                CreatedAtUtc = "2026-02-23T18:00:00Z",
                FeatureFlags = new SaveFeatureFlags(),
                Modules = new Dictionary<string, ModuleSaveBlock>()
            };

            var migrated = runner.MigrateTo(input, 1);

            Assert.That(migrated.SchemaVersion, Is.EqualTo(1));
        }

        [Test]
        public void SaveCoordinator_Load_IgnoresUnknownModuleBlocks_AndRestoresKnownModules()
        {
            var coreWorld = new RecordingModule("CoreWorld");
            var inventory = new RecordingModule("Inventory");
            var restoreOrder = new List<string>();
            coreWorld.OnRestore = restoreOrder.Add;
            inventory.OnRestore = restoreOrder.Add;

            var coordinator = CreateCoordinator(coreWorld, inventory);
            var repository = new SaveFileRepository();
            repository.WriteEnvelope(_savePath, CreateEnvelopeWithUnknownBlock());

            coordinator.Load(_savePath);

            Assert.That(coreWorld.RestoreCallCount, Is.EqualTo(1));
            Assert.That(inventory.RestoreCallCount, Is.EqualTo(1));
            Assert.That(restoreOrder, Is.EqualTo(new[] { "CoreWorld", "Inventory" }));
        }

        [Test]
        public void SaveCoordinator_Load_ThrowsWhenRequiredModuleBlockMissing()
        {
            var coreWorld = new RecordingModule("CoreWorld");
            var inventory = new RecordingModule("Inventory");
            var coordinator = CreateCoordinator(coreWorld, inventory);
            var repository = new SaveFileRepository();

            var envelope = new SaveEnvelope
            {
                SchemaVersion = 1,
                BuildVersion = "0.1.0-dev",
                CreatedAtUtc = "2026-02-23T18:00:00Z",
                FeatureFlags = new SaveFeatureFlags(),
                Modules = new Dictionary<string, ModuleSaveBlock>
                {
                    { "CoreWorld", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = coreWorld.CaptureModuleStateJson() } }
                }
            };

            repository.WriteEnvelope(_savePath, envelope);

            Assert.Throws<InvalidDataException>(() => coordinator.Load(_savePath));
            Assert.That(coreWorld.RestoreCallCount, Is.EqualTo(0));
            Assert.That(inventory.RestoreCallCount, Is.EqualTo(0));
        }

        [Test]
        public void SaveCoordinator_Load_ThrowsForCorruptedPayload_BeforeAnyRestore()
        {
            var coreWorld = new RecordingModule("CoreWorld");
            var inventory = new RecordingModule("Inventory");
            var coordinator = CreateCoordinator(coreWorld, inventory);
            var repository = new SaveFileRepository();

            var envelope = new SaveEnvelope
            {
                SchemaVersion = 1,
                BuildVersion = "0.1.0-dev",
                CreatedAtUtc = "2026-02-23T18:00:00Z",
                FeatureFlags = new SaveFeatureFlags(),
                Modules = new Dictionary<string, ModuleSaveBlock>
                {
                    { "CoreWorld", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = coreWorld.CaptureModuleStateJson() } },
                    { "Inventory", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{\"count\":42" } }
                }
            };

            repository.WriteEnvelope(_savePath, envelope);

            Assert.Throws<InvalidDataException>(() => coordinator.Load(_savePath));
            Assert.That(coreWorld.RestoreCallCount, Is.EqualTo(0));
            Assert.That(inventory.RestoreCallCount, Is.EqualTo(0));
        }

        [Test]
        public void SaveCoordinator_Load_RestoresInDeterministicOrder_ByRegistrationIndex()
        {
            var restoreOrder = new List<string>();
            var coreWorld = new RecordingModule("CoreWorld") { OnRestore = restoreOrder.Add };
            var inventory = new RecordingModule("Inventory") { OnRestore = restoreOrder.Add };

            var coordinator = new SaveCoordinator(
                new SaveFileRepository(),
                new MigrationRunner(new ISaveMigration[] { new SchemaV1ToV1NoOpMigration() }),
                new[]
                {
                    new SaveModuleRegistration(1, inventory),
                    new SaveModuleRegistration(0, coreWorld)
                },
                1);

            var repository = new SaveFileRepository();
            repository.WriteEnvelope(_savePath, CreateEnvelopeWithoutUnknownBlock());

            coordinator.Load(_savePath);

            Assert.That(restoreOrder, Is.EqualTo(new[] { "CoreWorld", "Inventory" }));
        }

        [Test]
        public void SaveCoordinator_Load_WhenValidationFails_RollsBackModuleState()
        {
            var coreWorld = new RecordingModule("CoreWorld");
            var inventory = new RecordingModule("Inventory");
            coreWorld.SetStateForTests("{\"name\":\"core-before\"}");
            inventory.SetStateForTests("{\"name\":\"inventory-before\"}");
            inventory.ThrowOnValidate = true;

            var coordinator = CreateCoordinator(coreWorld, inventory);
            var repository = new SaveFileRepository();
            var envelope = new SaveEnvelope
            {
                SchemaVersion = 1,
                BuildVersion = "0.1.0-dev",
                CreatedAtUtc = "2026-02-23T18:00:00Z",
                FeatureFlags = new SaveFeatureFlags(),
                Modules = new Dictionary<string, ModuleSaveBlock>
                {
                    { "CoreWorld", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{\"name\":\"core-after\"}" } },
                    { "Inventory", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{\"name\":\"inventory-after\"}" } }
                }
            };
            repository.WriteEnvelope(_savePath, envelope);

            Assert.Throws<InvalidOperationException>(() => coordinator.Load(_savePath));
            Assert.That(coreWorld.CurrentPayload, Is.EqualTo("{\"name\":\"core-before\"}"));
            Assert.That(inventory.CurrentPayload, Is.EqualTo("{\"name\":\"inventory-before\"}"));
        }

        [Test]
        public void SaveCoordinator_Load_WhenValidationFails_DoesNotFinalizeRuntimeBridges()
        {
            var coreWorld = new RecordingModule("CoreWorld");
            var inventory = new RecordingModule("Inventory") { ThrowOnValidate = true };
            var coordinator = CreateCoordinator(coreWorld, inventory);
            var repository = new SaveFileRepository();
            var envelope = new SaveEnvelope
            {
                SchemaVersion = 1,
                BuildVersion = "0.1.0-dev",
                CreatedAtUtc = "2026-02-23T18:00:00Z",
                FeatureFlags = new SaveFeatureFlags(),
                Modules = new Dictionary<string, ModuleSaveBlock>
                {
                    { "CoreWorld", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{\"name\":\"core-after\"}" } },
                    { "Inventory", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{\"name\":\"inventory-after\"}" } }
                }
            };
            repository.WriteEnvelope(_savePath, envelope);

            var probeBridge = new ProbeRuntimeBridge();
            SaveRuntimeBridgeRegistry.Register(probeBridge);
            try
            {
                Assert.Throws<InvalidOperationException>(() => coordinator.Load(_savePath));
                Assert.That(probeBridge.FinalizeAfterLoadCallCount, Is.EqualTo(0));
            }
            finally
            {
                SaveRuntimeBridgeRegistry.Unregister(probeBridge);
            }
        }

        private SaveCoordinator CreateCoordinator(RecordingModule coreWorld, RecordingModule inventory)
        {
            return new SaveCoordinator(
                new SaveFileRepository(),
                new MigrationRunner(new ISaveMigration[] { new SchemaV1ToV1NoOpMigration() }),
                new[]
                {
                    new SaveModuleRegistration(0, coreWorld),
                    new SaveModuleRegistration(1, inventory)
                },
                1);
        }

        private SaveEnvelope CreateEnvelopeWithUnknownBlock()
        {
            return new SaveEnvelope
            {
                SchemaVersion = 1,
                BuildVersion = "0.1.0-dev",
                CreatedAtUtc = "2026-02-23T18:00:00Z",
                FeatureFlags = new SaveFeatureFlags(),
                Modules = new Dictionary<string, ModuleSaveBlock>
                {
                    { "CoreWorld", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{\"name\":\"core\"}" } },
                    { "Inventory", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{\"name\":\"inventory\"}" } },
                    { "FutureModule", new ModuleSaveBlock { ModuleVersion = 9, PayloadJson = "{\"future\":true}" } }
                }
            };
        }

        private SaveEnvelope CreateEnvelopeWithoutUnknownBlock()
        {
            return new SaveEnvelope
            {
                SchemaVersion = 1,
                BuildVersion = "0.1.0-dev",
                CreatedAtUtc = "2026-02-23T18:00:00Z",
                FeatureFlags = new SaveFeatureFlags(),
                Modules = new Dictionary<string, ModuleSaveBlock>
                {
                    { "CoreWorld", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{\"name\":\"core\"}" } },
                    { "Inventory", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{\"name\":\"inventory\"}" } }
                }
            };
        }

        private sealed class RecordingModule : ISaveDomainModule
        {
            public RecordingModule(string moduleKey)
            {
                ModuleKey = moduleKey;
                CurrentPayload = "{\"name\":\"" + moduleKey + "\"}";
            }

            public string ModuleKey { get; }
            public int ModuleVersion => 1;
            public int RestoreCallCount { get; private set; }
            public Action<string> OnRestore { get; set; }
            public bool ThrowOnValidate { get; set; }
            public string CurrentPayload { get; private set; }

            public void SetStateForTests(string payloadJson)
            {
                CurrentPayload = payloadJson;
            }

            public string CaptureModuleStateJson()
            {
                return CurrentPayload;
            }

            public void RestoreModuleStateFromJson(string payloadJson)
            {
                var trimmed = (payloadJson ?? string.Empty).Trim();
                if (!trimmed.StartsWith("{", StringComparison.Ordinal) || !trimmed.EndsWith("}", StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"Payload is not a valid object JSON: '{payloadJson}'.");
                }

                RestoreCallCount++;
                CurrentPayload = trimmed;
                OnRestore?.Invoke(ModuleKey);
            }

            public void ValidateModuleState()
            {
                if (ThrowOnValidate)
                {
                    throw new InvalidOperationException($"Validation failed for module {ModuleKey}.");
                }
            }
        }

        private sealed class ProbeRuntimeBridge : ISaveRuntimeBridge
        {
            public int FinalizeAfterLoadCallCount { get; private set; }

            public void PrepareForSave(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
            {
            }

            public void FinalizeAfterLoad(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
            {
                FinalizeAfterLoadCallCount++;
            }
        }
    }
}
