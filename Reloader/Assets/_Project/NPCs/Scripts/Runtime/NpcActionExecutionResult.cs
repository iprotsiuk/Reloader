namespace Reloader.NPCs.Runtime
{
    public readonly struct NpcActionExecutionResult
    {
        public NpcActionExecutionResult(string actionKey, bool success, string reason, string payload = "")
        {
            ActionKey = actionKey ?? string.Empty;
            Success = success;
            Reason = reason ?? string.Empty;
            Payload = payload ?? string.Empty;
        }

        public string ActionKey { get; }
        public bool Success { get; }
        public string Reason { get; }
        public string Payload { get; }
    }
}
