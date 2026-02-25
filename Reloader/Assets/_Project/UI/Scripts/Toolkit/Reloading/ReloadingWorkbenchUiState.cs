using System;
using System.Collections.Generic;
using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Toolkit.Reloading
{
    public sealed class ReloadingWorkbenchUiState : UiRenderState
    {
        public readonly struct OperationState
        {
            public OperationState(int index, string label, bool isSelected)
            {
                Index = index;
                Label = label ?? string.Empty;
                IsSelected = isSelected;
            }

            public int Index { get; }
            public string Label { get; }
            public bool IsSelected { get; }
        }

        private readonly OperationState[] _operations;

        private ReloadingWorkbenchUiState(OperationState[] operations, string resultText)
            : base("reloading-workbench")
        {
            _operations = operations ?? Array.Empty<OperationState>();
            ResultText = resultText ?? string.Empty;
        }

        public IReadOnlyList<OperationState> Operations => _operations;
        public string ResultText { get; }

        public static ReloadingWorkbenchUiState Create(IEnumerable<OperationState> operations, string resultText)
        {
            var opList = operations == null ? Array.Empty<OperationState>() : new List<OperationState>(operations).ToArray();
            return new ReloadingWorkbenchUiState(opList, resultText);
        }
    }
}
