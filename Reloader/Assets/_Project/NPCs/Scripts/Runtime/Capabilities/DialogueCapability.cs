using System;
using UnityEngine;

namespace Reloader.NPCs.Runtime.Capabilities
{
    public sealed class DialogueCapability : MonoBehaviour, INpcCapability, INpcActionProvider, INpcActionExecutor
    {
        public const string ActionKey = "npc.dialogue.interact";

        [SerializeField] private string _displayName = "Talk";
        [SerializeField] private int _priority = 0;

        private int _interactionCount;
        private string _lastPayload = string.Empty;

        public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.Dialogue;
        public int InteractionCount => _interactionCount;
        public string LastPayload => _lastPayload;

        public event Action<NpcActionExecutionResult> DialogueStarted;

        public void Initialize(NpcAgent agent)
        {
        }

        public void Shutdown()
        {
        }

        public NpcActionDefinition[] GetActions()
        {
            return new[]
            {
                new NpcActionDefinition(ActionKey, _displayName, _priority)
            };
        }

        public bool CanExecuteAction(string actionKey)
        {
            return string.Equals(actionKey, ActionKey, StringComparison.Ordinal);
        }

        public bool TryExecuteAction(in NpcActionExecutionContext context, out NpcActionExecutionResult result)
        {
            if (!CanExecuteAction(context.ActionKey))
            {
                result = new NpcActionExecutionResult(context.ActionKey, false, "dialogue.invalid-action");
                return false;
            }

            _interactionCount++;
            _lastPayload = context.Payload ?? string.Empty;
            var eventPayload = string.IsNullOrEmpty(_lastPayload)
                ? "dialogue.started"
                : "dialogue.started:" + _lastPayload;

            result = new NpcActionExecutionResult(ActionKey, true, "dialogue.started", eventPayload);
            DialogueStarted?.Invoke(result);
            return true;
        }
    }
}
