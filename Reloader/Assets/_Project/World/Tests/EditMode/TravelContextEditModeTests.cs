using System;
using NUnit.Framework;
using Reloader.World.Travel;
using UnityEngine;

namespace Reloader.World.Tests.EditMode
{
    public class TravelContextEditModeTests
    {
        [Test]
        public void TravelContext_IsSerializable_AndAssignsRequiredFields()
        {
            var context = new TravelContext(
                "MainTown",
                "entry.indoor-range",
                "entry.main-town.return",
                TravelActivityType.IndoorRange,
                TravelTimeAdvancePolicy.ShortTrip);

            Assert.That(Attribute.IsDefined(typeof(TravelContext), typeof(SerializableAttribute)), Is.True);
            Assert.That(context.DestinationSceneName, Is.EqualTo("MainTown"));
            Assert.That(context.DestinationEntryPointId, Is.EqualTo("entry.indoor-range"));
            Assert.That(context.ReturnEntryPointId, Is.EqualTo("entry.main-town.return"));
            Assert.That(context.ActivityType, Is.EqualTo(TravelActivityType.IndoorRange));
            Assert.That(context.TimeAdvancePolicy, Is.EqualTo(TravelTimeAdvancePolicy.ShortTrip));
        }

        [Test]
        public void TravelContext_ThrowsWhenDestinationSceneNameMissing()
        {
            Assert.That(
                () => new TravelContext(" ", "entry-a", "entry-b", TravelActivityType.IndoorRange, TravelTimeAdvancePolicy.None),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void TravelContext_ThrowsWhenDestinationEntryPointIdMissing()
        {
            Assert.That(
                () => new TravelContext("MainTown", "", "entry-b", TravelActivityType.IndoorRange, TravelTimeAdvancePolicy.None),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void TravelContext_ThrowsWhenReturnEntryPointIdMissing()
        {
            Assert.That(
                () => new TravelContext("MainTown", "entry-a", null, TravelActivityType.IndoorRange, TravelTimeAdvancePolicy.None),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void SceneEntryPoint_EnsureStableId_RepairsMissingIdAndKeepsValueStable()
        {
            var gameObject = new GameObject("entry");
            var entryPoint = gameObject.AddComponent<SceneEntryPoint>();

            try
            {
                var originalId = entryPoint.EntryPointId;
                Assert.That(string.IsNullOrWhiteSpace(originalId), Is.False);

                JsonUtility.FromJsonOverwrite("{\"_entryPointId\":\"\"}", entryPoint);
                entryPoint.EnsureStableId();
                var repairedId = entryPoint.EntryPointId;

                Assert.That(string.IsNullOrWhiteSpace(repairedId), Is.False);

                entryPoint.EnsureStableId();
                Assert.That(entryPoint.EntryPointId, Is.EqualTo(repairedId));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SceneEntryPoint_TryFindById_ReturnsFalseForMissingOrInvalidIds()
        {
            var firstObject = new GameObject("first");
            var secondObject = new GameObject("second");
            var first = firstObject.AddComponent<SceneEntryPoint>();
            var second = secondObject.AddComponent<SceneEntryPoint>();

            try
            {
                JsonUtility.FromJsonOverwrite("{\"_entryPointId\":\"entry.first\"}", first);
                JsonUtility.FromJsonOverwrite("{\"_entryPointId\":\"entry.second\"}", second);

                var found = SceneEntryPoint.TryFindById(new[] { first, second }, "entry.second", out var resolved);
                Assert.That(found, Is.True);
                Assert.That(resolved, Is.SameAs(second));

                var missing = SceneEntryPoint.TryFindById(new[] { first, second }, "entry.missing", out var missingResolved);
                Assert.That(missing, Is.False);
                Assert.That(missingResolved, Is.Null);

                var invalid = SceneEntryPoint.TryFindById(new[] { first, second }, " ", out var invalidResolved);
                Assert.That(invalid, Is.False);
                Assert.That(invalidResolved, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(firstObject);
                UnityEngine.Object.DestroyImmediate(secondObject);
            }
        }
    }
}
