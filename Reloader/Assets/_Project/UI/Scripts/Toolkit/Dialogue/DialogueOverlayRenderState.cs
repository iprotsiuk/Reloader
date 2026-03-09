using System;
using System.Collections.Generic;

namespace Reloader.UI.Toolkit.Dialogue
{
    public sealed class DialogueOverlayRenderState
    {
        private static readonly IReadOnlyList<DialogueOverlayReplyState> EmptyReplies = Array.Empty<DialogueOverlayReplyState>();

        public static readonly DialogueOverlayRenderState Hidden = new(false, string.Empty, string.Empty, EmptyReplies, -1);

        public DialogueOverlayRenderState(
            bool isVisible,
            string speakerText,
            string lineText,
            IReadOnlyList<DialogueOverlayReplyState> replies,
            int selectedReplyIndex)
        {
            IsVisible = isVisible;
            SpeakerText = speakerText ?? string.Empty;
            LineText = lineText ?? string.Empty;
            Replies = replies ?? EmptyReplies;
            SelectedReplyIndex = Replies.Count == 0
                ? -1
                : Math.Clamp(selectedReplyIndex, 0, Replies.Count - 1);
        }

        public bool IsVisible { get; }
        public string SpeakerText { get; }
        public string LineText { get; }
        public IReadOnlyList<DialogueOverlayReplyState> Replies { get; }
        public int SelectedReplyIndex { get; }
    }
}
