using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Player;
using Reloader.Reloading.Runtime;
using Reloader.Reloading.World;
using UnityEngine;

namespace Reloader.Reloading.Tests.PlayMode
{
    public class ReloadingBenchInteractionPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            ReloadingWorkbenchUiContextStore.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ReloadingWorkbenchUiContextStore.Clear();
        }

        [Test]
        public void PickupPressOnBench_OpensWorkbench()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerReloadingBenchController>();

            var cameraGo = new GameObject("PlayerCamera");
            cameraGo.transform.position = Vector3.zero;
            cameraGo.transform.forward = Vector3.forward;
            var camera = cameraGo.AddComponent<Camera>();

            var resolver = root.AddComponent<PlayerReloadingBenchResolver>();
            resolver.SetCameraForTests(camera);
            controller.Configure(input, resolver);

            var benchGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            benchGo.transform.position = new Vector3(0f, 0f, 2f);
            var benchTarget = benchGo.AddComponent<TestBenchTarget>();

            input.PickupPressedThisFrame = true;
            controller.Tick();

            Assert.That(benchTarget.OpenCalls, Is.EqualTo(1));

            Object.Destroy(root);
            Object.Destroy(cameraGo);
            Object.Destroy(benchGo);
        }

        [Test]
        public void OpenWorkbench_WhenTargetLost_ClosesWorkbench()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerReloadingBenchController>();
            var resolver = root.AddComponent<TestBenchResolver>();
            var target = root.AddComponent<TestBenchTarget>();
            resolver.Target = target;
            controller.Configure(input, resolver);

            input.PickupPressedThisFrame = true;
            controller.Tick();
            Assert.That(target.OpenCalls, Is.EqualTo(1));
            Assert.That(target.IsWorkbenchOpen, Is.True);

            resolver.Target = null;
            controller.Tick();

            Assert.That(target.CloseCalls, Is.EqualTo(1));
            Assert.That(target.IsWorkbenchOpen, Is.False);

            Object.Destroy(root);
        }

        [Test]
        public void OpenWorkbench_WhenControllerDisabled_ClosesWorkbenchAndRaisesVisibilityFalse()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerReloadingBenchController>();
            var resolver = root.AddComponent<TestBenchResolver>();
            var target = root.AddComponent<TestBenchTarget>();
            resolver.Target = target;
            controller.Configure(input, resolver);

            var visibilityEvents = 0;
            var lastVisibility = false;
            void HandleVisibilityChanged(bool isVisible)
            {
                visibilityEvents++;
                lastVisibility = isVisible;
            }

            runtimeHub.OnWorkbenchMenuVisibilityChanged += HandleVisibilityChanged;
            try
            {
                input.PickupPressedThisFrame = true;
                controller.Tick();
                Assert.That(target.IsWorkbenchOpen, Is.True);
                Assert.That(lastVisibility, Is.True);

                controller.enabled = false;

                Assert.That(target.CloseCalls, Is.EqualTo(1));
                Assert.That(target.IsWorkbenchOpen, Is.False);
                Assert.That(visibilityEvents, Is.EqualTo(2));
                Assert.That(lastVisibility, Is.False);
            }
            finally
            {
                runtimeHub.OnWorkbenchMenuVisibilityChanged -= HandleVisibilityChanged;
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.Destroy(root);
            }
        }

        [Test]
        public void PickupPressWithoutBenchTarget_ConsumesInputAndDoesNotAutoOpenWhenBenchAppearsLater()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerReloadingBenchController>();
            var resolver = root.AddComponent<TestBenchResolver>();
            var target = root.AddComponent<TestBenchTarget>();
            controller.Configure(input, resolver);

            input.PickupPressedThisFrame = true;
            resolver.Target = null;
            controller.Tick();
            controller.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

            resolver.Target = target;
            controller.Tick();
            Assert.That(target.OpenCalls, Is.EqualTo(0));

            input.PickupPressedThisFrame = true;
            controller.Tick();
            Assert.That(target.OpenCalls, Is.EqualTo(1));

            Object.Destroy(root);
        }

        [Test]
        public void PlayerReloadingBenchController_UsesInjectedUiStateEvents_InsteadOfRuntimeKernelUiStateEvents()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            var root = new GameObject("PlayerRootInjectedUiEvents");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerReloadingBenchController>();
            var resolver = root.AddComponent<TestBenchResolver>();
            var target = root.AddComponent<TestBenchTarget>();
            var uiStateEvents = new TestUiStateEvents();
            resolver.Target = target;
            controller.Configure(input, resolver, uiStateEvents);

            var runtimeHubEventsCount = 0;
            runtimeHub.OnWorkbenchMenuVisibilityChanged += HandleRuntimeHubVisibilityChanged;
            void HandleRuntimeHubVisibilityChanged(bool _) => runtimeHubEventsCount++;

            try
            {
                input.PickupPressedThisFrame = true;
                controller.Tick();
                Assert.That(uiStateEvents.WorkbenchVisibilityRaiseCount, Is.EqualTo(1));
                Assert.That(uiStateEvents.IsWorkbenchMenuVisible, Is.True);
                Assert.That(runtimeHubEventsCount, Is.EqualTo(0));

                controller.enabled = false;
                Assert.That(uiStateEvents.WorkbenchVisibilityRaiseCount, Is.EqualTo(2));
                Assert.That(uiStateEvents.IsWorkbenchMenuVisible, Is.False);
            }
            finally
            {
                runtimeHub.OnWorkbenchMenuVisibilityChanged -= HandleRuntimeHubVisibilityChanged;
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.Destroy(root);
            }
        }

        [Test]
        public void Tick_TargetedBench_EmitsUseBenchHint()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerReloadingBenchController>();
            var resolver = root.AddComponent<TestBenchResolver>();
            var target = root.AddComponent<TestBenchTarget>();
            resolver.Target = target;
            controller.Configure(input, resolver);

            InteractionHintPayload hinted = default;
            runtimeHub.OnInteractionHintShown += payload => hinted = payload;

            try
            {
                controller.Tick();
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.Destroy(root);
            }

            Assert.That(hinted.ContextId, Is.EqualTo("bench"));
            Assert.That(hinted.ActionText, Is.EqualTo("Use bench"));
        }

        [Test]
        public void Tick_NoBenchTargetAfterHint_ClearsHint()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerReloadingBenchController>();
            var resolver = root.AddComponent<TestBenchResolver>();
            var target = root.AddComponent<TestBenchTarget>();
            resolver.Target = target;
            controller.Configure(input, resolver);

            var clearCount = 0;
            runtimeHub.OnInteractionHintCleared += () => clearCount++;

            try
            {
                controller.Tick();
                resolver.Target = null;
                controller.Tick();
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.Destroy(root);
            }

            Assert.That(clearCount, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void Tick_OpenWorkbench_PublishesUiSnapshot()
        {
            var root = new GameObject("PlayerRootSnapshotOpen");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerReloadingBenchController>();
            var resolver = root.AddComponent<TestBenchResolver>();

            var targetGo = new GameObject("BenchTargetSnapshotOpen");
            var target = targetGo.AddComponent<ReloadingBenchTarget>();
            target.SetWorkbenchDefinitionForTests(CreateWorkbenchDefinition("bench.snapshot.open", new MountSlotDefinition("press-slot"), new MountSlotDefinition("die-slot")));

            resolver.Target = target;
            controller.Configure(input, resolver);

            input.PickupPressedThisFrame = true;
            controller.Tick();

            Assert.That(ReloadingWorkbenchUiContextStore.TryGetLatest(out var snapshot), Is.True);
            Assert.That(snapshot.SetupSlots.Length, Is.EqualTo(2));
            Assert.That(snapshot.SetupSlots[0], Is.EqualTo("die-slot: missing"));
            Assert.That(snapshot.SetupSlots[1], Is.EqualTo("press-slot: missing"));
            Assert.That(snapshot.OperationStatuses.Length, Is.EqualTo(3));
            Assert.That(snapshot.OperationStatuses[0].IsEnabled, Is.False);
            Assert.That(snapshot.OperationStatuses[1].IsEnabled, Is.False);
            Assert.That(snapshot.OperationStatuses[2].IsEnabled, Is.False);

            Object.Destroy(root);
            Object.Destroy(targetGo);
        }

        [Test]
        public void Tick_TargetLostAfterOpen_ClearsUiSnapshot()
        {
            var root = new GameObject("PlayerRootSnapshotLost");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerReloadingBenchController>();
            var resolver = root.AddComponent<TestBenchResolver>();

            var targetGo = new GameObject("BenchTargetSnapshotLost");
            var target = targetGo.AddComponent<ReloadingBenchTarget>();
            target.SetWorkbenchDefinitionForTests(CreateWorkbenchDefinition("bench.snapshot.lost", new MountSlotDefinition("press-slot")));

            resolver.Target = target;
            controller.Configure(input, resolver);

            input.PickupPressedThisFrame = true;
            controller.Tick();
            Assert.That(ReloadingWorkbenchUiContextStore.TryGetLatest(out _), Is.True);

            resolver.Target = null;
            controller.Tick();

            Assert.That(ReloadingWorkbenchUiContextStore.TryGetLatest(out _), Is.False);

            Object.Destroy(root);
            Object.Destroy(targetGo);
        }

        [Test]
        public void Tick_WhenSwitchingActiveBench_ReplacesUiSnapshot()
        {
            var root = new GameObject("PlayerRootSnapshotSwitch");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerReloadingBenchController>();
            var resolver = root.AddComponent<TestBenchResolver>();

            var benchOneGo = new GameObject("BenchOne");
            var benchOne = benchOneGo.AddComponent<ReloadingBenchTarget>();
            benchOne.SetWorkbenchDefinitionForTests(CreateWorkbenchDefinition("bench.one", new MountSlotDefinition("slot-a")));

            var benchTwoGo = new GameObject("BenchTwo");
            var benchTwo = benchTwoGo.AddComponent<ReloadingBenchTarget>();
            benchTwo.SetWorkbenchDefinitionForTests(CreateWorkbenchDefinition("bench.two", new MountSlotDefinition("slot-b"), new MountSlotDefinition("slot-c")));

            resolver.Target = benchOne;
            controller.Configure(input, resolver);

            input.PickupPressedThisFrame = true;
            controller.Tick();
            Assert.That(ReloadingWorkbenchUiContextStore.TryGetLatest(out var firstSnapshot), Is.True);
            Assert.That(firstSnapshot.SetupSlots.Length, Is.EqualTo(1));
            Assert.That(firstSnapshot.SetupSlots[0], Is.EqualTo("slot-a: missing"));

            resolver.Target = benchTwo;
            input.PickupPressedThisFrame = true;
            controller.Tick();

            Assert.That(ReloadingWorkbenchUiContextStore.TryGetLatest(out var secondSnapshot), Is.True);
            Assert.That(secondSnapshot.SetupSlots.Length, Is.EqualTo(2));
            Assert.That(secondSnapshot.SetupSlots[0], Is.EqualTo("slot-b: missing"));
            Assert.That(secondSnapshot.SetupSlots[1], Is.EqualTo("slot-c: missing"));

            Object.Destroy(root);
            Object.Destroy(benchOneGo);
            Object.Destroy(benchTwoGo);
        }

        private static WorkbenchDefinition CreateWorkbenchDefinition(string workbenchId, params MountSlotDefinition[] slots)
        {
            var definition = ScriptableObject.CreateInstance<WorkbenchDefinition>();
            definition.SetValuesForTests(workbenchId, slots);
            return definition;
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool PickupPressedThisFrame;

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;

            public bool ConsumePickupPressed()
            {
                if (!PickupPressedThisFrame)
                {
                    return false;
                }

                PickupPressedThisFrame = false;
                return true;
            }
        }

        private sealed class TestBenchTarget : MonoBehaviour, IReloadingBenchTarget
        {
            public int OpenCalls { get; private set; }
            public int CloseCalls { get; private set; }
            public bool IsWorkbenchOpen { get; private set; }

            public void OpenWorkbench()
            {
                OpenCalls++;
                IsWorkbenchOpen = true;
            }

            public void CloseWorkbench()
            {
                CloseCalls++;
                IsWorkbenchOpen = false;
            }
        }

        private sealed class TestBenchResolver : MonoBehaviour, IPlayerReloadingBenchResolver
        {
            public IReloadingBenchTarget Target { get; set; }

            public bool TryResolveBenchTarget(out IReloadingBenchTarget target)
            {
                target = Target;
                return target != null;
            }
        }

        private sealed class TestUiStateEvents : IUiStateEvents
        {
            public bool IsShopTradeMenuOpen => false;
            public bool IsWorkbenchMenuVisible { get; private set; }
            public bool IsTabInventoryVisible => false;
            public bool IsAnyMenuOpen => IsWorkbenchMenuVisible;
            public int WorkbenchVisibilityRaiseCount { get; private set; }

            public event System.Action<bool> OnWorkbenchMenuVisibilityChanged;
            public event System.Action<bool> OnTabInventoryVisibilityChanged;

            public void RaiseWorkbenchMenuVisibilityChanged(bool isVisible)
            {
                IsWorkbenchMenuVisible = isVisible;
                WorkbenchVisibilityRaiseCount++;
                OnWorkbenchMenuVisibilityChanged?.Invoke(isVisible);
            }

            public void RaiseTabInventoryVisibilityChanged(bool isVisible)
            {
                OnTabInventoryVisibilityChanged?.Invoke(isVisible);
            }
        }
    }
}
