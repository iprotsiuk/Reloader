namespace Reloader.Inventory
{
    public readonly struct ContainerPermissions
    {
        public bool CanDragOut { get; }
        public bool CanDropIn { get; }
        public bool CanReorder { get; }
        public bool CanSplit { get; }
        public bool CanMerge { get; }

        public static ContainerPermissions PlayerMutable => new ContainerPermissions(
            canDragOut: true,
            canDropIn: true,
            canReorder: true,
            canSplit: true,
            canMerge: true);

        public static ContainerPermissions ReadOnlyVendor => new ContainerPermissions(
            canDragOut: false,
            canDropIn: false,
            canReorder: false,
            canSplit: false,
            canMerge: false);

        public ContainerPermissions(
            bool canDragOut,
            bool canDropIn,
            bool canReorder,
            bool canSplit,
            bool canMerge)
        {
            CanDragOut = canDragOut;
            CanDropIn = canDropIn;
            CanReorder = canReorder;
            CanSplit = canSplit;
            CanMerge = canMerge;
        }
    }
}
