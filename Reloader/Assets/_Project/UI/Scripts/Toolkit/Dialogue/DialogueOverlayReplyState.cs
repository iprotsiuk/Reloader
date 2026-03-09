namespace Reloader.UI.Toolkit.Dialogue
{
    public readonly struct DialogueOverlayReplyState
    {
        public DialogueOverlayReplyState(string replyId, string text)
        {
            ReplyId = string.IsNullOrWhiteSpace(replyId) ? string.Empty : replyId;
            Text = string.IsNullOrWhiteSpace(text) ? string.Empty : text;
        }

        public string ReplyId { get; }
        public string Text { get; }
    }
}
