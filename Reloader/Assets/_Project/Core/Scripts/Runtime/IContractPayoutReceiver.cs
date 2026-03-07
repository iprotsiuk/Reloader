namespace Reloader.Contracts.Runtime
{
    public interface IContractPayoutReceiver
    {
        bool TryAwardContractPayout(int amount);
    }
}
