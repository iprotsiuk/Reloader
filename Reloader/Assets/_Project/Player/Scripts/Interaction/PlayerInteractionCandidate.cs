using System;

namespace Reloader.Player.Interaction
{
    public readonly struct PlayerInteractionCandidate
    {
        public PlayerInteractionCandidate(
            string contextId,
            string actionText,
            string subjectText,
            int priority,
            string stableTieBreaker,
            PlayerInteractionActionKind actionKind,
            Action execute)
        {
            ContextId = contextId ?? string.Empty;
            ActionText = actionText ?? string.Empty;
            SubjectText = subjectText ?? string.Empty;
            Priority = priority;
            StableTieBreaker = stableTieBreaker ?? string.Empty;
            ActionKind = actionKind;
            Execute = execute;
        }

        public string ContextId { get; }
        public string ActionText { get; }
        public string SubjectText { get; }
        public int Priority { get; }
        public string StableTieBreaker { get; }
        public PlayerInteractionActionKind ActionKind { get; }
        public Action Execute { get; }
    }
}
