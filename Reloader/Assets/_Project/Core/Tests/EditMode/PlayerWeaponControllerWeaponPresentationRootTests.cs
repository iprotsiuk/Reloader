using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.Player;
using Reloader.Player.Viewmodel;
using Reloader.Weapons.Animations;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public sealed class PlayerWeaponControllerWeaponPresentationRootTests
    {
        [Test]
        public void ResolveReferences_RejectsLegacyIkHandGun_WhenNoExplicitPresentationRootExists()
        {
            var rig = CreateRigWithLegacyHandHierarchy();

            var controller = rig.PlayerRoot.AddComponent<PlayerWeaponController>();
            SetField(controller, "_packAnimator", rig.PlayerArmsVisual.GetComponent<Animator>());
            SetField(controller, "_weaponViewParent", rig.IkHandGun);

            Invoke(controller, "ResolveReferences");

            var resolvedParent = (Transform)GetField(controller, "_weaponViewParent");
            Assert.That(resolvedParent, Is.Null,
                "PlayerWeaponController should reject legacy ik_hand_gun ownership when no explicit presentation root is configured.");
            Assert.That(rig.CameraPivot.Find("WeaponPresentationRoot"), Is.Null,
                "ResolveReferences should not synthesize CameraPivot/WeaponPresentationRoot anymore.");
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
        public void EnsureEquippedWeaponViewParent_ReparentsLegacyHandMountedViewToExplicitWeaponPresentationRootWithoutRewritingAuthoredLocalPose()
        {
            var rig = CreateRigWithLegacyHandHierarchy();
            var explicitPresentationRoot = new GameObject("ExplicitWeaponPresentationRoot").transform;
            explicitPresentationRoot.SetParent(rig.CameraPivot, false);
            var defaults = rig.PlayerRoot.AddComponent<Reloader.Player.PlayerCameraDefaults>();
            SetField(defaults, "_cameraPivot", rig.CameraPivot);
            SetField(defaults, "_cameraFollowTarget", rig.CameraPivot);
            SetField(defaults, "_playerArmsRoot", rig.PlayerArms);
            SetField(defaults, "_playerArmsAnimator", rig.PlayerArmsVisual.GetComponent<Animator>());
            SetField(defaults, "_weaponPresentationRoot", explicitPresentationRoot);

            var equippedView = new GameObject("EquippedView_weapon-kar98k");
            equippedView.transform.SetParent(rig.IkHandGun, false);
            equippedView.transform.localPosition = new Vector3(1f, 2f, 3f);
            equippedView.transform.localRotation = Quaternion.Euler(10f, 20f, 30f);
            equippedView.transform.localScale = new Vector3(2f, 2f, 2f);

            var controller = rig.PlayerRoot.AddComponent<PlayerWeaponController>();
            SetField(controller, "_cameraDefaults", defaults);
            SetField(controller, "_packAnimator", rig.PlayerArmsVisual.GetComponent<Animator>());
            SetField(controller, "_weaponViewParent", explicitPresentationRoot);
            SetField(controller, "_equippedWeaponView", equippedView);

            Invoke(controller, "EnsureEquippedWeaponViewParent");

            Assert.That(equippedView.transform.parent, Is.Not.Null);
            Assert.That(equippedView.transform.parent, Is.SameAs(explicitPresentationRoot));
            Assert.That(equippedView.transform.parent.parent, Is.EqualTo(rig.CameraPivot));
            Assert.That(equippedView.transform.parent.parent, Is.Not.EqualTo(rig.PlayerArms));
            Assert.That(Vector3.Distance(equippedView.transform.localPosition, new Vector3(1f, 2f, 3f)), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(equippedView.transform.localRotation, Quaternion.Euler(10f, 20f, 30f)), Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(equippedView.transform.localScale, new Vector3(2f, 2f, 2f)), Is.LessThan(0.0001f));
        }

        [Test]
        public void ResolveReferences_UsesPlayerCameraDefaultsExplicitPresentationContract_WithoutCreatingLegacyFallback()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var presentationPivot = new GameObject("PresentationPivot").transform;
            presentationPivot.SetParent(playerRoot.transform, false);

            var explicitViewmodelRoot = new GameObject("ArmsBranch").transform;
            explicitViewmodelRoot.SetParent(presentationPivot, false);

            var playerArmsVisual = new GameObject("ViewArmsVisual");
            playerArmsVisual.transform.SetParent(explicitViewmodelRoot, false);
            var animator = playerArmsVisual.AddComponent<Animator>();

            var explicitPresentationRoot = new GameObject("WeaponMount").transform;
            explicitPresentationRoot.SetParent(presentationPivot, false);

            var defaults = playerRoot.AddComponent<Reloader.Player.PlayerCameraDefaults>();
            SetField(defaults, "_cameraPivot", presentationPivot);
            SetField(defaults, "_cameraFollowTarget", presentationPivot);
            SetField(defaults, "_playerArmsRoot", explicitViewmodelRoot);
            SetField(defaults, "_playerArmsAnimator", animator);
            SetField(defaults, "_weaponPresentationRoot", explicitPresentationRoot);

            var legacyHandGun = new GameObject("ik_hand_gun").transform;
            legacyHandGun.SetParent(explicitViewmodelRoot, false);

            var controller = playerRoot.AddComponent<PlayerWeaponController>();
            SetField(controller, "_cameraDefaults", defaults);
            SetField(controller, "_weaponViewParent", legacyHandGun);

            Invoke(controller, "ResolveReferences");

            Assert.That(GetField(controller, "_cameraPivot"), Is.SameAs(presentationPivot));
            Assert.That(GetField(controller, "_viewmodelRoot"), Is.SameAs(explicitViewmodelRoot));
            Assert.That(GetField(controller, "_packAnimator"), Is.SameAs(animator));
            Assert.That(GetField(controller, "_weaponViewParent"), Is.SameAs(explicitPresentationRoot));
            Assert.That(presentationPivot.Find("WeaponPresentationRoot"), Is.Null,
                "Explicit presentation contracts should not synthesize the legacy CameraPivot/WeaponPresentationRoot fallback.");
        }

        [Test]
        public void ExplicitOwnershipContract_UsesPlayerCameraDefaultsAcrossControllerBinderDriverAndHandRig()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var decoyAnimatorHost = new GameObject("DecoyAnimatorHost").transform;
            decoyAnimatorHost.SetParent(playerRoot.transform, false);
            decoyAnimatorHost.gameObject.AddComponent<Animator>();

            var presentationPivot = new GameObject("PresentationPivot").transform;
            presentationPivot.SetParent(playerRoot.transform, false);

            var weaponMount = new GameObject("WeaponMount").transform;
            weaponMount.SetParent(presentationPivot, false);

            var playerArmsRoot = new GameObject("ArmsBranch").transform;
            playerArmsRoot.SetParent(presentationPivot, false);
            playerArmsRoot.localPosition = new Vector3(0.6f, 0.4f, -0.2f);
            playerArmsRoot.localRotation = Quaternion.Euler(7f, 9f, 11f);
            playerArmsRoot.localScale = new Vector3(1.7f, 1.6f, 1.5f);

            var playerArmsVisual = new GameObject("ViewArmsVisual").transform;
            playerArmsVisual.SetParent(playerArmsRoot, false);
            var armsAnimator = playerArmsVisual.gameObject.AddComponent<Animator>();

            CreateArmChain(playerArmsVisual, "upperarm_l", "lowerarm_l", "hand_l", new Vector3(-0.2f, 0f, 0f));
            CreateArmChain(playerArmsVisual, "upperarm_r", "lowerarm_r", "hand_r", new Vector3(0.2f, 0f, 0f));

            var legacyArmature = new GameObject("Armature").transform;
            legacyArmature.SetParent(playerArmsRoot, false);
            var legacyHandRoot = new GameObject("ik_hand_root").transform;
            legacyHandRoot.SetParent(legacyArmature, false);
            var legacyHandGun = new GameObject("ik_hand_gun").transform;
            legacyHandGun.SetParent(legacyHandRoot, false);
            var legacyAnchors = legacyHandGun.gameObject.AddComponent<WeaponViewHandAnchors>();
            var legacyLeftGrip = new GameObject("LegacyLeftGrip").transform;
            legacyLeftGrip.SetParent(legacyHandGun, false);
            legacyLeftGrip.localPosition = new Vector3(4f, 5f, 6f);
            var legacyRightGrip = new GameObject("LegacyRightGrip").transform;
            legacyRightGrip.SetParent(legacyHandGun, false);
            legacyRightGrip.localPosition = new Vector3(-4f, -5f, -6f);
            legacyAnchors.SetHandTargets(legacyLeftGrip, legacyRightGrip);

            var defaults = playerRoot.AddComponent<PlayerCameraDefaults>();
            SetField(defaults, "_cameraPivot", presentationPivot);
            SetField(defaults, "_cameraFollowTarget", presentationPivot);
            SetField(defaults, "_viewmodelCameraParent", presentationPivot);
            SetField(defaults, "_playerArmsRoot", playerArmsRoot);
            SetField(defaults, "_playerArmsAnimator", armsAnimator);
            SetField(defaults, "_weaponPresentationRoot", weaponMount);
            var handTargetRoot = new GameObject("WeaponHandRigTargets").transform;
            handTargetRoot.SetParent(presentationPivot, false);
            var leftHandTarget = new GameObject("LeftHandTarget").transform;
            leftHandTarget.SetParent(handTargetRoot, false);
            var leftElbowHint = new GameObject("LeftElbowHint").transform;
            leftElbowHint.SetParent(handTargetRoot, false);
            var rightHandTarget = new GameObject("RightHandTarget").transform;
            rightHandTarget.SetParent(handTargetRoot, false);
            var rightElbowHint = new GameObject("RightElbowHint").transform;
            rightElbowHint.SetParent(handTargetRoot, false);

            var binder = playerRoot.AddComponent<PlayerWeaponAnimationBinder>();
            var driver = playerRoot.AddComponent<FpsViewmodelAnimatorDriver>();
            SetField(driver, "_cameraDefaults", defaults);
            SetField(driver, "_lockViewmodelRootPose", true);

            var handRigController = playerRoot.AddComponent<WeaponHandRigController>();
            SetField(handRigController, "_cameraDefaults", defaults);
            SetField(handRigController, "_handTargetRoot", handTargetRoot);
            SetField(handRigController, "_driveRightHand", true);

            var rigBuilderType = FindType("UnityEngine.Animations.Rigging.RigBuilder");
            var rigType = FindType("UnityEngine.Animations.Rigging.Rig");
            var twoBoneIKConstraintType = FindType("UnityEngine.Animations.Rigging.TwoBoneIKConstraint");
            Assert.That(rigBuilderType, Is.Not.Null, "Expected RigBuilder type.");
            Assert.That(rigType, Is.Not.Null, "Expected Rig type.");
            Assert.That(twoBoneIKConstraintType, Is.Not.Null, "Expected TwoBoneIKConstraint type.");

            var rigBuilder = armsAnimator.gameObject.AddComponent(rigBuilderType!);
            var weaponHandRigGo = new GameObject("WeaponHandRig");
            weaponHandRigGo.transform.SetParent(armsAnimator.transform, false);
            var weaponHandRig = weaponHandRigGo.AddComponent(rigType!);
            var leftConstraintGo = new GameObject("LeftHandConstraint");
            leftConstraintGo.transform.SetParent(weaponHandRigGo.transform, false);
            var leftConstraint = leftConstraintGo.AddComponent(twoBoneIKConstraintType!);
            var rightConstraintGo = new GameObject("RightHandConstraint");
            rightConstraintGo.transform.SetParent(weaponHandRigGo.transform, false);
            var rightConstraint = rightConstraintGo.AddComponent(twoBoneIKConstraintType!);
            var rigBuilderSerialized = new UnityEditor.SerializedObject(rigBuilder);
            var rigLayers = rigBuilderSerialized.FindProperty("m_RigLayers");
            Assert.That(rigLayers, Is.Not.Null, "Expected RigBuilder.m_RigLayers.");
            rigLayers!.arraySize = 1;
            var firstRigLayer = rigLayers.GetArrayElementAtIndex(0);
            Assert.That(firstRigLayer.FindPropertyRelative("m_Active"), Is.Not.Null);
            Assert.That(firstRigLayer.FindPropertyRelative("m_Rig"), Is.Not.Null);
            firstRigLayer.FindPropertyRelative("m_Active")!.boolValue = true;
            firstRigLayer.FindPropertyRelative("m_Rig")!.objectReferenceValue = weaponHandRig as UnityEngine.Object;
            rigBuilderSerialized.ApplyModifiedPropertiesWithoutUndo();
            SetField(handRigController, "_rigBuilder", rigBuilder);
            SetField(handRigController, "_weaponHandRig", weaponHandRig);
            SetField(handRigController, "_leftHandTarget", leftHandTarget);
            SetField(handRigController, "_leftHandHint", leftElbowHint);
            SetField(handRigController, "_rightHandTarget", rightHandTarget);
            SetField(handRigController, "_rightHandHint", rightElbowHint);
            SetField(handRigController, "_leftHandConstraint", leftConstraint);
            SetField(handRigController, "_rightHandConstraint", rightConstraint);

            var viewPrefab = new GameObject("Kar98kView");
            var adsPivot = new GameObject("AdsPivot").transform;
            adsPivot.SetParent(viewPrefab.transform, false);
            var ironSightAnchor = new GameObject("IronSightAnchor").transform;
            ironSightAnchor.SetParent(adsPivot, false);
            var muzzleSlot = new GameObject("MuzzleAttachmentSlot").transform;
            muzzleSlot.SetParent(adsPivot, false);
            ConfigureTestWeaponViewMounts(viewPrefab, adsPivot, ironSightAnchor, muzzleSlot);

            var handAnchors = viewPrefab.AddComponent<WeaponViewHandAnchors>();
            var prefabLeftGrip = new GameObject("RealLeftGrip").transform;
            prefabLeftGrip.SetParent(adsPivot, false);
            prefabLeftGrip.localPosition = new Vector3(0.11f, 0.22f, 0.33f);
            prefabLeftGrip.localRotation = Quaternion.Euler(7f, 17f, 27f);
            var prefabRightGrip = new GameObject("RealRightGrip").transform;
            prefabRightGrip.SetParent(adsPivot, false);
            prefabRightGrip.localPosition = new Vector3(-0.14f, -0.09f, 0.28f);
            prefabRightGrip.localRotation = Quaternion.Euler(-9f, 19f, -11f);
            handAnchors.SetHandTargets(prefabLeftGrip, prefabRightGrip);

            var controller = playerRoot.AddComponent<PlayerWeaponController>();
            SetField(controller, "_cameraDefaults", defaults);
            SetField(controller, "_cameraPivot", presentationPivot);
            SetField(controller, "_viewmodelRoot", playerArmsRoot);
            SetField(controller, "_packAnimator", armsAnimator);
            SetField(controller, "_weaponViewParent", weaponMount);
            SetWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

            Invoke(controller, "SpawnEquippedWeaponView", "weapon-kar98k");
            Invoke(binder, "ResolveReferences");
            Invoke(driver, "ResolveReferences");
            Invoke(driver, "StabilizeViewmodelRootPose");
            handRigController.SyncHandTargets();

            Assert.That(GetField(binder, "_animator"), Is.SameAs(armsAnimator));
            Assert.That(GetField(driver, "_animator"), Is.SameAs(armsAnimator));
            Assert.That(GetField(controller, "_cameraPivot"), Is.SameAs(presentationPivot));
            Assert.That(GetField(controller, "_viewmodelRoot"), Is.SameAs(playerArmsRoot));
            Assert.That(GetField(controller, "_packAnimator"), Is.SameAs(armsAnimator));
            Assert.That(GetField(controller, "_weaponViewParent"), Is.SameAs(weaponMount));
            Assert.That(controller.EquippedWeaponViewTransform, Is.Not.Null);
            Assert.That(controller.EquippedWeaponViewTransform!.parent, Is.SameAs(weaponMount));
            Assert.That(controller.EquippedWeaponViewTransform, Is.Not.SameAs(legacyHandGun));
            Assert.That(controller.EquippedWeaponViewTransform.IsChildOf(legacyHandGun), Is.False);
            Assert.That(GetField(handRigController, "_armsAnimator"), Is.SameAs(armsAnimator));
            Assert.That(Vector3.Distance(playerArmsRoot.localPosition, new Vector3(0f, -0.027f, 0.1f)), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(playerArmsRoot.localRotation, Quaternion.identity), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(playerArmsRoot.localScale, new Vector3(0.42f, 0.42f, 0.42f)), Is.LessThan(0.0001f));

            var liveAnchors = controller.EquippedWeaponViewTransform.GetComponent<WeaponViewHandAnchors>();
            Assert.That(liveAnchors, Is.Not.Null);
            Assert.That(Vector3.Distance(handRigController.LeftHandTarget.position, liveAnchors!.LeftHandGrip.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(handRigController.LeftHandTarget.rotation, liveAnchors.LeftHandGrip.rotation), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(handRigController.LeftHandTarget.position, legacyLeftGrip.position), Is.GreaterThan(0.001f));
            Assert.That(Vector3.Distance(handRigController.RightHandTarget.position, liveAnchors.RightHandGrip.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(handRigController.RightHandTarget.rotation, liveAnchors.RightHandGrip.rotation), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(handRigController.RightHandTarget.position, legacyRightGrip.position), Is.GreaterThan(0.001f));
        }

        [Test]
        public void ResolveReferences_DoesNotUseControllerSidePresentationRootCacheWithoutPlayerCameraDefaultsContract()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var presentationPivot = new GameObject("PresentationPivot").transform;
            presentationPivot.SetParent(playerRoot.transform, false);

            var explicitViewmodelRoot = new GameObject("PresentationArmsRoot").transform;
            explicitViewmodelRoot.SetParent(presentationPivot, false);

            var playerArmsVisual = new GameObject("PlayerArmsVisual");
            playerArmsVisual.transform.SetParent(explicitViewmodelRoot, false);
            var animator = playerArmsVisual.AddComponent<Animator>();

            var explicitPresentationRoot = new GameObject("ExplicitPresentationRoot").transform;
            explicitPresentationRoot.SetParent(presentationPivot, false);

            var controller = playerRoot.AddComponent<PlayerWeaponController>();
            SetField(controller, "_packAnimator", animator);
            SetField(controller, "_cameraPivot", presentationPivot);
            SetField(controller, "_viewmodelRoot", explicitViewmodelRoot);
            SetField(controller, "_weaponViewParent", explicitPresentationRoot);

            Invoke(controller, "ResolveReferences");

            var resolvedParent = (Transform)GetField(controller, "_weaponViewParent");
            Assert.That(resolvedParent, Is.Null,
                "PlayerWeaponController should ignore controller-side presentation-root cache when the explicit PlayerCameraDefaults contract is missing.");
            Assert.That(presentationPivot.Find("WeaponPresentationRoot"), Is.Null,
                "Ignoring the cache must not resurrect legacy CameraPivot/WeaponPresentationRoot creation.");
        }

        [Test]
        public void WeaponPresentationMountDriver_AlignsWeaponPresentationRootToExplicitAuthoredMount()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerRoot.transform, false);

            var playerArms = new GameObject("PlayerArms").transform;
            playerArms.SetParent(cameraPivot, false);

            var playerArmsVisual = new GameObject("PlayerArmsVisual").transform;
            playerArmsVisual.SetParent(playerArms, false);
            var armsAnimator = playerArmsVisual.gameObject.AddComponent<Animator>();

            var armature = new GameObject("Armature").transform;
            armature.SetParent(playerArmsVisual, false);
            var ikHandRoot = new GameObject("ik_hand_root").transform;
            ikHandRoot.SetParent(armature, false);
            var weaponMount = new GameObject("ik_hand_gun").transform;
            weaponMount.SetParent(ikHandRoot, false);
            weaponMount.localPosition = new Vector3(0.41f, -0.13f, 0.27f);
            weaponMount.localRotation = Quaternion.Euler(18f, 27f, 36f);

            var weaponPresentationRoot = new GameObject("WeaponPresentationRoot").transform;
            weaponPresentationRoot.SetParent(cameraPivot, false);
            weaponPresentationRoot.localPosition = Vector3.zero;
            weaponPresentationRoot.localRotation = Quaternion.identity;

            var driverType = FindType("Reloader.Player.Viewmodel.WeaponPresentationMountDriver");
            Assert.That(driverType, Is.Not.Null, "Expected explicit weapon-presentation mount driver to exist.");

            var driver = playerRoot.AddComponent(driverType!);
            SetField(driver, "_weaponPresentationRoot", weaponPresentationRoot);
            SetField(driver, "_weaponPresentationMount", weaponMount);
            SetField(driver, "_armsAnimator", armsAnimator);

            Invoke(driver, "LateUpdate");

            Assert.That(Vector3.Distance(weaponPresentationRoot.position, weaponMount.position), Is.LessThan(0.0001f),
                "WeaponPresentationRoot should follow the authored gun socket position after animator evaluation.");
            Assert.That(Quaternion.Angle(weaponPresentationRoot.rotation, weaponMount.rotation), Is.LessThan(0.01f),
                "WeaponPresentationRoot should follow the authored gun socket rotation after animator evaluation.");
        }

        [Test]
        public void WeaponPresentationMountDriver_HipPose_UsesRightHandGripAsWeaponOwnershipSeam()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerRoot.transform, false);

            var playerArms = new GameObject("PlayerArms").transform;
            playerArms.SetParent(cameraPivot, false);

            var playerArmsVisual = new GameObject("PlayerArmsVisual").transform;
            playerArmsVisual.SetParent(playerArms, false);
            var armsAnimator = playerArmsVisual.gameObject.AddComponent<Animator>();

            var armature = new GameObject("Armature").transform;
            armature.SetParent(playerArmsVisual, false);
            var ikHandRoot = new GameObject("ik_hand_root").transform;
            ikHandRoot.SetParent(armature, false);
            var weaponMount = new GameObject("ik_hand_gun").transform;
            weaponMount.SetParent(ikHandRoot, false);
            weaponMount.localPosition = new Vector3(0.41f, -0.13f, 0.27f);
            weaponMount.localRotation = Quaternion.Euler(18f, 27f, 36f);

            var animatedLeftGrip = new GameObject("ik_hand_l").transform;
            animatedLeftGrip.SetParent(weaponMount, false);
            animatedLeftGrip.localPosition = new Vector3(0.09f, 0.04f, 0.24f);
            animatedLeftGrip.localRotation = Quaternion.Euler(6f, 11f, -3f);

            var animatedRightGrip = new GameObject("ik_hand_r").transform;
            animatedRightGrip.SetParent(weaponMount, false);
            animatedRightGrip.localPosition = new Vector3(-0.03f, -0.02f, 0.11f);
            animatedRightGrip.localRotation = Quaternion.Euler(-4f, 7f, 2f);

            var weaponPresentationRoot = new GameObject("WeaponPresentationRoot").transform;
            weaponPresentationRoot.SetParent(cameraPivot, false);
            weaponPresentationRoot.localPosition = Vector3.zero;
            weaponPresentationRoot.localRotation = Quaternion.identity;

            var weaponView = new GameObject("Kar98kView").transform;
            weaponView.SetParent(weaponPresentationRoot, false);
            var leftGrip = new GameObject("LeftHandGrip").transform;
            leftGrip.SetParent(weaponView, false);
            leftGrip.localPosition = new Vector3(0.08f, 0.03f, 0.31f);
            leftGrip.localRotation = Quaternion.Euler(1f, 13f, -2f);

            var rightGrip = new GameObject("RightHandGrip").transform;
            rightGrip.SetParent(weaponView, false);
            rightGrip.localPosition = new Vector3(-0.04f, -0.03f, 0.18f);
            rightGrip.localRotation = Quaternion.Euler(-7f, 5f, 3f);

            var handAnchors = weaponView.gameObject.AddComponent<WeaponViewHandAnchors>();
            handAnchors.SetHandTargets(leftGrip, rightGrip);

            var driverType = FindType("Reloader.Player.Viewmodel.WeaponPresentationMountDriver");
            Assert.That(driverType, Is.Not.Null, "Expected explicit weapon-presentation mount driver to exist.");

            var driver = playerRoot.AddComponent(driverType!);
            SetField(driver, "_weaponPresentationRoot", weaponPresentationRoot);
            SetField(driver, "_weaponPresentationMount", weaponMount);
            SetField(driver, "_armsAnimator", armsAnimator);
            SetField(driver, "_resolvedAnimatedWeaponMount", weaponMount);
            SetField(driver, "_resolvedAnimatedRightHandAnchor", animatedRightGrip);

            Invoke(driver, "LateUpdate");
            var initialLocalPosition = weaponPresentationRoot.localPosition;
            var initialLocalRotation = weaponPresentationRoot.localRotation;

            leftGrip.localPosition += new Vector3(0.06f, 0.02f, -0.09f);
            leftGrip.localRotation = Quaternion.Euler(18f, -9f, 14f);

            Invoke(driver, "LateUpdate");

            Assert.That(Vector3.Distance(weaponPresentationRoot.localPosition, initialLocalPosition), Is.LessThan(0.0001f),
                "Changing LeftHandGrip should not reposition the rifle. Support-hand authoring must stay IK-only.");
            Assert.That(Quaternion.Angle(weaponPresentationRoot.localRotation, initialLocalRotation), Is.LessThan(0.01f),
                "Changing LeftHandGrip should not rotate the rifle. Support-hand authoring must stay IK-only.");

            var leftDrivenLocalPosition = weaponPresentationRoot.localPosition;
            var leftDrivenLocalRotation = weaponPresentationRoot.localRotation;
            rightGrip.localPosition += new Vector3(-0.08f, 0.04f, 0.03f);
            rightGrip.localRotation = Quaternion.Euler(-16f, 12f, -9f);

            Invoke(driver, "LateUpdate");

            Assert.That(Vector3.Distance(weaponPresentationRoot.localPosition, leftDrivenLocalPosition), Is.GreaterThan(0.01f),
                "Changing the weapon view's authored right-hand grip should move the HIP presentation pose.");
            Assert.That(Quaternion.Angle(weaponPresentationRoot.localRotation, leftDrivenLocalRotation), Is.GreaterThan(0.5f),
                "Changing the weapon view's authored right-hand grip should rotate the HIP presentation pose.");
        }

        [Test]
        public void WeaponPresentationMountDriver_ResolvePresentationBlendT_DoesNotForceHipWhileReloadingAndStillAiming()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var adsStateControllerType = FindType("Reloader.Game.Weapons.AdsStateController");
            Assert.That(adsStateControllerType, Is.Not.Null, "Expected scoped ADS runtime bridge type to exist.");
            var adsStateController = playerRoot.AddComponent(adsStateControllerType!);
            var viewmodelAnimationAdapter = playerRoot.AddComponent<ViewmodelAnimationAdapter>();

            SetField(adsStateController, "<AdsT>k__BackingField", 1f);
            SetField(viewmodelAnimationAdapter, "<IsReloadingDebug>k__BackingField", true);

            var driverType = FindType("Reloader.Player.Viewmodel.WeaponPresentationMountDriver");
            Assert.That(driverType, Is.Not.Null, "Expected explicit weapon-presentation mount driver to exist.");

            var driver = playerRoot.AddComponent(driverType!);
            SetField(driver, "_adsStateController", adsStateController);
            SetField(driver, "_viewmodelAnimationAdapter", viewmodelAnimationAdapter);

            var method = driverType!.GetMethod("ResolvePresentationBlendT", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Expected reload/ADS presentation blend helper.");

            var blendT = (float)method!.Invoke(driver, null);

            Assert.That(blendT, Is.EqualTo(1f).Within(0.0001f),
                "Reloading while still aiming should keep ADS presentation ownership instead of forcing the rifle back to HIP for a frame.");
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

        private static void Invoke(object instance, string methodName, params object[] args)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found.");
            method!.Invoke(instance, args);
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

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void SetWeaponViewBinding(PlayerWeaponController controller, string itemId, GameObject viewPrefab)
        {
            var bindingType = typeof(WeaponViewPrefabBinding);
            var binding = Activator.CreateInstance(bindingType);
            SetField(binding, "_itemId", itemId);
            SetField(binding, "_viewPrefab", viewPrefab);

            var array = Array.CreateInstance(bindingType, 1);
            array.SetValue(binding, 0);
            SetField(controller, "_weaponViewPrefabs", array);
        }

        private static void ConfigureTestWeaponViewMounts(GameObject viewRoot, Transform adsPivot, Transform ironSightAnchor, Transform muzzleSlot)
        {
            var attachmentManagerType = Type.GetType("Reloader.Game.Weapons.AttachmentManager, Reloader.Game.Weapons");
            Assert.That(attachmentManagerType, Is.Not.Null);
            viewRoot.AddComponent(attachmentManagerType!);
            var mounts = viewRoot.AddComponent<WeaponViewAttachmentMounts>();
            SetField(mounts, "_adsPivot", adsPivot);
            SetField(mounts, "_muzzleTransform", adsPivot);
            SetField(mounts, "_ironSightAnchor", ironSightAnchor);
            SetField(mounts, "_magazineSocket", null);
            SetField(mounts, "_magazineDropSocket", null);

            var slotEntryType = typeof(WeaponViewAttachmentMounts).GetNestedType("AttachmentSlotMount", BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(slotEntryType, Is.Not.Null);
            var entry = Activator.CreateInstance(slotEntryType!);
            SetField(entry, "_slotType", WeaponAttachmentSlotType.Muzzle);
            SetField(entry, "_slotTransform", muzzleSlot);
            var entries = Array.CreateInstance(slotEntryType!, 1);
            entries.SetValue(entry, 0);
            SetField(mounts, "_attachmentSlots", entries);
        }

        private static void CreateArmChain(Transform root, string upperArmName, string lowerArmName, string handName, Vector3 lowerArmLocalOffset)
        {
            var upperArm = new GameObject(upperArmName).transform;
            upperArm.SetParent(root, false);
            var lowerArm = new GameObject(lowerArmName).transform;
            lowerArm.SetParent(upperArm, false);
            lowerArm.localPosition = lowerArmLocalOffset;
            var hand = new GameObject(handName).transform;
            hand.SetParent(lowerArm, false);
            hand.localPosition = lowerArmLocalOffset;
        }
    }
}
