namespace Reloader.Inventory
{
    public enum InventoryTransferResult
    {
        None = 0,
        InvalidSource = 1,
        InvalidTarget = 2,
        PermissionDenied = 3,
        NoChange = 4,
        Moved = 5,
        Swapped = 6,
        MergedFull = 7,
        MergedPartial = 8
    }
}
