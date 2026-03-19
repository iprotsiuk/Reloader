using System.Reflection;
using NUnit.Framework;
using Reloader.Player.Viewmodel;
using Reloader.Weapons.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Reloader.Weapons.Tests.PlayMode
{
    public sealed class WeaponHandRigControllerPlayModeTests
    {
        [Test]
        public void SyncHandTargets_UsesEquippedWeaponViewAnchors()
        {
            var root = new GameObject("PlayerRoot");
            var controller = root.AddComponent<WeaponHandRigController>();
            var leftHandTarget = new GameObject("LeftHandTarget").transform;
            var rightHandTarget = new GameObject("RightHandTarget").transform;
            var weaponView = new GameObject("EquippedWeaponView");

            try
            {
                SetPrivateField(controller, "_driveRightHand", true);
                controller.ConfigureTargets(leftHandTarget, rightHandTarget);

                var anchors = weaponView.AddComponent<WeaponViewHandAnchors>();
                var leftGrip = new GameObject("LeftGrip").transform;
                leftGrip.SetParent(weaponView.transform, false);
                leftGrip.localPosition = new Vector3(0.12f, 0.28f, 0.34f);
                leftGrip.localRotation = Quaternion.Euler(12f, 34f, -8f);

                var rightGrip = new GameObject("RightGrip").transform;
                rightGrip.SetParent(weaponView.transform, false);
                rightGrip.localPosition = new Vector3(-0.08f, 0.19f, 0.26f);
                rightGrip.localRotation = Quaternion.Euler(-6f, 15f, 5f);

                anchors.SetHandTargets(leftGrip, rightGrip);
                controller.SetEquippedWeaponViewForTests(weaponView.transform);
                controller.SyncHandTargets();

                Assert.That(Vector3.Distance(leftHandTarget.position, leftGrip.position), Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(leftHandTarget.rotation, leftGrip.rotation), Is.LessThan(0.01f));
                Assert.That(Vector3.Distance(rightHandTarget.position, rightGrip.position), Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(rightHandTarget.rotation, rightGrip.rotation), Is.LessThan(0.01f));
                Assert.That(controller.HasResolvedWeaponAnchors, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(weaponView);
                Object.DestroyImmediate(leftHandTarget.gameObject);
                Object.DestroyImmediate(rightHandTarget.gameObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SyncHandTargets_CreatesLeftHandIkRigWhenAnimatorBonesExist()
        {
            var root = CreateWeaponHandRigTestRoot(out var controller, out var upperArm, out var lowerArm, out var hand);

            try
            {
                controller.SyncHandTargets();

                Assert.That(controller.LeftHandTarget, Is.Not.Null);
                Assert.That(controller.LeftHandHint, Is.Not.Null);
                Assert.That(controller.LeftHandConstraint, Is.Not.Null);
                Assert.That(controller.RigBuilder, Is.Not.Null);
                Assert.That(controller.LeftHandConstraint.data.root, Is.SameAs(upperArm));
                Assert.That(controller.LeftHandConstraint.data.mid, Is.SameAs(lowerArm));
                Assert.That(controller.LeftHandConstraint.data.tip, Is.SameAs(hand));
                Assert.That(controller.LeftHandConstraint.data.target, Is.SameAs(controller.LeftHandTarget));
                Assert.That(controller.LeftHandConstraint.data.hint, Is.SameAs(controller.LeftHandHint));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SyncHandTargets_ReleasesLeftHandConstraintDuringReload()
        {
            var root = CreateWeaponHandRigTestRoot(out var controller, out _, out _, out _);
            var weaponView = new GameObject("EquippedWeaponView");

            try
            {
                var anchors = weaponView.AddComponent<WeaponViewHandAnchors>();
                var leftGrip = new GameObject("LeftGrip").transform;
                leftGrip.SetParent(weaponView.transform, false);
                leftGrip.localPosition = new Vector3(0.12f, 0.28f, 0.34f);
                leftGrip.localRotation = Quaternion.Euler(12f, 34f, -8f);

                var rightGrip = new GameObject("RightGrip").transform;
                rightGrip.SetParent(weaponView.transform, false);
                rightGrip.localPosition = new Vector3(-0.08f, 0.19f, 0.26f);
                rightGrip.localRotation = Quaternion.Euler(-6f, 15f, 5f);

                anchors.SetHandTargets(leftGrip, rightGrip);
                controller.SetEquippedWeaponViewForTests(weaponView.transform);

                controller.SetReloadingOverrideForTests(false);
                controller.SyncHandTargets();
                Assert.That(controller.LeftHandConstraint, Is.Not.Null);
                Assert.That(controller.LeftHandConstraint.weight, Is.EqualTo(1f).Within(0.0001f));

                controller.SetReloadingOverrideForTests(true);
                controller.SyncHandTargets();
                Assert.That(controller.LeftHandConstraint.weight, Is.EqualTo(0f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(weaponView);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SyncHandTargets_UsesLeftHandAnchorWhenRightGripIsMissing()
        {
            var root = CreateWeaponHandRigTestRoot(out var controller, out var upperArm, out var lowerArm, out var hand);
            var weaponView = new GameObject("EquippedWeaponView");

            try
            {
                var anchors = weaponView.AddComponent<WeaponViewHandAnchors>();
                var leftGrip = new GameObject("LeftGrip").transform;
                leftGrip.SetParent(weaponView.transform, false);
                leftGrip.localPosition = new Vector3(0.12f, 0.28f, 0.34f);
                leftGrip.localRotation = Quaternion.Euler(12f, 34f, -8f);

                anchors.SetHandTargets(leftGrip, null);
                controller.SetEquippedWeaponViewForTests(weaponView.transform);

                controller.SyncHandTargets();

                Assert.That(controller.HasResolvedWeaponAnchors, Is.True);
                Assert.That(controller.LeftHandConstraint, Is.Not.Null);
                Assert.That(controller.LeftHandConstraint.data.root, Is.SameAs(upperArm));
                Assert.That(controller.LeftHandConstraint.data.mid, Is.SameAs(lowerArm));
                Assert.That(controller.LeftHandConstraint.data.tip, Is.SameAs(hand));
                Assert.That(controller.LeftHandConstraint.weight, Is.GreaterThan(0.01f));
                Assert.That(Vector3.Distance(controller.LeftHandTarget.position, leftGrip.position), Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(controller.LeftHandTarget.rotation, leftGrip.rotation), Is.LessThan(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(weaponView);
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateWeaponHandRigTestRoot(
            out WeaponHandRigController controller,
            out Transform upperArm,
            out Transform lowerArm,
            out Transform hand)
        {
            var root = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);

            var playerArms = new GameObject("PlayerArms").transform;
            playerArms.SetParent(cameraPivot, false);

            var armsVisual = new GameObject("PlayerArmsVisual").transform;
            armsVisual.SetParent(playerArms, false);
            armsVisual.gameObject.AddComponent<Animator>();

            upperArm = new GameObject("upperarm_l").transform;
            upperArm.SetParent(armsVisual, false);
            upperArm.localPosition = new Vector3(-0.1f, 0.05f, 0.2f);

            lowerArm = new GameObject("lowerarm_l").transform;
            lowerArm.SetParent(upperArm, false);
            lowerArm.localPosition = new Vector3(-0.2f, 0f, 0f);

            hand = new GameObject("hand_l").transform;
            hand.SetParent(lowerArm, false);
            hand.localPosition = new Vector3(-0.2f, 0f, 0f);

            controller = root.AddComponent<WeaponHandRigController>();
            return root;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
            field!.SetValue(target, value);
        }
    }
}
