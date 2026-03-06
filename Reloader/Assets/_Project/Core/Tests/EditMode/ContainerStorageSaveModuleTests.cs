using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;
using Reloader.Core.Save.Migrations;
using Reloader.Core.Save.Modules;

namespace Reloader.Core.Tests.EditMode
{
    public class ContainerStorageSaveModuleTests
    {
        private string _tempDir;
        private string _savePath;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "reloader-container-storage-save-tests-" + Guid.NewGuid().ToString("N"));
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
        public void SaveBootstrapper_DefaultCoordinatorCapture_IncludesContainerStorageModule()
        {
            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var envelope = coordinator.CaptureEnvelope("0.3.0-dev", new SaveFeatureFlags());

            Assert.That(envelope.SchemaVersion, Is.EqualTo(6));
            Assert.That(envelope.Modules.ContainsKey("ContainerStorage"), Is.True);
            Assert.That(envelope.Modules["ContainerStorage"].ModuleVersion, Is.EqualTo(1));
        }

        [Test]
        public void SaveBootstrapper_DefaultCoordinatorLoad_MigratesSchemaV2SaveMissingContainerStorage_AndRestoresSuccessfully()
        {
            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var repository = new SaveFileRepository();
            var envelope = coordinator.CaptureEnvelope("0.3.0-dev", new SaveFeatureFlags());
            envelope.SchemaVersion = 2;
            envelope.Modules.Remove("ContainerStorage");

            repository.WriteEnvelope(_savePath, envelope);

            Assert.DoesNotThrow(() => coordinator.Load(_savePath));
        }

        [Test]
        public void SchemaV2ToV3AddContainerStorageMigration_InsertsMissingModuleBlock()
        {
            var migration = new SchemaV2ToV3AddContainerStorageMigration();
            var envelope = new SaveEnvelope
            {
                SchemaVersion = 2,
                BuildVersion = "0.2.0-dev",
                CreatedAtUtc = "2026-02-28T00:00:00Z",
                FeatureFlags = new SaveFeatureFlags(),
                Modules = new Dictionary<string, ModuleSaveBlock>
                {
                    { "CoreWorld", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "Inventory", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "Weapons", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "WorldObjectState", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } }
                }
            };

            var migrated = migration.Apply(envelope);

            Assert.That(migrated.SchemaVersion, Is.EqualTo(3));
            Assert.That(migrated.Modules.ContainsKey("ContainerStorage"), Is.True);
            Assert.That(migrated.Modules["ContainerStorage"].ModuleVersion, Is.EqualTo(1));
            Assert.That(migrated.Modules["ContainerStorage"].PayloadJson, Is.EqualTo("{}"));
        }

        [Test]
        public void SchemaV2ToV3AddContainerStorageMigration_LeavesExistingModuleBlockUnchanged()
        {
            var migration = new SchemaV2ToV3AddContainerStorageMigration();
            var existingBlock = new ModuleSaveBlock
            {
                ModuleVersion = 99,
                PayloadJson = "{\"preserve\":true}"
            };

            var envelope = new SaveEnvelope
            {
                SchemaVersion = 2,
                BuildVersion = "0.2.0-dev",
                CreatedAtUtc = "2026-02-28T00:00:00Z",
                FeatureFlags = new SaveFeatureFlags(),
                Modules = new Dictionary<string, ModuleSaveBlock>
                {
                    { "CoreWorld", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "Inventory", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "Weapons", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "WorldObjectState", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "ContainerStorage", existingBlock }
                }
            };

            var migrated = migration.Apply(envelope);

            Assert.That(migrated.SchemaVersion, Is.EqualTo(3));
            Assert.That(migrated.Modules["ContainerStorage"], Is.SameAs(existingBlock));
            Assert.That(migrated.Modules["ContainerStorage"].ModuleVersion, Is.EqualTo(99));
            Assert.That(migrated.Modules["ContainerStorage"].PayloadJson, Is.EqualTo("{\"preserve\":true}"));
        }

        [Test]
        public void ContainerStorageModule_RoundTrip_PopulatedPayload_PreservesData()
        {
            var source = new ContainerStorageModule();
            source.Containers.Add(new ContainerStorageModule.ContainerRecord
            {
                ContainerId = "chest.mainTown.workbench.001",
                Policy = "Persistent",
                SlotItemIds = new List<string> { "powder-a", null, "primer-b" }
            });

            var payloadJson = source.CaptureModuleStateJson();
            var restored = new ContainerStorageModule();
            restored.RestoreModuleStateFromJson(payloadJson);

            Assert.That(restored.Containers.Count, Is.EqualTo(1));
            Assert.That(restored.Containers[0].ContainerId, Is.EqualTo("chest.mainTown.workbench.001"));
            Assert.That(restored.Containers[0].Policy, Is.EqualTo("Persistent"));
            Assert.That(restored.Containers[0].SlotItemIds.Count, Is.EqualTo(3));
            Assert.That(restored.Containers[0].SlotItemIds[0], Is.EqualTo("powder-a"));
            Assert.That(restored.Containers[0].SlotItemIds[1], Is.Null);
            Assert.That(restored.Containers[0].SlotItemIds[2], Is.EqualTo("primer-b"));
        }

        [Test]
        public void ContainerStorageModule_Restore_ToleratesEmptyPayloadInternals()
        {
            var module = new ContainerStorageModule();

            Assert.DoesNotThrow(() => module.RestoreModuleStateFromJson("{}"));
            Assert.That(module.Containers.Count, Is.EqualTo(0));
        }
    }
}
