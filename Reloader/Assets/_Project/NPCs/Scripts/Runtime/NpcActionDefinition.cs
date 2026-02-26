namespace Reloader.NPCs.Runtime
{
    public readonly struct NpcActionDefinition
    {
        public NpcActionDefinition(string actionId, string displayName, int priority, string payload = "")
        {
            ActionId = actionId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Priority = priority;
            Payload = payload ?? string.Empty;
        }

        public string ActionId { get; }
        public string ActionKey => ActionId;
        public string DisplayName { get; }
        public int Priority { get; }
        public string Payload { get; }
    }
}
