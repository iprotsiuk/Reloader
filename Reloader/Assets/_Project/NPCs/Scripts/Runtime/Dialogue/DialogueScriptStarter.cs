using UnityEngine;

namespace Reloader.NPCs.Runtime.Dialogue
{
    public static class DialogueScriptStarter
    {
        public static bool TryStartConversation(
            Component requester,
            Data.DialogueDefinition definition,
            Transform speakerTransform,
            out DialogueStartResult result,
            string payload = null,
            DialogueInterruptPolicy interruptPolicy = DialogueInterruptPolicy.DenyIfActive)
        {
            var request = new DialogueStartRequest(
                definition,
                speakerTransform,
                DialogueStartSourceKind.Script,
                payload,
                interruptPolicy);
            return DialogueOrchestrator.TryStartConversation(requester, request, out result);
        }
    }
}
