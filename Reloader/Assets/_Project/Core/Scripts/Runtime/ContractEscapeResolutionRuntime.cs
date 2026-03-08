using System;
using Reloader.Core.Events;
using Reloader.Core.Runtime;

namespace Reloader.Contracts.Runtime
{
    public sealed class ContractEscapeResolutionRuntime
    {
        internal readonly struct RuntimeStateSnapshot
        {
            public RuntimeStateSnapshot(
                AssassinationContractDefinition availableContract,
                AssassinationContractDefinition activeDefinition,
                AssassinationContractDefinition failedDefinition,
                AssassinationContractRuntimeState activeContract,
                PoliceHeatState heatState,
                bool offerConsumed,
                bool awaitingSearchClear,
                bool completionPending,
                int pendingPayoutAmount)
            {
                AvailableContract = availableContract;
                ActiveDefinition = activeDefinition;
                FailedDefinition = failedDefinition;
                ActiveContract = activeContract;
                HeatState = heatState;
                OfferConsumed = offerConsumed;
                AwaitingSearchClear = awaitingSearchClear;
                CompletionPending = completionPending;
                PendingPayoutAmount = pendingPayoutAmount;
            }

            public AssassinationContractDefinition AvailableContract { get; }
            public AssassinationContractDefinition ActiveDefinition { get; }
            public AssassinationContractDefinition FailedDefinition { get; }
            public AssassinationContractRuntimeState ActiveContract { get; }
            public PoliceHeatState HeatState { get; }
            public bool OfferConsumed { get; }
            public bool AwaitingSearchClear { get; }
            public bool CompletionPending { get; }
            public int PendingPayoutAmount { get; }
        }

        private readonly ContractRuntimeController _contractController = new ContractRuntimeController();
        private readonly PoliceHeatRuntime _policeHeatRuntime;
        private AssassinationContractDefinition _availableContract;
        private AssassinationContractDefinition _failedDefinition;
        private IContractPayoutReceiver _payoutReceiver;
        private bool _offerConsumed;
        private bool _awaitingSearchClear;
        private bool _completionPending;
        private int _pendingPayoutAmount;

        public ContractEscapeResolutionRuntime(
            AssassinationContractDefinition availableContract,
            float searchDurationSeconds = 45f,
            IContractPayoutReceiver payoutReceiver = null,
            ILawEnforcementEvents lawEnforcementEvents = null)
        {
            _availableContract = availableContract;
            _payoutReceiver = payoutReceiver;
            _policeHeatRuntime = new PoliceHeatRuntime(searchDurationSeconds, lawEnforcementEvents);
        }

        public AssassinationContractRuntimeState ActiveContract => _contractController.ActiveContract;
        public AssassinationContractDefinition ActiveDefinition => _contractController.ActiveDefinition;
        public PoliceHeatState CurrentHeatState => _policeHeatRuntime.CurrentState;
        public bool IsAwaitingSearchClear => _awaitingSearchClear;
        public bool HasPendingPayout => _pendingPayoutAmount > 0;

        public bool CanPublishAvailableContract()
        {
            if (_contractController.ActiveContract != null)
            {
                return false;
            }

            if (_awaitingSearchClear || _completionPending || _pendingPayoutAmount > 0)
            {
                return false;
            }

            return _policeHeatRuntime.CurrentState.Level == PoliceHeatLevel.Clear;
        }

        public void SetAvailableContract(AssassinationContractDefinition availableContract)
        {
            _availableContract = availableContract;
            _offerConsumed = false;
            ResetPendingResolution();
            ClearFailedContractState();
            _contractController.ClearActiveContract();
            _policeHeatRuntime.ForceClear();
        }

        public void ConfigurePayoutReceiver(IContractPayoutReceiver payoutReceiver)
        {
            _payoutReceiver = payoutReceiver;
        }

        public bool TryGetSnapshot(out ContractOfferSnapshot snapshot)
        {
            var activeDefinition = _contractController.ActiveDefinition;
            var activeContract = _contractController.ActiveContract;
            var hasFailedContract = activeContract == null && _failedDefinition != null;
            var hasAvailableContract = !hasFailedContract && !_offerConsumed && _availableContract != null && activeContract == null;
            var definition = activeDefinition ?? _failedDefinition ?? (hasAvailableContract ? _availableContract : null);
            if (definition == null)
            {
                snapshot = default;
                return false;
            }

            snapshot = new ContractOfferSnapshot(
                hasAvailableContract: hasAvailableContract,
                hasActiveContract: activeContract != null,
                hasFailedContract: hasFailedContract,
                contractId: definition.ContractId,
                title: definition.Title,
                targetId: definition.TargetId,
                targetDisplayName: definition.TargetDisplayName,
                targetDescription: definition.TargetDescription,
                briefingText: definition.BriefingText,
                distanceBandMeters: activeContract != null ? activeContract.DistanceBand : definition.DistanceBand,
                payout: activeContract != null ? activeContract.Payout : definition.Payout,
                canAccept: hasAvailableContract,
                canCancel: CanCancelActiveContract(),
                canClaimReward: CanClaimCompletedContractReward(),
                restrictionsText: BuildRestrictionsText(definition),
                failureConditionsText: BuildFailureConditionsText(definition),
                canClearFailed: CanClearFailedContract(),
                statusText: BuildStatusText(hasAvailableContract, activeContract != null, hasFailedContract));
            return true;
        }

        public bool AcceptAvailableContract()
        {
            if (_offerConsumed || _availableContract == null || _contractController.ActiveContract != null)
            {
                return false;
            }

            ResetPendingResolution();
            ClearFailedContractState();
            var accepted = _contractController.TryAcceptContract(_availableContract);
            if (accepted)
            {
                _offerConsumed = true;
                _policeHeatRuntime.ForceClear();
            }

            return accepted;
        }

        public bool CancelActiveContract()
        {
            if (!CanCancelActiveContract())
            {
                return false;
            }

            ResetPendingResolution();
            ClearFailedContractState();
            _policeHeatRuntime.ForceClear();
            _offerConsumed = false;
            return _contractController.TryFailActiveContract();
        }

        public bool ClaimCompletedContractReward()
        {
            if (!CanClaimCompletedContractReward())
            {
                return false;
            }

            if (_pendingPayoutAmount > 0)
            {
                if (_payoutReceiver == null || !_payoutReceiver.TryAwardContractPayout(_pendingPayoutAmount))
                {
                    return false;
                }
            }

            _completionPending = false;
            _pendingPayoutAmount = 0;
            ClearFailedContractState();
            return _contractController.TryCompleteActiveContract();
        }

        public bool ReportTargetEliminated(string targetId, bool wasExposed)
        {
            if (string.IsNullOrWhiteSpace(targetId))
            {
                return false;
            }

            var activeContract = _contractController.ActiveContract;
            if (activeContract == null)
            {
                if (!_offerConsumed &&
                    _availableContract != null &&
                    string.Equals(_availableContract.TargetId, targetId, StringComparison.Ordinal))
                {
                    _offerConsumed = true;
                    RaiseMurderHeatIfNeeded(wasExposed);
                    return true;
                }

                return false;
            }

            if (_completionPending || _awaitingSearchClear)
            {
                RaiseMurderHeatIfNeeded(wasExposed);
                return true;
            }

            var isCorrectTarget = string.Equals(activeContract.TargetId, targetId, StringComparison.Ordinal);
            if (!isCorrectTarget)
            {
                RaiseMurderHeatIfNeeded(wasExposed);
                if (!ShouldFailOnWrongTarget(_contractController.ActiveDefinition))
                {
                    return true;
                }

                _failedDefinition = _contractController.ActiveDefinition;
                ResetPendingResolution();
                _contractController.TryFailActiveContract();
                return false;
            }

            ClearFailedContractState();
            _completionPending = true;
            _pendingPayoutAmount = Math.Max(0, activeContract.Payout);
            if (wasExposed)
            {
                _awaitingSearchClear = true;
                RaiseMurderHeatIfNeeded(true);
                return true;
            }

            return true;
        }

        public void Advance(float deltaTimeSeconds)
        {
            _policeHeatRuntime.Advance(deltaTimeSeconds);
            if (_awaitingSearchClear && _policeHeatRuntime.CurrentState.Level == PoliceHeatLevel.Clear)
            {
                _awaitingSearchClear = false;
            }
        }

        internal RuntimeStateSnapshot CaptureRuntimeState()
        {
            return new RuntimeStateSnapshot(
                _availableContract,
                _contractController.ActiveDefinition,
                _failedDefinition,
                _contractController.ActiveContract,
                _policeHeatRuntime.CurrentState,
                _offerConsumed,
                _awaitingSearchClear,
                _completionPending,
                _pendingPayoutAmount);
        }

        internal static ContractEscapeResolutionRuntime RestoreRuntimeState(
            RuntimeStateSnapshot state,
            float searchDurationSeconds,
            IContractPayoutReceiver payoutReceiver = null,
            ILawEnforcementEvents lawEnforcementEvents = null)
        {
            var runtime = new ContractEscapeResolutionRuntime(
                state.AvailableContract,
                searchDurationSeconds,
                payoutReceiver,
                lawEnforcementEvents);

            runtime._offerConsumed = state.OfferConsumed;
            runtime._awaitingSearchClear = state.AwaitingSearchClear;
            runtime._completionPending = state.CompletionPending;
            runtime._pendingPayoutAmount = Math.Max(0, state.PendingPayoutAmount);
            runtime._failedDefinition = state.FailedDefinition;
            runtime._contractController.RestoreState(state.ActiveDefinition, state.ActiveContract);
            runtime._policeHeatRuntime.RestoreState(state.HeatState);
            return runtime;
        }

        private string BuildStatusText(bool hasAvailableContract, bool hasActiveContract, bool hasFailedContract)
        {
            if (hasFailedContract)
            {
                if (_awaitingSearchClear)
                {
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Failed: wrong target • Escape search: {0:0}s",
                        CurrentHeatState.SearchTimeRemainingSeconds);
                }

                return "Failed: wrong target";
            }

            if (_awaitingSearchClear)
            {
                return string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Escape search: {0:0}s",
                    CurrentHeatState.SearchTimeRemainingSeconds);
            }

            if (_completionPending)
            {
                return "Ready to claim";
            }

            if (hasActiveContract)
            {
                return "Active contract";
            }

            return hasAvailableContract ? "Available contract" : "No contracts available";
        }

        private static string BuildRestrictionsText(AssassinationContractDefinition definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            return definition.BuildRestrictionsText();
        }

        private static string BuildFailureConditionsText(AssassinationContractDefinition definition)
        {
            if (definition == null)
            {
                return "Manual cancel";
            }

            return definition.BuildFailureConditionsText();
        }

        private void RaiseMurderHeatIfNeeded(bool wasExposed)
        {
            if (!wasExposed)
            {
                return;
            }

            _policeHeatRuntime.ReportCrime(CrimeType.Murder);
            _policeHeatRuntime.ReportLineOfSightAcquired();
            _policeHeatRuntime.ReportLineOfSightLost();
        }

        private void ResetPendingResolution()
        {
            _awaitingSearchClear = false;
            _completionPending = false;
            _pendingPayoutAmount = 0;
        }

        private void ClearFailedContractState()
        {
            _failedDefinition = null;
        }

        private bool CanCancelActiveContract()
        {
            return _contractController.ActiveContract != null
                   && !_completionPending
                   && !_awaitingSearchClear;
        }

        private bool CanClearFailedContract()
        {
            return _contractController.ActiveContract == null
                   && _failedDefinition != null;
        }

        private bool CanClaimCompletedContractReward()
        {
            return _contractController.ActiveContract != null
                   && _completionPending
                   && !_awaitingSearchClear
                   && (_pendingPayoutAmount <= 0 || _payoutReceiver != null);
        }

        private static bool ShouldFailOnWrongTarget(AssassinationContractDefinition definition)
        {
            return definition != null && definition.FailsOnWrongTargetKill;
        }
    }
}
