using System;
using System.Collections.Generic;
using Reloader.Core;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using UnityEngine;

namespace Reloader.Player.Interaction
{
    public sealed class PlayerInteractionCoordinator : MonoBehaviour
    {
        [SerializeField] private bool _coordinatorModeEnabled;
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private List<MonoBehaviour> _providerBehaviours = new List<MonoBehaviour>();
        [SerializeField] private bool _logArbitrationTransitions;
        [Header("Debug (Runtime)")]
        [SerializeField] private string _winnerContextIdDebug;
        [SerializeField] private string _winnerActionTextDebug;
        [SerializeField] private string _winnerSubjectTextDebug;
        [SerializeField] private int _winnerPriorityDebug;
        [SerializeField] private string _winnerStableTieBreakerDebug;
        [SerializeField] private int _winnerProviderIndexDebug = -1;
        [SerializeField] private string _diagnosticSnapshot;

        private IPlayerInputSource _inputSource;
        private readonly List<IPlayerInteractionCandidateProvider> _providers = new List<IPlayerInteractionCandidateProvider>();
        private readonly List<IPlayerInteractionCoordinatorModeAware> _modeAwareProviders = new List<IPlayerInteractionCoordinatorModeAware>();

        private bool _hasWinner;
        private PlayerInteractionCandidate _winner;
        private int _winnerProviderIndex = -1;
        private bool _hasPublishedHint;
        private InteractionHintPayload _publishedHint;

        private void Awake()
        {
            ResolveReferences();
            ApplyCoordinatorModeToProviders();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ApplyCoordinatorModeToProviders();
        }

        private void OnDisable()
        {
            if (_coordinatorModeEnabled)
            {
                ClearPublishedHint();
            }

            SetNoWinnerDebugState();
            _diagnosticSnapshot = string.Empty;
            _hasWinner = false;
            _winnerProviderIndex = -1;
        }

        private void Update()
        {
            Tick();
        }

        public void Tick()
        {
            ResolveReferences();
            ApplyCoordinatorModeToProviders();

            if (!_coordinatorModeEnabled)
            {
                SetNoWinnerDebugState();
                _diagnosticSnapshot = BuildDiagnosticSnapshot(false, false);
                ClearPublishedHint();
                return;
            }

            _hasWinner = TryResolveWinner(out _winner, out _winnerProviderIndex);
            UpdateWinnerDebugState();

            if (!_hasWinner)
            {
                ClearPublishedHint();
                _diagnosticSnapshot = BuildDiagnosticSnapshot(true, false);
                return;
            }

            PublishWinnerHint(_winner);
            _diagnosticSnapshot = BuildDiagnosticSnapshot(true, true);

            var pickupPressed = _inputSource != null && _inputSource.ConsumePickupPressed();
            if (pickupPressed)
            {
                _winner.Execute?.Invoke();
            }
        }

        public string CaptureDebugSnapshot()
        {
            return _diagnosticSnapshot;
        }

        private bool TryResolveWinner(out PlayerInteractionCandidate winner, out int winnerProviderIndex)
        {
            winner = default;
            winnerProviderIndex = -1;

            var hasAny = false;
            for (var i = 0; i < _providers.Count; i++)
            {
                var provider = _providers[i];
                if (provider == null || !provider.TryGetInteractionCandidate(out var candidate))
                {
                    continue;
                }

                if (!hasAny || IsBetterCandidate(candidate, i, winner, winnerProviderIndex))
                {
                    winner = candidate;
                    winnerProviderIndex = i;
                    hasAny = true;
                }
            }

            return hasAny;
        }

        private bool IsBetterCandidate(PlayerInteractionCandidate left, int leftIndex, PlayerInteractionCandidate right, int rightIndex)
        {
            if (left.Priority != right.Priority)
            {
                return left.Priority > right.Priority;
            }

            var stableTieComparison = string.CompareOrdinal(left.StableTieBreaker, right.StableTieBreaker);
            if (stableTieComparison != 0)
            {
                return stableTieComparison < 0;
            }

            if (leftIndex != rightIndex)
            {
                return leftIndex < rightIndex;
            }

            var contextComparison = string.CompareOrdinal(left.ContextId, right.ContextId);
            if (contextComparison != 0)
            {
                return contextComparison < 0;
            }

            var actionComparison = string.CompareOrdinal(left.ActionText, right.ActionText);
            if (actionComparison != 0)
            {
                return actionComparison < 0;
            }

            return string.CompareOrdinal(left.SubjectText, right.SubjectText) < 0;
        }

        private void ResolveReferences()
        {
            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponents<MonoBehaviour>());
            }

            _providers.Clear();
            _modeAwareProviders.Clear();
            for (var i = 0; i < _providerBehaviours.Count; i++)
            {
                var behaviour = _providerBehaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour is IPlayerInteractionCandidateProvider provider)
                {
                    _providers.Add(provider);
                }

                if (behaviour is IPlayerInteractionCoordinatorModeAware modeAware)
                {
                    _modeAwareProviders.Add(modeAware);
                }
            }
        }

        private void ApplyCoordinatorModeToProviders()
        {
            for (var i = 0; i < _modeAwareProviders.Count; i++)
            {
                _modeAwareProviders[i]?.SetInteractionCoordinatorMode(_coordinatorModeEnabled);
            }
        }

        private void PublishWinnerHint(PlayerInteractionCandidate winner)
        {
            var hintEvents = RuntimeKernelBootstrapper.InteractionHintEvents;
            if (hintEvents == null)
            {
                return;
            }

            var payload = new InteractionHintPayload(winner.ContextId, winner.ActionText, winner.SubjectText);
            if (_hasPublishedHint
                && string.Equals(_publishedHint.ContextId, payload.ContextId, StringComparison.Ordinal)
                && string.Equals(_publishedHint.ActionText, payload.ActionText, StringComparison.Ordinal)
                && string.Equals(_publishedHint.SubjectText, payload.SubjectText, StringComparison.Ordinal))
            {
                return;
            }

            hintEvents.RaiseInteractionHintShown(payload);
            _publishedHint = payload;
            _hasPublishedHint = true;

            if (_logArbitrationTransitions)
            {
                Debug.Log($"[PlayerInteractionCoordinator] Winner={winner.ActionKind} context='{winner.ContextId}' action='{winner.ActionText}' subject='{winner.SubjectText}' priority={winner.Priority} tie='{winner.StableTieBreaker}' providerIndex={_winnerProviderIndex}", this);
            }
        }

        private void ClearPublishedHint()
        {
            if (!_hasPublishedHint)
            {
                return;
            }

            RuntimeKernelBootstrapper.InteractionHintEvents?.RaiseInteractionHintCleared(_publishedHint.ContextId);
            _hasPublishedHint = false;
            _publishedHint = default;
        }

        private void UpdateWinnerDebugState()
        {
            if (!_hasWinner)
            {
                SetNoWinnerDebugState();
                return;
            }

            _winnerContextIdDebug = _winner.ContextId;
            _winnerActionTextDebug = _winner.ActionText;
            _winnerSubjectTextDebug = _winner.SubjectText;
            _winnerPriorityDebug = _winner.Priority;
            _winnerStableTieBreakerDebug = _winner.StableTieBreaker;
            _winnerProviderIndexDebug = _winnerProviderIndex;
        }

        private void SetNoWinnerDebugState()
        {
            _winnerContextIdDebug = string.Empty;
            _winnerActionTextDebug = string.Empty;
            _winnerSubjectTextDebug = string.Empty;
            _winnerPriorityDebug = 0;
            _winnerStableTieBreakerDebug = string.Empty;
            _winnerProviderIndexDebug = -1;
        }

        private string BuildDiagnosticSnapshot(bool coordinatorEnabled, bool hasWinner)
        {
            return $"enabled={coordinatorEnabled}; providers={_providers.Count}; hasWinner={hasWinner}; winnerContext={_winnerContextIdDebug}; winnerAction={_winnerActionTextDebug}; winnerPriority={_winnerPriorityDebug}; winnerProviderIndex={_winnerProviderIndexDebug}";
        }
    }
}
