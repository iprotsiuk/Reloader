using Reloader.NPCs.Data;
using UnityEngine;

namespace Reloader.NPCs.Runtime.Dialogue
{
    public sealed class DialogueConversationState
    {
        public DialogueConversationState(DialogueDefinition definition, DialogueNodeDefinition currentNode, Transform speakerTransform, int selectedReplyIndex)
        {
            Definition = definition;
            CurrentNode = currentNode;
            SpeakerTransform = speakerTransform;
            SelectedReplyIndex = selectedReplyIndex;
        }

        public DialogueDefinition Definition { get; }
        public DialogueNodeDefinition CurrentNode { get; }
        public Transform SpeakerTransform { get; }
        public int SelectedReplyIndex { get; }

        public DialogueReplyDefinition SelectedReply
        {
            get
            {
                var replies = CurrentNode != null ? CurrentNode.Replies : null;
                if (replies == null || replies.Length == 0)
                {
                    return null;
                }

                var clampedIndex = SelectedReplyIndex;
                if (clampedIndex < 0 || clampedIndex >= replies.Length)
                {
                    clampedIndex = 0;
                }

                return replies[clampedIndex];
            }
        }
    }
}
