using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Combat;
using Reloader.UI.Toolkit.HealthHud;
using Reloader.UI.Toolkit.Runtime;
using Reloader.Weapons.Ballistics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class HealthHudRuntimeBridgePlayModeTests
    {
        [Test]
        public void RuntimeBridge_BindHealthHud_RendersInitialStateAndDamageFlash()
        {
            var bridgeGo = new GameObject("HealthHudBridge");
            var bridge = bridgeGo.AddComponent<UiToolkitScreenRuntimeBridge>();

            var playerRoot = new GameObject("PlayerRoot");
            var receiver = playerRoot.AddComponent<HumanoidDamageReceiver>();
            receiver.SetHealthStateForRuntime(100f, 100f);

            var root = BuildRoot();
            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindHealthHud",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var subscription = bindMethod.Invoke(
                bridge,
                new object[] { root, UiRuntimeCompositionIds.ControllerObjectNames.HealthHud }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            try
            {
                var controller = bridgeGo.transform.Find(UiRuntimeCompositionIds.ControllerObjectNames.HealthHud)?.GetComponent<HealthHudController>();
                Assert.That(controller, Is.Not.Null);

                Assert.That(root.Q<Label>("health-hud__value-label")!.text, Is.EqualTo("100 / 100 (100%)"));
                Assert.That(root.Q<VisualElement>("health-hud__flash")!.style.display.value, Is.EqualTo(DisplayStyle.None));

                receiver.ApplyDamage(new ProjectileImpactPayload(
                    "test-round",
                    Vector3.zero,
                    Vector3.up,
                    1000f,
                    playerRoot));

                controller!.GetType()
                    .GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(controller, null);

                Assert.That(root.Q<Label>("health-hud__value-label")!.text, Is.EqualTo("0 / 100 (0%)"));
                Assert.That(root.Q<Label>("health-hud__status-label")!.text, Is.EqualTo("DEAD"));
                Assert.That(root.Q<VisualElement>("health-hud__flash")!.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            }
            finally
            {
                subscription.Dispose();
                UnityEngine.Object.DestroyImmediate(bridgeGo);
                UnityEngine.Object.DestroyImmediate(playerRoot);
            }
        }

        private static VisualElement BuildRoot()
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
            return screen;
        }
    }
}
