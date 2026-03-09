using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.Runtime;
using UnityEngine;

namespace Reloader.UI.Toolkit.Dialogue
{
    public sealed class DialogueOverlayController : MonoBehaviour, IUiController
    {
        private IDialogueOverlayBridge _bridge;
        private DialogueOverlayViewBinder _viewBinder;

        private void OnEnable()
        {
            SubscribeBridge();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeBridge();
        }

        public void SetBridge(IDialogueOverlayBridge bridge)
        {
            if (ReferenceEquals(_bridge, bridge))
            {
                return;
            }

            UnsubscribeBridge();
            _bridge = bridge;
            SubscribeBridge();
            Refresh();
        }

        public void SetViewBinder(DialogueOverlayViewBinder binder)
        {
            _viewBinder = binder;
            Refresh();
        }

        public void HandleIntent(UiIntent intent)
        {
            if (_bridge == null)
            {
                return;
            }

            switch (intent.Key)
            {
                case UiRuntimeCompositionIds.IntentKeys.DialogueReplyPrevious:
                    _bridge.MoveSelection(-1);
                    break;
                case UiRuntimeCompositionIds.IntentKeys.DialogueReplyNext:
                    _bridge.MoveSelection(1);
                    break;
                case UiRuntimeCompositionIds.IntentKeys.DialogueReplySelect:
                    if (intent.Payload is int selectedIndex)
                    {
                        _bridge.SelectReply(selectedIndex);
                    }

                    break;
                case UiRuntimeCompositionIds.IntentKeys.DialogueReplySubmit:
                    if (intent.Payload is int submittedIndex)
                    {
                        _bridge.SelectReply(submittedIndex);
                    }

                    _bridge.SubmitSelectedReply();
                    break;
            }
        }

        private void Refresh()
        {
            if (_viewBinder == null)
            {
                return;
            }

            if (_bridge == null || !_bridge.TryGetState(out var state))
            {
                _viewBinder.Render(DialogueOverlayUiState.Hidden);
                return;
            }

            _viewBinder.Render(new DialogueOverlayUiState(
                state.IsVisible,
                state.SpeakerText,
                state.LineText,
                state.Replies,
                state.SelectedReplyIndex));
        }

        private void SubscribeBridge()
        {
            if (_bridge == null || !isActiveAndEnabled)
            {
                return;
            }

            _bridge.StateChanged -= HandleStateChanged;
            _bridge.StateChanged += HandleStateChanged;
        }

        private void UnsubscribeBridge()
        {
            if (_bridge == null)
            {
                return;
            }

            _bridge.StateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged()
        {
            Refresh();
        }
    }
}
