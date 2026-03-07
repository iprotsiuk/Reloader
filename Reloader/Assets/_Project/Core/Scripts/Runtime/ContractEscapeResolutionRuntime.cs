using System;
using Reloader.Core.Events;
using Reloader.Core.Runtime;

namespace Reloader.Contracts.Runtime
{
    public sealed class ContractEscapeResolutionRuntime
    {
        private readonly ContractRuntimeController _contractController = new ContractRuntimeController();
        private readonly PoliceHeatRuntime _policeHeatRuntime;
        private AssassinationContractDefinition _availableContract;
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

        public void SetAvailableContract(AssassinationContractDefinition availableContract)
        {
            _availableContract = availableContract;
            _offerConsumed = false;
            ResetPendingResolution();
            _contractController.ClearActiveContract();
            _policeHeatRuntime.ForceClear();
        }

        public void ConfigurePayoutReceiver(IContractPayoutReceiver payoutReceiver)
        {
            _payoutReceiver = payoutReceiver;
            TryResolvePendingPayout();
        }

        public bool TryGetSnapshot(out ContractOfferSnapshot snapshot)
        {
            var activeDefinition = _contractController.ActiveDefinition;
            var activeContract = _contractController.ActiveContract;
            var hasAvailableContract = !_offerConsumed && _availableContract != null && activeContract == null;
            var definition = activeDefinition ?? (hasAvailableContract ? _availableContract : null);
            if (definition == null)
            {
                snapshot = default;
                return false;
            }

            snapshot = new ContractOfferSnapshot(
                hasAvailableContract: hasAvailableContract,
                hasActiveContract: activeContract != null,
                contractId: definition.ContractId,
                title: definition.Title,
                targetId: definition.TargetId,
                targetDisplayName: definition.TargetDisplayName,
                targetDescription: definition.TargetDescription,
                briefingText: definition.BriefingText,
                distanceBandMeters: activeContract != null ? activeContract.DistanceBand : definition.DistanceBand,
                payout: activeContract != null ? activeContract.Payout : definition.Payout,
                canAccept: hasAvailableContract,
                statusText: BuildStatusText(hasAvailableContract, activeContract != null));
            return true;
        }

        public bool AcceptAvailableContract()
        {
            if (_offerConsumed || _availableContract == null || _contractController.ActiveContract != null)
            {
                return false;
            }

            ResetPendingResolution();
            var accepted = _contractController.TryAcceptContract(_availableContract);
            if (accepted)
            {
                _offerConsumed = true;
                _policeHeatRuntime.ForceClear();
            }

            return accepted;
        }

        public bool ReportTargetEliminated(string targetId, bool wasExposed)
        {
            var activeContract = _contractController.ActiveContract;
            if (activeContract == null || string.IsNullOrWhiteSpace(targetId))
            {
                return false;
            }

            var isCorrectTarget = string.Equals(activeContract.TargetId, targetId, StringComparison.Ordinal);
            if (!isCorrectTarget)
            {
                RaiseMurderHeatIfNeeded(wasExposed);
                ResetPendingResolution();
                _contractController.TryFailActiveContract();
                return false;
            }

            if (_completionPending || _awaitingSearchClear)
            {
                return true;
            }

            _completionPending = true;
            _pendingPayoutAmount = Math.Max(0, activeContract.Payout);
            if (wasExposed)
            {
                _awaitingSearchClear = true;
                RaiseMurderHeatIfNeeded(true);
                return true;
            }

            TryResolvePendingPayout();
            return true;
        }

        public void Advance(float deltaTimeSeconds)
        {
            _policeHeatRuntime.Advance(deltaTimeSeconds);
            if (_awaitingSearchClear && _policeHeatRuntime.CurrentState.Level == PoliceHeatLevel.Clear)
            {
                _awaitingSearchClear = false;
            }

            TryResolvePendingPayout();
        }

        private string BuildStatusText(bool hasAvailableContract, bool hasActiveContract)
        {
            if (_awaitingSearchClear)
            {
                return string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Escape search: {0:0}s",
                    CurrentHeatState.SearchTimeRemainingSeconds);
            }

            if (_pendingPayoutAmount > 0)
            {
                return "Processing payout";
            }

            if (hasActiveContract)
            {
                return "Active contract";
            }

            return hasAvailableContract ? "Available contract" : "No contracts available";
        }

        private void TryResolvePendingPayout()
        {
            if (_awaitingSearchClear || !_completionPending || _contractController.ActiveContract == null)
            {
                return;
            }

            if (_pendingPayoutAmount > 0)
            {
                if (_payoutReceiver == null || !_payoutReceiver.TryAwardContractPayout(_pendingPayoutAmount))
                {
                    return;
                }
            }

            _completionPending = false;
            _pendingPayoutAmount = 0;
            _contractController.TryCompleteActiveContract();
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
    }
}
