using UnityEngine;

namespace Reloader.NPCs.Runtime.Dialogue
{
    public interface IDialoguePresentationProvider
    {
        string DialogueSpeakerDisplayName { get; }

        Transform ResolveDialogueFocusTarget();
    }
}
