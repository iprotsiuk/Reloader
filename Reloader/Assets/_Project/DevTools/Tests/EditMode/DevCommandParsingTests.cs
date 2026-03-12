using NUnit.Framework;
using Reloader.DevTools.Runtime;

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
    }
}
