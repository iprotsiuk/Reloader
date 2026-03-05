using System;
using NUnit.Framework;
using Reloader.Core.Persistence;

namespace Reloader.Core.Tests.EditMode
{
    public class WorldObjectStateContractsTests
    {
        [Test]
        public void WorldScenePersistencePolicy_Defaults_ArePersistentAndTrackingEnabled()
        {
            var policy = new WorldScenePersistencePolicy();

            Assert.That(policy.ScenePath, Is.EqualTo(string.Empty));
            Assert.That(policy.Mode, Is.EqualTo(WorldObjectPersistenceMode.Persistent));
            Assert.That(policy.RetentionDays, Is.EqualTo(0));
            Assert.That(policy.CleanupRuleSetId, Is.EqualTo(string.Empty));
            Assert.That(policy.TrackConsumed, Is.True);
            Assert.That(policy.TrackDestroyed, Is.True);
            Assert.That(policy.TrackTransforms, Is.True);
            Assert.That(policy.TrackSpawnedObjects, Is.True);
        }

        [Test]
        public void WorldObjectStateStore_Upsert_ReplacesExistingRecordForSameScenePathAndObjectId()
        {
            var store = new WorldObjectStateStore();
            var initial = new WorldObjectStateRecord
            {
                ObjectId = "pickup-001",
                Consumed = false,
                LastUpdatedDay = 1
            };
            var updated = new WorldObjectStateRecord
            {
                ObjectId = "pickup-001",
                Consumed = true,
                LastUpdatedDay = 2
            };

            store.Upsert("Assets/Scenes/MainWorld.unity", initial);
            store.Upsert("Assets/Scenes/MainWorld.unity", updated);

            Assert.That(store.Count, Is.EqualTo(1));
            Assert.That(store.TryGet("Assets/Scenes/MainWorld.unity", "pickup-001", out var actual), Is.True);
            Assert.That(actual.Consumed, Is.True);
            Assert.That(actual.LastUpdatedDay, Is.EqualTo(2));
        }

        [Test]
        public void WorldObjectStateStore_KeysByScenePathPlusObjectId_SameObjectIdAcrossScenesDoesNotCollide()
        {
            var store = new WorldObjectStateStore();

            store.Upsert("Assets/Scenes/MainWorld.unity", new WorldObjectStateRecord { ObjectId = "shared-id", Consumed = true });
            store.Upsert("Assets/Scenes/IndoorRange.unity", new WorldObjectStateRecord { ObjectId = "shared-id", Consumed = false });

            Assert.That(store.Count, Is.EqualTo(2));
            Assert.That(store.TryGet("Assets/Scenes/MainWorld.unity", "shared-id", out var mainWorld), Is.True);
            Assert.That(store.TryGet("Assets/Scenes/IndoorRange.unity", "shared-id", out var indoorRange), Is.True);
            Assert.That(mainWorld.Consumed, Is.True);
            Assert.That(indoorRange.Consumed, Is.False);
        }

        [Test]
        public void WorldObjectStateStore_UpsertAndTryGet_UseStoredRecordInstance()
        {
            var store = new WorldObjectStateStore();
            var record = new WorldObjectStateRecord { ObjectId = "pickup-001" };

            store.Upsert("Assets/Scenes/MainWorld.unity", record);

            Assert.That(store.TryGet("Assets/Scenes/MainWorld.unity", "pickup-001", out var actual), Is.True);
            Assert.That(object.ReferenceEquals(actual, record), Is.True);
        }

        [Test]
        public void WorldObjectStateStore_Keying_DoesNotNormalizeScenePath()
        {
            var store = new WorldObjectStateStore();

            store.Upsert(" Assets/Scenes/MainWorld.unity ", new WorldObjectStateRecord { ObjectId = "pickup-001" });

            Assert.That(store.TryGet("Assets/Scenes/MainWorld.unity", "pickup-001", out _), Is.False);
            Assert.That(store.TryGet(" Assets/Scenes/MainWorld.unity ", "pickup-001", out _), Is.True);
        }

        [Test]
        public void WorldObjectStateStore_Upsert_ThrowsOnNullRecord()
        {
            var store = new WorldObjectStateStore();

            Assert.Throws<ArgumentNullException>(() => store.Upsert("Assets/Scenes/MainWorld.unity", null));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void WorldObjectStateStore_Upsert_ThrowsOnInvalidScenePath(string scenePath)
        {
            var store = new WorldObjectStateStore();

            Assert.Throws<ArgumentException>(() =>
                store.Upsert(scenePath, new WorldObjectStateRecord { ObjectId = "pickup-001" }));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void WorldObjectStateStore_Upsert_ThrowsOnInvalidRecordObjectId(string objectId)
        {
            var store = new WorldObjectStateStore();

            Assert.Throws<ArgumentException>(() =>
                store.Upsert("Assets/Scenes/MainWorld.unity", new WorldObjectStateRecord { ObjectId = objectId }));
        }

        [TestCase(null, "pickup-001")]
        [TestCase("", "pickup-001")]
        [TestCase(" ", "pickup-001")]
        [TestCase("Assets/Scenes/MainWorld.unity", null)]
        [TestCase("Assets/Scenes/MainWorld.unity", "")]
        [TestCase("Assets/Scenes/MainWorld.unity", " ")]
        public void WorldObjectStateStore_TryGet_ThrowsOnInvalidIdentifiers(string scenePath, string objectId)
        {
            var store = new WorldObjectStateStore();

            Assert.Throws<ArgumentException>(() => store.TryGet(scenePath, objectId, out _));
        }

        [Test]
        public void WorldObjectStateStore_Keying_DoesNotCollideWhenValuesContainDelimiterCharacter()
        {
            var store = new WorldObjectStateStore();
            var delimiter = "\u001f";

            var scenePathA = "sceneA" + delimiter + "tail";
            var objectIdA = "idA";
            var scenePathB = "sceneA";
            var objectIdB = "tail" + delimiter + "idA";

            store.Upsert(scenePathA, new WorldObjectStateRecord { ObjectId = objectIdA, Consumed = true });
            store.Upsert(scenePathB, new WorldObjectStateRecord { ObjectId = objectIdB, Consumed = false });

            Assert.That(store.Count, Is.EqualTo(2));
            Assert.That(store.TryGet(scenePathA, objectIdA, out var first), Is.True);
            Assert.That(store.TryGet(scenePathB, objectIdB, out var second), Is.True);
            Assert.That(first.Consumed, Is.True);
            Assert.That(second.Consumed, Is.False);
        }

        [Test]
        public void WorldObjectPersistenceRuntimeBridge_MarkConsumed_MergesIntoExistingRecordWithoutOverwritingOtherFields()
        {
            WorldObjectPersistenceRuntimeBridge.ResetForTests();
            var scenePath = "Assets/Scenes/MainWorld.unity";
            var objectId = "pickup-merge-001";
            var originalPosition = new UnityEngine.Vector3(12f, 3f, -8f);
            var originalRotation = UnityEngine.Quaternion.Euler(5f, 45f, 10f);
            var original = new WorldObjectStateRecord
            {
                ObjectId = objectId,
                Consumed = false,
                Destroyed = true,
                HasTransformOverride = true,
                Position = originalPosition,
                Rotation = originalRotation,
                LastUpdatedDay = 123,
                ItemInstanceId = "instance-77"
            };

            WorldObjectPersistenceRuntimeBridge.StateStore.Upsert(scenePath, original);
            WorldObjectPersistenceRuntimeBridge.MarkConsumed(scenePath, objectId);

            Assert.That(WorldObjectPersistenceRuntimeBridge.StateStore.TryGet(scenePath, objectId, out var merged), Is.True);
            Assert.That(merged.Consumed, Is.True);
            Assert.That(merged.Destroyed, Is.True);
            Assert.That(merged.HasTransformOverride, Is.True);
            Assert.That(merged.Position, Is.EqualTo(originalPosition));
            Assert.That(merged.Rotation, Is.EqualTo(originalRotation));
            Assert.That(merged.LastUpdatedDay, Is.EqualTo(123));
            Assert.That(merged.ItemInstanceId, Is.EqualTo("instance-77"));

            WorldObjectPersistenceRuntimeBridge.ResetForTests();
        }

        [Test]
        public void WorldObjectPersistenceRuntimeBridge_MarkRuntimeSpawned_DoesNotClearConsumedOrDestroyedFlags()
        {
            WorldObjectPersistenceRuntimeBridge.ResetForTests();
            var scenePath = "Assets/Scenes/MainWorld.unity";
            var objectId = "drop-merge-001";

            WorldObjectPersistenceRuntimeBridge.StateStore.Upsert(scenePath, new WorldObjectStateRecord
            {
                ObjectId = objectId,
                Consumed = true,
                Destroyed = true,
                ItemInstanceId = "drop-instance-001",
                ItemDefinitionId = "weapon-kar98k",
                StackQuantity = 1
            });

            WorldObjectPersistenceRuntimeBridge.MarkRuntimeSpawned(
                scenePath,
                objectId,
                "weapon-kar98k",
                1,
                new UnityEngine.Vector3(2f, 0.5f, -1f),
                UnityEngine.Quaternion.Euler(0f, 40f, 0f),
                runtimeDropInstanceId: null);

            Assert.That(WorldObjectPersistenceRuntimeBridge.StateStore.TryGet(scenePath, objectId, out var record), Is.True);
            Assert.That(record.Consumed, Is.True);
            Assert.That(record.Destroyed, Is.True);
            Assert.That(record.ItemInstanceId, Is.EqualTo("drop-instance-001"));

            WorldObjectPersistenceRuntimeBridge.ResetForTests();
        }
    }
}
