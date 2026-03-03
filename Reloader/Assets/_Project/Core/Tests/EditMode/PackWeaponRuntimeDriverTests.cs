using NUnit.Framework;
using Reloader.Weapons.PackRuntime;

namespace Reloader.Core.Tests.EditMode
{
    public class PackWeaponRuntimeDriverTests
    {
        [Test]
        public void TickAim_ChangesStateAndInterpolatesTowardAdsFov()
        {
            var state = new PackWeaponRuntimeState("weapon-rifle-01");
            var config = new PackWeaponPresentationConfig();
            var driver = new PackWeaponRuntimeDriver(state, config);

            driver.SetEquipped(true);
            Assert.That(driver.State.IsEquipped, Is.True);
            Assert.That(config.AdsFieldOfView, Is.LessThan(60f));
            var fov = 60f;
            var next = driver.TickAimFov(aimHeld: true, currentFov: fov, baseFov: 60f);

            Assert.That(driver.State.IsAiming, Is.True);
            Assert.That(next, Is.LessThan(fov));
        }

        [Test]
        public void ReloadLifecycle_StartCancelCompleteTransitionsRuntimeState()
        {
            var state = new PackWeaponRuntimeState("weapon-rifle-01");
            var driver = new PackWeaponRuntimeDriver(state, new PackWeaponPresentationConfig());
            driver.SetEquipped(true);
            Assert.That(driver.State.IsEquipped, Is.True);

            var started = driver.TryStartReload(now: 10f, durationSeconds: 0.5f);
            var completedEarly = driver.TryCompleteReload(now: 10.2f);
            var cancelled = driver.CancelReload();
            var startedAgain = driver.TryStartReload(now: 20f, durationSeconds: 0.5f);
            var completed = driver.TryCompleteReload(now: 20.6f);

            Assert.That(started, Is.True);
            Assert.That(completedEarly, Is.False);
            Assert.That(cancelled, Is.True);
            Assert.That(startedAgain, Is.True);
            Assert.That(completed, Is.True);
            Assert.That(driver.State.IsReloading, Is.False);
        }
    }
}
