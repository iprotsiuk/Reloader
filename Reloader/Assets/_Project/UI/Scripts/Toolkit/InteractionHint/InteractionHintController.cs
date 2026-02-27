using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.InteractionHint
{
    public sealed class InteractionHintController : MonoBehaviour, IUiController
    {
        private InteractionHintViewBinder _viewBinder;
        private IInteractionHintEvents _interactionHintEvents;
        private IInteractionHintEvents _subscribedInteractionHintEvents;
        private bool _useRuntimeKernelInteractionHintEvents = true;

        private void OnEnable()
        {
            SubscribeToRuntimeHubReconfigure();
            SubscribeToInteractionHintEvents(ResolveInteractionHintEvents());
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            UnsubscribeFromInteractionHintEvents();
        }

        public void Configure(IInteractionHintEvents interactionHintEvents = null)
        {
            _useRuntimeKernelInteractionHintEvents = interactionHintEvents == null;
            _interactionHintEvents = interactionHintEvents;
            if (isActiveAndEnabled)
            {
                SubscribeToInteractionHintEvents(ResolveInteractionHintEvents());
            }
        }

        public void SetViewBinder(InteractionHintViewBinder binder)
        {
            _viewBinder = binder;
            Refresh();
        }

        public void HandleIntent(UiIntent intent)
        {
        }

        public void Refresh()
        {
            if (_viewBinder == null)
            {
                return;
            }

            var interactionHintEvents = ResolveInteractionHintEvents();
            if (interactionHintEvents == null || !interactionHintEvents.HasInteractionHint)
            {
                _viewBinder.Render(new InteractionHintUiState(string.Empty, false));
                return;
            }

            _viewBinder.Render(new InteractionHintUiState(BuildTooltipText(interactionHintEvents.CurrentInteractionHint), true));
        }

        private void HandleInteractionHintShown(InteractionHintPayload payload)
        {
            _viewBinder?.Render(new InteractionHintUiState(BuildTooltipText(payload), true));
        }

        private void HandleInteractionHintCleared()
        {
            _viewBinder?.Render(new InteractionHintUiState(string.Empty, false));
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled || !_useRuntimeKernelInteractionHintEvents)
            {
                return;
            }

            SubscribeToInteractionHintEvents(ResolveInteractionHintEvents());
            Refresh();
        }

        private IInteractionHintEvents ResolveInteractionHintEvents()
        {
            if (_useRuntimeKernelInteractionHintEvents)
            {
                var runtimeInteractionHintEvents = RuntimeKernelBootstrapper.InteractionHintEvents;
                if (!ReferenceEquals(_interactionHintEvents, runtimeInteractionHintEvents))
                {
                    _interactionHintEvents = runtimeInteractionHintEvents;
                    SubscribeToInteractionHintEvents(_interactionHintEvents);
                }
                else if (!ReferenceEquals(_subscribedInteractionHintEvents, _interactionHintEvents))
                {
                    SubscribeToInteractionHintEvents(_interactionHintEvents);
                }

                return _interactionHintEvents;
            }

            if (!ReferenceEquals(_subscribedInteractionHintEvents, _interactionHintEvents))
            {
                SubscribeToInteractionHintEvents(_interactionHintEvents);
            }

            return _interactionHintEvents;
        }

        private void SubscribeToInteractionHintEvents(IInteractionHintEvents interactionHintEvents)
        {
            if (interactionHintEvents == null)
            {
                UnsubscribeFromInteractionHintEvents();
                return;
            }

            if (ReferenceEquals(_subscribedInteractionHintEvents, interactionHintEvents))
            {
                return;
            }

            UnsubscribeFromInteractionHintEvents();
            _subscribedInteractionHintEvents = interactionHintEvents;
            _subscribedInteractionHintEvents.OnInteractionHintShown += HandleInteractionHintShown;
            _subscribedInteractionHintEvents.OnInteractionHintCleared += HandleInteractionHintCleared;
        }

        private void UnsubscribeFromInteractionHintEvents()
        {
            if (_subscribedInteractionHintEvents == null)
            {
                return;
            }

            _subscribedInteractionHintEvents.OnInteractionHintShown -= HandleInteractionHintShown;
            _subscribedInteractionHintEvents.OnInteractionHintCleared -= HandleInteractionHintCleared;
            _subscribedInteractionHintEvents = null;
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

        private static string BuildTooltipText(InteractionHintPayload payload)
        {
            var actionText = (payload.ActionText ?? string.Empty).Trim();
            var subjectText = (payload.SubjectText ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(subjectText))
            {
                return actionText;
            }

            if (string.IsNullOrWhiteSpace(actionText))
            {
                return subjectText;
            }

            return $"{actionText} {subjectText}";
        }
    }
}
