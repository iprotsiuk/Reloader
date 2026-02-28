using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.World.Travel;

namespace Reloader.World.Tests.EditMode
{
    public class WorldTravelCoordinatorUiResetEditModeTests
    {
        [Test]
        public void ResetRuntimeUiStateAfterTravel_ClosesStorageUiSession()
        {
            var storageUiSessionType = Type.GetType("Reloader.Inventory.StorageUiSession, Reloader.Inventory");
            Assert.That(storageUiSessionType, Is.Not.Null, "StorageUiSession type was not found.");

            var openMethod = storageUiSessionType.GetMethod("Open", BindingFlags.Public | BindingFlags.Static);
            var closeMethod = storageUiSessionType.GetMethod("Close", BindingFlags.Public | BindingFlags.Static);
            var isOpenProperty = storageUiSessionType.GetProperty("IsOpen", BindingFlags.Public | BindingFlags.Static);
            var activeContainerProperty = storageUiSessionType.GetProperty("ActiveContainerId", BindingFlags.Public | BindingFlags.Static);

            Assert.That(openMethod, Is.Not.Null);
            Assert.That(closeMethod, Is.Not.Null);
            Assert.That(isOpenProperty, Is.Not.Null);
            Assert.That(activeContainerProperty, Is.Not.Null);

            closeMethod.Invoke(null, null);
            openMethod.Invoke(null, new object[] { "qa.travel.storage" });
            Assert.That((bool)isOpenProperty.GetValue(null), Is.True, "Expected storage UI session to be open before travel reset.");

            var resetMethod = typeof(WorldTravelCoordinator).GetMethod(
                "ResetRuntimeUiStateAfterTravel",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(resetMethod, Is.Not.Null, "ResetRuntimeUiStateAfterTravel method was not found.");

            resetMethod.Invoke(null, null);

            Assert.That((bool)isOpenProperty.GetValue(null), Is.False, "Travel reset should close the storage UI session.");
            Assert.That(activeContainerProperty.GetValue(null), Is.Null, "Travel reset should clear active storage container id.");
        }
    }
}
