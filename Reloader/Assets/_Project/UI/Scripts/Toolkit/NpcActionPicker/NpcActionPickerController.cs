using System.Collections.Generic;
using Reloader.NPCs.Runtime;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.NpcActionPicker
{
    public sealed class NpcActionPickerController : MonoBehaviour, IUiController
    {
        private readonly List<NpcActionDefinition> _actions = new List<NpcActionDefinition>();
        private INpcActionPickerBridge _bridge;
        private NpcActionPickerViewBinder _viewBinder;
        private int _selectedIndex = -1;
        private string _lastResultText = string.Empty;

        private void OnEnable()
        {
            SubscribeBridge();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeBridge();
        }

        public void SetBridge(INpcActionPickerBridge bridge)
        {
            if (ReferenceEquals(_bridge, bridge))
            {
                return;
            }

            UnsubscribeBridge();
            _bridge = bridge;
            SubscribeBridge();
            Refresh();
        }

        public void SetViewBinder(NpcActionPickerViewBinder binder)
        {
            _viewBinder = binder;
            Refresh();
        }

        public void HandleIntent(UiIntent intent)
        {
            if (intent.Key == "npc.action.select" && intent.Payload is int selectedIndex)
            {
                if (selectedIndex >= 0 && selectedIndex < _actions.Count)
                {
                    _selectedIndex = selectedIndex;
                    Refresh();
                }

                return;
            }

            if (intent.Key != "npc.action.execute")
            {
                return;
            }

            if (_selectedIndex < 0 || _selectedIndex >= _actions.Count || _bridge == null)
            {
                return;
            }

            var selected = _actions[_selectedIndex];
            _bridge.RequestExecuteAction(selected.ActionKey, selected.Payload);
        }

        private void HandleAvailableActionsChanged(IReadOnlyList<NpcActionDefinition> actions, string selectedActionKey)
        {
            _actions.Clear();
            if (actions != null)
            {
                for (var i = 0; i < actions.Count; i++)
                {
                    _actions.Add(actions[i]);
                }
            }

            _selectedIndex = ResolveSelectedIndex(selectedActionKey);
            Refresh();
        }

        private void HandleActionExecuted(NpcActionExecutionResult result)
        {
            _lastResultText = result.Reason;
            Refresh();
        }

        private int ResolveSelectedIndex(string selectedActionKey)
        {
            if (!string.IsNullOrWhiteSpace(selectedActionKey))
            {
                for (var i = 0; i < _actions.Count; i++)
                {
                    if (string.Equals(_actions[i].ActionKey, selectedActionKey, System.StringComparison.Ordinal))
                    {
                        return i;
                    }
                }
            }

            return _actions.Count > 0 ? 0 : -1;
        }

        private void Refresh()
        {
            _viewBinder?.Render(new NpcActionPickerUiState(_actions, _selectedIndex, _lastResultText));
        }

        private void SubscribeBridge()
        {
            if (_bridge == null || !isActiveAndEnabled)
            {
                return;
            }

            _bridge.AvailableActionsChanged -= HandleAvailableActionsChanged;
            _bridge.ActionExecuted -= HandleActionExecuted;
            _bridge.AvailableActionsChanged += HandleAvailableActionsChanged;
            _bridge.ActionExecuted += HandleActionExecuted;
        }

        private void UnsubscribeBridge()
        {
            if (_bridge == null)
            {
                return;
            }

            _bridge.AvailableActionsChanged -= HandleAvailableActionsChanged;
            _bridge.ActionExecuted -= HandleActionExecuted;
        }
    }
}
