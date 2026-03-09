using System;
using NUnit.Framework;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.Dialogue;
using Reloader.UI.Toolkit.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class DialogueOverlayUiToolkitPlayModeTests
    {
        [Test]
        public void Controller_RendersSpeakerLineAndSelectedReply()
        {
            var go = new GameObject("DialogueOverlayController");
            var binder = new DialogueOverlayViewBinder();
            var root = BuildRoot();
            binder.Initialize(root);

            var bridge = go.AddComponent<TestDialogueOverlayBridge>();
            bridge.SetState(new DialogueOverlayRenderState(
                isVisible: true,
                speakerText: "Mechanic",
                lineText: "You need a cleaner way in.",
                replies: new[]
                {
                    new DialogueOverlayReplyState("reply.1", "Ask about the route"),
                    new DialogueOverlayReplyState("reply.2", "Ask about the glass"),
                },
                selectedReplyIndex: 1));

            var controller = go.AddComponent<DialogueOverlayController>();
            controller.SetBridge(bridge);
            controller.SetViewBinder(binder);

            var speaker = root.Q<Label>("dialogue-overlay__speaker");
            var line = root.Q<Label>("dialogue-overlay__line");
            var replies = root.Q<VisualElement>("dialogue-overlay__replies");
            Assert.That(speaker, Is.Not.Null);
            Assert.That(line, Is.Not.Null);
            Assert.That(replies, Is.Not.Null);

            Assert.That(speaker.text, Is.EqualTo("Mechanic"));
            Assert.That(line.text, Is.EqualTo("You need a cleaner way in."));
            Assert.That(replies.childCount, Is.EqualTo(2));
            Assert.That(replies[1].ClassListContains("is-selected"), Is.True);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_KeyboardIntents_MoveSelectionAndSubmit()
        {
            var go = new GameObject("DialogueOverlayKeyboard");
            var binder = new DialogueOverlayViewBinder();
            var root = BuildRoot();
            binder.Initialize(root);

            var bridge = go.AddComponent<TestDialogueOverlayBridge>();
            bridge.SetState(new DialogueOverlayRenderState(
                isVisible: true,
                speakerText: "Handler",
                lineText: "Choose carefully.",
                replies: new[]
                {
                    new DialogueOverlayReplyState("reply.1", "Take the quick job"),
                    new DialogueOverlayReplyState("reply.2", "Wait for a cleaner angle"),
                },
                selectedReplyIndex: 0));

            var controller = go.AddComponent<DialogueOverlayController>();
            controller.SetBridge(bridge);
            controller.SetViewBinder(binder);

            controller.HandleIntent(new UiIntent(UiRuntimeCompositionIds.IntentKeys.DialogueReplyNext));
            controller.HandleIntent(new UiIntent(UiRuntimeCompositionIds.IntentKeys.DialogueReplySubmit));

            Assert.That(bridge.LastSelectedIndex, Is.EqualTo(1));
            Assert.That(bridge.LastSubmittedReplyId, Is.EqualTo("reply.2"));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Binder_TestHookHoverRaisesSelectIntent()
        {
            var binder = new DialogueOverlayViewBinder();
            var root = BuildRoot();
            binder.Initialize(root);

            UiIntent raisedIntent = default;
            var intentRaised = false;
            binder.IntentRaised += intent =>
            {
                raisedIntent = intent;
                intentRaised = true;
            };

            binder.Render(new DialogueOverlayUiState(
                isVisible: true,
                speakerText: "Fixer",
                lineText: "Pick your question.",
                replies: new[]
                {
                    new DialogueOverlayReplyState("reply.1", "Question one"),
                    new DialogueOverlayReplyState("reply.2", "Question two"),
                },
                selectedReplyIndex: 0));

            var replies = root.Q<VisualElement>("dialogue-overlay__replies");
            Assert.That(replies, Is.Not.Null);
            Assert.That(replies.childCount, Is.EqualTo(2));

            var invoked = binder.TryInvokeReplyHoverForTests(1);

            Assert.That(invoked, Is.True);
            Assert.That(intentRaised, Is.True);
            Assert.That(raisedIntent.Key, Is.EqualTo(UiRuntimeCompositionIds.IntentKeys.DialogueReplySelect));
            Assert.That(raisedIntent.Payload, Is.EqualTo(1));
        }

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement();
            var overlay = new VisualElement { name = "dialogue-overlay__root" };
            overlay.Add(new Label { name = "dialogue-overlay__speaker" });
            overlay.Add(new Label { name = "dialogue-overlay__line" });
            overlay.Add(new VisualElement { name = "dialogue-overlay__replies" });
            root.Add(overlay);
            return root;
        }

        private sealed class TestDialogueOverlayBridge : MonoBehaviour, IDialogueOverlayBridge
        {
            private DialogueOverlayRenderState _state = DialogueOverlayRenderState.Hidden;

            public event Action StateChanged;

            public int LastSelectedIndex { get; private set; } = -1;
            public string LastSubmittedReplyId { get; private set; } = string.Empty;

            public bool TryGetState(out DialogueOverlayRenderState state)
            {
                state = _state;
                return _state.IsVisible;
            }

            public void MoveSelection(int delta)
            {
                if (_state.Replies.Count == 0)
                {
                    return;
                }

                var nextIndex = Mathf.Clamp(_state.SelectedReplyIndex + delta, 0, _state.Replies.Count - 1);
                SelectReply(nextIndex);
            }

            public void SelectReply(int replyIndex)
            {
                LastSelectedIndex = replyIndex;
                _state = new DialogueOverlayRenderState(
                    isVisible: _state.IsVisible,
                    speakerText: _state.SpeakerText,
                    lineText: _state.LineText,
                    replies: _state.Replies,
                    selectedReplyIndex: replyIndex);
                StateChanged?.Invoke();
            }

            public void SubmitSelectedReply()
            {
                if (_state.SelectedReplyIndex < 0 || _state.SelectedReplyIndex >= _state.Replies.Count)
                {
                    return;
                }

                LastSubmittedReplyId = _state.Replies[_state.SelectedReplyIndex].ReplyId;
            }

            public void SetState(DialogueOverlayRenderState state)
            {
                _state = state;
                StateChanged?.Invoke();
            }
        }
    }
}
