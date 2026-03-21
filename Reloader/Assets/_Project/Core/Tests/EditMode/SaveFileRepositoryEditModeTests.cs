using System;
using System.IO;
using NUnit.Framework;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;

namespace Reloader.Core.Tests.EditMode
{
    public sealed class SaveFileRepositoryEditModeTests
    {
        private string _tempDir;
        private string _saveDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "reloader-save-file-repo-tests-" + Guid.NewGuid().ToString("N"));
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
        public void TryFindLatestSavePath_ReturnsMostRecentlyWrittenJsonFile()
        {
            var repository = new SaveFileRepository();
            var oldest = Path.Combine(_saveDir, "slot01.json");
            var newest = Path.Combine(_saveDir, "slot02.json");

            File.WriteAllText(oldest, "{}");
            File.WriteAllText(newest, "{}");
            File.SetLastWriteTimeUtc(oldest, DateTime.UtcNow.AddMinutes(-20));
            File.SetLastWriteTimeUtc(newest, DateTime.UtcNow);

            var found = repository.TryFindLatestSavePath(_saveDir, out var latestPath);

            Assert.That(found, Is.True);
            Assert.That(latestPath, Is.EqualTo(newest));
        }
    }
}
