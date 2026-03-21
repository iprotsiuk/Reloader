using System.Reflection;
using NUnit.Framework;
using Reloader.World.Runtime;
using Reloader.World.Travel;
using UnityEditor.SceneManagement;
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
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
                var persistentRoot = BootstrapWorldRoot.Initialize();
                var runtimePlayerRoot = persistentRoot.PlayerRootTransform;
                Assert.That(runtimePlayerRoot, Is.Not.Null, "Expected bootstrap to create the runtime-owned player root before travel.");

                var scenePlayerRoot = new GameObject("PlayerRoot");

                InvokePrivateStatic("PreparePersistentPlayerRootForTravel");

                Assert.That(persistentRoot.PlayerRootTransform, Is.SameAs(runtimePlayerRoot));
                Assert.That(scenePlayerRoot, Is.Not.Null, "Travel preparation should not destroy or adopt the scene-authored player root.");
                if (scenePlayerRoot != null)
                {
                    Object.DestroyImmediate(scenePlayerRoot);
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
        public void PreparePersistentPlayerRootForTravel_WithoutCanonicalRuntimePlayer_FailsClosedWithoutRecreatingPlayer()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
                var persistentRoot = BootstrapWorldRoot.Initialize();
                var runtimePlayerRoot = persistentRoot.PlayerRootTransform;
                AssignPlayerRootTransform(persistentRoot, null);
                if (runtimePlayerRoot != null)
                {
                    Object.DestroyImmediate(runtimePlayerRoot.gameObject);
                }

                var prepared = InvokePrivateStatic<bool>("PreparePersistentPlayerRootForTravel");

                Assert.That(prepared, Is.False,
                    "Travel should fail closed when the canonical runtime player is missing instead of recreating a fresh default player mid-session.");
                Assert.That(persistentRoot.PlayerRootTransform, Is.Null);
                Assert.That(Object.FindObjectsByType<PersistentPlayerRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(GameObject.Find("RuntimePlayerRoot"), Is.Null);
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

        private static T InvokePrivateStatic<T>(string methodName, params object[] parameters)
        {
            var method = typeof(WorldTravelCoordinator).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"Expected WorldTravelCoordinator.{methodName} to exist.");
            return (T)method.Invoke(null, parameters);
        }
    }
}
