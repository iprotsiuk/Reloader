using System.Reflection;
using NUnit.Framework;
using Reloader.Player;
using UnityEngine;

namespace Reloader.Player.Tests.EditMode
{
    public sealed class FpsViewmodelAnimatorDriverEditModeTests
    {
        [Test]
        public void StabilizeViewmodelRootPose_UsesExplicitPlayerArmsRootWithoutCameraPivotNameGate()
        {
            var root = new GameObject("PlayerRoot");
            var presentationPivot = new GameObject("PresentationPivot").transform;
            presentationPivot.SetParent(root.transform, false);

            var playerArmsRoot = new GameObject("ArmsBranch").transform;
            playerArmsRoot.SetParent(presentationPivot, false);
            playerArmsRoot.localPosition = new Vector3(5f, 6f, 7f);
            playerArmsRoot.localRotation = Quaternion.Euler(20f, 30f, 40f);
            playerArmsRoot.localScale = new Vector3(2f, 3f, 4f);

            var armsVisual = new GameObject("ViewArmsVisual");
            armsVisual.transform.SetParent(playerArmsRoot, false);
            var animator = armsVisual.AddComponent<Animator>();

            var cameraDefaults = root.AddComponent<PlayerCameraDefaults>();
            SetField(cameraDefaults, "_cameraPivot", presentationPivot);
            SetField(cameraDefaults, "_playerArmsRoot", playerArmsRoot);
            SetField(cameraDefaults, "_playerArmsAnimator", animator);

            var driver = root.AddComponent<FpsViewmodelAnimatorDriver>();
            SetField(driver, "_cameraDefaults", cameraDefaults);
            driver.LockViewmodelRootPose = true;

            Invoke(driver, "ResolveReferences");
            Invoke(driver, "StabilizeViewmodelRootPose");

            Assert.That(Vector3.Distance(playerArmsRoot.localPosition, new Vector3(0f, -0.027f, 0.1f)), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(playerArmsRoot.localRotation, Quaternion.identity), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(playerArmsRoot.localScale, new Vector3(0.42f, 0.42f, 0.42f)), Is.LessThan(0.0001f));
        }

        [Test]
        public void ResolveReferences_WithoutExplicitOwnershipContract_DoesNotRecoverAnimatorFromLegacyHierarchySearch()
        {
            var root = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);
            var playerArmsRoot = new GameObject("PlayerArms").transform;
            playerArmsRoot.SetParent(cameraPivot, false);
            var armsVisual = new GameObject("PlayerArmsVisual");
            armsVisual.transform.SetParent(playerArmsRoot, false);
            armsVisual.AddComponent<Animator>();

            var driver = root.AddComponent<FpsViewmodelAnimatorDriver>();

            Invoke(driver, "ResolveReferences");

            Assert.That(GetField(driver, "_animator"), Is.Null,
                "FpsViewmodelAnimatorDriver should not recover its animator from legacy CameraPivot/PlayerArms name searches when no explicit contract is configured.");
        }

        [Test]
        public void ResolveReferences_PreservesExplicitAnimatorAndRoot_WhenTheyRemainValidOnPlayerHierarchy()
        {
            var root = new GameObject("PlayerRoot");
            var presentationPivot = new GameObject("PresentationPivot").transform;
            presentationPivot.SetParent(root.transform, false);

            var explicitRoot = new GameObject("ExplicitArmsRoot").transform;
            explicitRoot.SetParent(root.transform, false);
            var explicitVisual = new GameObject("ExplicitArmsVisual").transform;
            explicitVisual.SetParent(explicitRoot, false);
            var explicitAnimator = explicitVisual.gameObject.AddComponent<Animator>();

            var defaultsRoot = new GameObject("DefaultsArmsRoot").transform;
            defaultsRoot.SetParent(presentationPivot, false);
            var defaultsVisual = new GameObject("DefaultsArmsVisual").transform;
            defaultsVisual.SetParent(defaultsRoot, false);
            var defaultsAnimator = defaultsVisual.gameObject.AddComponent<Animator>();

            var cameraDefaults = root.AddComponent<PlayerCameraDefaults>();
            SetField(cameraDefaults, "_cameraPivot", presentationPivot);
            SetField(cameraDefaults, "_playerArmsRoot", defaultsRoot);
            SetField(cameraDefaults, "_playerArmsAnimator", defaultsAnimator);

            var driver = root.AddComponent<FpsViewmodelAnimatorDriver>();
            SetField(driver, "_cameraDefaults", cameraDefaults);
            SetField(driver, "_animator", explicitAnimator);
            SetField(driver, "_viewmodelRoot", explicitRoot);

            Invoke(driver, "ResolveReferences");

            Assert.That(GetField(driver, "_animator"), Is.SameAs(explicitAnimator));
            Assert.That(GetField(driver, "_viewmodelRoot"), Is.SameAs(explicitRoot));
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
