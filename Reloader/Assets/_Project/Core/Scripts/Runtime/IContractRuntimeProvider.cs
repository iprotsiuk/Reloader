namespace Reloader.Contracts.Runtime
{
    public interface IContractRuntimeProvider
    {
        bool TryGetContractSnapshot(out ContractOfferSnapshot snapshot);
        bool AcceptAvailableContract();
        bool CancelActiveContract();
        bool ClearFailedContract();
        bool ClaimCompletedContractReward();
    }
}
