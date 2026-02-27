using NUnit.Framework;
using Reloader.Core.Persistence;
using Reloader.Inventory;
using Reloader.Weapons.World;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public class PickupTargetWorldIdentityEditModeTests
    {
        [Test]
        public void DefinitionPickupTarget_ObjectId_IsNonEmptyAndStableAcrossEnableDisable()
        {
            AssertPickupIdentityIsStable(go => go.AddComponent<DefinitionPickupTarget>());
        }

        [Test]
        public void WeaponPickupTarget_ObjectId_IsNonEmptyAndStableAcrossEnableDisable()
        {
            AssertPickupIdentityIsStable(go => go.AddComponent<WeaponPickupTarget>());
        }

        [Test]
        public void AmmoStackPickupTarget_ObjectId_IsNonEmptyAndStableAcrossEnableDisable()
        {
            AssertPickupIdentityIsStable(go => go.AddComponent<AmmoStackPickupTarget>());
        }

        private static void AssertPickupIdentityIsStable<T>(System.Func<GameObject, T> createTarget) where T : MonoBehaviour
        {
            var go = new GameObject(typeof(T).Name + "_GO");

            try
            {
                var target = createTarget(go);
                var firstObjectId = ReadObjectId(target);

                Assert.That(firstObjectId, Is.Not.Null.And.Not.Empty);
                Assert.That(go.GetComponent<WorldObjectIdentity>(), Is.Not.Null);

                go.SetActive(false);
                go.SetActive(true);

                var secondObjectId = ReadObjectId(target);
                Assert.That(secondObjectId, Is.EqualTo(firstObjectId));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static string ReadObjectId(object pickupTarget)
        {
            var property = pickupTarget.GetType().GetProperty("ObjectId");
            Assert.That(property, Is.Not.Null, pickupTarget.GetType().Name + " must expose ObjectId.");
            return property.GetValue(pickupTarget) as string;
        }
    }
}
