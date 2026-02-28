namespace Reloader.Inventory
{
    public static class StorageRuntimeBridge
    {
        private static StorageContainerRegistry _registry = new StorageContainerRegistry();

        public static StorageContainerRegistry Registry => _registry;

        public static void ResetForTests()
        {
            _registry = new StorageContainerRegistry();
            StorageUiSession.Close();
        }
    }
}
