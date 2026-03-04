using System.Collections;
using NUnit.Framework;
using Reloader.Core.Runtime;
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

            var mainCamera = GameObject.Find("Main Camera");
            Assert.That(mainCamera, Is.Not.Null, "Expected Main Camera in IndoorRange scene.");
            Assert.That(
                mainCamera.transform.parent,
                Is.Not.Null,
                "Expected Main Camera to be parented under PlayerRoot camera rig.");
            Assert.That(
                mainCamera.transform.parent.name,
                Is.EqualTo("CameraPivot"),
                "IndoorRange Main Camera should be parented to CameraPivot for player look/camera control.");

            var cameraPivot = mainCamera.transform.parent;
            var playerArms = cameraPivot.Find("PlayerArms");
            Assert.That(playerArms, Is.Not.Null, "IndoorRange rig should include PlayerArms under CameraPivot.");
            var armsAnimator = playerArms.GetComponentInChildren<Animator>(true);
            Assert.That(armsAnimator, Is.Not.Null, "PlayerArms should include an Animator.");
            Assert.That(armsAnimator.runtimeAnimatorController, Is.Not.Null, "PlayerArms Animator should have a RuntimeAnimatorController assigned.");

        }

        [UnityTest]
        public IEnumerator RoundTripTravel_UsesSceneEntryPoints_InBothDirections()
        {
            SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
            yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);
            AssertSinglePlayerRootGlobal();

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
            AssertSinglePlayerRootGlobal();
            AssertPlayerArmsRigPresentAndBound();

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
            AssertSinglePlayerRootGlobal();
            AssertMainTownControlRigWired();
        }

        [UnityTest]
        public IEnumerator RoundTripTravel_RepeatedIndoorArrival_KeepsPlayerArmsVisibleAndCanonical()
        {
            SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
            yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);

            yield return TravelViaTrigger("MainTown_SmokeToIndoor_Trigger", IndoorRangeSceneName, "entry.indoor.arrival");
            yield return TravelViaTrigger("IndoorRange_SmokeToMainTown_Trigger", MainTownSceneName, "entry.maintown.return");
            yield return TravelViaTrigger("MainTown_SmokeToIndoor_Trigger", IndoorRangeSceneName, "entry.indoor.arrival");

            var playerRoot = GameObject.Find("PlayerRoot");
            Assert.That(playerRoot, Is.Not.Null, "Expected PlayerRoot after second indoor arrival.");
            var cameraPivot = playerRoot.transform.Find("CameraPivot");
            Assert.That(cameraPivot, Is.Not.Null, "Expected CameraPivot after second indoor arrival.");
            var playerArms = cameraPivot.Find("PlayerArms");
            Assert.That(playerArms, Is.Not.Null, "Expected PlayerArms after second indoor arrival.");

            Assert.That(playerArms.gameObject.activeInHierarchy, Is.True, "PlayerArms should be active after second indoor arrival.");

            var renderers = playerArms.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers.Length, Is.GreaterThan(0), "Expected PlayerArms to include renderers.");
            for (var i = 0; i < renderers.Length; i++)
            {
                Assert.That(renderers[i].enabled, Is.True, "PlayerArms renderer should be enabled after second indoor arrival.");
            }

            Assert.That(playerArms.localPosition.x, Is.EqualTo(0f).Within(0.02f), "PlayerArms local X should be stabilized.");
            Assert.That(playerArms.localPosition.y, Is.EqualTo(-0.027f).Within(0.02f), "PlayerArms local Y should be stabilized.");
            Assert.That(playerArms.localPosition.z, Is.EqualTo(0.1f).Within(0.02f), "PlayerArms local Z should be stabilized.");
            Assert.That(Quaternion.Angle(playerArms.localRotation, Quaternion.identity), Is.LessThanOrEqualTo(1f), "PlayerArms local rotation should be stabilized.");
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
        public IEnumerator RoundTripTravel_ReturnToMainTown_ResetsMenuOpenState()
        {
            SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
            yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);

            var toIndoorObject = GameObject.Find("MainTown_SmokeToIndoor_Trigger");
            Assert.That(toIndoorObject, Is.Not.Null, "Expected authored smoke trigger in MainTown.");
            var toIndoor = toIndoorObject.GetComponent<TravelSceneTrigger>();
            Assert.That(toIndoor, Is.Not.Null);

            var startedTravel = false;
            var elapsedStart = 0f;
            while (!startedTravel && elapsedStart < 2f)
            {
                startedTravel = toIndoor.TryHandleInteractor(CreatePlayerInteractor());
                elapsedStart += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(startedTravel, Is.True, "Expected travel from MainTown to IndoorRange to start.");
            yield return WaitForActiveScene(IndoorRangeSceneName, SceneSwitchTimeoutSeconds);
            yield return WaitForResolvedEntryPoint("entry.indoor.arrival", SceneSwitchTimeoutSeconds);

            RuntimeKernelBootstrapper.ShopEvents?.RaiseShopTradeOpened("qa.vendor");
            RuntimeKernelBootstrapper.UiStateEvents?.RaiseWorkbenchMenuVisibilityChanged(true);
            RuntimeKernelBootstrapper.UiStateEvents?.RaiseTabInventoryVisibilityChanged(true);
            Assert.That(RuntimeKernelBootstrapper.UiStateEvents?.IsAnyMenuOpen ?? false, Is.True, "Expected menu state to be open before return travel.");

            var toTownObject = GameObject.Find("IndoorRange_SmokeToMainTown_Trigger");
            Assert.That(toTownObject, Is.Not.Null, "Expected authored smoke trigger in IndoorRangeInstance.");
            var toTown = toTownObject.GetComponent<TravelSceneTrigger>();
            Assert.That(toTown, Is.Not.Null);

            var startedReturnTravel = false;
            var elapsedReturn = 0f;
            while (!startedReturnTravel && elapsedReturn < 2f)
            {
                startedReturnTravel = toTown.TryHandleInteractor(CreatePlayerInteractor());
                elapsedReturn += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(startedReturnTravel, Is.True, "Expected travel from IndoorRange back to MainTown to start.");
            yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);
            yield return WaitForResolvedEntryPoint("entry.maintown.return", SceneSwitchTimeoutSeconds);

            Assert.That(RuntimeKernelBootstrapper.UiStateEvents?.IsAnyMenuOpen ?? false, Is.False, "Expected return travel to reset runtime menu-open state.");
            Assert.That(IsCursorLockMenuOpen(), Is.False, "Expected cursor lock menu-open flag to reset after return travel.");
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
        public IEnumerator Travel_MainTownToIndoor_PreservesEquippedWeaponMagazineAndChamberState()
        {
            SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
            yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);

            var playerRoot = GameObject.Find("PlayerRoot");
            Assert.That(playerRoot, Is.Not.Null, "Expected PlayerRoot in MainTown scene.");

            var inventoryController = playerRoot.GetComponent("PlayerInventoryController");
            Assert.That(inventoryController, Is.Not.Null, "Expected PlayerInventoryController on PlayerRoot.");
            var runtimeProperty = inventoryController.GetType().GetProperty("Runtime", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(runtimeProperty, Is.Not.Null, "Expected Runtime property on PlayerInventoryController.");
            var runtime = runtimeProperty.GetValue(inventoryController);
            Assert.That(runtime, Is.Not.Null, "Expected non-null PlayerInventoryRuntime.");

            var weaponController = playerRoot.GetComponent("PlayerWeaponController");
            Assert.That(weaponController, Is.Not.Null, "Expected PlayerWeaponController on PlayerRoot.");

            var applyRuntimeState = weaponController.GetType().GetMethod("ApplyRuntimeState", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(applyRuntimeState, Is.Not.Null, "Expected ApplyRuntimeState on PlayerWeaponController.");

            var candidateItemIds = new System.Collections.Generic.List<string>();
            var selectedBeltItemIdProperty = runtime.GetType().GetProperty("SelectedBeltItemId", BindingFlags.Instance | BindingFlags.Public);
            var selectedBeltItemId = selectedBeltItemIdProperty?.GetValue(runtime) as string;
            if (!string.IsNullOrWhiteSpace(selectedBeltItemId))
            {
                candidateItemIds.Add(selectedBeltItemId);
            }

            var beltSlotItemIdsProperty = runtime.GetType().GetProperty("BeltSlotItemIds", BindingFlags.Instance | BindingFlags.Public);
            if (beltSlotItemIdsProperty?.GetValue(runtime) is System.Collections.IEnumerable beltItems)
            {
                foreach (var entry in beltItems)
                {
                    if (entry is string id && !string.IsNullOrWhiteSpace(id) && !candidateItemIds.Contains(id))
                    {
                        candidateItemIds.Add(id);
                    }
                }
            }

            var weaponRegistryField = weaponController.GetType().GetField("_weaponRegistry", BindingFlags.Instance | BindingFlags.NonPublic);
            var weaponRegistry = weaponRegistryField?.GetValue(weaponController);
            var definitionsField = weaponRegistry?.GetType().GetField("_definitions", BindingFlags.Instance | BindingFlags.NonPublic);
            if (definitionsField?.GetValue(weaponRegistry) is System.Collections.IEnumerable definitions)
            {
                foreach (var definition in definitions)
                {
                    if (definition == null)
                    {
                        continue;
                    }

                    var itemId = definition.GetType().GetProperty("ItemId", BindingFlags.Instance | BindingFlags.Public)?.GetValue(definition) as string;
                    if (!string.IsNullOrWhiteSpace(itemId) && !candidateItemIds.Contains(itemId))
                    {
                        candidateItemIds.Add(itemId);
                    }
                }
            }

            string weaponItemId = null;
            for (var i = 0; i < candidateItemIds.Count; i++)
            {
                var candidateId = candidateItemIds[i];
                var applied = (bool)applyRuntimeState.Invoke(weaponController, new object[] { candidateId, 2, 11, true });
                if (!applied)
                {
                    continue;
                }

                weaponItemId = candidateId;
                break;
            }

            Assert.That(weaponItemId, Is.Not.Null.And.Not.Empty, "Expected a weapon item id that accepts runtime state apply.");

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
            var indoorWeaponController = indoorPlayerRoot.GetComponent("PlayerWeaponController");
            Assert.That(indoorWeaponController, Is.Not.Null, "Expected PlayerWeaponController after travel.");

            var tryGetRuntimeState = indoorWeaponController.GetType().GetMethod("TryGetRuntimeState", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(tryGetRuntimeState, Is.Not.Null, "Expected TryGetRuntimeState on PlayerWeaponController.");

            var tryGetArgs = new object[] { weaponItemId, null };
            var hasState = (bool)tryGetRuntimeState.Invoke(indoorWeaponController, tryGetArgs);
            Assert.That(hasState, Is.True, $"Expected runtime state for '{weaponItemId}' after travel.");
            Assert.That(tryGetArgs[1], Is.Not.Null, "Expected non-null runtime state payload.");

            var state = tryGetArgs[1];
            var magazineCountProperty = state.GetType().GetProperty("MagazineCount", BindingFlags.Instance | BindingFlags.Public);
            var chamberLoadedProperty = state.GetType().GetProperty("ChamberLoaded", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(magazineCountProperty, Is.Not.Null, "Expected MagazineCount on WeaponRuntimeState.");
            Assert.That(chamberLoadedProperty, Is.Not.Null, "Expected ChamberLoaded on WeaponRuntimeState.");

            var magazineCount = (int)magazineCountProperty.GetValue(state);
            var chamberLoaded = (bool)chamberLoadedProperty.GetValue(state);
            Assert.That(magazineCount, Is.EqualTo(2), "Expected magazine count to persist across travel.");
            Assert.That(chamberLoaded, Is.True, "Expected chamber loaded state to persist across travel.");
        }

        [UnityTest]
        public IEnumerator TravelCoordinator_DoesNotContainOwnedPickupHideWorkaround()
        {
            var workaroundMethod = typeof(WorldTravelCoordinator).GetMethod(
                "HideOwnedWeaponPickupsInScene",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(
                workaroundMethod,
                Is.Null,
                "Travel should rely on unified world-object persistence apply and must not keep ownership-based pickup hiding.");

            yield return null;
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

        [UnityTest]
        public IEnumerator MainTown_ReturnEntryPoint_IsGroundedAndNotVoid()
        {
            SceneManager.LoadScene(MainTownSceneName, LoadSceneMode.Single);
            yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);

            var activeScene = SceneManager.GetActiveScene();
            var returnEntry = FindEntryPointInScene(activeScene, "entry.maintown.return");
            Assert.That(returnEntry, Is.Not.Null, "Expected MainTown return entry point.");

            var origin = returnEntry.transform.position + Vector3.up * 2f;
            var hasGround = Physics.Raycast(origin, Vector3.down, out var hit, 8f);
            Assert.That(hasGround, Is.True, "MainTown return entry should have walkable ground underneath.");
            Assert.That(hit.point.y, Is.GreaterThan(-2f), "MainTown return entry should not resolve into void space.");
        }

        [Test]
        public void EnsureViewmodelRigAfterTravel_RecreatesPlayerArms_WhenMissing()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerRoot.transform, false);

            InvokeEnsureViewmodelRigAfterTravel(playerRoot.transform);

            var playerArms = cameraPivot.Find("PlayerArms");
            Assert.That(playerArms, Is.Not.Null, "Expected fallback travel rig healing to recreate PlayerArms.");

            var animator = playerArms.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null, "Expected recreated PlayerArms to include an Animator.");
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null, "Expected recreated PlayerArms Animator to have a controller.");

            Assert.That(playerArms.localPosition.x, Is.EqualTo(0f).Within(0.02f));
            Assert.That(playerArms.localPosition.y, Is.EqualTo(-0.027f).Within(0.02f));
            Assert.That(playerArms.localPosition.z, Is.EqualTo(0.1f).Within(0.02f));
            Assert.That(Quaternion.Angle(playerArms.localRotation, Quaternion.identity), Is.LessThanOrEqualTo(1f));

            Object.DestroyImmediate(playerRoot);
        }

        [Test]
        public void EnsureViewmodelRigAfterTravel_ReappliesControllerBeforeRebind_WhenAnimatorControllerMissing()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerRoot.transform, false);
            var playerArms = new GameObject("PlayerArms").transform;
            playerArms.SetParent(cameraPivot, false);

            var animator = playerArms.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = null;

            var probe = playerRoot.AddComponent<TravelRigRebindProbe>();
            probe.TargetAnimator = animator;

            InvokeEnsureViewmodelRigAfterTravel(playerRoot.transform);

            Assert.That(animator.runtimeAnimatorController, Is.Not.Null, "Expected travel rig healing to reapply animator controller when missing.");
            Assert.That(probe.ResolveReferencesCalled, Is.True, "Expected travel rig rebinding to run ResolveReferences.");
            Assert.That(probe.SawControllerDuringResolve, Is.True, "Expected controller to be restored before ResolveReferences rebinding.");

            Object.DestroyImmediate(playerRoot);
        }

        private static GameObject CreatePlayerInteractor()
        {
            var interactor = new GameObject("TestPlayerInteractor");
            interactor.tag = "Player";
            return interactor;
        }

        private static void InvokeEnsureViewmodelRigAfterTravel(Transform playerRootTransform)
        {
            var method = typeof(WorldTravelCoordinator).GetMethod("EnsureViewmodelRigAfterTravel", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Expected travel rig healing method on WorldTravelCoordinator.");
            method.Invoke(null, new object[] { playerRootTransform });
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

        private static void AssertSinglePlayerRootGlobal()
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var count = 0;
            for (var i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t != null && t.name == "PlayerRoot")
                {
                    count++;
                }
            }

            Assert.That(count, Is.EqualTo(1), "Expected exactly one PlayerRoot globally after travel.");
        }

        private static void AssertPlayerArmsRigPresentAndBound()
        {
            var playerRoot = GameObject.Find("PlayerRoot");
            Assert.That(playerRoot, Is.Not.Null, "Expected PlayerRoot.");

            var cameraPivot = playerRoot.transform.Find("CameraPivot");
            Assert.That(cameraPivot, Is.Not.Null, "Expected CameraPivot under PlayerRoot.");

            var playerArms = cameraPivot.Find("PlayerArms");
            Assert.That(playerArms, Is.Not.Null, "Expected PlayerArms under CameraPivot.");
            var animator = playerArms.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null, "Expected Animator on PlayerArms.");
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null, "Expected runtime animator controller on PlayerArms animator.");
        }

        private static void AssertMainTownControlRigWired()
        {
            var playerRoot = GameObject.Find("PlayerRoot");
            Assert.That(playerRoot, Is.Not.Null, "Expected PlayerRoot in MainTown.");

            var inputReader = playerRoot.GetComponent("PlayerInputReader");
            Assert.That(inputReader, Is.Not.Null, "Expected PlayerInputReader.");
            var actionsField = inputReader.GetType().GetField("_actionsAsset", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(actionsField, Is.Not.Null, "Expected _actionsAsset field on PlayerInputReader.");
            Assert.That(actionsField.GetValue(inputReader), Is.Not.Null, "Expected input actions asset assigned after travel.");
            var playerMapField = inputReader.GetType().GetField("_playerMap", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(playerMapField, Is.Not.Null, "Expected _playerMap field on PlayerInputReader.");
            var playerMap = playerMapField.GetValue(inputReader);
            Assert.That(playerMap, Is.Not.Null, "Expected resolved Player action map after travel.");
            var enabledProperty = playerMap.GetType().GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(enabledProperty, Is.Not.Null, "Expected enabled property on action map.");
            Assert.That((bool)enabledProperty.GetValue(playerMap), Is.True, "Player action map must be enabled after return travel.");

            var lookController = playerRoot.GetComponent("PlayerLookController");
            Assert.That(lookController, Is.Not.Null, "Expected PlayerLookController.");
            var lookInputField = lookController.GetType().GetField("_inputSourceBehaviour", BindingFlags.Instance | BindingFlags.NonPublic);
            var lookPitchField = lookController.GetType().GetField("_pitchTransform", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lookInputField?.GetValue(lookController), Is.Not.Null, "Look controller input reference should be assigned.");
            Assert.That(lookPitchField?.GetValue(lookController), Is.Not.Null, "Look controller pitch transform should be assigned.");

            var mover = playerRoot.GetComponent("PlayerMover");
            Assert.That(mover, Is.Not.Null, "Expected PlayerMover.");
            var moverInputField = mover.GetType().GetField("_inputSourceBehaviour", BindingFlags.Instance | BindingFlags.NonPublic);
            var moverControllerField = mover.GetType().GetField("_characterController", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(moverInputField?.GetValue(mover), Is.Not.Null, "Mover input reference should be assigned.");
            Assert.That(moverControllerField?.GetValue(mover), Is.Not.Null, "Mover character controller should be assigned.");

            var weaponController = playerRoot.GetComponent("PlayerWeaponController");
            Assert.That(weaponController, Is.Not.Null, "Expected PlayerWeaponController.");
            var weaponInputField = weaponController.GetType().GetField("_inputSourceBehaviour", BindingFlags.Instance | BindingFlags.NonPublic);
            var weaponInventoryField = weaponController.GetType().GetField("_inventoryController", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(weaponInputField?.GetValue(weaponController), Is.Not.Null, "Weapon controller input reference should be assigned.");
            Assert.That(weaponInventoryField?.GetValue(weaponController), Is.Not.Null, "Weapon controller inventory reference should be assigned.");
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

        private static bool IsCursorLockMenuOpen()
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType("Reloader.Player.PlayerCursorLockController", throwOnError: false);
                if (type == null)
                {
                    continue;
                }

                var property = type.GetProperty("IsAnyMenuOpen", BindingFlags.Public | BindingFlags.Static);
                if (property == null)
                {
                    return false;
                }

                return property.GetValue(null) as bool? ?? false;
            }

            return false;
        }

        private static IEnumerator TravelViaTrigger(string triggerObjectName, string expectedSceneName, string expectedEntryPointId)
        {
            var triggerObject = GameObject.Find(triggerObjectName);
            Assert.That(triggerObject, Is.Not.Null, $"Expected trigger object '{triggerObjectName}'.");
            var trigger = triggerObject.GetComponent<TravelSceneTrigger>();
            Assert.That(trigger, Is.Not.Null, $"Expected TravelSceneTrigger on '{triggerObjectName}'.");

            var startedTravel = false;
            var elapsed = 0f;
            while (!startedTravel && elapsed < 2f)
            {
                startedTravel = trigger.TryHandleInteractor(CreatePlayerInteractor());
                if (startedTravel)
                {
                    break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(startedTravel, Is.True, $"Expected travel trigger '{triggerObjectName}' to start travel.");
            yield return WaitForActiveScene(expectedSceneName, SceneSwitchTimeoutSeconds);
            yield return WaitForResolvedEntryPoint(expectedEntryPointId, SceneSwitchTimeoutSeconds);
        }

    }

    public sealed class TravelRigRebindProbe : MonoBehaviour
    {
        public Animator TargetAnimator;
        public bool ResolveReferencesCalled;
        public bool SawControllerDuringResolve;

        private void ResolveReferences()
        {
            ResolveReferencesCalled = true;
            SawControllerDuringResolve = TargetAnimator != null && TargetAnimator.runtimeAnimatorController != null;
        }
    }
}
