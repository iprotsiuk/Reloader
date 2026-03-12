using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Runtime;
using Reloader.Player;
using Reloader.PlayerDevice.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

namespace Reloader.PlayerDevice.Tests.PlayMode
{
    public class PlayerDeviceTargetSelectionPlayModeTests
    {
        [Test]
        public void BeginTargetSelection_ClosesTab_NextClickBindsTarget_AndPublishesConfirmationHint()
        {
            var controllerType = System.Type.GetType("Reloader.PlayerDevice.World.PlayerDeviceTargetSelectionController, Reloader.PlayerDevice");
            Assert.That(controllerType, Is.Not.Null, "PlayerDeviceTargetSelectionController type should exist.");

            var metricsType = System.Type.GetType("Reloader.Weapons.World.DummyTargetRangeMetrics, Reloader.Weapons");
            Assert.That(metricsType, Is.Not.Null, "DummyTargetRangeMetrics type should exist.");

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject targetParent = null;

            try
            {
                root = new GameObject("PlayerDeviceSelectionRoot");
                var input = root.AddComponent<TestInputSource>();
                var controller = root.AddComponent(controllerType);

                cameraGo = new GameObject("SelectionCamera");
                cameraGo.transform.position = Vector3.zero;
                cameraGo.transform.forward = Vector3.forward;
                var camera = cameraGo.AddComponent<Camera>();

                targetParent = new GameObject("Lane01");
                var target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                target.name = "RoundDummyTarget";
                target.transform.SetParent(targetParent.transform, worldPositionStays: false);
                target.transform.position = new Vector3(0f, 0f, 7f);

                var metrics = target.AddComponent(metricsType);
                InvokeMethod(metricsType, metrics, "Configure", "target.lane01.round", "Lane 01 Dummy", 137.5f);

                var runtimeState = new PlayerDeviceRuntimeState();
                InvokeMethod(controllerType, controller, "Configure", input, camera, runtimeState);

                runtimeHub.RaiseTabInventoryVisibilityChanged(true);
                InvokeMethod(controllerType, controller, "BeginTargetSelection");

                Assert.That(runtimeHub.IsTabInventoryVisible, Is.False);
                Assert.That(runtimeHub.CurrentInteractionHint.ContextId, Is.EqualTo("player-device.target-selection.pending"));
                Assert.That(runtimeHub.CurrentInteractionHint.ActionText, Is.EqualTo("Mark target"));

                input.PickupPressedThisFrame = true;
                InvokeMethod(controllerType, controller, "Tick");

                Assert.That(runtimeState.HasSelectedTargetBinding, Is.True);
                Assert.That(runtimeState.SelectedTargetBinding.TargetId, Is.EqualTo("target.lane01.round"));
                Assert.That(runtimeState.SelectedTargetBinding.DisplayName, Is.EqualTo("Lane 01 Dummy"));
                Assert.That(runtimeState.SelectedTargetBinding.DistanceMeters, Is.GreaterThan(0.1f));
                Assert.That(runtimeState.SelectedTargetBinding.DistanceMeters, Is.LessThan(20f));

                Assert.That(runtimeHub.CurrentInteractionHint.ContextId, Is.EqualTo("player-device.target-selection.confirmed"));
                Assert.That(runtimeHub.CurrentInteractionHint.ActionText, Is.EqualTo("Target marked"));
                Assert.That(runtimeHub.CurrentInteractionHint.SubjectText, Does.Contain("Lane01/RoundDummyTarget"));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }

                if (cameraGo != null)
                {
                    Object.DestroyImmediate(cameraGo);
                }

                if (targetParent != null)
                {
                    Object.DestroyImmediate(targetParent);
                }
            }
        }

        [Test]
        public void BeginTargetSelection_BindsDummyTargetDamageable_WhenNoSeparateRangeMetricsComponentPresent()
        {
            var controllerType = System.Type.GetType("Reloader.PlayerDevice.World.PlayerDeviceTargetSelectionController, Reloader.PlayerDevice");
            Assert.That(controllerType, Is.Not.Null, "PlayerDeviceTargetSelectionController type should exist.");

            var damageableType = System.Type.GetType("Reloader.Weapons.World.DummyTargetDamageable, Reloader.Weapons");
            Assert.That(damageableType, Is.Not.Null, "DummyTargetDamageable type should exist.");

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject targetParent = null;

            try
            {
                root = new GameObject("PlayerDeviceSelectionRoot_DamageableFallback");
                var input = root.AddComponent<TestInputSource>();
                var controller = root.AddComponent(controllerType);

                cameraGo = new GameObject("SelectionCamera");
                cameraGo.transform.position = Vector3.zero;
                cameraGo.transform.forward = Vector3.forward;
                var camera = cameraGo.AddComponent<Camera>();

                targetParent = new GameObject("Lane02");
                var target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                target.name = "RoundDummyTarget";
                target.transform.SetParent(targetParent.transform, worldPositionStays: false);
                target.transform.position = new Vector3(0f, 0f, 7f);
                target.AddComponent(damageableType);

                var runtimeState = new PlayerDeviceRuntimeState();
                InvokeMethod(controllerType, controller, "Configure", input, camera, runtimeState);
                InvokeMethod(controllerType, controller, "BeginTargetSelection");

                input.PickupPressedThisFrame = true;
                InvokeMethod(controllerType, controller, "Tick");

                Assert.That(runtimeState.HasSelectedTargetBinding, Is.True);
                Assert.That(runtimeState.SelectedTargetBinding.TargetId, Is.EqualTo("RoundDummyTarget"));
                Assert.That(runtimeHub.CurrentInteractionHint.ContextId, Is.EqualTo("player-device.target-selection.confirmed"));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }

                if (cameraGo != null)
                {
                    Object.DestroyImmediate(cameraGo);
                }

                if (targetParent != null)
                {
                    Object.DestroyImmediate(targetParent);
                }
            }
        }

        [Test]
        public void BeginTargetSelection_PrefersDedicatedRangeMetrics_WhenMultipleProvidersExist()
        {
            var controllerType = System.Type.GetType("Reloader.PlayerDevice.World.PlayerDeviceTargetSelectionController, Reloader.PlayerDevice");
            Assert.That(controllerType, Is.Not.Null, "PlayerDeviceTargetSelectionController type should exist.");

            var metricsType = System.Type.GetType("Reloader.Weapons.World.DummyTargetRangeMetrics, Reloader.Weapons");
            Assert.That(metricsType, Is.Not.Null, "DummyTargetRangeMetrics type should exist.");

            var damageableType = System.Type.GetType("Reloader.Weapons.World.DummyTargetDamageable, Reloader.Weapons");
            Assert.That(damageableType, Is.Not.Null, "DummyTargetDamageable type should exist.");

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject targetParent = null;

            try
            {
                root = new GameObject("PlayerDeviceSelectionRoot_PreferMetrics");
                var input = root.AddComponent<TestInputSource>();
                var controller = root.AddComponent(controllerType);

                cameraGo = new GameObject("SelectionCamera");
                cameraGo.transform.position = Vector3.zero;
                cameraGo.transform.forward = Vector3.forward;
                var camera = cameraGo.AddComponent<Camera>();

                targetParent = new GameObject("Lane04");
                var target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                target.name = "RoundDummyTarget";
                target.transform.SetParent(targetParent.transform, worldPositionStays: false);
                target.transform.position = new Vector3(0f, 0f, 7f);

                var damageable = target.AddComponent(damageableType);
                SetPrivateField(damageableType, damageable, "_targetId", "wrong.damageable.id");
                SetPrivateField(damageableType, damageable, "_displayName", "Wrong Damageable");
                SetPrivateField(damageableType, damageable, "_authoritativeDistanceMeters", 9f);

                var metrics = target.AddComponent(metricsType);
                InvokeMethod(metricsType, metrics, "Configure", "target.lane04.preferred", "Lane 04 Preferred", 177.7f);

                var runtimeState = new PlayerDeviceRuntimeState();
                InvokeMethod(controllerType, controller, "Configure", input, camera, runtimeState);
                InvokeMethod(controllerType, controller, "BeginTargetSelection");

                input.PickupPressedThisFrame = true;
                InvokeMethod(controllerType, controller, "Tick");

                Assert.That(runtimeState.HasSelectedTargetBinding, Is.True);
                Assert.That(runtimeState.SelectedTargetBinding.TargetId, Is.EqualTo("target.lane04.preferred"));
                Assert.That(runtimeState.SelectedTargetBinding.DisplayName, Is.EqualTo("Lane 04 Preferred"));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }

                if (cameraGo != null)
                {
                    Object.DestroyImmediate(cameraGo);
                }

                if (targetParent != null)
                {
                    Object.DestroyImmediate(targetParent);
                }
            }
        }

        [UnityTest]
        public IEnumerator BeginTargetSelection_ConfirmedHint_AutoClearsAfterTwoSeconds()
        {
            var controllerType = System.Type.GetType("Reloader.PlayerDevice.World.PlayerDeviceTargetSelectionController, Reloader.PlayerDevice");
            Assert.That(controllerType, Is.Not.Null, "PlayerDeviceTargetSelectionController type should exist.");

            var metricsType = System.Type.GetType("Reloader.Weapons.World.DummyTargetRangeMetrics, Reloader.Weapons");
            Assert.That(metricsType, Is.Not.Null, "DummyTargetRangeMetrics type should exist.");

            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;
            GameObject root = null;
            GameObject cameraGo = null;
            GameObject targetParent = null;

            try
            {
                root = new GameObject("PlayerDeviceSelectionRoot_AutoClear");
                var input = root.AddComponent<TestInputSource>();
                var controller = root.AddComponent(controllerType);

                cameraGo = new GameObject("SelectionCamera");
                cameraGo.transform.position = Vector3.zero;
                cameraGo.transform.forward = Vector3.forward;
                var camera = cameraGo.AddComponent<Camera>();

                targetParent = new GameObject("Lane03");
                var target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                target.name = "RoundDummyTarget";
                target.transform.SetParent(targetParent.transform, worldPositionStays: false);
                target.transform.position = new Vector3(0f, 0f, 7f);

                var metrics = target.AddComponent(metricsType);
                InvokeMethod(metricsType, metrics, "Configure", "target.lane03.round", "Lane 03 Dummy", 142.3f);

                var runtimeState = new PlayerDeviceRuntimeState();
                InvokeMethod(controllerType, controller, "Configure", input, camera, runtimeState);
                InvokeMethod(controllerType, controller, "BeginTargetSelection");

                input.PickupPressedThisFrame = true;
                InvokeMethod(controllerType, controller, "Tick");
                Assert.That(runtimeHub.HasInteractionHint, Is.True);
                Assert.That(runtimeHub.CurrentInteractionHint.ContextId, Is.EqualTo("player-device.target-selection.confirmed"));

                yield return new WaitForSecondsRealtime(2.1f);
                InvokeMethod(controllerType, controller, "Tick");

                Assert.That(runtimeHub.HasInteractionHint, Is.False);
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }

                if (cameraGo != null)
                {
                    Object.DestroyImmediate(cameraGo);
                }

                if (targetParent != null)
                {
                    Object.DestroyImmediate(targetParent);
                }
            }
        }

        private static void InvokeMethod(System.Type type, object instance, string methodName, params object[] args)
        {
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' on {type?.FullName}.");
            method.Invoke(instance, args);
        }

        private static void SetPrivateField(System.Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {type?.FullName}.");
            field.SetValue(instance, value);
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool PickupPressedThisFrame;

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
            public bool ConsumeDevConsoleTogglePressed() => false;
            public bool ConsumeAutocompletePressed() => false;
            public int ConsumeSuggestionDelta() => 0;

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
    }
}
