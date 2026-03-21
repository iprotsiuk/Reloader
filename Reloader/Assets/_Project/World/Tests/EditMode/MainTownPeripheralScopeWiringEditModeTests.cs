using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Reloader.World.Tests.EditMode
{
    public class MainTownPeripheralScopeWiringEditModeTests
    {
        private const string PlayerRootPrefabPath = "Assets/_Project/Player/Prefabs/PlayerRoot.prefab";

        private static readonly Type PlayerCameraDefaultsType = FindType("Reloader.Player.PlayerCameraDefaults");
        private static readonly Type PlayerWeaponControllerType = FindType("Reloader.Weapons.Controllers.PlayerWeaponController");
        private static readonly Type WeaponHandRigControllerType = FindType("Reloader.Player.Viewmodel.WeaponHandRigController");
        private static readonly Type WeaponPresentationMountDriverType = FindType("Reloader.Player.Viewmodel.WeaponPresentationMountDriver");
        private static readonly Type AdsStateControllerType = FindType("Reloader.Game.Weapons.AdsStateController");
        private static readonly Type RenderTextureScopeControllerType = FindType("Reloader.Game.Weapons.RenderTextureScopeController");
        private static readonly Type PeripheralScopeEffectsType = FindType("Reloader.Game.Weapons.PeripheralScopeEffects");
        private static readonly Type PeripheralScopeScreenMaskType = FindType("Reloader.Game.Weapons.PeripheralScopeScreenMask");
        private static readonly Type WeaponAimAlignerType = FindType("Reloader.Weapons.Runtime.WeaponAimAligner");
        private static readonly Type ScopeAdjustmentTooltipOverlayType = FindType("Reloader.Game.Weapons.ScopeAdjustmentTooltipOverlay");
        private static readonly Type ShotCameraRuntimeType = FindType("Reloader.Weapons.Cinematics.ShotCameraRuntime");
        private static readonly Type CinemachineBrainType = FindType("Unity.Cinemachine.CinemachineBrain");
        private static readonly Type CinemachineCameraType = FindType("Unity.Cinemachine.CinemachineCamera");
        private static readonly Type CinemachineHardLockToTargetType = FindType("Unity.Cinemachine.CinemachineHardLockToTarget");
        private static readonly Type CinemachineHardLookAtType = FindType("Unity.Cinemachine.CinemachineHardLookAt");
        private static readonly Type RigBuilderType = FindType("UnityEngine.Animations.Rigging.RigBuilder");
        private static readonly Type RigType = FindType("UnityEngine.Animations.Rigging.Rig");
        private static readonly Type TwoBoneIKConstraintType = FindType("UnityEngine.Animations.Rigging.TwoBoneIKConstraint");
        private static readonly Type UniversalAdditionalCameraDataType = FindType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");

        [Test]
        public void PlayerRootPrefab_WiresScopedAimAlignerAndUsesPeripheralBlurRuntimeState()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerRootPrefabPath);

            try
            {
                AssertPeripheralScopeWiring(prefabRoot, "PlayerRoot prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [Test]
        public void PlayerRootPrefab_WiresScopedAdsBridgeAndScopeCameraContract()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerRootPrefabPath);

            try
            {
                AssertScopedAdsBridgeWiring(prefabRoot, "PlayerRoot prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [Test]
        [Category("PlayerPrefabContract")]
        public void PlayerRootPrefab_AuthorsWorldCameraCinemachineContract()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerRootPrefabPath);

            try
            {
                AssertWorldCameraCinemachineContract(prefabRoot, "PlayerRoot prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [Test]
        [Category("PlayerPrefabContract")]
        public void PlayerRootPrefab_AuthorsWeaponHandRigContract()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerRootPrefabPath);

            try
            {
                AssertWeaponHandRigContract(prefabRoot, "PlayerRoot prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [Test]
        [Category("PlayerPrefabContract")]
        public void PlayerRootPrefab_AuthorsExplicitWeaponPresentationMountDriverContract()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerRootPrefabPath);

            try
            {
                AssertWeaponPresentationMountDriverContract(prefabRoot, "PlayerRoot prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void AssertPeripheralScopeWiring(GameObject root, string context)
        {
            Assert.That(root, Is.Not.Null, $"{context} should exist.");
            Assert.That(PeripheralScopeEffectsType, Is.Not.Null, "Expected PeripheralScopeEffects type.");
            Assert.That(PeripheralScopeScreenMaskType, Is.Not.Null, "Expected PeripheralScopeScreenMask type.");
            Assert.That(WeaponAimAlignerType, Is.Not.Null, "Expected WeaponAimAligner type.");

            var peripheralEffects = root.GetComponent(PeripheralScopeEffectsType);
            Assert.That(peripheralEffects, Is.Not.Null, $"{context} should include PeripheralScopeEffects.");

            var weaponAimAligner = root.GetComponent(WeaponAimAlignerType);
            Assert.That(weaponAimAligner, Is.Not.Null, $"{context} should include WeaponAimAligner as the canonical scoped owner.");

            var screenMask = root.GetComponent(PeripheralScopeScreenMaskType);
            Assert.That(screenMask, Is.Null, $"{context} should not keep PeripheralScopeScreenMask on the canonical scoped path.");

            var serializedEffects = new SerializedObject(peripheralEffects);
            var scopedBehaviours = serializedEffects.FindProperty("_scopedBehaviours");
            Assert.That(scopedBehaviours, Is.Not.Null, $"{context} should serialize scoped behaviours.");
            Assert.That(scopedBehaviours.arraySize, Is.EqualTo(0), $"{context} should not author the old screen-mask scoped behaviour path.");
        }

        private static void AssertScopedAdsBridgeWiring(GameObject root, string context)
        {
            Assert.That(root, Is.Not.Null, $"{context} should exist.");
            Assert.That(PlayerWeaponControllerType, Is.Not.Null, "Expected PlayerWeaponController type.");
            Assert.That(AdsStateControllerType, Is.Not.Null, "Expected AdsStateController type.");
            Assert.That(RenderTextureScopeControllerType, Is.Not.Null, "Expected RenderTextureScopeController type.");
            Assert.That(PeripheralScopeEffectsType, Is.Not.Null, "Expected PeripheralScopeEffects type.");
            Assert.That(ScopeAdjustmentTooltipOverlayType, Is.Not.Null, "Expected ScopeAdjustmentTooltipOverlay type.");
            Assert.That(UniversalAdditionalCameraDataType, Is.Not.Null, "Expected UniversalAdditionalCameraData type.");
            Assert.That(ShotCameraRuntimeType, Is.Not.Null, "Expected ShotCameraRuntime type.");

            var playerWeaponController = root.GetComponent(PlayerWeaponControllerType);
            Assert.That(playerWeaponController, Is.Not.Null, $"{context} should include PlayerWeaponController.");

            var adsStateController = root.GetComponent(AdsStateControllerType);
            Assert.That(adsStateController, Is.Not.Null, $"{context} should include AdsStateController.");

            var renderTextureScopeController = root.GetComponent(RenderTextureScopeControllerType);
            Assert.That(renderTextureScopeController, Is.Not.Null, $"{context} should include RenderTextureScopeController.");

            var peripheralEffects = root.GetComponent(PeripheralScopeEffectsType);
            Assert.That(peripheralEffects, Is.Not.Null, $"{context} should include PeripheralScopeEffects.");

            var tooltipOverlay = root.GetComponent(ScopeAdjustmentTooltipOverlayType);
            Assert.That(tooltipOverlay, Is.Not.Null, $"{context} should include ScopeAdjustmentTooltipOverlay.");

            var shotCameraRuntime = root.GetComponent(ShotCameraRuntimeType);
            Assert.That(shotCameraRuntime, Is.Not.Null, $"{context} should author ShotCameraRuntime on the canonical player prefab.");

            var scopeCameraTransform = root.transform.Find("CameraPivot/Camera/ScopeCamera");
            Assert.That(scopeCameraTransform, Is.Not.Null, $"{context} should author a ScopeCamera under the world camera.");

            var worldCameraTransform = root.transform.Find("CameraPivot/Camera");
            Assert.That(worldCameraTransform, Is.Not.Null, $"{context} should author the world camera under CameraPivot.");
            Assert.That(scopeCameraTransform.parent, Is.SameAs(worldCameraTransform), $"{context} should parent ScopeCamera to the world camera.");

            var scopeCamera = scopeCameraTransform.GetComponent<Camera>();
            Assert.That(scopeCamera, Is.Not.Null, $"{context} should attach a Camera component to ScopeCamera.");
            Assert.That(scopeCameraTransform.GetComponent(UniversalAdditionalCameraDataType), Is.Not.Null, $"{context} should attach UniversalAdditionalCameraData to ScopeCamera.");

            var playerWeaponSerialized = new SerializedObject(playerWeaponController);
            Assert.That(playerWeaponSerialized.FindProperty("_scopeCamera")?.objectReferenceValue, Is.SameAs(scopeCamera), $"{context} should serialize PlayerWeaponController._scopeCamera to the authored ScopeCamera.");
            Assert.That(playerWeaponSerialized.FindProperty("_adsCamera")?.objectReferenceValue, Is.Null, $"{context} should keep PlayerWeaponController._adsCamera null.");
            Assert.That(playerWeaponSerialized.FindProperty("_shotCameraSettings")?.FindPropertyRelative("_enabled")?.boolValue, Is.True, $"{context} should keep shot camera enabled on the canonical prefab.");
            Assert.That(playerWeaponSerialized.FindProperty("_shotCameraRuntimeBehaviour")?.objectReferenceValue, Is.SameAs(shotCameraRuntime), $"{context} should wire PlayerWeaponController._shotCameraRuntimeBehaviour to the authored ShotCameraRuntime.");

            var adsStateSerialized = new SerializedObject(adsStateController);
            Assert.That(adsStateSerialized.FindProperty("_worldCamera")?.objectReferenceValue, Is.SameAs(worldCameraTransform.GetComponent<Camera>()), $"{context} should wire AdsStateController._worldCamera.");
            Assert.That(adsStateSerialized.FindProperty("_viewmodelCamera")?.objectReferenceValue, Is.SameAs(root.transform.Find("CameraPivot/ViewmodelCamera")?.GetComponent<Camera>()), $"{context} should wire AdsStateController._viewmodelCamera.");
            Assert.That(adsStateSerialized.FindProperty("_renderTextureScopeController")?.objectReferenceValue, Is.SameAs(renderTextureScopeController), $"{context} should wire AdsStateController._renderTextureScopeController.");
            Assert.That(adsStateSerialized.FindProperty("_peripheralScopeEffects")?.objectReferenceValue, Is.SameAs(peripheralEffects), $"{context} should wire AdsStateController._peripheralScopeEffects.");
            Assert.That(adsStateSerialized.FindProperty("_scopeAdjustmentTooltipOverlay")?.objectReferenceValue, Is.SameAs(tooltipOverlay), $"{context} should wire AdsStateController._scopeAdjustmentTooltipOverlay.");

            var renderTextureSerialized = new SerializedObject(renderTextureScopeController);
            Assert.That(renderTextureSerialized.FindProperty("_scopeCamera")?.objectReferenceValue, Is.SameAs(scopeCamera), $"{context} should wire RenderTextureScopeController._scopeCamera.");
        }

        private static void AssertWeaponHandRigContract(GameObject root, string context)
        {
            Assert.That(root, Is.Not.Null, $"{context} should exist.");
            Assert.That(WeaponHandRigControllerType, Is.Not.Null, "Expected WeaponHandRigController type.");
            Assert.That(RigBuilderType, Is.Not.Null, "Expected RigBuilder type.");
            Assert.That(RigType, Is.Not.Null, "Expected Rig type.");
            Assert.That(TwoBoneIKConstraintType, Is.Not.Null, "Expected TwoBoneIKConstraint type.");

            var cameraPivot = root.transform.Find("CameraPivot");
            Assert.That(cameraPivot, Is.Not.Null, $"{context} should author CameraPivot.");

            var weaponHandRigTargets = root.transform.Find("CameraPivot/WeaponHandRigTargets");
            Assert.That(weaponHandRigTargets, Is.Not.Null, $"{context} should author WeaponHandRigTargets under CameraPivot.");

            var leftHandTarget = weaponHandRigTargets.Find("LeftHandTarget");
            Assert.That(leftHandTarget, Is.Not.Null, $"{context} should author LeftHandTarget under WeaponHandRigTargets.");

            var leftElbowHint = weaponHandRigTargets.Find("LeftElbowHint");
            Assert.That(leftElbowHint, Is.Not.Null, $"{context} should author LeftElbowHint under WeaponHandRigTargets.");

            var playerArmsVisual = root.transform.Find("CameraPivot/PlayerArms/PlayerArmsVisual");
            Assert.That(playerArmsVisual, Is.Not.Null, $"{context} should author PlayerArmsVisual on the canonical player path.");

            var animator = playerArmsVisual.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null, $"{context} should keep Animator on PlayerArmsVisual.");

            var rigBuilder = playerArmsVisual.GetComponent(RigBuilderType);
            Assert.That(rigBuilder, Is.Not.Null, $"{context} should author RigBuilder on PlayerArmsVisual.");

            var weaponHandRig = playerArmsVisual.Find("WeaponHandRig");
            Assert.That(weaponHandRig, Is.Not.Null, $"{context} should author WeaponHandRig under PlayerArmsVisual.");

            var rig = weaponHandRig.GetComponent(RigType);
            Assert.That(rig, Is.Not.Null, $"{context} should keep Rig on WeaponHandRig.");

            var leftHandConstraint = weaponHandRig.Find("LeftHandConstraint");
            Assert.That(leftHandConstraint, Is.Not.Null, $"{context} should author LeftHandConstraint under WeaponHandRig.");

            var constraint = leftHandConstraint.GetComponent(TwoBoneIKConstraintType);
            Assert.That(constraint, Is.Not.Null, $"{context} should keep TwoBoneIKConstraint on LeftHandConstraint.");

            var rigBuilderSerialized = new SerializedObject(rigBuilder);
            var rigLayers = rigBuilderSerialized.FindProperty("m_RigLayers");
            Assert.That(rigLayers, Is.Not.Null, $"{context} should serialize RigBuilder.m_RigLayers.");
            Assert.That(rigLayers.arraySize, Is.EqualTo(1), $"{context} should author exactly one rig layer.");

            var firstRigLayer = rigLayers.GetArrayElementAtIndex(0);
            Assert.That(firstRigLayer.FindPropertyRelative("m_Active")?.boolValue, Is.True, $"{context} should keep the rig layer active.");
            Assert.That(firstRigLayer.FindPropertyRelative("m_Rig")?.objectReferenceValue, Is.SameAs(rig), $"{context} should bind the rig layer to WeaponHandRig.");

            var controller = root.GetComponent(WeaponHandRigControllerType);
            Assert.That(controller, Is.Not.Null, $"{context} should include WeaponHandRigController.");

            var controllerSerialized = new SerializedObject(controller);
            Assert.That(controllerSerialized.FindProperty("_armsAnimator")?.objectReferenceValue, Is.SameAs(animator), $"{context} should wire WeaponHandRigController._armsAnimator.");
            Assert.That(controllerSerialized.FindProperty("_rigBuilder")?.objectReferenceValue, Is.SameAs(rigBuilder), $"{context} should wire WeaponHandRigController._rigBuilder.");
            Assert.That(controllerSerialized.FindProperty("_weaponHandRig")?.objectReferenceValue, Is.SameAs(rig), $"{context} should wire WeaponHandRigController._weaponHandRig.");
            Assert.That(controllerSerialized.FindProperty("_leftHandConstraint")?.objectReferenceValue, Is.SameAs(constraint), $"{context} should wire WeaponHandRigController._leftHandConstraint.");
            Assert.That(controllerSerialized.FindProperty("_leftHandTarget")?.objectReferenceValue, Is.SameAs(leftHandTarget), $"{context} should wire WeaponHandRigController._leftHandTarget.");
            Assert.That(controllerSerialized.FindProperty("_leftHandHint")?.objectReferenceValue, Is.SameAs(leftElbowHint), $"{context} should wire WeaponHandRigController._leftHandHint.");
        }

        private static void AssertWorldCameraCinemachineContract(GameObject root, string context)
        {
            Assert.That(root, Is.Not.Null, $"{context} should exist.");
            Assert.That(PlayerCameraDefaultsType, Is.Not.Null, "Expected PlayerCameraDefaults type.");
            Assert.That(CinemachineBrainType, Is.Not.Null, "Expected CinemachineBrain type.");
            Assert.That(CinemachineCameraType, Is.Not.Null, "Expected CinemachineCamera type.");
            Assert.That(CinemachineHardLockToTargetType, Is.Not.Null, "Expected CinemachineHardLockToTarget type.");
            Assert.That(CinemachineHardLookAtType, Is.Not.Null, "Expected CinemachineHardLookAt type.");

            var cameraPivot = root.transform.Find("CameraPivot");
            Assert.That(cameraPivot, Is.Not.Null, $"{context} should author CameraPivot.");

            var cameraLookTarget = root.transform.Find("CameraPivot/CameraLookTarget");
            Assert.That(cameraLookTarget, Is.Not.Null, $"{context} should author CameraPivot/CameraLookTarget.");

            var worldCameraTransform = root.transform.Find("CameraPivot/Camera");
            Assert.That(worldCameraTransform, Is.Not.Null, $"{context} should author the world camera under CameraPivot.");

            var worldCamera = worldCameraTransform.GetComponent<Camera>();
            Assert.That(worldCamera, Is.Not.Null, $"{context} should author a Camera component on the world camera.");
            Assert.That(worldCameraTransform.GetComponent(CinemachineBrainType), Is.Not.Null, $"{context} should author a CinemachineBrain on the world camera.");

            var cinemachineCameraTransform = root.transform.Find("CM_PlayerCamera");
            Assert.That(cinemachineCameraTransform, Is.Not.Null, $"{context} should author a CM_PlayerCamera child.");
            Assert.That(cinemachineCameraTransform.parent, Is.SameAs(root.transform), $"{context} should keep CM_PlayerCamera under the player root.");

            var cinemachineCamera = cinemachineCameraTransform.GetComponent(CinemachineCameraType);
            Assert.That(cinemachineCamera, Is.Not.Null, $"{context} should author a CinemachineCamera component.");
            Assert.That(cinemachineCameraTransform.GetComponent(CinemachineHardLockToTargetType), Is.Not.Null, $"{context} should author CinemachineHardLockToTarget on the virtual camera.");
            Assert.That(cinemachineCameraTransform.GetComponent(CinemachineHardLookAtType), Is.Not.Null, $"{context} should author CinemachineHardLookAt on the virtual camera.");

            var cinemachineSerialized = new SerializedObject(cinemachineCamera);
            var target = cinemachineSerialized.FindProperty("Target");
            Assert.That(target, Is.Not.Null, $"{context} should serialize CinemachineCamera.Target.");
            Assert.That(target.FindPropertyRelative("TrackingTarget")?.objectReferenceValue, Is.SameAs(cameraPivot), $"{context} should follow CameraPivot.");
            Assert.That(target.FindPropertyRelative("LookAtTarget")?.objectReferenceValue, Is.SameAs(cameraLookTarget), $"{context} should look at CameraLookTarget.");
            Assert.That(target.FindPropertyRelative("CustomLookAtTarget")?.boolValue, Is.True, $"{context} should author a custom look-at target.");

            var defaults = root.GetComponent(PlayerCameraDefaultsType);
            Assert.That(defaults, Is.Not.Null, $"{context} should author PlayerCameraDefaults.");

            var defaultsSerialized = new SerializedObject(defaults);
            Assert.That(defaultsSerialized.FindProperty("_mainCamera")?.objectReferenceValue, Is.SameAs(worldCamera), $"{context} should wire PlayerCameraDefaults._mainCamera.");
            Assert.That(defaultsSerialized.FindProperty("_brain")?.objectReferenceValue, Is.SameAs(worldCameraTransform.GetComponent(CinemachineBrainType)), $"{context} should wire PlayerCameraDefaults._brain.");
            Assert.That(defaultsSerialized.FindProperty("_cinemachineCamera")?.objectReferenceValue, Is.SameAs(cinemachineCamera), $"{context} should wire PlayerCameraDefaults._cinemachineCamera.");
            Assert.That(defaultsSerialized.FindProperty("_cameraFollowTarget")?.objectReferenceValue, Is.SameAs(cameraPivot), $"{context} should wire PlayerCameraDefaults._cameraFollowTarget.");
            Assert.That(defaultsSerialized.FindProperty("_cameraLookTarget")?.objectReferenceValue, Is.SameAs(cameraLookTarget), $"{context} should wire PlayerCameraDefaults._cameraLookTarget.");
            Assert.That(defaultsSerialized.FindProperty("_viewmodelCameraParent")?.objectReferenceValue, Is.SameAs(cameraPivot), $"{context} should keep the viewmodel camera parent intact.");
            Assert.That(defaultsSerialized.FindProperty("_viewmodelCamera")?.objectReferenceValue, Is.SameAs(root.transform.Find("CameraPivot/ViewmodelCamera")?.GetComponent<Camera>()), $"{context} should keep the viewmodel camera intact.");
            Assert.That(root.transform.Find("CameraPivot/Camera/ScopeCamera"), Is.Not.Null, $"{context} should keep the authored ScopeCamera under the world camera.");
            Assert.That(root.transform.Find("CameraPivot/ViewmodelCamera"), Is.Not.Null, $"{context} should keep the authored ViewmodelCamera under CameraPivot.");
        }

        private static void AssertWeaponPresentationMountDriverContract(GameObject root, string context)
        {
            Assert.That(root, Is.Not.Null, $"{context} should exist.");
            Assert.That(WeaponPresentationMountDriverType, Is.Not.Null, "Expected WeaponPresentationMountDriver type.");

            var weaponPresentationRoot = root.transform.Find("CameraPivot/WeaponPresentationRoot");
            Assert.That(weaponPresentationRoot, Is.Not.Null, $"{context} should author WeaponPresentationRoot under CameraPivot.");

            var explicitWeaponPresentationMount = root.transform.Find("CameraPivot/PlayerArms/PlayerArmsVisual/Armature/root/ik_hand_root/ik_hand_gun");
            Assert.That(explicitWeaponPresentationMount, Is.Not.Null,
                $"{context} should keep the authored ik_hand_gun gun socket as the explicit weapon presentation mount seam.");

            var mountDriver = root.GetComponent(WeaponPresentationMountDriverType);
            Assert.That(mountDriver, Is.Not.Null, $"{context} should include WeaponPresentationMountDriver on PlayerRoot.");

            var serialized = new SerializedObject(mountDriver);
            Assert.That(serialized.FindProperty("_weaponPresentationRoot")?.objectReferenceValue, Is.SameAs(weaponPresentationRoot),
                $"{context} should wire WeaponPresentationMountDriver._weaponPresentationRoot to CameraPivot/WeaponPresentationRoot.");
            Assert.That(serialized.FindProperty("_weaponPresentationMount")?.objectReferenceValue, Is.Null,
                $"{context} should not rely on a fragile nested-object reference for the weapon presentation mount.");
            Assert.That(serialized.FindProperty("_weaponPresentationMountPath")?.stringValue,
                Is.EqualTo("CameraPivot/PlayerArms/PlayerArmsVisual/Armature/root/ik_hand_root/ik_hand_gun"),
                $"{context} should wire WeaponPresentationMountDriver._weaponPresentationMountPath to the authored ik_hand_gun socket seam.");
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
    }
}
