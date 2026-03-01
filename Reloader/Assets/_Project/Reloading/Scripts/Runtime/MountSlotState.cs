namespace Reloader.Reloading.Runtime
{
    public sealed class MountSlotState
    {
        public MountSlotState(MountSlotDefinition definition, MountNode ownerNode)
        {
            Definition = definition;
            OwnerNode = ownerNode;
        }

        public MountSlotDefinition Definition { get; }

        public MountNode OwnerNode { get; }

        public MountNode MountedNode { get; private set; }

        public bool IsOccupied => MountedNode != null;

        public void SetMountedNode(MountNode node)
        {
            MountedNode = node;
        }
    }
}
