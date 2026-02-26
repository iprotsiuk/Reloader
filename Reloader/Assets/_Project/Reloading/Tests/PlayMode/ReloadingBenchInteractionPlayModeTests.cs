using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Player;
using Reloader.Reloading.World;
using UnityEngine;

namespace Reloader.Reloading.Tests.PlayMode
{
    public class ReloadingBenchInteractionPlayModeTests
    {
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

            GameEvents.OnWorkbenchMenuVisibilityChanged += HandleVisibilityChanged;
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
                GameEvents.OnWorkbenchMenuVisibilityChanged -= HandleVisibilityChanged;
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
    }
}
