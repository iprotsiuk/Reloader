using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;
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
            var envelope = coordinator.CaptureEnvelope("0.3.0-dev");

            Assert.That(envelope.SchemaVersion, Is.EqualTo(8));
            Assert.That(envelope.Modules.ContainsKey("ContainerStorage"), Is.True);
            Assert.That(envelope.Modules["ContainerStorage"].ModuleVersion, Is.EqualTo(1));
        }

        [Test]
        public void SaveBootstrapper_DefaultCoordinatorLoad_RejectsSaveMissingContainerStorageModule()
        {
            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var repository = new SaveFileRepository();
            var envelope = coordinator.CaptureEnvelope("0.3.0-dev");
            envelope.Modules.Remove("ContainerStorage");

            repository.WriteEnvelope(_savePath, envelope);

            var ex = Assert.Throws<InvalidDataException>(() => coordinator.Load(_savePath));
            Assert.That(ex.Message, Does.Contain("Missing required module block"));
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
