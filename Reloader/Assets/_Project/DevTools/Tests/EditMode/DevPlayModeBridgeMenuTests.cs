using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace Reloader.DevTools.Tests.EditMode
{
    public sealed class DevPlayModeBridgeMenuTests
    {
        [Test]
        public void McpBridgeMenu_DefinesExpectedPlayModeActions()
        {
            var menuType = ResolveRequiredType("Reloader.DevTools.Editor.DevPlayModeBridgeMenu");
            var menuItems = menuType
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .SelectMany(static method => method
                    .GetCustomAttributesData()
                    .Where(static attribute => attribute.AttributeType == typeof(MenuItem))
                    .Select(static attribute => (string)attribute.ConstructorArguments[0].Value))
                .ToArray();

            CollectionAssert.IsSubsetOf(
                new[]
                {
                    "Tools/DevTools/MCP Bridge/Play Mode/Give Test",
                    "Tools/DevTools/MCP Bridge/Play Mode/Seed Hip Ready",
                    "Tools/DevTools/MCP Bridge/Play Mode/Seed ADS Ready",
                    "Tools/DevTools/MCP Bridge/Play Mode/Step Look Yaw +1 (Live Input)"
                },
                menuItems);

            Assert.That(menuItems, Does.Not.Contain("Tools/DevTools/MCP Bridge/Play Mode/Nudge Yaw +1"));
        }

        private static Type ResolveRequiredType(string fullName)
        {
            var type = Type.GetType(fullName);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            Assert.Fail($"Expected type '{fullName}' to be loaded in the editor domain.");
            return null;
        }
    }
}
