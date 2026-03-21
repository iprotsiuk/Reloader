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
        private const string PlayerRootPrefabPath = "Assets/_Project/Player/Prefabs/PlayerRoot.prefab";

        [TearDown]
        public void TearDown()
        {
            if (PersistentPlayerRoot.Instance != null && PersistentPlayerRoot.Instance.PlayerRootTransform != null)
            {
                Object.DestroyImmediate(PersistentPlayerRoot.Instance.PlayerRootTransform.gameObject);
            }

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
        public void BootstrapScene_SerializesCanonicalRuntimePlayerPrefabReference()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
                var bootstrapWorldRoot = FindBootstrapWorldRoot(scene);
                var serialized = new SerializedObject(bootstrapWorldRoot);
                var prefabProperty = serialized.FindProperty("_playerRootPrefab");

                Assert.That(prefabProperty, Is.Not.Null);
                Assert.That(prefabProperty.objectReferenceValue, Is.Not.Null);
                Assert.That(
                    AssetDatabase.GetAssetPath(prefabProperty.objectReferenceValue),
                    Is.EqualTo(PlayerRootPrefabPath),
                    "Bootstrap scene should serialize the canonical runtime player prefab so builds can instantiate it without editor-only APIs.");
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

        [Test]
        public void Initialize_CreatesSinglePersistentRootWithCanonicalRuntimePlayerPrefab()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
                var root = BootstrapWorldRoot.Initialize();

                Assert.That(root, Is.Not.Null);
                Assert.That(PersistentPlayerRoot.Instance, Is.SameAs(root));
                Assert.That(root.PlayerRootTransform, Is.Not.Null, "Bootstrap should create the canonical runtime player root.");
                Assert.That(root.PlayerRootTransform.name, Is.EqualTo("RuntimePlayerRoot"),
                    "Bootstrap should instantiate the canonical runtime player root from its serialized prefab reference.");
                if (Application.isPlaying)
                {
                    Assert.That(root.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
                    Assert.That(root.PlayerRootTransform.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
                }
                else
                {
                    Assert.That(root.gameObject.scene.IsValid(), Is.True);
                    Assert.That(root.PlayerRootTransform.gameObject.scene.IsValid(), Is.True);
                }

                var roots = Object.FindObjectsByType<PersistentPlayerRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.That(roots.Length, Is.EqualTo(1));
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

        [Test]
        public void Initialize_WhenCalledTwice_ReusesSameRootInstanceAndPlayerPrefabInstance()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
            var first = BootstrapWorldRoot.Initialize();
            var firstPlayerRoot = first.PlayerRootTransform;
            var second = BootstrapWorldRoot.Initialize();

            Assert.That(second, Is.SameAs(first));
            Assert.That(second.PlayerRootTransform, Is.SameAs(firstPlayerRoot));

            var roots = Object.FindObjectsByType<PersistentPlayerRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(roots.Length, Is.EqualTo(1));
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

        [Test]
        public void Awake_WhenDuplicateExists_DestroysDuplicateAndKeepsOriginalInstance()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
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
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        [Test]
        public void MoveRuntimePlayerRootToScene_WithCanonicalRuntimePlayerRoot_DoesNotSwapToSceneAuthoredPlayerRoot()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(BootstrapScenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);

            try
            {
                var persistentRoot = BootstrapWorldRoot.Initialize();
                var scenePlayerRoot = new GameObject("PlayerRoot");
                SceneManager.MoveGameObjectToScene(scenePlayerRoot, scene);

                var captured = MoveRuntimePlayerRootToScene(persistentRoot, scene);

                Assert.That(captured, Is.Not.Null);
                Assert.That(captured, Is.SameAs(persistentRoot.PlayerRootTransform));
                Assert.That(captured.gameObject.scene, Is.EqualTo(scene));
                Assert.That(captured, Is.Not.SameAs(scenePlayerRoot.transform),
                    "PersistentPlayerRoot should preserve the canonical runtime-owned player root instead of swapping to scene content.");
                Assert.That(persistentRoot.PlayerRootTransform, Is.SameAs(captured));
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        [Test]
        public void MoveRuntimePlayerRootToScene_WithoutCanonicalRuntimePlayerRoot_FailsClosedInsteadOfAdoptingScenePlayerRoot()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(BootstrapScenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);

            try
            {
                var persistentRoot = BootstrapWorldRoot.Initialize();
                var runtimePlayerRoot = persistentRoot.PlayerRootTransform;
                AssignPlayerRootTransform(persistentRoot, null);
                if (runtimePlayerRoot != null)
                {
                    Object.DestroyImmediate(runtimePlayerRoot.gameObject);
                }

                var scenePlayerRoot = new GameObject("PlayerRoot");
                SceneManager.MoveGameObjectToScene(scenePlayerRoot, scene);

                var captured = MoveRuntimePlayerRootToScene(persistentRoot, scene);

                Assert.That(captured, Is.Null,
                    "Bootstrap must create the canonical runtime player root. PersistentPlayerRoot should not adopt scene-authored PlayerRoot content.");
                Assert.That(persistentRoot.PlayerRootTransform, Is.Null);
                Assert.That(scenePlayerRoot, Is.Not.Null);
                Assert.That(scenePlayerRoot.scene, Is.EqualTo(scene));
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
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

        private static BootstrapWorldRoot FindBootstrapWorldRoot(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var bootstrapWorldRoot = roots[i].GetComponent<BootstrapWorldRoot>();
                if (bootstrapWorldRoot != null)
                {
                    return bootstrapWorldRoot;
                }
            }

            Assert.Fail($"Expected BootstrapWorldRoot component in scene '{scene.path}'.");
            return null;
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

        private static Transform MoveRuntimePlayerRootToScene(PersistentPlayerRoot persistentRoot, Scene scene)
        {
            var moveMethod = typeof(PersistentPlayerRoot).GetMethod("MoveRuntimePlayerRootToScene");
            Assert.That(moveMethod, Is.Not.Null, "PersistentPlayerRoot should expose a runtime-only scene move seam.");
            return moveMethod.Invoke(persistentRoot, new object[] { scene }) as Transform;
        }

        private static void AssignPlayerRootTransform(PersistentPlayerRoot persistentRoot, Transform playerRootTransform)
        {
            var serialized = new SerializedObject(persistentRoot);
            serialized.FindProperty("_playerRootTransform").objectReferenceValue = playerRootTransform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
