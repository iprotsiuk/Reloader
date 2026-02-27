using System;
using System.Collections;
using NUnit.Framework;
using Reloader.Core.Persistence;
using Reloader.Inventory;
using Reloader.Weapons.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class PickupTargetWorldIdentityPlayModeTests
    {
        [UnityTest]
        public IEnumerator DefinitionPickupTarget_ObjectId_IsNonEmptyAndStableAcrossEnableDisable()
        {
            yield return AssertPickupIdentityIsStable(go => go.AddComponent<DefinitionPickupTarget>());
        }

        [UnityTest]
        public IEnumerator WeaponPickupTarget_ObjectId_IsNonEmptyAndStableAcrossEnableDisable()
        {
            yield return AssertPickupIdentityIsStable(go => go.AddComponent<WeaponPickupTarget>());
        }

        [UnityTest]
        public IEnumerator AmmoStackPickupTarget_ObjectId_IsNonEmptyAndStableAcrossEnableDisable()
        {
            yield return AssertPickupIdentityIsStable(go => go.AddComponent<AmmoStackPickupTarget>());
        }

        [UnityTest]
        public IEnumerator WeaponPickupTarget_DuplicateInstance_DoesNotShareObjectId()
        {
            var originalGo = new GameObject("WeaponPickup_Original_PlayMode");
            var originalTarget = originalGo.AddComponent<WeaponPickupTarget>();
            var originalObjectId = ReadObjectId(originalTarget);

            var duplicateGo = UnityEngine.Object.Instantiate(originalGo);
            duplicateGo.name = "WeaponPickup_Duplicate_PlayMode";
            yield return null;

            try
            {
                var duplicateTarget = duplicateGo.GetComponent<WeaponPickupTarget>();
                var duplicateObjectId = ReadObjectId(duplicateTarget);

                Assert.That(originalObjectId, Is.Not.Null.And.Not.Empty);
                Assert.That(duplicateObjectId, Is.Not.Null.And.Not.Empty);
                Assert.That(duplicateObjectId, Is.Not.EqualTo(originalObjectId));
            }
            finally
            {
                UnityEngine.Object.Destroy(duplicateGo);
                UnityEngine.Object.Destroy(originalGo);
            }
        }

        private static IEnumerator AssertPickupIdentityIsStable<T>(Func<GameObject, T> createTarget) where T : MonoBehaviour
        {
            var go = new GameObject(typeof(T).Name + "_PlayModeGO");

            try
            {
                var target = createTarget(go);
                var firstObjectId = ReadObjectId(target);

                Assert.That(firstObjectId, Is.Not.Null.And.Not.Empty);
                Assert.That(go.GetComponent<WorldObjectIdentity>(), Is.Not.Null);

                go.SetActive(false);
                yield return null;

                go.SetActive(true);
                yield return null;

                var secondObjectId = ReadObjectId(target);
                Assert.That(secondObjectId, Is.EqualTo(firstObjectId));
                Assert.That(go.GetComponent<WorldObjectIdentity>(), Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.Destroy(go);
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
