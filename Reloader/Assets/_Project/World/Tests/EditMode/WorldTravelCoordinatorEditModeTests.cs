using System;
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
using Object = UnityEngine.Object;

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
        public void ClearTransientTravelSessionState_ClearsCachedPopulationSnapshotsPendingSnapshotAndResolvedEntry()
        {
            var cache = GetPrivateStaticField<Dictionary<string, object>>("_travelPopulationModulesByScene");
            cache["Assets/_Project/World/Scenes/MainTown.unity"] = new object();
            SetPrivateStaticField("_pendingTravelPopulationModule", new object());
            SetPrivateStaticField("<LastResolvedEntryPointId>k__BackingField", "entry.maintown.spawn");

            WorldTravelCoordinator.ClearTransientTravelSessionState();

            Assert.That(cache, Is.Empty);
            Assert.That(GetPrivateStaticField<object>("_pendingTravelPopulationModule"), Is.Null);
            Assert.That(WorldTravelCoordinator.LastResolvedEntryPointId, Is.Null);
        }

        [Test]
        public void GetTravelPopulationSceneKey_WhenSceneHasNoAssetPath_ReturnsNull()
        {
            var originalScene = SceneManager.GetActiveScene();
            var transientScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            try
            {
                Assert.That(transientScene.IsValid(), Is.True);
                Assert.That(transientScene.path, Is.Empty);

                var sceneKey = InvokePrivateStatic<string>("GetTravelPopulationSceneKey", transientScene);

                Assert.That(sceneKey, Is.Null);
            }
            finally
            {
                CloseSceneIfLoaded(transientScene);
                if (originalScene.IsValid() && originalScene.isLoaded)
                {
                    SceneManager.SetActiveScene(originalScene);
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
                Assert.That(GetPrivateStaticField<bool>("_pendingTravelSuppressesCarriedInventoryReplay"), Is.False);
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
                Assert.That(GetPrivateStaticField<bool>("_pendingTravelSuppressesCarriedInventoryReplay"), Is.False);
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
        public void OnSceneLoaded_WhenPendingAnchorTargetsStandaloneRespawnAnchor_RepositionsPlayerAndPublishesRespawnAnchorId()
        {
            var originalScene = SceneManager.GetActiveScene();
            var bootstrapScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
            var destinationScene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(bootstrapScene);

                var persistentRoot = BootstrapWorldRoot.Initialize();
                var playerRoot = persistentRoot.PlayerRootTransform;
                Assert.That(playerRoot, Is.Not.Null, "Expected canonical runtime player before resolving a standalone respawn anchor.");

                SetPrivateStaticField("_pendingSceneName", MainTownSceneName);
                SetPrivateStaticField("_pendingEntryPointId", "entry.maintown.respawn.police");
                InvokePrivateStatic("OnSceneLoaded", destinationScene, LoadSceneMode.Additive);

                var respawnAnchor = FindSpawnAnchor(destinationScene, "entry.maintown.respawn.police");
                Assert.That(respawnAnchor, Is.Not.Null);
                Assert.That(playerRoot!.gameObject.scene, Is.EqualTo(destinationScene));
                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(destinationScene));
                Assert.That(WorldTravelCoordinator.LastResolvedEntryPointId, Is.EqualTo("entry.maintown.respawn.police"));
                Assert.That(playerRoot.position, Is.EqualTo(respawnAnchor!.transform.position));
                Assert.That(playerRoot.rotation, Is.EqualTo(respawnAnchor.transform.rotation));
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
        public void FinalizePlayerTravelHandoff_WhenEntryStartsFarFromOrigin_PreparesOneCanonicalRebaseBackIntoLocalOriginWindow()
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
                InvokePrivateStatic("FinalizePlayerTravelHandoff", resolvedPlayerRoot, entryPoint.transform, false);

                Assert.That(playerRoot.gameObject.scene, Is.EqualTo(destinationScene));

                var firstRebase = rebaseController!.TryRebaseIfNeeded(10f);
                Assert.That(firstRebase, Is.True,
                    "Expected far MainTown travel handoff to prepare one canonical rebase back into the local-origin window when the controller next evaluates distance.");
                Assert.That(
                    new Vector2(playerRoot.position.x, playerRoot.position.z).magnitude,
                    Is.LessThan(rebaseController.RebaseDistanceMeters),
                    "Expected canonical rebase evaluation to normalize far MainTown entries back into the local-origin window for runtime precision.");
                var secondRebase = rebaseController.TryRebaseIfNeeded(11f);
                Assert.That(secondRebase, Is.False,
                    "Expected a second immediate canonical rebase evaluation to stay idle once the player has been returned to the local-origin window.");
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

        [Test]
        public void TryMoveRuntimePlayerToLoadedEntryPoint_WhenRespawnAnchorExists_RepositionsPlayerAndPublishesResolvedAnchor()
        {
            var originalScene = SceneManager.GetActiveScene();
            var bootstrapScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
            var destinationScene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(bootstrapScene);

                var persistentRoot = BootstrapWorldRoot.Initialize();
                var playerRoot = persistentRoot.PlayerRootTransform;
                Assert.That(playerRoot, Is.Not.Null, "Expected canonical runtime player before moving to a loaded respawn anchor.");

                playerRoot.position = new Vector3(9f, 1f, -7f);
                playerRoot.rotation = Quaternion.Euler(0f, 15f, 0f);

                var moved = WorldTravelCoordinator.TryMoveRuntimePlayerToLoadedEntryPoint(
                    MainTownScenePath,
                    "entry.maintown.respawn.hospital");

                Assert.That(moved, Is.True);
                Assert.That(playerRoot.gameObject.scene, Is.EqualTo(destinationScene));
                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(destinationScene));
                Assert.That(WorldTravelCoordinator.LastResolvedEntryPointId, Is.EqualTo("entry.maintown.respawn.hospital"));

                var respawnAnchor = FindSpawnAnchor(destinationScene, "entry.maintown.respawn.hospital");
                Assert.That(respawnAnchor, Is.Not.Null);
                Assert.That(playerRoot.position, Is.EqualTo(respawnAnchor!.transform.position));
                Assert.That(playerRoot.rotation, Is.EqualTo(respawnAnchor.transform.rotation));
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
        public void TryMoveRuntimePlayerToLoadedEntryPoint_WhenInventoryReplayIsSuppressed_DoesNotRestorePendingCarriedItems()
        {
            var originalScene = SceneManager.GetActiveScene();
            var bootstrapScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
            var destinationScene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(bootstrapScene);

                var persistentRoot = BootstrapWorldRoot.Initialize();
                var playerRoot = persistentRoot.PlayerRootTransform;
                Assert.That(playerRoot, Is.Not.Null, "Expected canonical runtime player before moving to a loaded respawn anchor.");

                var inventoryControllerType = System.Type.GetType("Reloader.Inventory.PlayerInventoryController, Reloader.Inventory");
                Assert.That(inventoryControllerType, Is.Not.Null, "Expected PlayerInventoryController type to resolve.");
                var inventoryController = playerRoot.gameObject.AddComponent(inventoryControllerType);
                Invoke(inventoryController, "Configure", null, null, null, null);
                var inventoryRuntime = GetInventoryRuntime(inventoryController);
                Assert.That(inventoryRuntime, Is.Not.Null, "Expected inventory runtime probe on the canonical runtime player.");

                Invoke(inventoryRuntime, "ClearCarriedItems");
                Invoke(inventoryRuntime, "SelectBeltSlot", 1);

                SeedPendingInventoryReplay("qa.travel.confiscated.item", quantity: 1, selectedBeltIndex: 4);

                var moved = WorldTravelCoordinator.TryMoveRuntimePlayerToLoadedEntryPoint(
                    MainTownScenePath,
                    "entry.maintown.respawn.hospital",
                    suppressCarriedInventoryReplay: true);

                Assert.That(moved, Is.True);
                var postTravelRuntime = GetInventoryRuntime(inventoryController);
                Assert.That(GetItemQuantity(postTravelRuntime, "qa.travel.confiscated.item"), Is.EqualTo(0));
                Assert.That(GetSelectedBeltIndex(postTravelRuntime), Is.EqualTo(1));
                Assert.That(GetPendingInventorySnapshotCount(), Is.EqualTo(0));
                Assert.That(GetPrivateStaticField<int>("_pendingSelectedBeltIndex"), Is.EqualTo(-1));
                Assert.That(WorldTravelCoordinator.LastResolvedEntryPointId, Is.EqualTo("entry.maintown.respawn.hospital"));
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
        public void TryLoadSceneAtEntry_WhenRecoverySuppressesCarriedInventoryReplay_DoesNotReapplyCarriedSnapshotBeforeRecoveryClear()
        {
            var originalScene = SceneManager.GetActiveScene();
            var bootstrapScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(bootstrapScene);

                var persistentRoot = BootstrapWorldRoot.Initialize();
                var playerRoot = persistentRoot.PlayerRootTransform;
                Assert.That(playerRoot, Is.Not.Null, "Expected canonical runtime player before starting recovery travel.");

                var runtime = InstallInventoryReplayProbe(playerRoot!, "qa.travel.recovery.item");

                var applied = WorldTravelCoordinator.TryLoadSceneAtEntry(
                    MainTownScenePath,
                    "entry.maintown.respawn.police",
                    suppressCarriedInventoryReplay: true);

                Assert.That(applied, Is.True);
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MainTownSceneName));
                Assert.That(runtime.GetItemQuantityCallCount, Is.EqualTo(1),
                    "Recovery scene load should capture the carried snapshot once and skip replay reads.");
                Assert.That(runtime.TryAddStackItemCallCount, Is.EqualTo(0),
                    "Recovery scene load should skip carried inventory replay.");
                Assert.That(runtime.TryStoreItemCallCount, Is.EqualTo(0),
                    "Recovery scene load should skip carried inventory replay.");
                Assert.That(runtime.SelectBeltSlotCallCount, Is.EqualTo(0),
                    "Recovery scene load should not reapply the carried belt selection.");
                runtime.ClearCarriedItems();
                Assert.That(runtime.ClearCarriedItemsCallCount, Is.EqualTo(1),
                    "Recovery flow should still clear carried items after travel completes.");
                Assert.That(runtime.SelectedBeltIndex, Is.EqualTo(-1));
                Assert.That(runtime.GetItemQuantity("qa.travel.recovery.item"), Is.EqualTo(0));
                Assert.That(WorldTravelCoordinator.LastResolvedEntryPointId, Is.EqualTo("entry.maintown.respawn.police"));
            }
            finally
            {
                if (originalScene.IsValid() && originalScene.isLoaded)
                {
                    SceneManager.SetActiveScene(originalScene);
                }

                EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
                CloseSceneIfLoaded(SceneManager.GetSceneByName(MainTownSceneName));

                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        [Test]
        public void TryLoadSceneAtEntry_WhenInventoryReplayIsEnabled_RestoresCapturedCarriedInventoryOnSceneLoad()
        {
            var originalScene = SceneManager.GetActiveScene();
            var bootstrapScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(bootstrapScene);

                var persistentRoot = BootstrapWorldRoot.Initialize();
                var playerRoot = persistentRoot.PlayerRootTransform;
                Assert.That(playerRoot, Is.Not.Null, "Expected canonical runtime player before starting travel.");

                var runtime = InstallInventoryReplayProbe(playerRoot!, "qa.travel.normal.item");

                var started = WorldTravelCoordinator.TryLoadSceneAtEntry(
                    MainTownSceneName,
                    "entry.maintown.spawn",
                    suppressCarriedInventoryReplay: false);

                Assert.That(started, Is.True);
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MainTownSceneName));
                Assert.That(runtime.GetItemQuantityCallCount, Is.EqualTo(2),
                    "Normal scene travel should read carried inventory during capture and replay.");
                Assert.That(runtime.TryAddStackItemCallCount, Is.EqualTo(1),
                    "Normal scene travel should restore the carried stack snapshot.");
                Assert.That(runtime.TryStoreItemCallCount, Is.EqualTo(0));
                Assert.That(runtime.SelectBeltSlotCallCount, Is.EqualTo(1),
                    "Normal scene travel should restore the carried belt selection.");
                Assert.That(runtime.ClearCarriedItemsCallCount, Is.EqualTo(0));
                Assert.That(runtime.SelectedBeltIndex, Is.EqualTo(0));
                Assert.That(runtime.GetItemQuantity("qa.travel.normal.item"), Is.EqualTo(1));
                Assert.That(WorldTravelCoordinator.LastResolvedEntryPointId, Is.EqualTo("entry.maintown.spawn"));
            }
            finally
            {
                if (originalScene.IsValid() && originalScene.isLoaded)
                {
                    SceneManager.SetActiveScene(originalScene);
                }

                EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
                CloseSceneIfLoaded(SceneManager.GetSceneByName(MainTownSceneName));

                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        [Test]
        public void TryLoadSceneAtEntry_WhenFinalizeThrows_ClearsPendingStateAndReplaySuppression()
        {
            var originalScene = SceneManager.GetActiveScene();
            var bootstrapScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(bootstrapScene);

                var persistentRoot = BootstrapWorldRoot.Initialize();
                var playerRoot = persistentRoot.PlayerRootTransform;
                Assert.That(playerRoot, Is.Not.Null, "Expected canonical runtime player before starting travel.");

                var runtime = InstallInventoryReplayProbe(playerRoot!, "qa.travel.failure.item");
                var previousFinalizeHook = GetPrivateStaticField<Action>("_finalizePlayerTravelHandoffHookForTests");
                SetPrivateStaticField(
                    "_finalizePlayerTravelHandoffHookForTests",
                    (Action)(() => throw new InvalidOperationException("Intentional finalize failure for travel cleanup regression coverage.")));

                try
                {
                    var started = WorldTravelCoordinator.TryLoadSceneAtEntry(
                        MainTownScenePath,
                        "entry.maintown.spawn",
                        suppressCarriedInventoryReplay: true);

                    Assert.That(started, Is.False, "Travel should fail when finalization throws.");
                    Assert.That(SceneManager.GetSceneByPath(MainTownScenePath).isLoaded, Is.False,
                        "Failed travel should unload the destination scene again.");
                    Assert.That(runtime.GetItemQuantityCallCount, Is.EqualTo(1),
                        "Travel should capture carried inventory before the failing handoff path.");
                    Assert.That(runtime.TryAddStackItemCallCount, Is.EqualTo(0),
                        "Suppressed replay should not restore carried inventory before the handoff failure.");
                    Assert.That(GetPendingInventorySnapshotCount(), Is.EqualTo(0));
                    Assert.That(GetPendingWeaponSnapshotCount(), Is.EqualTo(0));
                    Assert.That(GetPrivateStaticField<object>("_pendingTravelPopulationModule"), Is.Null);
                    Assert.That(GetPrivateStaticField<string>("_pendingSceneName"), Is.Null);
                    Assert.That(GetPrivateStaticField<string>("_pendingEntryPointId"), Is.Null);
                    Assert.That(GetPrivateStaticField<bool>("_pendingTravelSuppressesCarriedInventoryReplay"), Is.False);
                    Assert.That(WorldTravelCoordinator.LastResolvedEntryPointId, Is.Null);
                }
                finally
                {
                    SetPrivateStaticField("_finalizePlayerTravelHandoffHookForTests", previousFinalizeHook);
                }
            }
            finally
            {
                if (originalScene.IsValid() && originalScene.isLoaded)
                {
                    SceneManager.SetActiveScene(originalScene);
                }

                EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
                CloseSceneIfLoaded(SceneManager.GetSceneByName(MainTownSceneName));

                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        [Test]
        public void OnSceneLoaded_WhenPublishedAnchorHookThrows_ClearsResolvedEntryPointAndPendingState()
        {
            var originalScene = SceneManager.GetActiveScene();
            var bootstrapScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
            var destinationScene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(bootstrapScene);

                var persistentRoot = BootstrapWorldRoot.Initialize();
                Assert.That(persistentRoot.PlayerRootTransform, Is.Not.Null,
                    "Expected canonical runtime player before starting travel.");

                var previousHook = GetPrivateStaticField<Action>("_afterResolvedEntryPointPublishedHookForTests");
                SetPrivateStaticField(
                    "_afterResolvedEntryPointPublishedHookForTests",
                    (Action)(() => throw new InvalidOperationException("Intentional post-publish failure for travel cleanup regression coverage.")));

                try
                {
                    SetPrivateStaticField("_pendingSceneName", MainTownSceneName);
                    SetPrivateStaticField("_pendingEntryPointId", "entry.maintown.spawn");
                    LogAssert.Expect(LogType.Warning,
                        "Travel failed: Intentional post-publish failure for travel cleanup regression coverage.");
                    InvokePrivateStatic("OnSceneLoaded", destinationScene, LoadSceneMode.Additive);

                    Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(bootstrapScene),
                        "Travel should fail closed when a post-publish handoff step throws.");
                    Assert.That(destinationScene.isLoaded, Is.False,
                        "Destination scene should be unloaded again when a post-publish handoff step fails.");
                    Assert.That(WorldTravelCoordinator.LastResolvedEntryPointId, Is.Null,
                        "Travel should clear the resolved anchor when a post-publish handoff step throws after publication.");
                    Assert.That(GetPrivateStaticField<string>("_pendingSceneName"), Is.Null);
                    Assert.That(GetPrivateStaticField<string>("_pendingEntryPointId"), Is.Null);
                    Assert.That(GetPrivateStaticField<bool>("_pendingTravelSuppressesCarriedInventoryReplay"), Is.False);
                }
                finally
                {
                    SetPrivateStaticField("_afterResolvedEntryPointPublishedHookForTests", previousHook);
                }
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
            var method = ResolvePrivateStaticMethod(methodName, parameters.Length);
            Assert.That(method, Is.Not.Null, $"Expected WorldTravelCoordinator.{methodName} to exist.");
            method.Invoke(null, parameters);
        }

        private static T InvokePrivateStatic<T>(string methodName, params object[] parameters)
        {
            var method = ResolvePrivateStaticMethod(methodName, parameters.Length);
            Assert.That(method, Is.Not.Null, $"Expected WorldTravelCoordinator.{methodName} to exist.");
            return (T)method.Invoke(null, parameters);
        }

        private static System.Reflection.MethodInfo ResolvePrivateStaticMethod(string methodName, int parameterCount)
        {
            var methods = typeof(WorldTravelCoordinator).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                if (!string.Equals(method.Name, methodName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (method.GetParameters().Length == parameterCount)
                {
                    return method;
                }
            }

            return null;
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

        private static PlayerSpawnAnchor FindSpawnAnchor(Scene scene, string anchorId)
        {
            var anchors = Object.FindObjectsByType<PlayerSpawnAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < anchors.Length; i++)
            {
                if (anchors[i] != null
                    && anchors[i].gameObject.scene == scene
                    && string.Equals(anchors[i].AnchorId, anchorId, System.StringComparison.Ordinal))
                {
                    return anchors[i];
                }
            }

            return null;
        }

        private static InventoryReplayProbeRuntime InstallInventoryReplayProbe(Transform playerRoot, string carriedItemId)
        {
            Assert.That(playerRoot, Is.Not.Null);

            var existingControllers = playerRoot.GetComponents<MonoBehaviour>();
            for (var i = 0; i < existingControllers.Length; i++)
            {
                var controller = existingControllers[i];
                if (controller == null || controller.GetType().Name != "PlayerInventoryController")
                {
                    continue;
                }

                Object.DestroyImmediate(controller);
            }

            var probeControllerType = System.Type.GetType("Reloader.Inventory.PlayerInventoryController, Reloader.World.Tests.EditMode");
            Assert.That(probeControllerType, Is.Not.Null, "Expected the probe PlayerInventoryController type to resolve from the test assembly.");
            var probeController = playerRoot.gameObject.AddComponent(probeControllerType);
            var runtime = new InventoryReplayProbeRuntime();
            runtime.SeedCarriedItem(carriedItemId);
            var runtimeProperty = probeControllerType.GetProperty("Runtime", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(runtimeProperty, Is.Not.Null, "Expected the probe PlayerInventoryController.Runtime property.");
            runtimeProperty.SetValue(probeController, runtime);
            return runtime;
        }

        private static object GetInventoryRuntime(Component inventoryController)
        {
            var runtimeProperty = inventoryController.GetType().GetProperty("Runtime", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(runtimeProperty, Is.Not.Null, "Expected Runtime property on PlayerInventoryController.");
            return runtimeProperty!.GetValue(inventoryController);
        }

        private static int GetItemQuantity(object inventoryRuntime, string itemId)
        {
            var method = inventoryRuntime.GetType().GetMethod("GetItemQuantity", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, "Expected GetItemQuantity on inventory runtime probe.");
            return (int)method!.Invoke(inventoryRuntime, new object[] { itemId });
        }

        private static int GetSelectedBeltIndex(object inventoryRuntime)
        {
            var property = inventoryRuntime.GetType().GetProperty("SelectedBeltIndex", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, "Expected SelectedBeltIndex on inventory runtime probe.");
            return (int)property!.GetValue(inventoryRuntime);
        }

        private static void SeedPendingInventoryReplay(string itemId, int quantity, int selectedBeltIndex)
        {
            var pendingInventory = GetPrivateStaticField<Dictionary<string, int>>("_pendingInventoryQuantities");
            pendingInventory.Clear();
            pendingInventory[itemId] = quantity;
            SetPrivateStaticField("_pendingSelectedBeltIndex", selectedBeltIndex);
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

        private sealed class InventoryReplayProbeRuntime
        {
            private readonly Dictionary<string, int> _quantities = new(StringComparer.Ordinal);
            private readonly HashSet<string> _capturedItemIds = new(StringComparer.Ordinal);
            private bool _restored;

            public InventoryReplayProbeRuntime()
            {
                BeltSlotItemIds = new string[5];
                BackpackItemIds = new List<string>();
                SelectedBeltIndex = -1;
            }

            public string[] BeltSlotItemIds { get; }
            public List<string> BackpackItemIds { get; }
            public int SelectedBeltIndex { get; private set; }
            public int GetItemQuantityCallCount { get; private set; }
            public int TryAddStackItemCallCount { get; private set; }
            public int TryStoreItemCallCount { get; private set; }
            public int SelectBeltSlotCallCount { get; private set; }
            public int ClearCarriedItemsCallCount { get; private set; }

            public void SeedCarriedItem(string itemId)
            {
                _quantities[itemId] = 1;
                BeltSlotItemIds[0] = itemId;
                BackpackItemIds.Clear();
                SelectedBeltIndex = 0;
            }

            public int GetItemQuantity(string itemId)
            {
                GetItemQuantityCallCount++;
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    return 0;
                }

                if (!_capturedItemIds.Contains(itemId))
                {
                    _capturedItemIds.Add(itemId);
                    return _quantities.TryGetValue(itemId, out var quantity) ? quantity : 0;
                }

                if (!_restored)
                {
                    return 0;
                }

                return _quantities.TryGetValue(itemId, out var quantityAfterReplay) ? quantityAfterReplay : 0;
            }

            public bool TryAddStackItem(string itemId, int quantity, out object storedArea, out int storedIndex, out object rejectReason)
            {
                TryAddStackItemCallCount++;
                storedArea = null;
                storedIndex = -1;
                rejectReason = null;

                if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
                {
                    return false;
                }

                _quantities[itemId] = quantity;
                _restored = true;
                if (string.IsNullOrWhiteSpace(BeltSlotItemIds[0]))
                {
                    BeltSlotItemIds[0] = itemId;
                }

                return true;
            }

            public bool TryStoreItem(string itemId, out object storedArea, out int storedIndex, out object rejectReason)
            {
                TryStoreItemCallCount++;
                storedArea = null;
                storedIndex = -1;
                rejectReason = null;
                return TryAddStackItem(itemId, 1, out _, out _, out _);
            }

            public void SelectBeltSlot(int beltSlotIndex)
            {
                SelectBeltSlotCallCount++;
                if (beltSlotIndex < 0 || beltSlotIndex >= BeltSlotItemIds.Length)
                {
                    return;
                }

                SelectedBeltIndex = beltSlotIndex;
            }

            public void ClearSelectedBeltSlot()
            {
                SelectedBeltIndex = -1;
            }

            public void ClearCarriedItems()
            {
                ClearCarriedItemsCallCount++;
                for (var i = 0; i < BeltSlotItemIds.Length; i++)
                {
                    BeltSlotItemIds[i] = null;
                }

                BackpackItemIds.Clear();
                _quantities.Clear();
                SelectedBeltIndex = -1;
                _restored = false;
                _capturedItemIds.Clear();
            }
        }

    }
}

namespace Reloader.Inventory
{
    public sealed class PlayerInventoryController : MonoBehaviour
    {
        public object Runtime { get; set; }
    }
}
