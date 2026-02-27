namespace Reloader.Core.Events
{
    public readonly struct InteractionHintPayload
    {
        public InteractionHintPayload(string contextId, string actionText, string subjectText = "")
        {
            ContextId = contextId ?? string.Empty;
            ActionText = actionText ?? string.Empty;
            SubjectText = subjectText ?? string.Empty;
        }

        public string ContextId { get; }
        public string ActionText { get; }
        public string SubjectText { get; }
    }
}
