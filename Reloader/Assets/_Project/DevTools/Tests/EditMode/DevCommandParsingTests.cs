using NUnit.Framework;
using Reloader.DevTools.Runtime;
using System.Linq;

namespace Reloader.DevTools.Tests.EditMode
{
    public sealed class DevCommandParsingTests
    {
        [Test]
        public void Parse_GiveItemCommand_PreservesCommandAndArguments()
        {
            var result = DevCommandLineParser.Parse("give item ammo-308 50");

            Assert.That(result.CommandName, Is.EqualTo("give"));
            Assert.That(result.Arguments, Is.EqualTo(new[] { "item", "ammo-308", "50" }));
        }

        [Test]
        public void Parse_WhitespaceOnlyInput_ReturnsEmptyResult()
        {
            var result = DevCommandLineParser.Parse("   ");

            Assert.That(result.HasCommand, Is.False);
            Assert.That(result.Arguments, Is.Empty);
        }

        [Test]
        public void GetSuggestions_ReturnsStableSnapshotsAcrossCalls()
        {
            var runtime = new DevToolsRuntime();

            var first = runtime.GetSuggestions("n", 0);
            var second = runtime.GetSuggestions("g", 0);

            Assert.That(first.Select(static suggestion => suggestion.Token), Is.EqualTo(new[] { "noclip" }));
            Assert.That(second.Select(static suggestion => suggestion.Token), Is.EqualTo(new[] { "give" }));
        }

        [Test]
        public void GetSuggestions_MatchesAliasesAcceptedByExecution()
        {
            var catalog = new DevCommandCatalog();
            catalog.Register(new DevCommandDefinition("teleport", "Move player.", new[] { "tp" }));
            var runtime = new DevToolsRuntime(catalog, new DevToolsState(), new DevCommandContext());

            var suggestions = runtime.GetSuggestions("tp", 0);
            var tokens = suggestions.Select(static suggestion => suggestion.Token).ToArray();
            var labels = suggestions.Select(static suggestion => suggestion.Label).ToArray();

            Assert.That(tokens, Is.EqualTo(new[] { "tp" }));
            Assert.That(labels, Is.EqualTo(new[] { "teleport (tp)" }));
        }
    }
}
