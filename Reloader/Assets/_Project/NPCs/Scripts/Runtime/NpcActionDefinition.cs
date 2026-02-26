namespace Reloader.NPCs.Runtime
{
    public readonly struct NpcActionDefinition
    {
        public NpcActionDefinition(string actionId, string displayName, int priority)
        {
            ActionId = actionId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Priority = priority;
        }

        public string ActionId { get; }
        public string DisplayName { get; }
        public int Priority { get; }
    }
}
