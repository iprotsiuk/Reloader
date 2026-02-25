namespace Reloader.UI
{
    public readonly struct InventoryTooltipModel
    {
        public string DisplayName { get; }
        public int Quantity { get; }
        public string Category { get; }
        public string ShortStats { get; }

        public InventoryTooltipModel(string displayName, int quantity, string category, string shortStats)
        {
            DisplayName = displayName;
            Quantity = quantity;
            Category = category;
            ShortStats = shortStats;
        }
    }
}
