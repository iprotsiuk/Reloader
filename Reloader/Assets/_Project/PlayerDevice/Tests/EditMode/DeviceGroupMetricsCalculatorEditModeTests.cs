using NUnit.Framework;
using Reloader.PlayerDevice.Runtime;
using UnityEngine;

namespace Reloader.PlayerDevice.Tests.EditMode
{
    public class DeviceGroupMetricsCalculatorEditModeTests
    {
        [Test]
        public void Calculate_WithTwoValidShots_UsesAngularSpaceAndReturnsMoa()
        {
            var shots = new[]
            {
                new DeviceGroupMetricsCalculator.ShotSample(new Vector2(0f, 0f), 100f),
                new DeviceGroupMetricsCalculator.ShotSample(new Vector2(0.0254f, 0f), 100f),
            };

            var metrics = DeviceGroupMetricsCalculator.Calculate(shots);

            Assert.That(metrics.ShotCount, Is.EqualTo(2));
            Assert.That(metrics.ValidShotCount, Is.EqualTo(2));
            Assert.That(metrics.IsMoaAvailable, Is.True);
            Assert.That(metrics.AngularSpreadRadians, Is.GreaterThan(0d));
            Assert.That(metrics.Moa, Is.GreaterThan(0d));
        }

        [Test]
        public void Calculate_WithFewerThanTwoValidShots_ReturnsUnavailableMoa()
        {
            var shots = new[]
            {
                new DeviceGroupMetricsCalculator.ShotSample(new Vector2(0f, 0f), 0f),
                new DeviceGroupMetricsCalculator.ShotSample(new Vector2(0.1f, 0f), 100f),
            };

            var metrics = DeviceGroupMetricsCalculator.Calculate(shots);

            Assert.That(metrics.ShotCount, Is.EqualTo(2));
            Assert.That(metrics.ValidShotCount, Is.EqualTo(1));
            Assert.That(metrics.IsMoaAvailable, Is.False);
            Assert.That(metrics.AngularSpreadRadians, Is.EqualTo(0d));
            Assert.That(metrics.Moa, Is.EqualTo(0d));
        }

        [Test]
        public void Calculate_ExcludesInvalidDistancesDeterministically_AndUsesMaxPairDelta()
        {
            var shots = new[]
            {
                new DeviceGroupMetricsCalculator.ShotSample(new Vector2(0f, 0f), 100f),
                new DeviceGroupMetricsCalculator.ShotSample(new Vector2(0.05f, 0f), 100f),
                new DeviceGroupMetricsCalculator.ShotSample(new Vector2(0f, 0.05f), 100f),
                new DeviceGroupMetricsCalculator.ShotSample(new Vector2(1000f, 1000f), -10f),
            };

            var metrics = DeviceGroupMetricsCalculator.Calculate(shots);

            var theta = Mathf.Atan(0.05f / 100f);
            var expectedSpreadRadians = Mathf.Sqrt((theta * theta) + (theta * theta));
            var expectedMoa = expectedSpreadRadians * Mathf.Rad2Deg * 60f;

            Assert.That(metrics.ShotCount, Is.EqualTo(4));
            Assert.That(metrics.ValidShotCount, Is.EqualTo(3));
            Assert.That(metrics.IsMoaAvailable, Is.True);
            Assert.That(metrics.AngularSpreadRadians, Is.EqualTo(expectedSpreadRadians).Within(1e-8));
            Assert.That(metrics.Moa, Is.EqualTo(expectedMoa).Within(1e-6));
        }
    }
}
