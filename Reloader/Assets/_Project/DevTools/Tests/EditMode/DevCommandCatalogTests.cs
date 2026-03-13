using NUnit.Framework;
using Reloader.DevTools.Runtime;
using System;
using System.Linq;

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
            Assert.That(catalog.Contains("trace"), Is.True);
            Assert.That(catalog.Contains("spawn"), Is.True);
        }

        [Test]
        public void Register_SameCommandName_ReplacesRemovedAliases()
        {
            var catalog = new DevCommandCatalog();
            catalog.Register(new DevCommandDefinition("give", "first", new[] { "g", "grant" }));
            catalog.Register(new DevCommandDefinition("give", "second", new[] { "g" }));

            Assert.That(catalog.Contains("grant"), Is.False);
            Assert.That(catalog.TryGet("g", out var definition), Is.True);
            Assert.That(definition.Description, Is.EqualTo("second"));
        }

        [Test]
        public void Register_DuplicateAliasForDifferentCommands_Throws()
        {
            var catalog = new DevCommandCatalog();
            catalog.Register(new DevCommandDefinition("give", "first", new[] { "g" }));

            Assert.That(
                () => catalog.Register(new DevCommandDefinition("god", "second", new[] { "g" })),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Register_AliasCollisionDuringReplacement_PreservesPreviousState()
        {
            var catalog = new DevCommandCatalog();
            catalog.Register(new DevCommandDefinition("give", "first", new[] { "g", "grant" }));
            catalog.Register(new DevCommandDefinition("god", "second", new[] { "godmode" }));

            Assert.That(
                () => catalog.Register(new DevCommandDefinition("give", "replacement", new[] { "g", "godmode" })),
                Throws.TypeOf<InvalidOperationException>());

            Assert.That(catalog.Contains("grant"), Is.True);
            Assert.That(catalog.TryGet("give", out var definition), Is.True);
            Assert.That(definition.Description, Is.EqualTo("first"));
        }

        [Test]
        public void GetDefinitions_PreservesRegistrationOrder()
        {
            var catalog = DevCommandCatalog.CreateDefault();

            var names = catalog.GetDefinitions().Select(static definition => definition.Name).ToArray();

            Assert.That(names, Is.EqualTo(new[] { "noclip", "give", "trace", "spawn" }));
        }
    }
}
