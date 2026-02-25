using System;

namespace Reloader.UI
{
    public sealed class InventoryTooltipService
    {
        public bool TryBuild(string itemId, int quantity, int maxStack, out InventoryTooltipModel model)
        {
            model = default;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            var displayName = ToDisplayName(itemId);
            var category = ResolveCategory(itemId);
            var stats = maxStack > 1
                ? "Stack: " + Math.Max(0, quantity) + "/" + Math.Max(1, maxStack)
                : "Single item";

            model = new InventoryTooltipModel(displayName, Math.Max(0, quantity), category, stats);
            return true;
        }

        private static string ToDisplayName(string itemId)
        {
            return itemId.Replace("-", " ").Replace("_", " ").Trim();
        }

        private static string ResolveCategory(string itemId)
        {
            if (itemId.Contains("ammo", StringComparison.OrdinalIgnoreCase))
            {
                return "Ammunition";
            }

            if (itemId.Contains("powder", StringComparison.OrdinalIgnoreCase)
                || itemId.Contains("primer", StringComparison.OrdinalIgnoreCase)
                || itemId.Contains("bullet", StringComparison.OrdinalIgnoreCase)
                || itemId.Contains("case", StringComparison.OrdinalIgnoreCase))
            {
                return "Reloading Component";
            }

            if (itemId.Contains("weapon", StringComparison.OrdinalIgnoreCase)
                || itemId.Contains("rifle", StringComparison.OrdinalIgnoreCase)
                || itemId.Contains("pistol", StringComparison.OrdinalIgnoreCase))
            {
                return "Weapon";
            }

            return "Inventory Item";
        }
    }
}
