using System.Collections;
using NUnit.Framework;
using Reloader.World.Travel;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

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
            SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
            yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);

            var playerHouse = GameObject.Find("PlayerHouse");
            Assert.That(playerHouse, Is.Not.Null, "Expected authored PlayerHouse object in MainTown.");

            var interactor = CreatePlayerInteractor();
            var toIndoorObject = GameObject.Find("MainTown_SmokeToIndoor_Trigger");
            Assert.That(toIndoorObject, Is.Not.Null, "Expected authored smoke trigger in MainTown.");
            var toIndoor = toIndoorObject.GetComponent<TravelSceneTrigger>();
            Assert.That(toIndoor, Is.Not.Null);

            Assert.That(toIndoor.TryHandleInteractor(interactor), Is.True);
            yield return WaitForActiveScene(IndoorRangeSceneName, SceneSwitchTimeoutSeconds);
            yield return WaitForResolvedEntryPoint("entry.indoor.arrival", SceneSwitchTimeoutSeconds);

            var returnInteractor = CreatePlayerInteractor();
            var toTownObject = GameObject.Find("IndoorRange_SmokeToMainTown_Trigger");
            Assert.That(toTownObject, Is.Not.Null, "Expected authored smoke trigger in IndoorRangeInstance.");
            var toTown = toTownObject.GetComponent<TravelSceneTrigger>();
            Assert.That(toTown, Is.Not.Null);

            Assert.That(toTown.TryHandleInteractor(returnInteractor), Is.True);
            yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);
            yield return WaitForResolvedEntryPoint("entry.maintown.return", SceneSwitchTimeoutSeconds);
        }

        private static GameObject CreatePlayerInteractor()
        {
            var interactor = new GameObject("TestPlayerInteractor");
            interactor.tag = "Player";
            return interactor;
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

        private static IEnumerator WaitForResolvedEntryPoint(string expectedEntryPointId, float timeoutSeconds)
        {
            var elapsed = 0f;
            while (WorldTravelCoordinator.LastResolvedEntryPointId != expectedEntryPointId && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(
                WorldTravelCoordinator.LastResolvedEntryPointId,
                Is.EqualTo(expectedEntryPointId),
                $"Timed out waiting for resolved entry point '{expectedEntryPointId}'.");
        }

    }
}
