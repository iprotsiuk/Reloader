using System;
using System.Collections.Generic;

namespace Reloader.PlayerDevice.Runtime
{
    public sealed class PlayerDeviceRuntimeState
    {
        private readonly HashSet<DeviceAttachmentType> _installedAttachments = new HashSet<DeviceAttachmentType>();
        private readonly List<DeviceGroupSession> _savedGroupSessions = new List<DeviceGroupSession>();

        private DeviceTargetBinding _selectedTargetBinding;

        public PlayerDeviceRuntimeState()
        {
            ActiveGroupSession = new DeviceGroupSession();
            NotesText = string.Empty;
            _selectedTargetBinding = DeviceTargetBinding.None;
        }

        public bool IsDeviceAvailable => true;

        public bool IsNotesAvailable => true;

        public string NotesText { get; set; }

        public DeviceTargetBinding SelectedTargetBinding => _selectedTargetBinding;

        public bool HasSelectedTargetBinding => _selectedTargetBinding.IsValid;

        public DeviceGroupSession ActiveGroupSession { get; private set; }

        public IReadOnlyList<DeviceGroupSession> SavedGroupSessions => _savedGroupSessions;

        public bool IsAttachmentInstalled(DeviceAttachmentType attachmentType)
        {
            return _installedAttachments.Contains(attachmentType);
        }

        public void InstallAttachment(DeviceAttachmentType attachmentType)
        {
            if (attachmentType == DeviceAttachmentType.None)
            {
                return;
            }

            _installedAttachments.Add(attachmentType);
        }

        public void UninstallAttachment(DeviceAttachmentType attachmentType)
        {
            if (attachmentType == DeviceAttachmentType.None)
            {
                return;
            }

            _installedAttachments.Remove(attachmentType);
        }

        public DeviceAttachmentState GetAttachmentState(DeviceAttachmentType attachmentType)
        {
            return new DeviceAttachmentState(attachmentType, IsAttachmentInstalled(attachmentType));
        }

        public void SetSelectedTargetBinding(DeviceTargetBinding binding)
        {
            _selectedTargetBinding = binding;
        }

        public void SetSelectedTargetBinding(IRangeTargetMetrics metrics)
        {
            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            _selectedTargetBinding = new DeviceTargetBinding(metrics.TargetId, metrics.DisplayName, metrics.DistanceMeters);
        }

        public void ClearSelectedTargetBinding()
        {
            _selectedTargetBinding = DeviceTargetBinding.None;
        }

        public void AddShotSampleToActiveGroup(DeviceShotSample sample)
        {
            ActiveGroupSession.AddShotSample(sample);
        }

        public void SaveCurrentGroup()
        {
            _savedGroupSessions.Add(ActiveGroupSession.Clone());
        }

        public void ClearActiveGroup()
        {
            ActiveGroupSession = new DeviceGroupSession();
        }
    }
}
