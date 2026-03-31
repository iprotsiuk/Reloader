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
    public sealed class PoliceResponderMoverPlayModeTests
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
        public IEnumerator DuringIdentifiedActivePursuit_MovesTowardPlayer()
        {
            var moverType = ResolveMoverType();

            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject policeRoot = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();

                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 10f));

                policeRoot = new GameObject("PoliceResponder");
                policeRoot.transform.position = new Vector3(0f, 1f, 0f);

                yield return null;

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);
                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));
                Assert.That(provider.CurrentHeatState.IsPlayerIdentified, Is.True);

                var mover = policeRoot.AddComponent(moverType);
                ConfigureMoverForTests(mover, moveSpeedMetersPerSecond: 8f, searchRadiusMeters: 1.5f, searchOrbitDegreesPerSecond: 180f);

                var initialDistance = Vector3.Distance(policeRoot.transform.position, playerRoot.transform.position);

                yield return new WaitForSecondsRealtime(0.6f);

                var finalDistance = Vector3.Distance(policeRoot.transform.position, playerRoot.transform.position);
                Assert.That(finalDistance, Is.LessThan(initialDistance - 1f),
                    "Expected the responder motor to close distance to the player during identified active pursuit.");
                Assert.That(policeRoot.transform.position.z, Is.GreaterThan(1f));
            }
            finally
            {
                DestroyImmediateIfNeeded(policeRoot);
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        [UnityTest]
        public IEnumerator DuringSearch_MovesTowardLastKnownPlayerPositionThenSearchesAroundIt()
        {
            var moverType = ResolveMoverType();

            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject policeRoot = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();

                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 3f));

                policeRoot = new GameObject("PoliceResponder");
                policeRoot.transform.position = new Vector3(0f, 1f, 0f);

                yield return null;

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);

                var mover = policeRoot.AddComponent(moverType);
                ConfigureMoverForTests(mover, moveSpeedMetersPerSecond: 6f, searchRadiusMeters: 1f, searchOrbitDegreesPerSecond: 180f);

                yield return null;

                var lastKnownPlayerPosition = playerRoot.transform.position;
                ForceSearchState(provider);
                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
                playerRoot.transform.position = new Vector3(12f, 1f, 12f);

                yield return new WaitForSecondsRealtime(0.2f);

                var approachPosition = policeRoot.transform.position;
                var distanceToLastKnownDuringApproach = Vector3.Distance(approachPosition, lastKnownPlayerPosition);
                var distanceToCurrentPlayerDuringApproach = Vector3.Distance(approachPosition, playerRoot.transform.position);
                Assert.That(approachPosition.z, Is.GreaterThan(0.8f),
                    "Expected the responder to first move toward the last known player position while entering search.");
                Assert.That(distanceToLastKnownDuringApproach, Is.LessThan(distanceToCurrentPlayerDuringApproach),
                    "Expected early search movement to stay biased toward the cached last known player position instead of chasing the relocated player.");

                yield return new WaitForSecondsRealtime(1.2f);

                var searchPosition = policeRoot.transform.position;
                Assert.That(Vector3.Distance(searchPosition, lastKnownPlayerPosition), Is.LessThan(Vector3.Distance(searchPosition, playerRoot.transform.position)),
                    "Expected search behavior to remain clustered around the cached last known player position instead of switching to the relocated player.");
                Assert.That(Mathf.Abs(searchPosition.x), Is.GreaterThan(0.1f),
                    "Expected the responder to begin deterministic search motion around the last known position.");
            }
            finally
            {
                DestroyImmediateIfNeeded(policeRoot);
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        [UnityTest]
        public IEnumerator DuringSearch_WithDistinctDispatchSlots_SpreadsRespondersAcrossDifferentOrbitSides()
        {
            var moverType = ResolveMoverType();

            GameObject leftPoliceRoot = null;
            GameObject rightPoliceRoot = null;

            try
            {
                var searchCenter = new Vector3(0f, 1f, 3f);

                leftPoliceRoot = new GameObject("PoliceResponderLeft");
                leftPoliceRoot.transform.position = new Vector3(-2f, 1f, 0f);

                rightPoliceRoot = new GameObject("PoliceResponderRight");
                rightPoliceRoot.transform.position = new Vector3(2f, 1f, 0f);

                var leftMover = leftPoliceRoot.AddComponent(moverType);
                var rightMover = rightPoliceRoot.AddComponent(moverType);
                ConfigureMoverForTests(leftMover, moveSpeedMetersPerSecond: 6f, searchRadiusMeters: 1.25f, searchOrbitDegreesPerSecond: 0f);
                ConfigureMoverForTests(rightMover, moveSpeedMetersPerSecond: 6f, searchRadiusMeters: 1.25f, searchOrbitDegreesPerSecond: 0f);
                InvokeVoid(leftMover, "ConfigureDispatchSearchSlot", 0, 2);
                InvokeVoid(rightMover, "ConfigureDispatchSearchSlot", 1, 2);
                ConfigureSearchStateForTests(leftMover, searchCenter);
                ConfigureSearchStateForTests(rightMover, searchCenter);

                yield return new WaitForSecondsRealtime(1.1f);

                Assert.That(leftPoliceRoot.transform.position.x, Is.GreaterThan(0.35f),
                    "Expected slot 0 to settle on the positive-x side of the shared search orbit.");
                Assert.That(rightPoliceRoot.transform.position.x, Is.LessThan(-0.35f),
                    "Expected slot 1 to settle on the negative-x side of the shared search orbit.");
                Assert.That(Vector3.Distance(leftPoliceRoot.transform.position, searchCenter), Is.LessThan(2.5f));
                Assert.That(Vector3.Distance(rightPoliceRoot.transform.position, searchCenter), Is.LessThan(2.5f));
            }
            finally
            {
                DestroyImmediateIfNeeded(rightPoliceRoot);
                DestroyImmediateIfNeeded(leftPoliceRoot);
            }
        }

        [UnityTest]
        public IEnumerator OnEnable_AfterHeatRestore_SamplesCurrentStateWithoutWaitingForNewHeatEvent()
        {
            var moverType = ResolveMoverType();

            GameObject providerGo = null;
            GameObject playerRoot = null;
            GameObject policeRoot = null;

            try
            {
                providerGo = new GameObject("ContractProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                playerRoot = CreatePlayerRoot(new Vector3(0f, 1f, 8f));

                yield return null;

                Assert.That(provider.TryHandleDialogueAction("police.stop.leave", string.Empty), Is.True);
                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit));

                RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
                yield return null;

                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.ActivePursuit),
                    "Expected the contract provider to preserve restored police heat across runtime hub rebuild.");

                policeRoot = new GameObject("PoliceResponder");
                policeRoot.transform.position = new Vector3(0f, 1f, 0f);

                var mover = policeRoot.AddComponent(moverType);
                ConfigureMoverForTests(mover, moveSpeedMetersPerSecond: 8f, searchRadiusMeters: 1.5f, searchOrbitDegreesPerSecond: 180f);

                var initialDistance = Vector3.Distance(policeRoot.transform.position, playerRoot.transform.position);

                yield return new WaitForSecondsRealtime(0.6f);

                var finalDistance = Vector3.Distance(policeRoot.transform.position, playerRoot.transform.position);
                Assert.That(finalDistance, Is.LessThan(initialDistance - 1f),
                    "Expected a responder enabled after heat restore to sample current police heat immediately without waiting for another event.");
            }
            finally
            {
                DestroyImmediateIfNeeded(policeRoot);
                DestroyImmediateIfNeeded(playerRoot);
                DestroyImmediateIfNeeded(providerGo);
            }
        }

        private static Type ResolveMoverType()
        {
            var moverType = Type.GetType("Reloader.NPCs.Combat.PoliceResponderMover, Reloader.NPCs", throwOnError: false);
            Assert.That(moverType, Is.Not.Null, "Expected a PoliceResponderMover runtime component.");
            return moverType!;
        }

        private static void ConfigureMoverForTests(object mover, float moveSpeedMetersPerSecond, float searchRadiusMeters, float searchOrbitDegreesPerSecond)
        {
            SetField(mover, "_moveSpeedMetersPerSecond", moveSpeedMetersPerSecond);
            SetField(mover, "_searchRadiusMeters", searchRadiusMeters);
            SetField(mover, "_searchOrbitDegreesPerSecond", searchOrbitDegreesPerSecond);
        }

        private static void ConfigureSearchStateForTests(object mover, Vector3 lastKnownPlayerPosition)
        {
            SetField(mover, "_currentHeatState", new PoliceHeatState(PoliceHeatLevel.Search, CrimeType.Murder, 8f, false, 1, true));
            SetField(mover, "_lastKnownPlayerPosition", lastKnownPlayerPosition);
            SetField(mover, "_hasLastKnownPlayerPosition", true);
        }

        private static void InvokeVoid(object instance, string methodName, params object[] arguments)
        {
            var method = instance.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected {instance.GetType().Name}.{methodName} to exist.");
            method!.Invoke(instance, arguments);
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

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {instance.GetType().Name}.");
            field!.SetValue(instance, value);
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
