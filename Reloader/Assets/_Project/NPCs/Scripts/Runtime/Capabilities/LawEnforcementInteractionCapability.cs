using System;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime.Dialogue;
using UnityEngine;

namespace Reloader.NPCs.Runtime.Capabilities
{
    public sealed class LawEnforcementInteractionCapability : MonoBehaviour, INpcCapability, INpcActionProvider, INpcActionExecutor
    {
        public const string ActionKey = "npc.law-enforcement.interact";

        [SerializeField] private string _displayName = "Question";
        [SerializeField] private int _priority = 20;
        [SerializeField] private DialogueDefinition _definition;

        private NpcAgent _agent;

        public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.LawEnforcementInteraction;

        public void Initialize(NpcAgent agent)
        {
            _agent = agent;
        }

        public void Shutdown()
        {
            _agent = null;
        }

        public NpcActionDefinition[] GetActions()
        {
            if (!HasValidDefinition())
            {
                return Array.Empty<NpcActionDefinition>();
            }

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

            if (!HasValidDefinition())
            {
                result = new NpcActionExecutionResult(ActionKey, false, "dialogue.invalid-definition");
                return false;
            }

            var runtime = UnityEngine.Object.FindFirstObjectByType<DialogueRuntimeController>(FindObjectsInactive.Include);
            if (runtime == null)
            {
                result = new NpcActionExecutionResult(ActionKey, false, "dialogue.runtime-missing");
                return false;
            }

            if (!runtime.TryOpenConversation(_definition, _agent != null ? _agent.transform : transform, out var reason))
            {
                result = new NpcActionExecutionResult(ActionKey, false, reason);
                return false;
            }

            result = new NpcActionExecutionResult(ActionKey, true, reason, _definition.DialogueId);
            return true;
        }

        private bool HasValidDefinition()
        {
            return _definition != null && _definition.IsValid(out _);
        }
    }
}
