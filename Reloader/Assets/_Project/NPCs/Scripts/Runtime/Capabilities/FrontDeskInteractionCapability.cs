using System;
using UnityEngine;

namespace Reloader.NPCs.Runtime.Capabilities
{
    public sealed class FrontDeskInteractionCapability : MonoBehaviour, INpcCapability, INpcActionProvider, INpcActionExecutor
    {
        public const string ActionKey = "npc.front-desk.interact";

        [SerializeField] private string _displayName = "Front Desk";
        [SerializeField] private int _priority = 10;

        private int _requestCount;
        private string _lastPayload = string.Empty;

        public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.FrontDeskInteraction;
        public int RequestCount => _requestCount;
        public string LastPayload => _lastPayload;

        public event Action<NpcActionExecutionResult> RequestProcessed;

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
                result = new NpcActionExecutionResult(context.ActionKey, false, "front-desk.invalid-action");
                return false;
            }

            _requestCount++;
            _lastPayload = context.Payload ?? string.Empty;
            var responsePayload = string.IsNullOrEmpty(_lastPayload)
                ? "request.accepted"
                : "request.accepted:" + _lastPayload;

            result = new NpcActionExecutionResult(ActionKey, true, "front-desk.request-processed", responsePayload);
            RequestProcessed?.Invoke(result);
            return true;
        }
    }
}
