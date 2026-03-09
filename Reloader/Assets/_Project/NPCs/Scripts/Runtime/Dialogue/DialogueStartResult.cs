namespace Reloader.NPCs.Runtime.Dialogue
{
    public readonly struct DialogueStartResult
    {
        public DialogueStartResult(
            bool success,
            string reason,
            DialogueStartSourceKind sourceKind,
            DialogueRuntimeController runtimeController)
        {
            Success = success;
            Reason = reason ?? string.Empty;
            SourceKind = sourceKind;
            RuntimeController = runtimeController;
        }

        public bool Success { get; }
        public string Reason { get; }
        public DialogueStartSourceKind SourceKind { get; }
        public DialogueRuntimeController RuntimeController { get; }
    }
}
