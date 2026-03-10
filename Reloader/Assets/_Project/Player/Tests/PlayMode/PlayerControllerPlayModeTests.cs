using NUnit.Framework;
using Reloader.Player;
using Reloader.Player.Viewmodel;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using System.Reflection;
using UnityEngine;

namespace Reloader.Player.Tests.PlayMode
{
    public class PlayerControllerPlayModeTests
    {
        [Test]
        public void PlayerMover_Tick_MovesRelativeToYaw()
        {
            var root = new GameObject("PlayerRoot");
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.3f;

            var input = root.AddComponent<TestInputSource>();
            input.Move = Vector2.up;

            var settings = new PlayerMovementSettings
            {
                WalkSpeed = 6f,
                SprintSpeed = 9f,
                Acceleration = 100f,
                Gravity = -25f,
                JumpHeight = 1.25f
            };

            var mover = root.AddComponent<PlayerMover>();
            mover.Configure(input, settings);

            root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            mover.Tick(0.1f);

            Assert.That(root.transform.position.x, Is.GreaterThan(0.2f));
            Assert.That(Mathf.Abs(root.transform.position.z), Is.LessThan(0.1f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void PlayerMover_Tick_JumpSetsPositiveVerticalVelocityWhenGrounded()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);

            var root = new GameObject("PlayerRoot");
            root.transform.position = new Vector3(0f, 0.05f, 0f);
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 1f, 0f);

            var input = root.AddComponent<TestInputSource>();
            var mover = root.AddComponent<PlayerMover>();
            mover.Configure(input, new PlayerMovementSettings());

            mover.Tick(0.02f);
            input.JumpPressedThisFrame = true;
            mover.Tick(0.02f);

            Assert.That(mover.VerticalVelocity, Is.GreaterThan(0f));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(ground);
        }

        [Test]
        public void PlayerMover_Tick_JumpBuffer_TriggersJumpWhenGroundedNextFrame()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);

            var root = new GameObject("PlayerRoot");
            root.transform.position = new Vector3(0f, 0.4f, 0f);
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 1f, 0f);

            var input = root.AddComponent<TestInputSource>();
            var settings = new PlayerMovementSettings
            {
                JumpBufferTime = 0.2f,
                Gravity = -25f,
                JumpHeight = 1.4f
            };

            var mover = root.AddComponent<PlayerMover>();
            mover.Configure(input, settings);

            input.JumpPressedThisFrame = true;
            mover.Tick(0.02f); // likely still becoming grounded
            input.JumpPressedThisFrame = false;
            mover.Tick(0.02f); // should consume buffered jump once grounded

            Assert.That(mover.VerticalVelocity, Is.GreaterThan(0f));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(ground);
        }

        [Test]
        public void PlayerLookController_Tick_AppliesYawAndClampsPitch()
        {
            var root = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot");
            cameraPivot.transform.SetParent(root.transform);

            var input = root.AddComponent<TestInputSource>();
            input.Look = new Vector2(4f, -100f);

            var look = root.AddComponent<PlayerLookController>();
            look.Configure(input, cameraPivot.transform);
            look.PitchClamp = new Vector2(-85f, 85f);
            look.LookSensitivity = new Vector2(1f, 1f);
            look.Tick(1f);

            Assert.That(root.transform.eulerAngles.y, Is.EqualTo(4f).Within(0.05f));
            Assert.That(Mathf.DeltaAngle(0f, cameraPivot.transform.localEulerAngles.x), Is.EqualTo(85f).Within(0.05f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void PlayerLookController_Tick_MenuOpen_DoesNotRotateCamera()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            GameObject root = null;

            try
            {
                RuntimeKernelBootstrapper.Events = runtimeEvents;

                root = new GameObject("PlayerRoot");
                var cameraPivot = new GameObject("CameraPivot");
                cameraPivot.transform.SetParent(root.transform);

                var input = root.AddComponent<TestInputSource>();
                input.Look = new Vector2(12f, -8f);

                var look = root.AddComponent<PlayerLookController>();
                look.Configure(input, cameraPivot.transform);
                look.LookSensitivity = Vector2.one;
                look.Tick(1f);
                var yawAfterFirstTick = root.transform.eulerAngles.y;

                runtimeEvents.RaiseTabInventoryVisibilityChanged(true);
                look.Tick(1f);
                var yawAfterMenuOpenTick = root.transform.eulerAngles.y;

                Assert.That(yawAfterMenuOpenTick, Is.EqualTo(yawAfterFirstTick).Within(0.001f));
            }
            finally
            {
                runtimeEvents.RaiseTabInventoryVisibilityChanged(false);
                RuntimeKernelBootstrapper.Events = originalHub;
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void PlayerLookController_Tick_EscMenuOpen_DoesNotRotateCamera()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            GameObject root = null;

            try
            {
                RuntimeKernelBootstrapper.Events = runtimeEvents;

                root = new GameObject("PlayerRoot");
                var cameraPivot = new GameObject("CameraPivot");
                cameraPivot.transform.SetParent(root.transform);

                var input = root.AddComponent<TestInputSource>();
                input.Look = new Vector2(12f, -8f);

                var look = root.AddComponent<PlayerLookController>();
                look.Configure(input, cameraPivot.transform);
                look.LookSensitivity = Vector2.one;
                look.Tick(1f);
                var yawAfterFirstTick = root.transform.eulerAngles.y;

                runtimeEvents.RaiseEscMenuVisibilityChanged(true);
                look.Tick(1f);
                var yawAfterMenuOpenTick = root.transform.eulerAngles.y;

                Assert.That(yawAfterMenuOpenTick, Is.EqualTo(yawAfterFirstTick).Within(0.001f));
            }
            finally
            {
                runtimeEvents.RaiseEscMenuVisibilityChanged(false);
                RuntimeKernelBootstrapper.Events = originalHub;
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void PlayerLookController_Tick_UsesInjectedUiStateEvents_InsteadOfRuntimeKernelUiStateEvents()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeUiStateEvents = new DefaultRuntimeEvents();
            GameObject root = null;

            try
            {
                RuntimeKernelBootstrapper.Events = runtimeUiStateEvents;

                root = new GameObject("PlayerRoot");
                var cameraPivot = new GameObject("CameraPivot");
                cameraPivot.transform.SetParent(root.transform);

                var input = root.AddComponent<TestInputSource>();
                input.Look = new Vector2(12f, -8f);

                var uiStateEvents = new TestUiStateEvents();
                var look = root.AddComponent<PlayerLookController>();
                look.Configure(input, cameraPivot.transform, uiStateEvents);
                look.LookSensitivity = Vector2.one;
                look.Tick(1f);
                var yawAfterFirstTick = root.transform.eulerAngles.y;

                runtimeUiStateEvents.RaiseTabInventoryVisibilityChanged(true);
                look.Tick(1f);
                var yawAfterRuntimeKernelOpenTick = root.transform.eulerAngles.y;

                uiStateEvents.RaiseTabInventoryVisibilityChanged(true);
                look.Tick(1f);
                var yawAfterInjectedUiStateOpenTick = root.transform.eulerAngles.y;

                Assert.That(yawAfterRuntimeKernelOpenTick, Is.GreaterThan(yawAfterFirstTick + 0.01f));
                Assert.That(yawAfterInjectedUiStateOpenTick, Is.EqualTo(yawAfterRuntimeKernelOpenTick).Within(0.001f));
            }
            finally
            {
                runtimeUiStateEvents.RaiseTabInventoryVisibilityChanged(false);
                RuntimeKernelBootstrapper.Events = originalHub;
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void PlayerLookController_WithoutInjectedUiStateEvents_RebindsWhenRuntimeKernelHubIsReplaced()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            GameObject root = null;

            try
            {
                RuntimeKernelBootstrapper.Events = initialHub;

                root = new GameObject("PlayerRoot");
                var cameraPivot = new GameObject("CameraPivot");
                cameraPivot.transform.SetParent(root.transform);

                var input = root.AddComponent<TestInputSource>();
                input.Look = new Vector2(12f, 0f);

                var look = root.AddComponent<PlayerLookController>();
                look.Configure(input, cameraPivot.transform);
                look.LookSensitivity = Vector2.one;
                look.Tick(1f);
                var yawAfterFirstTick = root.transform.eulerAngles.y;

                initialHub.RaiseTabInventoryVisibilityChanged(true);
                look.Tick(1f);
                var yawAfterInitialHubOpenTick = root.transform.eulerAngles.y;

                initialHub.RaiseTabInventoryVisibilityChanged(false);
                look.Tick(1f);
                var yawAfterInitialHubCloseTick = root.transform.eulerAngles.y;

                RuntimeKernelBootstrapper.Events = replacementHub;

                initialHub.RaiseTabInventoryVisibilityChanged(true);
                look.Tick(1f);
                var yawAfterOldHubOpenPostSwapTick = root.transform.eulerAngles.y;

                replacementHub.RaiseTabInventoryVisibilityChanged(true);
                look.Tick(1f);
                var yawAfterReplacementHubOpenTick = root.transform.eulerAngles.y;

                Assert.That(yawAfterInitialHubOpenTick, Is.EqualTo(yawAfterFirstTick).Within(0.001f));
                Assert.That(yawAfterInitialHubCloseTick, Is.GreaterThan(yawAfterInitialHubOpenTick + 0.01f));
                Assert.That(yawAfterOldHubOpenPostSwapTick, Is.GreaterThan(yawAfterInitialHubCloseTick + 0.01f));
                Assert.That(yawAfterReplacementHubOpenTick, Is.EqualTo(yawAfterOldHubOpenPostSwapTick).Within(0.001f));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void PlayerLookController_Tick_FocusTargetOverride_AppliesWhileInjectedUiStateEventsReportMenuOpen()
        {
            GameObject root = null;

            try
            {
                root = new GameObject("PlayerRoot");
                var cameraPivot = new GameObject("CameraPivot");
                cameraPivot.transform.SetParent(root.transform);
                cameraPivot.transform.localPosition = new Vector3(0f, 1.8f, 0f);

                var focusTarget = new GameObject("FocusTarget");
                focusTarget.transform.position = new Vector3(10f, 1.0f, 0f);
                var desiredDirection = (focusTarget.transform.position - cameraPivot.transform.position).normalized;

                var input = root.AddComponent<TestInputSource>();
                var uiStateEvents = new TestUiStateEvents();
                uiStateEvents.RaiseTabInventoryVisibilityChanged(true);

                var look = root.AddComponent<PlayerLookController>();
                look.Configure(input, cameraPivot.transform, uiStateEvents);
                look.SetFocusTargetOverride(focusTarget.transform);
                look.Tick(0.05f);

                Assert.That(root.transform.eulerAngles.y, Is.GreaterThan(0.1f));
                Assert.That(Mathf.DeltaAngle(0f, cameraPivot.transform.localEulerAngles.x), Is.GreaterThan(0.1f));
                Assert.That(Vector3.Dot(cameraPivot.transform.forward, desiredDirection), Is.GreaterThan(0.1f));

                Object.DestroyImmediate(focusTarget);
            }
            finally
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void PlayerLookController_Tick_Aiming_UsesAdsSensitivityMultiplier()
        {
            var root = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot");
            cameraPivot.transform.SetParent(root.transform);

            var input = root.AddComponent<TestInputSource>();
            input.Look = new Vector2(4f, 0f);

            var look = root.AddComponent<PlayerLookController>();
            look.Configure(input, cameraPivot.transform);
            look.LookSensitivity = Vector2.one;
            look.AdsSensitivityMultiplier = new Vector2(0.25f, 0.25f);

            look.Tick(1f);
            var hipFireYaw = root.transform.eulerAngles.y;

            root.transform.rotation = Quaternion.identity;
            look.Configure(input, cameraPivot.transform);
            input.AimHeldValue = true;
            look.Tick(1f);
            var adsYaw = root.transform.eulerAngles.y;

            Assert.That(hipFireYaw, Is.EqualTo(4f).Within(0.05f));
            Assert.That(adsYaw, Is.EqualTo(1f).Within(0.05f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void PlayerLookController_Tick_WithLookSmoothingEnabled_SpreadsStepAcrossFrames()
        {
            var smoothRoot = new GameObject("SmoothPlayerRoot");
            var smoothCameraPivot = new GameObject("SmoothCameraPivot");
            smoothCameraPivot.transform.SetParent(smoothRoot.transform);
            var smoothInput = smoothRoot.AddComponent<TestInputSource>();

            var smoothLook = smoothRoot.AddComponent<PlayerLookController>();
            smoothLook.Configure(smoothInput, smoothCameraPivot.transform);
            smoothLook.LookSensitivity = Vector2.one;
            smoothLook.LookSmoothingEnabled = true;
            smoothLook.LookSmoothingSpeed = 1f;
            smoothLook.LookSmoothingStrength = 1f;

            smoothInput.Look = new Vector2(4f, 0f);
            smoothLook.Tick(0.1f);
            smoothInput.Look = Vector2.zero;
            smoothLook.Tick(0.1f);
            var yawWithSmoothing = smoothRoot.transform.eulerAngles.y;

            var rawRoot = new GameObject("RawPlayerRoot");
            var rawCameraPivot = new GameObject("RawCameraPivot");
            rawCameraPivot.transform.SetParent(rawRoot.transform);
            var rawInput = rawRoot.AddComponent<TestInputSource>();

            var rawLook = rawRoot.AddComponent<PlayerLookController>();
            rawLook.Configure(rawInput, rawCameraPivot.transform);
            rawLook.LookSensitivity = Vector2.one;
            rawLook.LookSmoothingEnabled = false;

            rawInput.Look = new Vector2(4f, 0f);
            rawLook.Tick(0.1f);
            rawInput.Look = Vector2.zero;
            rawLook.Tick(0.1f);
            var yawWithoutSmoothing = rawRoot.transform.eulerAngles.y;

            Assert.That(yawWithoutSmoothing, Is.EqualTo(4f).Within(0.05f));
            Assert.That(yawWithSmoothing, Is.GreaterThan(yawWithoutSmoothing + 0.05f));

            Object.DestroyImmediate(smoothRoot);
            Object.DestroyImmediate(rawRoot);
        }

        [Test]
        public void PlayerLookController_Tick_LowerMainCameraFov_ReducesYawDeltaForSameInput()
        {
            var root = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot");
            cameraPivot.transform.SetParent(root.transform);
            var camera = root.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 60f;

            var input = root.AddComponent<TestInputSource>();
            input.Look = new Vector2(4f, 0f);

            var look = root.AddComponent<PlayerLookController>();
            look.Configure(input, cameraPivot.transform);
            look.LookSensitivity = Vector2.one;

            look.Tick(1f);
            var hipFireYaw = root.transform.eulerAngles.y;

            root.transform.rotation = Quaternion.identity;
            look.Configure(input, cameraPivot.transform);
            camera.fieldOfView = 30f;
            look.Tick(1f);
            var zoomYaw = root.transform.eulerAngles.y;

            Assert.That(hipFireYaw, Is.EqualTo(4f).Within(0.05f));
            Assert.That(zoomYaw, Is.EqualTo(1.86f).Within(0.08f));
            Assert.That(zoomYaw, Is.LessThan(hipFireYaw));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void PlayerLookController_Tick_PlayerCameraDefaultsFov_OverridesMainCameraFallback()
        {
            var mainCameraRoot = new GameObject("MainCameraRoot");
            var mainCamera = mainCameraRoot.AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            mainCamera.fieldOfView = 60f;

            var root = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot");
            cameraPivot.transform.SetParent(root.transform);

            var defaultsCameraRoot = new GameObject("DefaultsCameraRoot");
            defaultsCameraRoot.transform.SetParent(root.transform);
            var defaultsCamera = defaultsCameraRoot.AddComponent<Camera>();
            defaultsCamera.fieldOfView = 30f;

            var defaults = root.AddComponent<PlayerCameraDefaults>();
            defaults.GetType()
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, defaultsCamera);

            var input = root.AddComponent<TestInputSource>();
            input.Look = new Vector2(4f, 0f);

            var look = root.AddComponent<PlayerLookController>();
            look.Configure(input, cameraPivot.transform);
            look.LookSensitivity = Vector2.one;
            look.Tick(1f);

            Assert.That(root.transform.eulerAngles.y, Is.EqualTo(1.86f).Within(0.08f));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(mainCameraRoot);
        }

        [Test]
        public void PlayerInputReader_EnableDisable_DoesNotThrowWithoutActionAsset()
        {
            var go = new GameObject("InputReader");
            var reader = go.AddComponent<PlayerInputReader>();

            Assert.DoesNotThrow(() =>
            {
                reader.enabled = true;
                reader.enabled = false;
            });

            Object.DestroyImmediate(go);
        }

        [Test]
        public void PlayerCursorLockController_LockCursor_LocksAndHidesCursor()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;
            try
            {
                var go = new GameObject("CursorLock");
                var controller = go.AddComponent<PlayerCursorLockController>();

                controller.LockCursor();

                Assert.That(controller.IsCursorLockRequested, Is.True);
                Object.DestroyImmediate(go);
            }
            finally
            {
                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
        }

        [Test]
        public void PlayerCursorLockController_UnlockCursor_UnlocksAndShowsCursor()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;
            try
            {
                var go = new GameObject("CursorLock");
                var controller = go.AddComponent<PlayerCursorLockController>();
                controller.LockCursor();

                controller.UnlockCursor();

                Assert.That(controller.IsCursorLockRequested, Is.False);
                Object.DestroyImmediate(go);
            }
            finally
            {
                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
        }

        [Test]
        public void PlayerCursorLockController_MenuVisibility_UnlocksWhileOpen_AndRestoresLockOnClose()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeUiStateEvents = new DefaultRuntimeEvents();
            try
            {
                RuntimeKernelBootstrapper.Events = runtimeUiStateEvents;
                var go = new GameObject("CursorLock");
                var controller = go.AddComponent<PlayerCursorLockController>();

                controller.LockCursor();
                Assert.That(controller.IsCursorLockRequested, Is.True);

                runtimeUiStateEvents.RaiseTabInventoryVisibilityChanged(true);
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.True);

                runtimeUiStateEvents.RaiseTabInventoryVisibilityChanged(false);
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.False);
                Assert.That(controller.IsCursorLockRequested, Is.True);

                Object.DestroyImmediate(go);
            }
            finally
            {
                runtimeUiStateEvents.RaiseTabInventoryVisibilityChanged(false);
                RuntimeKernelBootstrapper.Events = originalHub;
                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
        }

        [Test]
        public void PlayerCursorLockController_EscMenuVisibility_UnlocksWhileOpen_AndRestoresLockOnClose()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeUiStateEvents = new DefaultRuntimeEvents();
            try
            {
                RuntimeKernelBootstrapper.Events = runtimeUiStateEvents;
                var go = new GameObject("CursorLockEscMenu");
                var controller = go.AddComponent<PlayerCursorLockController>();

                controller.LockCursor();
                Assert.That(controller.IsCursorLockRequested, Is.True);

                runtimeUiStateEvents.RaiseEscMenuVisibilityChanged(true);
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.True);

                runtimeUiStateEvents.RaiseEscMenuVisibilityChanged(false);
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.False);
                Assert.That(controller.IsCursorLockRequested, Is.True);

                Object.DestroyImmediate(go);
            }
            finally
            {
                runtimeUiStateEvents.RaiseEscMenuVisibilityChanged(false);
                RuntimeKernelBootstrapper.Events = originalHub;
                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
        }

        [Test]
        public void PlayerCursorLockController_UsesInjectedUiAndShopEventChannels()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeUiStateEvents = new DefaultRuntimeEvents();
            var go = new GameObject("CursorLockInjectedEvents");
            go.SetActive(false);

            var controller = go.AddComponent<PlayerCursorLockController>();
            var uiStateEvents = new TestUiStateEvents();
            var shopEvents = new TestShopEvents();
            controller.Configure(uiStateEvents, shopEvents);

            try
            {
                RuntimeKernelBootstrapper.Events = runtimeUiStateEvents;
                go.SetActive(true);
                controller.LockCursor();

                runtimeUiStateEvents.RaiseTabInventoryVisibilityChanged(true);
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.False);

                uiStateEvents.RaiseTabInventoryVisibilityChanged(true);
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.True);

                uiStateEvents.RaiseTabInventoryVisibilityChanged(false);
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.False);

                shopEvents.RaiseShopTradeOpened("vendor-1");
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.True);

                shopEvents.RaiseShopTradeClosed();
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.False);
            }
            finally
            {
                runtimeUiStateEvents.RaiseTabInventoryVisibilityChanged(false);
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(go);
                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
        }

        [Test]
        public void PlayerCursorLockController_WithoutInjectedEvents_RebindsWhenRuntimeKernelHubIsReconfigured()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(System.Array.Empty<RuntimeModuleRegistration>(), initialHub);

            var go = new GameObject("CursorLockRuntimeHubReconfigure");
            var controller = go.AddComponent<PlayerCursorLockController>();

            try
            {
                controller.LockCursor();

                initialHub.RaiseTabInventoryVisibilityChanged(true);
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.True);

                initialHub.RaiseTabInventoryVisibilityChanged(false);
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.False);

                RuntimeKernelBootstrapper.Configure(System.Array.Empty<RuntimeModuleRegistration>(), replacementHub);

                initialHub.RaiseTabInventoryVisibilityChanged(true);
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.False);

                replacementHub.RaiseTabInventoryVisibilityChanged(true);
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.True);
            }
            finally
            {
                replacementHub.RaiseTabInventoryVisibilityChanged(false);
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(go);
                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
        }

        [Test]
        public void PlayerCursorLockController_WithoutInjectedEvents_RebindsWhenRuntimeKernelHubIsReplacedViaSetter()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = initialHub;

            var go = new GameObject("CursorLockRuntimeHubSetterSwap");
            go.SetActive(false);
            var controller = go.AddComponent<PlayerCursorLockController>();
            go.SetActive(true);

            try
            {
                initialHub.RaiseWorkbenchMenuVisibilityChanged(true);
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.True);

                initialHub.RaiseWorkbenchMenuVisibilityChanged(false);
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.False);

                RuntimeKernelBootstrapper.Events = replacementHub;

                initialHub.RaiseWorkbenchMenuVisibilityChanged(true);
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.False);

                replacementHub.RaiseWorkbenchMenuVisibilityChanged(true);
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.True);
            }
            finally
            {
                replacementHub.RaiseWorkbenchMenuVisibilityChanged(false);
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(go);
                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
        }

        [Test]
        public void PlayerCursorLockController_RuntimeHubSwap_ReconcilesMenuFlagsImmediately()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = initialHub;

            var go = new GameObject("CursorLockRuntimeHubSwapStateReconcile");
            var controller = go.AddComponent<PlayerCursorLockController>();

            try
            {
                controller.LockCursor();
                initialHub.RaiseShopTradeOpened("vendor-1");
                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.True);

                RuntimeKernelBootstrapper.Events = replacementHub;

                Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.False);
                Assert.That(controller.IsCursorLockRequested, Is.True);
            }
            finally
            {
                replacementHub.RaiseShopTradeClosed();
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(go);
                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
        }

        [Test]
        public void TestInputSource_ExposesFireAndReloadConsumeMethods()
        {
            var root = new GameObject("InputSourceRoot");
            var input = root.AddComponent<TestInputSource>();
            IPlayerInputSource source = input;

            input.FirePressedThisFrame = true;
            input.ReloadPressedThisFrame = true;

            Assert.That(source.ConsumeFirePressed(), Is.True);
            Assert.That(source.ConsumeReloadPressed(), Is.True);
            Assert.That(source.ConsumeFirePressed(), Is.False);
            Assert.That(source.ConsumeReloadPressed(), Is.False);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TestInputSource_ExposesAimHeldProperty()
        {
            var root = new GameObject("InputSourceRoot");
            var input = root.AddComponent<TestInputSource>();
            IPlayerInputSource source = input;
            input.AimHeldValue = true;

            Assert.That(source.AimHeld, Is.True);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void PlayerMover_Tick_AimHeld_AppliesAdsMultiplier()
        {
            var root = new GameObject("PlayerRoot");
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.3f;

            var input = root.AddComponent<TestInputSource>();
            input.Move = Vector2.up;
            input.AimHeldValue = true;
            var settings = new PlayerMovementSettings
            {
                WalkSpeed = 10f,
                SprintSpeed = 14f,
                Acceleration = 100f,
                Gravity = -25f,
                JumpHeight = 1.25f
            };

            var mover = root.AddComponent<PlayerMover>();
            mover.Configure(input, settings);
            mover.Tick(0.1f);

            // With default ADS multiplier target speed should be below base walk speed.
            Assert.That(root.transform.position.z, Is.GreaterThan(0.4f));
            Assert.That(root.transform.position.z, Is.LessThan(1f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void FpsViewmodelAnimatorDriver_NormalizeSpeed_UsesMaxWalkSprintAndClamps()
        {
            var normalizedMid = FpsViewmodelAnimatorDriver.NormalizeSpeed(3f, 4f, 8f);
            var normalizedHigh = FpsViewmodelAnimatorDriver.NormalizeSpeed(100f, 4f, 8f);
            var normalizedZero = FpsViewmodelAnimatorDriver.NormalizeSpeed(3f, 0f, 0f);

            Assert.That(normalizedMid, Is.EqualTo(0.375f).Within(0.0001f));
            Assert.That(normalizedHigh, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(normalizedZero, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void PlayerCameraDefaults_ApplyDefaults_ForcesBrainAndBlendLateUpdate()
        {
            var root = new GameObject("CameraDefaultsRoot");
            var camera = root.AddComponent<Camera>();
            var defaults = root.AddComponent<PlayerCameraDefaults>();
            var brainField = defaults.GetType()
                .GetField("_brain", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(brainField, Is.Not.Null);
            var brain = root.AddComponent(brainField.FieldType);

            defaults.GetType()
                .GetField("_mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(defaults, camera);
            brainField.SetValue(defaults, brain);

            var brainType = brain.GetType();
            var updateMethodProp = brainType.GetProperty("UpdateMethod");
            var blendUpdateMethodProp = brainType.GetProperty("BlendUpdateMethod");
            var updateMethodField = brainType.GetField("UpdateMethod");
            var blendUpdateMethodField = brainType.GetField("BlendUpdateMethod");
            Assert.That(updateMethodProp != null || updateMethodField != null, Is.True);
            Assert.That(blendUpdateMethodProp != null || blendUpdateMethodField != null, Is.True);

            var updateMethodsType = updateMethodProp != null ? updateMethodProp.PropertyType : updateMethodField.FieldType;
            var brainUpdateMethodsType = blendUpdateMethodProp != null ? blendUpdateMethodProp.PropertyType : blendUpdateMethodField.FieldType;

            var smartUpdate = System.Enum.Parse(updateMethodsType, "SmartUpdate");
            var fixedUpdate = System.Enum.Parse(brainUpdateMethodsType, "FixedUpdate");
            if (updateMethodProp != null)
            {
                updateMethodProp.SetValue(brain, smartUpdate);
            }
            else
            {
                updateMethodField.SetValue(brain, smartUpdate);
            }

            if (blendUpdateMethodProp != null)
            {
                blendUpdateMethodProp.SetValue(brain, fixedUpdate);
            }
            else
            {
                blendUpdateMethodField.SetValue(brain, fixedUpdate);
            }

            defaults.ApplyDefaults();

            var updateMethodValue = updateMethodProp != null ? updateMethodProp.GetValue(brain) : updateMethodField.GetValue(brain);
            var blendMethodValue = blendUpdateMethodProp != null ? blendUpdateMethodProp.GetValue(brain) : blendUpdateMethodField.GetValue(brain);
            Assert.That(updateMethodValue?.ToString(), Is.EqualTo("LateUpdate"));
            Assert.That(blendMethodValue?.ToString(), Is.EqualTo("LateUpdate"));
            Assert.That(camera.nearClipPlane, Is.EqualTo(0.001f).Within(0.0001f));
            Assert.That(camera.farClipPlane, Is.GreaterThan(camera.nearClipPlane));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ViewmodelAnimationAdapter_MapsWeaponEventsToRuntimeState()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeWeaponEvents = new DefaultRuntimeEvents();
            GameObject root = null;

            try
            {
                RuntimeKernelBootstrapper.Events = runtimeWeaponEvents;
                root = new GameObject("ViewmodelAdapterRoot");
                var adapter = root.AddComponent<ViewmodelAnimationAdapter>();
                adapter.SetEquippedItemIdForTests("weapon-kar98k");

                runtimeWeaponEvents.RaiseWeaponReloadStarted("weapon-kar98k");
                runtimeWeaponEvents.RaiseWeaponFired("weapon-kar98k", Vector3.zero, Vector3.forward);
                runtimeWeaponEvents.RaiseWeaponAimChanged("weapon-kar98k", true);
                runtimeWeaponEvents.RaiseWeaponReloadCancelled("weapon-kar98k", WeaponReloadCancelReason.Sprint);

                Assert.That(adapter.IsReloadingDebug, Is.False);
                Assert.That(adapter.IsAimingDebug, Is.True);
                Assert.That(adapter.AimWeightDebug, Is.EqualTo(1f));
                Assert.That(adapter.FireTriggerCountDebug, Is.EqualTo(1));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void ViewmodelBindingResolver_ReportsMissingRequiredBindPointsAsInvalid()
        {
            var root = new GameObject("WeaponRoot");
            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(root.transform);

            var result = ViewmodelBindingResolver.Resolve(root.transform);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorsCount, Is.GreaterThan(0));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ViewmodelProfileResolver_PrefersSpecificThenFamilyThenGlobal()
        {
            var global = ScriptableObject.CreateInstance<AnimationContractProfile>();
            var family = ScriptableObject.CreateInstance<AnimationContractProfile>();
            var specific = ScriptableObject.CreateInstance<AnimationContractProfile>();

            try
            {
                Assert.That(ViewmodelProfileResolver.Resolve(specific, family, global), Is.SameAs(specific));
                Assert.That(ViewmodelProfileResolver.Resolve(null, family, global), Is.SameAs(family));
                Assert.That(ViewmodelProfileResolver.Resolve(null, null, global), Is.SameAs(global));
            }
            finally
            {
                Object.DestroyImmediate(global);
                Object.DestroyImmediate(family);
                Object.DestroyImmediate(specific);
            }
        }

        [Test]
        public void PlayerMover_Tick_MovementLocked_DoesNotMove()
        {
            var root = new GameObject("PlayerRoot");
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.3f;

            var input = root.AddComponent<TestInputSource>();
            input.Move = Vector2.up;

            var settings = new PlayerMovementSettings
            {
                WalkSpeed = 6f,
                SprintSpeed = 9f,
                Acceleration = 100f,
                Gravity = -25f,
                JumpHeight = 1.25f
            };

            var mover = root.AddComponent<PlayerMover>();
            mover.Configure(input, settings);
            mover.SetMovementLocked(true);

            try
            {
                var startingPosition = root.transform.position;
                mover.Tick(0.1f);

                var horizontalDisplacement = root.transform.position - startingPosition;
                horizontalDisplacement.y = 0f;
                Assert.That(horizontalDisplacement.sqrMagnitude, Is.LessThan(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerLookController_Tick_FocusTargetOverride_SmoothlyConvergesTowardTargetAndIgnoresLookInput()
        {
            var root = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot");
            cameraPivot.transform.SetParent(root.transform);
            cameraPivot.transform.localPosition = new Vector3(0f, 1.8f, 0f);

            var focusTarget = new GameObject("FocusTarget");
            focusTarget.transform.position = new Vector3(10f, 2.4f, 10f);

            var input = root.AddComponent<TestInputSource>();
            input.Look = new Vector2(-25f, 50f);

            var look = root.AddComponent<PlayerLookController>();
            try
            {
                look.Configure(input, cameraPivot.transform);
                look.LookSensitivity = Vector2.one;
                look.SetFocusTargetOverride(focusTarget.transform);
                look.Tick(0.05f);

                var desiredYaw = Mathf.Atan2(10f, 10f) * Mathf.Rad2Deg;
                var desiredPitch = -Mathf.Atan2(0.6f, Mathf.Sqrt(200f)) * Mathf.Rad2Deg;
                var firstTickYaw = root.transform.eulerAngles.y;
                var firstTickPitch = Mathf.DeltaAngle(0f, cameraPivot.transform.localEulerAngles.x);
                var desiredDirection = (focusTarget.transform.position - cameraPivot.transform.position).normalized;

                Assert.That(Mathf.DeltaAngle(firstTickYaw, desiredYaw), Is.GreaterThan(1f),
                    "Dialogue focus should ease toward the target instead of snapping on the first tick.");
                Assert.That(firstTickYaw, Is.GreaterThan(0.1f));
                Assert.That(firstTickPitch, Is.LessThan(-0.1f));
                Assert.That(Vector3.Dot(cameraPivot.transform.forward, desiredDirection), Is.LessThan(0.999f));

                for (var i = 0; i < 20; i++)
                {
                    look.Tick(0.05f);
                }

                Assert.That(root.transform.eulerAngles.y, Is.EqualTo(desiredYaw).Within(0.5f));
                Assert.That(Mathf.DeltaAngle(0f, cameraPivot.transform.localEulerAngles.x), Is.EqualTo(desiredPitch).Within(0.5f));
                Assert.That(Vector3.Dot(cameraPivot.transform.forward, desiredDirection), Is.GreaterThan(0.999f));
            }
            finally
            {
                Object.DestroyImmediate(focusTarget);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerCursorLockController_ForcedUnlockOverride_ShowsCursorWithoutDroppingRequestedLock()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;
            GameObject go = null;

            try
            {
                go = new GameObject("CursorLockDialogueOverride");
                var controller = go.AddComponent<PlayerCursorLockController>();
                controller.LockCursor();
                controller.SetForcedCursorUnlock(true);

                Assert.That(controller.IsCursorLockRequested, Is.True);
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                Assert.That(Cursor.visible, Is.True);

                controller.SetForcedCursorUnlock(false);

                Assert.That(controller.IsCursorLockRequested, Is.True);
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked));
                Assert.That(Cursor.visible, Is.False);
            }
            finally
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }

                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
        }

        [Test]
        public void PlayerCursorLockController_ForcedUnlockOverride_IgnoresEscapeUntilOverrideEnds()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;
            GameObject go = null;

            try
            {
                go = new GameObject("CursorLockDialogueEscape");
                var controller = go.AddComponent<PlayerCursorLockController>();
                controller.SetEscapeKeySource(new TestEscapeKeySource(alwaysPressed: true));
                controller.LockCursor();
                controller.SetForcedCursorUnlock(true);

                InvokePrivateUpdate(controller);

                Assert.That(controller.IsCursorLockRequested, Is.True);
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                Assert.That(Cursor.visible, Is.True);

                controller.SetForcedCursorUnlock(false);

                Assert.That(controller.IsCursorLockRequested, Is.True);
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked));
                Assert.That(Cursor.visible, Is.False);
            }
            finally
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }

                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
        }

        private static void InvokePrivateUpdate(PlayerCursorLockController controller)
        {
            var updateMethod = typeof(PlayerCursorLockController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(updateMethod, Is.Not.Null);
            updateMethod.Invoke(controller, null);
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public Vector2 Move;
            public Vector2 Look;
            public bool SprintHeldValue;
            public bool AimHeldValue;
            public bool JumpPressedThisFrame;
            public bool PickupPressedThisFrame;
            public bool FirePressedThisFrame;
            public bool ReloadPressedThisFrame;
            public int BeltSlotPressed = -1;

            public Vector2 MoveInput => Move;
            public Vector2 LookInput => Look;
            public bool SprintHeld => SprintHeldValue;
            public bool AimHeld => AimHeldValue;
            public bool ConsumeJumpPressed()
            {
                if (!JumpPressedThisFrame)
                {
                    return false;
                }

                JumpPressedThisFrame = false;
                return true;
            }

            public bool ConsumePickupPressed()
            {
                if (!PickupPressedThisFrame)
                {
                    return false;
                }

                PickupPressedThisFrame = false;
                return true;
            }

            public int ConsumeBeltSelectPressed()
            {
                var pressed = BeltSlotPressed;
                BeltSlotPressed = -1;
                return pressed;
            }

            public bool ConsumeMenuTogglePressed()
            {
                return false;
            }

            public bool ConsumeAimTogglePressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;

            public bool ConsumeFirePressed()
            {
                if (!FirePressedThisFrame)
                {
                    return false;
                }

                FirePressedThisFrame = false;
                return true;
            }

            public bool ConsumeReloadPressed()
            {
                if (!ReloadPressedThisFrame)
                {
                    return false;
                }

                ReloadPressedThisFrame = false;
                return true;
            }

            private void LateUpdate()
            {
                JumpPressedThisFrame = false;
                PickupPressedThisFrame = false;
                FirePressedThisFrame = false;
                ReloadPressedThisFrame = false;
                BeltSlotPressed = -1;
            }
        }

        private sealed class TestEscapeKeySource : IPlayerCursorEscapeKeySource
        {
            private readonly bool _alwaysPressed;

            public TestEscapeKeySource(bool alwaysPressed)
            {
                _alwaysPressed = alwaysPressed;
            }

            public bool WasEscapePressedThisFrame()
            {
                return _alwaysPressed;
            }
        }

        private sealed class TestUiStateEvents : IUiStateEvents
        {
            public bool IsShopTradeMenuOpen { get; private set; }
            public bool IsWorkbenchMenuVisible { get; private set; }
            public bool IsTabInventoryVisible { get; private set; }
            public bool IsEscMenuVisible { get; private set; }
            public bool IsAnyMenuOpen => IsShopTradeMenuOpen || IsWorkbenchMenuVisible || IsTabInventoryVisible || IsEscMenuVisible;

            public event System.Action<bool> OnWorkbenchMenuVisibilityChanged;
            public event System.Action<bool> OnTabInventoryVisibilityChanged;
            public event System.Action<bool> OnEscMenuVisibilityChanged;

            public void RaiseWorkbenchMenuVisibilityChanged(bool isVisible)
            {
                IsWorkbenchMenuVisible = isVisible;
                OnWorkbenchMenuVisibilityChanged?.Invoke(isVisible);
            }

            public void RaiseTabInventoryVisibilityChanged(bool isVisible)
            {
                IsTabInventoryVisible = isVisible;
                OnTabInventoryVisibilityChanged?.Invoke(isVisible);
            }

            public void RaiseEscMenuVisibilityChanged(bool isVisible)
            {
                IsEscMenuVisible = isVisible;
                OnEscMenuVisibilityChanged?.Invoke(isVisible);
            }

            public void SetShopTradeMenuOpen(bool isOpen)
            {
                IsShopTradeMenuOpen = isOpen;
            }
        }

        private sealed class TestShopEvents : IShopEvents
        {
            public event System.Action<string> OnShopTradeOpenRequested;
            public event System.Action<string> OnShopTradeOpened;
            public event System.Action OnShopTradeClosed;
            public event System.Action<string, int> OnShopBuyRequested;
            public event System.Action<string, int> OnShopSellRequested;
            public event System.Action<ShopCheckoutRequest> OnShopBuyCheckoutRequested;
            public event System.Action<ShopCheckoutRequest> OnShopSellCheckoutRequested;
            public event System.Action<ShopTradeResultPayload> OnShopTradeResultReceived;

            public void RaiseShopTradeOpenRequested(string vendorId) => OnShopTradeOpenRequested?.Invoke(vendorId);

            public void RaiseShopTradeOpened(string vendorId)
            {
                OnShopTradeOpened?.Invoke(vendorId);
            }

            public void RaiseShopTradeClosed()
            {
                OnShopTradeClosed?.Invoke();
            }

            public void RaiseShopBuyRequested(string itemId, int quantity) => OnShopBuyRequested?.Invoke(itemId, quantity);
            public void RaiseShopSellRequested(string itemId, int quantity) => OnShopSellRequested?.Invoke(itemId, quantity);
            public void RaiseShopBuyCheckoutRequested(ShopCheckoutRequest request) => OnShopBuyCheckoutRequested?.Invoke(request);
            public void RaiseShopSellCheckoutRequested(ShopCheckoutRequest request) => OnShopSellCheckoutRequested?.Invoke(request);

            public void RaiseShopTradeResult(ShopTradeResultPayload payload)
            {
                OnShopTradeResultReceived?.Invoke(payload);
            }
        }
    }
}
