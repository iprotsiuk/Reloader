using System;

namespace Reloader.PlayerDevice.Runtime
{
    public readonly struct DeviceGroupMetrics
    {
        public static readonly DeviceGroupMetrics Unavailable = new(
            shotCount: 0,
            validShotCount: 0,
            isMoaAvailable: false,
            linearSpreadMeters: 0d,
            angularSpreadRadians: 0d,
            moa: 0d);

        public DeviceGroupMetrics(
            int shotCount,
            int validShotCount,
            bool isMoaAvailable,
            double linearSpreadMeters,
            double angularSpreadRadians,
            double moa)
        {
            ShotCount = shotCount;
            ValidShotCount = validShotCount;
            IsMoaAvailable = isMoaAvailable;
            LinearSpreadMeters = linearSpreadMeters;
            AngularSpreadRadians = angularSpreadRadians;
            Moa = moa;
        }

        public int ShotCount { get; }

        public int ValidShotCount { get; }

        public bool IsMoaAvailable { get; }

        public double LinearSpreadMeters { get; }

        public double AngularSpreadRadians { get; }

        public double Moa { get; }
    }
}
