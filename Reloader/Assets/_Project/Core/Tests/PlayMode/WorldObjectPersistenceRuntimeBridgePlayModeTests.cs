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
        private const string IndoorRangeSceneName = "IndoorRangeInstance";
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

        [UnityTest]
        public IEnumerator SceneLoad_AppliesForLoadedNonActiveScene_CallbackPath()
        {
            yield return LoadScene(MainTownSceneName);
            yield return LoadSceneAdditive(IndoorRangeSceneName);

            var mainTownScene = SceneManager.GetSceneByName(MainTownSceneName);
            var indoorScene = SceneManager.GetSceneByName(IndoorRangeSceneName);
            Assert.That(mainTownScene.IsValid() && mainTownScene.isLoaded, Is.True);
            Assert.That(indoorScene.IsValid() && indoorScene.isLoaded, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MainTownSceneName));

            var targetIdentity = CreateIdentityInScene(indoorScene, "qa.persistence.non-active-scene");

            WorldObjectPersistenceRuntimeBridge.StateStore.Upsert(indoorScene.path, new WorldObjectStateRecord
            {
                ObjectId = targetIdentity.ObjectId,
                Consumed = true
            });

            WorldObjectPersistenceRuntimeBridge.EnsureInitialized();
            InvokeSceneLoaded(indoorScene, LoadSceneMode.Additive);

            Assert.That(targetIdentity.gameObject.activeSelf, Is.False, "Loaded non-active scene should still be applied.");

            yield return UnloadSceneIfLoaded(IndoorRangeSceneName);
        }

        [UnityTest]
        public IEnumerator SceneLoad_PolicyTrackTransformsFalse_DoesNotApplyTransformOverride()
        {
            yield return LoadScene(MainTownSceneName);
            var activeScene = SceneManager.GetActiveScene();
            var targetIdentity = CreateIdentityInScene(activeScene, "qa.persistence.policy.track-transforms");
            var originalPosition = targetIdentity.transform.position;
            var originalRotation = targetIdentity.transform.rotation;

            WorldObjectPersistenceRuntimeBridge.RegisterScenePolicy(new WorldScenePersistencePolicy
            {
                ScenePath = activeScene.path,
                Mode = WorldObjectPersistenceMode.Persistent,
                TrackTransforms = false,
                TrackConsumed = true,
                TrackDestroyed = true,
                TrackSpawnedObjects = true
            });

            WorldObjectPersistenceRuntimeBridge.StateStore.Upsert(activeScene.path, new WorldObjectStateRecord
            {
                ObjectId = targetIdentity.ObjectId,
                HasTransformOverride = true,
                Position = originalPosition + new Vector3(5f, 0f, 0f),
                Rotation = Quaternion.Euler(0f, 45f, 0f)
            });

            WorldObjectPersistenceRuntimeBridge.EnsureInitialized();
            InvokeSceneLoaded(activeScene, LoadSceneMode.Single);

            Assert.That(targetIdentity.transform.position, Is.EqualTo(originalPosition));
            Assert.That(targetIdentity.transform.rotation, Is.EqualTo(originalRotation));
        }

        [UnityTest]
        public IEnumerator SceneLoad_PolicyTrackConsumedFalse_DoesNotDeactivateConsumedObject()
        {
            yield return LoadScene(MainTownSceneName);
            var activeScene = SceneManager.GetActiveScene();
            var targetIdentity = CreateIdentityInScene(activeScene, "qa.persistence.policy.track-consumed");

            WorldObjectPersistenceRuntimeBridge.RegisterScenePolicy(new WorldScenePersistencePolicy
            {
                ScenePath = activeScene.path,
                Mode = WorldObjectPersistenceMode.Persistent,
                TrackTransforms = true,
                TrackConsumed = false,
                TrackDestroyed = true,
                TrackSpawnedObjects = true
            });

            WorldObjectPersistenceRuntimeBridge.StateStore.Upsert(activeScene.path, new WorldObjectStateRecord
            {
                ObjectId = targetIdentity.ObjectId,
                Consumed = true
            });

            WorldObjectPersistenceRuntimeBridge.EnsureInitialized();
            InvokeSceneLoaded(activeScene, LoadSceneMode.Single);

            Assert.That(targetIdentity.gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator SceneLoad_PolicyTrackDestroyedFalse_DoesNotDeactivateDestroyedObject()
        {
            yield return LoadScene(MainTownSceneName);
            var activeScene = SceneManager.GetActiveScene();
            var targetIdentity = CreateIdentityInScene(activeScene, "qa.persistence.policy.track-destroyed");

            WorldObjectPersistenceRuntimeBridge.RegisterScenePolicy(new WorldScenePersistencePolicy
            {
                ScenePath = activeScene.path,
                Mode = WorldObjectPersistenceMode.Persistent,
                TrackTransforms = true,
                TrackConsumed = true,
                TrackDestroyed = false,
                TrackSpawnedObjects = true
            });

            WorldObjectPersistenceRuntimeBridge.StateStore.Upsert(activeScene.path, new WorldObjectStateRecord
            {
                ObjectId = targetIdentity.ObjectId,
                Destroyed = true
            });

            WorldObjectPersistenceRuntimeBridge.EnsureInitialized();
            InvokeSceneLoaded(activeScene, LoadSceneMode.Single);

            Assert.That(targetIdentity.gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator SceneLoad_RuntimeSpawnedRecord_ConsumedTrue_TrackConsumedFalse_StillRestores()
        {
            yield return LoadScene(MainTownSceneName);
            var activeScene = SceneManager.GetActiveScene();
            var objectId = "qa.persistence.spawned.consumed-ignored";
            var expectedPosition = new Vector3(3f, 1f, -2f);
            var expectedRotation = Quaternion.Euler(0f, 15f, 0f);

            WorldObjectPersistenceRuntimeBridge.RegisterScenePolicy(new WorldScenePersistencePolicy
            {
                ScenePath = activeScene.path,
                Mode = WorldObjectPersistenceMode.Persistent,
                TrackTransforms = true,
                TrackConsumed = false,
                TrackDestroyed = true,
                TrackSpawnedObjects = true
            });

            WorldObjectPersistenceRuntimeBridge.StateStore.Upsert(activeScene.path, new WorldObjectStateRecord
            {
                ObjectId = objectId,
                Consumed = true,
                HasTransformOverride = true,
                Position = expectedPosition,
                Rotation = expectedRotation,
                ItemInstanceId = "drop:qa:consumed-ignored",
                ItemDefinitionId = "weapon-kar98k",
                StackQuantity = 1
            });

            WorldObjectPersistenceRuntimeBridge.RegisterRuntimeSpawnRestorer((scene, record) =>
            {
                var restored = CreateIdentityInScene(scene, record.ObjectId);
                restored.transform.SetPositionAndRotation(record.Position, record.Rotation);
                return true;
            });

            WorldObjectPersistenceRuntimeBridge.EnsureInitialized();
            InvokeSceneLoaded(activeScene, LoadSceneMode.Single);

            var restoredIdentity = FindIdentityInSceneByObjectId(activeScene, objectId);
            Assert.That(restoredIdentity, Is.Not.Null);
            Assert.That(restoredIdentity.transform.position, Is.EqualTo(expectedPosition));
            Assert.That(Quaternion.Angle(restoredIdentity.transform.rotation, expectedRotation), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator SceneLoad_DailyResetMode_StillAppliesObjectState()
        {
            yield return LoadScene(MainTownSceneName);
            var activeScene = SceneManager.GetActiveScene();
            var targetIdentity = CreateIdentityInScene(activeScene, "qa.persistence.policy.daily-reset");

            WorldObjectPersistenceRuntimeBridge.RegisterScenePolicy(new WorldScenePersistencePolicy
            {
                ScenePath = activeScene.path,
                Mode = WorldObjectPersistenceMode.DailyReset,
                TrackTransforms = true,
                TrackConsumed = true,
                TrackDestroyed = true,
                TrackSpawnedObjects = true
            });

            WorldObjectPersistenceRuntimeBridge.StateStore.Upsert(activeScene.path, new WorldObjectStateRecord
            {
                ObjectId = targetIdentity.ObjectId,
                Consumed = true
            });

            WorldObjectPersistenceRuntimeBridge.EnsureInitialized();
            InvokeSceneLoaded(activeScene, LoadSceneMode.Single);

            Assert.That(targetIdentity.gameObject.activeSelf, Is.False, "DailyReset policy should still apply per-scene state.");
        }

        [UnityTest]
        public IEnumerator SceneLoad_RestoresMissingRuntimeSpawnedObject_WhenTrackedSpawnRecordExists()
        {
            yield return LoadScene(MainTownSceneName);
            var activeScene = SceneManager.GetActiveScene();
            var objectId = "qa.persistence.spawned.restore";
            var expectedPosition = new Vector3(9f, 1.5f, -4f);
            var expectedRotation = Quaternion.Euler(0f, 80f, 0f);

            WorldObjectPersistenceRuntimeBridge.RegisterScenePolicy(new WorldScenePersistencePolicy
            {
                ScenePath = activeScene.path,
                Mode = WorldObjectPersistenceMode.Persistent,
                TrackTransforms = true,
                TrackConsumed = true,
                TrackDestroyed = true,
                TrackSpawnedObjects = true
            });

            WorldObjectPersistenceRuntimeBridge.StateStore.Upsert(activeScene.path, new WorldObjectStateRecord
            {
                ObjectId = objectId,
                HasTransformOverride = true,
                Position = expectedPosition,
                Rotation = expectedRotation,
                ItemInstanceId = "item.spawned.restore"
            });

            WorldObjectPersistenceRuntimeBridge.RegisterRuntimeSpawnRestorer((scene, record) =>
            {
                var restored = CreateIdentityInScene(scene, record.ObjectId);
                restored.transform.SetPositionAndRotation(record.Position, record.Rotation);
                return true;
            });

            WorldObjectPersistenceRuntimeBridge.EnsureInitialized();
            InvokeSceneLoaded(activeScene, LoadSceneMode.Single);

            var restoredIdentity = FindIdentityInSceneByObjectId(activeScene, objectId);
            Assert.That(restoredIdentity, Is.Not.Null, "Missing runtime-spawned object should be re-instantiated from saved state.");
            Assert.That(restoredIdentity.transform.position, Is.EqualTo(expectedPosition));
            Assert.That(Quaternion.Angle(restoredIdentity.transform.rotation, expectedRotation), Is.LessThan(0.001f));
            Assert.That(restoredIdentity.gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator DayBoundary_DailyResetScene_SameDayRetainsState()
        {
            yield return LoadScene(MainTownSceneName);
            var activeScene = SceneManager.GetActiveScene();
            var targetIdentity = CreateIdentityInScene(activeScene, "qa.persistence.day-boundary.same-day");

            WorldObjectPersistenceRuntimeBridge.RegisterScenePolicy(new WorldScenePersistencePolicy
            {
                ScenePath = activeScene.path,
                Mode = WorldObjectPersistenceMode.DailyReset,
                TrackTransforms = true,
                TrackConsumed = true,
                TrackDestroyed = true,
                TrackSpawnedObjects = true
            });

            WorldObjectPersistenceRuntimeBridge.StateStore.Upsert(activeScene.path, new WorldObjectStateRecord
            {
                ObjectId = targetIdentity.ObjectId,
                Consumed = true,
                LastUpdatedDay = 6,
                ItemInstanceId = "item.same-day"
            });

            var cleaned = WorldObjectPersistenceRuntimeBridge.ProcessDayBoundary(6, 6);
            Assert.That(cleaned, Is.EqualTo(0));
            Assert.That(
                WorldObjectPersistenceRuntimeBridge.StateStore.TryGet(activeScene.path, targetIdentity.ObjectId, out _),
                Is.True,
                "Same-day boundary processing must retain DailyReset state.");

            WorldObjectPersistenceRuntimeBridge.EnsureInitialized();
            InvokeSceneLoaded(activeScene, LoadSceneMode.Single);

            Assert.That(targetIdentity.gameObject.activeSelf, Is.False, "Same-day DailyReset state must still apply on scene load.");
        }

        [UnityTest]
        public IEnumerator DayBoundary_DailyResetScene_CleansState_AndMovesToReclaimStorage()
        {
            yield return LoadScene(MainTownSceneName);
            var activeScene = SceneManager.GetActiveScene();
            var targetIdentity = CreateIdentityInScene(activeScene, "qa.persistence.day-boundary.cleanup");

            WorldObjectPersistenceRuntimeBridge.RegisterScenePolicy(new WorldScenePersistencePolicy
            {
                ScenePath = activeScene.path,
                Mode = WorldObjectPersistenceMode.DailyReset,
                TrackTransforms = true,
                TrackConsumed = true,
                TrackDestroyed = true,
                TrackSpawnedObjects = true
            });

            WorldObjectPersistenceRuntimeBridge.StateStore.Upsert(activeScene.path, new WorldObjectStateRecord
            {
                ObjectId = targetIdentity.ObjectId,
                Consumed = true,
                LastUpdatedDay = 4,
                ItemInstanceId = "item.reclaim.001"
            });

            var cleaned = WorldObjectPersistenceRuntimeBridge.ProcessDayBoundary(4, 5);
            Assert.That(cleaned, Is.EqualTo(1));
            Assert.That(
                WorldObjectPersistenceRuntimeBridge.StateStore.TryGet(activeScene.path, targetIdentity.ObjectId, out _),
                Is.False,
                "DailyReset state should be removed after day changes.");

            Assert.That(
                WorldObjectPersistenceRuntimeBridge.ReclaimStorage.TryGetEntry("item.reclaim.001", out var reclaimEntry),
                Is.True,
                "Cleaned record should be moved to reclaim storage.");
            Assert.That(reclaimEntry.ScenePath, Is.EqualTo(activeScene.path));
            Assert.That(reclaimEntry.ObjectId, Is.EqualTo(targetIdentity.ObjectId));
            Assert.That(reclaimEntry.CleanedOnDay, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator DayBoundary_PersistentScene_StateIsUnaffected()
        {
            yield return LoadScene(MainTownSceneName);
            var activeScene = SceneManager.GetActiveScene();
            var targetIdentity = CreateIdentityInScene(activeScene, "qa.persistence.day-boundary.persistent");

            WorldObjectPersistenceRuntimeBridge.RegisterScenePolicy(new WorldScenePersistencePolicy
            {
                ScenePath = activeScene.path,
                Mode = WorldObjectPersistenceMode.Persistent,
                TrackTransforms = true,
                TrackConsumed = true,
                TrackDestroyed = true,
                TrackSpawnedObjects = true
            });

            WorldObjectPersistenceRuntimeBridge.StateStore.Upsert(activeScene.path, new WorldObjectStateRecord
            {
                ObjectId = targetIdentity.ObjectId,
                Consumed = true,
                LastUpdatedDay = 8,
                ItemInstanceId = "item.persistent.001"
            });

            var cleaned = WorldObjectPersistenceRuntimeBridge.ProcessDayBoundary(8, 9);
            Assert.That(cleaned, Is.EqualTo(0));
            Assert.That(WorldObjectPersistenceRuntimeBridge.StateStore.TryGet(activeScene.path, targetIdentity.ObjectId, out _), Is.True);
            Assert.That(
                WorldObjectPersistenceRuntimeBridge.ReclaimStorage.TryGetEntry("item.persistent.001", out _),
                Is.False,
                "Persistent scene records must not be moved to reclaim storage on day boundary.");
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

        private static WorldObjectIdentity FindIdentityInSceneByObjectId(Scene scene, string objectId)
        {
            var roots = scene.GetRootGameObjects();
            for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                var root = roots[rootIndex];
                if (root == null)
                {
                    continue;
                }

                var identities = root.GetComponentsInChildren<WorldObjectIdentity>(true);
                for (var identityIndex = 0; identityIndex < identities.Length; identityIndex++)
                {
                    var identity = identities[identityIndex];
                    if (identity == null)
                    {
                        continue;
                    }

                    if (identity.ObjectId == objectId)
                    {
                        return identity;
                    }
                }
            }

            return null;
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

        private static IEnumerator LoadSceneAdditive(string sceneName)
        {
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                yield break;
            }

            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);

            var elapsed = 0f;
            while (elapsed < SceneSwitchTimeoutSeconds)
            {
                var scene = SceneManager.GetSceneByName(sceneName);
                if (scene.IsValid() && scene.isLoaded)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.Fail($"Timed out waiting for additive scene '{sceneName}' to load.");
        }

        private static IEnumerator UnloadSceneIfLoaded(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                yield break;
            }

            var operation = SceneManager.UnloadSceneAsync(scene);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }
        }
    }
}
