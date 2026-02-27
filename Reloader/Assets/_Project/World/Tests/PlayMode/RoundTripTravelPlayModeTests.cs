using System.Collections;
using NUnit.Framework;
using Reloader.World.Travel;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using System.Reflection;

namespace Reloader.World.Tests.PlayMode
{
    public class RoundTripTravelPlayModeTests
    {
        private const string BootstrapSceneName = "Bootstrap";
        private const string MainTownSceneName = "MainTown";
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";

        private const string IndoorRangeSceneName = "IndoorRangeInstance";
        private const float SceneSwitchTimeoutSeconds = 5f;

        [UnityTest]
        public IEnumerator BootstrapLoad_DoesNotAutoTravelToIndoorRange()
        {
            SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
            yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);

            var elapsed = 0f;
            while (elapsed < 1.5f)
            {
                Assert.That(
                    SceneManager.GetActiveScene().name,
                    Is.EqualTo(MainTownSceneName),
                    "MainTown should remain active after bootstrap load and must not auto-travel.");
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator IndoorRange_PlayerRig_HasInputAssetAndBeltHud()
        {
            SceneManager.LoadScene(IndoorRangeSceneName, LoadSceneMode.Single);
            yield return WaitForActiveScene(IndoorRangeSceneName, SceneSwitchTimeoutSeconds);

            var playerRoot = GameObject.Find("PlayerRoot");
            Assert.That(playerRoot, Is.Not.Null, "Expected PlayerRoot in IndoorRange scene.");

            var inputReader = playerRoot.GetComponent("PlayerInputReader");
            Assert.That(inputReader, Is.Not.Null, "Expected PlayerInputReader on IndoorRange PlayerRoot.");

            var actionsField = inputReader.GetType().GetField("_actionsAsset", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(actionsField, Is.Not.Null, "Expected _actionsAsset field on PlayerInputReader.");
            var actionsAsset = actionsField.GetValue(inputReader);
            Assert.That(actionsAsset, Is.Not.Null, "IndoorRange PlayerInputReader must have an InputActionAsset assigned.");

            var beltHud = GameObject.Find("BeltHud");
            Assert.That(beltHud, Is.Not.Null, "IndoorRange scene should include BeltHud runtime prefab.");
        }

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
            AssertPlayerRootIsAtEntryPoint("entry.indoor.arrival");

            var returnInteractor = CreatePlayerInteractor();
            var toTownObject = GameObject.Find("IndoorRange_SmokeToMainTown_Trigger");
            Assert.That(toTownObject, Is.Not.Null, "Expected authored smoke trigger in IndoorRangeInstance.");
            var toTown = toTownObject.GetComponent<TravelSceneTrigger>();
            Assert.That(toTown, Is.Not.Null);

            Assert.That(toTown.TryHandleInteractor(returnInteractor), Is.True);
            yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);
            yield return WaitForResolvedEntryPoint("entry.maintown.return", SceneSwitchTimeoutSeconds);
            AssertPlayerRootIsAtEntryPoint("entry.maintown.return");
        }

        [UnityTest]
        public IEnumerator TryLoadSceneAtEntry_MatchesScenePathIdentifier_OnSceneLoaded()
        {
            SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
            yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);

            var started = WorldTravelCoordinator.TryLoadSceneAtEntry(MainTownScenePath, "entry.maintown.spawn");
            Assert.That(started, Is.True);

            yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);
            yield return WaitForResolvedEntryPoint("entry.maintown.spawn", SceneSwitchTimeoutSeconds);
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

        private static void AssertPlayerRootIsAtEntryPoint(string entryPointId)
        {
            var activeScene = SceneManager.GetActiveScene();
            var playerRoot = FindPlayerRootInScene(activeScene);
            Assert.That(playerRoot, Is.Not.Null, $"Expected PlayerRoot in scene '{activeScene.name}'.");

            var entryPoint = FindEntryPointInScene(activeScene, entryPointId);
            Assert.That(entryPoint, Is.Not.Null, $"Expected SceneEntryPoint '{entryPointId}' in scene '{activeScene.name}'.");

            var distance = Vector3.Distance(playerRoot.position, entryPoint.transform.position);
            Assert.That(
                distance,
                Is.LessThanOrEqualTo(0.25f),
                $"Expected PlayerRoot to be moved to '{entryPointId}', but distance was {distance:0.###}.");
        }

        private static Transform FindPlayerRootInScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root != null && root.name == "PlayerRoot")
                {
                    return root.transform;
                }
            }

            return null;
        }

        private static SceneEntryPoint FindEntryPointInScene(Scene scene, string entryPointId)
        {
            var candidates = Object.FindObjectsByType<SceneEntryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (candidate == null || candidate.gameObject.scene != scene)
                {
                    continue;
                }

                if (candidate.EntryPointId == entryPointId)
                {
                    return candidate;
                }
            }

            return null;
        }

    }
}
