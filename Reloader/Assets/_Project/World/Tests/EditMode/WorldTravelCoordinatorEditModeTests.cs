using System.Reflection;
using NUnit.Framework;
using Reloader.World.Runtime;
using Reloader.World.Travel;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Tests.EditMode
{
    public sealed class WorldTravelCoordinatorEditModeTests
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

        [TearDown]
        public void TearDown()
        {
            if (PersistentPlayerRoot.Instance != null && PersistentPlayerRoot.Instance.PlayerRootTransform != null)
            {
                Object.DestroyImmediate(PersistentPlayerRoot.Instance.PlayerRootTransform.gameObject);
            }

            var persistentRoots = Object.FindObjectsByType<PersistentPlayerRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < persistentRoots.Length; i++)
            {
                if (persistentRoots[i] != null)
                {
                    Object.DestroyImmediate(persistentRoots[i].gameObject);
                }
            }
        }

        [Test]
        public void PreparePersistentPlayerRootForTravel_DoesNotAdoptSceneAuthoredOriginPlayerRoot()
        {
            var persistentRoot = BootstrapWorldRoot.Initialize();
            var runtimePlayerRoot = persistentRoot.PlayerRootTransform;
            Assert.That(runtimePlayerRoot, Is.Not.Null, "Expected bootstrap to create the runtime-owned player root before travel.");

            var scenePlayerRoot = new GameObject("PlayerRoot");

            try
            {
                InvokePrivateStatic("PreparePersistentPlayerRootForTravel");

                Assert.That(persistentRoot.PlayerRootTransform, Is.SameAs(runtimePlayerRoot));
                Assert.That(scenePlayerRoot, Is.Not.Null, "Travel preparation should not destroy or adopt the scene-authored player root.");
            }
            finally
            {
                if (scenePlayerRoot != null)
                {
                    Object.DestroyImmediate(scenePlayerRoot);
                }
            }
        }

        [Test]
        public void RepositionPlayerToEntryPoint_WithoutCanonicalRuntimePlayerRoot_FailsClosedAndLeavesScenePlayerRootUntouched()
        {
            var persistentRoot = BootstrapWorldRoot.Initialize();
            var runtimePlayerRoot = persistentRoot.PlayerRootTransform;
            AssignPlayerRootTransform(persistentRoot, null);
            if (runtimePlayerRoot != null)
            {
                Object.DestroyImmediate(runtimePlayerRoot.gameObject);
            }

            var originalScene = SceneManager.GetActiveScene();
            var destinationScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(BootstrapScenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);

            try
            {
                var scenePlayerRoot = new GameObject("PlayerRoot");
                scenePlayerRoot.transform.position = new Vector3(4f, 5f, 6f);
                SceneManager.MoveGameObjectToScene(scenePlayerRoot, destinationScene);

                var entryPoint = new GameObject("EntryPoint");
                entryPoint.transform.position = new Vector3(11f, 12f, 13f);
                SceneManager.MoveGameObjectToScene(entryPoint, destinationScene);

                InvokePrivateStatic("RepositionPlayerToEntryPoint", destinationScene, entryPoint.transform);

                Assert.That(persistentRoot.PlayerRootTransform, Is.Null);
                Assert.That(scenePlayerRoot.transform.position, Is.EqualTo(new Vector3(4f, 5f, 6f)),
                    "Travel should fail closed when the canonical runtime player does not exist instead of repositioning a scene-authored replacement.");
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(destinationScene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        private static void AssignPlayerRootTransform(PersistentPlayerRoot persistentRoot, Transform playerRootTransform)
        {
            var serialized = new UnityEditor.SerializedObject(persistentRoot);
            serialized.FindProperty("_playerRootTransform").objectReferenceValue = playerRootTransform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InvokePrivateStatic(string methodName, params object[] parameters)
        {
            var method = typeof(WorldTravelCoordinator).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"Expected WorldTravelCoordinator.{methodName} to exist.");
            method.Invoke(null, parameters);
        }
    }
}
