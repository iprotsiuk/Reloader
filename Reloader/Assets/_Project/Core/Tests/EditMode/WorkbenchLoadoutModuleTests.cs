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
    public class WorkbenchLoadoutModuleTests
    {
        private string _tempDir;
        private string _savePath;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "reloader-workbench-loadout-save-tests-" + Guid.NewGuid().ToString("N"));
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
        public void SaveBootstrapper_DefaultCoordinatorCapture_IncludesWorkbenchLoadoutModule()
        {
            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var envelope = coordinator.CaptureEnvelope("0.5.0-dev", new SaveFeatureFlags());

            Assert.That(envelope.SchemaVersion, Is.EqualTo(5));
            Assert.That(envelope.Modules.ContainsKey("WorkbenchLoadout"), Is.True);
            Assert.That(envelope.Modules["WorkbenchLoadout"].ModuleVersion, Is.EqualTo(1));
        }

        [Test]
        public void SaveBootstrapper_DefaultCoordinatorLoad_MigratesSchemaV4SaveMissingWorkbenchLoadout_AndRestoresSuccessfully()
        {
            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var repository = new SaveFileRepository();
            var envelope = coordinator.CaptureEnvelope("0.5.0-dev", new SaveFeatureFlags());
            envelope.SchemaVersion = 4;
            envelope.Modules.Remove("WorkbenchLoadout");

            repository.WriteEnvelope(_savePath, envelope);

            Assert.DoesNotThrow(() => coordinator.Load(_savePath));
        }

        [Test]
        public void SchemaV4ToV5AddWorkbenchLoadoutMigration_InsertsMissingModuleBlock()
        {
            var migration = new SchemaV4ToV5AddWorkbenchLoadoutMigration();
            var envelope = new SaveEnvelope
            {
                SchemaVersion = 4,
                BuildVersion = "0.4.0-dev",
                CreatedAtUtc = "2026-03-01T00:00:00Z",
                FeatureFlags = new SaveFeatureFlags(),
                Modules = new Dictionary<string, ModuleSaveBlock>
                {
                    { "CoreWorld", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "Inventory", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "Weapons", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "WorldObjectState", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "ContainerStorage", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "PlayerDevice", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } }
                }
            };

            var migrated = migration.Apply(envelope);

            Assert.That(migrated.SchemaVersion, Is.EqualTo(5));
            Assert.That(migrated.Modules.ContainsKey("WorkbenchLoadout"), Is.True);
            Assert.That(migrated.Modules["WorkbenchLoadout"].ModuleVersion, Is.EqualTo(1));
            Assert.That(migrated.Modules["WorkbenchLoadout"].PayloadJson, Is.EqualTo("{}"));
        }

        [Test]
        public void SchemaV4ToV5AddWorkbenchLoadoutMigration_LeavesExistingModuleBlockUnchanged()
        {
            var migration = new SchemaV4ToV5AddWorkbenchLoadoutMigration();
            var existingBlock = new ModuleSaveBlock
            {
                ModuleVersion = 8,
                PayloadJson = "{\"preserve\":true}"
            };

            var envelope = new SaveEnvelope
            {
                SchemaVersion = 4,
                BuildVersion = "0.4.0-dev",
                CreatedAtUtc = "2026-03-01T00:00:00Z",
                FeatureFlags = new SaveFeatureFlags(),
                Modules = new Dictionary<string, ModuleSaveBlock>
                {
                    { "CoreWorld", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "Inventory", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "Weapons", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "WorldObjectState", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "ContainerStorage", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "PlayerDevice", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "WorkbenchLoadout", existingBlock }
                }
            };

            var migrated = migration.Apply(envelope);

            Assert.That(migrated.SchemaVersion, Is.EqualTo(5));
            Assert.That(migrated.Modules["WorkbenchLoadout"], Is.SameAs(existingBlock));
            Assert.That(migrated.Modules["WorkbenchLoadout"].ModuleVersion, Is.EqualTo(8));
            Assert.That(migrated.Modules["WorkbenchLoadout"].PayloadJson, Is.EqualTo("{\"preserve\":true}"));
        }

        [Test]
        public void WorkbenchLoadoutModule_RoundTrip_PopulatedNestedPayload_PreservesData()
        {
            var source = new WorkbenchLoadoutModule();
            source.Workbenches.Add(new WorkbenchLoadoutModule.WorkbenchRecord
            {
                WorkbenchId = "bench.mainTown.basement.01",
                SlotNodes = new List<WorkbenchLoadoutModule.SlotNodeRecord>
                {
                    new WorkbenchLoadoutModule.SlotNodeRecord
                    {
                        SlotId = "press.mount",
                        MountedItemId = "item.press.001",
                        ChildSlots = new List<WorkbenchLoadoutModule.SlotNodeRecord>
                        {
                            new WorkbenchLoadoutModule.SlotNodeRecord
                            {
                                SlotId = "press.dieStation.01",
                                MountedItemId = "item.die.fullLength.308",
                                ChildSlots = new List<WorkbenchLoadoutModule.SlotNodeRecord>
                                {
                                    new WorkbenchLoadoutModule.SlotNodeRecord
                                    {
                                        SlotId = "die.shellHolder",
                                        MountedItemId = "item.shellHolder.308"
                                    }
                                }
                            }
                        }
                    }
                }
            });

            var payloadJson = source.CaptureModuleStateJson();
            var restored = new WorkbenchLoadoutModule();
            restored.RestoreModuleStateFromJson(payloadJson);

            Assert.That(restored.Workbenches.Count, Is.EqualTo(1));
            Assert.That(restored.Workbenches[0].WorkbenchId, Is.EqualTo("bench.mainTown.basement.01"));
            Assert.That(restored.Workbenches[0].SlotNodes.Count, Is.EqualTo(1));
            Assert.That(restored.Workbenches[0].SlotNodes[0].MountedItemId, Is.EqualTo("item.press.001"));
            Assert.That(restored.Workbenches[0].SlotNodes[0].ChildSlots.Count, Is.EqualTo(1));
            Assert.That(restored.Workbenches[0].SlotNodes[0].ChildSlots[0].MountedItemId, Is.EqualTo("item.die.fullLength.308"));
            Assert.That(restored.Workbenches[0].SlotNodes[0].ChildSlots[0].ChildSlots.Count, Is.EqualTo(1));
            Assert.That(restored.Workbenches[0].SlotNodes[0].ChildSlots[0].ChildSlots[0].MountedItemId, Is.EqualTo("item.shellHolder.308"));
        }

        [Test]
        public void WorkbenchLoadoutModule_Restore_ToleratesEmptyPayloadInternals()
        {
            var module = new WorkbenchLoadoutModule();

            Assert.DoesNotThrow(() => module.RestoreModuleStateFromJson("{}"));
            Assert.That(module.Workbenches.Count, Is.EqualTo(0));
        }

        [Test]
        public void WorkbenchLoadoutModule_Validate_ThrowsWhenNestedSlotIdMissing()
        {
            var module = new WorkbenchLoadoutModule();
            module.Workbenches.Add(new WorkbenchLoadoutModule.WorkbenchRecord
            {
                WorkbenchId = "bench.mainTown.basement.01",
                SlotNodes = new List<WorkbenchLoadoutModule.SlotNodeRecord>
                {
                    new WorkbenchLoadoutModule.SlotNodeRecord
                    {
                        SlotId = "press.mount",
                        ChildSlots = new List<WorkbenchLoadoutModule.SlotNodeRecord>
                        {
                            new WorkbenchLoadoutModule.SlotNodeRecord
                            {
                                SlotId = " "
                            }
                        }
                    }
                }
            });

            Assert.Throws<InvalidOperationException>(() => module.ValidateModuleState());
        }
    }
}
