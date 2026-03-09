using System;
using UnityEngine;

namespace Reloader.NPCs.Runtime.Dialogue
{
    [Serializable]
    public sealed class DialogueNodeDefinition
    {
        [SerializeField] private string _nodeId = string.Empty;
        [TextArea]
        [SerializeField] private string _speakerText = string.Empty;
        [SerializeField] private DialogueReplyDefinition[] _replies = Array.Empty<DialogueReplyDefinition>();

        public DialogueNodeDefinition(string nodeId, string speakerText, DialogueReplyDefinition[] replies)
        {
            _nodeId = nodeId ?? string.Empty;
            _speakerText = speakerText ?? string.Empty;
            _replies = replies ?? Array.Empty<DialogueReplyDefinition>();
        }

        public string NodeId => _nodeId ?? string.Empty;
        public string SpeakerText => _speakerText ?? string.Empty;
        public DialogueReplyDefinition[] Replies => _replies ?? Array.Empty<DialogueReplyDefinition>();

        public bool HasValidReplies()
        {
            if (Replies.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < Replies.Length; i++)
            {
                var reply = Replies[i];
                if (reply == null || !reply.IsValid())
                {
                    return false;
                }
            }

            return true;
        }
    }
}
