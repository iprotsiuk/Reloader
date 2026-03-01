using NUnit.Framework;
using Reloader.UI.Toolkit.Runtime;

namespace Reloader.UI.Tests.EditMode
{
    public class UiCompositionAndActionMapTests
    {
        [TestCase("belt-hud")]
        [TestCase("ammo-hud")]
        [TestCase("tab-inventory")]
        [TestCase("trade-ui")]
        [TestCase("reloading-workbench")]
        public void CompositionConfig_DefaultsContainRequiredScreens(string screenId)
        {
            var config = UiScreenCompositionConfig.CreateWithDefaults();

            var resolved = config.TryGetComponents(screenId, out var components);

            Assert.That(resolved, Is.True);
            Assert.That(components, Is.Not.Null);
            Assert.That(components.Count, Is.GreaterThan(0));
        }

        [TestCase("belt.slot.select")]
        [TestCase("inventory.drag.merge")]
        [TestCase("inventory.drag.swap")]
        [TestCase("inventory.drag.drop")]
        [TestCase("trade.confirm.buy")]
        [TestCase("trade.confirm.sell")]
        [TestCase("reloading.operation.select")]
        [TestCase("reloading.operation.execute")]
        public void ActionMap_DefaultsContainRequiredIntentKeys(string intentKey)
        {
            var map = UiActionMapConfig.CreateWithDefaults();

            var resolved = map.TryResolve(intentKey, out var commandName);

            Assert.That(resolved, Is.True);
            Assert.That(string.IsNullOrWhiteSpace(commandName), Is.False);
        }
    }
}
