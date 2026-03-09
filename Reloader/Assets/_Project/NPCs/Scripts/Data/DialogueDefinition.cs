using System;
using System.Collections.Generic;
using Reloader.NPCs.Runtime.Dialogue;
using UnityEngine;

namespace Reloader.NPCs.Data
{
    [CreateAssetMenu(fileName = "DialogueDefinition", menuName = "Reloader/NPCs/Dialogue Definition")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        [SerializeField] private string _dialogueId = string.Empty;
        [SerializeField] private string _entryNodeId = string.Empty;
        [SerializeField] private DialogueNodeDefinition[] _nodes = Array.Empty<DialogueNodeDefinition>();

        public string DialogueId => _dialogueId ?? string.Empty;
        public string EntryNodeId => _entryNodeId ?? string.Empty;
        public DialogueNodeDefinition[] Nodes => _nodes ?? Array.Empty<DialogueNodeDefinition>();

        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(DialogueId))
            {
                reason = "dialogue.definition.missing-id";
                return false;
            }

            if (string.IsNullOrWhiteSpace(EntryNodeId))
            {
                reason = "dialogue.definition.missing-entry-node";
                return false;
            }

            if (Nodes.Length == 0)
            {
                reason = "dialogue.definition.missing-nodes";
                return false;
            }

            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            var hasEntryNode = false;
            for (var i = 0; i < Nodes.Length; i++)
            {
                var node = Nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
                {
                    reason = "dialogue.definition.invalid-node";
                    return false;
                }

                if (!nodeIds.Add(node.NodeId))
                {
                    reason = "dialogue.definition.duplicate-node-id";
                    return false;
                }

                if (string.Equals(node.NodeId, EntryNodeId, StringComparison.Ordinal))
                {
                    hasEntryNode = true;
                }

                if (!node.HasValidReplies())
                {
                    reason = "dialogue.definition.invalid-reply";
                    return false;
                }
            }

            if (!hasEntryNode)
            {
                reason = "dialogue.definition.missing-entry-node";
                return false;
            }

            for (var i = 0; i < Nodes.Length; i++)
            {
                var replies = Nodes[i].Replies;
                for (var replyIndex = 0; replyIndex < replies.Length; replyIndex++)
                {
                    var nextNodeId = replies[replyIndex].NextNodeId;
                    if (string.IsNullOrWhiteSpace(nextNodeId))
                    {
                        continue;
                    }

                    if (!nodeIds.Contains(nextNodeId))
                    {
                        reason = "dialogue.definition.invalid-next-node";
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
        }

        public bool TryGetEntryNode(out DialogueNodeDefinition node)
        {
            return TryGetNode(EntryNodeId, out node);
        }

        public bool TryGetNode(string nodeId, out DialogueNodeDefinition node)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                node = null;
                return false;
            }

            for (var i = 0; i < Nodes.Length; i++)
            {
                var candidate = Nodes[i];
                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.NodeId, nodeId, StringComparison.Ordinal))
                {
                    node = candidate;
                    return true;
                }
            }

            node = null;
            return false;
        }
    }
}
