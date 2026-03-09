using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime.Dialogue;
using Reloader.NPCs.World;
using Reloader.Player;
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

        [Test]
        public void RuntimeBridge_RebindDialogueOverlay_DoesNotLeakProviderStateChangedSubscriptions()
        {
            var bridgeGo = new GameObject("DialogueOverlayBridge");
            var bridge = bridgeGo.AddComponent<UiToolkitScreenRuntimeBridge>();
            var providerGo = new GameObject("DialogueOverlayProvider");
            var provider = providerGo.AddComponent<TestDialogueOverlayBridge>();
            provider.SetState(new DialogueOverlayRenderState(
                isVisible: true,
                speakerText: "Handler",
                lineText: "Keep moving.",
                replies: new[]
                {
                    new DialogueOverlayReplyState("reply.1", "First"),
                },
                selectedReplyIndex: 0));

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindDialogueOverlay",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var firstRoot = BuildRoot();
            var secondRoot = BuildRoot();
            IDisposable firstSubscription = null;
            IDisposable secondSubscription = null;

            try
            {
                firstSubscription = bindMethod.Invoke(
                    bridge,
                    new object[] { firstRoot, UiRuntimeCompositionIds.ControllerObjectNames.DialogueOverlay }) as IDisposable;
                Assert.That(firstSubscription, Is.Not.Null);
                Assert.That(provider.ListenerCountForTests, Is.EqualTo(1));

                firstSubscription.Dispose();
                Assert.That(provider.ListenerCountForTests, Is.EqualTo(0));

                secondSubscription = bindMethod.Invoke(
                    bridge,
                    new object[] { secondRoot, UiRuntimeCompositionIds.ControllerObjectNames.DialogueOverlay }) as IDisposable;
                Assert.That(secondSubscription, Is.Not.Null);
                Assert.That(provider.ListenerCountForTests, Is.EqualTo(1));
            }
            finally
            {
                secondSubscription?.Dispose();
                firstSubscription?.Dispose();
                UnityEngine.Object.DestroyImmediate(providerGo);
                UnityEngine.Object.DestroyImmediate(bridgeGo);
            }
        }

        [Test]
        public void RuntimeOverlayBridge_WithForeignRuntimePresent_UsesPlayerHostRuntime()
        {
            var foreignGo = new GameObject("ForeignRuntime");
            var foreignRuntime = foreignGo.AddComponent<DialogueRuntimeController>();
            var foreignSpeaker = new GameObject("ForeignSpeaker");

            var playerRoot = new GameObject("PlayerRoot");
            playerRoot.AddComponent<PlayerNpcInteractionController>();
            var playerRuntime = playerRoot.AddComponent<DialogueRuntimeController>();
            var playerSpeaker = new GameObject("PlayerSpeaker");

            var bridgeGo = new GameObject("DialogueRuntimeOverlayBridge");
            var bridge = bridgeGo.AddComponent<DialogueRuntimeOverlayBridge>();

            var foreignDefinition = CreateDefinition(
                "dialogue.foreign",
                "entry",
                new DialogueNodeDefinition("entry", "Wrong runtime.", new[]
                {
                    new DialogueReplyDefinition("reply.foreign", "Ignore", string.Empty, string.Empty, string.Empty)
                }));
            var playerDefinition = CreateDefinition(
                "dialogue.player",
                "entry",
                new DialogueNodeDefinition("entry", "Correct runtime.", new[]
                {
                    new DialogueReplyDefinition("reply.player", "Continue", string.Empty, string.Empty, string.Empty)
                }));

            try
            {
                Assert.That(foreignRuntime.TryOpenConversation(foreignDefinition, foreignSpeaker.transform, out var foreignReason), Is.True, foreignReason);
                Assert.That(playerRuntime.TryOpenConversation(playerDefinition, playerSpeaker.transform, out var playerReason), Is.True, playerReason);

                Assert.That(bridge.TryGetState(out var state), Is.True);
                Assert.That(state.LineText, Is.EqualTo("Correct runtime."));
                Assert.That(state.SpeakerText, Is.EqualTo("PlayerSpeaker"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerDefinition);
                UnityEngine.Object.DestroyImmediate(foreignDefinition);
                UnityEngine.Object.DestroyImmediate(bridgeGo);
                UnityEngine.Object.DestroyImmediate(playerSpeaker);
                UnityEngine.Object.DestroyImmediate(playerRoot);
                UnityEngine.Object.DestroyImmediate(foreignSpeaker);
                UnityEngine.Object.DestroyImmediate(foreignGo);
            }
        }

        [Test]
        public void RuntimeOverlayBridge_FirstObservedFrame_DoesNotConsumeQueuedPickupPress()
        {
            var runtimeGo = new GameObject("DialogueRuntime");
            var runtime = runtimeGo.AddComponent<DialogueRuntimeController>();
            var speaker = new GameObject("DialogueSpeaker");
            var inputGo = new GameObject("DialogueInput");
            var input = inputGo.AddComponent<TestInputSource>();
            input.PickupPressed = true;

            var bridgeGo = new GameObject("DialogueRuntimeOverlayBridge");
            var bridge = bridgeGo.AddComponent<DialogueRuntimeOverlayBridge>();
            SetPrivateField(bridge, "_runtimeControllerSource", runtime);
            SetPrivateField(bridge, "_inputSourceBehaviour", input);

            var definition = CreateDefinition(
                "dialogue.input-guard",
                "entry",
                new DialogueNodeDefinition("entry", "Do not skip.", new[]
                {
                    new DialogueReplyDefinition("reply.ok", "Continue", string.Empty, string.Empty, string.Empty)
                }));

            try
            {
                Assert.That(runtime.TryOpenConversation(definition, speaker.transform, out var reason), Is.True, reason);

                InvokePrivateUpdate(bridge);

                Assert.That(runtime.HasActiveConversation, Is.True);
                Assert.That(runtime.ActiveConversation.CurrentNode.NodeId, Is.EqualTo("entry"));
                Assert.That(runtime.LastOutcome.ReplyId, Is.Null.Or.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(bridgeGo);
                UnityEngine.Object.DestroyImmediate(inputGo);
                UnityEngine.Object.DestroyImmediate(speaker);
                UnityEngine.Object.DestroyImmediate(runtimeGo);
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

        private static DialogueDefinition CreateDefinition(string dialogueId, string entryNodeId, params DialogueNodeDefinition[] nodes)
        {
            var definition = ScriptableObject.CreateInstance<DialogueDefinition>();
            SetPrivateField(definition, "_dialogueId", dialogueId);
            SetPrivateField(definition, "_entryNodeId", entryNodeId);
            SetPrivateField(definition, "_nodes", nodes);
            return definition;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static void InvokePrivateUpdate(object target)
        {
            var method = target.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected private Update on {target.GetType().Name}.");
            method.Invoke(target, null);
        }

        private sealed class TestDialogueOverlayBridge : MonoBehaviour, IDialogueOverlayBridge
        {
            private DialogueOverlayRenderState _state = DialogueOverlayRenderState.Hidden;

            public event Action StateChanged;

            public string LastSubmittedReplyId { get; private set; } = string.Empty;
            public int ListenerCountForTests => StateChanged?.GetInvocationList().Length ?? 0;

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

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool PickupPressed { get; set; }
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public bool ConsumePickupPressed()
            {
                var pressed = PickupPressed;
                PickupPressed = false;
                return pressed;
            }
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
        }
    }
}
