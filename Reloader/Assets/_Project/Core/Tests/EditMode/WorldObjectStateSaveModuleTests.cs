using System.Collections.Generic;
using NUnit.Framework;
using Reloader.Core.Save;
using Reloader.Core.Save.Migrations;
using Reloader.Core.Save.Modules;

namespace Reloader.Core.Tests.EditMode
{
    public class WorldObjectStateSaveModuleTests
    {
        [Test]
        public void SaveBootstrapper_DefaultCoordinatorCapture_IncludesWorldObjectStateModule()
        {
            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var envelope = coordinator.CaptureEnvelope("0.2.0-dev", new SaveFeatureFlags());

            Assert.That(envelope.SchemaVersion, Is.EqualTo(2));
            Assert.That(envelope.Modules.ContainsKey("WorldObjectState"), Is.True);
            Assert.That(envelope.Modules["WorldObjectState"].ModuleVersion, Is.EqualTo(1));
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
    }
}
