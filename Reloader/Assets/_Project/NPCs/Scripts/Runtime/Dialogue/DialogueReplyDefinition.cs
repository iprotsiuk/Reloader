using System;
using UnityEngine;

namespace Reloader.NPCs.Runtime.Dialogue
{
    [Serializable]
    public sealed class DialogueReplyDefinition
    {
        [SerializeField] private string _replyId = string.Empty;
        [SerializeField] private string _replyText = string.Empty;
        [SerializeField] private string _nextNodeId = string.Empty;
        [SerializeField] private string _outcomeActionId = string.Empty;
        [SerializeField] private string _outcomePayload = string.Empty;

        public DialogueReplyDefinition(string replyId, string replyText, string nextNodeId, string outcomeActionId, string outcomePayload)
        {
            _replyId = replyId ?? string.Empty;
            _replyText = replyText ?? string.Empty;
            _nextNodeId = nextNodeId ?? string.Empty;
            _outcomeActionId = outcomeActionId ?? string.Empty;
            _outcomePayload = outcomePayload ?? string.Empty;
        }

        public string ReplyId => _replyId ?? string.Empty;
        public string ReplyText => _replyText ?? string.Empty;
        public string NextNodeId => _nextNodeId ?? string.Empty;
        public string OutcomeActionId => _outcomeActionId ?? string.Empty;
        public string OutcomePayload => _outcomePayload ?? string.Empty;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ReplyId)
                && !string.IsNullOrWhiteSpace(ReplyText);
        }
    }
}
