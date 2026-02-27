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
    }
}
