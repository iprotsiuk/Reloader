#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Reloader.Core.Persistence;
using Reloader.World.Contracts;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Reloader.World.Editor
{
    public static class WorldScenePersistencePolicyValidator
    {
        public const string DefaultPolicyFolderPath = "Assets/_Project/World/Data/SceneContracts";
        private const string WorldScenesRootPath = "Assets/_Project/World/Scenes/";

        [MenuItem("Reloader/World/Validate Scene Persistence Policies")]
        public static void ValidateAllPoliciesMenu()
        {
            var report = ValidateAllPolicies();
            LogReport(report);
        }

        public static WorldScenePersistencePolicyValidationReport ValidateAllPolicies()
        {
            var requiredScenePaths = CollectRequiredScenePaths();
            var policies = LoadPolicyEntries();
            return ValidatePolicyEntries(requiredScenePaths, policies);
        }

        public static WorldScenePersistencePolicyValidationReport ValidatePolicies(
            IEnumerable<string> requiredScenePaths,
            IEnumerable<WorldScenePersistencePolicy> policies)
        {
            var entries = (policies ?? Enumerable.Empty<WorldScenePersistencePolicy>())
                .Select(policy => new PolicyEntry(policy, null, string.Empty));

            return ValidatePolicyEntries(requiredScenePaths, entries);
        }

        public static void LogReport(WorldScenePersistencePolicyValidationReport report)
        {
            if (report == null)
            {
                Debug.LogError("World scene persistence policy validation produced no report.");
                return;
            }

            if (report.IsSuccess)
            {
                Debug.Log(
                    $"World scene persistence policies valid. Policies checked: {report.PoliciesValidated}. Required scenes checked: {report.RequiredScenesChecked}.");
                return;
            }

            foreach (var issue in report.Issues)
            {
                Debug.LogError(issue.ToLogString(), issue.PolicyAsset);
            }

            Debug.LogError(
                $"World scene persistence policy validation failed. Issues: {report.Issues.Count}. Policies checked: {report.PoliciesValidated}. Required scenes checked: {report.RequiredScenesChecked}.");
        }

        private static WorldScenePersistencePolicyValidationReport ValidatePolicyEntries(
            IEnumerable<string> requiredScenePaths,
            IEnumerable<PolicyEntry> policyEntries)
        {
            var report = new WorldScenePersistencePolicyValidationReport();
            var required = NormalizeRequiredScenePaths(requiredScenePaths);
            report.RequiredScenesChecked = required.Count;

            var scenePathToFirstAssetPath = new Dictionary<string, string>(StringComparer.Ordinal);
            var coveredScenePaths = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entry in policyEntries ?? Enumerable.Empty<PolicyEntry>())
            {
                if (entry.Policy == null)
                {
                    continue;
                }

                report.PoliciesValidated++;

                var scenePath = (entry.Policy.ScenePath ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(scenePath))
                {
                    report.AddIssue(WorldScenePersistencePolicyValidationIssue.ForPolicy(
                        entry.PolicyAsset,
                        entry.AssetPath,
                        scenePath,
                        "Policy scene path is empty."));
                    continue;
                }

                coveredScenePaths.Add(scenePath);

                if (!scenePathToFirstAssetPath.TryAdd(scenePath, entry.AssetPath))
                {
                    var firstAssetPath = scenePathToFirstAssetPath[scenePath];
                    report.AddIssue(WorldScenePersistencePolicyValidationIssue.ForPolicy(
                        entry.PolicyAsset,
                        entry.AssetPath,
                        scenePath,
                        $"Duplicate scenePath policy detected for '{scenePath}'. First policy asset: '{firstAssetPath}'."));
                }
            }

            foreach (var requiredScenePath in required)
            {
                if (coveredScenePaths.Contains(requiredScenePath))
                {
                    continue;
                }

                report.AddIssue(WorldScenePersistencePolicyValidationIssue.ForScene(
                    requiredScenePath,
                    "Required scene is missing persistence policy."));
            }

            return report;
        }

        private static HashSet<string> CollectRequiredScenePaths()
        {
            var required = new HashSet<string>(StringComparer.Ordinal);

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled)
                {
                    continue;
                }

                var path = (scene.path ?? string.Empty).Trim();
                if (!path.StartsWith(WorldScenesRootPath, StringComparison.Ordinal))
                {
                    continue;
                }

                required.Add(path);
            }

            var contractGuids = AssetDatabase.FindAssets("t:WorldSceneContract", new[] { DefaultPolicyFolderPath });
            foreach (var guid in contractGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var contract = AssetDatabase.LoadAssetAtPath<WorldSceneContract>(assetPath);
                if (contract == null)
                {
                    continue;
                }

                var scenePath = (contract.ScenePath ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(scenePath))
                {
                    required.Add(scenePath);
                }
            }

            return required;
        }

        private static IEnumerable<PolicyEntry> LoadPolicyEntries()
        {
            var guids = AssetDatabase.FindAssets("t:WorldScenePersistencePolicyAsset", new[] { DefaultPolicyFolderPath });
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var policyAsset = AssetDatabase.LoadAssetAtPath<WorldScenePersistencePolicyAsset>(assetPath);
                if (policyAsset == null)
                {
                    continue;
                }

                yield return new PolicyEntry(policyAsset.ToPolicy(), policyAsset, assetPath);
            }
        }

        private static HashSet<string> NormalizeRequiredScenePaths(IEnumerable<string> scenePaths)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (scenePaths == null)
            {
                return result;
            }

            foreach (var scenePathRaw in scenePaths)
            {
                var scenePath = (scenePathRaw ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(scenePath))
                {
                    result.Add(scenePath);
                }
            }

            return result;
        }

        private readonly struct PolicyEntry
        {
            public PolicyEntry(WorldScenePersistencePolicy policy, Object policyAsset, string assetPath)
            {
                Policy = policy;
                PolicyAsset = policyAsset;
                AssetPath = assetPath ?? string.Empty;
            }

            public WorldScenePersistencePolicy Policy { get; }
            public Object PolicyAsset { get; }
            public string AssetPath { get; }
        }
    }

    public sealed class WorldScenePersistencePolicyValidationReport
    {
        private readonly List<WorldScenePersistencePolicyValidationIssue> _issues = new();

        public int PoliciesValidated { get; internal set; }
        public int RequiredScenesChecked { get; internal set; }
        public IReadOnlyList<WorldScenePersistencePolicyValidationIssue> Issues => _issues;
        public bool IsSuccess => _issues.Count == 0;

        internal void AddIssue(WorldScenePersistencePolicyValidationIssue issue)
        {
            if (issue != null)
            {
                _issues.Add(issue);
            }
        }
    }

    public sealed class WorldScenePersistencePolicyValidationIssue
    {
        public Object PolicyAsset { get; private set; }
        public string PolicyAssetPath { get; private set; }
        public string ScenePath { get; private set; }
        public string Message { get; private set; }

        public string ToLogString()
        {
            return $"World scene persistence policy violation: {Message} (scene='{ScenePath}', policyAsset='{PolicyAssetPath}').";
        }

        public static WorldScenePersistencePolicyValidationIssue ForPolicy(
            Object policyAsset,
            string policyAssetPath,
            string scenePath,
            string message)
        {
            return Create(policyAsset, policyAssetPath, scenePath, message);
        }

        public static WorldScenePersistencePolicyValidationIssue ForScene(string scenePath, string message)
        {
            return Create(null, string.Empty, scenePath, message);
        }

        private static WorldScenePersistencePolicyValidationIssue Create(
            Object policyAsset,
            string policyAssetPath,
            string scenePath,
            string message)
        {
            return new WorldScenePersistencePolicyValidationIssue
            {
                PolicyAsset = policyAsset,
                PolicyAssetPath = policyAssetPath ?? string.Empty,
                ScenePath = scenePath ?? string.Empty,
                Message = message ?? string.Empty
            };
        }
    }
}
#endif
