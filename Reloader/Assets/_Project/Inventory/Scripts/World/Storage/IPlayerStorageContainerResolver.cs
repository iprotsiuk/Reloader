namespace Reloader.Inventory
{
    public interface IPlayerStorageContainerResolver
    {
        bool TryResolveStorageContainer(out WorldStorageContainer container);
    }
}
