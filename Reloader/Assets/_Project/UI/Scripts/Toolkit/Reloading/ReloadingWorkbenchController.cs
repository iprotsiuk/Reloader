using System.Collections.Generic;
using Reloader.Core.Runtime;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.Reloading
{
    public sealed class ReloadingWorkbenchController : MonoBehaviour, IUiController
    {
        [SerializeField] private string[] _operationLabels = { "Resize", "Prime", "Seat" };

        private ReloadingWorkbenchViewBinder _viewBinder;
        private IUiStateEvents _uiStateEvents;
        private IUiStateEvents _subscribedUiStateEvents;
        private bool _useRuntimeKernelUiStateEvents = true;
        private int _selectedOperation;
        private string _resultText;
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

            _viewBinder.SetVisible(_isVisible);
            if (!_isVisible)
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

        private void HandleWorkbenchVisibilityChanged(bool isVisible)
        {
            _isVisible = isVisible;
            Refresh();
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled || !_useRuntimeKernelUiStateEvents)
            {
                return;
            }

            SubscribeToUiStateEvents(ResolveUiStateEvents());
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
