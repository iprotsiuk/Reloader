using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Reloader.Core.Persistence;

namespace Reloader.Core.Tests
{
    public class WorldScenePersistencePolicyValidatorTests
    {
        [Test]
        public void ValidatePolicies_ReportsMissingPolicyForRequiredScene()
        {
            var requiredScenePaths = new[]
            {
                "Assets/_Project/World/Scenes/MainTown.unity",
                "Assets/_Project/World/Scenes/IndoorRangeInstance.unity"
            };

            var policies = new[]
            {
                new WorldScenePersistencePolicy
                {
                    ScenePath = "Assets/_Project/World/Scenes/MainTown.unity"
                }
            };

            var report = InvokeValidatePolicies(requiredScenePaths, policies);

            Assert.That(GetBoolProperty(report, "IsSuccess"), Is.False);
            var issues = GetIssues(report);
            Assert.That(issues, Has.Count.EqualTo(1));

            var issue = issues[0];
            Assert.That(GetStringProperty(issue, "ScenePath"), Is.EqualTo("Assets/_Project/World/Scenes/IndoorRangeInstance.unity"));
            Assert.That(GetStringProperty(issue, "Message"), Does.Contain("missing persistence policy").IgnoreCase);
        }

        [Test]
        public void ValidatePolicies_ReportsDuplicateScenePathPolicies()
        {
            var requiredScenePaths = new[]
            {
                "Assets/_Project/World/Scenes/MainTown.unity"
            };

            var policies = new[]
            {
                new WorldScenePersistencePolicy { ScenePath = "Assets/_Project/World/Scenes/MainTown.unity" },
                new WorldScenePersistencePolicy { ScenePath = "Assets/_Project/World/Scenes/MainTown.unity" }
            };

            var report = InvokeValidatePolicies(requiredScenePaths, policies);

            Assert.That(GetBoolProperty(report, "IsSuccess"), Is.False);
            var issues = GetIssues(report);
            Assert.That(issues, Has.Count.EqualTo(1));

            var issue = issues[0];
            Assert.That(GetStringProperty(issue, "ScenePath"), Is.EqualTo("Assets/_Project/World/Scenes/MainTown.unity"));
            Assert.That(GetStringProperty(issue, "Message"), Does.Contain("duplicate").IgnoreCase);
        }

        [Test]
        public void ValidateAllPolicies_PassesForCurrentBuildAndContractScenes()
        {
            var validatorType = FindType("Reloader.World.Editor.WorldScenePersistencePolicyValidator");
            Assert.That(validatorType, Is.Not.Null, "Expected world policy validator type to exist.");

            var method = validatorType.GetMethod("ValidateAllPolicies", Type.EmptyTypes);
            Assert.That(method, Is.Not.Null, "Expected ValidateAllPolicies() method.");

            var report = method.Invoke(null, null);
            var issues = GetIssues(report).Cast<object>().ToArray();
            var issueSummary = string.Join(
                Environment.NewLine,
                issues.Select(issue => $"{GetStringProperty(issue, "ScenePath")}: {GetStringProperty(issue, "Message")}"));

            Assert.That(GetBoolProperty(report, "IsSuccess"), Is.True, issueSummary);
        }

        private static object InvokeValidatePolicies(IEnumerable<string> requiredScenePaths, IEnumerable<WorldScenePersistencePolicy> policies)
        {
            var validatorType = FindType("Reloader.World.Editor.WorldScenePersistencePolicyValidator");
            Assert.That(validatorType, Is.Not.Null, "Expected world policy validator type to exist.");

            var method = validatorType.GetMethod("ValidatePolicies", new[] { typeof(IEnumerable<string>), typeof(IEnumerable<WorldScenePersistencePolicy>) });
            Assert.That(method, Is.Not.Null, "Expected ValidatePolicies(requiredScenePaths, policies) method.");

            return method.Invoke(null, new object[] { requiredScenePaths, policies });
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static IList GetIssues(object report)
        {
            var issues = report.GetType().GetProperty("Issues")?.GetValue(report);
            Assert.That(issues, Is.Not.Null, "Expected report to expose Issues collection.");
            return issues as IList ?? ((IEnumerable)issues).Cast<object>().ToList();
        }

        private static bool GetBoolProperty(object target, string propertyName)
        {
            var value = target.GetType().GetProperty(propertyName)?.GetValue(target);
            Assert.That(value, Is.TypeOf<bool>(), $"Expected '{propertyName}' to be bool.");
            return (bool)value;
        }

        private static string GetStringProperty(object target, string propertyName)
        {
            var value = target.GetType().GetProperty(propertyName)?.GetValue(target);
            Assert.That(value, Is.TypeOf<string>(), $"Expected '{propertyName}' to be string.");
            return (string)value;
        }
    }
}
