using System;
using System.Collections.Generic;
using Reloader.NPCs.World;
using UnityEngine;

namespace Reloader.NPCs.Runtime
{
    public sealed class PlayerNpcInteractionUiBridge : MonoBehaviour, INpcActionPickerBridge
    {
        [SerializeField] private MonoBehaviour _interactionControllerSource;

        private PlayerNpcInteractionController _interactionController;

        public event Action<IReadOnlyList<NpcActionDefinition>, string> AvailableActionsChanged;
        public event Action<NpcActionExecutionResult> ActionExecuted;
        public event Action<string, string> ExecuteActionRequested;

        private void OnEnable()
        {
            EnsureControllerSubscription();
        }

        private void OnDisable()
        {
            if (_interactionController != null)
            {
                _interactionController.InteractionProcessed -= HandleInteractionProcessed;
            }
        }

        public void PublishAvailableActions(IEnumerable<NpcActionDefinition> actions, string selectedActionKey = "")
        {
            IReadOnlyList<NpcActionDefinition> actionList = actions == null
                ? Array.Empty<NpcActionDefinition>()
                : new List<NpcActionDefinition>(actions);
            AvailableActionsChanged?.Invoke(actionList, selectedActionKey ?? string.Empty);
        }

        public void PublishExecutionResult(NpcActionExecutionResult result)
        {
            ActionExecuted?.Invoke(result);
        }

        public void RequestExecuteAction(string actionKey, string payload)
        {
            if (string.IsNullOrWhiteSpace(actionKey))
            {
                return;
            }

            ExecuteActionRequested?.Invoke(actionKey, payload ?? string.Empty);
            EnsureControllerSubscription();
            if (_interactionController == null)
            {
                return;
            }

            _interactionController.TryInteract(actionKey, payload);
        }

        public void PublishAvailableActionsFromCollection(NpcActionCollection actions, string selectedActionKey = "")
        {
            var list = new List<NpcActionDefinition>(actions.Count);
            for (var i = 0; i < actions.Count; i++)
            {
                list.Add(actions[i]);
            }

            PublishAvailableActions(list, selectedActionKey);
        }

        private void HandleInteractionProcessed(NpcActionExecutionResult result)
        {
            ActionExecuted?.Invoke(result);
        }

        private void ResolveReferences()
        {
            if (_interactionController != null)
            {
                return;
            }

            _interactionController = _interactionControllerSource as PlayerNpcInteractionController;
            _interactionController ??= GetComponent<PlayerNpcInteractionController>();
            _interactionController ??= GetComponentInParent<PlayerNpcInteractionController>(true);
            _interactionController ??= GetComponentInChildren<PlayerNpcInteractionController>(true);
        }

        private void EnsureControllerSubscription()
        {
            ResolveReferences();
            if (_interactionController == null)
            {
                return;
            }

            _interactionController.InteractionProcessed -= HandleInteractionProcessed;
            _interactionController.InteractionProcessed += HandleInteractionProcessed;
        }
    }
}
