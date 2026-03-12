using NUnit.Framework;
using Reloader.DevTools.Runtime;

namespace Reloader.DevTools.Tests.EditMode
{
    public sealed class DevCommandCatalogTests
    {
        [Test]
        public void Catalog_BuildsDefaultCommands_ForApprovedFirstSlice()
        {
            var catalog = DevCommandCatalog.CreateDefault();

            Assert.That(catalog.Contains("noclip"), Is.True);
            Assert.That(catalog.Contains("give"), Is.True);
            Assert.That(catalog.Contains("traces"), Is.True);
            Assert.That(catalog.Contains("spawn"), Is.True);
        }
    }
}
