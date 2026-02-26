namespace Reloader.NPCs.Runtime
{
    public readonly struct NpcActionExecutionContext
    {
        public NpcActionExecutionContext(string actionKey, string payload = "")
        {
            ActionKey = actionKey ?? string.Empty;
            Payload = payload ?? string.Empty;
        }

        public string ActionKey { get; }
        public string Payload { get; }
    }
}
