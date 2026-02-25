using NUnit.Framework;
using Reloader.Weapons.Ballistics;

namespace Reloader.Core.Tests.EditMode
{
    public class CartridgeBallisticSpecBuilderTests
    {
        [Test]
        public void Build_WithFactorySnapshot_ProducesSpecInFps()
        {
            var snapshot = new AmmoBallisticSnapshot(
                ammoSource: AmmoSourceType.Factory,
                muzzleVelocityFps: 2650f,
                velocityStdDevFps: 18f,
                projectileMassGrains: 168f,
                ballisticCoefficientG1: 0.462f,
                dispersionMoa: 1.2f);

            var spec = CartridgeBallisticSpecBuilder.Build(snapshot, rngSample01: 0.5f);

            Assert.That(spec.MuzzleVelocityFps, Is.GreaterThan(0f));
            Assert.That(spec.BallisticCoefficientG1, Is.EqualTo(0.462f).Within(0.0001f));
            Assert.That(spec.ProjectileMassGrains, Is.EqualTo(168f).Within(0.0001f));
        }

        [Test]
        public void Build_WithRngSpread_AppliesVelocityStdDeviation()
        {
            var snapshot = new AmmoBallisticSnapshot(
                ammoSource: AmmoSourceType.Handload,
                muzzleVelocityFps: 2500f,
                velocityStdDevFps: 40f,
                projectileMassGrains: 175f,
                ballisticCoefficientG1: 0.51f,
                dispersionMoa: 0.9f);

            var low = CartridgeBallisticSpecBuilder.Build(snapshot, rngSample01: 0f);
            var high = CartridgeBallisticSpecBuilder.Build(snapshot, rngSample01: 1f);

            Assert.That(low.MuzzleVelocityFps, Is.LessThan(high.MuzzleVelocityFps));
            Assert.That(low.MuzzleVelocityFps, Is.EqualTo(2460f).Within(0.0001f));
            Assert.That(high.MuzzleVelocityFps, Is.EqualTo(2540f).Within(0.0001f));
        }

        [Test]
        public void Build_ClampsInvalidInputs_ToSafeMinimums()
        {
            var snapshot = new AmmoBallisticSnapshot(
                ammoSource: AmmoSourceType.Factory,
                muzzleVelocityFps: -5f,
                velocityStdDevFps: -10f,
                projectileMassGrains: -1f,
                ballisticCoefficientG1: -1f,
                dispersionMoa: -2f);

            var spec = CartridgeBallisticSpecBuilder.Build(snapshot, rngSample01: 0.5f);

            Assert.That(spec.MuzzleVelocityFps, Is.EqualTo(1f));
            Assert.That(spec.ProjectileMassGrains, Is.EqualTo(1f));
            Assert.That(spec.BallisticCoefficientG1, Is.EqualTo(0.01f));
            Assert.That(spec.DispersionMoa, Is.EqualTo(0f));
        }
    }
}
