using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.Runtime;

namespace Reloader.UI.Toolkit.Dialogue
{
    public sealed class DialogueOverlayUiState : UiRenderState
    {
        public static readonly DialogueOverlayUiState Hidden = new(false, string.Empty, string.Empty, System.Array.Empty<DialogueOverlayReplyState>(), -1);

        public DialogueOverlayUiState(
            bool isVisible,
            string speakerText,
            string lineText,
            System.Collections.Generic.IReadOnlyList<DialogueOverlayReplyState> replies,
            int selectedReplyIndex)
            : base(UiRuntimeCompositionIds.ScreenIds.DialogueOverlay)
        {
            IsVisible = isVisible;
            SpeakerText = speakerText ?? string.Empty;
            LineText = lineText ?? string.Empty;
            Replies = replies ?? System.Array.Empty<DialogueOverlayReplyState>();
            SelectedReplyIndex = Replies.Count == 0
                ? -1
                : System.Math.Clamp(selectedReplyIndex, 0, Replies.Count - 1);
        }

        public bool IsVisible { get; }
        public string SpeakerText { get; }
        public string LineText { get; }
        public System.Collections.Generic.IReadOnlyList<DialogueOverlayReplyState> Replies { get; }
        public int SelectedReplyIndex { get; }
    }
}
