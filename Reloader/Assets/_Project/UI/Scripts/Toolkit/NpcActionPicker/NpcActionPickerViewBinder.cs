using System;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.NpcActionPicker
{
    public sealed class NpcActionPickerViewBinder : IUiViewBinder
    {
        private VisualElement _list;
        private Label _selectedLabel;
        private Label _resultLabel;
        private Button _executeButton;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root)
        {
            _list = root?.Q<VisualElement>("npc-actions__list");
            _selectedLabel = root?.Q<Label>("npc-actions__selected");
            _resultLabel = root?.Q<Label>("npc-actions__result");
            _executeButton = root?.Q<Button>("npc-actions__execute");
            if (_executeButton != null)
            {
                _executeButton.clicked += RaiseExecuteIntent;
            }
        }

        public void Render(UiRenderState state)
        {
            if (state is not NpcActionPickerUiState pickerState)
            {
                return;
            }

            RenderList(pickerState);
            if (_selectedLabel != null)
            {
                if (pickerState.SelectedIndex >= 0 && pickerState.SelectedIndex < pickerState.Actions.Count)
                {
                    _selectedLabel.text = pickerState.Actions[pickerState.SelectedIndex].DisplayName;
                }
                else
                {
                    _selectedLabel.text = string.Empty;
                }
            }

            if (_resultLabel != null)
            {
                _resultLabel.text = pickerState.ResultText;
            }
        }

        private void RenderList(NpcActionPickerUiState state)
        {
            if (_list == null)
            {
                return;
            }

            _list.Clear();
            for (var i = 0; i < state.Actions.Count; i++)
            {
                var index = i;
                var button = new Button(() => IntentRaised?.Invoke(new UiIntent("npc.action.select", index)))
                {
                    text = state.Actions[i].DisplayName
                };

                button.EnableInClassList("is-selected", index == state.SelectedIndex);
                _list.Add(button);
            }
        }

        private void RaiseExecuteIntent()
        {
            IntentRaised?.Invoke(new UiIntent("npc.action.execute"));
        }
    }
}
