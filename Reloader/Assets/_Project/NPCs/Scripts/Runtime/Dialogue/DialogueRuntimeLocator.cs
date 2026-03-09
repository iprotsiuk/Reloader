using Reloader.NPCs.World;
using Reloader.Player;
using UnityEngine;

namespace Reloader.NPCs.Runtime.Dialogue
{
    public static class DialogueRuntimeLocator
    {
        public static DialogueRuntimeController EnsureRuntimeForPlayerHost(Component requester = null)
        {
            var runtime = Object.FindFirstObjectByType<DialogueRuntimeController>(FindObjectsInactive.Include);
            if (runtime != null)
            {
                EnsureConversationMode(runtime.gameObject);
                return runtime;
            }

            var host = ResolvePlayerHost(requester);
            if (host == null)
            {
                return null;
            }

            runtime = host.GetComponent<DialogueRuntimeController>();
            if (runtime == null)
            {
                runtime = host.AddComponent<DialogueRuntimeController>();
            }

            EnsureConversationMode(host);
            return runtime;
        }

        private static GameObject ResolvePlayerHost(Component requester)
        {
            var fromRequester = FindPlayerHostInHierarchy(requester != null ? requester.transform : null);
            if (fromRequester != null)
            {
                return fromRequester.gameObject;
            }

            var interactionController = Object.FindFirstObjectByType<PlayerNpcInteractionController>(FindObjectsInactive.Include);
            if (interactionController != null)
            {
                return interactionController.gameObject;
            }

            var cursorLockController = Object.FindFirstObjectByType<PlayerCursorLockController>(FindObjectsInactive.Include);
            if (cursorLockController != null)
            {
                return cursorLockController.gameObject;
            }

            var lookController = Object.FindFirstObjectByType<PlayerLookController>(FindObjectsInactive.Include);
            if (lookController != null)
            {
                return lookController.gameObject;
            }

            var mover = Object.FindFirstObjectByType<PlayerMover>(FindObjectsInactive.Include);
            if (mover != null)
            {
                return mover.gameObject;
            }

            var playerRoot = GameObject.Find("PlayerRoot");
            return playerRoot;
        }

        private static Transform FindPlayerHostInHierarchy(Transform current)
        {
            while (current != null)
            {
                if (current.GetComponent<PlayerNpcInteractionController>() != null
                    || current.GetComponent<PlayerCursorLockController>() != null
                    || current.GetComponent<PlayerLookController>() != null
                    || current.GetComponent<PlayerMover>() != null
                    || string.Equals(current.name, "PlayerRoot", System.StringComparison.Ordinal))
                {
                    return current;
                }

                current = current.parent;
            }

            return null;
        }

        private static void EnsureConversationMode(GameObject host)
        {
            if (host == null || host.GetComponent<DialogueConversationModeController>() != null)
            {
                return;
            }

            host.AddComponent<DialogueConversationModeController>();
        }
    }
}
