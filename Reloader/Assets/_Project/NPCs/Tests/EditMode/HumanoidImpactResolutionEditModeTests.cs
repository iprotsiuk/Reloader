using NUnit.Framework;
using System;
using System.Reflection;

namespace Reloader.NPCs.Tests.EditMode
{
    public sealed class HumanoidImpactResolutionEditModeTests
    {
        [TestCase("Head", 900f, true)]
        [TestCase("Neck", 900f, true)]
        [TestCase("ArmL", 900f, false)]
        [TestCase("Torso", 80f, false)]
        public void Resolve_WithExpectedZoneAndEnergyContract_ProducesExpectedLethality(string bodyZoneName, float deliveredEnergyJoules, bool expectedIsLethal)
        {
            var isLethal = ResolveIsLethal(bodyZoneName, deliveredEnergyJoules);
            Assert.That(isLethal, Is.EqualTo(expectedIsLethal));
        }

        private static bool ResolveIsLethal(string bodyZoneName, float deliveredEnergyJoules)
        {
            var bodyZoneType = ResolveType("Reloader.NPCs.Combat.HumanoidBodyZone");
            var resolverType = ResolveType("Reloader.NPCs.Combat.HumanoidImpactResolution");

            var resolveMethod = resolverType.GetMethod(
                "Resolve",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { bodyZoneType, typeof(float) },
                modifiers: null);
            Assert.That(resolveMethod, Is.Not.Null, "Expected static Resolve(HumanoidBodyZone, float) on HumanoidImpactResolution.");

            var bodyZoneValue = Enum.Parse(bodyZoneType, bodyZoneName);
            var result = resolveMethod!.Invoke(null, new object[] { bodyZoneValue, deliveredEnergyJoules });
            Assert.That(result, Is.Not.Null, "Expected HumanoidImpactResolution.Resolve to return a result object.");

            var isLethalProperty = result!.GetType().GetProperty("IsLethal", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(isLethalProperty, Is.Not.Null, "Expected resolver result to expose IsLethal.");

            return (bool)isLethalProperty!.GetValue(result);
        }

        private static Type ResolveType(string fullTypeName)
        {
            var type = Type.GetType($"{fullTypeName}, Reloader.NPCs", throwOnError: false);
            if (type != null)
            {
                return type;
            }

            type = Type.GetType(fullTypeName, throwOnError: false);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullTypeName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            Assert.Fail($"Expected type {fullTypeName} to exist.");
            return null;
        }
    }
}
