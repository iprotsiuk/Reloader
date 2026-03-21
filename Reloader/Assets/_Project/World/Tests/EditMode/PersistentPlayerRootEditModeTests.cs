using NUnit.Framework;
using Reloader.World.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Tests.EditMode
{
    public class PersistentPlayerRootEditModeTests
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

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

        [Test]
        public void CaptureOrAdoptPlayerRootForScene_WithCanonicalRuntimePlayerRoot_DoesNotSwapToSceneAuthoredPlayerRoot()
        {
            var persistentRoot = BootstrapWorldRoot.Initialize();
            var runtimeOwnedPlayerRoot = new GameObject("RuntimeOwnedPlayerRoot");
            AssignPlayerRootTransform(persistentRoot, runtimeOwnedPlayerRoot.transform);

            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
                var scenePlayerRoot = new GameObject("PlayerRoot");
                SceneManager.MoveGameObjectToScene(scenePlayerRoot, scene);

                var captured = persistentRoot.CaptureOrAdoptPlayerRootForScene(scene, preferSceneRoot: true);

                Assert.That(captured, Is.Not.Null);
                Assert.That(captured.name, Is.EqualTo("RuntimeOwnedPlayerRoot"),
                    "PersistentPlayerRoot should preserve the canonical runtime-owned player root instead of swapping to scene content.");
                Assert.That(persistentRoot.PlayerRootTransform, Is.SameAs(captured));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }

                if (runtimeOwnedPlayerRoot != null)
                {
                    Object.DestroyImmediate(runtimeOwnedPlayerRoot);
                }
            }
        }

        [Test]
        public void CaptureOrAdoptPlayerRootForScene_WithoutCanonicalRuntimePlayerRoot_FailsClosedInsteadOfAdoptingScenePlayerRoot()
        {
            var persistentRoot = BootstrapWorldRoot.Initialize();
            AssignPlayerRootTransform(persistentRoot, null);

            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
                var scenePlayerRoot = new GameObject("PlayerRoot");
                SceneManager.MoveGameObjectToScene(scenePlayerRoot, scene);

                var captured = persistentRoot.CaptureOrAdoptPlayerRootForScene(scene, preferSceneRoot: false);

                Assert.That(captured, Is.Null,
                    "Bootstrap must create the canonical runtime player root. PersistentPlayerRoot should not adopt scene-authored PlayerRoot content.");
                Assert.That(persistentRoot.PlayerRootTransform, Is.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
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

        private static void AssignPlayerRootTransform(PersistentPlayerRoot persistentRoot, Transform playerRootTransform)
        {
            var serialized = new SerializedObject(persistentRoot);
            serialized.FindProperty("_playerRootTransform").objectReferenceValue = playerRootTransform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
