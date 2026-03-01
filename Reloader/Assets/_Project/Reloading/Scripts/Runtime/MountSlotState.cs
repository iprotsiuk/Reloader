namespace Reloader.Reloading.Runtime
{
    public sealed class MountSlotState
    {
        public MountSlotState(MountSlotDefinition definition, MountNode ownerNode, string graphSlotId)
        {
            Definition = definition;
            OwnerNode = ownerNode;
            GraphSlotId = graphSlotId;
        }

        public MountSlotDefinition Definition { get; }

        public MountNode OwnerNode { get; }

        public string GraphSlotId { get; }

        public MountNode MountedNode { get; private set; }

        public bool IsOccupied => MountedNode != null;

        public void SetMountedNode(MountNode node)
        {
            MountedNode = node;
        }
    }
}
