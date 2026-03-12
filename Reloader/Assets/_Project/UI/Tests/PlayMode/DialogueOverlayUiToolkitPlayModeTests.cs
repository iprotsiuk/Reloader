using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Events;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime.Dialogue;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.Dialogue;
using Reloader.UI.Toolkit.Runtime;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.UI.Tests.PlayMode
{
    public class DialogueOverlayUiToolkitPlayModeTests
    {
        private const string FrontDeskDialogueAssetPath = "Assets/_Project/NPCs/Data/Definitions/Dialogue_FrontDeskClerk.asset";
        private const string PoliceDialogueAssetPath = "Assets/_Project/NPCs/Data/Definitions/Dialogue_PoliceStop.asset";

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

        [Test]
        public void Binder_HiddenState_DisablesFullscreenOverlayHitTarget()
        {
            var binder = new DialogueOverlayViewBinder();
            var root = BuildRoot();
            binder.Initialize(root);

            binder.Render(new DialogueOverlayUiState(
                isVisible: false,
                speakerText: string.Empty,
                lineText: string.Empty,
                replies: Array.Empty<DialogueOverlayReplyState>(),
                selectedReplyIndex: -1));

            var screen = root.Q<VisualElement>("dialogue-overlay__screen");
            var overlay = root.Q<VisualElement>("dialogue-overlay__root");
            Assert.That(screen, Is.Not.Null);
            Assert.That(overlay, Is.Not.Null);
            Assert.That(screen.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(screen.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(overlay.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

#if UNITY_EDITOR
        [Test]
        public void RenderedReplyButtonClick_SubmitsAuthoredDialogueOutcomeThroughRealRuntimeBridge()
        {
            var dialogueAsset = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(FrontDeskDialogueAssetPath);
            Assert.That(dialogueAsset, Is.Not.Null, $"Expected dialogue asset at {FrontDeskDialogueAssetPath}.");

            var runtimeGo = new GameObject("DialogueRuntime");
            var runtime = runtimeGo.AddComponent<DialogueRuntimeController>();
            var speaker = new GameObject("FrontDeskSpeaker");
            var inputGo = new GameObject("DialogueInput");
            var input = inputGo.AddComponent<TestInputSource>();

            var bridgeGo = new GameObject("DialogueOverlayBridge");
            var bridge = bridgeGo.AddComponent<DialogueRuntimeOverlayBridge>();
            SetField(bridge, "_runtimeControllerSource", runtime);
            SetField(bridge, "_inputSourceBehaviour", input);

            var screenBridgeGo = new GameObject("UiToolkitScreenRuntimeBridge");
            var screenBridge = screenBridgeGo.AddComponent<UiToolkitScreenRuntimeBridge>();
            var root = BuildRoot();

            try
            {
                Assert.That(runtime.TryOpenConversation(dialogueAsset, speaker.transform, out var reason), Is.True, reason);

                var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                    "BindDialogueOverlay",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(bindMethod, Is.Not.Null);

                var subscription = bindMethod.Invoke(
                    screenBridge,
                    new object[] { root, UiRuntimeCompositionIds.ControllerObjectNames.DialogueOverlay }) as IDisposable;
                Assert.That(subscription, Is.Not.Null);

                var replies = root.Q<VisualElement>("dialogue-overlay__replies");
                Assert.That(replies, Is.Not.Null);
                Assert.That(replies.childCount, Is.EqualTo(3));

                var button = replies.Q<Button>("dialogue-overlay__reply-2");
                Assert.That(button, Is.Not.Null);
                TriggerButtonClick(button);

                Assert.That(runtime.HasActiveConversation, Is.False);
                Assert.That(runtime.LastOutcome.ReplyId, Is.EqualTo("reply.frontdesk.leave"));
                Assert.That(runtime.LastOutcome.ActionId, Is.EqualTo("dialogue.frontdesk.exit"));
                Assert.That(runtime.LastOutcome.Payload, Is.EqualTo("leave"));

                subscription.Dispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(screenBridgeGo);
                UnityEngine.Object.DestroyImmediate(bridgeGo);
                UnityEngine.Object.DestroyImmediate(inputGo);
                UnityEngine.Object.DestroyImmediate(speaker);
                UnityEngine.Object.DestroyImmediate(runtimeGo);
            }
        }

        [Test]
        public void RenderedReplyButtonClick_PoliceLeaveOutcomeEscalatesSharedContractRuntime()
        {
            var dialogueAsset = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(PoliceDialogueAssetPath);
            Assert.That(dialogueAsset, Is.Not.Null, $"Expected dialogue asset at {PoliceDialogueAssetPath}.");

            var providerGo = new GameObject("ContractProvider");
            var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();

            var runtimeGo = new GameObject("DialogueRuntime");
            var runtime = runtimeGo.AddComponent<DialogueRuntimeController>();
            runtimeGo.AddComponent<DialogueOutcomeContractRuntimeBridge>();

            var speaker = new GameObject("PoliceSpeaker");
            var inputGo = new GameObject("DialogueInput");
            var input = inputGo.AddComponent<TestInputSource>();

            var bridgeGo = new GameObject("DialogueOverlayBridge");
            var bridge = bridgeGo.AddComponent<DialogueRuntimeOverlayBridge>();
            SetField(bridge, "_runtimeControllerSource", runtime);
            SetField(bridge, "_inputSourceBehaviour", input);

            var screenBridgeGo = new GameObject("UiToolkitScreenRuntimeBridge");
            var screenBridge = screenBridgeGo.AddComponent<UiToolkitScreenRuntimeBridge>();
            var root = BuildRoot();

            try
            {
                Assert.That(runtime.TryOpenConversation(dialogueAsset, speaker.transform, out var reason), Is.True, reason);

                var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                    "BindDialogueOverlay",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(bindMethod, Is.Not.Null);

                var subscription = bindMethod.Invoke(
                    screenBridge,
                    new object[] { root, UiRuntimeCompositionIds.ControllerObjectNames.DialogueOverlay }) as IDisposable;
                Assert.That(subscription, Is.Not.Null);

                var replies = root.Q<VisualElement>("dialogue-overlay__replies");
                Assert.That(replies, Is.Not.Null);
                Assert.That(replies.childCount, Is.EqualTo(3));

                var button = replies.Q<Button>("dialogue-overlay__reply-2");
                Assert.That(button, Is.Not.Null, "Expected the authored leave option as the third reply.");
                TriggerButtonClick(button);

                var providerRuntime = ReadContractRuntime(provider);
                Assert.That(providerRuntime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
                Assert.That(providerRuntime.CurrentHeatState.LastCrimeType, Is.EqualTo(CrimeType.Fleeing));
                Assert.That(runtime.LastOutcome.ActionId, Is.EqualTo("police.stop.leave"));

                subscription.Dispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(screenBridgeGo);
                UnityEngine.Object.DestroyImmediate(bridgeGo);
                UnityEngine.Object.DestroyImmediate(inputGo);
                UnityEngine.Object.DestroyImmediate(speaker);
                UnityEngine.Object.DestroyImmediate(runtimeGo);
                UnityEngine.Object.DestroyImmediate(providerGo);
            }
        }
#endif

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement();
            var screen = new VisualElement { name = "dialogue-overlay__screen" };
            var overlay = new VisualElement { name = "dialogue-overlay__root" };
            overlay.Add(new Label { name = "dialogue-overlay__speaker" });
            overlay.Add(new Label { name = "dialogue-overlay__line" });
            overlay.Add(new VisualElement { name = "dialogue-overlay__replies" });
            screen.Add(overlay);
            root.Add(screen);
            return root;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static void TriggerButtonClick(Button button)
        {
            var clickableField = typeof(Button).GetField("m_Clickable", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(clickableField, Is.Not.Null);

            var clickable = clickableField.GetValue(button);
            Assert.That(clickable, Is.Not.Null);

            var clickedField = clickable.GetType().GetField("clicked", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(clickedField, Is.Not.Null);

            var clicked = clickedField.GetValue(clickable) as Action;
            Assert.That(clicked, Is.Not.Null);
            clicked.Invoke();
        }

        private static ContractEscapeResolutionRuntime ReadContractRuntime(StaticContractRuntimeProvider provider)
        {
            var field = typeof(StaticContractRuntimeProvider).GetField("_runtime", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Expected StaticContractRuntimeProvider runtime backing field.");
            var value = field.GetValue(provider) as ContractEscapeResolutionRuntime;
            Assert.That(value, Is.Not.Null, "Expected provider runtime to be initialized.");
            return value!;
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

        private sealed class TestInputSource : MonoBehaviour, Reloader.Player.IPlayerInputSource
        {
            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public bool ConsumePickupPressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeDevConsoleTogglePressed() => false;
            public bool ConsumeAutocompletePressed() => false;
            public int ConsumeSuggestionDelta() => 0;
        }
    }
}
