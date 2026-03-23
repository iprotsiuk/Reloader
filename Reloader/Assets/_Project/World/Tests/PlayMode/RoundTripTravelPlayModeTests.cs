using System.Collections;
using System.Collections.Generic;
using System;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.NPCs.Runtime;
using Reloader.Core.Runtime;
using Reloader.Player;
using Reloader.World.Runtime;
using Reloader.World.Travel;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using System.Reflection;
using Object = UnityEngine.Object;

namespace Reloader.World.Tests.PlayMode
{
    public class RoundTripTravelPlayModeTests
    {
        private const string BootstrapSceneName = "Bootstrap";
        private const string MainTownSceneName = "MainTown";
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";

        private const string IndoorRangeSceneName = "IndoorRangeInstance";
        private const float SceneSwitchTimeoutSeconds = 5f;

        [TearDown]
        public void TearDown()
        {
            ResetTravelCoordinatorState();
            ResetRuntimeKernelState();
            DestroyPersistentPlayerRoot();
        }

        [UnityTest]
        public IEnumerator BootstrapLoad_StaysOnExplicitFrontDoorWithoutAutoTravel()
        {
            SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
            yield return WaitForActiveScene(BootstrapSceneName, SceneSwitchTimeoutSeconds);

            var elapsed = 0f;
            while (elapsed < 1.5f)
            {
                Assert.That(
                    SceneManager.GetActiveScene().name,
                    Is.EqualTo(BootstrapSceneName),
                    "Bootstrap should remain the explicit front door until an authored startup action routes into gameplay.");
                Assert.That(
                    SceneManager.GetSceneByName(MainTownSceneName).isLoaded,
                    Is.False,
                    "Bootstrap front door should not auto-load MainTown.");
                Assert.That(
                    SceneManager.GetSceneByName(IndoorRangeSceneName).isLoaded,
                    Is.False,
                    "Bootstrap front door should not auto-load IndoorRange.");
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator IndoorRange_PlayerRig_HasInputAssetAndBeltHud()
        {
            SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
            yield return null;

            var bootstrapRoot = Object.FindFirstObjectByType<BootstrapWorldRoot>(FindObjectsInactive.Include);
            Assert.That(bootstrapRoot, Is.Not.Null, "Expected Bootstrap to keep the canonical BootstrapWorldRoot loaded.");

            var persistentRoot = BootstrapWorldRoot.Initialize();
            Assert.That(persistentRoot, Is.Not.Null, "Expected Bootstrap runtime initialization to produce a canonical PersistentPlayerRoot.");
            Assert.That(PersistentPlayerRoot.Instance, Is.SameAs(persistentRoot), "Expected one canonical PersistentPlayerRoot instance.");
            Assert.That(persistentRoot.PlayerRootTransform, Is.Not.Null, "Expected a canonical runtime player root before routing into IndoorRange.");

            var startedTravel = WorldTravelCoordinator.TryLoadSceneAtEntry(IndoorRangeSceneName, "entry.indoor.arrival");
            Assert.That(startedTravel, Is.True, "Expected Bootstrap to route into IndoorRange via the authored entry point.");
            yield return WaitForActiveScene(IndoorRangeSceneName, SceneSwitchTimeoutSeconds);
            yield return WaitForResolvedEntryPoint("entry.indoor.arrival", SceneSwitchTimeoutSeconds);
            yield return WaitForCanonicalPlayerStartupStabilization();

            var playerRoot = GetCanonicalPlayerRoot();
            Assert.That(playerRoot, Is.Not.Null, "Expected canonical runtime player root after arriving in IndoorRange.");

            var inputReader = playerRoot.GetComponent("PlayerInputReader");
            Assert.That(inputReader, Is.Not.Null, "Expected PlayerInputReader on IndoorRange PlayerRoot.");

            var actionsField = inputReader.GetType().GetField("_actionsAsset", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(actionsField, Is.Not.Null, "Expected _actionsAsset field on PlayerInputReader.");
            var actionsAsset = actionsField.GetValue(inputReader);
            Assert.That(actionsAsset, Is.Not.Null, "IndoorRange PlayerInputReader must have an InputActionAsset assigned.");

            var beltHud = GameObject.Find("BeltHud");
            Assert.That(beltHud, Is.Not.Null, "IndoorRange scene should include BeltHud runtime prefab.");

            var cameraDefaults = playerRoot.GetComponent<PlayerCameraDefaults>();
            Assert.That(cameraDefaults, Is.Not.Null, "Expected PlayerCameraDefaults on the canonical IndoorRange player root.");

            Assert.That(cameraDefaults!.TryGetMainCamera(out var mainCamera), Is.True, "Expected PlayerCameraDefaults to resolve the canonical runtime main camera.");
            Assert.That(
                mainCamera!.transform.IsChildOf(playerRoot),
                Is.True,
                "Expected the canonical runtime main camera to stay under the canonical player rig.");

            Assert.That(cameraDefaults.TryGetCameraPivot(out var cameraPivot), Is.True, "Expected PlayerCameraDefaults to resolve CameraPivot.");
            Assert.That(
                mainCamera.transform.parent,
                Is.EqualTo(cameraPivot),
                "IndoorRange main camera should stay parented to CameraPivot for player look/camera control.");

            Assert.That(cameraDefaults.TryGetPlayerArmsRoot(out var playerArms), Is.True, "Expected PlayerCameraDefaults to resolve PlayerArms.");
            Assert.That(playerArms!.parent, Is.EqualTo(cameraPivot), "Expected PlayerArms to stay parented under CameraPivot.");

            Assert.That(cameraDefaults.TryGetPlayerArmsAnimator(out var armsAnimator), Is.True, "Expected PlayerCameraDefaults to resolve the PlayerArms animator.");
            Assert.That(armsAnimator.runtimeAnimatorController, Is.Not.Null, "PlayerArms Animator should have a RuntimeAnimatorController assigned.");
        }

        [UnityTest]
        public IEnumerator RoundTripTravel_UsesSceneEntryPoints_InBothDirections()
        {
            yield return LoadMainTownAndBindRuntimePlayerRoot();
            AssertSinglePlayerRootGlobal();

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
            yield return LoadMainTownAndBindRuntimePlayerRoot();

            yield return TravelViaTrigger("MainTown_SmokeToIndoor_Trigger", IndoorRangeSceneName, "entry.indoor.arrival");
            yield return TravelViaTrigger("IndoorRange_SmokeToMainTown_Trigger", MainTownSceneName, "entry.maintown.return");
            yield return TravelViaTrigger("MainTown_SmokeToIndoor_Trigger", IndoorRangeSceneName, "entry.indoor.arrival");

            var playerRoot = GetCanonicalPlayerRoot();
            Assert.That(playerRoot, Is.Not.Null, "Expected PlayerRoot after second indoor arrival.");
            var cameraPivot = playerRoot.Find("CameraPivot");
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
        public IEnumerator RoundTripTravel_ReturnToMainTown_PreservesRetiredCivilianAndDoesNotResetProceduralOffer()
        {
            yield return LoadMainTownAndBindRuntimePlayerRoot();
            yield return null;

            var bridge = FindPopulationBridge();
            var provider = FindContractProvider();
            Assert.That(bridge, Is.Not.Null, "Expected MainTown population bridge.");
            Assert.That(provider, Is.Not.Null, "Expected MainTown contract runtime provider.");
            Assert.That(provider!.TryGetContractSnapshot(out var initialSnapshot), Is.True, "Expected initial procedural contract offer in MainTown.");

            var retiredCivilianId = initialSnapshot.TargetId;
            Assert.That(bridge!.TryRetireCivilian(retiredCivilianId, retiredAtDay: 1), Is.True, "Expected the initially offered civilian to retire for this regression.");
            bridge.RebuildScenePopulation();
            yield return null;

            Assert.That(bridge.TryResolveSpawnedCivilian(retiredCivilianId, out _), Is.False, "Expected the retired civilian to leave the live scene before travel.");
            Assert.That(provider.TryGetContractSnapshot(out var beforeTravelSnapshot), Is.True, "Expected MainTown to republish a live offer after retiring the current target.");
            Assert.That(beforeTravelSnapshot.TargetId, Is.Not.EqualTo(retiredCivilianId), "Expected the republished offer to move away from the retired civilian before travel.");

            yield return TravelViaTrigger("MainTown_SmokeToIndoor_Trigger", IndoorRangeSceneName, "entry.indoor.arrival");
            yield return TravelViaTrigger("IndoorRange_SmokeToMainTown_Trigger", MainTownSceneName, "entry.maintown.return");
            yield return null;

            bridge = FindPopulationBridge();
            provider = FindContractProvider();
            Assert.That(bridge, Is.Not.Null, "Expected MainTown population bridge after returning from the range.");
            Assert.That(provider, Is.Not.Null, "Expected MainTown contract runtime provider after returning from the range.");

            Assert.That(bridge!.TryResolveSpawnedCivilian(retiredCivilianId, out _), Is.False,
                "Expected returning to MainTown to keep the previously retired civilian absent instead of respawning the original target.");
            Assert.That(provider!.TryGetContractSnapshot(out var afterReturnSnapshot), Is.True, "Expected a procedural contract offer after returning to MainTown.");
            Assert.That(afterReturnSnapshot.TargetId, Is.Not.EqualTo(retiredCivilianId),
                "Expected return travel to avoid resetting the available procedural offer back to the original first target.");
        }

        [UnityTest]
        public IEnumerator Travel_ToIndoor_DoesNotImmediatelyBounceBackToMainTown()
        {
            yield return LoadMainTownAndBindRuntimePlayerRoot();

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
            yield return LoadMainTownAndBindRuntimePlayerRoot();

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
            yield return LoadMainTownAndBindRuntimePlayerRoot();

            var initialPlayerRoot = GetCanonicalPlayerRoot();
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

            Assert.That(startedTravel, Is.True, "Expected direct travel to IndoorRange to start.");

            yield return WaitForActiveScene(IndoorRangeSceneName, SceneSwitchTimeoutSeconds);
            yield return WaitForResolvedEntryPoint("entry.indoor.arrival", SceneSwitchTimeoutSeconds);

            var indoorPlayerRoot = GetCanonicalPlayerRoot();
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
            Assert.That(indoorPlayerRoot.GetComponent<Reloader.Player.Viewmodel.ViewmodelAnimationAdapter>(), Is.Not.Null, "Expected typed runtime viewmodel adapter after travel.");
            Assert.That(indoorPlayerRoot.GetComponent<FpsViewmodelAnimatorDriver>(), Is.Not.Null, "Expected viewmodel animator driver after travel.");
        }

        [UnityTest]
        public IEnumerator Travel_MainTownToIndoor_PreservesEquippedWeaponMagazineAndChamberState()
        {
            yield return LoadMainTownAndBindRuntimePlayerRoot();

            var playerRoot = GetCanonicalPlayerRoot();
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
            var applyRuntimeAttachments = weaponController.GetType().GetMethod("ApplyRuntimeAttachments", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(applyRuntimeAttachments, Is.Not.Null, "Expected ApplyRuntimeAttachments on PlayerWeaponController.");

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

            var tryStoreItem = runtime.GetType().GetMethod("TryStoreItem", BindingFlags.Instance | BindingFlags.Public);
            var selectBeltSlot = runtime.GetType().GetMethod("SelectBeltSlot", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(tryStoreItem, Is.Not.Null, "Expected TryStoreItem on inventory runtime.");
            Assert.That(selectBeltSlot, Is.Not.Null, "Expected SelectBeltSlot on inventory runtime.");

            var storeArgs = new object[] { weaponItemId, null, -1, null };
            var storedWeapon = (bool)tryStoreItem.Invoke(runtime, storeArgs);
            Assert.That(storedWeapon, Is.True, $"Expected '{weaponItemId}' to be stored before travel.");

            var storedBeltIndex = (int)storeArgs[2];
            selectBeltSlot.Invoke(runtime, new object[] { storedBeltIndex });
            yield return null;

            var equippedItemIdProperty = weaponController.GetType().GetProperty("EquippedItemId", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(equippedItemIdProperty, Is.Not.Null, "Expected EquippedItemId on PlayerWeaponController.");
            Assert.That(
                equippedItemIdProperty.GetValue(weaponController) as string,
                Is.EqualTo(weaponItemId),
                "Expected the scoped travel weapon to be equipped before snapshot capture.");

            var slotEnumType = System.Type.GetType("Reloader.Weapons.Data.WeaponAttachmentSlotType, Reloader.Weapons");
            Assert.That(slotEnumType, Is.Not.Null, "Expected WeaponAttachmentSlotType enum type.");
            var scopeSlot = System.Enum.Parse(slotEnumType, "Scope");
            var attachmentsParameterType = applyRuntimeAttachments.GetParameters()[1].ParameterType;
            var attachmentsMapType = typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(slotEnumType, typeof(string));
            object attachmentsMap = System.Activator.CreateInstance(attachmentsMapType);
            attachmentsMapType.GetMethod("Add", new[] { slotEnumType, typeof(string) })
                ?.Invoke(attachmentsMap, new object[] { scopeSlot, "att-kar98k-scope-remote-a" });
            Assert.That(
                attachmentsParameterType.IsInstanceOfType(attachmentsMap),
                Is.True,
                "Expected runtime attachment map assignable to ApplyRuntimeAttachments parameter type.");
            var appliedAttachments = (bool)applyRuntimeAttachments.Invoke(weaponController, new object[] { weaponItemId, attachmentsMap });
            Assert.That(appliedAttachments, Is.True, "Expected attachment runtime state to apply before travel.");

            var startedTravel = WorldTravelCoordinator.TryLoadSceneAtEntry(IndoorRangeSceneName, "entry.indoor.arrival");
            Assert.That(startedTravel, Is.True, "Expected travel trigger to start scene travel.");
            yield return WaitForActiveScene(IndoorRangeSceneName, SceneSwitchTimeoutSeconds);
            yield return WaitForResolvedEntryPoint("entry.indoor.arrival", SceneSwitchTimeoutSeconds);

            var indoorPlayerRoot = GetCanonicalPlayerRoot();
            Assert.That(indoorPlayerRoot, Is.Not.Null, "Expected PlayerRoot after arriving to IndoorRange.");
            var indoorWeaponController = indoorPlayerRoot.GetComponent("PlayerWeaponController");
            Assert.That(indoorWeaponController, Is.Not.Null, "Expected PlayerWeaponController after travel.");
            Assert.That(
                equippedItemIdProperty.GetValue(indoorWeaponController) as string,
                Is.EqualTo(weaponItemId),
                "Expected travel to preserve the equipped weapon item across the scene transition.");

            var tryGetRuntimeState = indoorWeaponController.GetType().GetMethod("TryGetRuntimeState", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(tryGetRuntimeState, Is.Not.Null, "Expected TryGetRuntimeState on PlayerWeaponController.");

            var tryGetArgs = new object[] { weaponItemId, null };
            var hasState = (bool)tryGetRuntimeState.Invoke(indoorWeaponController, tryGetArgs);
            Assert.That(hasState, Is.True, $"Expected runtime state for '{weaponItemId}' after travel.");
            Assert.That(tryGetArgs[1], Is.Not.Null, "Expected non-null runtime state payload.");

            var state = tryGetArgs[1];
            var magazineCountProperty = state.GetType().GetProperty("MagazineCount", BindingFlags.Instance | BindingFlags.Public);
            var chamberLoadedProperty = state.GetType().GetProperty("ChamberLoaded", BindingFlags.Instance | BindingFlags.Public);
            var getAttachmentMethod = state.GetType().GetMethod("GetEquippedAttachmentItemId", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(magazineCountProperty, Is.Not.Null, "Expected MagazineCount on WeaponRuntimeState.");
            Assert.That(chamberLoadedProperty, Is.Not.Null, "Expected ChamberLoaded on WeaponRuntimeState.");
            Assert.That(getAttachmentMethod, Is.Not.Null, "Expected GetEquippedAttachmentItemId on WeaponRuntimeState.");

            var magazineCount = (int)magazineCountProperty.GetValue(state);
            var chamberLoaded = (bool)chamberLoadedProperty.GetValue(state);
            var scopeAttachmentId = getAttachmentMethod.Invoke(state, new[] { scopeSlot }) as string;
            Assert.That(magazineCount, Is.EqualTo(2), "Expected magazine count to persist across travel.");
            Assert.That(chamberLoaded, Is.True, "Expected chamber loaded state to persist across travel.");
            Assert.That(scopeAttachmentId, Is.EqualTo("att-kar98k-scope-remote-a"), "Expected equipped scope attachment to persist across travel.");

            var hasActiveScopedAdsAlignmentProperty = indoorWeaponController.GetType().GetProperty("HasActiveScopedAdsAlignment", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(hasActiveScopedAdsAlignmentProperty, Is.Not.Null, "Expected HasActiveScopedAdsAlignment on PlayerWeaponController.");

            var alignmentReady = false;
            var alignmentTimeout = 1f;
            var alignmentElapsed = 0f;
            while (!alignmentReady && alignmentElapsed < alignmentTimeout)
            {
                alignmentReady = hasActiveScopedAdsAlignmentProperty.GetValue(indoorWeaponController) is true;
                if (alignmentReady)
                {
                    break;
                }

                alignmentElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(alignmentReady, Is.True, "Expected travel restore to rebuild the live scoped ADS bridge before the first manual swap.");

            var inputReader = GetCanonicalPlayerRoot()?.GetComponent("PlayerInputReader");
            Assert.That(inputReader, Is.Not.Null, "Expected PlayerInputReader on IndoorRange PlayerRoot after travel.");

            var aimHeldProperty = inputReader!.GetType().GetProperty("AimHeld", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(aimHeldProperty, Is.Not.Null, "Expected AimHeld property on PlayerInputReader.");
            aimHeldProperty!.SetValue(inputReader, true);

            var currentAdsBlendProperty = indoorWeaponController.GetType().GetProperty("CurrentAdsBlendT", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(currentAdsBlendProperty, Is.Not.Null, "Expected CurrentAdsBlendT on PlayerWeaponController.");

            var adsElapsed = 0f;
            while ((float)currentAdsBlendProperty!.GetValue(indoorWeaponController) < 0.999f && adsElapsed < 1.5f)
            {
                adsElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return null;
            if (!Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame();
            }

            var hasStableScopedAdsAlignmentProperty = indoorWeaponController.GetType().GetProperty("HasStableScopedAdsAlignment", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(hasStableScopedAdsAlignmentProperty, Is.Not.Null, "Expected HasStableScopedAdsAlignment on PlayerWeaponController.");

            var stableAlignmentElapsed = 0f;
            while (!(hasStableScopedAdsAlignmentProperty!.GetValue(indoorWeaponController) as bool? ?? false) && stableAlignmentElapsed < 1f)
            {
                stableAlignmentElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(
                hasStableScopedAdsAlignmentProperty.GetValue(indoorWeaponController) as bool? ?? false,
                Is.True,
                "Expected travel-restored scoped ADS to reach stable magnified alignment once full ADS is reached.");

            var equippedViewTransformProperty = indoorWeaponController.GetType().GetProperty("EquippedWeaponViewTransform", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(equippedViewTransformProperty, Is.Not.Null, "Expected EquippedWeaponViewTransform on PlayerWeaponController.");
            var equippedView = equippedViewTransformProperty!.GetValue(indoorWeaponController) as Transform;
            Assert.That(equippedView, Is.Not.Null, "Expected the scoped weapon view to stay mounted after travel.");

            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            Assert.That(attachmentManagerType, Is.Not.Null, "Expected AttachmentManager runtime type for scoped travel verification.");
            var manager = equippedView!.GetComponent(attachmentManagerType);
            Assert.That(manager, Is.Not.Null, "Expected AttachmentManager on the equipped weapon view after travel.");

            var getActiveSightAnchor = attachmentManagerType!.GetMethod("GetActiveSightAnchor", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(getActiveSightAnchor, Is.Not.Null, "Expected GetActiveSightAnchor on AttachmentManager.");
            var activeSightAnchor = getActiveSightAnchor!.Invoke(manager, null) as Transform;
            Assert.That(activeSightAnchor, Is.Not.Null, "Expected a live scoped sight anchor after travel.");
        }

        [UnityTest]
        public IEnumerator MainTownAndIndoorRange_ShareSupportedWeaponIdsAndViewMappings()
        {
            yield return LoadMainTownAndBindRuntimePlayerRoot();

            var expectedIds = new[] { "weapon-canik-tp9", "weapon-kar98k" };
            CollectionAssert.AreEquivalent(expectedIds, GetWeaponRegistryItemIdsForActivePlayer());
            CollectionAssert.AreEquivalent(expectedIds, GetWeaponViewItemIdsForActivePlayer());

            var startedTravel = WorldTravelCoordinator.TryLoadSceneAtEntry(IndoorRangeSceneName, "entry.indoor.arrival");
            Assert.That(startedTravel, Is.True, "Expected direct indoor travel to start for parity verification.");
            yield return WaitForActiveScene(IndoorRangeSceneName, SceneSwitchTimeoutSeconds);
            yield return WaitForResolvedEntryPoint("entry.indoor.arrival", SceneSwitchTimeoutSeconds);

            CollectionAssert.AreEquivalent(expectedIds, GetWeaponRegistryItemIdsForActivePlayer());
            CollectionAssert.AreEquivalent(expectedIds, GetWeaponViewItemIdsForActivePlayer());
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
        public IEnumerator MainTown_ReturnEntryPoint_IsGroundedAndNotVoid()
        {
            yield return LoadMainTownAndBindRuntimePlayerRoot();
            yield return TravelViaTrigger("MainTown_SmokeToIndoor_Trigger", IndoorRangeSceneName, "entry.indoor.arrival");
            yield return TravelViaTrigger("IndoorRange_SmokeToMainTown_Trigger", MainTownSceneName, "entry.maintown.return");

            var activeScene = SceneManager.GetActiveScene();
            var returnEntry = FindEntryPointInScene(activeScene, "entry.maintown.return");
            Assert.That(returnEntry, Is.Not.Null, "Expected MainTown return entry point.");

            var origin = returnEntry.transform.position + Vector3.up * 2f;
            yield return WaitForGroundBelowPoint(origin, 8f, 2f);

            Physics.SyncTransforms();
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

        [Test]
        public void EnsureViewmodelRigAfterTravel_BindsTypedViewmodelAdapter_WhenSimpleNameCollides()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerRoot.transform, false);
            var playerArms = new GameObject("PlayerArms").transform;
            playerArms.SetParent(cameraPivot, false);
            var visuals = new GameObject("PlayerArmsVisual").transform;
            visuals.SetParent(playerArms, false);

            var animator = visuals.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = null;

            playerRoot.AddComponent<ViewmodelAnimationAdapter>();
            var realAdapter = playerRoot.AddComponent<Reloader.Player.Viewmodel.ViewmodelAnimationAdapter>();
            SetPrivateAnimatorField(realAdapter, null);

            InvokeEnsureViewmodelRigAfterTravel(playerRoot.transform);

            var boundAnimator = GetPrivateAnimatorField(realAdapter);
            Assert.That(boundAnimator, Is.Not.Null, "Expected typed travel rebinding to set animator on real ViewmodelAnimationAdapter.");
            Assert.That(boundAnimator, Is.EqualTo(animator), "Expected typed rebinding to target PlayerArms animator.");

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

        private static Animator GetPrivateAnimatorField(object component)
        {
            var field = component.GetType().GetField("_animator", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private _animator field on {component.GetType().Name}.");
            return field.GetValue(component) as Animator;
        }

        private static void SetPrivateAnimatorField(object component, Animator animator)
        {
            var field = component.GetType().GetField("_animator", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private _animator field on {component.GetType().Name}.");
            field.SetValue(component, animator);
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

        private static void ResetTravelCoordinatorState()
        {
            var resetStateMethod = typeof(WorldTravelCoordinator).GetMethod("ResetState", BindingFlags.Static | BindingFlags.NonPublic);
            resetStateMethod?.Invoke(null, null);
        }

        private static void ResetRuntimeKernelState()
        {
            var resetKernelMethod = typeof(RuntimeKernelBootstrapper).GetMethod("ResetForTests", BindingFlags.Static | BindingFlags.NonPublic);
            resetKernelMethod?.Invoke(null, null);
        }

        private static void DestroyPersistentPlayerRoot()
        {
            var runtimePlayerRoots = new System.Collections.Generic.HashSet<GameObject>();
            var persistentRoots = Object.FindObjectsByType<PersistentPlayerRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < persistentRoots.Length; i++)
            {
                var persistentRoot = persistentRoots[i];
                if (persistentRoot != null && persistentRoot.PlayerRootTransform != null)
                {
                    runtimePlayerRoots.Add(persistentRoot.PlayerRootTransform.gameObject);
                }
            }

            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < allTransforms.Length; i++)
            {
                var transform = allTransforms[i];
                if (transform != null && transform.name == "RuntimePlayerRoot")
                {
                    runtimePlayerRoots.Add(transform.gameObject);
                }
            }

            foreach (var runtimePlayerRoot in runtimePlayerRoots)
            {
                if (runtimePlayerRoot != null)
                {
                    Object.DestroyImmediate(runtimePlayerRoot);
                }
            }

            for (var i = 0; i < persistentRoots.Length; i++)
            {
                var persistentRoot = persistentRoots[i];
                if (persistentRoot != null)
                {
                    Object.DestroyImmediate(persistentRoot.gameObject);
                }
            }
        }

        private static IEnumerator LoadMainTownAndBindRuntimePlayerRoot()
        {
            var previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
                yield return null;

                var bootstrapRoot = Object.FindFirstObjectByType<BootstrapWorldRoot>(FindObjectsInactive.Include);
                Assert.That(bootstrapRoot, Is.Not.Null, "Expected Bootstrap to keep the canonical BootstrapWorldRoot loaded.");

                var persistentRoot = BootstrapWorldRoot.Initialize();
                Assert.That(persistentRoot, Is.Not.Null, "Expected Bootstrap runtime initialization to produce a canonical PersistentPlayerRoot.");
                Assert.That(PersistentPlayerRoot.Instance, Is.SameAs(persistentRoot), "Expected one canonical PersistentPlayerRoot instance.");
                Assert.That(persistentRoot.PlayerRootTransform, Is.Not.Null, "Expected a canonical runtime player root before routing into MainTown.");

                yield return null;

                var started = WorldTravelCoordinator.TryLoadSceneAtEntry(MainTownScenePath, "entry.maintown.spawn");
                Assert.That(started, Is.True, "Expected Bootstrap to route into MainTown via the authored entry point.");

                yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);
                yield return WaitForResolvedEntryPoint("entry.maintown.spawn", SceneSwitchTimeoutSeconds);
                yield return WaitForCanonicalPlayerStartupStabilization();
                yield return null;
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            }
        }

        private static void AssertPlayerRootIsAtEntryPoint(string entryPointId)
        {
            var activeScene = SceneManager.GetActiveScene();
            var playerRoot = GetCanonicalPlayerRoot();
            Assert.That(playerRoot, Is.Not.Null, $"Expected PlayerRoot in scene '{activeScene.name}'.");

            var entryPoint = FindEntryPointInScene(activeScene, entryPointId);
            Assert.That(entryPoint, Is.Not.Null, $"Expected SceneEntryPoint '{entryPointId}' in scene '{activeScene.name}'.");
            Assert.That(
                WorldTravelCoordinator.LastResolvedEntryPointId,
                Is.EqualTo(entryPointId),
                $"Expected travel to resolve '{entryPointId}' in scene '{activeScene.name}'.");
        }

        private static Transform GetCanonicalPlayerRoot()
        {
            return PersistentPlayerRoot.Instance?.PlayerRootTransform;
        }

        private static void AssertSinglePlayerRootGlobal()
        {
            var playerRoot = GetCanonicalPlayerRoot();
            Assert.That(playerRoot, Is.Not.Null, "Expected canonical runtime player root after travel.");

            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var count = 0;
            for (var i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t != null && t == playerRoot)
                {
                    count++;
                }
            }

            Assert.That(count, Is.EqualTo(1), "Expected exactly one canonical runtime player root globally after travel.");
        }

        private static void AssertPlayerArmsRigPresentAndBound()
        {
            var playerRoot = GetCanonicalPlayerRoot();
            Assert.That(playerRoot, Is.Not.Null, "Expected PlayerRoot.");

            var cameraPivot = playerRoot.Find("CameraPivot");
            Assert.That(cameraPivot, Is.Not.Null, "Expected CameraPivot under PlayerRoot.");

            var playerArms = cameraPivot.Find("PlayerArms");
            Assert.That(playerArms, Is.Not.Null, "Expected PlayerArms under CameraPivot.");
            var animator = playerArms.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null, "Expected Animator on PlayerArms.");
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null, "Expected runtime animator controller on PlayerArms animator.");
        }

        private static void AssertMainTownControlRigWired()
        {
            var playerRoot = GetCanonicalPlayerRoot();
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

        private static IReadOnlyList<string> GetWeaponRegistryItemIdsForActivePlayer()
        {
            var playerRoot = GetCanonicalPlayerRoot();
            Assert.That(playerRoot, Is.Not.Null, "Expected PlayerRoot.");

            var weaponController = playerRoot.GetComponent("PlayerWeaponController");
            Assert.That(weaponController, Is.Not.Null, "Expected PlayerWeaponController.");

            var weaponRegistryField = weaponController.GetType().GetField("_weaponRegistry", BindingFlags.Instance | BindingFlags.NonPublic);
            var weaponRegistry = weaponRegistryField?.GetValue(weaponController);
            Assert.That(weaponRegistry, Is.Not.Null, "Expected WeaponRegistry.");

            var definitionsField = weaponRegistry.GetType().GetField("_definitions", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(definitionsField, Is.Not.Null, "Expected _definitions field on WeaponRegistry.");

            var ids = new List<string>();
            if (definitionsField.GetValue(weaponRegistry) is IEnumerable definitions)
            {
                foreach (var definition in definitions)
                {
                    if (definition == null)
                    {
                        continue;
                    }

                    var itemId = definition.GetType().GetProperty("ItemId", BindingFlags.Instance | BindingFlags.Public)?.GetValue(definition) as string;
                    if (!string.IsNullOrWhiteSpace(itemId))
                    {
                        ids.Add(itemId);
                    }
                }
            }

            return ids;
        }

        private static IReadOnlyList<string> GetWeaponViewItemIdsForActivePlayer()
        {
            var playerRoot = GetCanonicalPlayerRoot();
            Assert.That(playerRoot, Is.Not.Null, "Expected PlayerRoot.");

            var weaponController = playerRoot.GetComponent("PlayerWeaponController");
            Assert.That(weaponController, Is.Not.Null, "Expected PlayerWeaponController.");

            var viewsField = weaponController.GetType().GetField("_weaponViewPrefabs", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(viewsField, Is.Not.Null, "Expected _weaponViewPrefabs field on PlayerWeaponController.");

            var ids = new List<string>();
            if (viewsField.GetValue(weaponController) is IEnumerable entries)
            {
                foreach (var entry in entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    var itemId = entry.GetType().GetProperty("ItemId", BindingFlags.Instance | BindingFlags.Public)?.GetValue(entry) as string;
                    if (!string.IsNullOrWhiteSpace(itemId))
                    {
                        ids.Add(itemId);
                    }
                }
            }

            return ids;
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

        private static CivilianPopulationRuntimeBridge FindPopulationBridge()
        {
            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected MainTownPopulationRuntime root.");
            return root!.GetComponent<CivilianPopulationRuntimeBridge>();
        }

        private static StaticContractRuntimeProvider FindContractProvider()
        {
            var root = GameObject.Find("MainTownContractRuntime");
            Assert.That(root, Is.Not.Null, "Expected MainTownContractRuntime root.");
            return root!.GetComponent<StaticContractRuntimeProvider>();
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

        private static Type ResolveType(string fullName)
        {
            var direct = Type.GetType(fullName);
            if (direct != null)
            {
                return direct;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var resolved = assemblies[i].GetType(fullName);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            return null;
        }

        private static IEnumerator TravelViaTrigger(string triggerObjectName, string expectedSceneName, string expectedEntryPointId)
        {
            var triggerObject = GameObject.Find(triggerObjectName);
            Assert.That(triggerObject, Is.Not.Null, $"Expected trigger object '{triggerObjectName}'.");
            var trigger = triggerObject.GetComponent<TravelSceneTrigger>();
            Assert.That(trigger, Is.Not.Null, $"Expected TravelSceneTrigger on '{triggerObjectName}'.");

            var startedTravel = false;
            var elapsed = 0f;
            while (!startedTravel && elapsed < 4f)
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
            yield return WaitForCanonicalPlayerStartupStabilization();
        }

        private static IEnumerator WaitForCanonicalPlayerStartupStabilization()
        {
            var playerRoot = GetCanonicalPlayerRoot();
            Assert.That(playerRoot, Is.Not.Null, "Expected canonical runtime player root before startup stabilization.");

            var stableFrameCount = 0;
            var previousPosition = playerRoot.position;
            var elapsed = 0f;
            while (elapsed < 2f && stableFrameCount < 3)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;

                var currentPosition = playerRoot.position;
                if (Vector3.Distance(currentPosition, previousPosition) <= 0.001f)
                {
                    stableFrameCount++;
                }
                else
                {
                    stableFrameCount = 0;
                }

                previousPosition = currentPosition;
            }

            Assert.That(stableFrameCount, Is.GreaterThanOrEqualTo(3), "Expected the canonical runtime player to settle before asserting travel state.");
        }

        private static IEnumerator WaitForGroundBelowPoint(Vector3 origin, float maxDistance, float timeoutSeconds)
        {
            var elapsed = 0f;
            while (elapsed < timeoutSeconds)
            {
                Physics.SyncTransforms();
                if (Physics.Raycast(origin, Vector3.down, maxDistance))
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
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

    public sealed class ViewmodelAnimationAdapter : MonoBehaviour
    {
    }
}
