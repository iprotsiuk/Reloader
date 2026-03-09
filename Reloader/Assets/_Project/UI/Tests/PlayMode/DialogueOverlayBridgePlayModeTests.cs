using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.UI.Toolkit.Dialogue;
using Reloader.UI.Toolkit.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class DialogueOverlayBridgePlayModeTests
    {
        [Test]
        public void RuntimeBridge_BindDialogueOverlay_RendersSceneDialogueState()
        {
            var bridgeGo = new GameObject("DialogueOverlayBridge");
            var bridge = bridgeGo.AddComponent<UiToolkitScreenRuntimeBridge>();
            var providerGo = new GameObject("DialogueOverlayProvider");
            var provider = providerGo.AddComponent<TestDialogueOverlayBridge>();
            provider.SetState(new DialogueOverlayRenderState(
                isVisible: true,
                speakerText: "Handler",
                lineText: "The target crosses the office window at dusk.",
                replies: new[]
                {
                    new DialogueOverlayReplyState("reply.1", "Ask about timing"),
                    new DialogueOverlayReplyState("reply.2", "Ask about witnesses"),
                },
                selectedReplyIndex: 0));

            var root = BuildRoot();
            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindDialogueOverlay",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var subscription = bindMethod.Invoke(
                bridge,
                new object[] { root, UiRuntimeCompositionIds.ControllerObjectNames.DialogueOverlay }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            var controller = bridgeGo.transform.Find(UiRuntimeCompositionIds.ControllerObjectNames.DialogueOverlay)?.GetComponent<DialogueOverlayController>();
            Assert.That(controller, Is.Not.Null);

            var speaker = root.Q<Label>("dialogue-overlay__speaker");
            var line = root.Q<Label>("dialogue-overlay__line");
            var replies = root.Q<VisualElement>("dialogue-overlay__replies");
            Assert.That(speaker, Is.Not.Null);
            Assert.That(line, Is.Not.Null);
            Assert.That(replies, Is.Not.Null);

            try
            {
                Assert.That(speaker.text, Is.EqualTo("Handler"));
                Assert.That(line.text, Is.EqualTo("The target crosses the office window at dusk."));
                Assert.That(replies.childCount, Is.EqualTo(2));

                controller.HandleIntent(new Reloader.UI.Toolkit.Contracts.UiIntent(UiRuntimeCompositionIds.IntentKeys.DialogueReplyNext));
                controller.HandleIntent(new Reloader.UI.Toolkit.Contracts.UiIntent(UiRuntimeCompositionIds.IntentKeys.DialogueReplySubmit));
                Assert.That(provider.LastSubmittedReplyId, Is.EqualTo("reply.2"));
            }
            finally
            {
                subscription.Dispose();
                UnityEngine.Object.DestroyImmediate(providerGo);
                UnityEngine.Object.DestroyImmediate(bridgeGo);
            }
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
                _state = new DialogueOverlayRenderState(
                    isVisible: _state.IsVisible,
                    speakerText: _state.SpeakerText,
                    lineText: _state.LineText,
                    replies: _state.Replies,
                    selectedReplyIndex: nextIndex);
                StateChanged?.Invoke();
            }

            public void SelectReply(int replyIndex)
            {
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
