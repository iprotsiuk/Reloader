using NUnit.Framework;
using Reloader.NPCs.Combat;

namespace Reloader.NPCs.Tests.EditMode
{
    public sealed class HumanoidImpactResolutionEditModeTests
    {
        [Test]
        public void Resolve_WhenHeadHitCarriesRifleEnergy_ReturnsLethal()
        {
            var result = HumanoidImpactResolution.Resolve(
                bodyZone: HumanoidBodyZone.Head,
                deliveredEnergyJoules: 900f);

            Assert.That(result.IsLethal, Is.True);
        }

        [Test]
        public void Resolve_WhenNeckHitCarriesRifleEnergy_ReturnsLethal()
        {
            var result = HumanoidImpactResolution.Resolve(
                bodyZone: HumanoidBodyZone.Neck,
                deliveredEnergyJoules: 900f);

            Assert.That(result.IsLethal, Is.True);
        }

        [Test]
        public void Resolve_WhenArmLHitCarriesRifleEnergy_ReturnsNonLethal()
        {
            var result = HumanoidImpactResolution.Resolve(
                bodyZone: HumanoidBodyZone.ArmL,
                deliveredEnergyJoules: 900f);

            Assert.That(result.IsLethal, Is.False);
        }

        [Test]
        public void Resolve_WhenTorsoHitCarriesLowEnergy_ReturnsNonLethal()
        {
            var result = HumanoidImpactResolution.Resolve(
                bodyZone: HumanoidBodyZone.Torso,
                deliveredEnergyJoules: 80f);

            Assert.That(result.IsLethal, Is.False);
        }
    }
}
