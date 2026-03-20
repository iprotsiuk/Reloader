using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Weapons.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.Weapons.Tests.PlayMode
{
    public class WeaponViewPoseTuningHelperPlayModeTests
    {
        [UnityTest]
        public IEnumerator ShouldWriteEquippedRootPoseAtRuntime_ReturnsTrueInPlayModeWithoutEditorSelection()
        {
            GameObject root = null;

            try
            {
                root = new GameObject("WeaponView");
                var helper = root.AddComponent<WeaponViewPoseTuningHelper>();

#if UNITY_EDITOR
                Selection.objects = new Object[0];
#endif

                yield return null;

                var shouldWrite = InvokeShouldWriteEquippedRootPoseAtRuntime(helper);
                Assert.That(shouldWrite, Is.True,
                    "Play mode equipped root pose writes should not depend on the helper being selected in the editor.");
            }
            finally
            {
#if UNITY_EDITOR
                Selection.objects = new Object[0];
#endif

                if (root != null)
                {
                    Object.Destroy(root);
                }
            }
        }

        private static bool InvokeShouldWriteEquippedRootPoseAtRuntime(WeaponViewPoseTuningHelper helper)
        {
            var method = typeof(WeaponViewPoseTuningHelper).GetMethod(
                "ShouldWriteEquippedRootPoseAtRuntime",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Expected runtime write gate to exist on WeaponViewPoseTuningHelper.");
            return (bool)method!.Invoke(helper, null);
        }
    }
}
