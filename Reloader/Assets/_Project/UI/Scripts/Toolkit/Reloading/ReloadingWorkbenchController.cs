using System;
using System.Collections.Generic;
using System.Reflection;
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
        private static MethodInfo s_tryGetLatestMethod;
        private static Type s_snapshotType;
        private static Type s_operationSnapshotType;
        private static PropertyInfo s_setupSlotsProperty;
        private static PropertyInfo s_operationStatusesProperty;
        private static PropertyInfo s_operationLabelProperty;
        private static PropertyInfo s_operationEnabledProperty;
        private static PropertyInfo s_operationDiagnosticProperty;

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
                var snapshotData = ResolveSnapshotData();
                _operateDiagnosticsText = GetSelectedOperationDiagnostic(snapshotData);
                Refresh();
                return;
            }

            if (intent.Key == "reloading.operation.select" && intent.Payload is int index)
            {
                var snapshotData = ResolveSnapshotData();
                _selectedOperation = Mathf.Clamp(index, 0, Mathf.Max(0, GetOperationCount(snapshotData) - 1));
                if (_mode == ReloadingWorkbenchMode.Operate)
                {
                    _operateDiagnosticsText = GetSelectedOperationDiagnostic(snapshotData);
                }

                Refresh();
                return;
            }

            if (intent.Key == "reloading.operation.execute")
            {
                var snapshotData = ResolveSnapshotData();
                if (_mode == ReloadingWorkbenchMode.Setup)
                {
                    _setupDiagnosticsText = _setupExecuteDiagnostics;
                    _resultText = string.Empty;
                    Refresh();
                    return;
                }

                if (!IsOperationEnabled(_selectedOperation, snapshotData))
                {
                    _resultText = "Operation blocked";
                    _operateDiagnosticsText = GetSelectedOperationDiagnostic(snapshotData);
                    Refresh();
                    return;
                }

                _operateDiagnosticsText = string.Empty;
                _resultText = GetOperationCount(snapshotData) == 0
                    ? "No operations configured"
                    : $"Executed {GetOperationLabel(_selectedOperation, snapshotData)}";
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

            var snapshotData = ResolveSnapshotData();
            var operationCount = GetOperationCount(snapshotData);
            _selectedOperation = Mathf.Clamp(_selectedOperation, 0, Mathf.Max(0, operationCount - 1));

            var operations = new List<ReloadingWorkbenchUiState.OperationState>(operationCount);
            for (var i = 0; i < operationCount; i++)
            {
                operations.Add(new ReloadingWorkbenchUiState.OperationState(
                    i,
                    GetOperationLabel(i, snapshotData),
                    i == _selectedOperation,
                    IsOperationEnabled(i, snapshotData),
                    GetOperationDiagnostic(i, snapshotData)));
            }

            if (string.IsNullOrEmpty(_setupDiagnosticsText) && _mode == ReloadingWorkbenchMode.Setup)
            {
                _setupDiagnosticsText = _defaultSetupDiagnostics;
            }

            _viewBinder.Render(ReloadingWorkbenchUiState.Create(
                mode: _mode,
                operations: operations,
                setupSlotsText: BuildSetupSlotsText(snapshotData),
                setupDiagnosticsText: _setupDiagnosticsText,
                operateDiagnosticsText: _operateDiagnosticsText,
                resultText: _resultText));
        }

        private void HandleWorkbenchVisibilityChanged(bool isVisible)
        {
            _isVisible = isVisible;
            Refresh();
        }

        private string BuildSetupSlotsText(SnapshotData snapshotData)
        {
            if (snapshotData.HasSnapshot && snapshotData.SetupSlots.Length > 0)
            {
                return string.Join("\n", snapshotData.SetupSlots);
            }

            if (_setupSlots == null || _setupSlots.Length == 0)
            {
                return "No setup slots configured.";
            }

            return string.Join("\n", _setupSlots);
        }

        private int GetOperationCount(SnapshotData snapshotData)
        {
            if (snapshotData.HasSnapshot)
            {
                return snapshotData.Operations.Length;
            }

            return _operationLabels?.Length ?? 0;
        }

        private string GetOperationLabel(int index, SnapshotData snapshotData)
        {
            if (snapshotData.HasSnapshot
                && index >= 0
                && index < snapshotData.Operations.Length)
            {
                return snapshotData.Operations[index].Label;
            }

            if (_operationLabels == null || index < 0 || index >= _operationLabels.Length)
            {
                return string.Empty;
            }

            return _operationLabels[index] ?? string.Empty;
        }

        private bool IsOperationEnabled(int index, SnapshotData snapshotData)
        {
            if (snapshotData.HasSnapshot
                && index >= 0
                && index < snapshotData.Operations.Length)
            {
                return snapshotData.Operations[index].IsEnabled;
            }

            return _operationEnabled != null
                   && index >= 0
                   && index < _operationEnabled.Length
                   && _operationEnabled[index];
        }

        private string GetOperationDiagnostic(int index, SnapshotData snapshotData)
        {
            if (snapshotData.HasSnapshot
                && index >= 0
                && index < snapshotData.Operations.Length)
            {
                return snapshotData.Operations[index].Diagnostic;
            }

            if (_operationBlockedDiagnostics == null
                || index < 0
                || index >= _operationBlockedDiagnostics.Length)
            {
                return string.Empty;
            }

            return _operationBlockedDiagnostics[index] ?? string.Empty;
        }

        private string GetSelectedOperationDiagnostic(SnapshotData snapshotData)
        {
            if (IsOperationEnabled(_selectedOperation, snapshotData))
            {
                return string.Empty;
            }

            var configured = GetOperationDiagnostic(_selectedOperation, snapshotData);
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

        private SnapshotData ResolveSnapshotData()
        {
            if (!TryReadSnapshot(out var snapshot))
            {
                return SnapshotData.Empty;
            }

            return snapshot;
        }

        private static bool TryReadSnapshot(out SnapshotData snapshot)
        {
            snapshot = SnapshotData.Empty;
            if (!TryResolveSnapshotReflection())
            {
                return false;
            }

            var args = new object[] { null };
            if (!(s_tryGetLatestMethod.Invoke(null, args) is bool hasSnapshot) || !hasSnapshot || args[0] == null)
            {
                return false;
            }

            var setupSlots = ReadSetupSlots(args[0]);
            var operations = ReadOperationSnapshots(args[0]);
            snapshot = new SnapshotData(true, setupSlots, operations);
            return true;
        }

        private static bool TryResolveSnapshotReflection()
        {
            if (s_tryGetLatestMethod != null
                && s_setupSlotsProperty != null
                && s_operationStatusesProperty != null
                && s_operationLabelProperty != null
                && s_operationEnabledProperty != null
                && s_operationDiagnosticProperty != null)
            {
                return true;
            }

            var storeType = Type.GetType("Reloader.Reloading.Runtime.ReloadingWorkbenchUiContextStore, Assembly-CSharp");
            s_snapshotType = Type.GetType("Reloader.Reloading.Runtime.ReloadingWorkbenchUiSnapshot, Assembly-CSharp");
            s_operationSnapshotType = Type.GetType("Reloader.Reloading.Runtime.ReloadingWorkbenchUiSnapshot+OperationGateSnapshot, Assembly-CSharp");
            if (storeType == null || s_snapshotType == null || s_operationSnapshotType == null)
            {
                return false;
            }

            s_tryGetLatestMethod = storeType.GetMethod("TryGetLatest", BindingFlags.Public | BindingFlags.Static);
            s_setupSlotsProperty = s_snapshotType.GetProperty("SetupSlots", BindingFlags.Public | BindingFlags.Instance);
            s_operationStatusesProperty = s_snapshotType.GetProperty("OperationStatuses", BindingFlags.Public | BindingFlags.Instance);
            s_operationLabelProperty = s_operationSnapshotType.GetProperty("Label", BindingFlags.Public | BindingFlags.Instance);
            s_operationEnabledProperty = s_operationSnapshotType.GetProperty("IsEnabled", BindingFlags.Public | BindingFlags.Instance);
            s_operationDiagnosticProperty = s_operationSnapshotType.GetProperty("Diagnostic", BindingFlags.Public | BindingFlags.Instance);

            return s_tryGetLatestMethod != null
                   && s_setupSlotsProperty != null
                   && s_operationStatusesProperty != null
                   && s_operationLabelProperty != null
                   && s_operationEnabledProperty != null
                   && s_operationDiagnosticProperty != null;
        }

        private static string[] ReadSetupSlots(object snapshot)
        {
            if (!(s_setupSlotsProperty.GetValue(snapshot) is string[] slots) || slots.Length == 0)
            {
                return Array.Empty<string>();
            }

            var copy = new string[slots.Length];
            for (var i = 0; i < slots.Length; i++)
            {
                copy[i] = slots[i] ?? string.Empty;
            }

            return copy;
        }

        private static OperationSnapshotData[] ReadOperationSnapshots(object snapshot)
        {
            if (!(s_operationStatusesProperty.GetValue(snapshot) is Array operations) || operations.Length == 0)
            {
                return Array.Empty<OperationSnapshotData>();
            }

            var copy = new OperationSnapshotData[operations.Length];
            for (var i = 0; i < operations.Length; i++)
            {
                var operation = operations.GetValue(i);
                var label = s_operationLabelProperty.GetValue(operation) as string ?? string.Empty;
                var isEnabled = s_operationEnabledProperty.GetValue(operation) is bool enabled && enabled;
                var diagnostic = s_operationDiagnosticProperty.GetValue(operation) as string ?? string.Empty;
                copy[i] = new OperationSnapshotData(label, isEnabled, diagnostic);
            }

            return copy;
        }

        private readonly struct SnapshotData
        {
            public static readonly SnapshotData Empty = new SnapshotData(false, Array.Empty<string>(), Array.Empty<OperationSnapshotData>());

            public SnapshotData(bool hasSnapshot, string[] setupSlots, OperationSnapshotData[] operations)
            {
                HasSnapshot = hasSnapshot;
                SetupSlots = setupSlots ?? Array.Empty<string>();
                Operations = operations ?? Array.Empty<OperationSnapshotData>();
            }

            public bool HasSnapshot { get; }

            public string[] SetupSlots { get; }

            public OperationSnapshotData[] Operations { get; }
        }

        private readonly struct OperationSnapshotData
        {
            public OperationSnapshotData(string label, bool isEnabled, string diagnostic)
            {
                Label = label ?? string.Empty;
                IsEnabled = isEnabled;
                Diagnostic = diagnostic ?? string.Empty;
            }

            public string Label { get; }

            public bool IsEnabled { get; }

            public string Diagnostic { get; }
        }
    }
}
