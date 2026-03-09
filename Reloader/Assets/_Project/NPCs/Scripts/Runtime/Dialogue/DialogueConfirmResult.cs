namespace Reloader.NPCs.Runtime.Dialogue
{
    public readonly struct DialogueConfirmResult
    {
        public DialogueConfirmResult(bool success, string reason, DialogueOutcome outcome)
        {
            Success = success;
            Reason = reason ?? string.Empty;
            Outcome = outcome;
        }

        public bool Success { get; }
        public string Reason { get; }
        public DialogueOutcome Outcome { get; }
    }
}
