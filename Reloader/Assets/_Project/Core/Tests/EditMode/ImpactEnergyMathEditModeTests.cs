using NUnit.Framework;
using Reloader.Core.Runtime;

namespace Reloader.Core.Tests.EditMode
{
    public sealed class ImpactEnergyMathEditModeTests
    {
        [Test]
        public void ComputeDeliveredEnergyJoules_Default308MuzzleVelocityAndMass_ComputesExpectedEnergy()
        {
            var energyJoules = ImpactEnergyMath.ComputeDeliveredEnergyJoules(847.344f, 147f);

            Assert.That(energyJoules, Is.EqualTo(3419.6f).Within(0.5f));
        }
    }
}
