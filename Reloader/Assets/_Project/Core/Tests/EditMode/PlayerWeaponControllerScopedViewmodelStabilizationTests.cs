using System.Reflection;
using NUnit.Framework;
using Reloader.Player;
using Reloader.Weapons.Controllers;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public sealed class PlayerWeaponControllerScopedViewmodelStabilizationTests
    {
        [Test]
        public void ResolveCurrentAdsBlendT_WithoutAdsBridge_DoesNotFallBackToRawAimHeld()
        {
            var root = new GameObject("PlayerRoot");
            var controller = root.AddComponent<PlayerWeaponController>();
            SetField(controller, "_inputSource", new StubWeaponInputSource { AimHeld = true });

            var method = typeof(PlayerWeaponController).GetMethod(
                "ResolveCurrentAdsBlendT",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected private ADS blend resolver to exist.");

            var actual = (float)method!.Invoke(controller, null);
            Assert.That(actual, Is.EqualTo(0f));
        }

        [TestCase(false, true, true, 0.999f, true, true)]
        [TestCase(false, true, true, 1.0f, true, true)]
        [TestCase(false, true, true, 0.998f, true, false)]
        [TestCase(true, true, true, 0.95f, true, true)]
        [TestCase(true, true, true, 0.949f, true, false)]
        [TestCase(false, true, false, 1.0f, true, false)]
        [TestCase(false, false, true, 1.0f, true, false)]
        [TestCase(false, true, true, 1.0f, false, false)]
        public void ShouldStabilizeScopedViewmodelPresentation_UsesOnlyFullyScopedMagnifiedActiveState(
            bool isCurrentlyStable,
            bool hasScopedAdsAlignment,
            bool hasMagnifiedOpticEquipped,
            float adsBlendT,
            bool hasEquippedView,
            bool expected)
        {
            var method = typeof(PlayerWeaponController).GetMethod(
                "ShouldStabilizeScopedViewmodelPresentation",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected private scoped viewmodel stabilization helper to exist.");

            var actual = (bool)method!.Invoke(null, new object[]
            {
                isCurrentlyStable,
                hasScopedAdsAlignment,
                hasMagnifiedOpticEquipped,
                adsBlendT,
                hasEquippedView
            });

            Assert.That(actual, Is.EqualTo(expected));
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field!.SetValue(instance, value);
        }

        private sealed class StubWeaponInputSource : IPlayerInputSource
        {
            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool FirePressed => false;
            public bool AimHeld { get; set; }
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public bool ConsumeFirePressed() => FirePressed;
            public bool ConsumeReloadPressed() => false;
            public bool ConsumePickupPressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;
            public int ConsumeBeltSelectPressed() => 0;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeDevConsoleTogglePressed() => false;
            public bool ConsumeAutocompletePressed() => false;
            public int ConsumeSuggestionDelta() => 0;
        }
    }
}
