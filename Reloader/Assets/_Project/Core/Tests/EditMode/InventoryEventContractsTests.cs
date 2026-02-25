using NUnit.Framework;
using Reloader.Core.Events;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public class InventoryEventContractsTests
    {
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

            GameEvents.OnItemStored += Handler;
            try
            {
                GameEvents.RaiseItemStored("item-1", InventoryArea.Backpack, 3);
            }
            finally
            {
                GameEvents.OnItemStored -= Handler;
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

            GameEvents.OnWeaponEquipped += Handler;
            try
            {
                GameEvents.RaiseWeaponEquipped("weapon-rifle-01");
            }
            finally
            {
                GameEvents.OnWeaponEquipped -= Handler;
            }

            Assert.That(equippedItemId, Is.EqualTo("weapon-rifle-01"));
        }

        [Test]
        public void RaiseWeaponEquipStarted_InvokesEventWithPayload()
        {
            string equippedItemId = null;
            void Handler(string itemId) => equippedItemId = itemId;

            GameEvents.OnWeaponEquipStarted += Handler;
            try
            {
                GameEvents.RaiseWeaponEquipStarted("weapon-rifle-01");
            }
            finally
            {
                GameEvents.OnWeaponEquipStarted -= Handler;
            }

            Assert.That(equippedItemId, Is.EqualTo("weapon-rifle-01"));
        }

        [Test]
        public void RaiseWeaponUnequipStarted_InvokesEventWithPayload()
        {
            string unequippedItemId = null;
            void Handler(string itemId) => unequippedItemId = itemId;

            GameEvents.OnWeaponUnequipStarted += Handler;
            try
            {
                GameEvents.RaiseWeaponUnequipStarted("weapon-rifle-01");
            }
            finally
            {
                GameEvents.OnWeaponUnequipStarted -= Handler;
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

            GameEvents.OnWeaponFired += Handler;
            try
            {
                GameEvents.RaiseWeaponFired("weapon-rifle-01", new Vector3(1f, 2f, 3f), Vector3.forward);
            }
            finally
            {
                GameEvents.OnWeaponFired -= Handler;
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

            GameEvents.OnWeaponReloaded += Handler;
            try
            {
                GameEvents.RaiseWeaponReloaded("weapon-rifle-01", 5, 25);
            }
            finally
            {
                GameEvents.OnWeaponReloaded -= Handler;
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

            GameEvents.OnWeaponReloadStarted += Handler;
            try
            {
                GameEvents.RaiseWeaponReloadStarted("weapon-rifle-01");
            }
            finally
            {
                GameEvents.OnWeaponReloadStarted -= Handler;
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

            GameEvents.OnWeaponReloadCancelled += Handler;
            try
            {
                GameEvents.RaiseWeaponReloadCancelled("weapon-rifle-01", WeaponReloadCancelReason.Sprint);
            }
            finally
            {
                GameEvents.OnWeaponReloadCancelled -= Handler;
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

            GameEvents.OnWeaponAimChanged += Handler;
            try
            {
                GameEvents.RaiseWeaponAimChanged("weapon-rifle-01", true);
            }
            finally
            {
                GameEvents.OnWeaponAimChanged -= Handler;
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

            GameEvents.OnProjectileHit += Handler;
            try
            {
                GameEvents.RaiseProjectileHit("weapon-rifle-01", new Vector3(3f, 1f, -2f), 42.5f);
            }
            finally
            {
                GameEvents.OnProjectileHit -= Handler;
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

            GameEvents.OnShopTradeOpenRequested += Handler;
            try
            {
                GameEvents.RaiseShopTradeOpenRequested("vendor-reloading-01");
            }
            finally
            {
                GameEvents.OnShopTradeOpenRequested -= Handler;
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

            GameEvents.OnShopBuyRequested += Handler;
            try
            {
                GameEvents.RaiseShopBuyRequested("powder-varget", 3);
            }
            finally
            {
                GameEvents.OnShopBuyRequested -= Handler;
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

            GameEvents.OnShopTradeResult += Handler;
            try
            {
                GameEvents.RaiseShopTradeResult("primer-cci", 100, true, false, "InsufficientFunds");
            }
            finally
            {
                GameEvents.OnShopTradeResult -= Handler;
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

            GameEvents.OnMoneyChanged += Handler;
            try
            {
                GameEvents.RaiseMoneyChanged(420);
            }
            finally
            {
                GameEvents.OnMoneyChanged -= Handler;
            }

            Assert.That(money, Is.EqualTo(420));
        }
    }
}
