using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Reloader.NPCs.Runtime
{
    public sealed class PlayerNpcInteractionUiBridge : MonoBehaviour, INpcActionPickerBridge
    {
        [SerializeField] private MonoBehaviour _interactionControllerSource;

        private object _interactionController;
        private EventInfo _interactionProcessedEvent;
        private Action<NpcActionExecutionResult> _interactionProcessedHandler;

        public event Action<IReadOnlyList<NpcActionDefinition>, string> AvailableActionsChanged;
        public event Action<NpcActionExecutionResult> ActionExecuted;
        public event Action<string, string> ExecuteActionRequested;

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeInteractionProcessed();
        }

        private void OnDisable()
        {
            UnsubscribeInteractionProcessed();
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
            ResolveReferences();
            if (_interactionController == null)
            {
                return;
            }

            var method = _interactionController.GetType().GetMethod("TryInteract", BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(_interactionController, new object[] { actionKey, payload });
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

            _interactionController = _interactionControllerSource != null
                ? _interactionControllerSource
                : GetComponent<MonoBehaviour>();
            _interactionProcessedEvent = _interactionController?.GetType().GetEvent("InteractionProcessed", BindingFlags.Instance | BindingFlags.Public);
            _interactionProcessedHandler = HandleInteractionProcessed;
        }

        private void SubscribeInteractionProcessed()
        {
            if (_interactionProcessedEvent == null || _interactionProcessedHandler == null)
            {
                return;
            }

            _interactionProcessedEvent.RemoveEventHandler(_interactionController, _interactionProcessedHandler);
            _interactionProcessedEvent.AddEventHandler(_interactionController, _interactionProcessedHandler);
        }

        private void UnsubscribeInteractionProcessed()
        {
            if (_interactionProcessedEvent == null || _interactionProcessedHandler == null)
            {
                return;
            }

            _interactionProcessedEvent.RemoveEventHandler(_interactionController, _interactionProcessedHandler);
        }
    }
}
