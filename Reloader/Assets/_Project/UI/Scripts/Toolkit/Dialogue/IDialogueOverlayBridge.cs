using System;

namespace Reloader.UI.Toolkit.Dialogue
{
    public interface IDialogueOverlayBridge
    {
        event Action StateChanged;

        bool TryGetState(out DialogueOverlayRenderState state);

        void MoveSelection(int delta);

        void SelectReply(int replyIndex);

        void SubmitSelectedReply();
    }
}
