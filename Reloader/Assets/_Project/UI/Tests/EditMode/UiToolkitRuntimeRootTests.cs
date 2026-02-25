using NUnit.Framework;
using Reloader.UI.Toolkit.Runtime;

namespace Reloader.UI.Tests.EditMode
{
    public class UiToolkitRuntimeRootTests
    {
        [Test]
        public void Registry_WhenScreenRegistered_TryGetReturnsTrue()
        {
            var registry = new UiScreenRegistry();
            registry.Register("belt-hud", "Reloader.UI.Toolkit.BeltHud");

            var resolved = registry.TryGet("belt-hud", out var moduleTypeName);

            Assert.That(resolved, Is.True);
            Assert.That(moduleTypeName, Is.EqualTo("Reloader.UI.Toolkit.BeltHud"));
        }

        [Test]
        public void CompositionConfig_WhenScreenConfigured_ReturnsEnabledComponents()
        {
            var config = new UiScreenCompositionConfig();
            config.SetComponents("tab-inventory", new[]
            {
                "inventory.grid",
                "inventory.tooltip",
                "inventory.quick-actions"
            });

            var resolved = config.TryGetComponents("tab-inventory", out var components);

            Assert.That(resolved, Is.True);
            Assert.That(components, Is.EquivalentTo(new[]
            {
                "inventory.grid",
                "inventory.tooltip",
                "inventory.quick-actions"
            }));
        }

        [Test]
        public void ActionMap_WhenKeyMissing_TryResolveReturnsFalseWithoutThrowing()
        {
            var config = new UiActionMapConfig();
            config.Set("inventory.slot.primary", "InspectItem");

            var resolved = config.TryResolve("inventory.slot.secondary", out var command);

            Assert.That(resolved, Is.False);
            Assert.That(command, Is.Null);
        }
    }
}
