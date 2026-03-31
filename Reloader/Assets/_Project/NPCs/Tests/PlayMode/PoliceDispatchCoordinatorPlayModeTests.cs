using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.NPCs.Combat;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.NPCs.Tests.PlayMode
{
    public sealed class PoliceDispatchCoordinatorPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
        }

        [TearDown]
        public void TearDown()
        {
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
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
                    dispatchReplacementDistanceThresholdMeters: 0f);

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
                    dispatchReplacementDistanceThresholdMeters: 0f);

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
                    dispatchReplacementDistanceThresholdMeters: 0f);

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
                    dispatchReplacementDistanceThresholdMeters: 0.5f);

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
                    dispatchReplacementDistanceThresholdMeters: 0f);

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

                yield return null;

                Assert.That(ReadDispatchSearchSlotIndex(midMover), Is.EqualTo(midSlotIndex));
                Assert.That(ReadDispatchSearchSlotIndex(farMover), Is.EqualTo(farSlotIndex));
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
            float dispatchReplacementDistanceThresholdMeters)
        {
            SetField(coordinator, "_maxActiveDispatchCount", maxActiveDispatchCount);
            SetField(coordinator, "_dispatchReassignmentHoldSeconds", dispatchReassignmentHoldSeconds);
            SetField(coordinator, "_dispatchReplacementDistanceThresholdMeters", dispatchReplacementDistanceThresholdMeters);
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
