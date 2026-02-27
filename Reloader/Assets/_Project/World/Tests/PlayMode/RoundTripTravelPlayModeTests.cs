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

            var startedTravel = false;
            var startTimeout = 2f;
            var elapsedStart = 0f;
            while (!startedTravel && elapsedStart < startTimeout)
            {
                startedTravel = toIndoor.TryHandleInteractor(interactor);
                if (startedTravel)
                {
                    break;
                }

                elapsedStart += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(startedTravel, Is.True, "Expected indoor travel to start once suppression window passes.");
            yield return WaitForActiveScene(IndoorRangeSceneName, SceneSwitchTimeoutSeconds);
            yield return WaitForResolvedEntryPoint("entry.indoor.arrival", SceneSwitchTimeoutSeconds);
            AssertPlayerRootIsAtEntryPoint("entry.indoor.arrival");

            var returnInteractor = CreatePlayerInteractor();
            var toTownObject = GameObject.Find("IndoorRange_SmokeToMainTown_Trigger");
            Assert.That(toTownObject, Is.Not.Null, "Expected authored smoke trigger in IndoorRangeInstance.");
            var toTown = toTownObject.GetComponent<TravelSceneTrigger>();
            Assert.That(toTown, Is.Not.Null);

            var startedReturnTravel = false;
            var returnTimeout = 2f;
            var elapsedReturn = 0f;
            while (!startedReturnTravel && elapsedReturn < returnTimeout)
            {
                startedReturnTravel = toTown.TryHandleInteractor(returnInteractor);
                if (startedReturnTravel)
                {
                    break;
                }

                elapsedReturn += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(startedReturnTravel, Is.True, "Expected return travel to start once suppression window passes.");
            yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);
            yield return WaitForResolvedEntryPoint("entry.maintown.return", SceneSwitchTimeoutSeconds);
            AssertPlayerRootIsAtEntryPoint("entry.maintown.return");
        }

        [UnityTest]
        public IEnumerator Travel_ToIndoor_DoesNotImmediatelyBounceBackToMainTown()
        {
            var startedTravel = WorldTravelCoordinator.TryLoadSceneAtEntry(IndoorRangeSceneName, "entry.indoor.arrival");
            Assert.That(startedTravel, Is.True, "Expected direct indoor travel to start.");
            yield return WaitForActiveScene(IndoorRangeSceneName, SceneSwitchTimeoutSeconds);

            var elapsed = 0f;
            while (elapsed < 1.2f)
            {
                Assert.That(
                    SceneManager.GetActiveScene().name,
                    Is.EqualTo(IndoorRangeSceneName),
                    "IndoorRange should remain active briefly after arrival and must not bounce travel immediately.");
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Travel_MainTownToIndoor_PreservesPlayerRootIdentityAndInventoryState()
        {
            SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
            yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);

            var initialPlayerRoot = GameObject.Find("PlayerRoot");
            Assert.That(initialPlayerRoot, Is.Not.Null, "Expected PlayerRoot in MainTown scene.");

            var inventoryController = initialPlayerRoot.GetComponent("PlayerInventoryController");
            Assert.That(inventoryController, Is.Not.Null, "Expected PlayerInventoryController on PlayerRoot.");

            var runtimeProperty = inventoryController.GetType().GetProperty("Runtime", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(runtimeProperty, Is.Not.Null, "Expected Runtime property on PlayerInventoryController.");
            var runtime = runtimeProperty.GetValue(inventoryController);
            Assert.That(runtime, Is.Not.Null, "Expected non-null PlayerInventoryRuntime.");

            var testItemId = "qa.travel.persist.item";
            var tryStoreItem = runtime.GetType().GetMethod("TryStoreItem", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(tryStoreItem, Is.Not.Null, "Expected TryStoreItem on PlayerInventoryRuntime.");
            var storeArgs = new object[] { testItemId, null, null, null };
            var stored = (bool)tryStoreItem.Invoke(runtime, storeArgs);
            Assert.That(stored, Is.True, "Expected to seed one inventory item before travel.");

            var interactor = CreatePlayerInteractor();
            var toIndoorObject = GameObject.Find("MainTown_SmokeToIndoor_Trigger");
            Assert.That(toIndoorObject, Is.Not.Null, "Expected authored smoke trigger in MainTown.");
            var toIndoor = toIndoorObject.GetComponent<TravelSceneTrigger>();
            Assert.That(toIndoor, Is.Not.Null);
            var startedTravel = false;
            var startTimeout = 2f;
            var elapsedStart = 0f;
            while (!startedTravel && elapsedStart < startTimeout)
            {
                startedTravel = toIndoor.TryHandleInteractor(interactor);
                if (startedTravel)
                {
                    break;
                }

                elapsedStart += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(startedTravel, Is.True, "Expected travel trigger to start scene travel.");

            yield return WaitForActiveScene(IndoorRangeSceneName, SceneSwitchTimeoutSeconds);
            yield return WaitForResolvedEntryPoint("entry.indoor.arrival", SceneSwitchTimeoutSeconds);

            var indoorPlayerRoot = GameObject.Find("PlayerRoot");
            Assert.That(indoorPlayerRoot, Is.Not.Null, "Expected PlayerRoot after arriving to IndoorRange.");

            var indoorInventoryController = indoorPlayerRoot.GetComponent("PlayerInventoryController");
            Assert.That(indoorInventoryController, Is.Not.Null, "Expected PlayerInventoryController after travel.");
            var indoorRuntime = runtimeProperty.GetValue(indoorInventoryController);
            Assert.That(indoorRuntime, Is.Not.Null, "Expected non-null PlayerInventoryRuntime after travel.");

            var getItemQuantity = indoorRuntime.GetType().GetMethod("GetItemQuantity", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(getItemQuantity, Is.Not.Null, "Expected GetItemQuantity on PlayerInventoryRuntime.");
            var quantity = (int)getItemQuantity.Invoke(indoorRuntime, new object[] { testItemId });
            Assert.That(quantity, Is.EqualTo(1), "Expected inventory quantity to persist after scene travel.");

            Assert.That(indoorPlayerRoot.GetComponent("PlayerLookController"), Is.Not.Null, "Expected look controller after travel.");
            Assert.That(indoorPlayerRoot.GetComponent("PlayerMover"), Is.Not.Null, "Expected movement controller after travel.");
            Assert.That(indoorPlayerRoot.GetComponent("PlayerCursorLockController"), Is.Not.Null, "Expected cursor lock controller after travel.");
            Assert.That(indoorPlayerRoot.GetComponent("ViewmodelAnimationAdapter"), Is.Not.Null, "Expected viewmodel adapter after travel.");
            Assert.That(indoorPlayerRoot.GetComponent("FpsViewmodelAnimatorDriver"), Is.Not.Null, "Expected viewmodel animator driver after travel.");
        }

        [UnityTest]
        public IEnumerator IndoorRange_HasShootingRangeBlockoutGeometry()
        {
            SceneManager.LoadScene(IndoorRangeSceneName, LoadSceneMode.Single);
            yield return WaitForActiveScene(IndoorRangeSceneName, SceneSwitchTimeoutSeconds);

            var requiredObjects = new[]
            {
                "IndoorRange_Geometry",
                "Range_Floor",
                "Range_Ceiling",
                "Range_Wall_Left",
                "Range_Wall_Right",
                "Range_Wall_Backstop",
                "FiringLine",
                "LaneDivider_1",
                "LaneDivider_2",
                "TargetPlate_1",
                "TargetPlate_2",
                "TargetPlate_3"
            };

            for (var i = 0; i < requiredObjects.Length; i++)
            {
                var gameObject = GameObject.Find(requiredObjects[i]);
                Assert.That(gameObject, Is.Not.Null, $"Expected IndoorRange geometry object '{requiredObjects[i]}'.");
            }
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

            var globalPlayerRoot = GameObject.Find("PlayerRoot");
            return globalPlayerRoot != null ? globalPlayerRoot.transform : null;
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
