using System;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.DevConsole
{
    public sealed class DevConsoleViewBinder : IUiViewBinder
    {
        private static readonly StyleColor ConsoleTextColor = new(Color.black);
        private static readonly StyleColor ConsolePanelBackgroundColor = new(new Color(0.94f, 0.96f, 0.97f, 0.97f));

        private VisualElement _screenRoot;
        private VisualElement _panelRoot;
        private Label _promptLabel;
        private TextField _commandField;
        private VisualElement _suggestionsRoot;
        private Label _statusLabel;
        private bool _suppressOpeningBackquoteOnNextKeyDown;
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
            _commandField?.RegisterCallback<KeyDownEvent>(HandleCommandFieldKeyDown, TrickleDown.TrickleDown);
            ApplyTheme();
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

                    row.style.color = ConsoleTextColor;

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

            if (!consoleState.IsVisible)
            {
                _suppressOpeningBackquoteOnNextKeyDown = false;
            }

            ApplyTheme();

            if (becameVisible && _commandField != null)
            {
                _suppressOpeningBackquoteOnNextKeyDown = true;
                _commandField.Focus();
            }

            _wasVisibleLastRender = consoleState.IsVisible;
        }

        private void ApplyTheme()
        {
            if (_panelRoot != null)
            {
                _panelRoot.style.backgroundColor = ConsolePanelBackgroundColor;
            }

            ApplyTextColor(_promptLabel);
            ApplyTextColor(_commandField);
            ApplyTextColor(_commandField?.Q(className: "unity-text-input"));
            ApplyTextColor(_statusLabel);
        }

        private static void ApplyTextColor(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            element.style.color = ConsoleTextColor;
        }

        private void HandleCommandFieldKeyDown(KeyDownEvent keyDownEvent)
        {
            if (!_suppressOpeningBackquoteOnNextKeyDown || keyDownEvent == null)
            {
                return;
            }

            _suppressOpeningBackquoteOnNextKeyDown = false;
            if (keyDownEvent.keyCode != KeyCode.BackQuote && keyDownEvent.character != '`')
            {
                return;
            }

            _commandField?.SetValueWithoutNotify(string.Empty);
            keyDownEvent.StopPropagation();
            keyDownEvent.StopImmediatePropagation();
            keyDownEvent.PreventDefault();
        }
    }
}
