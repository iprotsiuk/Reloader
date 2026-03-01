namespace Reloader.PlayerDevice.Runtime
{
    public readonly struct DeviceAttachmentState
    {
        public DeviceAttachmentState(DeviceAttachmentType attachmentType, bool isInstalled)
        {
            AttachmentType = attachmentType;
            IsInstalled = isInstalled;
        }

        public DeviceAttachmentType AttachmentType { get; }

        public bool IsInstalled { get; }
    }
}
