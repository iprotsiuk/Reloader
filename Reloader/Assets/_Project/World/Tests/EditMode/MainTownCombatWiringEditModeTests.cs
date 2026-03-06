using System.Reflection;
using NUnit.Framework;
using Reloader.Weapons.Runtime;
using Reloader.World.Editor;
using UnityEditor;
using UnityEngine;

namespace Reloader.World.Tests.EditMode
{
    public class MainTownCombatWiringEditModeTests
    {
        private static readonly MethodInfo ShouldSeedDefaultWeaponPoseTuningMethod =
            typeof(MainTownCombatWiring).GetMethod(
                "ShouldSeedDefaultWeaponPoseTuning",
                BindingFlags.NonPublic | BindingFlags.Static);

        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("PoseTuningRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        [Test]
        public void ShouldSeedDefaultWeaponPoseTuning_WhenHelperHasFreshDefaultBlendSpeed_ReturnsTrue()
        {
            var helper = _root.AddComponent<WeaponViewPoseTuningHelper>();
            var serializedObject = new SerializedObject(helper);

            var shouldSeed = InvokeShouldSeedDefaultWeaponPoseTuning(serializedObject);

            Assert.That(shouldSeed, Is.True);
        }

        [Test]
        public void ShouldSeedDefaultWeaponPoseTuning_WhenHelperHasAuthoredPose_ReturnsFalse()
        {
            var helper = _root.AddComponent<WeaponViewPoseTuningHelper>();
            var serializedObject = new SerializedObject(helper);
            serializedObject.FindProperty("_adsLocalPosition").vector3Value = new Vector3(0f, 0.2f, 0.05f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var shouldSeed = InvokeShouldSeedDefaultWeaponPoseTuning(serializedObject);

            Assert.That(shouldSeed, Is.False);
        }

        private static bool InvokeShouldSeedDefaultWeaponPoseTuning(SerializedObject serializedObject)
        {
            Assert.That(ShouldSeedDefaultWeaponPoseTuningMethod, Is.Not.Null);
            return (bool)ShouldSeedDefaultWeaponPoseTuningMethod.Invoke(null, new object[] { serializedObject });
        }
    }
}
