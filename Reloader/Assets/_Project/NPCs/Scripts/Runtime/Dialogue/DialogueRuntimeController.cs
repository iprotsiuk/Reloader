using Reloader.NPCs.Data;
using UnityEngine;
using System;

namespace Reloader.NPCs.Runtime.Dialogue
{
    public sealed class DialogueRuntimeController : MonoBehaviour
    {
        private DialogueConversationState _activeConversation;
        private DialogueOutcome _lastOutcome;
        private string _lastCloseReason = string.Empty;
        private int _conversationSessionId;

        public bool HasActiveConversation => _activeConversation != null;
        public DialogueConversationState ActiveConversation => _activeConversation;
        public DialogueOutcome LastOutcome => _lastOutcome;
        public string LastCloseReason => _lastCloseReason;
        public int ConversationSessionId => _conversationSessionId;
        public event Action<DialogueConfirmResult> DialogueConfirmed;

        public bool TryOpenConversation(DialogueDefinition definition, Transform speakerTransform, out string reason)
        {
            if (definition == null || !definition.IsValid(out _))
            {
                reason = "dialogue.invalid-definition";
                return false;
            }

            if (!IsSpeakerValid(speakerTransform))
            {
                reason = "dialogue.invalid-speaker";
                return false;
            }

            if (!definition.TryGetEntryNode(out var entryNode) || entryNode == null)
            {
                reason = "dialogue.invalid-definition";
                return false;
            }

            _lastCloseReason = string.Empty;
            _conversationSessionId++;
            _activeConversation = new DialogueConversationState(definition, entryNode, speakerTransform, 0);
            reason = "dialogue.started";
            return true;
        }

        public bool TrySelectReply(int replyIndex)
        {
            if (!HasActiveConversation)
            {
                return false;
            }

            var replies = _activeConversation.CurrentNode.Replies;
            if (replyIndex < 0 || replyIndex >= replies.Length)
            {
                return false;
            }

            _activeConversation = new DialogueConversationState(
                _activeConversation.Definition,
                _activeConversation.CurrentNode,
                _activeConversation.SpeakerTransform,
                replyIndex);
            return true;
        }

        public bool TryConfirmSelectedReply(out DialogueConfirmResult result)
        {
            RefreshActiveConversationState();
            if (!HasActiveConversation)
            {
                result = new DialogueConfirmResult(false, "dialogue.no-active-conversation", default);
                return false;
            }

            var selectedReply = _activeConversation.SelectedReply;
            if (selectedReply == null)
            {
                CloseConversation("dialogue.invalid-state");
                result = new DialogueConfirmResult(false, "dialogue.invalid-state", default);
                return false;
            }

            var outcome = new DialogueOutcome(
                selectedReply.ReplyId,
                selectedReply.OutcomeActionId,
                selectedReply.OutcomePayload,
                selectedReply.NextNodeId);
            _lastOutcome = outcome;

            if (!string.IsNullOrWhiteSpace(selectedReply.NextNodeId))
            {
                if (_activeConversation.Definition.TryGetNode(selectedReply.NextNodeId, out var nextNode) && nextNode != null)
                {
                    _activeConversation = new DialogueConversationState(
                        _activeConversation.Definition,
                        nextNode,
                        _activeConversation.SpeakerTransform,
                        0);
                    result = new DialogueConfirmResult(true, "dialogue.confirmed", outcome);
                    DialogueConfirmed?.Invoke(result);
                    return true;
                }

                CloseConversation("dialogue.invalid-state");
                result = new DialogueConfirmResult(false, "dialogue.invalid-state", outcome);
                return false;
            }

            CloseConversation("dialogue.confirmed");
            result = new DialogueConfirmResult(true, "dialogue.confirmed", outcome);
            DialogueConfirmed?.Invoke(result);
            return true;
        }

        public void RefreshActiveConversationState()
        {
            if (!HasActiveConversation)
            {
                return;
            }

            if (IsSpeakerValid(_activeConversation.SpeakerTransform))
            {
                return;
            }

            CloseConversation("dialogue.target-invalid");
        }

        public void CloseConversation(string reason = "dialogue.closed")
        {
            _activeConversation = null;
            _lastCloseReason = reason ?? string.Empty;
        }

        private void LateUpdate()
        {
            RefreshActiveConversationState();
        }

        private static bool IsSpeakerValid(Transform speakerTransform)
        {
            return speakerTransform != null
                && speakerTransform.gameObject != null
                && speakerTransform.gameObject.activeInHierarchy;
        }
    }
}
