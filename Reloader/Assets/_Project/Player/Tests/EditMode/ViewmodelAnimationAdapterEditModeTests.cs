using System.Reflection;
using NUnit.Framework;
using Reloader.Player;
using Reloader.Player.Viewmodel;
using UnityEngine;

namespace Reloader.Player.Tests.EditMode
{
    public sealed class ViewmodelAnimationAdapterEditModeTests
    {
        [Test]
        public void ResolveAnimator_UsesExplicitPlayerCameraDefaultsAnimator()
        {
            var root = new GameObject("PlayerRoot");
            var presentationPivot = new GameObject("PresentationPivot").transform;
            presentationPivot.SetParent(root.transform, false);

            var playerArmsRoot = new GameObject("ArmsBranch").transform;
            playerArmsRoot.SetParent(presentationPivot, false);
            var armsVisual = new GameObject("ViewArmsVisual").transform;
            armsVisual.SetParent(playerArmsRoot, false);
            var animator = armsVisual.gameObject.AddComponent<Animator>();

            var defaults = root.AddComponent<PlayerCameraDefaults>();
            SetField(defaults, "_cameraPivot", presentationPivot);
            SetField(defaults, "_playerArmsRoot", playerArmsRoot);
            SetField(defaults, "_playerArmsAnimator", animator);

            var adapter = root.AddComponent<ViewmodelAnimationAdapter>();

            Invoke(adapter, "ResolveAnimator");

            Assert.That(GetField(adapter, "_animator"), Is.SameAs(animator));
        }

        [Test]
        public void ResolveAnimator_WithoutExplicitOwnershipContract_DoesNotSearchLegacyHierarchy()
        {
            var root = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);
            var playerArmsRoot = new GameObject("PlayerArms").transform;
            playerArmsRoot.SetParent(cameraPivot, false);
            var playerArmsVisual = new GameObject("PlayerArmsVisual").transform;
            playerArmsVisual.SetParent(playerArmsRoot, false);
            playerArmsVisual.gameObject.AddComponent<Animator>();

            var adapter = root.AddComponent<ViewmodelAnimationAdapter>();

            Invoke(adapter, "ResolveAnimator");

            Assert.That(GetField(adapter, "_animator"), Is.Null,
                "ViewmodelAnimationAdapter should not recover its animator from CameraPivot/PlayerArms/PlayerArmsVisual when the explicit camera-defaults contract is absent.");
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
