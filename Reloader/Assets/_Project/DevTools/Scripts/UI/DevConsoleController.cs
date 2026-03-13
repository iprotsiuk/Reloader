using System;
using System.Collections.Generic;
using System.Reflection;
using Reloader.Core.Runtime;
using Reloader.DevTools.Runtime;
using Reloader.Player;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.DevConsole
{
    public sealed class DevConsoleController : MonoBehaviour, IUiController
    {
        private static readonly PropertyInfo CommandFieldCursorIndexProperty = typeof(TextField).GetProperty(
            "cursorIndex",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly PropertyInfo CommandFieldSelectIndexProperty = typeof(TextField).GetProperty(
            "selectIndex",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private sealed class KeyboardDevConsoleKeySource : IDevConsoleKeySource
        {
            public bool ConsumeCancelPressed()
            {
                return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            }

            public bool ConsumeSubmitPressed()
            {
                return Keyboard.current != null
                    && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
            }
        }

        private DevConsoleViewBinder _viewBinder;
        private DevToolsRuntime _runtime;
        private IPlayerInputSource _inputSource;
        private IUiStateEvents _uiStateEvents;
        private IDevConsoleKeySource _consoleKeySource;
        private bool _useRuntimeKernelUiStateEvents = true;
        private bool _ownsRuntime;
        private int _highlightedSuggestionIndex;
        private IReadOnlyList<DevConsoleSuggestion> _suggestions = Array.Empty<DevConsoleSuggestion>();
        private string _statusText = string.Empty;
        private IUiStateEvents _lastPublishedUiStateEvents;
        private bool? _lastPublishedVisibility;

        private void OnEnable()
        {
            SubscribeToRuntimeHubReconfigure();
            _consoleKeySource ??= new KeyboardDevConsoleKeySource();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            if (_runtime != null && _runtime.IsConsoleVisible)
            {
                SetConsoleOpen(false);
            }
        }

        private void OnDestroy()
        {
            DisposeOwnedRuntime();
        }

        private void Update()
        {
            Tick();
        }

        public void Configure(IUiStateEvents uiStateEvents = null)
        {
            _useRuntimeKernelUiStateEvents = uiStateEvents == null;
            _uiStateEvents = uiStateEvents;
        }

        public void SetRuntime(DevToolsRuntime runtime)
        {
            if (ReferenceEquals(_runtime, runtime))
            {
                return;
            }

            DisposeOwnedRuntime();
            _runtime = runtime;
            _ownsRuntime = false;
            Refresh();
        }

        public void SetInputSource(IPlayerInputSource inputSource)
        {
            _inputSource = inputSource;
        }

        public void SetConsoleKeySource(IDevConsoleKeySource consoleKeySource)
        {
            _consoleKeySource = consoleKeySource;
        }

        public void SetViewBinder(DevConsoleViewBinder binder)
        {
            _viewBinder = binder;
            Refresh();
        }

        public void Tick()
        {
            EnsureRuntime();
            if (_runtime == null || _inputSource == null)
            {
                return;
            }

            if (_runtime.IsConsoleVisible && _consoleKeySource != null && _consoleKeySource.ConsumeCancelPressed())
            {
                PlayerCursorLockController.MarkEscapeConsumedThisFrame();
                SetConsoleOpen(false);
                return;
            }

            if (_runtime.IsConsoleVisible)
            {
                var commandText = _viewBinder?.GetCommandText() ?? string.Empty;
                RefreshSuggestions(commandText);

                var suggestionDelta = _inputSource.ConsumeSuggestionDelta();
                if (suggestionDelta != 0)
                {
                    var maxIndex = Mathf.Max(0, _suggestions.Count - 1);
                    _highlightedSuggestionIndex = Mathf.Clamp(_highlightedSuggestionIndex + suggestionDelta, 0, maxIndex);
                    RefreshSuggestions(commandText);
                }

                if (_inputSource.ConsumeAutocompletePressed() && _suggestions.Count > 0)
                {
                    commandText = ApplyHighlightedSuggestion();
                    MoveCaretToEndOfLine(commandText);
                }

                if (_consoleKeySource != null && _consoleKeySource.ConsumeSubmitPressed())
                {
                    if (TryAcceptHighlightedSuggestionOnSubmit(commandText, out var acceptedCommandText))
                    {
                        commandText = acceptedCommandText;
                        Refresh();
                        MoveCaretToEndOfLine(commandText);
                        return;
                    }

                    _runtime.TryExecute(commandText, out _statusText);
                    RefreshSuggestions(commandText);
                }

                Refresh();
            }

            if (_inputSource.ConsumeDevConsoleTogglePressed())
            {
                SetConsoleOpen(!_runtime.IsConsoleVisible);
            }
        }

        public void HandleIntent(UiIntent intent)
        {
        }

        private void SetConsoleOpen(bool isVisible)
        {
            _runtime.SetConsoleVisible(isVisible);
            _highlightedSuggestionIndex = 0;
            _statusText = string.Empty;
            if (isVisible)
            {
                PlayerCursorLockController.MarkEscapeConsumedThisFrame();
            }

            Refresh();
        }

        private void Refresh()
        {
            if (_viewBinder == null)
            {
                return;
            }

            if (_runtime == null)
            {
                _viewBinder.Render(DevConsoleUiState.Hidden);
                return;
            }

            var commandText = _viewBinder.GetCommandText();
            if (_runtime.IsConsoleVisible)
            {
                RefreshSuggestions(commandText);
            }
            else
            {
                _suggestions = Array.Empty<DevConsoleSuggestion>();
            }

            PublishVisibilityIfNeeded();

            _viewBinder.Render(new DevConsoleUiState(
                _runtime.IsConsoleVisible,
                promptText: ">",
                inputText: commandText,
                statusText: _runtime.IsConsoleVisible
                    ? (string.IsNullOrWhiteSpace(_statusText) ? "Developer tools active" : _statusText)
                    : string.Empty,
                suggestions: _suggestions,
                highlightedSuggestionIndex: _highlightedSuggestionIndex));
        }

        private void RefreshSuggestions(string commandText)
        {
            _suggestions = _runtime.GetSuggestions(commandText, _highlightedSuggestionIndex) ?? Array.Empty<DevConsoleSuggestion>();
            if (_suggestions.Count == 0)
            {
                _highlightedSuggestionIndex = 0;
                return;
            }

            _highlightedSuggestionIndex = Mathf.Clamp(_highlightedSuggestionIndex, 0, _suggestions.Count - 1);
        }

        private string ApplyHighlightedSuggestion()
        {
            if (_suggestions.Count == 0)
            {
                return _viewBinder?.GetCommandText() ?? string.Empty;
            }

            var selected = _suggestions[Mathf.Clamp(_highlightedSuggestionIndex, 0, _suggestions.Count - 1)];
            var acceptedText = selected.ApplyText;
            _viewBinder?.SetCommandText(acceptedText);
            RefreshSuggestions(acceptedText);
            return acceptedText;
        }

        private bool TryAcceptHighlightedSuggestionOnSubmit(string commandText, out string acceptedCommandText)
        {
            acceptedCommandText = commandText ?? string.Empty;
            if (_suggestions.Count == 0)
            {
                return false;
            }

            var selected = _suggestions[Mathf.Clamp(_highlightedSuggestionIndex, 0, _suggestions.Count - 1)];
            if (string.IsNullOrWhiteSpace(selected.ApplyText)
                || string.Equals(selected.ApplyText, acceptedCommandText, StringComparison.Ordinal))
            {
                return false;
            }

            acceptedCommandText = ApplyHighlightedSuggestion();
            return true;
        }

        private void MoveCaretToEndOfLine(string commandText)
        {
            var commandField = _viewBinder?.Root?.Q<TextField>("dev-console__command");
            if (commandField == null)
            {
                return;
            }

            var endOfLine = commandText?.Length ?? 0;
            commandField.Focus();
            SetIntProperty(CommandFieldCursorIndexProperty, commandField, endOfLine);
            SetIntProperty(CommandFieldSelectIndexProperty, commandField, endOfLine);
        }

        private static void SetIntProperty(PropertyInfo property, object target, int value)
        {
            if (property?.CanWrite != true || target == null)
            {
                return;
            }

            property.SetValue(target, value);
        }

        private IUiStateEvents ResolveUiStateEvents()
        {
            return _useRuntimeKernelUiStateEvents
                ? RuntimeKernelBootstrapper.UiStateEvents
                : _uiStateEvents;
        }

        private void SubscribeToRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            RuntimeKernelBootstrapper.EventsReconfigured += HandleRuntimeEventsReconfigured;
        }

        private void UnsubscribeFromRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled || !_useRuntimeKernelUiStateEvents)
            {
                return;
            }

            _uiStateEvents = RuntimeKernelBootstrapper.UiStateEvents;
            _lastPublishedUiStateEvents = null;
            Refresh();
        }

        private void PublishVisibilityIfNeeded()
        {
            var uiStateEvents = ResolveUiStateEvents();
            if (uiStateEvents == null || _runtime == null)
            {
                return;
            }

            var isVisible = _runtime.IsConsoleVisible;
            if (ReferenceEquals(_lastPublishedUiStateEvents, uiStateEvents)
                && _lastPublishedVisibility == isVisible)
            {
                return;
            }

            uiStateEvents.RaiseDevConsoleVisibilityChanged(isVisible);
            _lastPublishedUiStateEvents = uiStateEvents;
            _lastPublishedVisibility = isVisible;
        }

        private void EnsureRuntime()
        {
            if (_runtime != null)
            {
                return;
            }

            _runtime = new DevToolsRuntime();
            _ownsRuntime = true;
        }

        private void DisposeOwnedRuntime()
        {
            if (!_ownsRuntime || _runtime == null)
            {
                return;
            }

            _runtime.Dispose();
            _runtime = null;
            _ownsRuntime = false;
        }
    }

    public interface IDevConsoleKeySource
    {
        bool ConsumeCancelPressed();
        bool ConsumeSubmitPressed();
    }
}
