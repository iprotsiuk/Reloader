using NUnit.Framework;
using Reloader.UI.Toolkit.HealthHud;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.EditMode
{
    public class HealthHudViewBinderEditModeTests
    {
        [Test]
        public void Render_WhenHealthy_UpdatesLabelFillAndClearsAlertClasses()
        {
            var root = BuildRoot();
            var binder = new HealthHudViewBinder();
            binder.Initialize(root);

            binder.Render(HealthHudUiState.Create(
                "87 / 100 (87%)",
                0.87f,
                isVisible: true,
                isLowHealth: false,
                isCritical: false,
                isDead: false,
                isDamageFlashVisible: false));

            var hudRoot = root.Q<VisualElement>("health-hud__root");
            Assert.That(hudRoot, Is.Not.Null);
            Assert.That(hudRoot!.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(hudRoot.ClassListContains("is-low-health"), Is.False);
            Assert.That(hudRoot.ClassListContains("is-critical"), Is.False);
            Assert.That(hudRoot.ClassListContains("is-dead"), Is.False);
            Assert.That(hudRoot.ClassListContains("is-damage-flash"), Is.False);

            Assert.That(root.Q<Label>("health-hud__value-label")!.text, Is.EqualTo("87 / 100 (87%)"));
            Assert.That(root.Q<Label>("health-hud__status-label")!.text, Is.EqualTo(string.Empty));
            Assert.That(root.Q<VisualElement>("health-hud__fill")!.style.width.value.value, Is.EqualTo(87f).Within(0.01f));
            Assert.That(root.Q<VisualElement>("health-hud__flash")!.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [Test]
        public void Render_WhenLowHealth_ShowsLowAndCriticalState()
        {
            var root = BuildRoot();
            var binder = new HealthHudViewBinder();
            binder.Initialize(root);

            binder.Render(HealthHudUiState.Create(
                "12 / 100 (12%)",
                0.12f,
                isVisible: true,
                isLowHealth: true,
                isCritical: true,
                isDead: false,
                isDamageFlashVisible: true));

            var hudRoot = root.Q<VisualElement>("health-hud__root");
            Assert.That(hudRoot, Is.Not.Null);
            Assert.That(hudRoot!.ClassListContains("is-low-health"), Is.True);
            Assert.That(hudRoot.ClassListContains("is-critical"), Is.True);
            Assert.That(hudRoot.ClassListContains("is-dead"), Is.False);
            Assert.That(hudRoot.ClassListContains("is-damage-flash"), Is.True);
            Assert.That(root.Q<Label>("health-hud__status-label")!.text, Is.EqualTo("CRITICAL"));
            Assert.That(root.Q<VisualElement>("health-hud__flash")!.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void Render_WhenDead_CollapsesHealthyIndicatorsAndShowsDeadState()
        {
            var root = BuildRoot();
            var binder = new HealthHudViewBinder();
            binder.Initialize(root);

            binder.Render(HealthHudUiState.Create(
                "0 / 100 (0%)",
                0f,
                isVisible: true,
                isLowHealth: false,
                isCritical: true,
                isDead: true,
                isDamageFlashVisible: false));

            var hudRoot = root.Q<VisualElement>("health-hud__root");
            Assert.That(hudRoot, Is.Not.Null);
            Assert.That(hudRoot!.ClassListContains("is-low-health"), Is.False);
            Assert.That(hudRoot.ClassListContains("is-critical"), Is.True);
            Assert.That(hudRoot.ClassListContains("is-dead"), Is.True);
            Assert.That(root.Q<Label>("health-hud__status-label")!.text, Is.EqualTo("DEAD"));
            Assert.That(root.Q<VisualElement>("health-hud__flash")!.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [Test]
        public void Render_WhenHidden_CollapsesRoot()
        {
            var root = BuildRoot();
            var binder = new HealthHudViewBinder();
            binder.Initialize(root);

            binder.Render(HealthHudUiState.Create(
                "-- / --",
                0f,
                isVisible: false,
                isLowHealth: false,
                isCritical: false,
                isDead: false,
                isDamageFlashVisible: false));

            var hudRoot = root.Q<VisualElement>("health-hud__root");
            Assert.That(hudRoot, Is.Not.Null);
            Assert.That(hudRoot!.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(root.Q<VisualElement>("health-hud__flash")!.style.display.value, Is.EqualTo(DisplayStyle.None));
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
