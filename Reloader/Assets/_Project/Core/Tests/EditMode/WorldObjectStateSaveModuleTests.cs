using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Reloader.Core.Persistence;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;
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
            var envelope = coordinator.CaptureEnvelope("0.2.0-dev");

            Assert.That(envelope.SchemaVersion, Is.EqualTo(10));
            Assert.That(envelope.Modules.ContainsKey("WorldObjectState"), Is.True);
            Assert.That(envelope.Modules["WorldObjectState"].ModuleVersion, Is.EqualTo(1));
        }

        [Test]
        public void SaveBootstrapper_DefaultCoordinatorLoad_RejectsSaveMissingWorldObjectStateModule()
        {
            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var repository = new SaveFileRepository();
            var envelope = coordinator.CaptureEnvelope("0.2.0-dev");
            envelope.Modules.Remove("WorldObjectState");

            repository.WriteEnvelope(_savePath, envelope);

            var ex = Assert.Throws<InvalidDataException>(() => coordinator.Load(_savePath));
            Assert.That(ex.Message, Does.Contain("Missing required module block"));
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
        public void WorldObjectStateModule_Restore_MissingReclaimEntries_DefaultsEmpty()
        {
            var module = new WorldObjectStateModule();
            var payloadJson = "{\"sceneObjectStates\":[{\"scenePath\":\"Assets/Scenes/MainWorld.unity\",\"records\":[{\"objectId\":\"pickup-001\"}]}]}";

            module.RestoreModuleStateFromJson(payloadJson);

            Assert.That(module.SceneObjectStates.Count, Is.EqualTo(1));
            Assert.That(module.ReclaimEntries.Count, Is.EqualTo(0));
        }

        [Test]
        public void SaveCoordinator_CaptureEnvelope_CopiesRuntimeWorldObjectStateIntoModule()
        {
            WorldObjectPersistenceRuntimeBridge.ResetForTests();
            try
            {
                WorldObjectPersistenceRuntimeBridge.StateStore.Upsert("Assets/_Project/World/Scenes/MainTown.unity", new WorldObjectStateRecord
                {
                    ObjectId = "drop.runtime.001",
                    HasTransformOverride = true,
                    Position = new UnityEngine.Vector3(1.5f, 0.5f, -3f),
                    Rotation = UnityEngine.Quaternion.Euler(0f, 35f, 0f),
                    ItemDefinitionId = "weapon-kar98k",
                    ItemInstanceId = "drop-instance-001",
                    StackQuantity = 2
                });

                var reclaimSeed = new WorldObjectStateRecord
                {
                    ObjectId = "drop.runtime.cleaned",
                    ItemInstanceId = "drop-instance-cleaned",
                    Consumed = true
                };
                WorldObjectPersistenceRuntimeBridge.ReclaimStorage.AddFromRecord(
                    "Assets/_Project/World/Scenes/MainTown.unity",
                    reclaimSeed,
                    cleanedOnDay: 12);

                var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
                var envelope = coordinator.CaptureEnvelope("0.9.0-dev");
                var module = new WorldObjectStateModule();
                module.RestoreModuleStateFromJson(envelope.Modules["WorldObjectState"].PayloadJson);

                Assert.That(module.SceneObjectStates.Count, Is.EqualTo(1));
                Assert.That(module.SceneObjectStates[0].ScenePath, Is.EqualTo("Assets/_Project/World/Scenes/MainTown.unity"));
                Assert.That(module.SceneObjectStates[0].Records.Count, Is.EqualTo(1));
                Assert.That(module.SceneObjectStates[0].Records[0].ObjectId, Is.EqualTo("drop.runtime.001"));
                Assert.That(module.SceneObjectStates[0].Records[0].ItemInstanceId, Is.EqualTo("drop-instance-001"));
                Assert.That(module.SceneObjectStates[0].Records[0].ItemDefinitionId, Is.EqualTo("weapon-kar98k"));
                Assert.That(module.SceneObjectStates[0].Records[0].StackQuantity, Is.EqualTo(2));

                Assert.That(module.ReclaimEntries.Count, Is.EqualTo(1));
                Assert.That(module.ReclaimEntries[0].ItemInstanceId, Is.EqualTo("drop-instance-cleaned"));
            }
            finally
            {
                WorldObjectPersistenceRuntimeBridge.ResetForTests();
            }
        }

        [Test]
        public void SaveCoordinator_Load_RestoresRuntimeWorldObjectState_OnSinglePersistencePath()
        {
            WorldObjectPersistenceRuntimeBridge.ResetForTests();
            try
            {
                var module = new WorldObjectStateModule();
                module.SceneObjectStates.Add(new WorldObjectStateModule.SceneObjectStateRecord
                {
                    ScenePath = "Assets/_Project/World/Scenes/MainTown.unity",
                    Records = new List<WorldObjectStateModule.WorldObjectRecord>
                    {
                        new WorldObjectStateModule.WorldObjectRecord
                        {
                            ObjectId = "drop.runtime.002",
                            HasTransformOverride = true,
                            PositionX = 6f,
                            PositionY = 0.25f,
                            PositionZ = -9f,
                            RotationX = 0f,
                            RotationY = 0.258819f,
                            RotationZ = 0f,
                            RotationW = 0.9659258f,
                            ItemDefinitionId = "ammo-308",
                            ItemInstanceId = "drop-instance-002",
                            StackQuantity = 4
                        }
                    }
                });

                var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
                var repository = new SaveFileRepository();
                var envelope = coordinator.CaptureEnvelope("0.9.0-dev");
                var playerStateModule = new PlayerStateModule
                {
                    CurrentScenePath = "Assets/_Project/World/Scenes/MainTown.unity",
                    CurrentAnchorId = "entry.maintown.return",
                    PositionX = 0f,
                    PositionY = 0f,
                    PositionZ = 0f,
                    RotationX = 0f,
                    RotationY = 0f,
                    RotationZ = 0f,
                    RotationW = 1f,
                    SelectedBeltSlotIndex = -1
                };

                envelope.Modules["PlayerState"] = new ModuleSaveBlock
                {
                    ModuleVersion = 1,
                    PayloadJson = playerStateModule.CaptureModuleStateJson()
                };
                envelope.Modules["WorldObjectState"] = new ModuleSaveBlock
                {
                    ModuleVersion = 1,
                    PayloadJson = module.CaptureModuleStateJson()
                };
                repository.WriteEnvelope(_savePath, envelope);

                coordinator.Load(_savePath);
                coordinator.Load(_savePath);

                Assert.That(WorldObjectPersistenceRuntimeBridge.StateStore.Count, Is.EqualTo(1));
                Assert.That(WorldObjectPersistenceRuntimeBridge.StateStore.TryGet(
                    "Assets/_Project/World/Scenes/MainTown.unity",
                    "drop.runtime.002",
                    out var restoredRecord), Is.True);
                Assert.That(restoredRecord.ItemInstanceId, Is.EqualTo("drop-instance-002"));
                Assert.That(restoredRecord.ItemDefinitionId, Is.EqualTo("ammo-308"));
                Assert.That(restoredRecord.StackQuantity, Is.EqualTo(4));
            }
            finally
            {
                WorldObjectPersistenceRuntimeBridge.ResetForTests();
            }
        }
    }
}
