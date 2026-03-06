namespace Reloader.Core.Runtime
{
    public interface IRuntimeEvents
    {
        IContractEvents ContractEvents { get; }
        IInventoryEvents InventoryEvents { get; }
        IWeaponEvents WeaponEvents { get; }
        IShopEvents ShopEvents { get; }
        IUiStateEvents UiStateEvents { get; }
        IInteractionHintEvents InteractionHintEvents { get; }
    }
}
