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
    public class WorldObjectStateSaveModuleTests
    {
        private string _tempDir;
        private string _savePath;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "reloader-world-object-save-tests-" + Guid.NewGuid().ToString("N"));
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
        public void SaveBootstrapper_DefaultCoordinatorCapture_IncludesWorldObjectStateModule()
        {
            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var envelope = coordinator.CaptureEnvelope("0.2.0-dev", new SaveFeatureFlags());

            Assert.That(envelope.SchemaVersion, Is.EqualTo(5));
            Assert.That(envelope.Modules.ContainsKey("WorldObjectState"), Is.True);
            Assert.That(envelope.Modules["WorldObjectState"].ModuleVersion, Is.EqualTo(1));
        }

        [Test]
        public void SaveBootstrapper_DefaultCoordinatorLoad_MigratesSchemaV1SaveMissingWorldObjectState_AndRestoresSuccessfully()
        {
            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var repository = new SaveFileRepository();
            var envelope = coordinator.CaptureEnvelope("0.2.0-dev", new SaveFeatureFlags());
            envelope.SchemaVersion = 1;
            envelope.Modules.Remove("WorldObjectState");

            repository.WriteEnvelope(_savePath, envelope);

            Assert.DoesNotThrow(() => coordinator.Load(_savePath));
        }

        [Test]
        public void SchemaV1ToV2AddWorldObjectStateMigration_InsertsMissingModuleBlock()
        {
            var migration = new SchemaV1ToV2AddWorldObjectStateMigration();
            var envelope = new SaveEnvelope
            {
                SchemaVersion = 1,
                BuildVersion = "0.1.0-dev",
                CreatedAtUtc = "2026-02-27T00:00:00Z",
                FeatureFlags = new SaveFeatureFlags(),
                Modules = new Dictionary<string, ModuleSaveBlock>
                {
                    { "CoreWorld", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "Inventory", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "Weapons", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } }
                }
            };

            var migrated = migration.Apply(envelope);

            Assert.That(migrated.SchemaVersion, Is.EqualTo(2));
            Assert.That(migrated.Modules.ContainsKey("WorldObjectState"), Is.True);
            Assert.That(migrated.Modules["WorldObjectState"].ModuleVersion, Is.EqualTo(1));
            Assert.That(migrated.Modules["WorldObjectState"].PayloadJson, Is.EqualTo("{}"));
        }

        [Test]
        public void SchemaV1ToV2AddWorldObjectStateMigration_LeavesExistingModuleBlockUnchanged()
        {
            var migration = new SchemaV1ToV2AddWorldObjectStateMigration();
            var existingBlock = new ModuleSaveBlock
            {
                ModuleVersion = 77,
                PayloadJson = "{\"preserve\":true}"
            };

            var envelope = new SaveEnvelope
            {
                SchemaVersion = 1,
                BuildVersion = "0.1.0-dev",
                CreatedAtUtc = "2026-02-27T00:00:00Z",
                FeatureFlags = new SaveFeatureFlags(),
                Modules = new Dictionary<string, ModuleSaveBlock>
                {
                    { "CoreWorld", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "Inventory", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "Weapons", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "WorldObjectState", existingBlock }
                }
            };

            var migrated = migration.Apply(envelope);

            Assert.That(migrated.SchemaVersion, Is.EqualTo(2));
            Assert.That(migrated.Modules["WorldObjectState"], Is.SameAs(existingBlock));
            Assert.That(migrated.Modules["WorldObjectState"].ModuleVersion, Is.EqualTo(77));
            Assert.That(migrated.Modules["WorldObjectState"].PayloadJson, Is.EqualTo("{\"preserve\":true}"));
        }

        [Test]
        public void SchemaV1ToV2AddWorldObjectStateMigration_ReplacesNullWorldObjectStateBlockWithDefault()
        {
            var migration = new SchemaV1ToV2AddWorldObjectStateMigration();
            var envelope = new SaveEnvelope
            {
                SchemaVersion = 1,
                BuildVersion = "0.1.0-dev",
                CreatedAtUtc = "2026-02-27T00:00:00Z",
                FeatureFlags = new SaveFeatureFlags(),
                Modules = new Dictionary<string, ModuleSaveBlock>
                {
                    { "CoreWorld", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "Inventory", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "Weapons", new ModuleSaveBlock { ModuleVersion = 1, PayloadJson = "{}" } },
                    { "WorldObjectState", null }
                }
            };

            var migrated = migration.Apply(envelope);

            Assert.That(migrated.SchemaVersion, Is.EqualTo(2));
            Assert.That(migrated.Modules["WorldObjectState"], Is.Not.Null);
            Assert.That(migrated.Modules["WorldObjectState"].ModuleVersion, Is.EqualTo(1));
            Assert.That(migrated.Modules["WorldObjectState"].PayloadJson, Is.EqualTo("{}"));
        }

        [Test]
        public void WorldObjectStateModule_RoundTrip_PopulatedPayload_PreservesData()
        {
            var source = new WorldObjectStateModule();
            source.SceneObjectStates.Add(new WorldObjectStateModule.SceneObjectStateRecord
            {
                ScenePath = "Assets/Scenes/MainWorld.unity",
                Records = new List<WorldObjectStateModule.WorldObjectRecord>
                {
                    new WorldObjectStateModule.WorldObjectRecord
                    {
                        ObjectId = "pickup-001",
                        Consumed = true,
                        Destroyed = false,
                        HasTransformOverride = true,
                        PositionX = 1.5f,
                        PositionY = -2.25f,
                        PositionZ = 9f,
                        RotationX = 0f,
                        RotationY = 0.5f,
                        RotationZ = 0.25f,
                        RotationW = 0.75f,
                        LastUpdatedDay = 12,
                        ItemInstanceId = "item-123"
                    }
                }
            });
            source.ReclaimEntries.Add(new WorldObjectStateModule.ReclaimRecord
            {
                ScenePath = "Assets/Scenes/MainWorld.unity",
                ObjectId = "pickup-001",
                ItemInstanceId = "item-123",
                CleanedOnDay = 13
            });

            var payloadJson = source.CaptureModuleStateJson();
            var restored = new WorldObjectStateModule();
            restored.RestoreModuleStateFromJson(payloadJson);

            Assert.That(restored.SceneObjectStates.Count, Is.EqualTo(1));
            Assert.That(restored.SceneObjectStates[0].ScenePath, Is.EqualTo("Assets/Scenes/MainWorld.unity"));
            Assert.That(restored.SceneObjectStates[0].Records.Count, Is.EqualTo(1));

            var restoredRecord = restored.SceneObjectStates[0].Records[0];
            Assert.That(restoredRecord.ObjectId, Is.EqualTo("pickup-001"));
            Assert.That(restoredRecord.Consumed, Is.True);
            Assert.That(restoredRecord.Destroyed, Is.False);
            Assert.That(restoredRecord.HasTransformOverride, Is.True);
            Assert.That(restoredRecord.PositionX, Is.EqualTo(1.5f));
            Assert.That(restoredRecord.PositionY, Is.EqualTo(-2.25f));
            Assert.That(restoredRecord.PositionZ, Is.EqualTo(9f));
            Assert.That(restoredRecord.RotationX, Is.EqualTo(0f));
            Assert.That(restoredRecord.RotationY, Is.EqualTo(0.5f));
            Assert.That(restoredRecord.RotationZ, Is.EqualTo(0.25f));
            Assert.That(restoredRecord.RotationW, Is.EqualTo(0.75f));
            Assert.That(restoredRecord.LastUpdatedDay, Is.EqualTo(12));
            Assert.That(restoredRecord.ItemInstanceId, Is.EqualTo("item-123"));

            Assert.That(restored.ReclaimEntries.Count, Is.EqualTo(1));
            var reclaimRecord = restored.ReclaimEntries[0];
            Assert.That(reclaimRecord.ScenePath, Is.EqualTo("Assets/Scenes/MainWorld.unity"));
            Assert.That(reclaimRecord.ObjectId, Is.EqualTo("pickup-001"));
            Assert.That(reclaimRecord.ItemInstanceId, Is.EqualTo("item-123"));
            Assert.That(reclaimRecord.CleanedOnDay, Is.EqualTo(13));
        }

        [Test]
        public void WorldObjectStateModule_Restore_KeepsValidScene_WhenAllSceneRecordsNormalizeToEmpty()
        {
            var module = new WorldObjectStateModule();
            var payloadJson = "{\"sceneObjectStates\":[{\"scenePath\":\"Assets/Scenes/MainWorld.unity\",\"records\":[null,{\"objectId\":\"\"}]}]}";

            module.RestoreModuleStateFromJson(payloadJson);

            Assert.That(module.SceneObjectStates.Count, Is.EqualTo(1));
            Assert.That(module.SceneObjectStates[0].ScenePath, Is.EqualTo("Assets/Scenes/MainWorld.unity"));
            Assert.That(module.SceneObjectStates[0].Records.Count, Is.EqualTo(0));
        }

        [TestCase("{}")]
        [TestCase("{\"sceneObjectStates\":null}")]
        [TestCase("{\"sceneObjectStates\":[]}")]
        [TestCase("{\"sceneObjectStates\":[{\"scenePath\":\"\",\"records\":null}]}")]
        public void WorldObjectStateModule_Restore_ToleratesEmptyPayloadInternals(string payloadJson)
        {
            var module = new WorldObjectStateModule();

            Assert.DoesNotThrow(() => module.RestoreModuleStateFromJson(payloadJson));
            Assert.That(module.SceneObjectStates.Count, Is.EqualTo(0));
        }

        [Test]
        public void WorldObjectStateModule_Restore_BackwardCompatiblePayload_WithoutReclaimEntriesDefaultsEmpty()
        {
            var module = new WorldObjectStateModule();
            var payloadJson = "{\"sceneObjectStates\":[{\"scenePath\":\"Assets/Scenes/MainWorld.unity\",\"records\":[{\"objectId\":\"pickup-001\"}]}]}";

            module.RestoreModuleStateFromJson(payloadJson);

            Assert.That(module.SceneObjectStates.Count, Is.EqualTo(1));
            Assert.That(module.ReclaimEntries.Count, Is.EqualTo(0));
        }
    }
}
