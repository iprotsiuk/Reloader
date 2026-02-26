namespace Reloader.NPCs.Runtime
{
    public readonly struct NpcDecision
    {
        public NpcDecision(string actionId, string reason)
        {
            ActionId = actionId ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public string ActionId { get; }
        public string Reason { get; }
    }
}
