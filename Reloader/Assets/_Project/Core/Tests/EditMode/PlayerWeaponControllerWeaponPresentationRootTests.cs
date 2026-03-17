using System.Reflection;
using NUnit.Framework;
using Reloader.Weapons.Controllers;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public sealed class PlayerWeaponControllerWeaponPresentationRootTests
    {
        [Test]
        public void ResolveReferences_CreatesWeaponPresentationRootAsCameraPivotSibling_WhenLegacyHierarchyIsPresent()
        {
            var rig = CreateRigWithLegacyHandHierarchy();
            var legacyPresentationRoot = new GameObject("WeaponPresentationRoot").transform;
            legacyPresentationRoot.SetParent(rig.PlayerArms, false);

            var controller = rig.PlayerRoot.AddComponent<PlayerWeaponController>();
            SetField(controller, "_packAnimator", rig.PlayerArmsVisual.GetComponent<Animator>());
            SetField(controller, "_weaponViewParent", rig.IkHandGun);

            Invoke(controller, "ResolveReferences");

            var viewmodelLayer = LayerMask.NameToLayer("Viewmodel");
            var resolvedParent = (Transform)GetField(controller, "_weaponViewParent");
            Assert.That(resolvedParent, Is.Not.Null);
            Assert.That(resolvedParent.name, Is.EqualTo("WeaponPresentationRoot"));
            Assert.That(resolvedParent.parent, Is.EqualTo(rig.CameraPivot));
            Assert.That(resolvedParent, Is.Not.EqualTo(legacyPresentationRoot));
            Assert.That(legacyPresentationRoot.parent, Is.EqualTo(rig.PlayerArms));
            Assert.That(resolvedParent.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(resolvedParent.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(resolvedParent.localScale, Is.EqualTo(Vector3.one));
            Assert.That(rig.PlayerArms.gameObject.layer, Is.EqualTo(viewmodelLayer));
            Assert.That(rig.IkHandGun.gameObject.layer, Is.EqualTo(viewmodelLayer));
            Assert.That(resolvedParent.gameObject.layer, Is.EqualTo(viewmodelLayer));
        }

        [Test]
        public void UpdateEquipFromSelection_DoesNotClearEquippedWeaponWhenInventoryRuntimeIsUnavailable()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var controller = playerRoot.AddComponent<PlayerWeaponController>();

            SetField(controller, "_equippedItemId", "weapon-kar98k");

            Invoke(controller, "UpdateEquipFromSelection");

            Assert.That((string)GetField(controller, "_equippedItemId"), Is.EqualTo("weapon-kar98k"));
        }

        [Test]
        public void EnsureEquippedWeaponViewParent_ReparentsLegacyHandMountedViewToWeaponPresentationRoot()
        {
            var rig = CreateRigWithLegacyHandHierarchy();

            var equippedView = new GameObject("EquippedView_weapon-kar98k");
            equippedView.transform.SetParent(rig.IkHandGun, false);
            equippedView.transform.localPosition = new Vector3(1f, 2f, 3f);
            equippedView.transform.localRotation = Quaternion.Euler(10f, 20f, 30f);
            equippedView.transform.localScale = new Vector3(2f, 2f, 2f);

            var controller = rig.PlayerRoot.AddComponent<PlayerWeaponController>();
            SetField(controller, "_packAnimator", rig.PlayerArmsVisual.GetComponent<Animator>());
            SetField(controller, "_weaponViewParent", rig.IkHandGun);
            SetField(controller, "_equippedWeaponView", equippedView);

            Invoke(controller, "EnsureEquippedWeaponViewParent");

            Assert.That(equippedView.transform.parent, Is.Not.Null);
            Assert.That(equippedView.transform.parent.name, Is.EqualTo("WeaponPresentationRoot"));
            Assert.That(equippedView.transform.parent.parent, Is.EqualTo(rig.CameraPivot));
            Assert.That(equippedView.transform.parent.parent, Is.Not.EqualTo(rig.PlayerArms));
            Assert.That(equippedView.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(equippedView.transform.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(equippedView.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [TestCase(true, true, true, true, true, false)]
        [TestCase(true, false, true, true, true, true)]
        [TestCase(true, true, false, true, true, true)]
        [TestCase(true, true, true, false, true, true)]
        [TestCase(true, true, true, true, false, true)]
        [TestCase(false, false, false, false, false, false)]
        public void ShouldRepairScopedRuntimePresentation_OnlyRepairsScopedViewsWithBrokenBridges(
            bool hasEquippedScopedAttachment,
            bool hasScopedAdsStateBridge,
            bool hasScopedAttachmentManagerBridge,
            bool hasWeaponAimAlignerBridge,
            bool hasScopedAdsAlignment,
            bool expected)
        {
            var method = typeof(PlayerWeaponController).GetMethod(
                "ShouldRepairScopedRuntimePresentation",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected private scoped-runtime repair helper to exist.");

            var actual = (bool)method!.Invoke(null, new object[]
            {
                hasEquippedScopedAttachment,
                hasScopedAdsStateBridge,
                hasScopedAttachmentManagerBridge,
                hasWeaponAimAlignerBridge,
                hasScopedAdsAlignment
            });

            Assert.That(actual, Is.EqualTo(expected));
        }

        private static TestRig CreateRigWithLegacyHandHierarchy()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerRoot.transform, false);

            var playerArms = new GameObject("PlayerArms").transform;
            playerArms.SetParent(cameraPivot, false);

            var playerArmsVisual = new GameObject("PlayerArmsVisual");
            playerArmsVisual.transform.SetParent(playerArms, false);
            playerArmsVisual.AddComponent<Animator>();

            var armature = new GameObject("Armature").transform;
            armature.SetParent(playerArmsVisual.transform, false);
            var ikHandRoot = new GameObject("ik_hand_root").transform;
            ikHandRoot.SetParent(armature, false);
            var ikHandGun = new GameObject("ik_hand_gun").transform;
            ikHandGun.SetParent(ikHandRoot, false);

            return new TestRig(playerRoot, cameraPivot, playerArms, playerArmsVisual, ikHandGun);
        }

        private readonly struct TestRig
        {
            public TestRig(GameObject playerRoot, Transform cameraPivot, Transform playerArms, GameObject playerArmsVisual, Transform ikHandGun)
            {
                PlayerRoot = playerRoot;
                CameraPivot = cameraPivot;
                PlayerArms = playerArms;
                PlayerArmsVisual = playerArmsVisual;
                IkHandGun = ikHandGun;
            }

            public GameObject PlayerRoot { get; }
            public Transform CameraPivot { get; }
            public Transform PlayerArms { get; }
            public GameObject PlayerArmsVisual { get; }
            public Transform IkHandGun { get; }
        }

        private static void Invoke(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found.");
            method!.Invoke(instance, null);
        }

        private static object GetField(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            return field!.GetValue(instance);
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field!.SetValue(instance, value);
        }
    }
}
