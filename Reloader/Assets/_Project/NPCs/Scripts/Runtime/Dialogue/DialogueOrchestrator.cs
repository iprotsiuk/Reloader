using UnityEngine;

namespace Reloader.NPCs.Runtime.Dialogue
{
    public static class DialogueOrchestrator
    {
        public static bool TryStartConversation(Component requester, in DialogueStartRequest request, out DialogueStartResult result)
        {
            var runtime = DialogueRuntimeLocator.EnsureRuntimeForPlayerHost(requester);
            if (runtime == null)
            {
                result = new DialogueStartResult(false, "dialogue.runtime-missing", request.SourceKind, null);
                return false;
            }

            if (runtime.HasActiveConversation)
            {
                if (request.InterruptPolicy == DialogueInterruptPolicy.DenyIfActive)
                {
                    result = new DialogueStartResult(false, "dialogue.conversation-already-active", request.SourceKind, runtime);
                    return false;
                }

                runtime.CloseConversation("dialogue.interrupted");
            }

            if (!runtime.TryOpenConversation(request.Definition, request.SpeakerTransform, out var reason))
            {
                result = new DialogueStartResult(false, reason, request.SourceKind, runtime);
                return false;
            }

            result = new DialogueStartResult(true, reason, request.SourceKind, runtime);
            return true;
        }
    }
}
