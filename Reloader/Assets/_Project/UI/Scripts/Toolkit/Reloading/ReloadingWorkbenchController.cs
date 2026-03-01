using System.Collections.Generic;
using Reloader.Core.Runtime;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.Reloading
{
    public sealed class ReloadingWorkbenchController : MonoBehaviour, IUiController
    {
        [SerializeField] private string[] _operationLabels = { "Resize", "Prime", "Seat" };
        [SerializeField] private bool[] _operationEnabled = { true, false, false };
        [SerializeField] private string[] _operationBlockedDiagnostics =
        {
            string.Empty,
            "Prime blocked: Missing die mount in setup mode.",
            "Seat blocked: Missing seat die mount in setup mode."
        };
        [SerializeField] private string[] _setupSlots =
        {
            "press-slot: installed",
            "die-slot: missing"
        };
        [SerializeField] private string _defaultSetupDiagnostics = "Install required tooling before switching to operate mode.";
        [SerializeField] private string _setupExecuteDiagnostics = "Switch to Operate mode to execute an operation.";

        private ReloadingWorkbenchViewBinder _viewBinder;
        private IUiStateEvents _uiStateEvents;
        private IUiStateEvents _subscribedUiStateEvents;
        private bool _useRuntimeKernelUiStateEvents = true;
        private int _selectedOperation;
        private string _resultText;
        private string _setupDiagnosticsText = string.Empty;
        private string _operateDiagnosticsText = string.Empty;
        private ReloadingWorkbenchMode _mode = ReloadingWorkbenchMode.Setup;
        private bool _isVisible;

        private void OnEnable()
        {
            SubscribeToRuntimeHubReconfigure();
            SubscribeToUiStateEvents(ResolveUiStateEvents());
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            UnsubscribeFromUiStateEvents();
        }

        public void Configure(IUiStateEvents uiStateEvents = null)
        {
            _useRuntimeKernelUiStateEvents = uiStateEvents == null;
            _uiStateEvents = uiStateEvents;
            if (isActiveAndEnabled)
            {
                SubscribeToUiStateEvents(ResolveUiStateEvents());
            }
        }

        public void SetViewBinder(ReloadingWorkbenchViewBinder binder)
        {
            _viewBinder = binder;
            Refresh();
        }

        public void HandleIntent(UiIntent intent)
        {
            if (intent.Key == "reloading.mode.setup")
            {
                _mode = ReloadingWorkbenchMode.Setup;
                _setupDiagnosticsText = _defaultSetupDiagnostics;
                _operateDiagnosticsText = string.Empty;
                Refresh();
                return;
            }

            if (intent.Key == "reloading.mode.operate")
            {
                _mode = ReloadingWorkbenchMode.Operate;
                _setupDiagnosticsText = string.Empty;
                _operateDiagnosticsText = GetSelectedOperationDiagnostic();
                Refresh();
                return;
            }

            if (intent.Key == "reloading.operation.select" && intent.Payload is int index)
            {
                _selectedOperation = Mathf.Clamp(index, 0, Mathf.Max(0, _operationLabels.Length - 1));
                if (_mode == ReloadingWorkbenchMode.Operate)
                {
                    _operateDiagnosticsText = GetSelectedOperationDiagnostic();
                }

                Refresh();
                return;
            }

            if (intent.Key == "reloading.operation.execute")
            {
                if (_mode == ReloadingWorkbenchMode.Setup)
                {
                    _setupDiagnosticsText = _setupExecuteDiagnostics;
                    _resultText = string.Empty;
                    Refresh();
                    return;
                }

                if (!IsOperationEnabled(_selectedOperation))
                {
                    _resultText = "Operation blocked";
                    _operateDiagnosticsText = GetSelectedOperationDiagnostic();
                    Refresh();
                    return;
                }

                _operateDiagnosticsText = string.Empty;
                _resultText = _operationLabels.Length == 0
                    ? "No operations configured"
                    : $"Executed {_operationLabels[_selectedOperation]}";
                Refresh();
            }
        }

        private void Refresh()
        {
            if (_viewBinder == null)
            {
                return;
            }

            _viewBinder.SetVisible(_isVisible);
            if (!_isVisible)
            {
                return;
            }

            var operations = new List<ReloadingWorkbenchUiState.OperationState>(_operationLabels.Length);
            for (var i = 0; i < _operationLabels.Length; i++)
            {
                operations.Add(new ReloadingWorkbenchUiState.OperationState(
                    i,
                    _operationLabels[i],
                    i == _selectedOperation,
                    IsOperationEnabled(i),
                    GetOperationDiagnostic(i)));
            }

            if (string.IsNullOrEmpty(_setupDiagnosticsText) && _mode == ReloadingWorkbenchMode.Setup)
            {
                _setupDiagnosticsText = _defaultSetupDiagnostics;
            }

            _viewBinder.Render(ReloadingWorkbenchUiState.Create(
                mode: _mode,
                operations: operations,
                setupSlotsText: BuildSetupSlotsText(),
                setupDiagnosticsText: _setupDiagnosticsText,
                operateDiagnosticsText: _operateDiagnosticsText,
                resultText: _resultText));
        }

        private void HandleWorkbenchVisibilityChanged(bool isVisible)
        {
            _isVisible = isVisible;
            Refresh();
        }

        private string BuildSetupSlotsText()
        {
            if (_setupSlots == null || _setupSlots.Length == 0)
            {
                return "No setup slots configured.";
            }

            return string.Join("\n", _setupSlots);
        }

        private bool IsOperationEnabled(int index)
        {
            return _operationEnabled != null
                   && index >= 0
                   && index < _operationEnabled.Length
                   && _operationEnabled[index];
        }

        private string GetOperationDiagnostic(int index)
        {
            if (_operationBlockedDiagnostics == null
                || index < 0
                || index >= _operationBlockedDiagnostics.Length)
            {
                return string.Empty;
            }

            return _operationBlockedDiagnostics[index] ?? string.Empty;
        }

        private string GetSelectedOperationDiagnostic()
        {
            if (IsOperationEnabled(_selectedOperation))
            {
                return string.Empty;
            }

            var configured = GetOperationDiagnostic(_selectedOperation);
            if (!string.IsNullOrEmpty(configured))
            {
                return configured;
            }

            return "Operation blocked: missing setup requirement.";
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled || !_useRuntimeKernelUiStateEvents)
            {
                return;
            }

            SubscribeToUiStateEvents(ResolveUiStateEvents());
            ReconcileVisibilityAfterRuntimeHubSwap();
        }

        private IUiStateEvents ResolveUiStateEvents()
        {
            if (_useRuntimeKernelUiStateEvents)
            {
                var runtimeUiStateEvents = RuntimeKernelBootstrapper.UiStateEvents;
                if (!ReferenceEquals(_uiStateEvents, runtimeUiStateEvents))
                {
                    _uiStateEvents = runtimeUiStateEvents;
                    SubscribeToUiStateEvents(_uiStateEvents);
                }
                else if (!ReferenceEquals(_subscribedUiStateEvents, _uiStateEvents))
                {
                    SubscribeToUiStateEvents(_uiStateEvents);
                }
            }
            else if (!ReferenceEquals(_subscribedUiStateEvents, _uiStateEvents))
            {
                SubscribeToUiStateEvents(_uiStateEvents);
            }

            return _uiStateEvents;
        }

        private void ReconcileVisibilityAfterRuntimeHubSwap()
        {
            _isVisible = _uiStateEvents?.IsWorkbenchMenuVisible ?? false;
            Refresh();
        }

        private void SubscribeToUiStateEvents(IUiStateEvents uiStateEvents)
        {
            if (uiStateEvents == null)
            {
                UnsubscribeFromUiStateEvents();
                return;
            }

            if (ReferenceEquals(_subscribedUiStateEvents, uiStateEvents))
            {
                return;
            }

            UnsubscribeFromUiStateEvents();
            _subscribedUiStateEvents = uiStateEvents;
            _subscribedUiStateEvents.OnWorkbenchMenuVisibilityChanged += HandleWorkbenchVisibilityChanged;
        }

        private void UnsubscribeFromUiStateEvents()
        {
            if (_subscribedUiStateEvents == null)
            {
                return;
            }

            _subscribedUiStateEvents.OnWorkbenchMenuVisibilityChanged -= HandleWorkbenchVisibilityChanged;
            _subscribedUiStateEvents = null;
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
    }
}
