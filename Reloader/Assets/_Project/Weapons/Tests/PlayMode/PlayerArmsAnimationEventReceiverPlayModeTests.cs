using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class PlayerArmsAnimationEventReceiverPlayModeTests
    {
        private const string ReceiverTypeName = "Reloader.Weapons.Animations.PlayerArmsAnimationEventReceiver, Reloader.Weapons";

        [Test]
        public void ReceiverType_ExposesLegacyAnimationEventMethods()
        {
            var receiverType = Type.GetType(ReceiverTypeName);
            Assert.That(receiverType, Is.Not.Null, "Expected a receiver type for legacy animation events.");

            AssertHasReceiverMethod(receiverType, "OnAnimationEndedHolster");
            AssertHasReceiverMethod(receiverType, "OnAmmunitionFill");
            AssertHasReceiverMethod(receiverType, "OnAnimationEndedReload");
        }

        [Test]
        public void EnsureReceiver_AddsAndReusesReceiverOnAnimatorGameObject()
        {
            var receiverType = Type.GetType(ReceiverTypeName);
            Assert.That(receiverType, Is.Not.Null, "Expected a receiver type for legacy animation events.");

            var ensureMethod = receiverType.GetMethod(
                "EnsureReceiver",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Animator) },
                null);
            Assert.That(ensureMethod, Is.Not.Null, "Expected static EnsureReceiver(Animator) helper.");

            var root = new GameObject("ArmsVisualReceiverTest");
            var animator = root.AddComponent<Animator>();

            try
            {
                var first = ensureMethod.Invoke(null, new object[] { animator }) as Component;
                var second = ensureMethod.Invoke(null, new object[] { animator }) as Component;

                Assert.That(first, Is.Not.Null);
                Assert.That(second, Is.SameAs(first));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssertHasReceiverMethod(Type receiverType, string methodName)
        {
            var method = receiverType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null);

            Assert.That(method, Is.Not.Null, $"Expected receiver method '{methodName}' to exist.");
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
        }
    }
}
