using System;
using System.Collections.Generic;
using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Toolkit.Reloading
{
    public enum ReloadingWorkbenchMode
    {
        Setup = 0,
        Operate = 1
    }

    public sealed class ReloadingWorkbenchUiState : UiRenderState
    {
        public readonly struct OperationState
        {
            public OperationState(int index, string label, bool isSelected, bool isEnabled, string diagnosticsText)
            {
                Index = index;
                Label = label ?? string.Empty;
                IsSelected = isSelected;
                IsEnabled = isEnabled;
                DiagnosticsText = diagnosticsText ?? string.Empty;
            }

            public int Index { get; }
            public string Label { get; }
            public bool IsSelected { get; }
            public bool IsEnabled { get; }
            public string DiagnosticsText { get; }
        }

        private readonly OperationState[] _operations;

        private ReloadingWorkbenchUiState(
            ReloadingWorkbenchMode mode,
            OperationState[] operations,
            string setupSlotsText,
            string setupDiagnosticsText,
            string operateDiagnosticsText,
            string resultText)
            : base("reloading-workbench")
        {
            Mode = mode;
            _operations = operations ?? Array.Empty<OperationState>();
            SetupSlotsText = setupSlotsText ?? string.Empty;
            SetupDiagnosticsText = setupDiagnosticsText ?? string.Empty;
            OperateDiagnosticsText = operateDiagnosticsText ?? string.Empty;
            ResultText = resultText ?? string.Empty;
        }

        public ReloadingWorkbenchMode Mode { get; }
        public IReadOnlyList<OperationState> Operations => _operations;
        public string SetupSlotsText { get; }
        public string SetupDiagnosticsText { get; }
        public string OperateDiagnosticsText { get; }
        public string ResultText { get; }

        public static ReloadingWorkbenchUiState Create(
            ReloadingWorkbenchMode mode,
            IEnumerable<OperationState> operations,
            string setupSlotsText,
            string setupDiagnosticsText,
            string operateDiagnosticsText,
            string resultText)
        {
            var opList = operations == null ? Array.Empty<OperationState>() : new List<OperationState>(operations).ToArray();
            return new ReloadingWorkbenchUiState(
                mode,
                opList,
                setupSlotsText,
                setupDiagnosticsText,
                operateDiagnosticsText,
                resultText);
        }
    }
}
