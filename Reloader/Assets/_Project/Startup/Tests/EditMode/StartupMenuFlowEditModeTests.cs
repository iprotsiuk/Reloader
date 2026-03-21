using System;
using System.IO;
using NUnit.Framework;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;
using Reloader.Core.Save.Modules;
using Reloader.Startup.Runtime;

namespace Reloader.Startup.Tests.EditMode
{
    public sealed class StartupMenuFlowEditModeTests
    {
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

            var flow = new StartupMenuFlow(repository, new TestSceneTravel(), new TestSaveLoader(), _saveDir);
            var state = flow.RefreshState();

            Assert.That(state.CanContinue, Is.True);
            Assert.That(state.LatestSavePath, Is.EqualTo(newerPath));
            Assert.That(state.CurrentScenePath, Is.EqualTo("Assets/_Project/World/Scenes/MainTown.unity"));
            Assert.That(state.CurrentSceneName, Is.EqualTo("MainTown"));
            Assert.That(state.CurrentAnchorId, Is.EqualTo("entry.maintown.return"));
            Assert.That(state.StatusMessage, Does.Contain("MainTown"));
        }

        [Test]
        public void TryStartNewGame_RoutesToMainTownSpawn()
        {
            var travel = new TestSceneTravel();
            var flow = new StartupMenuFlow(new SaveFileRepository(), travel, new TestSaveLoader(), _saveDir);

            var started = flow.TryStartNewGame();

            Assert.That(started, Is.True);
            Assert.That(travel.SceneName, Is.EqualTo("MainTown"));
            Assert.That(travel.EntryPointId, Is.EqualTo("entry.maintown.spawn"));
        }

        [Test]
        public void TryContinueLatest_LoadsSceneBeforeRestoringSave()
        {
            var repository = new SaveFileRepository();
            var savePath = Path.Combine(_saveDir, "slot01.json");
            repository.WriteEnvelope(savePath, CreateEnvelope("Assets/_Project/World/Scenes/MainTown.unity", "entry.maintown.spawn"));

            var travel = new TestSceneTravel();
            var loader = new TestSaveLoader(travel);
            var flow = new StartupMenuFlow(repository, travel, loader, _saveDir);

            var continued = flow.TryContinueLatest();

            Assert.That(continued, Is.True);
            Assert.That(travel.SceneName, Is.EqualTo("MainTown"));
            Assert.That(travel.EntryPointId, Is.EqualTo("entry.maintown.spawn"));
            Assert.That(loader.LoadedPath, Is.EqualTo(savePath));
            Assert.That(loader.LoadCalledAfterTravel, Is.True);
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
            public string SceneName { get; private set; }
            public string EntryPointId { get; private set; }

            public bool TryLoadSceneAtEntry(string sceneName, string entryPointId)
            {
                SceneName = sceneName;
                EntryPointId = entryPointId;
                return true;
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
    }
}
