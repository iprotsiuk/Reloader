using NUnit.Framework;
using Reloader.Core.Runtime;
using Reloader.Player.Viewmodel;
using UnityEngine;

namespace Reloader.Player.Tests.PlayMode
{
    public class ViewmodelAnimationAdapterWeaponEventsPlayModeTests
    {
        [Test]
        public void Configure_WithInjectedWeaponEvents_RuntimeKernelEventsDoNotDriveAdapter()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeWeaponEvents = new DefaultRuntimeEvents();
            var root = new GameObject("ViewmodelAdapterInjectedEvents");
            var adapter = root.AddComponent<ViewmodelAnimationAdapter>();
            adapter.SetEquippedItemIdForTests("weapon-injected");

            var injectedWeaponEvents = new DefaultRuntimeEvents();
            adapter.ConfigureEventChannel(injectedWeaponEvents);

            try
            {
                RuntimeKernelBootstrapper.Events = runtimeWeaponEvents;
                runtimeWeaponEvents.RaiseWeaponReloadStarted("weapon-injected");
                runtimeWeaponEvents.RaiseWeaponFired("weapon-injected", Vector3.zero, Vector3.forward);
                runtimeWeaponEvents.RaiseWeaponAimChanged("weapon-injected", true);

                Assert.That(adapter.IsReloadingDebug, Is.False);
                Assert.That(adapter.FireTriggerCountDebug, Is.EqualTo(0));
                Assert.That(adapter.IsAimingDebug, Is.False);

                injectedWeaponEvents.RaiseWeaponReloadStarted("weapon-injected");
                injectedWeaponEvents.RaiseWeaponFired("weapon-injected", Vector3.zero, Vector3.forward);
                injectedWeaponEvents.RaiseWeaponAimChanged("weapon-injected", true);

                Assert.That(adapter.IsReloadingDebug, Is.True);
                Assert.That(adapter.FireTriggerCountDebug, Is.EqualTo(1));
                Assert.That(adapter.IsAimingDebug, Is.True);
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Configure_WithoutInjectedWeaponEvents_RebindsWhenRuntimeKernelHubIsReplaced()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = initialHub;

            var root = new GameObject("ViewmodelAdapterFallbackRebind");
            var adapter = root.AddComponent<ViewmodelAnimationAdapter>();
            adapter.SetEquippedItemIdForTests("weapon-fallback");

            try
            {
                initialHub.RaiseWeaponFired("weapon-fallback", Vector3.zero, Vector3.forward);
                Assert.That(adapter.FireTriggerCountDebug, Is.EqualTo(1));

                RuntimeKernelBootstrapper.Events = replacementHub;

                initialHub.RaiseWeaponFired("weapon-fallback", Vector3.zero, Vector3.forward);
                Assert.That(adapter.FireTriggerCountDebug, Is.EqualTo(1));

                replacementHub.RaiseWeaponFired("weapon-fallback", Vector3.zero, Vector3.forward);
                Assert.That(adapter.FireTriggerCountDebug, Is.EqualTo(2));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
            }
        }
    }
}
