using System.Collections;
using NUnit.Framework;
using Reloader.World.Travel;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Reloader.World.Tests.PlayMode
{
    public class RoundTripTravelPlayModeTests
    {
        private const string BootstrapSceneName = "Bootstrap";
        private const string MainTownSceneName = "MainTown";
        private const string IndoorRangeSceneName = "IndoorRangeInstance";
        private const float SceneSwitchTimeoutSeconds = 5f;

        [UnityTest]
        public IEnumerator RoundTripTravel_UsesSceneEntryPoints_InBothDirections()
        {
            SceneManager.sceneLoaded += EnsureTravelEntryPointsForTest;
            try
            {
                SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
                yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);

                var toIndoor = CreateTrigger(
                    "MainTownToIndoorTrigger",
                    new TravelContext(
                        IndoorRangeSceneName,
                        "entry.indoor.arrival",
                        "entry.maintown.return",
                        TravelActivityType.IndoorRange,
                        TravelTimeAdvancePolicy.ShortTrip));

                Assert.That(toIndoor.TryTravel(), Is.True);
                yield return WaitForActiveScene(IndoorRangeSceneName, SceneSwitchTimeoutSeconds);
                Assert.That(WorldTravelCoordinator.LastResolvedEntryPointId, Is.EqualTo("entry.indoor.arrival"));

                var toTown = CreateTrigger(
                    "IndoorToMainTownTrigger",
                    new TravelContext(
                        MainTownSceneName,
                        "entry.maintown.return",
                        "entry.indoor.arrival",
                        TravelActivityType.IndoorRange,
                        TravelTimeAdvancePolicy.ShortTrip));

                Assert.That(toTown.TryTravel(), Is.True);
                yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);
                Assert.That(WorldTravelCoordinator.LastResolvedEntryPointId, Is.EqualTo("entry.maintown.return"));
            }
            finally
            {
                SceneManager.sceneLoaded -= EnsureTravelEntryPointsForTest;
            }
        }

        private static TravelSceneTrigger CreateTrigger(string objectName, TravelContext context)
        {
            var triggerObject = new GameObject(objectName);
            var trigger = triggerObject.AddComponent<TravelSceneTrigger>();
            trigger.Configure(context);
            return trigger;
        }

        private static IEnumerator WaitForActiveScene(string expectedSceneName, float timeoutSeconds)
        {
            var elapsed = 0f;
            while (SceneManager.GetActiveScene().name != expectedSceneName && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(
                SceneManager.GetActiveScene().name,
                Is.EqualTo(expectedSceneName),
                $"Timed out waiting for scene '{expectedSceneName}'.");
        }

        private static void EnsureTravelEntryPointsForTest(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == MainTownSceneName)
            {
                CreateEntryPoint(scene, "MainTownEntry_Return", "entry.maintown.return");
            }
            else if (scene.name == IndoorRangeSceneName)
            {
                CreateEntryPoint(scene, "IndoorRangeEntry_Arrival", "entry.indoor.arrival");
            }
        }

        private static void CreateEntryPoint(Scene scene, string objectName, string entryPointId)
        {
            var gameObject = new GameObject(objectName);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            var entryPoint = gameObject.AddComponent<SceneEntryPoint>();
            JsonUtility.FromJsonOverwrite($"{{\"_entryPointId\":\"{entryPointId}\"}}", entryPoint);
            entryPoint.EnsureStableId();
        }
    }
}
