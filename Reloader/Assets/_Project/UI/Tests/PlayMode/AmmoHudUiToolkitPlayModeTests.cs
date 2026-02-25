using NUnit.Framework;
using Reloader.UI.Toolkit.AmmoHud;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class AmmoHudUiToolkitPlayModeTests
    {
        [Test]
        public void Render_WhenVisible_UpdatesAmmoLabelText()
        {
            var root = new VisualElement { name = "ammo__root" };
            var label = new Label { name = "ammo__count-label" };
            root.Add(label);

            var binder = new AmmoHudViewBinder();
            binder.Initialize(root);

            binder.Render(new AmmoHudUiState("5.56 NATO 18/90", true));

            Assert.That(label.text, Is.EqualTo("5.56 NATO 18/90"));
            Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void Render_WhenHidden_CollapsesRoot()
        {
            var root = new VisualElement { name = "ammo__root" };
            var label = new Label { name = "ammo__count-label" };
            root.Add(label);

            var binder = new AmmoHudViewBinder();
            binder.Initialize(root);

            binder.Render(new AmmoHudUiState("-- 0/0", false));

            Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
        }
    }
}
