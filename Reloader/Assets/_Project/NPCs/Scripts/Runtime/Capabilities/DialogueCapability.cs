using System;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime.Dialogue;
using UnityEngine;

namespace Reloader.NPCs.Runtime.Capabilities
{
    public sealed class DialogueCapability : MonoBehaviour, INpcCapability, INpcActionProvider, INpcActionExecutor
    {
        public const string ActionKey = "npc.dialogue.interact";

        [SerializeField] private string _displayName = "Talk";
        [SerializeField] private int _priority = 0;
        [SerializeField] private DialogueDefinition _definition;

        private NpcAgent _agent;
        private int _interactionCount;
        private string _lastPayload = string.Empty;

        public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.Dialogue;
        public DialogueDefinition Definition => _definition;
        public int InteractionCount => _interactionCount;
        public string LastPayload => _lastPayload;

        public event Action<NpcActionExecutionResult> DialogueStarted;

        public void Initialize(NpcAgent agent)
        {
            _agent = agent;
        }

        public void Shutdown()
        {
            _agent = null;
        }

        public void ConfigureRuntimeDefinition(DialogueDefinition definition, string displayName = null)
        {
            _definition = definition;
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                _displayName = displayName;
            }
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

            var speakerTransform = DialoguePresentationResolver.ResolveFocusTarget(_agent != null ? _agent.transform : transform);
            var request = new DialogueStartRequest(
                _definition,
                speakerTransform,
                DialogueStartSourceKind.PlayerInteract,
                context.Payload,
                DialogueInterruptPolicy.DenyIfActive);
            if (!DialogueOrchestrator.TryStartConversation(_agent != null ? _agent : this, request, out var startResult))
            {
                result = new NpcActionExecutionResult(ActionKey, false, startResult.Reason);
                return false;
            }

            _interactionCount++;
            _lastPayload = context.Payload ?? string.Empty;
            var eventPayload = string.IsNullOrEmpty(_lastPayload)
                ? _definition.DialogueId
                : _definition.DialogueId + ":" + _lastPayload;

            result = new NpcActionExecutionResult(ActionKey, true, startResult.Reason, eventPayload);
            DialogueStarted?.Invoke(result);
            return true;
        }

        private bool HasValidDefinition()
        {
            return _definition != null && _definition.IsValid(out _);
        }
    }
}
