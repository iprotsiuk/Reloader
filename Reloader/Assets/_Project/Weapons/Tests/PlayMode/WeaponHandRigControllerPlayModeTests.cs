using System;
using System.Collections;
using System.Reflection;
using Reloader.Inventory;
using Reloader.Player;
using NUnit.Framework;
using Reloader.Player.Viewmodel;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.Weapons.Tests.PlayMode
{
    public sealed class WeaponHandRigControllerPlayModeTests
    {
        [Test]
        public void SyncHandTargets_UsesEquippedWeaponViewAnchors()
        {
            var root = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);

            var playerArms = new GameObject("PlayerArms").transform;
            playerArms.SetParent(cameraPivot, false);

            var armsVisual = new GameObject("PlayerArmsVisual").transform;
            armsVisual.SetParent(playerArms, false);
            var armsAnimator = armsVisual.gameObject.AddComponent<Animator>();

            var defaults = ConfigurePlayerCameraDefaults(root, cameraPivot, playerArms, armsAnimator);
            var handTargetRoot = CreateWeaponHandRigTargets(cameraPivot);

            var controller = root.AddComponent<WeaponHandRigController>();
            SetPrivateField(controller, "_cameraDefaults", defaults);
            SetPrivateField(controller, "_handTargetRoot", handTargetRoot);
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

        [UnityTest]
        public IEnumerator SyncHandTargets_IgnoresLegacyIkHandGunHierarchyWhenControllerOwnsEquippedView()
        {
            GameObject root = null;
            GameObject registryGo = null;
            GameObject viewPrefab = null;
            WeaponDefinition definition = null;

            try
            {
                root = new GameObject("PlayerRoot");
                var cameraPivot = new GameObject("CameraPivot").transform;
                cameraPivot.SetParent(root.transform, false);

                var playerArms = new GameObject("PlayerArms").transform;
                playerArms.SetParent(cameraPivot, false);

                var armsVisual = new GameObject("PlayerArmsVisual").transform;
                armsVisual.SetParent(playerArms, false);
                var armsAnimator = armsVisual.gameObject.AddComponent<Animator>();
                var defaults = ConfigurePlayerCameraDefaults(root, cameraPivot, playerArms, armsAnimator);
                var handTargetRoot = CreateWeaponHandRigTargets(cameraPivot);
#if UNITY_EDITOR
                armsAnimator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/_Project/Player/Resources/Viewmodels/Characters/ViewmodelArms.controller");
                Assert.That(armsAnimator.runtimeAnimatorController, Is.Not.Null,
                    "Hand-rig seam test requires a RuntimeAnimatorController to avoid Animator.Play warnings.");
#endif

                var upperArm = new GameObject("upperarm_l").transform;
                upperArm.SetParent(armsVisual, false);

                var lowerArm = new GameObject("lowerarm_l").transform;
                lowerArm.SetParent(upperArm, false);

                var hand = new GameObject("hand_l").transform;
                hand.SetParent(lowerArm, false);

                var armature = new GameObject("Armature").transform;
                armature.SetParent(playerArms, false);
                var legacyHandRoot = new GameObject("ik_hand_root").transform;
                legacyHandRoot.SetParent(armature, false);
                var legacyHandGun = new GameObject("ik_hand_gun").transform;
                legacyHandGun.SetParent(legacyHandRoot, false);

                var legacyAnchors = legacyHandGun.gameObject.AddComponent<WeaponViewHandAnchors>();
                var legacyLeftGrip = new GameObject("LegacyLeftGrip").transform;
                legacyLeftGrip.SetParent(legacyHandGun, false);
                legacyLeftGrip.localPosition = new Vector3(4f, 5f, 6f);
                legacyLeftGrip.localRotation = Quaternion.Euler(15f, 25f, 35f);

                var legacyRightGrip = new GameObject("LegacyRightGrip").transform;
                legacyRightGrip.SetParent(legacyHandGun, false);
                legacyRightGrip.localPosition = new Vector3(-4f, -5f, -6f);
                legacyRightGrip.localRotation = Quaternion.Euler(-15f, -25f, -35f);
                legacyAnchors.SetHandTargets(legacyLeftGrip, legacyRightGrip);

                var handRigController = root.AddComponent<WeaponHandRigController>();
                SetPrivateField(handRigController, "_cameraDefaults", defaults);
                SetPrivateField(handRigController, "_handTargetRoot", handTargetRoot);
                SetPrivateField(handRigController, "_driveRightHand", true);
                var leftHandTarget = new GameObject("LeftHandTarget").transform;
                var rightHandTarget = new GameObject("RightHandTarget").transform;
                handRigController.ConfigureTargets(leftHandTarget, rightHandTarget);

                root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(root.GetComponent<TestInputSource>(), resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);

                viewPrefab = new GameObject("Kar98kView");
                var adsPivot = new GameObject("AdsPivot").transform;
                adsPivot.SetParent(viewPrefab.transform, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(adsPivot, false);
                var muzzleSlot = new GameObject("MuzzleAttachmentSlot").transform;
                muzzleSlot.SetParent(adsPivot, false);
                ConfigureTestWeaponViewMounts(viewPrefab, adsPivot, ironSightAnchor, muzzleSlot);

                var prefabAnchors = viewPrefab.AddComponent<WeaponViewHandAnchors>();
                var prefabLeftGrip = new GameObject("RealLeftGrip").transform;
                prefabLeftGrip.SetParent(adsPivot, false);
                prefabLeftGrip.localPosition = new Vector3(0.11f, 0.22f, 0.33f);
                prefabLeftGrip.localRotation = Quaternion.Euler(7f, 17f, 27f);

                var prefabRightGrip = new GameObject("RealRightGrip").transform;
                prefabRightGrip.SetParent(adsPivot, false);
                prefabRightGrip.localPosition = new Vector3(-0.14f, -0.09f, 0.28f);
                prefabRightGrip.localRotation = Quaternion.Euler(-9f, 19f, -11f);
                prefabAnchors.SetHandTargets(prefabLeftGrip, prefabRightGrip);

                SetPrivateField(definition, "_iconSourcePrefab", viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var weaponController = root.AddComponent<PlayerWeaponController>();
                var explicitWeaponPresentationRoot = EnsureWeaponPresentationRoot(cameraPivot);
                SetPrivateField(defaults, "_weaponPresentationRoot", explicitWeaponPresentationRoot);
                SetPrivateField(weaponController, "_weaponRegistry", registry);
                SetPrivateField(weaponController, "_weaponViewParent", explicitWeaponPresentationRoot);
                SetWeaponViewBinding(weaponController, "weapon-kar98k", viewPrefab);

                var frames = 0;
                while (weaponController.EquippedWeaponViewTransform == null && frames < 10)
                {
                    frames++;
                    yield return null;
                }

                Assert.That(weaponController.EquippedWeaponViewTransform, Is.Not.Null);

                handRigController.SyncHandTargets();

                var liveAnchors = weaponController.EquippedWeaponViewTransform!.GetComponent<WeaponViewHandAnchors>();
                Assert.That(liveAnchors, Is.Not.Null);
                Assert.That(weaponController.EquippedWeaponViewTransform, Is.Not.SameAs(legacyHandGun));
                Assert.That(weaponController.EquippedWeaponViewTransform.IsChildOf(legacyHandGun), Is.False);
                Assert.That(Vector3.Distance(leftHandTarget.position, liveAnchors!.LeftHandGrip.position), Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(leftHandTarget.rotation, liveAnchors.LeftHandGrip.rotation), Is.LessThan(0.01f));
                Assert.That(Vector3.Distance(rightHandTarget.position, liveAnchors.RightHandGrip.position), Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(rightHandTarget.rotation, liveAnchors.RightHandGrip.rotation), Is.LessThan(0.01f));
                Assert.That(Vector3.Distance(leftHandTarget.position, legacyLeftGrip.position), Is.GreaterThan(0.001f));
                Assert.That(Quaternion.Angle(leftHandTarget.rotation, legacyLeftGrip.rotation), Is.GreaterThan(0.1f));
                Assert.That(Vector3.Distance(rightHandTarget.position, legacyRightGrip.position), Is.GreaterThan(0.001f));
                Assert.That(Quaternion.Angle(rightHandTarget.rotation, legacyRightGrip.rotation), Is.GreaterThan(0.1f));
                Assert.That(handRigController.HasResolvedWeaponAnchors, Is.True);
            }
            finally
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }
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
        public void SyncHandTargets_DoesNotCreateHandTargetRoot_WhenExplicitRootIsMissing()
        {
            var root = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);

            var playerArms = new GameObject("PlayerArms").transform;
            playerArms.SetParent(cameraPivot, false);

            var armsVisual = new GameObject("PlayerArmsVisual").transform;
            armsVisual.SetParent(playerArms, false);
            var armsAnimator = armsVisual.gameObject.AddComponent<Animator>();
            var defaults = ConfigurePlayerCameraDefaults(root, cameraPivot, playerArms, armsAnimator);

            var controller = root.AddComponent<WeaponHandRigController>();
            SetPrivateField(controller, "_cameraDefaults", defaults);

            try
            {
                controller.SyncHandTargets();

                Assert.That(cameraPivot.Find("WeaponHandRigTargets"), Is.Null,
                    "WeaponHandRigController should not recreate WeaponHandRigTargets under CameraPivot when the explicit authored root is missing.");
                Assert.That(controller.HasResolvedWeaponAnchors, Is.False);
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
            var armsAnimator = armsVisual.gameObject.AddComponent<Animator>();
            var defaults = ConfigurePlayerCameraDefaults(root, cameraPivot, playerArms, armsAnimator);
            var handTargetRoot = CreateWeaponHandRigTargets(cameraPivot);

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
            SetPrivateField(controller, "_cameraDefaults", defaults);
            SetPrivateField(controller, "_handTargetRoot", handTargetRoot);
            return root;
        }

        private static PlayerCameraDefaults ConfigurePlayerCameraDefaults(
            GameObject root,
            Transform cameraPivot,
            Transform playerArmsRoot,
            Animator playerArmsAnimator)
        {
            var defaults = root.AddComponent<PlayerCameraDefaults>();
            SetPrivateField(defaults, "_cameraPivot", cameraPivot);
            SetPrivateField(defaults, "_cameraFollowTarget", cameraPivot);
            SetPrivateField(defaults, "_playerArmsRoot", playerArmsRoot);
            SetPrivateField(defaults, "_playerArmsAnimator", playerArmsAnimator);
            return defaults;
        }

        private static Transform CreateWeaponHandRigTargets(Transform cameraPivot)
        {
            var existing = cameraPivot.Find("WeaponHandRigTargets");
            if (existing != null)
            {
                return existing;
            }

            var targetRoot = new GameObject("WeaponHandRigTargets").transform;
            targetRoot.SetParent(cameraPivot, false);
            return targetRoot;
        }

        private static Transform EnsureWeaponPresentationRoot(Transform cameraPivot)
        {
            var existing = cameraPivot.Find("WeaponPresentationRoot");
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject("WeaponPresentationRoot").transform;
            root.SetParent(cameraPivot, false);
            return root;
        }

        private static void SetWeaponViewBinding(PlayerWeaponController controller, string itemId, GameObject viewPrefab)
        {
            var bindingType = typeof(WeaponViewPrefabBinding);
            var binding = Activator.CreateInstance(bindingType);
            SetPrivateField(binding, "_itemId", itemId);
            SetPrivateField(binding, "_viewPrefab", viewPrefab);
            SetPrivateField(controller, "_weaponViewPrefabs", new[] { (WeaponViewPrefabBinding)binding });
        }

        private static void ConfigureTestWeaponViewMounts(
            GameObject viewPrefab,
            Transform adsPivot,
            Transform ironSightAnchor,
            Transform muzzleSlot)
        {
            var mounts = viewPrefab.AddComponent<WeaponViewAttachmentMounts>();
            SetPrivateField(mounts, "_adsPivot", adsPivot);
            SetPrivateField(mounts, "_muzzleTransform", null);
            SetPrivateField(mounts, "_ironSightAnchor", ironSightAnchor);
            SetPrivateField(mounts, "_magazineSocket", null);
            SetPrivateField(mounts, "_magazineDropSocket", null);

            var slotEntryType = typeof(WeaponViewAttachmentMounts).GetNestedType("AttachmentSlotMount", BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(slotEntryType, Is.Not.Null);
            var entries = Array.CreateInstance(slotEntryType!, 1);
            var entry = Activator.CreateInstance(slotEntryType);
            SetPrivateField(entry, "_slotType", WeaponAttachmentSlotType.Muzzle);
            SetPrivateField(entry, "_slotTransform", muzzleSlot);
            entries.SetValue(entry, 0);
            SetPrivateField(mounts, "_attachmentSlots", entries);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
            field!.SetValue(target, value);
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public bool ConsumePickupPressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeDevConsoleTogglePressed() => false;
            public bool ConsumeAutocompletePressed() => false;
            public int ConsumeSuggestionDelta() => 0;
        }

        private sealed class TestPickupResolver : MonoBehaviour, IInventoryPickupTargetResolver
        {
            public bool TryResolvePickupTarget(out IInventoryPickupTarget target)
            {
                target = null;
                return false;
            }
        }
    }
}
