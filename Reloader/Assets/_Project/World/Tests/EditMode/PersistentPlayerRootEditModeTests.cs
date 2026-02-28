using NUnit.Framework;
using Reloader.World.Runtime;
using UnityEngine;

namespace Reloader.World.Tests.EditMode
{
    public class PersistentPlayerRootEditModeTests
    {
        [TearDown]
        public void TearDown()
        {
            var roots = FindPersistentRoots();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null)
                {
                    Object.DestroyImmediate(roots[i].gameObject);
                }
            }
        }

        [Test]
        public void Initialize_CreatesSinglePersistentRootAndMovesItToDontDestroyOnLoadScene()
        {
            var root = BootstrapWorldRoot.Initialize();

            Assert.That(root, Is.Not.Null);
            Assert.That(PersistentPlayerRoot.Instance, Is.SameAs(root));
            if (Application.isPlaying)
            {
                Assert.That(root.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
            }
            else
            {
                Assert.That(root.gameObject.scene.IsValid(), Is.True);
            }

            var roots = Object.FindObjectsByType<PersistentPlayerRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(roots.Length, Is.EqualTo(1));
        }

        [Test]
        public void Initialize_WhenCalledTwice_ReusesSameRootInstance()
        {
            var first = BootstrapWorldRoot.Initialize();
            var second = BootstrapWorldRoot.Initialize();

            Assert.That(second, Is.SameAs(first));

            var roots = Object.FindObjectsByType<PersistentPlayerRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(roots.Length, Is.EqualTo(1));
        }

        [Test]
        public void Awake_WhenDuplicateExists_DestroysDuplicateAndKeepsOriginalInstance()
        {
            var first = BootstrapWorldRoot.Initialize();
            var duplicateGameObject = new GameObject("PersistentPlayerRoot_Duplicate");
            var duplicate = duplicateGameObject.AddComponent<PersistentPlayerRoot>();

            Assert.That(duplicate, Is.Not.Null);
            Assert.That(PersistentPlayerRoot.Instance, Is.SameAs(first));
            var roots = Object.FindObjectsByType<PersistentPlayerRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (Application.isPlaying)
            {
                Assert.That(duplicate == null, Is.True);
                Assert.That(roots.Length, Is.EqualTo(1));
                Assert.That(roots[0], Is.SameAs(first));
            }
            else
            {
                Assert.That(ContainsRoot(roots, first), Is.True);
            }
        }

        private static PersistentPlayerRoot[] FindPersistentRoots()
        {
            return Object.FindObjectsByType<PersistentPlayerRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private static bool ContainsRoot(PersistentPlayerRoot[] roots, PersistentPlayerRoot target)
        {
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i] == target)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
