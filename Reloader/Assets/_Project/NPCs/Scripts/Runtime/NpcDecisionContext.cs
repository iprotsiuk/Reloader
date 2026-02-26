namespace Reloader.NPCs.Runtime
{
    public readonly struct NpcDecisionContext
    {
        public NpcDecisionContext(string npcId, float timeSinceLastActionSeconds)
        {
            NpcId = npcId ?? string.Empty;
            TimeSinceLastActionSeconds = timeSinceLastActionSeconds;
        }

        public string NpcId { get; }
        public float TimeSinceLastActionSeconds { get; }
    }
}
