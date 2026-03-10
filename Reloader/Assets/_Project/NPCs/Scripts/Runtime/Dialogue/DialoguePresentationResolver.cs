using UnityEngine;

namespace Reloader.NPCs.Runtime.Dialogue
{
    public static class DialoguePresentationResolver
    {
        public static string ResolveSpeakerDisplayName(Transform speakerTransform)
        {
            if (TryResolveProvider(speakerTransform, out var provider)
                && !string.IsNullOrWhiteSpace(provider.DialogueSpeakerDisplayName))
            {
                return provider.DialogueSpeakerDisplayName.Trim();
            }

            return speakerTransform != null ? speakerTransform.name : string.Empty;
        }

        public static Transform ResolveFocusTarget(Transform speakerTransform)
        {
            if (TryResolveProvider(speakerTransform, out var provider))
            {
                var focusTarget = provider.ResolveDialogueFocusTarget();
                if (focusTarget != null)
                {
                    return focusTarget;
                }
            }

            return speakerTransform;
        }

        private static bool TryResolveProvider(Transform speakerTransform, out IDialoguePresentationProvider provider)
        {
            provider = null;
            if (speakerTransform == null)
            {
                return false;
            }

            provider = speakerTransform.GetComponent<IDialoguePresentationProvider>();
            provider ??= speakerTransform.GetComponentInParent<IDialoguePresentationProvider>();
            return provider != null;
        }
    }
}
