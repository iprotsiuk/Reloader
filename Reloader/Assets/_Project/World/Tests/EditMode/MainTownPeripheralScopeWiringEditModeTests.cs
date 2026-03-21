using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Reloader.World.Tests.EditMode
{
    public class MainTownPeripheralScopeWiringEditModeTests
    {
        private const string PlayerRootPrefabPath = "Assets/_Project/Player/Prefabs/PlayerRoot.prefab";

        private static readonly Type PlayerWeaponControllerType = FindType("Reloader.Weapons.Controllers.PlayerWeaponController");
        private static readonly Type AdsStateControllerType = FindType("Reloader.Game.Weapons.AdsStateController");
        private static readonly Type RenderTextureScopeControllerType = FindType("Reloader.Game.Weapons.RenderTextureScopeController");
        private static readonly Type PeripheralScopeEffectsType = FindType("Reloader.Game.Weapons.PeripheralScopeEffects");
        private static readonly Type PeripheralScopeScreenMaskType = FindType("Reloader.Game.Weapons.PeripheralScopeScreenMask");
        private static readonly Type ScopeAdjustmentTooltipOverlayType = FindType("Reloader.Game.Weapons.ScopeAdjustmentTooltipOverlay");
        private static readonly Type UniversalAdditionalCameraDataType = FindType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");

        [Test]
        public void PlayerRootPrefab_WiresPeripheralScopeEffectsToScreenMask()
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

        private static void AssertPeripheralScopeWiring(GameObject root, string context)
        {
            Assert.That(root, Is.Not.Null, $"{context} should exist.");
            Assert.That(PeripheralScopeEffectsType, Is.Not.Null, "Expected PeripheralScopeEffects type.");
            Assert.That(PeripheralScopeScreenMaskType, Is.Not.Null, "Expected PeripheralScopeScreenMask type.");

            var peripheralEffects = root.GetComponent(PeripheralScopeEffectsType);
            Assert.That(peripheralEffects, Is.Not.Null, $"{context} should include PeripheralScopeEffects.");

            var screenMask = root.GetComponent(PeripheralScopeScreenMaskType);
            Assert.That(screenMask, Is.Not.Null, $"{context} should include PeripheralScopeScreenMask.");

            var serializedEffects = new SerializedObject(peripheralEffects);
            var scopedBehaviours = serializedEffects.FindProperty("_scopedBehaviours");
            Assert.That(scopedBehaviours, Is.Not.Null, $"{context} should serialize scoped behaviours.");
            Assert.That(scopedBehaviours.arraySize, Is.GreaterThan(0), $"{context} should author at least one scoped behaviour.");

            var firstBehaviour = scopedBehaviours.GetArrayElementAtIndex(0).objectReferenceValue as Behaviour;
            Assert.That(firstBehaviour, Is.SameAs(screenMask), $"{context} should route PeripheralScopeEffects to the authored screen mask.");
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
            Assert.That(playerWeaponSerialized.FindProperty("_shotCameraSettings")?.FindPropertyRelative("_enabled")?.boolValue, Is.False, $"{context} should explicitly disable shot camera on the canonical prefab for this branch.");
            Assert.That(playerWeaponSerialized.FindProperty("_shotCameraRuntimeBehaviour")?.objectReferenceValue, Is.Null, $"{context} should keep shot camera runtime unauthored while the feature is disabled.");

            var adsStateSerialized = new SerializedObject(adsStateController);
            Assert.That(adsStateSerialized.FindProperty("_worldCamera")?.objectReferenceValue, Is.SameAs(worldCameraTransform.GetComponent<Camera>()), $"{context} should wire AdsStateController._worldCamera.");
            Assert.That(adsStateSerialized.FindProperty("_viewmodelCamera")?.objectReferenceValue, Is.SameAs(root.transform.Find("CameraPivot/ViewmodelCamera")?.GetComponent<Camera>()), $"{context} should wire AdsStateController._viewmodelCamera.");
            Assert.That(adsStateSerialized.FindProperty("_renderTextureScopeController")?.objectReferenceValue, Is.SameAs(renderTextureScopeController), $"{context} should wire AdsStateController._renderTextureScopeController.");
            Assert.That(adsStateSerialized.FindProperty("_peripheralScopeEffects")?.objectReferenceValue, Is.SameAs(peripheralEffects), $"{context} should wire AdsStateController._peripheralScopeEffects.");
            Assert.That(adsStateSerialized.FindProperty("_scopeAdjustmentTooltipOverlay")?.objectReferenceValue, Is.SameAs(tooltipOverlay), $"{context} should wire AdsStateController._scopeAdjustmentTooltipOverlay.");

            var renderTextureSerialized = new SerializedObject(renderTextureScopeController);
            Assert.That(renderTextureSerialized.FindProperty("_scopeCamera")?.objectReferenceValue, Is.SameAs(scopeCamera), $"{context} should wire RenderTextureScopeController._scopeCamera.");
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
