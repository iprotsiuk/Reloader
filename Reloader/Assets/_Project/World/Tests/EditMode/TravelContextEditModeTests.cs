using System;
using NUnit.Framework;
using Reloader.World.Travel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
                TravelActivityType.ContractPrep,
                TravelTimeAdvancePolicy.ShortTrip);

            Assert.That(Attribute.IsDefined(typeof(TravelContext), typeof(SerializableAttribute)), Is.True);
            Assert.That(context.DestinationSceneName, Is.EqualTo("MainTown"));
            Assert.That(context.DestinationEntryPointId, Is.EqualTo("entry.indoor-range"));
            Assert.That(context.ReturnEntryPointId, Is.EqualTo("entry.main-town.return"));
            Assert.That(context.ActivityType, Is.EqualTo(TravelActivityType.ContractPrep));
            Assert.That(context.TimeAdvancePolicy, Is.EqualTo(TravelTimeAdvancePolicy.ShortTrip));
        }

        [Test]
        public void TravelActivityType_ContainsContractPrepAndContractExecutionLanguage()
        {
            Assert.That(Enum.IsDefined(typeof(TravelActivityType), nameof(TravelActivityType.ContractPrep)), Is.True);
            Assert.That(Enum.IsDefined(typeof(TravelActivityType), nameof(TravelActivityType.ContractExecution)), Is.True);
            Assert.That(Enum.IsDefined(typeof(TravelActivityType), nameof(TravelActivityType.PoliceEscape)), Is.True);
            Assert.That(Enum.IsDefined(typeof(TravelActivityType), nameof(TravelActivityType.TradeAndResupply)), Is.True);
        }

        [Test]
        public void TravelContext_ThrowsWhenDestinationSceneNameMissing()
        {
            Assert.That(
                () => new TravelContext(" ", "entry-a", "entry-b", TravelActivityType.ContractPrep, TravelTimeAdvancePolicy.None),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void TravelContext_ThrowsWhenDestinationEntryPointIdMissing()
        {
            Assert.That(
                () => new TravelContext("MainTown", "", "entry-b", TravelActivityType.ContractPrep, TravelTimeAdvancePolicy.None),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void TravelContext_ThrowsWhenReturnEntryPointIdMissing()
        {
            Assert.That(
                () => new TravelContext("MainTown", "entry-a", null, TravelActivityType.ContractPrep, TravelTimeAdvancePolicy.None),
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

        [Test]
        public void TravelSceneTrigger_IsInteractorAllowed_UsesOptionalTagFilter()
        {
            var triggerObject = new GameObject("trigger");
            var interactor = new GameObject("interactor");
            var trigger = triggerObject.AddComponent<TravelSceneTrigger>();

            try
            {
                Assert.That(trigger.IsInteractorAllowed(interactor), Is.True);

                trigger.SetInteractorTag("Untagged");
                Assert.That(trigger.IsInteractorAllowed(interactor), Is.True);

                trigger.SetInteractorTag("Player");
                Assert.That(trigger.IsInteractorAllowed(interactor), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(triggerObject);
                UnityEngine.Object.DestroyImmediate(interactor);
            }
        }

        [Test]
        public void TravelSceneTrigger_TryHandleInteractor_ReturnsFalseWithoutContext()
        {
            var triggerObject = new GameObject("trigger");
            var interactor = new GameObject("interactor");
            var trigger = triggerObject.AddComponent<TravelSceneTrigger>();

            try
            {
                Assert.That(trigger.TryHandleInteractor(interactor), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(triggerObject);
                UnityEngine.Object.DestroyImmediate(interactor);
            }
        }

        [Test]
        public void WorldTravelCoordinator_TryTravel_ReturnsFalse_WhenContextIsInvalid()
        {
            var context = new TravelContext(
                "MainTown",
                "entry.indoor-range",
                "entry.main-town.return",
                TravelActivityType.ContractPrep,
                TravelTimeAdvancePolicy.None);

            JsonUtility.FromJsonOverwrite("{\"_destinationSceneName\":\"\"}", context);

            var result = false;
            Assert.That(() => result = WorldTravelCoordinator.TryTravel(context), Throws.Nothing);
            Assert.That(result, Is.False);
        }

        [Test]
        public void WorldTravelCoordinator_TryLoadSceneAtEntry_ReturnsFalse_WhenSceneIsMissing()
        {
            var result = false;
            Assert.That(() => result = WorldTravelCoordinator.TryLoadSceneAtEntry("__MissingScene__", "entry.any"), Throws.Nothing);
            Assert.That(result, Is.False);
        }

        [Test]
        public void TravelSceneTrigger_IsInteractorAllowed_ReturnsFalse_WhenRequiredTagIsInvalid()
        {
            var triggerObject = new GameObject("trigger");
            var interactor = new GameObject("interactor");
            var trigger = triggerObject.AddComponent<TravelSceneTrigger>();

            try
            {
                trigger.SetInteractorTag("__InvalidTag__");
                var previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
                LogAssert.ignoreFailingMessages = true;
                var allowed = true;
                try
                {
                    Assert.That(() => allowed = trigger.IsInteractorAllowed(interactor), Throws.Nothing);
                    Assert.That(allowed, Is.False);
                }
                finally
                {
                    LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(triggerObject);
                UnityEngine.Object.DestroyImmediate(interactor);
            }
        }

        [Test]
        public void TravelSceneTrigger_Contract_DoesNotSerializePlayerRootReference()
        {
            var triggerObject = new GameObject("trigger");
            var trigger = triggerObject.AddComponent<TravelSceneTrigger>();

            try
            {
                var serialized = new SerializedObject(trigger);
                Assert.That(serialized.FindProperty("_travelContext"), Is.Not.Null);
                Assert.That(serialized.FindProperty("_requiredInteractorTag"), Is.Not.Null);
                Assert.That(serialized.FindProperty("_playerRoot"), Is.Null);
                Assert.That(serialized.FindProperty("_scenePlayerRoot"), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(triggerObject);
            }
        }

        [TestCase("Assets/_Project/World/Scenes/MainTown.unity", "entry.maintown.spawn", "entry.maintown.return")]
        [TestCase("Assets/_Project/World/Scenes/IndoorRangeInstance.unity", "entry.indoor.arrival")]
        public void SceneEntryPoints_ProvideExplicitAnchorIds(string scenePath, params string[] expectedEntryPointIds)
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            try
            {
                for (var i = 0; i < expectedEntryPointIds.Length; i++)
                {
                    var expectedEntryPointId = expectedEntryPointIds[i];
                    Assert.That(HasEntryPoint(scene, expectedEntryPointId), Is.True,
                        $"Scene '{scenePath}' should expose explicit anchor id '{expectedEntryPointId}' for spawn resolution.");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        [TestCase(
            "Assets/_Project/World/Scenes/MainTown.unity",
            "Assets/_Project/World/Scenes/IndoorRangeInstance.unity",
            "entry.indoor.arrival",
            "entry.maintown.return")]
        [TestCase(
            "Assets/_Project/World/Scenes/IndoorRangeInstance.unity",
            "Assets/_Project/World/Scenes/MainTown.unity",
            "entry.maintown.return",
            "entry.indoor.arrival")]
        public void TravelSceneTrigger_UsesAnchorIdsThatResolveInDestinationScenes(
            string originScenePath,
            string destinationScenePath,
            string expectedDestinationEntryPointId,
            string expectedReturnEntryPointId)
        {
            var originalScene = SceneManager.GetActiveScene();
            var destinationScene = EditorSceneManager.OpenScene(destinationScenePath, OpenSceneMode.Additive);
            var originScene = EditorSceneManager.OpenScene(originScenePath, OpenSceneMode.Additive);

            try
            {
                var trigger = FindComponentInScene<TravelSceneTrigger>(originScene);
                Assert.That(trigger, Is.Not.Null, $"Expected TravelSceneTrigger in scene '{originScenePath}'.");

                var serializedTrigger = new SerializedObject(trigger);
                var travelContextProperty = serializedTrigger.FindProperty("_travelContext");
                Assert.That(travelContextProperty, Is.Not.Null);

                var destinationSceneName = travelContextProperty.FindPropertyRelative("_destinationSceneName")?.stringValue;
                var destinationEntryPointId = travelContextProperty.FindPropertyRelative("_destinationEntryPointId")?.stringValue;
                var returnEntryPointId = travelContextProperty.FindPropertyRelative("_returnEntryPointId")?.stringValue;

                Assert.That(destinationSceneName, Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(destinationScenePath)));
                Assert.That(destinationEntryPointId, Is.EqualTo(expectedDestinationEntryPointId));
                Assert.That(returnEntryPointId, Is.EqualTo(expectedReturnEntryPointId));
                Assert.That(HasEntryPoint(destinationScene, destinationEntryPointId), Is.True,
                    $"Travel trigger in '{originScenePath}' should resolve destination entry point '{destinationEntryPointId}' via explicit scene anchors.");
            }
            finally
            {
                EditorSceneManager.CloseScene(originScene, true);
                EditorSceneManager.CloseScene(destinationScene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        private static bool HasEntryPoint(Scene scene, string entryPointId)
        {
            if (!scene.IsValid() || string.IsNullOrWhiteSpace(entryPointId))
            {
                return false;
            }

            var entryPoint = FindEntryPoint(scene, entryPointId);
            return entryPoint != null;
        }

        private static SceneEntryPoint FindEntryPoint(Scene scene, string entryPointId)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var entryPoints = roots[i].GetComponentsInChildren<SceneEntryPoint>(true);
                for (var j = 0; j < entryPoints.Length; j++)
                {
                    var candidate = entryPoints[j];
                    if (candidate != null && candidate.EntryPointId == entryPointId)
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid())
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var candidate = roots[i].GetComponentInChildren<T>(true);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
