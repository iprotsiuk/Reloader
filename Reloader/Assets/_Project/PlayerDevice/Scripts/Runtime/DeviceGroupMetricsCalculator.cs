using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloader.PlayerDevice.Runtime
{
    public static class DeviceGroupMetricsCalculator
    {
        private const double RadiansToMoa = (180d / Math.PI) * 60d;

        public readonly struct ShotSample
        {
            public ShotSample(Vector2 localImpactMeters, float distanceMeters)
            {
                LocalImpactMeters = localImpactMeters;
                DistanceMeters = distanceMeters;
            }

            public Vector2 LocalImpactMeters { get; }

            public float DistanceMeters { get; }
        }

        public static DeviceGroupMetrics Calculate(IReadOnlyList<ShotSample> shots)
        {
            if (shots == null)
            {
                throw new ArgumentNullException(nameof(shots));
            }

            var shotCount = shots.Count;
            var validShots = new List<ShotSample>(shotCount);

            for (var i = 0; i < shotCount; i++)
            {
                var shot = shots[i];
                if (IsValidShot(shot))
                {
                    validShots.Add(shot);
                }
            }

            var validShotCount = validShots.Count;
            if (validShotCount < 2)
            {
                return new DeviceGroupMetrics(
                    shotCount,
                    validShotCount,
                    isMoaAvailable: false,
                    linearSpreadMeters: 0d,
                    angularSpreadRadians: 0d,
                    moa: 0d);
            }

            var maxLinearSpreadMeters = 0d;
            var maxAngularSpreadRadians = 0d;

            for (var i = 0; i < validShotCount - 1; i++)
            {
                var first = validShots[i];
                var thetaXFirst = Math.Atan(first.LocalImpactMeters.x / first.DistanceMeters);
                var thetaYFirst = Math.Atan(first.LocalImpactMeters.y / first.DistanceMeters);

                for (var j = i + 1; j < validShotCount; j++)
                {
                    var second = validShots[j];

                    var dx = second.LocalImpactMeters.x - first.LocalImpactMeters.x;
                    var dy = second.LocalImpactMeters.y - first.LocalImpactMeters.y;
                    var linearSpreadMeters = Math.Sqrt((dx * dx) + (dy * dy));
                    if (linearSpreadMeters > maxLinearSpreadMeters)
                    {
                        maxLinearSpreadMeters = linearSpreadMeters;
                    }

                    var thetaXSecond = Math.Atan(second.LocalImpactMeters.x / second.DistanceMeters);
                    var thetaYSecond = Math.Atan(second.LocalImpactMeters.y / second.DistanceMeters);

                    var deltaX = thetaXSecond - thetaXFirst;
                    var deltaY = thetaYSecond - thetaYFirst;
                    var angularSpreadRadians = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
                    if (angularSpreadRadians > maxAngularSpreadRadians)
                    {
                        maxAngularSpreadRadians = angularSpreadRadians;
                    }
                }
            }

            var moa = maxAngularSpreadRadians * RadiansToMoa;
            return new DeviceGroupMetrics(
                shotCount,
                validShotCount,
                isMoaAvailable: true,
                linearSpreadMeters: maxLinearSpreadMeters,
                angularSpreadRadians: maxAngularSpreadRadians,
                moa: moa);
        }

        public static DeviceGroupMetrics Calculate(IEnumerable<ShotSample> shots)
        {
            if (shots == null)
            {
                throw new ArgumentNullException(nameof(shots));
            }

            var materialized = shots as IReadOnlyList<ShotSample>;
            if (materialized != null)
            {
                return Calculate(materialized);
            }

            return Calculate(new List<ShotSample>(shots));
        }

        private static bool IsValidShot(ShotSample shot)
        {
            if (!float.IsFinite(shot.DistanceMeters) || shot.DistanceMeters <= 0f)
            {
                return false;
            }

            return float.IsFinite(shot.LocalImpactMeters.x) && float.IsFinite(shot.LocalImpactMeters.y);
        }
    }
}
