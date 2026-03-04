using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Reloader.Reloading.Tests.EditMode
{
    public class WorkbenchLoadoutControllerEditModeTests
    {
        [Test]
        public void Source_DoesNotUsePrivateFieldReflection()
        {
            var controllerPath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Reloading",
                "Scripts",
                "Runtime",
                "WorkbenchLoadoutController.cs");

            Assert.That(File.Exists(controllerPath), Is.True, $"Expected source file at {controllerPath}");

            var source = File.ReadAllText(controllerPath);

            Assert.That(source, Does.Not.Contain("using System.Reflection;"));
            Assert.That(source, Does.Not.Contain("GetField("));
            Assert.That(source, Does.Not.Contain("BindingFlags."));
            Assert.That(source, Does.Not.Contain("_slotsById"));
        }
    }
}
