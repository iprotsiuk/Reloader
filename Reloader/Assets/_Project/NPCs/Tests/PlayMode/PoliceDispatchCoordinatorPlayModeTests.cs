using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.NPCs.Combat;
using Reloader.NPCs.Generation;
using Reloader.NPCs.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.NPCs.Tests.PlayMode
{
    public sealed class PoliceDispatchCoordinatorPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            DestroyExistingCoordinator();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
        }

        [TearDown]
        public void TearDown()
        {
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
            DestroyExistingCoordinator();
        }

        [UnityTest]
        public IEnumerator OnEnable_AfterRestoredIdentifiedHeat_AssignsOnlyNearestCappedSubsetImmediately()
        {
            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject coordinatorRoot = null;
            GameObject copNearRoot = null;
            GameObject copMidRoot = null;
            GameObject copFarRoot = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 10f));

                yield return null;

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);
                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));
                Assert.That(provider.CurrentHeatState.IsPlayerIdentified, Is.True);

                copNearRoot = CreatePoliceResponder(new Vector3(0f, 1f, 0f));
                copMidRoot = CreatePoliceResponder(new Vector3(0f, 1f, 3f));
                copFarRoot = CreatePoliceResponder(new Vector3(0f, 1f, 6f));

                var coordinator = GetOrCreateCoordinator(out coordinatorRoot);
                ConfigureCoordinator(
                    coordinator,
                    maxActiveDispatchCount: 2,
                    dispatchReassignmentHoldSeconds: 0f,
                    dispatchReplacementDistanceThresholdMeters: 0f,
                    dispatchActivationIntervalSeconds: 0f);

                yield return null;

                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);

                ForceSearchState(provider);
                playerRoot.transform.position = new Vector3(25f, 1f, 25f);
                yield return null;

                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));

                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);

                RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
                yield return null;

                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search),
                    "Expected the restored police heat to survive runtime hub rebuild.");

                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);
            }
            finally
            {
                DestroyImmediateIfNeeded(coordinatorRoot);
                DestroyImmediateIfNeeded(copFarRoot);
                DestroyImmediateIfNeeded(copMidRoot);
                DestroyImmediateIfNeeded(copNearRoot);
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        [UnityTest]
        public IEnumerator ClearHeat_DisablesAllDispatchAssignments()
        {
            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject coordinatorRoot = null;
            GameObject copNearRoot = null;
            GameObject copMidRoot = null;
            GameObject copFarRoot = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 10f));

                yield return null;

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);
                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));

                copNearRoot = CreatePoliceResponder(new Vector3(0f, 1f, 0f));
                copMidRoot = CreatePoliceResponder(new Vector3(0f, 1f, 3f));
                copFarRoot = CreatePoliceResponder(new Vector3(0f, 1f, 6f));

                var coordinator = GetOrCreateCoordinator(out coordinatorRoot);
                ConfigureCoordinator(
                    coordinator,
                    maxActiveDispatchCount: 2,
                    dispatchReassignmentHoldSeconds: 0f,
                    dispatchReplacementDistanceThresholdMeters: 0f,
                    dispatchActivationIntervalSeconds: 0f);

                yield return null;

                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);

                ForceClearHeat(provider);
                yield return null;

                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: false);
                AssertResponderEnabled(copFarRoot, expectedEnabled: false);
                Assert.That(ReadDispatchSearchSlotIndex(copNearRoot.GetComponent<PoliceResponderMover>()), Is.EqualTo(-1));
                Assert.That(ReadDispatchSearchSlotCount(copNearRoot.GetComponent<PoliceResponderMover>()), Is.EqualTo(0));
                Assert.That(ReadDispatchSearchSlotIndex(copMidRoot.GetComponent<PoliceResponderMover>()), Is.EqualTo(-1));
                Assert.That(ReadDispatchSearchSlotCount(copMidRoot.GetComponent<PoliceResponderMover>()), Is.EqualTo(0));
                Assert.That(ReadDispatchSearchSlotIndex(copFarRoot.GetComponent<PoliceResponderMover>()), Is.EqualTo(-1));
                Assert.That(ReadDispatchSearchSlotCount(copFarRoot.GetComponent<PoliceResponderMover>()), Is.EqualTo(0));
            }
            finally
            {
                DestroyImmediateIfNeeded(coordinatorRoot);
                DestroyImmediateIfNeeded(copFarRoot);
                DestroyImmediateIfNeeded(copMidRoot);
                DestroyImmediateIfNeeded(copNearRoot);
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        [UnityTest]
        public IEnumerator ActivePursuit_ReassignmentHoldWindow_PreservesCurrentAssignedSubsetUntilItExpires()
        {
            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject coordinatorRoot = null;
            GameObject copNearRoot = null;
            GameObject copMidRoot = null;
            GameObject copFarRoot = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 10f));

                yield return null;

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);
                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));

                copNearRoot = CreatePoliceResponder(new Vector3(0f, 1f, 0f));
                copMidRoot = CreatePoliceResponder(new Vector3(0f, 1f, 5f));
                copFarRoot = CreatePoliceResponder(new Vector3(0f, 1f, 7f));

                var coordinator = GetOrCreateCoordinator(out coordinatorRoot);
                ConfigureCoordinator(
                    coordinator,
                    maxActiveDispatchCount: 2,
                    dispatchReassignmentHoldSeconds: 0.5f,
                    dispatchReplacementDistanceThresholdMeters: 0f,
                    dispatchActivationIntervalSeconds: 0f);

                yield return null;

                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(2));

                playerRoot.transform.position = new Vector3(0f, 1f, 2.75f);
                yield return null;

                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(2));

                yield return new WaitForSecondsRealtime(0.55f);
                yield return null;

                AssertResponderEnabled(copNearRoot, expectedEnabled: true);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: false);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(2));
            }
            finally
            {
                DestroyImmediateIfNeeded(coordinatorRoot);
                DestroyImmediateIfNeeded(copFarRoot);
                DestroyImmediateIfNeeded(copMidRoot);
                DestroyImmediateIfNeeded(copNearRoot);
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        [UnityTest]
        public IEnumerator ActivePursuit_ReplacementThreshold_BlocksMarginalSubsetSwap()
        {
            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject coordinatorRoot = null;
            GameObject copNearRoot = null;
            GameObject copMidRoot = null;
            GameObject copFarRoot = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 10f));

                yield return null;

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);
                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));

                copNearRoot = CreatePoliceResponder(new Vector3(0f, 1f, 0f));
                copMidRoot = CreatePoliceResponder(new Vector3(0f, 1f, 5f));
                copFarRoot = CreatePoliceResponder(new Vector3(0f, 1f, 5.8f));

                var coordinator = GetOrCreateCoordinator(out coordinatorRoot);
                ConfigureCoordinator(
                    coordinator,
                    maxActiveDispatchCount: 2,
                    dispatchReassignmentHoldSeconds: 0f,
                    dispatchReplacementDistanceThresholdMeters: 0.5f,
                    dispatchActivationIntervalSeconds: 0f);

                yield return null;

                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);

                playerRoot.transform.position = new Vector3(0f, 1f, 2.75f);
                yield return null;

                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(2));
            }
            finally
            {
                DestroyImmediateIfNeeded(coordinatorRoot);
                DestroyImmediateIfNeeded(copFarRoot);
                DestroyImmediateIfNeeded(copMidRoot);
                DestroyImmediateIfNeeded(copNearRoot);
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        [UnityTest]
        public IEnumerator ActivePursuit_WhenReservePoliceSlotsExist_DispatchSpawnsMissingResponders()
        {
            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject bridgeRoot = null;
            MainTownPopulationDefinition definition = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 10f));
                bridgeRoot = new GameObject("CivilianPopulationRuntimeBridge");
                var bridge = bridgeRoot.AddComponent<CivilianPopulationRuntimeBridge>();
                CreateAnchor(bridgeRoot.transform, "Anchor_Cop_Ambient", new Vector3(0f, 1f, 6f));
                CreateAnchor(bridgeRoot.transform, "Anchor_Cop_Reserve", new Vector3(0f, 1f, 3f));
                definition = CreatePoliceReservePopulationDefinition();

                ConfigureBridgeForPoliceReserveDispatch(bridge, definition);

                yield return null;

                var initialSpawned = bridgeRoot.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
                Assert.That(initialSpawned.Length, Is.EqualTo(1));
                Assert.That(initialSpawned[0].PopulationSlotId, Is.EqualTo("cops.ambient"));

                var coordinator = UnityEngine.Object.FindFirstObjectByType<PoliceDispatchCoordinator>(FindObjectsInactive.Include);
                Assert.That(coordinator, Is.Not.Null, "Expected ambient police spawn to bootstrap a dispatch coordinator.");
                ConfigureCoordinator(
                    coordinator!,
                    maxActiveDispatchCount: 2,
                    dispatchReassignmentHoldSeconds: 0f,
                    dispatchReplacementDistanceThresholdMeters: 0f,
                    dispatchActivationIntervalSeconds: 0f);

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);
                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));

                yield return null;
                yield return null;

                var spawned = bridgeRoot.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
                Assert.That(spawned.Length, Is.EqualTo(2), "Expected dispatch to materialize the hidden reserve police slot when active pressure exceeds current registered police.");
                Assert.That(spawned.Any(component => component.PopulationSlotId == "cops.reserve"), Is.True);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(2));
                Assert.That(coordinator.RegisteredResponderCount, Is.EqualTo(2));
            }
            finally
            {
                DestroyImmediateIfNeeded(definition);
                DestroyImmediateIfNeeded(bridgeRoot);
                DestroyExistingCoordinator();
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        [UnityTest]
        public IEnumerator ClearHeat_WhenReservePoliceWasSpawnedByDispatch_ReturnsReserveToHiddenState()
        {
            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject bridgeRoot = null;
            MainTownPopulationDefinition definition = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 10f));
                bridgeRoot = new GameObject("CivilianPopulationRuntimeBridge");
                var bridge = bridgeRoot.AddComponent<CivilianPopulationRuntimeBridge>();
                CreateAnchor(bridgeRoot.transform, "Anchor_Cop_Ambient", new Vector3(0f, 1f, 6f));
                CreateAnchor(bridgeRoot.transform, "Anchor_Cop_Reserve", new Vector3(0f, 1f, 3f));
                definition = CreatePoliceReservePopulationDefinition();

                ConfigureBridgeForPoliceReserveDispatch(bridge, definition);

                yield return null;

                var coordinator = UnityEngine.Object.FindFirstObjectByType<PoliceDispatchCoordinator>(FindObjectsInactive.Include);
                Assert.That(coordinator, Is.Not.Null, "Expected ambient police spawn to bootstrap a dispatch coordinator.");
                ConfigureCoordinator(
                    coordinator!,
                    maxActiveDispatchCount: 2,
                    dispatchReassignmentHoldSeconds: 0f,
                    dispatchReplacementDistanceThresholdMeters: 0f,
                    dispatchActivationIntervalSeconds: 0f);

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);

                yield return null;
                yield return null;

                Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.0002", out _), Is.True,
                    "Expected dispatch pressure to materialize the hidden reserve police slot first.");

                ForceClearHeat(provider);
                yield return null;
                yield return null;

                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
                Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.0001", out var ambientSpawn), Is.True,
                    "Expected ambient police to remain scene-spawned after heat clears.");
                Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.0002", out _), Is.False,
                    "Expected dispatch-spawned reserve police to be returned to hidden reserve state after heat clears.");
                Assert.That(ambientSpawn!.GetComponent<PoliceResponderMover>().enabled, Is.False);
                Assert.That(ambientSpawn.GetComponent<PoliceHostileShooter>().enabled, Is.False);
                Assert.That(bridge.Runtime.Civilians.Count, Is.EqualTo(2));
                Assert.That(bridge.Runtime.Civilians.Single(record => record.CivilianId == "citizen.mainTown.0002").IsAlive, Is.True);
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));
            }
            finally
            {
                DestroyImmediateIfNeeded(definition);
                DestroyImmediateIfNeeded(bridgeRoot);
                DestroyExistingCoordinator();
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        [UnityTest]
        public IEnumerator SearchDecay_WhenReservePoliceWasSpawnedByDispatch_ReturnsDeselectedReserveToHiddenState()
        {
            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject bridgeRoot = null;
            MainTownPopulationDefinition definition = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 10f));
                bridgeRoot = new GameObject("CivilianPopulationRuntimeBridge");
                var bridge = bridgeRoot.AddComponent<CivilianPopulationRuntimeBridge>();
                CreateAnchor(bridgeRoot.transform, "Anchor_Cop_Ambient", new Vector3(0f, 1f, 6f));
                CreateAnchor(bridgeRoot.transform, "Anchor_Cop_Reserve", new Vector3(0f, 1f, 3f));
                definition = CreatePoliceReservePopulationDefinition();

                ConfigureBridgeForPoliceReserveDispatch(bridge, definition);

                yield return null;

                var coordinator = UnityEngine.Object.FindFirstObjectByType<PoliceDispatchCoordinator>(FindObjectsInactive.Include);
                Assert.That(coordinator, Is.Not.Null, "Expected ambient police spawn to bootstrap a dispatch coordinator.");
                ConfigureCoordinator(
                    coordinator!,
                    maxActiveDispatchCount: 2,
                    dispatchReassignmentHoldSeconds: 0f,
                    dispatchReplacementDistanceThresholdMeters: 0f,
                    dispatchActivationIntervalSeconds: 0f,
                    maxSearchDispatchCount: 2,
                    minSearchDispatchCount: 1,
                    searchResponderDecayIntervalSeconds: 0.2f);

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);

                yield return null;
                yield return null;

                Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.0002", out _), Is.True,
                    "Expected dispatch pressure to materialize the hidden reserve police slot before search decay.");
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(2));

                ForceSearchState(provider);
                playerRoot.transform.position = new Vector3(25f, 1f, 25f);
                yield return null;

                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
                Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.0002", out _), Is.True);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(2));

                yield return new WaitForSecondsRealtime(0.22f);
                yield return null;
                yield return null;

                Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.0001", out var ambientSpawn), Is.True,
                    "Expected ambient police to remain scene-spawned as search pressure decays.");
                Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.0002", out _), Is.False,
                    "Expected the deselected dispatch-spawned reserve police responder to return to hidden reserve state.");
                Assert.That(ambientSpawn!.GetComponent<PoliceResponderMover>().enabled, Is.True);
                Assert.That(ambientSpawn.GetComponent<PoliceHostileShooter>().enabled, Is.True);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(1));
                Assert.That(bridge.Runtime.Civilians.Single(record => record.CivilianId == "citizen.mainTown.0002").IsAlive, Is.True);
                Assert.That(bridge.Runtime.PendingReplacements.Count, Is.EqualTo(0));
            }
            finally
            {
                DestroyImmediateIfNeeded(definition);
                DestroyImmediateIfNeeded(bridgeRoot);
                DestroyExistingCoordinator();
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        [UnityTest]
        public IEnumerator ActivePursuit_WhenFirstCandidateBridgeIsInactive_SkipsInactiveReserveSpawns()
        {
            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject coordinatorRoot = null;
            GameObject bridgeRootA = null;
            GameObject bridgeRootB = null;
            MainTownPopulationDefinition definitionA = null;
            MainTownPopulationDefinition definitionB = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 10f));

                bridgeRootA = new GameObject("CivilianPopulationRuntimeBridge_A");
                var bridgeA = bridgeRootA.AddComponent<CivilianPopulationRuntimeBridge>();
                CreateAnchor(bridgeRootA.transform, "Anchor_Cop_Reserve", new Vector3(0f, 1f, 2f));
                definitionA = CreatePoliceReservePopulationDefinition(includeAmbientPolice: false);
                ConfigureBridgeForPoliceReserveDispatch(bridgeA, definitionA);

                bridgeRootB = new GameObject("CivilianPopulationRuntimeBridge_B");
                var bridgeB = bridgeRootB.AddComponent<CivilianPopulationRuntimeBridge>();
                CreateAnchor(bridgeRootB.transform, "Anchor_Cop_Reserve", new Vector3(0f, 1f, 3f));
                definitionB = CreatePoliceReservePopulationDefinition(includeAmbientPolice: false);
                ConfigureBridgeForPoliceReserveDispatch(bridgeB, definitionB);

                var bridgeOrder = UnityEngine.Object.FindObjectsByType<CivilianPopulationRuntimeBridge>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.That(bridgeOrder.Length, Is.EqualTo(2));

                var inactiveBridge = bridgeOrder[0];
                var activeBridge = bridgeOrder[1];
                inactiveBridge.gameObject.SetActive(false);

                var coordinator = GetOrCreateCoordinator(out coordinatorRoot);
                ConfigureCoordinator(
                    coordinator,
                    maxActiveDispatchCount: 1,
                    dispatchReassignmentHoldSeconds: 0f,
                    dispatchReplacementDistanceThresholdMeters: 0f,
                    dispatchActivationIntervalSeconds: 0f);

                yield return null;

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);
                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));

                yield return null;
                yield return null;

                var inactiveSpawned = inactiveBridge.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
                var activeSpawned = activeBridge.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
                Assert.That(inactiveSpawned.Length, Is.EqualTo(0), "Expected the coordinator to ignore inactive population bridges when spawning dispatch reserves.");
                Assert.That(activeSpawned.Length, Is.EqualTo(1));
                Assert.That(activeSpawned[0].PopulationSlotId, Is.EqualTo("cops.reserve"));
                Assert.That(coordinator.RegisteredResponderCount, Is.EqualTo(1));
            }
            finally
            {
                DestroyImmediateIfNeeded(definitionB);
                DestroyImmediateIfNeeded(definitionA);
                DestroyImmediateIfNeeded(bridgeRootB);
                DestroyImmediateIfNeeded(bridgeRootA);
                DestroyImmediateIfNeeded(coordinatorRoot);
                DestroyExistingCoordinator();
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        [UnityTest]
        public IEnumerator ActivePursuit_WhenReserveCandidatesSpanMultipleBridges_SpawnsNearestReserveGlobally()
        {
            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject coordinatorRoot = null;
            GameObject bridgeRootA = null;
            GameObject bridgeRootB = null;
            MainTownPopulationDefinition definitionA = null;
            MainTownPopulationDefinition definitionB = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 10f));

                bridgeRootA = new GameObject("CivilianPopulationRuntimeBridge_A");
                var bridgeA = bridgeRootA.AddComponent<CivilianPopulationRuntimeBridge>();
                CreateAnchor(bridgeRootA.transform, "Anchor_Cop_Reserve", Vector3.zero);
                definitionA = CreatePoliceReservePopulationDefinition(includeAmbientPolice: false);
                ConfigureBridgeForPoliceReserveDispatch(bridgeA, definitionA);

                bridgeRootB = new GameObject("CivilianPopulationRuntimeBridge_B");
                var bridgeB = bridgeRootB.AddComponent<CivilianPopulationRuntimeBridge>();
                CreateAnchor(bridgeRootB.transform, "Anchor_Cop_Reserve", Vector3.zero);
                definitionB = CreatePoliceReservePopulationDefinition(includeAmbientPolice: false);
                ConfigureBridgeForPoliceReserveDispatch(bridgeB, definitionB);

                var bridgeOrder = UnityEngine.Object.FindObjectsByType<CivilianPopulationRuntimeBridge>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.That(bridgeOrder.Length, Is.EqualTo(2));

                SetReserveAnchorPosition(bridgeOrder[0], new Vector3(0f, 1f, 20f));
                SetReserveAnchorPosition(bridgeOrder[1], new Vector3(0f, 1f, 3f));

                var coordinator = GetOrCreateCoordinator(out coordinatorRoot);
                ConfigureCoordinator(
                    coordinator,
                    maxActiveDispatchCount: 1,
                    dispatchReassignmentHoldSeconds: 0f,
                    dispatchReplacementDistanceThresholdMeters: 0f,
                    dispatchActivationIntervalSeconds: 0f);

                yield return null;

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);
                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));

                yield return null;
                yield return null;

                var farSpawned = bridgeOrder[0].GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
                var nearSpawned = bridgeOrder[1].GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
                Assert.That(farSpawned.Length, Is.EqualTo(0), "Expected dispatch to rank reserve candidates globally instead of taking the first bridge that can spawn one.");
                Assert.That(nearSpawned.Length, Is.EqualTo(1));
                Assert.That(nearSpawned[0].PopulationSlotId, Is.EqualTo("cops.reserve"));
                Assert.That(Vector3.Distance(nearSpawned[0].transform.position, new Vector3(0f, 1f, 3f)), Is.LessThan(0.1f));
                Assert.That(coordinator.RegisteredResponderCount, Is.EqualTo(1));
            }
            finally
            {
                DestroyImmediateIfNeeded(definitionB);
                DestroyImmediateIfNeeded(definitionA);
                DestroyImmediateIfNeeded(bridgeRootB);
                DestroyImmediateIfNeeded(bridgeRootA);
                DestroyImmediateIfNeeded(coordinatorRoot);
                DestroyExistingCoordinator();
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        [UnityTest]
        public IEnumerator ActivePursuit_SelectedResponders_EnableInRankedDispatchWaves()
        {
            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject coordinatorRoot = null;
            GameObject copNearRoot = null;
            GameObject copMidRoot = null;
            GameObject copFarRoot = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 10f));

                yield return null;

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);
                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));

                copNearRoot = CreatePoliceResponder(new Vector3(0f, 1f, 0f));
                copMidRoot = CreatePoliceResponder(new Vector3(0f, 1f, 3f));
                copFarRoot = CreatePoliceResponder(new Vector3(0f, 1f, 6f));

                var coordinator = GetOrCreateCoordinator(out coordinatorRoot);
                ConfigureCoordinator(
                    coordinator,
                    maxActiveDispatchCount: 3,
                    dispatchReassignmentHoldSeconds: 0f,
                    dispatchReplacementDistanceThresholdMeters: 0f,
                    dispatchActivationIntervalSeconds: 0.2f);

                yield return null;

                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: false);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(1));

                yield return new WaitForSecondsRealtime(0.22f);
                yield return null;

                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(2));

                yield return new WaitForSecondsRealtime(0.22f);
                yield return null;

                AssertResponderEnabled(copNearRoot, expectedEnabled: true);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(3));
            }
            finally
            {
                DestroyImmediateIfNeeded(coordinatorRoot);
                DestroyImmediateIfNeeded(copFarRoot);
                DestroyImmediateIfNeeded(copMidRoot);
                DestroyImmediateIfNeeded(copNearRoot);
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        [UnityTest]
        public IEnumerator Search_UsesReducedDispatchCapComparedToActivePursuit()
        {
            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject coordinatorRoot = null;
            GameObject copNearRoot = null;
            GameObject copMidRoot = null;
            GameObject copFarRoot = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 10f));

                yield return null;

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);
                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));

                copNearRoot = CreatePoliceResponder(new Vector3(0f, 1f, 0f));
                copMidRoot = CreatePoliceResponder(new Vector3(0f, 1f, 3f));
                copFarRoot = CreatePoliceResponder(new Vector3(0f, 1f, 6f));

                var coordinator = GetOrCreateCoordinator(out coordinatorRoot);
                ConfigureCoordinator(
                    coordinator,
                    maxActiveDispatchCount: 3,
                    dispatchReassignmentHoldSeconds: 0f,
                    dispatchReplacementDistanceThresholdMeters: 0f,
                    dispatchActivationIntervalSeconds: 0f,
                    maxSearchDispatchCount: 2);

                yield return null;

                AssertResponderEnabled(copNearRoot, expectedEnabled: true);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(3));

                ForceSearchState(provider);
                playerRoot.transform.position = new Vector3(25f, 1f, 25f);
                yield return null;

                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(2));
            }
            finally
            {
                DestroyImmediateIfNeeded(coordinatorRoot);
                DestroyImmediateIfNeeded(copFarRoot);
                DestroyImmediateIfNeeded(copMidRoot);
                DestroyImmediateIfNeeded(copNearRoot);
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        [UnityTest]
        public IEnumerator Search_GraduallyShedsResponderPressureOverTime()
        {
            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject coordinatorRoot = null;
            GameObject copNearRoot = null;
            GameObject copMidRoot = null;
            GameObject copFarRoot = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 10f));

                yield return null;

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);
                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));

                copNearRoot = CreatePoliceResponder(new Vector3(0f, 1f, 0f));
                copMidRoot = CreatePoliceResponder(new Vector3(0f, 1f, 3f));
                copFarRoot = CreatePoliceResponder(new Vector3(0f, 1f, 6f));

                var coordinator = GetOrCreateCoordinator(out coordinatorRoot);
                ConfigureCoordinator(
                    coordinator,
                    maxActiveDispatchCount: 3,
                    dispatchReassignmentHoldSeconds: 0f,
                    dispatchReplacementDistanceThresholdMeters: 0f,
                    dispatchActivationIntervalSeconds: 0f,
                    maxSearchDispatchCount: 3,
                    minSearchDispatchCount: 1,
                    searchResponderDecayIntervalSeconds: 0.2f);

                yield return null;

                AssertResponderEnabled(copNearRoot, expectedEnabled: true);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(3));

                ForceSearchState(provider);
                playerRoot.transform.position = new Vector3(25f, 1f, 25f);
                yield return null;

                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
                AssertResponderEnabled(copNearRoot, expectedEnabled: true);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(3));

                yield return new WaitForSecondsRealtime(0.22f);
                yield return null;

                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(2));

                yield return new WaitForSecondsRealtime(0.22f);
                yield return null;

                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: false);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(1));
            }
            finally
            {
                DestroyImmediateIfNeeded(coordinatorRoot);
                DestroyImmediateIfNeeded(copFarRoot);
                DestroyImmediateIfNeeded(copMidRoot);
                DestroyImmediateIfNeeded(copNearRoot);
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        [UnityTest]
        public IEnumerator ClearHeat_CancelsPendingDispatchWarmups()
        {
            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject coordinatorRoot = null;
            GameObject copNearRoot = null;
            GameObject copMidRoot = null;
            GameObject copFarRoot = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 10f));

                yield return null;

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);
                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));

                copNearRoot = CreatePoliceResponder(new Vector3(0f, 1f, 0f));
                copMidRoot = CreatePoliceResponder(new Vector3(0f, 1f, 3f));
                copFarRoot = CreatePoliceResponder(new Vector3(0f, 1f, 6f));

                var coordinator = GetOrCreateCoordinator(out coordinatorRoot);
                ConfigureCoordinator(
                    coordinator,
                    maxActiveDispatchCount: 3,
                    dispatchReassignmentHoldSeconds: 0f,
                    dispatchReplacementDistanceThresholdMeters: 0f,
                    dispatchActivationIntervalSeconds: 0.3f);

                yield return null;

                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: false);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(1));

                ForceClearHeat(provider);
                yield return new WaitForSecondsRealtime(0.7f);
                yield return null;

                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: false);
                AssertResponderEnabled(copFarRoot, expectedEnabled: false);
                Assert.That(coordinator.ActiveResponderCount, Is.EqualTo(0));
                Assert.That(ReadDispatchSearchSlotIndex(copNearRoot.GetComponent<PoliceResponderMover>()), Is.EqualTo(-1));
                Assert.That(ReadDispatchSearchSlotCount(copNearRoot.GetComponent<PoliceResponderMover>()), Is.EqualTo(0));
                Assert.That(ReadDispatchSearchSlotIndex(copMidRoot.GetComponent<PoliceResponderMover>()), Is.EqualTo(-1));
                Assert.That(ReadDispatchSearchSlotCount(copMidRoot.GetComponent<PoliceResponderMover>()), Is.EqualTo(0));
                Assert.That(ReadDispatchSearchSlotIndex(copFarRoot.GetComponent<PoliceResponderMover>()), Is.EqualTo(-1));
                Assert.That(ReadDispatchSearchSlotCount(copFarRoot.GetComponent<PoliceResponderMover>()), Is.EqualTo(0));
            }
            finally
            {
                DestroyImmediateIfNeeded(coordinatorRoot);
                DestroyImmediateIfNeeded(copFarRoot);
                DestroyImmediateIfNeeded(copMidRoot);
                DestroyImmediateIfNeeded(copNearRoot);
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        [UnityTest]
        public IEnumerator Search_AssignsDistinctStableSearchSlotsToSelectedResponders()
        {
            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject coordinatorRoot = null;
            GameObject copNearRoot = null;
            GameObject copMidRoot = null;
            GameObject copFarRoot = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 10f));

                yield return null;

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);

                copNearRoot = CreatePoliceResponder(new Vector3(0f, 1f, 0f));
                copMidRoot = CreatePoliceResponder(new Vector3(0f, 1f, 3f));
                copFarRoot = CreatePoliceResponder(new Vector3(0f, 1f, 6f));

                var coordinator = GetOrCreateCoordinator(out coordinatorRoot);
                ConfigureCoordinator(
                    coordinator,
                    maxActiveDispatchCount: 2,
                    dispatchReassignmentHoldSeconds: 0f,
                    dispatchReplacementDistanceThresholdMeters: 0f,
                    dispatchActivationIntervalSeconds: 0.3f);

                yield return null;

                ForceSearchState(provider);
                playerRoot.transform.position = new Vector3(25f, 1f, 25f);
                yield return null;

                var midMover = copMidRoot.GetComponent<PoliceResponderMover>();
                var farMover = copFarRoot.GetComponent<PoliceResponderMover>();
                var nearMover = copNearRoot.GetComponent<PoliceResponderMover>();
                var midSlotIndex = ReadDispatchSearchSlotIndex(midMover);
                var farSlotIndex = ReadDispatchSearchSlotIndex(farMover);

                Assert.That(ReadDispatchSearchSlotCount(midMover), Is.EqualTo(2));
                Assert.That(ReadDispatchSearchSlotCount(farMover), Is.EqualTo(2));
                Assert.That(midSlotIndex, Is.Not.EqualTo(farSlotIndex));
                Assert.That(ReadDispatchSearchSlotIndex(nearMover), Is.EqualTo(-1));
                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: false);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);

                yield return new WaitForSecondsRealtime(0.35f);
                yield return null;

                Assert.That(ReadDispatchSearchSlotIndex(midMover), Is.EqualTo(midSlotIndex));
                Assert.That(ReadDispatchSearchSlotIndex(farMover), Is.EqualTo(farSlotIndex));
                AssertResponderEnabled(copNearRoot, expectedEnabled: false);
                AssertResponderEnabled(copMidRoot, expectedEnabled: true);
                AssertResponderEnabled(copFarRoot, expectedEnabled: true);
            }
            finally
            {
                DestroyImmediateIfNeeded(coordinatorRoot);
                DestroyImmediateIfNeeded(copFarRoot);
                DestroyImmediateIfNeeded(copMidRoot);
                DestroyImmediateIfNeeded(copNearRoot);
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        private static GameObject CreatePoliceResponder(Vector3 position, float moveSpeedMetersPerSecond = 0f)
        {
            var responderRoot = new GameObject($"PoliceResponder_{position.z:0}");
            responderRoot.transform.position = position;

            var mover = responderRoot.AddComponent<PoliceResponderMover>();
            ConfigureMoverForTests(mover, moveSpeedMetersPerSecond: moveSpeedMetersPerSecond, searchRadiusMeters: 1.25f, searchOrbitDegreesPerSecond: 180f);

            var shooter = responderRoot.AddComponent<PoliceHostileShooter>();
            InvokeVoid(shooter, "SetHostileOverrideForTests", true);
            ConfigureShooterForTests(shooter, rangeMeters: 0.25f);

            return responderRoot;
        }

        private static GameObject CreatePlayerRoot(Vector3 position)
        {
            var playerRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerRoot.name = "RuntimePlayerRoot";
            playerRoot.transform.position = position;
            playerRoot.transform.localScale = new Vector3(1.25f, 2f, 1.25f);
            playerRoot.AddComponent<HumanoidDamageReceiver>();
            playerRoot.AddComponent<PlayerDeathContractBridge>();
            return playerRoot;
        }

        private static void ConfigureBridgeForPoliceReserveDispatch(CivilianPopulationRuntimeBridge bridge, MainTownPopulationDefinition definition)
        {
            SetField(bridge, "_appearanceLibrary", CreateLibrary());
            SetField(bridge, "_civilianIdPrefix", "citizen.mainTown");
            SetField(bridge, "_initialPopulationCount", 0);
            SetField(bridge, "_spawnAnchorIds", Array.Empty<string>());
            SetField(bridge, "_populationDefinition", definition);
        }

        private static CivilianAppearanceLibrary CreateLibrary()
        {
            return new CivilianAppearanceLibrary
            {
                BaseBodyIds = new[] { "body.male.a" },
                PresentationTypes = new[] { "masculine" },
                HairIds = new[] { "hair.short.01" },
                HairColorIds = new[] { "hair.black" },
                BeardIds = new[] { "beard.none" },
                OutfitTopIds = new[] { "top.coat.01" },
                OutfitBottomIds = new[] { "bottom.jeans.01" },
                OuterwearIds = new[] { "outer.gray.coat" },
                MaterialColorIds = new[] { "color.gray" },
                DescriptionTags = new[] { "gray coat" }
            };
        }

        private static MainTownPopulationDefinition CreatePoliceReservePopulationDefinition(bool includeAmbientPolice = true)
        {
            var definition = ScriptableObject.CreateInstance<MainTownPopulationDefinition>();
            var slots = new System.Collections.Generic.List<MainTownPopulationSlotDefinition>();
            if (includeAmbientPolice)
            {
                slots.Add(new MainTownPopulationSlotDefinition
                        {
                            PopulationSlotId = "cops.ambient",
                            PoolId = "cops",
                            AreaTag = "maintown.watch",
                            SpawnAnchorId = "Anchor_Cop_Ambient",
                            Habitat = MainTownPopulationHabitat.Town,
                            IsProtectedFromContracts = true,
                            SpawnOnSceneLoad = true
                        });
            }

            slots.Add(new MainTownPopulationSlotDefinition
                        {
                            PopulationSlotId = "cops.reserve",
                            PoolId = "cops",
                            AreaTag = "maintown.watch.reserve",
                            SpawnAnchorId = "Anchor_Cop_Reserve",
                            Habitat = MainTownPopulationHabitat.Town,
                            IsProtectedFromContracts = true,
                            SpawnOnSceneLoad = false
                        });

            definition.Pools = new[]
            {
                new MainTownPopulationPoolDefinition
                {
                    PoolId = "cops",
                    Slots = slots.ToArray()
                }
            };
            return definition;
        }

        private static Transform CreateAnchor(Transform parent, string name, Vector3 position)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = position;
            return anchor;
        }

        private static void SetReserveAnchorPosition(CivilianPopulationRuntimeBridge bridge, Vector3 position)
        {
            var reserveAnchor = bridge.transform.Find("Anchor_Cop_Reserve");
            Assert.That(reserveAnchor, Is.Not.Null, "Expected a reserve anchor on the police dispatch bridge.");
            reserveAnchor!.localPosition = position;
        }

        private static PoliceDispatchCoordinator GetOrCreateCoordinator(out GameObject coordinatorRoot)
        {
            var existingCoordinator = UnityEngine.Object.FindFirstObjectByType<PoliceDispatchCoordinator>(FindObjectsInactive.Include);
            if (existingCoordinator != null)
            {
                UnityEngine.Object.DestroyImmediate(existingCoordinator.gameObject);
            }

            coordinatorRoot = new GameObject("PoliceDispatchCoordinator");
            return coordinatorRoot.AddComponent<PoliceDispatchCoordinator>();
        }

        private static void DestroyExistingCoordinator()
        {
            var existingCoordinator = UnityEngine.Object.FindFirstObjectByType<PoliceDispatchCoordinator>(FindObjectsInactive.Include);
            if (existingCoordinator != null)
            {
                UnityEngine.Object.DestroyImmediate(existingCoordinator.gameObject);
            }
        }

        private static void AssertResponderEnabled(GameObject responderRoot, bool expectedEnabled)
        {
            var mover = responderRoot.GetComponent<PoliceResponderMover>();
            var shooter = responderRoot.GetComponent<PoliceHostileShooter>();

            Assert.That(mover, Is.Not.Null);
            Assert.That(shooter, Is.Not.Null);
            Assert.That(mover!.enabled, Is.EqualTo(expectedEnabled), responderRoot.name);
            Assert.That(shooter!.enabled, Is.EqualTo(expectedEnabled), responderRoot.name);
        }

        private static void ConfigureMoverForTests(PoliceResponderMover mover, float moveSpeedMetersPerSecond, float searchRadiusMeters, float searchOrbitDegreesPerSecond)
        {
            SetField(mover, "_moveSpeedMetersPerSecond", moveSpeedMetersPerSecond);
            SetField(mover, "_searchRadiusMeters", searchRadiusMeters);
            SetField(mover, "_searchOrbitDegreesPerSecond", searchOrbitDegreesPerSecond);
        }

        private static void ConfigureShooterForTests(PoliceHostileShooter shooter, float rangeMeters)
        {
            SetField(shooter, "_rangeMeters", rangeMeters);
        }

        private static void ConfigureCoordinator(
            PoliceDispatchCoordinator coordinator,
            int maxActiveDispatchCount,
            float dispatchReassignmentHoldSeconds,
            float dispatchReplacementDistanceThresholdMeters,
            float dispatchActivationIntervalSeconds,
            int maxSearchDispatchCount = 2,
            int minSearchDispatchCount = 1,
            float searchResponderDecayIntervalSeconds = 0f)
        {
            SetField(coordinator, "_maxActiveDispatchCount", maxActiveDispatchCount);
            SetField(coordinator, "_dispatchReassignmentHoldSeconds", dispatchReassignmentHoldSeconds);
            SetField(coordinator, "_dispatchReplacementDistanceThresholdMeters", dispatchReplacementDistanceThresholdMeters);
            SetField(coordinator, "_dispatchActivationIntervalSeconds", dispatchActivationIntervalSeconds);
            SetField(coordinator, "_maxSearchDispatchCount", maxSearchDispatchCount);
            SetField(coordinator, "_minSearchDispatchCount", minSearchDispatchCount);
            SetField(coordinator, "_searchResponderDecayIntervalSeconds", searchResponderDecayIntervalSeconds);
        }

        private static void ForceSearchState(StaticContractRuntimeProvider provider)
        {
            var runtimeField = typeof(StaticContractRuntimeProvider).GetField("_runtime", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(runtimeField, Is.Not.Null);
            var runtime = runtimeField!.GetValue(provider);
            Assert.That(runtime, Is.Not.Null);

            var heatField = runtime!.GetType().GetField("_policeHeatRuntime", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(heatField, Is.Not.Null);
            var heatRuntime = heatField!.GetValue(runtime);
            Assert.That(heatRuntime, Is.Not.Null);

            var reportLineOfSightLost = heatRuntime!.GetType().GetMethod("ReportLineOfSightLost", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(reportLineOfSightLost, Is.Not.Null);
            reportLineOfSightLost!.Invoke(heatRuntime, null);
        }

        private static void ForceClearHeat(StaticContractRuntimeProvider provider)
        {
            var runtimeField = typeof(StaticContractRuntimeProvider).GetField("_runtime", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(runtimeField, Is.Not.Null);
            var runtime = runtimeField!.GetValue(provider);
            Assert.That(runtime, Is.Not.Null);

            var heatField = runtime!.GetType().GetField("_policeHeatRuntime", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(heatField, Is.Not.Null);
            var heatRuntime = heatField!.GetValue(runtime);
            Assert.That(heatRuntime, Is.Not.Null);

            var forceClear = heatRuntime!.GetType().GetMethod("ForceClear", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(forceClear, Is.Not.Null);
            forceClear!.Invoke(heatRuntime, null);
        }

        private static void InvokeVoid(object instance, string methodName, object argument)
        {
            var method = instance.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Expected {instance.GetType().Name}.{methodName} to exist.");
            method!.Invoke(instance, new[] { argument });
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {instance.GetType().Name}.");
            field!.SetValue(instance, value);
        }

        private static int ReadDispatchSearchSlotIndex(PoliceResponderMover mover)
        {
            return (int)ReadField(mover, "_dispatchSearchSlotIndex");
        }

        private static int ReadDispatchSearchSlotCount(PoliceResponderMover mover)
        {
            return (int)ReadField(mover, "_dispatchSearchSlotCount");
        }

        private static object ReadField(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {instance.GetType().Name}.");
            return field!.GetValue(instance);
        }

        private static void DestroyImmediateIfNeeded(UnityEngine.Object instance)
        {
            if (instance == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
