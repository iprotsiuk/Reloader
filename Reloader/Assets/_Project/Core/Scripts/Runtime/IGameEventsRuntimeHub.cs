namespace Reloader.Core.Runtime
{
    public interface IGameEventsRuntimeHub : IRuntimeEvents, IInventoryEvents, IWeaponEvents, IShopEvents, IUiStateEvents, IInteractionHintEvents
    {
        event System.Action<string> OnContractAccepted;
        event System.Action<string> OnContractFailed;
        event System.Action<string, float> OnContractCompleted;

        void RaiseContractAccepted(string contractId);
        void RaiseContractFailed(string contractId);
        void RaiseContractCompleted(string contractId, float payout);
    }
}
