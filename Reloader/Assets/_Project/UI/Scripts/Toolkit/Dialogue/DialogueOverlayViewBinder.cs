using System;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.Runtime;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.Dialogue
{
    public sealed class DialogueOverlayViewBinder : IUiViewBinder
    {
        private VisualElement _screenRoot;
        private VisualElement _root;
        private Label _speakerLabel;
        private Label _lineLabel;
        private VisualElement _repliesRoot;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root)
        {
            _screenRoot = root?.Q<VisualElement>("dialogue-overlay__screen") ?? root;
            _root = root?.Q<VisualElement>("dialogue-overlay__root") ?? _screenRoot;
            _speakerLabel = root?.Q<Label>("dialogue-overlay__speaker");
            _lineLabel = root?.Q<Label>("dialogue-overlay__line");
            _repliesRoot = root?.Q<VisualElement>("dialogue-overlay__replies");
        }

        public void Render(UiRenderState state)
        {
            if (state is not DialogueOverlayUiState dialogueState)
            {
                return;
            }

            if (_speakerLabel != null)
            {
                _speakerLabel.text = dialogueState.SpeakerText;
            }

            if (_lineLabel != null)
            {
                _lineLabel.text = dialogueState.LineText;
            }

            RenderReplies(dialogueState);

            if (_screenRoot != null)
            {
                _screenRoot.style.display = dialogueState.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
                _screenRoot.pickingMode = dialogueState.IsVisible ? PickingMode.Position : PickingMode.Ignore;
            }

            if (_root != null)
            {
                _root.style.display = dialogueState.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public bool TryInvokeReplyHoverForTests(int replyIndex)
        {
            if (_repliesRoot == null || replyIndex < 0 || replyIndex >= _repliesRoot.childCount)
            {
                return false;
            }

            IntentRaised?.Invoke(new UiIntent(UiRuntimeCompositionIds.IntentKeys.DialogueReplySelect, replyIndex));
            return true;
        }

        private void RenderReplies(DialogueOverlayUiState state)
        {
            if (_repliesRoot == null)
            {
                return;
            }

            _repliesRoot.Clear();
            for (var i = 0; i < state.Replies.Count; i++)
            {
                var replyIndex = i;
                var button = new Button(() =>
                {
                    IntentRaised?.Invoke(new UiIntent(UiRuntimeCompositionIds.IntentKeys.DialogueReplySubmit, replyIndex));
                })
                {
                    text = state.Replies[i].Text,
                    name = $"dialogue-overlay__reply-{i}"
                };

                button.AddToClassList("dialogue-overlay__reply");
                button.EnableInClassList("is-selected", replyIndex == state.SelectedReplyIndex);
                button.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    IntentRaised?.Invoke(new UiIntent(UiRuntimeCompositionIds.IntentKeys.DialogueReplySelect, replyIndex));
                });
                button.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    IntentRaised?.Invoke(new UiIntent(UiRuntimeCompositionIds.IntentKeys.DialogueReplySelect, replyIndex));
                });
                button.RegisterCallback<PointerOverEvent>(_ =>
                {
                    IntentRaised?.Invoke(new UiIntent(UiRuntimeCompositionIds.IntentKeys.DialogueReplySelect, replyIndex));
                });

                _repliesRoot.Add(button);
            }
        }
    }
}
