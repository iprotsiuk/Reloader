using System;
using NUnit.Framework;
using Reloader.NPCs.Combat;
using Reloader.UI.Toolkit.HealthHud;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using Object = UnityEngine.Object;

namespace Reloader.UI.Tests.EditMode
{
    public class HealthHudControllerEditModeTests
    {
        [Test]
        public void Refresh_WhenCanonicalPlayerRootIsPresent_ShowsHealthAndRefreshesAcrossHealthStates()
        {
            var fixture = new HealthHudFixture();

            try
            {
                fixture.Controller.Refresh();

                Assert.That(fixture.ValueLabel.text, Is.EqualTo("100 / 100 (100%)"));
                Assert.That(fixture.Root.style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(fixture.Root.ClassListContains("is-low-health"), Is.False);
                Assert.That(fixture.Root.ClassListContains("is-critical"), Is.False);
                Assert.That(fixture.Root.ClassListContains("is-dead"), Is.False);

                fixture.Receiver.SetHealthStateForRuntime(24f, 100f);

                fixture.Controller.Refresh();

                Assert.That(fixture.ValueLabel.text, Is.EqualTo("24 / 100 (24%)"));
                Assert.That(fixture.Root.ClassListContains("is-low-health"), Is.True);
                Assert.That(fixture.Root.ClassListContains("is-critical"), Is.False);
                Assert.That(fixture.Root.ClassListContains("is-dead"), Is.False);

                fixture.Receiver.SetHealthStateForRuntime(0f, 100f);
                fixture.Controller.Refresh();

                Assert.That(fixture.ValueLabel.text, Is.EqualTo("0 / 100 (0%)"));
                Assert.That(fixture.Root.ClassListContains("is-critical"), Is.True);
                Assert.That(fixture.Root.ClassListContains("is-dead"), Is.True);
                Assert.That(fixture.StatusLabel.text, Is.EqualTo("DEAD"));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void Refresh_WhenHealthStateChangesSilently_UpdatesHudWithoutDamageFlash()
        {
            var fixture = new HealthHudFixture();

            try
            {
                fixture.Controller.Refresh();

                fixture.Receiver.SetHealthStateForRuntime(24f, 100f);
                InvokeLateUpdate(fixture.Controller);

                Assert.That(fixture.ValueLabel.text, Is.EqualTo("24 / 100 (24%)"));
                Assert.That(fixture.Root.ClassListContains("is-low-health"), Is.True);
                Assert.That(fixture.Root.ClassListContains("is-critical"), Is.False);
                Assert.That(fixture.Flash.style.display.value, Is.EqualTo(DisplayStyle.None));

                fixture.Receiver.SetHealthStateForRuntime(100f, 100f);
                InvokeLateUpdate(fixture.Controller);

                Assert.That(fixture.ValueLabel.text, Is.EqualTo("100 / 100 (100%)"));
                Assert.That(fixture.Root.ClassListContains("is-low-health"), Is.False);
                Assert.That(fixture.Root.ClassListContains("is-critical"), Is.False);
                Assert.That(fixture.Flash.style.display.value, Is.EqualTo(DisplayStyle.None));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void Refresh_WhenReenabledAfterDisable_ResubscribesToDamageEvents()
        {
            var fixture = new HealthHudFixture();

            try
            {
                fixture.Controller.Refresh();

                fixture.ControllerRoot.SetActive(false);
                fixture.ControllerRoot.SetActive(true);

                InvokeApplyDamage(fixture.Receiver, CreateImpactPayload(fixture.PlayerRoot, deliveredEnergyJoules: 3500f));

                InvokeLateUpdate(fixture.Controller);

                Assert.That(fixture.ValueLabel.text, Is.EqualTo("0 / 100 (0%)"));
                Assert.That(fixture.Root.ClassListContains("is-dead"), Is.True);
                Assert.That(fixture.Flash.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void Refresh_WhenNoPlayerReceiverCanBeResolved_HidesHud()
        {
            var go = new GameObject("HealthHudControllerOnly");
            var controller = go.AddComponent<HealthHudController>();
            var (binder, root, valueLabel, statusLabel, _) = BuildBinder();
            controller.SetViewBinder(binder);

            try
            {
                controller.Refresh();

                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(valueLabel.text, Is.EqualTo("-- / --"));
                Assert.That(statusLabel.text, Is.EqualTo(string.Empty));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Refresh_WhenResolvingReceiverDuringRefresh_DoesNotLeaveRefreshQueued()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var receiver = playerRoot.AddComponent<HumanoidDamageReceiver>();
            receiver.SetHealthStateForRuntime(100f, 100f);

            var controllerRoot = new GameObject("HealthHudController");
            controllerRoot.SetActive(false);
            var controller = controllerRoot.AddComponent<HealthHudController>();

            var (binder, root, valueLabel, _, _) = BuildBinder();
            SetPrivateField(controller, "_viewBinder", binder);

            try
            {
                controller.Refresh();

                Assert.That(GetPrivateField<bool>(controller, "_pendingRefresh"), Is.False);
                Assert.That(valueLabel.text, Is.EqualTo("100 / 100 (100%)"));
                Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            }
            finally
            {
                Object.DestroyImmediate(controllerRoot);
                Object.DestroyImmediate(playerRoot);
            }
        }

        [Test]
        public void SetDamageReceiver_WhenPreviousReceiverWasDestroyed_UnsubscribesDestroyedReceiver()
        {
            var controllerRoot = new GameObject("HealthHudController");
            controllerRoot.SetActive(false);
            var controller = controllerRoot.AddComponent<HealthHudController>();

            var receiverRoot = new GameObject("PlayerRoot");
            var receiver = receiverRoot.AddComponent<HumanoidDamageReceiver>();
            receiver.SetHealthStateForRuntime(100f, 100f);

            try
            {
                controller.SetDamageReceiver(receiver);

                Object.DestroyImmediate(receiverRoot);

                controller.SetDamageReceiver(null);

                Assert.That(ReferenceEquals(GetPrivateField<object>(controller, "_subscribedDamageReceiver"), null), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(controllerRoot);
            }
        }

        private sealed class HealthHudFixture
        {
            public HealthHudFixture()
            {
                PlayerRoot = new GameObject("PlayerRoot");
                Receiver = PlayerRoot.AddComponent<HumanoidDamageReceiver>();
                Receiver.SetHealthStateForRuntime(100f, 100f);

                ControllerRoot = new GameObject("HealthHudController");
                Controller = ControllerRoot.AddComponent<HealthHudController>();

                var (binder, root, valueLabel, statusLabel, flash) = BuildBinder();
                Root = root;
                ValueLabel = valueLabel;
                StatusLabel = statusLabel;
                Flash = flash;

                Controller.SetViewBinder(binder);
            }

            public GameObject PlayerRoot { get; }
            public HumanoidDamageReceiver Receiver { get; }
            public GameObject ControllerRoot { get; }
            public HealthHudController Controller { get; }
            public VisualElement Root { get; }
            public Label ValueLabel { get; }
            public Label StatusLabel { get; }
            public VisualElement Flash { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(ControllerRoot);
                Object.DestroyImmediate(PlayerRoot);
            }
        }

        private static (HealthHudViewBinder binder, VisualElement Root, Label ValueLabel, Label StatusLabel, VisualElement Flash) BuildBinder()
        {
            var screen = new VisualElement { name = "health-hud__screen" };
            var root = new VisualElement { name = "health-hud__root" };
            var frame = new VisualElement { name = "health-hud__frame" };
            var topRow = new VisualElement { name = "health-hud__top-row" };
            var title = new Label("HEALTH") { name = "health-hud__title" };
            var value = new Label { name = "health-hud__value-label" };
            var track = new VisualElement { name = "health-hud__track" };
            var fill = new VisualElement { name = "health-hud__fill" };
            var flash = new VisualElement { name = "health-hud__flash" };
            var status = new Label { name = "health-hud__status-label" };

            track.Add(fill);
            track.Add(flash);
            topRow.Add(title);
            topRow.Add(value);
            frame.Add(topRow);
            frame.Add(track);
            frame.Add(status);
            root.Add(frame);
            screen.Add(root);

            var binder = new HealthHudViewBinder();
            binder.Initialize(screen);
            return (binder, root, value, status, flash);
        }

        private static void InvokeLateUpdate(HealthHudController controller)
        {
            typeof(HealthHudController)
                .GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(controller, null);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            return (T)target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(target);
        }

        private static object CreateImpactPayload(GameObject hitObject, float deliveredEnergyJoules = 1000f)
        {
            var payloadType = Type.GetType("Reloader.Weapons.Ballistics.ProjectileImpactPayload, Reloader.Weapons", throwOnError: false);
            Assert.That(payloadType, Is.Not.Null, "Expected ProjectileImpactPayload type to exist.");

            return Activator.CreateInstance(
                payloadType!,
                "test-round",
                hitObject.transform.position,
                Vector3.back,
                0f,
                hitObject,
                (Vector3?)Vector3.zero,
                (Vector3?)Vector3.forward,
                0f,
                0f,
                deliveredEnergyJoules)!;
        }

        private static void InvokeApplyDamage(HumanoidDamageReceiver receiver, object payload)
        {
            var method = typeof(HumanoidDamageReceiver).GetMethod(
                nameof(HumanoidDamageReceiver.ApplyDamage),
                BindingFlags.Instance | BindingFlags.Public);

            Assert.That(method, Is.Not.Null, "Expected HumanoidDamageReceiver.ApplyDamage to exist.");
            method!.Invoke(receiver, new[] { payload });
        }
    }
}
