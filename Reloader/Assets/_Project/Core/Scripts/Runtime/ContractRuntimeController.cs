using Reloader.Core.Runtime;

namespace Reloader.Contracts.Runtime
{
    public sealed class ContractRuntimeController
    {
        public AssassinationContractRuntimeState ActiveContract { get; private set; }
        public AssassinationContractDefinition ActiveDefinition { get; private set; }

        public bool TryAcceptContract(AssassinationContractDefinition definition)
        {
            if (definition == null || ActiveContract != null)
            {
                return false;
            }

            ActiveDefinition = definition;
            ActiveContract = new AssassinationContractRuntimeState(
                definition.ContractId,
                definition.TargetId,
                definition.DistanceBand,
                definition.Payout);

            RuntimeKernelBootstrapper.ContractEvents?.RaiseContractAccepted(ActiveContract.ContractId);
            return true;
        }

        public bool TryCompleteActiveContract()
        {
            if (ActiveContract == null)
            {
                return false;
            }

            var completed = ActiveContract;
            ActiveContract = null;
            ActiveDefinition = null;
            RuntimeKernelBootstrapper.ContractEvents?.RaiseContractCompleted(completed.ContractId, completed.Payout);
            return true;
        }

        public bool TryFailActiveContract()
        {
            if (ActiveContract == null)
            {
                return false;
            }

            var failed = ActiveContract;
            ActiveContract = null;
            ActiveDefinition = null;
            RuntimeKernelBootstrapper.ContractEvents?.RaiseContractFailed(failed.ContractId);
            return true;
        }

        public void ClearActiveContract()
        {
            ActiveContract = null;
            ActiveDefinition = null;
        }

        internal void RestoreState(
            AssassinationContractDefinition activeDefinition,
            AssassinationContractRuntimeState activeContract)
        {
            ActiveDefinition = activeDefinition;
            ActiveContract = activeContract;
        }
    }
}
