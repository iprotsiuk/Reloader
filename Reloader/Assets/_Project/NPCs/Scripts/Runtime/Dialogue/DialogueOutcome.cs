namespace Reloader.NPCs.Runtime.Dialogue
{
    public readonly struct DialogueOutcome
    {
        public DialogueOutcome(string replyId, string actionId, string payload, string nextNodeId)
        {
            ReplyId = replyId ?? string.Empty;
            ActionId = actionId ?? string.Empty;
            Payload = payload ?? string.Empty;
            NextNodeId = nextNodeId ?? string.Empty;
        }

        public string ReplyId { get; }
        public string ActionId { get; }
        public string Payload { get; }
        public string NextNodeId { get; }
    }
}
