namespace Reloader.Inventory
{
    public static class StorageUiSession
    {
        public static bool IsOpen { get; private set; }
        public static string ActiveContainerId { get; private set; }

        public static void Open(string containerId)
        {
            if (string.IsNullOrWhiteSpace(containerId))
            {
                return;
            }

            ActiveContainerId = containerId.Trim();
            IsOpen = true;
        }

        public static void Close()
        {
            IsOpen = false;
            ActiveContainerId = null;
        }
    }
}
