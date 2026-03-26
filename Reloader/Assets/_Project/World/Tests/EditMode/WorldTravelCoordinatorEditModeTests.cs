using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using Reloader.World.Runtime;
using Reloader.World.Runtime.Origin;
using Reloader.World.Travel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Reloader.World.Tests.EditMode
{
    public sealed class WorldTravelCoordinatorEditModeTests
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string MainTownSceneName = "MainTown";
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string IndoorRangeScenePath = "Assets/_Project/World/Scenes/IndoorRangeInstance.unity";

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
        public void OnSceneLoaded_WhenDestinationEntryPointIsMissing_FailsClosedWithoutLeavingHalfTravelState()
        {
            var originalScene = SceneManager.GetActiveScene();
            var bootstrapScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
            var destinationScene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(bootstrapScene);

                var persistentRoot = BootstrapWorldRoot.Initialize();
                var runtimePlayerRoot = persistentRoot.PlayerRootTransform;
                Assert.That(runtimePlayerRoot, Is.Not.Null, "Expected canonical runtime player before starting travel.");

                runtimePlayerRoot.position = new Vector3(3f, 4f, 5f);
                runtimePlayerRoot.rotation = Quaternion.Euler(0f, 35f, 0f);

                SetPrivateStaticField("_pendingSceneName", MainTownSceneName);
                SetPrivateStaticField("_pendingEntryPointId", "entry.missing");
                InvokePrivateStatic("OnSceneLoaded", destinationScene, LoadSceneMode.Additive);

                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(bootstrapScene),
                    "Missing destination entry point should leave the origin scene active.");
                Assert.That(destinationScene.isLoaded, Is.False,
                    "Destination scene should be unloaded again when entry validation fails.");
                Assert.That(persistentRoot.PlayerRootTransform, Is.SameAs(runtimePlayerRoot));
                Assert.That(runtimePlayerRoot.gameObject.scene, Is.EqualTo(bootstrapScene),
                    "Canonical runtime player should remain with the origin scene when travel fails closed.");
                Assert.That(runtimePlayerRoot.position, Is.EqualTo(new Vector3(3f, 4f, 5f)));
                Assert.That(WorldTravelCoordinator.LastResolvedEntryPointId, Is.Null);
                Assert.That(GetPrivateStaticField<string>("_pendingSceneName"), Is.Null);
                Assert.That(GetPrivateStaticField<string>("_pendingEntryPointId"), Is.Null);
                Assert.That(GetPendingInventorySnapshotCount(), Is.EqualTo(0));
                Assert.That(GetPendingWeaponSnapshotCount(), Is.EqualTo(0));
                Assert.That(GetPrivateStaticField<object>("_pendingTravelPopulationModule"), Is.Null);
            }
            finally
            {
                CloseSceneIfLoaded(destinationScene);
                EditorSceneManager.CloseScene(bootstrapScene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        [Test]
        public void OnSceneLoaded_WhenCanonicalTravelPlayerRootCannotBeResolved_FailsClosedWithoutPublishingResolvedEntry()
        {
            var originalScene = SceneManager.GetActiveScene();
            var bootstrapScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
            var destinationScene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(bootstrapScene);

                var persistentRoot = BootstrapWorldRoot.Initialize();
                var runtimePlayerRoot = persistentRoot.PlayerRootTransform;
                Assert.That(runtimePlayerRoot, Is.Not.Null, "Expected canonical runtime player before starting travel.");

                runtimePlayerRoot.position = new Vector3(11f, 12f, 13f);
                runtimePlayerRoot.rotation = Quaternion.Euler(0f, 70f, 0f);
                AssignPlayerRootTransform(persistentRoot, null);

                SetPrivateStaticField("_pendingSceneName", MainTownSceneName);
                SetPrivateStaticField("_pendingEntryPointId", "entry.maintown.spawn");
                LogAssert.Expect(LogType.Error,
                    "Travel failed: canonical runtime player root could not be repositioned to entry 'entry.maintown.spawn' in scene 'MainTown'.");
                InvokePrivateStatic("OnSceneLoaded", destinationScene, LoadSceneMode.Additive);

                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(bootstrapScene),
                    "Travel should fail closed when the canonical runtime player root cannot be resolved for destination handoff.");
                Assert.That(destinationScene.isLoaded, Is.False,
                    "Destination scene should be unloaded again when player-root handoff cannot complete.");
                Assert.That(runtimePlayerRoot.gameObject.scene, Is.EqualTo(bootstrapScene),
                    "Existing runtime player object should remain in the origin scene when destination handoff fails.");
                Assert.That(runtimePlayerRoot.position, Is.EqualTo(new Vector3(11f, 12f, 13f)));
                Assert.That(WorldTravelCoordinator.LastResolvedEntryPointId, Is.Null,
                    "Travel should not publish a resolved entry point when the canonical runtime player was never repositioned.");
                Assert.That(GetPrivateStaticField<string>("_pendingSceneName"), Is.Null);
                Assert.That(GetPrivateStaticField<string>("_pendingEntryPointId"), Is.Null);
            }
            finally
            {
                CloseSceneIfLoaded(destinationScene);
                CloseSceneIfLoaded(bootstrapScene);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
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
                SceneManager.SetActiveScene(scene);

                var persistentRoot = BootstrapWorldRoot.Initialize();
                var runtimePlayerRoot = persistentRoot.PlayerRootTransform;
                Assert.That(runtimePlayerRoot, Is.Not.Null, "Expected bootstrap to create the runtime-owned player root before travel.");

                var scenePlayerRoot = new GameObject("PlayerRoot");
                SceneManager.MoveGameObjectToScene(scenePlayerRoot, scene);

                var prepared = InvokePrivateStatic<bool>("PreparePersistentPlayerRootForTravel");

                Assert.That(prepared, Is.True, "Travel preparation should move the canonical runtime player into the active origin scene.");
                Assert.That(persistentRoot.PlayerRootTransform, Is.SameAs(runtimePlayerRoot));
                Assert.That(runtimePlayerRoot.gameObject.scene, Is.EqualTo(scene));
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

        [Test]
        public void TryLoadSceneAtEntry_WhileAnotherTravelIsPending_FailsClosedAndPreservesFirstRequest()
        {
            var originalScene = SceneManager.GetActiveScene();
            var bootstrapScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(bootstrapScene);

                var persistentRoot = BootstrapWorldRoot.Initialize();
                Assert.That(persistentRoot.PlayerRootTransform, Is.Not.Null,
                    "Expected canonical runtime player before starting travel.");

                SetPrivateStaticField("_pendingSceneName", MainTownScenePath);
                SetPrivateStaticField("_pendingEntryPointId", "entry.maintown.spawn");

                var started = InvokePrivateStatic<bool>(
                    "TryLoadSceneAtEntry",
                    IndoorRangeScenePath,
                    "entry.indoor.arrival");

                Assert.That(started, Is.False, "Overlapping travel requests should be rejected while the first one is still pending.");
                Assert.That(GetPrivateStaticField<string>("_pendingSceneName"), Is.EqualTo(MainTownScenePath));
                Assert.That(GetPrivateStaticField<string>("_pendingEntryPointId"), Is.EqualTo("entry.maintown.spawn"));
                Assert.That(SceneManager.GetSceneByPath(IndoorRangeScenePath).isLoaded, Is.False,
                    "Rejected overlapping travel request should not load a replacement scene.");
            }
            finally
            {
                CloseSceneIfLoaded(SceneManager.GetSceneByPath(IndoorRangeScenePath));
                CloseSceneIfLoaded(bootstrapScene);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }

                SetPrivateStaticField<string>("_pendingSceneName", null);
                SetPrivateStaticField<string>("_pendingEntryPointId", null);
            }
        }

        [Test]
        public void PreparePersistentPlayerRootForTravel_PreservesActiveStableLocalMapping()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(scene);

                var persistentRoot = BootstrapWorldRoot.Initialize();
                var state = persistentRoot.GetComponent(GetOriginType("DynamicOriginRebaseState"));

                Assert.That(state, Is.Not.Null, "Bootstrap initialization should provision the canonical floating-origin state on the persistent runtime owner.");

                Invoke(state, "ApplyRebase", new Vector3(-420f, 0f, 160f), new Vector3(420f, 0f, -160f), 14f);
                var stableOffsetBefore = GetVector3Property(state, "StableOriginOffset");
                var localOffsetBefore = GetVector3Property(state, "LocalOriginOffset");

                var prepared = InvokePrivateStatic<bool>("PreparePersistentPlayerRootForTravel");
                var stateAfter = persistentRoot.GetComponent(GetOriginType("DynamicOriginRebaseState"));

                Assert.That(prepared, Is.True);
                Assert.That(stateAfter, Is.SameAs(state));
                Assert.That(GetVector3Property(stateAfter, "StableOriginOffset"), Is.EqualTo(stableOffsetBefore));
                Assert.That(GetVector3Property(stateAfter, "LocalOriginOffset"), Is.EqualTo(localOffsetBefore));
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
        public void OnSceneLoaded_WhenTravelSucceeds_PreservesPersistentOriginSeamsAndStableLocalMapping()
        {
            var originalScene = SceneManager.GetActiveScene();
            var bootstrapScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
            var destinationScene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(bootstrapScene);

                var persistentRoot = BootstrapWorldRoot.Initialize();
                var stateType = GetOriginType("DynamicOriginRebaseState");
                var bridgeType = GetOriginType("StableWorldCoordinateBridge");
                var controllerType = GetOriginType("DynamicOriginRebaseController");

                var state = persistentRoot.GetComponent(stateType);
                Assert.That(state, Is.Not.Null, "Expected the canonical floating-origin state on the persistent runtime owner before travel.");

                Invoke(state, "ApplyRebase", new Vector3(-420f, 0f, 160f), new Vector3(420f, 0f, -160f), 14f);
                var stableOffsetBefore = GetVector3Property(state, "StableOriginOffset");
                var localOffsetBefore = GetVector3Property(state, "LocalOriginOffset");

                SetPrivateStaticField("_pendingSceneName", MainTownSceneName);
                SetPrivateStaticField("_pendingEntryPointId", "entry.maintown.spawn");
                InvokePrivateStatic("OnSceneLoaded", destinationScene, LoadSceneMode.Additive);

                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(destinationScene));
                Assert.That(bootstrapScene.isLoaded, Is.False, "Successful travel should unload the previous world scene.");
                Assert.That(persistentRoot.GetComponent(stateType), Is.SameAs(state));
                Assert.That(persistentRoot.GetComponent(bridgeType), Is.Not.Null);
                Assert.That(persistentRoot.GetComponent(controllerType), Is.Not.Null);
                Assert.That(GetVector3Property(state, "StableOriginOffset"), Is.EqualTo(stableOffsetBefore));
                Assert.That(GetVector3Property(state, "LocalOriginOffset"), Is.EqualTo(localOffsetBefore));
            }
            finally
            {
                CloseSceneIfLoaded(destinationScene);
                CloseSceneIfLoaded(bootstrapScene);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        [Test]
        public void DisableActiveEventSystemsForTravel_TemporarilyDisablesExistingEventSystems_AndRestoreReenablesThem()
        {
            var first = new GameObject("FirstEventSystem");
            var second = new GameObject("SecondEventSystem");
            first.AddComponent<EventSystem>();
            second.AddComponent<EventSystem>();

            try
            {
                InvokePrivateStatic("DisableActiveEventSystemsForTravel");

                Assert.That(first.activeSelf, Is.False, "Travel should temporarily disable the currently active origin-scene EventSystem before additive scene load.");
                Assert.That(second.activeSelf, Is.False, "Travel should disable every active EventSystem root to avoid duplicate-active warnings during additive load.");

                InvokePrivateStatic("RestoreTemporarilyDisabledEventSystems");

                Assert.That(first.activeSelf, Is.True, "Failed or cancelled travel should restore the previous EventSystem root.");
                Assert.That(second.activeSelf, Is.True, "Failed or cancelled travel should restore all previously active EventSystem roots.");
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void RearmActiveEventSystemsAfterTravel_RestoresDisabledEventSystems_WhenDestinationHasNoActiveEventSystem()
        {
            var originEventSystem = new GameObject("OriginEventSystem");
            originEventSystem.AddComponent<EventSystem>();
            var destinationScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            try
            {
                InvokePrivateStatic("DisableActiveEventSystemsForTravel");

                Assert.That(originEventSystem.activeSelf, Is.False,
                    "Travel should temporarily disable the origin-scene EventSystem before additive scene load.");

                InvokePrivateStatic("RearmActiveEventSystemsAfterTravel", destinationScene);

                Assert.That(originEventSystem.activeSelf, Is.True,
                    "When the destination scene has no active EventSystem, travel should restore the previously disabled origin EventSystem instead of leaving it disabled.");
            }
            finally
            {
                InvokePrivateStatic("RestoreTemporarilyDisabledEventSystems");
                Object.DestroyImmediate(originEventSystem);
                if (destinationScene.IsValid())
                {
                    EditorSceneManager.CloseScene(destinationScene, true);
                }
            }
        }

        [Test]
        public void RepositionPlayerToEntryPoint_RefreshesOriginBaselineBeforeNextRebaseCheck()
        {
            var originalScene = SceneManager.GetActiveScene();
            var bootstrapScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
            var destinationScene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(bootstrapScene);

                var persistentRoot = BootstrapWorldRoot.Initialize();
                var playerRoot = persistentRoot.PlayerRootTransform;
                var rebaseController = persistentRoot.GetComponent<DynamicOriginRebaseController>();

                Assert.That(playerRoot, Is.Not.Null, "Expected canonical runtime player before repositioning to a travel entry.");
                Assert.That(rebaseController, Is.Not.Null, "Expected canonical DynamicOriginRebaseController on the persistent runtime owner.");

                var entryPoint = Object.FindFirstObjectByType<SceneEntryPoint>(FindObjectsInactive.Include);
                Assert.That(entryPoint, Is.Not.Null, "Expected at least one authored SceneEntryPoint in MainTown.");

                if (entryPoint != null && entryPoint.EntryPointId != "entry.maintown.return")
                {
                    var entryPoints = Object.FindObjectsByType<SceneEntryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    for (var i = 0; i < entryPoints.Length; i++)
                    {
                        if (entryPoints[i] != null && entryPoints[i].gameObject.scene == destinationScene && entryPoints[i].EntryPointId == "entry.maintown.return")
                        {
                            entryPoint = entryPoints[i];
                            break;
                        }
                    }
                }

                Assert.That(entryPoint, Is.Not.Null, "Expected the authored MainTown return entry point.");
                Assert.That(entryPoint!.gameObject.scene, Is.EqualTo(destinationScene));

                var resolvedPlayerRoot = persistentRoot.MoveRuntimePlayerRootToScene(destinationScene);
                Assert.That(resolvedPlayerRoot, Is.SameAs(playerRoot), "Expected canonical runtime player root to move into the destination scene before reposition.");

                InvokePrivateStatic("RepositionPlayerToEntryPoint", resolvedPlayerRoot, entryPoint.transform);

                Assert.That(playerRoot.gameObject.scene, Is.EqualTo(destinationScene));
                Assert.That(playerRoot.position, Is.EqualTo(entryPoint.transform.position));

                var rebased = rebaseController!.TryRebaseIfNeeded(10f);

                Assert.That(rebased, Is.False,
                    "Expected travel reposition to refresh the floating-origin baseline so the next rebase evaluation does not pull the player back toward bootstrap-space.");
                Assert.That(playerRoot.position, Is.EqualTo(entryPoint.transform.position));
            }
            finally
            {
                CloseSceneIfLoaded(destinationScene);
                CloseSceneIfLoaded(bootstrapScene);
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
            var method = typeof(WorldTravelCoordinator).GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"Expected WorldTravelCoordinator.{methodName} to exist.");
            method.Invoke(null, parameters);
        }

        private static T InvokePrivateStatic<T>(string methodName, params object[] parameters)
        {
            var method = typeof(WorldTravelCoordinator).GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"Expected WorldTravelCoordinator.{methodName} to exist.");
            return (T)method.Invoke(null, parameters);
        }

        private static T GetPrivateStaticField<T>(string fieldName)
        {
            var field = typeof(WorldTravelCoordinator).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"Expected WorldTravelCoordinator.{fieldName} field to exist.");
            return (T)field.GetValue(null);
        }

        private static void SetPrivateStaticField<T>(string fieldName, T value)
        {
            var field = typeof(WorldTravelCoordinator).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"Expected WorldTravelCoordinator.{fieldName} field to exist.");
            field.SetValue(null, value);
        }

        private static int GetPendingInventorySnapshotCount()
        {
            var snapshots = GetPrivateStaticField<Dictionary<string, int>>("_pendingInventoryQuantities");
            return snapshots.Count;
        }

        private static int GetPendingWeaponSnapshotCount()
        {
            var field = typeof(WorldTravelCoordinator).GetField("_pendingWeaponSnapshots", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null);
            var value = field.GetValue(null) as System.Collections.ICollection;
            Assert.That(value, Is.Not.Null);
            return value.Count;
        }

        private static void CloseSceneIfLoaded(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static System.Type GetOriginType(string typeName)
        {
            var resolvedType = System.Type.GetType($"Reloader.World.Runtime.Origin.{typeName}, Reloader.World");
            Assert.That(resolvedType, Is.Not.Null, $"Expected origin runtime type '{typeName}' in assembly 'Reloader.World'.");
            return resolvedType;
        }

        private static void Invoke(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected {target.GetType().Name}.{methodName} to exist.");
            method.Invoke(target, args);
        }

        private static Vector3 GetVector3Property(object target, string propertyName)
        {
            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, $"Expected {target.GetType().Name}.{propertyName} property to exist.");
            return (Vector3)property.GetValue(target);
        }
    }
}
