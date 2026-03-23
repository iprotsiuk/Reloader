using System.Reflection;
using NUnit.Framework;

namespace Reloader.World.Tests.EditMode
{
    public sealed class DynamicOriginRebaseControllerEditModeTests
    {
        [Test]
        public void FloatingOriginRuntime_DefinesCanonicalOriginTypesInWorldAssembly()
        {
            AssertFloatingOriginTypeExists("Reloader.World.Runtime.Origin.DynamicOriginRebaseController");
            AssertFloatingOriginTypeExists("Reloader.World.Runtime.Origin.StableWorldCoordinateBridge");
            AssertFloatingOriginTypeExists("Reloader.World.Runtime.Origin.DynamicOriginRebaseState");
        }

        [Test]
        public void DynamicOriginRebaseController_ExposesCanonicalDistanceAndCooldownContract()
        {
            var controllerType = AssertFloatingOriginTypeExists("Reloader.World.Runtime.Origin.DynamicOriginRebaseController");
            if (controllerType == null)
            {
                return;
            }

            Assert.That(
                controllerType.GetProperty("RebaseDistanceMeters", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                Is.Not.Null,
                "Floating-origin slice one needs a canonical rebase distance contract so the runtime player root rebases from one world seam.");
            Assert.That(
                controllerType.GetProperty("RebaseCooldownSeconds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                Is.Not.Null,
                "Floating-origin slice one needs a cooldown-backed trigger contract instead of multiple rebase paths.");
        }

        [Test]
        public void DynamicOriginRebaseController_DoesNotExposeAdsSpecificRebaseMembers()
        {
            var controllerType = AssertFloatingOriginTypeExists("Reloader.World.Runtime.Origin.DynamicOriginRebaseController");
            if (controllerType == null)
            {
                return;
            }

            var members = controllerType.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            for (var i = 0; i < members.Length; i++)
            {
                Assert.That(
                    members[i].Name,
                    Does.Not.Contain("Ads").IgnoreCase.And.Not.Contain("AimDownSights").IgnoreCase,
                    $"Floating-origin rebasing should stay canonical and cooldown-backed instead of reviving ADS-specific trigger paths. Offending member: {members[i].Name}.");
            }
        }

        private static System.Type AssertFloatingOriginTypeExists(string fullTypeName)
        {
            var resolvedType = System.Type.GetType($"{fullTypeName}, Reloader.World");
            Assert.That(resolvedType, Is.Not.Null, $"Expected floating-origin runtime type '{fullTypeName}' in assembly 'Reloader.World'.");
            return resolvedType;
        }
    }
}
