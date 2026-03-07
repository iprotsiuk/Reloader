namespace Reloader.UI.Toolkit.TabInventory
{
    public interface ITabInventoryContractController
    {
        bool TryGetStatus(out TabInventoryContractStatus status);
        bool AcceptAvailableContract();
        bool CancelActiveContract();
        bool ClaimCompletedContractReward();
    }
}
