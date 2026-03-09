using System;
using Reloader.NPCs.Runtime.Dialogue;
using Reloader.Player;
using UnityEngine;

namespace Reloader.UI.Toolkit.Dialogue
{
    public sealed class DialogueRuntimeOverlayBridge : MonoBehaviour, IDialogueOverlayBridge
    {
        private const float NavigationThreshold = 0.5f;

        [SerializeField] private MonoBehaviour _runtimeControllerSource;
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;

        private DialogueRuntimeController _runtimeController;
        private IPlayerInputSource _inputSource;
        private DialogueOverlayRenderState _lastState = DialogueOverlayRenderState.Hidden;
        private int _lastVerticalDirection;
        private int _observedConversationSessionId = -1;

        public event Action StateChanged;

        private void OnEnable()
        {
            ResolveReferences();
            PublishIfStateChanged(force: true);
        }

        private void OnDisable()
        {
            _lastVerticalDirection = 0;
            _observedConversationSessionId = -1;
            PublishIfStateChanged(force: true);
        }

        private void Update()
        {
            ResolveReferences();
            HandleKeyboardInput();
            PublishIfStateChanged();
        }

        public bool TryGetState(out DialogueOverlayRenderState state)
        {
            state = BuildState();
            return state.IsVisible;
        }

        public void MoveSelection(int delta)
        {
            if (_runtimeController == null || !_runtimeController.HasActiveConversation)
            {
                return;
            }

            var activeConversation = _runtimeController.ActiveConversation;
            var replyCount = activeConversation?.CurrentNode?.Replies?.Length ?? 0;
            if (replyCount == 0)
            {
                return;
            }

            var selectedIndex = Mathf.Clamp(activeConversation.SelectedReplyIndex + delta, 0, replyCount - 1);
            if (_runtimeController.TrySelectReply(selectedIndex))
            {
                PublishIfStateChanged(force: true);
            }
        }

        public void SelectReply(int replyIndex)
        {
            if (_runtimeController == null || !_runtimeController.HasActiveConversation)
            {
                return;
            }

            if (_runtimeController.TrySelectReply(replyIndex))
            {
                PublishIfStateChanged(force: true);
            }
        }

        public void SubmitSelectedReply()
        {
            if (_runtimeController == null)
            {
                return;
            }

            if (_runtimeController.TryConfirmSelectedReply(out _))
            {
                PublishIfStateChanged(force: true);
            }
        }

        private void HandleKeyboardInput()
        {
            if (_inputSource == null || _runtimeController == null || !_runtimeController.HasActiveConversation)
            {
                _lastVerticalDirection = 0;
                _observedConversationSessionId = -1;
                return;
            }

            if (_runtimeController.ConversationSessionId != _observedConversationSessionId)
            {
                _observedConversationSessionId = _runtimeController.ConversationSessionId;
                _lastVerticalDirection = 0;
                _inputSource.ConsumePickupPressed();
                return;
            }

            var vertical = _inputSource.MoveInput.y;
            var direction = 0;
            if (vertical >= NavigationThreshold)
            {
                direction = 1;
            }
            else if (vertical <= -NavigationThreshold)
            {
                direction = -1;
            }

            if (direction != 0 && direction != _lastVerticalDirection)
            {
                MoveSelection(direction > 0 ? -1 : 1);
            }

            _lastVerticalDirection = direction;

            if (_inputSource.ConsumePickupPressed())
            {
                SubmitSelectedReply();
            }
        }

        private DialogueOverlayRenderState BuildState()
        {
            if (_runtimeController == null || !_runtimeController.HasActiveConversation)
            {
                return DialogueOverlayRenderState.Hidden;
            }

            var conversation = _runtimeController.ActiveConversation;
            var node = conversation?.CurrentNode;
            var replies = node?.Replies ?? Array.Empty<DialogueReplyDefinition>();
            var replyStates = new DialogueOverlayReplyState[replies.Length];
            for (var i = 0; i < replies.Length; i++)
            {
                replyStates[i] = new DialogueOverlayReplyState(replies[i].ReplyId, replies[i].ReplyText);
            }

            var speakerText = conversation?.SpeakerTransform != null
                ? conversation.SpeakerTransform.name
                : string.Empty;

            return new DialogueOverlayRenderState(
                isVisible: true,
                speakerText: speakerText,
                lineText: node?.SpeakerText ?? string.Empty,
                replies: replyStates,
                selectedReplyIndex: conversation != null ? conversation.SelectedReplyIndex : -1);
        }

        private void PublishIfStateChanged(bool force = false)
        {
            var currentState = BuildState();
            if (!force && AreEquivalent(_lastState, currentState))
            {
                return;
            }

            _lastState = currentState;
            StateChanged?.Invoke();
        }

        private void ResolveReferences()
        {
            if (_runtimeControllerSource is DialogueRuntimeController explicitRuntime)
            {
                _runtimeController = explicitRuntime;
            }
            else
            {
                var resolvedRuntime = DialogueRuntimeLocator.FindRuntimeForPlayerHost(this);
                if (resolvedRuntime != null)
                {
                    _runtimeController = resolvedRuntime;
                }
            }

            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
            _inputSource ??= GetComponent<IPlayerInputSource>();
            if (_inputSource == null)
            {
                _inputSource = FindFirstObjectByType<PlayerInputReader>(FindObjectsInactive.Include);
            }
        }

        private static bool AreEquivalent(DialogueOverlayRenderState left, DialogueOverlayRenderState right)
        {
            if (left == null || right == null)
            {
                return ReferenceEquals(left, right);
            }

            if (left.IsVisible != right.IsVisible
                || !string.Equals(left.SpeakerText, right.SpeakerText, StringComparison.Ordinal)
                || !string.Equals(left.LineText, right.LineText, StringComparison.Ordinal)
                || left.SelectedReplyIndex != right.SelectedReplyIndex
                || left.Replies.Count != right.Replies.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Replies.Count; i++)
            {
                if (!string.Equals(left.Replies[i].ReplyId, right.Replies[i].ReplyId, StringComparison.Ordinal)
                    || !string.Equals(left.Replies[i].Text, right.Replies[i].Text, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
