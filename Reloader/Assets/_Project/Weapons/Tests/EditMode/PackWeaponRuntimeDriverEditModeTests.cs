using NUnit.Framework;
using Reloader.Weapons.PackRuntime;

namespace Reloader.Weapons.Tests.EditMode
{
    public sealed class PackWeaponRuntimeDriverEditModeTests
    {
        [Test]
        public void ReloadStartedWhileAiming_PreservesAdsWhileAimRemainsHeld()
        {
            var state = new PackWeaponRuntimeState("weapon-kar98k");
            state.SetEquipped(true);

            var config = new PackWeaponPresentationConfig();
            var driver = new PackWeaponRuntimeDriver(state, config);

            driver.TickAimFov(aimHeld: true, currentFov: 60f, baseFov: 60f, deltaTime: 1f / 60f);
            Assert.That(state.IsAiming, Is.True, "Precondition failed: ADS should be active before reload starts.");

            var started = driver.TryStartReload(now: 1f, durationSeconds: 0.5f);

            Assert.That(started, Is.True, "Reload should start in the regression scenario.");
            Assert.That(state.IsReloading, Is.True);
            Assert.That(state.IsAiming, Is.True,
                "Reload should not cancel ADS when the player began reloading while already aiming.");

            driver.TickAimFov(aimHeld: true, currentFov: 45f, baseFov: 60f, deltaTime: 1f / 60f);

            Assert.That(state.IsAiming, Is.True,
                "Holding ADS through reload should keep ADS active instead of briefly dropping out and snapping back.");
        }
    }
}
