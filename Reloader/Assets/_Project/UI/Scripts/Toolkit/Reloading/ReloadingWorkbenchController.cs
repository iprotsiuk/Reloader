using System.Collections.Generic;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.Reloading
{
    public sealed class ReloadingWorkbenchController : MonoBehaviour, IUiController
    {
        [SerializeField] private string[] _operationLabels = { "Resize", "Prime", "Seat" };

        private ReloadingWorkbenchViewBinder _viewBinder;
        private int _selectedOperation;
        private string _resultText;

        public void SetViewBinder(ReloadingWorkbenchViewBinder binder)
        {
            _viewBinder = binder;
            Refresh();
        }

        public void HandleIntent(UiIntent intent)
        {
            if (intent.Key == "reloading.operation.select" && intent.Payload is int index)
            {
                _selectedOperation = Mathf.Clamp(index, 0, Mathf.Max(0, _operationLabels.Length - 1));
                Refresh();
                return;
            }

            if (intent.Key == "reloading.operation.execute")
            {
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

            var operations = new List<ReloadingWorkbenchUiState.OperationState>(_operationLabels.Length);
            for (var i = 0; i < _operationLabels.Length; i++)
            {
                operations.Add(new ReloadingWorkbenchUiState.OperationState(i, _operationLabels[i], i == _selectedOperation));
            }

            _viewBinder.Render(ReloadingWorkbenchUiState.Create(operations, _resultText));
        }
    }
}
