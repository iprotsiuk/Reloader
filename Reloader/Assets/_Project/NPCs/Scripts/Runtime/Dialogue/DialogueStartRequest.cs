using Reloader.NPCs.Data;
using UnityEngine;

namespace Reloader.NPCs.Runtime.Dialogue
{
    public readonly struct DialogueStartRequest
    {
        public DialogueStartRequest(
            DialogueDefinition definition,
            Transform speakerTransform,
            DialogueStartSourceKind sourceKind,
            string payload = null,
            DialogueInterruptPolicy interruptPolicy = DialogueInterruptPolicy.DenyIfActive)
        {
            Definition = definition;
            SpeakerTransform = speakerTransform;
            SourceKind = sourceKind;
            Payload = payload ?? string.Empty;
            InterruptPolicy = interruptPolicy;
        }

        public DialogueDefinition Definition { get; }
        public Transform SpeakerTransform { get; }
        public DialogueStartSourceKind SourceKind { get; }
        public string Payload { get; }
        public DialogueInterruptPolicy InterruptPolicy { get; }
    }
}
