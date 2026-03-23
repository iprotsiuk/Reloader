using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;
using Reloader.Core.Save.Modules;
using Reloader.Startup.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Reloader.Startup.Tests.EditMode
{
    public sealed class StartupMenuFlowEditModeTests
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private string _tempDir;
        private string _saveDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "reloader-startup-flow-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _saveDir = Path.Combine(_tempDir, "Saves");
            Directory.CreateDirectory(_saveDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Test]
        public void RefreshState_WhenLatestSaveExists_ReportsLatestSceneAndEntryPoint()
        {
            var repository = new SaveFileRepository();
            var olderPath = Path.Combine(_saveDir, "slot01.json");
            var newerPath = Path.Combine(_saveDir, "slot02.json");

            repository.WriteEnvelope(olderPath, CreateEnvelope("Assets/_Project/World/Scenes/OldTown.unity", "entry.oldtown.spawn"));
            repository.WriteEnvelope(newerPath, CreateEnvelope("Assets/_Project/World/Scenes/MainTown.unity", "entry.maintown.return"));
            File.SetLastWriteTimeUtc(olderPath, DateTime.UtcNow.AddMinutes(-15));
            File.SetLastWriteTimeUtc(newerPath, DateTime.UtcNow);

            var flow = new StartupMenuFlow(
                repository,
                new TestSceneTravel(),
                new TestSaveLoader(),
                new TestRuntimeBootstrapper(),
                new TestDeferredContinueLoad(),
                _saveDir);
            var state = flow.RefreshState();

            Assert.That(state.CanContinue, Is.True);
            Assert.That(state.LatestSavePath, Is.EqualTo(newerPath));
            Assert.That(state.CurrentScenePath, Is.EqualTo("Assets/_Project/World/Scenes/MainTown.unity"));
            Assert.That(state.CurrentSceneName, Is.EqualTo("MainTown"));
            Assert.That(state.CurrentAnchorId, Is.EqualTo("entry.maintown.return"));
            Assert.That(state.StatusMessage, Does.Contain("MainTown"));
        }

        [Test]
        public void RefreshState_WhenLatestSaveIsUnreadable_FallsBackToNextNewestReadableSave()
        {
            var repository = new SaveFileRepository();
            var olderPath = Path.Combine(_saveDir, "slot01.json");
            var newerPath = Path.Combine(_saveDir, "slot02.json");

            repository.WriteEnvelope(olderPath, CreateEnvelope("Assets/_Project/World/Scenes/MainTown.unity", "entry.maintown.return"));
            File.WriteAllText(newerPath, "{ definitely-not-valid-json");
            File.SetLastWriteTimeUtc(olderPath, DateTime.UtcNow.AddMinutes(-15));
            File.SetLastWriteTimeUtc(newerPath, DateTime.UtcNow);

            var flow = new StartupMenuFlow(
                repository,
                new TestSceneTravel(),
                new TestSaveLoader(),
                new TestRuntimeBootstrapper(),
                new TestDeferredContinueLoad(),
                _saveDir);

            LogAssert.Expect(LogType.Warning, new Regex("Startup menu failed to read save"));
            var state = flow.RefreshState();

            Assert.That(state.CanContinue, Is.True);
            Assert.That(state.LatestSavePath, Is.EqualTo(olderPath));
            Assert.That(state.CurrentSceneName, Is.EqualTo("MainTown"));
            Assert.That(state.CurrentAnchorId, Is.EqualTo("entry.maintown.return"));
        }

        [Test]
        public void TryStartNewGame_RoutesToMainTownSpawn()
        {
            var bootstrapper = new TestRuntimeBootstrapper();
            var travel = new TestSceneTravel(bootstrapper);
            var flow = new StartupMenuFlow(
                new SaveFileRepository(),
                travel,
                new TestSaveLoader(),
                bootstrapper,
                new TestDeferredContinueLoad(),
                _saveDir);

            var started = flow.TryStartNewGame();

            Assert.That(started, Is.True);
            Assert.That(bootstrapper.CallCount, Is.EqualTo(1));
            Assert.That(travel.WasCalledAfterBootstrapper, Is.True);
            Assert.That(travel.SceneName, Is.EqualTo("MainTown"));
            Assert.That(travel.EntryPointId, Is.EqualTo("entry.maintown.spawn"));
        }

        [Test]
        public void TryContinueLatest_DefersSaveRestoreUntilDeferredTravelCompletion()
        {
            var repository = new SaveFileRepository();
            var savePath = Path.Combine(_saveDir, "slot01.json");
            repository.WriteEnvelope(savePath, CreateEnvelope("Assets/_Project/World/Scenes/MainTown.unity", "entry.maintown.spawn"));

            var bootstrapper = new TestRuntimeBootstrapper();
            var travel = new TestSceneTravel(bootstrapper);
            var loader = new TestSaveLoader(travel);
            var deferredLoad = new TestDeferredContinueLoad(travel);
            var flow = new StartupMenuFlow(repository, travel, loader, bootstrapper, deferredLoad, _saveDir);

            var continued = flow.TryContinueLatest();

            Assert.That(continued, Is.True);
            Assert.That(bootstrapper.CallCount, Is.EqualTo(1));
            Assert.That(travel.WasCalledAfterBootstrapper, Is.True);
            Assert.That(travel.SceneName, Is.EqualTo("MainTown"));
            Assert.That(travel.EntryPointId, Is.EqualTo("entry.maintown.spawn"));
            Assert.That(loader.LoadedPath, Is.Null);
            Assert.That(deferredLoad.WasScheduledAfterTravel, Is.False,
                "Deferred continue restore must be armed before travel starts so synchronous scene load completion cannot miss the restore callback.");
            Assert.That(deferredLoad.SceneName, Is.EqualTo("MainTown"));
            Assert.That(deferredLoad.EntryPointId, Is.EqualTo("entry.maintown.spawn"));

            deferredLoad.Complete();

            Assert.That(loader.LoadedPath, Is.EqualTo(savePath));
            Assert.That(loader.LoadCalledAfterTravel, Is.True);
        }

        [Test]
        public void TryContinueLatest_SchedulesDeferredRestoreBeforeStartingTravel()
        {
            var repository = new SaveFileRepository();
            var savePath = Path.Combine(_saveDir, "slot01.json");
            repository.WriteEnvelope(savePath, CreateEnvelope("Assets/_Project/World/Scenes/MainTown.unity", "entry.maintown.spawn"));

            var bootstrapper = new TestRuntimeBootstrapper();
            var deferredLoad = new TestDeferredContinueLoad();
            var travel = new TestSceneTravel(bootstrapper, deferredLoad);
            var loader = new TestSaveLoader(travel);
            var flow = new StartupMenuFlow(repository, travel, loader, bootstrapper, deferredLoad, _saveDir);

            var continued = flow.TryContinueLatest();

            Assert.That(continued, Is.True);
            Assert.That(travel.WasCalledAfterBootstrapper, Is.True);
            Assert.That(travel.WasCalledAfterDeferredSchedule, Is.True,
                "Deferred continue restore should already be registered before travel starts so an immediate sceneLoaded callback cannot drop the restore.");
            Assert.That(loader.LoadedPath, Is.Null);
            deferredLoad.Complete();
            Assert.That(loader.LoadedPath, Is.EqualTo(savePath));
        }

        [Test]
        public void TryContinueLatest_WhenTravelStartFails_CancelsDeferredRestore()
        {
            var repository = new SaveFileRepository();
            var savePath = Path.Combine(_saveDir, "slot01.json");
            repository.WriteEnvelope(savePath, CreateEnvelope("Assets/_Project/World/Scenes/MainTown.unity", "entry.maintown.spawn"));

            var deferredLoad = new TestDeferredContinueLoad();
            var travel = new TestSceneTravel(shouldSucceed: false);
            var flow = new StartupMenuFlow(
                repository,
                travel,
                new TestSaveLoader(),
                new TestRuntimeBootstrapper(),
                deferredLoad,
                _saveDir);

            var continued = flow.TryContinueLatest();

            Assert.That(continued, Is.False);
            Assert.That(deferredLoad.HasPendingRestore, Is.False,
                "Failed travel start should clear the deferred restore instead of leaving a stale continue callback armed.");
        }

        [Test]
        public void TryContinueLatest_WhenDeferredSaveRestoreThrows_LogsWarningAndDoesNotBubble()
        {
            var repository = new SaveFileRepository();
            var savePath = Path.Combine(_saveDir, "slot01.json");
            repository.WriteEnvelope(savePath, CreateEnvelope("Assets/_Project/World/Scenes/MainTown.unity", "entry.maintown.spawn"));

            var deferredLoad = new TestDeferredContinueLoad();
            var loader = new ThrowingTestSaveLoader("save restore failed");
            var flow = new StartupMenuFlow(
                repository,
                new TestSceneTravel(),
                loader,
                new TestRuntimeBootstrapper(),
                deferredLoad,
                _saveDir);

            var continued = flow.TryContinueLatest();

            Assert.That(continued, Is.True);
            LogAssert.Expect(LogType.Warning, new Regex("Startup menu failed to continue from save"));
            Assert.That(() => deferredLoad.Complete(), Throws.Nothing);
            Assert.That(loader.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void UnityStartupDeferredContinueLoad_WaitsForTravelResolutionBeforeInvokingRestore()
        {
            var deferredType = typeof(StartupMenuFlow).Assembly.GetType("Reloader.Startup.Runtime.UnityStartupDeferredContinueLoad");
            Assert.That(deferredType, Is.Not.Null, "Expected UnityStartupDeferredContinueLoad type.");

            var deferred = Activator.CreateInstance(deferredType!);
            var restored = false;
            var originalScene = SceneManager.GetActiveScene();
            var originScene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            var destinationScene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);

            try
            {
                SetInstanceField(deferred, "_pendingSceneName", destinationScene.name);
                SetInstanceField(deferred, "_pendingEntryPointId", "entry.maintown.spawn");
                SetInstanceField(deferred, "_restoreAction", (Action)(() => restored = true));

                SetWorldTravelCoordinatorLastResolvedEntryPointId(null);
                InvokeInstanceMethod(deferred, "OnSceneLoaded", destinationScene, LoadSceneMode.Additive);

                Assert.That(restored, Is.False,
                    "Deferred continue restore should not fire before travel resolution has published the resolved entry id.");
                Assert.That(GetInstanceField<Action>(deferred, "_restoreAction"), Is.Not.Null,
                    "Pending restore should remain armed until the travel coordinator finishes resolving the destination entry.");
                Assert.That(InvokeStaticMethod<bool>(deferredType, "IsMatchingPendingScene", destinationScene, destinationScene.name), Is.True,
                    "Deferred continue harness should be driving the same scene identity path that the runtime callback uses.");

                SetWorldTravelCoordinatorLastResolvedEntryPointId("entry.maintown.spawn");
                InvokeInstanceMethod(deferred, "OnActiveSceneChanged", originScene, destinationScene);

                Assert.That(restored, Is.True);
                Assert.That(GetInstanceField<Action>(deferred, "_restoreAction"), Is.Null,
                    "Successful deferred restore should clear the pending callback after it runs.");
            }
            finally
            {
                SetWorldTravelCoordinatorLastResolvedEntryPointId(null);
                EditorSceneManager.CloseScene(destinationScene, true);
                if (originalScene.IsValid() && !string.IsNullOrWhiteSpace(originalScene.path))
                {
                    EditorSceneManager.OpenScene(originalScene.path, OpenSceneMode.Single);
                }
            }
        }

        [Test]
        public void BootstrapScene_EventSystemUiModule_UsesProjectInputActions()
        {
            const string actionsAssetPath = "Assets/_Project/Player/InputSystem_Actions.inputactions";

            var originalScene = SceneManager.GetActiveScene();
            EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);

            try
            {
                var eventSystem = GameObject.Find("EventSystem");
                Assert.That(eventSystem, Is.Not.Null, "Expected EventSystem root in Bootstrap.");

                var inputSystemUiModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                Assert.That(inputSystemUiModuleType, Is.Not.Null, "Expected InputSystemUIInputModule type to resolve.");

                var uiModule = eventSystem!.GetComponent(inputSystemUiModuleType!);
                Assert.That(uiModule, Is.Not.Null, "Expected InputSystemUIInputModule on Bootstrap EventSystem.");

                var projectActions = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(actionsAssetPath);
                Assert.That(projectActions, Is.Not.Null, $"Expected project input actions asset at {actionsAssetPath}.");

                var actionsAssetProperty = inputSystemUiModuleType!.GetProperty("actionsAsset", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(actionsAssetProperty, Is.Not.Null, "Expected InputSystemUIInputModule.actionsAsset property.");
                Assert.That(actionsAssetProperty!.GetValue(uiModule), Is.SameAs(projectActions),
                    "Expected Bootstrap EventSystem UI actions to use the shared project input actions asset.");

                var pointProperty = inputSystemUiModuleType.GetProperty("point", BindingFlags.Instance | BindingFlags.Public);
                var leftClickProperty = inputSystemUiModuleType.GetProperty("leftClick", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(pointProperty, Is.Not.Null, "Expected InputSystemUIInputModule.point property.");
                Assert.That(leftClickProperty, Is.Not.Null, "Expected InputSystemUIInputModule.leftClick property.");

                var pointReference = pointProperty!.GetValue(uiModule);
                var leftClickReference = leftClickProperty!.GetValue(uiModule);
                Assert.That(pointReference, Is.Not.Null, "Expected Bootstrap UI point action reference to be assigned.");
                Assert.That(leftClickReference, Is.Not.Null, "Expected Bootstrap UI left-click action reference to be assigned.");

                var actionProperty = pointReference!.GetType().GetProperty("action", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(actionProperty, Is.Not.Null, "Expected InputActionReference.action property.");
                Assert.That(actionProperty!.GetValue(pointReference), Is.Not.Null, "Expected Bootstrap UI point action to resolve to a real input action.");
                Assert.That(actionProperty.GetValue(leftClickReference), Is.Not.Null, "Expected Bootstrap UI left-click action to resolve to a real input action.");
            }
            finally
            {
                if (originalScene.IsValid() && !string.IsNullOrWhiteSpace(originalScene.path))
                {
                    EditorSceneManager.OpenScene(originalScene.path, OpenSceneMode.Single);
                }
            }
        }

        private sealed class TestDeferredContinueLoad : IStartupDeferredContinueLoad
        {
            private readonly TestSceneTravel _travel;
            private Action _restoreAction;

            public TestDeferredContinueLoad(TestSceneTravel travel = null)
            {
                _travel = travel;
            }

            public string SceneName { get; private set; }
            public string EntryPointId { get; private set; }
            public bool WasScheduledAfterTravel { get; private set; }
            public bool HasPendingRestore => _restoreAction != null;

            public void Schedule(string sceneName, string entryPointId, Action restoreAction)
            {
                WasScheduledAfterTravel = _travel == null || !string.IsNullOrWhiteSpace(_travel.SceneName);
                SceneName = sceneName;
                EntryPointId = entryPointId;
                _restoreAction = restoreAction;
            }

            public void CancelPending()
            {
                _restoreAction = null;
            }

            public void Complete()
            {
                Assert.That(_restoreAction, Is.Not.Null, "Expected deferred continue restore action.");
                _restoreAction.Invoke();
            }
        }

        private static SaveEnvelope CreateEnvelope(string scenePath, string anchorId)
        {
            var playerState = new PlayerStateModule
            {
                CurrentScenePath = scenePath,
                CurrentAnchorId = anchorId,
                RotationW = 1f
            };

            return new SaveEnvelope
            {
                SchemaVersion = 10,
                BuildVersion = "0.1.0-dev",
                CreatedAtUtc = "2026-03-20T00:00:00Z",
                Modules = new System.Collections.Generic.Dictionary<string, ModuleSaveBlock>
                {
                    {
                        "PlayerState",
                        new ModuleSaveBlock
                        {
                            ModuleVersion = 1,
                            PayloadJson = playerState.CaptureModuleStateJson()
                        }
                    }
                }
            };
        }

        private sealed class TestSceneTravel : IStartupMenuSceneTravel
        {
            private readonly TestRuntimeBootstrapper _bootstrapper;
            private readonly TestDeferredContinueLoad _deferredLoad;
            private readonly bool _shouldSucceed;

            public TestSceneTravel(TestRuntimeBootstrapper bootstrapper = null, TestDeferredContinueLoad deferredLoad = null, bool shouldSucceed = true)
            {
                _bootstrapper = bootstrapper;
                _deferredLoad = deferredLoad;
                _shouldSucceed = shouldSucceed;
            }

            public string SceneName { get; private set; }
            public string EntryPointId { get; private set; }
            public bool WasCalledAfterBootstrapper { get; private set; }
            public bool WasCalledAfterDeferredSchedule { get; private set; }

            public bool TryLoadSceneAtEntry(string sceneName, string entryPointId)
            {
                WasCalledAfterBootstrapper = _bootstrapper == null || _bootstrapper.CallCount > 0;
                WasCalledAfterDeferredSchedule = _deferredLoad == null || _deferredLoad.HasPendingRestore;
                SceneName = sceneName;
                EntryPointId = entryPointId;
                return _shouldSucceed;
            }
        }

        private sealed class TestRuntimeBootstrapper : IStartupRuntimeBootstrapper
        {
            public int CallCount { get; private set; }

            public void EnsureCanonicalRuntimePlayerRoot()
            {
                CallCount++;
            }
        }

        private sealed class TestSaveLoader : IStartupSaveLoader
        {
            private readonly TestSceneTravel _travel;

            public TestSaveLoader(TestSceneTravel travel = null)
            {
                _travel = travel;
            }

            public string LoadedPath { get; private set; }
            public bool LoadCalledAfterTravel { get; private set; }

            public void Load(string absolutePath)
            {
                LoadCalledAfterTravel = _travel == null || !string.IsNullOrWhiteSpace(_travel.SceneName);
                LoadedPath = absolutePath;
            }
        }

        private sealed class ThrowingTestSaveLoader : IStartupSaveLoader
        {
            private readonly string _message;

            public ThrowingTestSaveLoader(string message)
            {
                _message = message;
            }

            public int CallCount { get; private set; }

            public void Load(string absolutePath)
            {
                CallCount++;
                throw new InvalidOperationException(_message);
            }
        }

        private static void InvokeInstanceMethod(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' on {target.GetType().Name}.");
            method!.Invoke(target, args);
        }

        private static T GetInstanceField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {target.GetType().Name}.");
            return (T)field!.GetValue(target);
        }

        private static void SetInstanceField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {target.GetType().Name}.");
            field!.SetValue(target, value);
        }

        private static void SetWorldTravelCoordinatorLastResolvedEntryPointId(string entryPointId)
        {
            var coordinatorType = typeof(StartupMenuFlow).Assembly.GetType("Reloader.World.Travel.WorldTravelCoordinator")
                ?? Type.GetType("Reloader.World.Travel.WorldTravelCoordinator, Reloader.World");
            Assert.That(coordinatorType, Is.Not.Null, "Expected WorldTravelCoordinator type.");

            var field = coordinatorType!.GetField("<LastResolvedEntryPointId>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Expected WorldTravelCoordinator.LastResolvedEntryPointId backing field.");
            field!.SetValue(null, entryPointId);
        }

        private static T InvokeStaticMethod<T>(Type type, string methodName, params object[] args)
        {
            var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, $"Expected static method '{methodName}' on {type.Name}.");
            return (T)method!.Invoke(null, args);
        }
    }
}
