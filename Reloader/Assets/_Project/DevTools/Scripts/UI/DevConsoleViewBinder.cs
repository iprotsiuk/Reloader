using System;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.DevConsole
{
    public sealed class DevConsoleViewBinder : IUiViewBinder
    {
        private VisualElement _screenRoot;
        private VisualElement _panelRoot;
        private Label _promptLabel;
        private TextField _commandField;
        private VisualElement _suggestionsRoot;
        private Label _statusLabel;
        private bool _wasVisibleLastRender;

        public event Action<UiIntent> IntentRaised;
        public VisualElement Root => _screenRoot;

        public void Initialize(VisualElement root)
        {
            _screenRoot = root?.Q<VisualElement>("dev-console__screen") ?? root;
            _panelRoot = root?.Q<VisualElement>("dev-console__panel") ?? _screenRoot;
            _promptLabel = root?.Q<Label>("dev-console__prompt");
            _commandField = root?.Q<TextField>("dev-console__command");
            _suggestionsRoot = root?.Q<VisualElement>("dev-console__suggestions");
            _statusLabel = root?.Q<Label>("dev-console__status");
        }

        public string GetCommandText()
        {
            return _commandField?.value ?? string.Empty;
        }

        public void SetCommandText(string text)
        {
            _commandField?.SetValueWithoutNotify(text ?? string.Empty);
        }

        public void Render(UiRenderState state)
        {
            if (state is not DevConsoleUiState consoleState)
            {
                return;
            }

            var becameVisible = consoleState.IsVisible && !_wasVisibleLastRender;

            if (_promptLabel != null)
            {
                _promptLabel.text = consoleState.PromptText;
            }

            if (_commandField != null)
            {
                _commandField.SetValueWithoutNotify(consoleState.InputText);
            }

            if (_statusLabel != null)
            {
                _statusLabel.text = consoleState.StatusText;
            }

            if (_suggestionsRoot != null)
            {
                _suggestionsRoot.Clear();
                for (var i = 0; i < consoleState.Suggestions.Count; i++)
                {
                    var suggestion = consoleState.Suggestions[i];
                    var row = new Label(suggestion.Label)
                    {
                        name = $"dev-console__suggestion-{i}"
                    };
                    row.AddToClassList("dev-console__suggestion");
                    if (i == consoleState.HighlightedSuggestionIndex)
                    {
                        row.AddToClassList("dev-console__suggestion--selected");
                    }

                    _suggestionsRoot.Add(row);
                }

                _suggestionsRoot.style.display = consoleState.IsVisible && consoleState.Suggestions.Count > 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_screenRoot != null)
            {
                _screenRoot.style.display = consoleState.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
                _screenRoot.pickingMode = consoleState.IsVisible ? PickingMode.Position : PickingMode.Ignore;
            }

            if (_panelRoot != null)
            {
                _panelRoot.style.display = consoleState.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (becameVisible && _commandField != null)
            {
                _commandField.Focus();
            }

            _wasVisibleLastRender = consoleState.IsVisible;
        }
    }
}
