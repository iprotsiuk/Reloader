using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Reloader.Core.Tests.PlayMode
{
    public class WorldObjectPersistenceRuntimeBridgePlayModeTests
    {
        private const string MainTownSceneName = "MainTown";
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const float SceneSwitchTimeoutSeconds = 8f;

        [SetUp]
        public void SetUp()
        {
            WorldObjectPersistenceRuntimeBridge.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            WorldObjectPersistenceRuntimeBridge.ResetForTests();
        }

        [UnityTest]
        public IEnumerator SceneLoad_AppliesConsumedState_ForMatchingScenePathAndObjectId()
        {
            yield return LoadScene(MainTownSceneName);

            var activeScene = SceneManager.GetActiveScene();
            Assert.That(activeScene.path, Is.EqualTo(MainTownScenePath));
            var targetIdentity = CreateIdentityInScene(activeScene, "qa.persistence.consumed");

            WorldObjectPersistenceRuntimeBridge.StateStore.Upsert(activeScene.path, new WorldObjectStateRecord
            {
                ObjectId = targetIdentity.ObjectId,
                Consumed = true
            });

            WorldObjectPersistenceRuntimeBridge.EnsureInitialized();
            InvokeSceneLoaded(activeScene, LoadSceneMode.Single);

            Assert.That(targetIdentity.gameObject.activeSelf, Is.False, "Consumed objects must be inactive after scene load apply.");
        }

        [UnityTest]
        public IEnumerator SceneLoad_AppliesDestroyedAndTransformOverride_ForMatchingScenePathAndObjectId()
        {
            yield return LoadScene(MainTownSceneName);
            var activeScene = SceneManager.GetActiveScene();

            var targetIdentity = CreateIdentityInScene(activeScene, "qa.persistence.destroyed");
            var expectedPosition = new Vector3(12.5f, 1.25f, -8.75f);
            var expectedRotation = Quaternion.Euler(0f, 123f, 0f);

            WorldObjectPersistenceRuntimeBridge.StateStore.Upsert(activeScene.path, new WorldObjectStateRecord
            {
                ObjectId = targetIdentity.ObjectId,
                Destroyed = true,
                HasTransformOverride = true,
                Position = expectedPosition,
                Rotation = expectedRotation
            });

            WorldObjectPersistenceRuntimeBridge.EnsureInitialized();
            InvokeSceneLoaded(activeScene, LoadSceneMode.Single);

            Assert.That(targetIdentity.transform.position, Is.EqualTo(expectedPosition));
            Assert.That(targetIdentity.transform.rotation, Is.EqualTo(expectedRotation));
            Assert.That(targetIdentity.gameObject.activeSelf, Is.False, "Destroyed objects must be inactive after scene load apply.");
        }

        [UnityTest]
        public IEnumerator SceneLoad_DoesNotApplyRecord_WhenScenePathDoesNotMatchLoadedScene()
        {
            yield return LoadScene(MainTownSceneName);
            var activeScene = SceneManager.GetActiveScene();
            var targetIdentity = CreateIdentityInScene(activeScene, "qa.persistence.path-mismatch");

            var originalPosition = targetIdentity.transform.position;
            var originalRotation = targetIdentity.transform.rotation;

            WorldObjectPersistenceRuntimeBridge.StateStore.Upsert("Assets/_Project/World/Scenes/IndoorRangeInstance.unity", new WorldObjectStateRecord
            {
                ObjectId = targetIdentity.ObjectId,
                Consumed = true,
                HasTransformOverride = true,
                Position = originalPosition + new Vector3(3f, 2f, 1f),
                Rotation = Quaternion.Euler(0f, 30f, 0f)
            });

            WorldObjectPersistenceRuntimeBridge.EnsureInitialized();
            InvokeSceneLoaded(activeScene, LoadSceneMode.Single);

            Assert.That(targetIdentity.gameObject.activeSelf, Is.True, "State from other scene path must not deactivate this object.");
            Assert.That(targetIdentity.transform.position, Is.EqualTo(originalPosition));
            Assert.That(targetIdentity.transform.rotation, Is.EqualTo(originalRotation));
        }

        private static WorldObjectIdentity CreateIdentityInScene(Scene scene, string objectId)
        {
            var gameObject = new GameObject("QaWorldObject");
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            var identity = gameObject.AddComponent<WorldObjectIdentity>();

            var objectIdField = typeof(WorldObjectIdentity).GetField("_objectId", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(objectIdField, Is.Not.Null, "Expected private _objectId field on WorldObjectIdentity.");
            objectIdField.SetValue(identity, objectId);

            // Re-run identity reservation with deterministic test id.
            Assert.That(identity.ObjectId, Is.EqualTo(objectId));
            return identity;
        }

        private static void InvokeSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var callback = typeof(WorldObjectPersistenceRuntimeBridge).GetMethod("OnSceneLoaded", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(callback, Is.Not.Null, "Expected scene-loaded callback on runtime bridge.");
            callback.Invoke(null, new object[] { scene, mode });
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

            var elapsed = 0f;
            while (elapsed < SceneSwitchTimeoutSeconds)
            {
                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.IsValid() && activeScene.isLoaded && activeScene.name == sceneName)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.Fail($"Timed out waiting for active scene '{sceneName}'.");
        }
    }
}
