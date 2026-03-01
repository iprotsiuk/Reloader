using System.Collections.Generic;

namespace Reloader.PlayerDevice.Runtime
{
    public sealed class DeviceGroupSession
    {
        private readonly List<DeviceShotSample> _shotSamples;

        public DeviceGroupSession()
        {
            _shotSamples = new List<DeviceShotSample>();
        }

        public DeviceGroupSession(IReadOnlyList<DeviceShotSample> shotSamples)
        {
            _shotSamples = shotSamples == null ? new List<DeviceShotSample>() : new List<DeviceShotSample>(shotSamples);
        }

        public IReadOnlyList<DeviceShotSample> ShotSamples => _shotSamples;

        public int ShotCount => _shotSamples.Count;

        public bool HasMetrics => ShotCount >= 2;

        public void AddShotSample(DeviceShotSample sample)
        {
            if (!sample.IsValid)
            {
                return;
            }

            _shotSamples.Add(sample);
        }

        public DeviceGroupSession Clone()
        {
            return new DeviceGroupSession(_shotSamples);
        }
    }
}
