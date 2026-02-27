using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Player.Interaction;
using UnityEngine;

namespace Reloader.Player.Tests.PlayMode
{
    public class PlayerInteractionCoordinatorPlayModeTests
    {
        [Test]
        public void Tick_ArbitratesByPriorityThenStableTieBreaker()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            var root = new GameObject("InteractionCoordinatorRoot");
            var coordinator = root.AddComponent<PlayerInteractionCoordinator>();
            var input = root.AddComponent<TestInputSource>();
            var providerA = root.AddComponent<TestCandidateProvider>();
            var providerB = root.AddComponent<TestCandidateProvider>();

            providerA.Candidate = new PlayerInteractionCandidate("pickup", "Pick up", "Ammo", 10, "zzz", PlayerInteractionActionKind.Pickup, null);
            providerA.HasCandidate = true;
            providerB.Candidate = new PlayerInteractionCandidate("vendor", "Trade", "Vendor", 10, "aaa", PlayerInteractionActionKind.VendorTrade, null);
            providerB.HasCandidate = true;

            try
            {
                ConfigureCoordinator(coordinator, input, new MonoBehaviour[] { providerA, providerB }, enabled: true);
                coordinator.Tick();

                Assert.That(runtimeHub.HasInteractionHint, Is.True);
                Assert.That(runtimeHub.CurrentInteractionHint.ContextId, Is.EqualTo("vendor"));
                Assert.That(runtimeHub.CurrentInteractionHint.ActionText, Is.EqualTo("Trade"));

                providerA.Candidate = new PlayerInteractionCandidate("pickup", "Pick up", "Ammo", 80, "zzz", PlayerInteractionActionKind.Pickup, null);
                coordinator.Tick();

                Assert.That(runtimeHub.CurrentInteractionHint.ContextId, Is.EqualTo("pickup"));
                Assert.That(runtimeHub.CurrentInteractionHint.ActionText, Is.EqualTo("Pick up"));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Tick_ConsumesPickupOnce_AndExecutesWinnerOnly()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            var root = new GameObject("InteractionCoordinatorRoot");
            var coordinator = root.AddComponent<PlayerInteractionCoordinator>();
            var input = root.AddComponent<TestInputSource>();
            var winner = root.AddComponent<TestCandidateProvider>();
            var loser = root.AddComponent<TestCandidateProvider>();

            winner.HasCandidate = true;
            loser.HasCandidate = true;

            winner.Candidate = new PlayerInteractionCandidate(
                "bench",
                "Use bench",
                string.Empty,
                100,
                "bench",
                PlayerInteractionActionKind.Workbench,
                () => winner.ExecuteCount++);

            loser.Candidate = new PlayerInteractionCandidate(
                "pickup",
                "Pick up",
                "Item",
                10,
                "pickup",
                PlayerInteractionActionKind.Pickup,
                () => loser.ExecuteCount++);

            input.PickupPressed = true;

            try
            {
                ConfigureCoordinator(coordinator, input, new MonoBehaviour[] { loser, winner }, enabled: true);
                coordinator.Tick();

                Assert.That(input.PickupConsumeCount, Is.EqualTo(1));
                Assert.That(winner.ExecuteCount, Is.EqualTo(1));
                Assert.That(loser.ExecuteCount, Is.EqualTo(0));

                coordinator.Tick();
                Assert.That(input.PickupConsumeCount, Is.EqualTo(2));
                Assert.That(winner.ExecuteCount, Is.EqualTo(1));
                Assert.That(loser.ExecuteCount, Is.EqualTo(0));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Tick_PublishesHintAndClearsWhenNoCandidate_RemainsWithDiagnostics()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            var root = new GameObject("InteractionCoordinatorRoot");
            var coordinator = root.AddComponent<PlayerInteractionCoordinator>();
            var input = root.AddComponent<TestInputSource>();
            var provider = root.AddComponent<TestCandidateProvider>();

            provider.HasCandidate = true;
            provider.Candidate = new PlayerInteractionCandidate("pickup", "Pick up", "Ammo", 10, "pickup", PlayerInteractionActionKind.Pickup, null);

            try
            {
                ConfigureCoordinator(coordinator, input, new MonoBehaviour[] { provider }, enabled: true);
                coordinator.Tick();

                Assert.That(runtimeHub.HasInteractionHint, Is.True);
                Assert.That(runtimeHub.CurrentInteractionHint.ContextId, Is.EqualTo("pickup"));
                StringAssert.Contains("hasWinner=True", coordinator.CaptureDebugSnapshot());

                provider.HasCandidate = false;
                coordinator.Tick();

                Assert.That(runtimeHub.HasInteractionHint, Is.False);
                StringAssert.Contains("hasWinner=False", coordinator.CaptureDebugSnapshot());
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureCoordinator(PlayerInteractionCoordinator coordinator, TestInputSource input, IReadOnlyList<MonoBehaviour> providers, bool enabled)
        {
            SetPrivateField(coordinator, "_coordinatorModeEnabled", enabled);
            SetPrivateField(coordinator, "_inputSourceBehaviour", input as MonoBehaviour);
            SetPrivateField(coordinator, "_providerBehaviours", new List<MonoBehaviour>(providers));
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {instance.GetType().Name}");
            field.SetValue(instance, value);
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool PickupPressed;
            public int PickupConsumeCount;

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;

            public bool ConsumePickupPressed()
            {
                PickupConsumeCount++;
                if (!PickupPressed)
                {
                    return false;
                }

                PickupPressed = false;
                return true;
            }
        }

        private sealed class TestCandidateProvider : MonoBehaviour, IPlayerInteractionCandidateProvider
        {
            public bool HasCandidate;
            public PlayerInteractionCandidate Candidate;
            public int ExecuteCount;

            public bool TryGetInteractionCandidate(out PlayerInteractionCandidate candidate)
            {
                candidate = Candidate;
                return HasCandidate;
            }
        }
    }
}
