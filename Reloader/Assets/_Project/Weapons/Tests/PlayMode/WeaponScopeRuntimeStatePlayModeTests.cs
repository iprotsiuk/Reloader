using NUnit.Framework;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class WeaponScopeRuntimeStatePlayModeTests
    {
        [Test]
        public void Constructor_UsesConfiguredDefaultZoomAndZero()
        {
            var config = WeaponScopeConfiguration.Create(
                true,
                5f,
                25f,
                12f,
                "ebr-7c",
                100,
                25);
            var state = new WeaponScopeRuntimeState(config);

            Assert.That(state.CurrentZoom, Is.EqualTo(12f));
            Assert.That(state.CurrentZeroMeters, Is.EqualTo(100));
        }

        [Test]
        public void ApplyZoomDelta_ClampsBetweenMinAndMax()
        {
            var config = WeaponScopeConfiguration.Create(
                true,
                5f,
                25f,
                5f,
                "ebr-7c",
                100,
                25);
            var state = new WeaponScopeRuntimeState(config);

            state.ApplyZoomDelta(100f);
            Assert.That(state.CurrentZoom, Is.EqualTo(25f));

            state.ApplyZoomDelta(-100f);
            Assert.That(state.CurrentZoom, Is.EqualTo(5f));
        }

        [Test]
        public void ApplyZeroSteps_UsesConfiguredStepSize()
        {
            var config = WeaponScopeConfiguration.Create(
                true,
                5f,
                25f,
                10f,
                "ebr-7c",
                100,
                25);
            var state = new WeaponScopeRuntimeState(config);

            state.ApplyZeroSteps(2);
            Assert.That(state.CurrentZeroMeters, Is.EqualTo(150));

            state.ApplyZeroSteps(-20);
            Assert.That(state.CurrentZeroMeters, Is.EqualTo(0));
        }

        [Test]
        public void ApplyWindageAndElevationClicks_TracksOpticAdjustmentState()
        {
            var config = WeaponScopeConfiguration.Create(
                true,
                5f,
                25f,
                10f,
                "ebr-7c",
                100,
                25);
            var state = new WeaponScopeRuntimeState(config);

            state.ApplyWindageClicks(3);
            state.ApplyElevationClicks(-2);

            Assert.That(state.CurrentWindageClicks, Is.EqualTo(3));
            Assert.That(state.CurrentElevationClicks, Is.EqualTo(-2));
            Assert.That(state.CurrentZeroMeters, Is.EqualTo(100), "Windage/elevation adjustments should not be stored as zero-meter hacks.");
        }

        [Test]
        public void AdjustmentSnapshot_RestoresZoomZeroWindageAndElevation()
        {
            var config = WeaponScopeConfiguration.Create(
                true,
                5f,
                25f,
                10f,
                "ebr-7c",
                100,
                25);
            var state = new WeaponScopeRuntimeState(config);

            state.ApplyZoomDelta(4f);
            state.ApplyZeroSteps(2);
            state.ApplyWindageClicks(-5);
            state.ApplyElevationClicks(7);

            var snapshot = state.CreateAdjustmentSnapshot();

            var restored = new WeaponScopeRuntimeState(config);
            restored.RestoreAdjustmentSnapshot(snapshot);

            Assert.That(restored.CurrentZoom, Is.EqualTo(state.CurrentZoom));
            Assert.That(restored.CurrentZeroMeters, Is.EqualTo(state.CurrentZeroMeters));
            Assert.That(restored.CurrentWindageClicks, Is.EqualTo(-5));
            Assert.That(restored.CurrentElevationClicks, Is.EqualTo(7));
        }
    }
}
