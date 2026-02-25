namespace Reloader.Inventory
{
    public interface IInventoryDefinitionPickupTarget : IInventoryPickupTarget
    {
        ItemSpawnDefinition SpawnDefinition { get; }
    }
}
