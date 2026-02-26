using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using UnityEngine;
using System;

namespace Reloader.Core.Tests.EditMode
{
    public class InventoryEventContractsTests
    {
        [SetUp]
        public void SetUp()
        {
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
        }

        [TearDown]
        public void TearDown()
        {
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
        }

        [Test]
        public void RaiseItemStored_InvokesEventWithPayload()
        {
            string storedItemId = null;
            var storedArea = InventoryArea.Belt;
            var storedIndex = -1;

            void Handler(string itemId, InventoryArea area, int index)
            {
                storedItemId = itemId;
                storedArea = area;
                storedIndex = index;
            }

            RuntimeKernelBootstrapper.InventoryEvents.OnItemStored += Handler;
            try
            {
                RuntimeKernelBootstrapper.InventoryEvents.RaiseItemStored("item-1", InventoryArea.Backpack, 3);
            }
            finally
            {
                RuntimeKernelBootstrapper.InventoryEvents.OnItemStored -= Handler;
            }

            Assert.That(storedItemId, Is.EqualTo("item-1"));
            Assert.That(storedArea, Is.EqualTo(InventoryArea.Backpack));
            Assert.That(storedIndex, Is.EqualTo(3));
        }

        [Test]
        public void RaiseWeaponEquipped_InvokesEventWithPayload()
        {
            string equippedItemId = null;
            void Handler(string itemId) => equippedItemId = itemId;

            RuntimeKernelBootstrapper.WeaponEvents.OnWeaponEquipped += Handler;
            try
            {
                RuntimeKernelBootstrapper.WeaponEvents.RaiseWeaponEquipped("weapon-rifle-01");
            }
            finally
            {
                RuntimeKernelBootstrapper.WeaponEvents.OnWeaponEquipped -= Handler;
            }

            Assert.That(equippedItemId, Is.EqualTo("weapon-rifle-01"));
        }

        [Test]
        public void RaiseWeaponEquipStarted_InvokesEventWithPayload()
        {
            string equippedItemId = null;
            void Handler(string itemId) => equippedItemId = itemId;

            RuntimeKernelBootstrapper.WeaponEvents.OnWeaponEquipStarted += Handler;
            try
            {
                RuntimeKernelBootstrapper.WeaponEvents.RaiseWeaponEquipStarted("weapon-rifle-01");
            }
            finally
            {
                RuntimeKernelBootstrapper.WeaponEvents.OnWeaponEquipStarted -= Handler;
            }

            Assert.That(equippedItemId, Is.EqualTo("weapon-rifle-01"));
        }

        [Test]
        public void RaiseWeaponUnequipStarted_InvokesEventWithPayload()
        {
            string unequippedItemId = null;
            void Handler(string itemId) => unequippedItemId = itemId;

            RuntimeKernelBootstrapper.WeaponEvents.OnWeaponUnequipStarted += Handler;
            try
            {
                RuntimeKernelBootstrapper.WeaponEvents.RaiseWeaponUnequipStarted("weapon-rifle-01");
            }
            finally
            {
                RuntimeKernelBootstrapper.WeaponEvents.OnWeaponUnequipStarted -= Handler;
            }

            Assert.That(unequippedItemId, Is.EqualTo("weapon-rifle-01"));
        }

        [Test]
        public void RaiseWeaponFired_InvokesEventWithPayload()
        {
            string firedItemId = null;
            var firedOrigin = Vector3.zero;
            var firedDirection = Vector3.zero;
            void Handler(string itemId, Vector3 origin, Vector3 direction)
            {
                firedItemId = itemId;
                firedOrigin = origin;
                firedDirection = direction;
            }

            RuntimeKernelBootstrapper.WeaponEvents.OnWeaponFired += Handler;
            try
            {
                RuntimeKernelBootstrapper.WeaponEvents.RaiseWeaponFired("weapon-rifle-01", new Vector3(1f, 2f, 3f), Vector3.forward);
            }
            finally
            {
                RuntimeKernelBootstrapper.WeaponEvents.OnWeaponFired -= Handler;
            }

            Assert.That(firedItemId, Is.EqualTo("weapon-rifle-01"));
            Assert.That(firedOrigin, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(firedDirection, Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void RaiseWeaponReloaded_InvokesEventWithPayload()
        {
            string reloadedItemId = null;
            var magCount = -1;
            var reserveCount = -1;
            void Handler(string itemId, int mag, int reserve)
            {
                reloadedItemId = itemId;
                magCount = mag;
                reserveCount = reserve;
            }

            RuntimeKernelBootstrapper.WeaponEvents.OnWeaponReloaded += Handler;
            try
            {
                RuntimeKernelBootstrapper.WeaponEvents.RaiseWeaponReloaded("weapon-rifle-01", 5, 25);
            }
            finally
            {
                RuntimeKernelBootstrapper.WeaponEvents.OnWeaponReloaded -= Handler;
            }

            Assert.That(reloadedItemId, Is.EqualTo("weapon-rifle-01"));
            Assert.That(magCount, Is.EqualTo(5));
            Assert.That(reserveCount, Is.EqualTo(25));
        }

        [Test]
        public void RaiseWeaponReloadStarted_InvokesEventWithPayload()
        {
            string reloadedItemId = null;
            void Handler(string itemId) => reloadedItemId = itemId;

            RuntimeKernelBootstrapper.WeaponEvents.OnWeaponReloadStarted += Handler;
            try
            {
                RuntimeKernelBootstrapper.WeaponEvents.RaiseWeaponReloadStarted("weapon-rifle-01");
            }
            finally
            {
                RuntimeKernelBootstrapper.WeaponEvents.OnWeaponReloadStarted -= Handler;
            }

            Assert.That(reloadedItemId, Is.EqualTo("weapon-rifle-01"));
        }

        [Test]
        public void RaiseWeaponReloadCancelled_InvokesEventWithPayload()
        {
            string reloadedItemId = null;
            var reason = WeaponReloadCancelReason.DryStateInvalidated;

            void Handler(string itemId, WeaponReloadCancelReason cancelReason)
            {
                reloadedItemId = itemId;
                reason = cancelReason;
            }

            RuntimeKernelBootstrapper.WeaponEvents.OnWeaponReloadCancelled += Handler;
            try
            {
                RuntimeKernelBootstrapper.WeaponEvents.RaiseWeaponReloadCancelled("weapon-rifle-01", WeaponReloadCancelReason.Sprint);
            }
            finally
            {
                RuntimeKernelBootstrapper.WeaponEvents.OnWeaponReloadCancelled -= Handler;
            }

            Assert.That(reloadedItemId, Is.EqualTo("weapon-rifle-01"));
            Assert.That(reason, Is.EqualTo(WeaponReloadCancelReason.Sprint));
        }

        [Test]
        public void RaiseWeaponAimChanged_InvokesEventWithPayload()
        {
            string aimedItemId = null;
            var isAiming = false;
            void Handler(string itemId, bool value)
            {
                aimedItemId = itemId;
                isAiming = value;
            }

            RuntimeKernelBootstrapper.WeaponEvents.OnWeaponAimChanged += Handler;
            try
            {
                RuntimeKernelBootstrapper.WeaponEvents.RaiseWeaponAimChanged("weapon-rifle-01", true);
            }
            finally
            {
                RuntimeKernelBootstrapper.WeaponEvents.OnWeaponAimChanged -= Handler;
            }

            Assert.That(aimedItemId, Is.EqualTo("weapon-rifle-01"));
            Assert.That(isAiming, Is.True);
        }

        [Test]
        public void RaiseProjectileHit_InvokesEventWithPayload()
        {
            string hitItemId = null;
            var hitPoint = Vector3.zero;
            var damage = 0f;
            void Handler(string itemId, Vector3 point, float hitDamage)
            {
                hitItemId = itemId;
                hitPoint = point;
                damage = hitDamage;
            }

            RuntimeKernelBootstrapper.WeaponEvents.OnProjectileHit += Handler;
            try
            {
                RuntimeKernelBootstrapper.WeaponEvents.RaiseProjectileHit("weapon-rifle-01", new Vector3(3f, 1f, -2f), 42.5f);
            }
            finally
            {
                RuntimeKernelBootstrapper.WeaponEvents.OnProjectileHit -= Handler;
            }

            Assert.That(hitItemId, Is.EqualTo("weapon-rifle-01"));
            Assert.That(hitPoint, Is.EqualTo(new Vector3(3f, 1f, -2f)));
            Assert.That(damage, Is.EqualTo(42.5f));
        }

        [Test]
        public void RaiseShopTradeOpenRequested_InvokesEventWithPayload()
        {
            string vendorId = null;
            void Handler(string value) => vendorId = value;

            RuntimeKernelBootstrapper.ShopEvents.OnShopTradeOpenRequested += Handler;
            try
            {
                RuntimeKernelBootstrapper.ShopEvents.RaiseShopTradeOpenRequested("vendor-reloading-01");
            }
            finally
            {
                RuntimeKernelBootstrapper.ShopEvents.OnShopTradeOpenRequested -= Handler;
            }

            Assert.That(vendorId, Is.EqualTo("vendor-reloading-01"));
        }

        [Test]
        public void RaiseShopBuyRequested_InvokesEventWithPayload()
        {
            string itemId = null;
            var quantity = -1;
            void Handler(string value, int amount)
            {
                itemId = value;
                quantity = amount;
            }

            RuntimeKernelBootstrapper.ShopEvents.OnShopBuyRequested += Handler;
            try
            {
                RuntimeKernelBootstrapper.ShopEvents.RaiseShopBuyRequested("powder-varget", 3);
            }
            finally
            {
                RuntimeKernelBootstrapper.ShopEvents.OnShopBuyRequested -= Handler;
            }

            Assert.That(itemId, Is.EqualTo("powder-varget"));
            Assert.That(quantity, Is.EqualTo(3));
        }

        [Test]
        public void RaiseShopTradeResult_InvokesEventWithPayload()
        {
            string itemId = null;
            var quantity = -1;
            var isBuy = false;
            var success = false;
            string reason = null;
            void Handler(string value, int amount, bool buy, bool wasSuccessful, string failureReason)
            {
                itemId = value;
                quantity = amount;
                isBuy = buy;
                success = wasSuccessful;
                reason = failureReason;
            }

            RuntimeKernelBootstrapper.ShopEvents.OnShopTradeResult += Handler;
            try
            {
                RuntimeKernelBootstrapper.ShopEvents.RaiseShopTradeResult("primer-cci", 100, true, false, "InsufficientFunds");
            }
            finally
            {
                RuntimeKernelBootstrapper.ShopEvents.OnShopTradeResult -= Handler;
            }

            Assert.That(itemId, Is.EqualTo("primer-cci"));
            Assert.That(quantity, Is.EqualTo(100));
            Assert.That(isBuy, Is.True);
            Assert.That(success, Is.False);
            Assert.That(reason, Is.EqualTo("InsufficientFunds"));
        }

        [Test]
        public void RaiseMoneyChanged_InvokesEventWithPayload()
        {
            var money = -1;
            void Handler(int value) => money = value;

            RuntimeKernelBootstrapper.InventoryEvents.OnMoneyChanged += Handler;
            try
            {
                RuntimeKernelBootstrapper.InventoryEvents.RaiseMoneyChanged(420);
            }
            finally
            {
                RuntimeKernelBootstrapper.InventoryEvents.OnMoneyChanged -= Handler;
            }

            Assert.That(money, Is.EqualTo(420));
        }
    }
}
