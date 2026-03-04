using System;
using System.Collections.Generic;

namespace Reloader.UI.Toolkit.Runtime
{
    public sealed class UiScreenCompositionConfig
    {
        private readonly Dictionary<string, List<string>> _componentsByScreen = new(StringComparer.Ordinal);

        public void SetComponents(string screenId, IEnumerable<string> components)
        {
            if (string.IsNullOrWhiteSpace(screenId))
            {
                throw new ArgumentException("Screen id is required.", nameof(screenId));
            }

            if (components == null)
            {
                throw new ArgumentNullException(nameof(components));
            }

            _componentsByScreen[screenId] = new List<string>(components);
        }

        public bool TryGetComponents(string screenId, out IReadOnlyList<string> components)
        {
            if (string.IsNullOrWhiteSpace(screenId))
            {
                components = null;
                return false;
            }

            if (_componentsByScreen.TryGetValue(screenId, out var resolved))
            {
                components = resolved;
                return true;
            }

            components = null;
            return false;
        }

        public static UiScreenCompositionConfig CreateWithDefaults()
        {
            var config = new UiScreenCompositionConfig();
            config.SetComponents(UiRuntimeCompositionIds.ScreenIds.BeltHud, new[] { "belt.slots" });
            config.SetComponents(UiRuntimeCompositionIds.ScreenIds.AmmoHud, new[] { "ammo.label" });
            config.SetComponents(UiRuntimeCompositionIds.ScreenIds.TabInventory, new[] { "inventory.slots", "inventory.tooltip", "inventory.drag" });
            config.SetComponents(UiRuntimeCompositionIds.ScreenIds.EscMenu, new[] { "esc.menu.panel", "esc.menu.actions", "esc.menu.settings" });
            config.SetComponents(UiRuntimeCompositionIds.ScreenIds.Trade, new[] { "trade.tabs", "trade.cart", "trade.order" });
            config.SetComponents(UiRuntimeCompositionIds.ScreenIds.ReloadingWorkbench, new[] { "reloading.operations", "reloading.result" });
            config.SetComponents(UiRuntimeCompositionIds.ScreenIds.InteractionHint, new[] { "interaction-hint.text" });
            return config;
        }
    }
}
