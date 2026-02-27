#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Reloader.World.Contracts;
using Reloader.World.Travel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Reloader.World.Editor
{
    public static class WorldSceneContractValidator
    {
        public const string DefaultContractFolderPath = "Assets/_Project/World/Data/SceneContracts";

        [MenuItem("Reloader/World/Validate All Scene Contracts")]
        public static void ValidateAllSceneContractsMenu()
        {
            var report = ValidateAllSceneContracts();
            LogReport(report);
        }

        public static WorldSceneContractValidationReport ValidateAllSceneContracts()
        {
            var guids = AssetDatabase.FindAssets("t:WorldSceneContract", new[] { DefaultContractFolderPath });
            var contracts = new List<WorldSceneContract>(guids.Length);

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var contract = AssetDatabase.LoadAssetAtPath<WorldSceneContract>(assetPath);
                if (contract != null)
                {
                    contracts.Add(contract);
                }
            }

            return ValidateContracts(contracts);
        }

        public static WorldSceneContractValidationReport ValidateContracts(IEnumerable<WorldSceneContract> contracts)
        {
            var report = new WorldSceneContractValidationReport();
            if (contracts == null)
            {
                return report;
            }

            foreach (var contract in contracts.Where(c => c != null))
            {
                report.ContractsValidated++;
                ValidateSingleContract(contract, report);
            }

            return report;
        }

        public static void LogReport(WorldSceneContractValidationReport report)
        {
            if (report == null)
            {
                Debug.LogError("World scene contract validation produced no report.");
                return;
            }

            if (report.IsSuccess)
            {
                Debug.Log($"World scene contracts valid. Contracts checked: {report.ContractsValidated}.");
                return;
            }

            foreach (var issue in report.Issues)
            {
                Debug.LogError(issue.ToLogString(), issue.ContractAsset);
            }

            Debug.LogError($"World scene contract validation failed. Issues: {report.Issues.Count}. Contracts checked: {report.ContractsValidated}.");
        }

        private static void ValidateSingleContract(WorldSceneContract contract, WorldSceneContractValidationReport report)
        {
            var contractAssetPath = AssetDatabase.GetAssetPath(contract);
            var scenePath = (contract.ScenePath ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                report.AddIssue(WorldSceneContractValidationIssue.ForContract(
                    contract,
                    contractAssetPath,
                    scenePath,
                    "Contract scene path is empty."));
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                report.AddIssue(WorldSceneContractValidationIssue.ForContract(
                    contract,
                    contractAssetPath,
                    scenePath,
                    $"Scene asset not found at path '{scenePath}'."));
                return;
            }

            var activeBefore = SceneManager.GetActiveScene();
            var wasOpen = TryGetOpenScene(scenePath, out var scene);

            if (!wasOpen)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }

            try
            {
                if (!scene.IsValid())
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForContract(
                        contract,
                        contractAssetPath,
                        scenePath,
                        $"Failed to open scene '{scenePath}' for validation."));
                    return;
                }

                ValidateRequiredObjectPaths(contract, report, scene, contractAssetPath, scenePath);
                ValidateRequiredComponentContracts(contract, report, scene, contractAssetPath, scenePath);
                ValidateSceneEntryPoints(contract, report, scene, contractAssetPath, scenePath);
            }
            finally
            {
                if (!wasOpen && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (activeBefore.IsValid())
                {
                    SceneManager.SetActiveScene(activeBefore);
                }
            }
        }

        private static void ValidateRequiredObjectPaths(
            WorldSceneContract contract,
            WorldSceneContractValidationReport report,
            Scene scene,
            string contractAssetPath,
            string scenePath)
        {
            for (var i = 0; i < contract.RequiredObjectPaths.Count; i++)
            {
                var objectPath = (contract.RequiredObjectPaths[i] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(objectPath))
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForContract(
                        contract,
                        contractAssetPath,
                        scenePath,
                        $"Required object path entry at index {i} is empty."));
                    continue;
                }

                if (!TryFindByPath(scene, objectPath, out _))
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForObjectPath(
                        contract,
                        contractAssetPath,
                        scenePath,
                        objectPath,
                        "Required object path not found in scene."));
                }
            }
        }

        private static void ValidateRequiredComponentContracts(
            WorldSceneContract contract,
            WorldSceneContractValidationReport report,
            Scene scene,
            string contractAssetPath,
            string scenePath)
        {
            foreach (var componentContract in contract.RequiredComponentContracts)
            {
                if (componentContract == null)
                {
                    continue;
                }

                var objectPath = (componentContract.ObjectPath ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(objectPath))
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForContract(
                        contract,
                        contractAssetPath,
                        scenePath,
                        "Component contract is missing object path."));
                    continue;
                }

                if (!TryFindByPath(scene, objectPath, out var targetGameObject))
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForObjectPath(
                        contract,
                        contractAssetPath,
                        scenePath,
                        objectPath,
                        "Component contract object path not found."));
                    continue;
                }

                var componentType = ResolveComponentType(componentContract, out var componentTypeLabel, out var resolutionError);
                if (componentType == null)
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForComponent(
                        contract,
                        contractAssetPath,
                        scenePath,
                        objectPath,
                        componentTypeLabel,
                        resolutionError));
                    continue;
                }

                var component = targetGameObject.GetComponent(componentType);
                if (component == null)
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForComponent(
                        contract,
                        contractAssetPath,
                        scenePath,
                        objectPath,
                        componentType.FullName,
                        "Required component is missing on target object."));
                    continue;
                }

                var so = new SerializedObject(component);
                ValidateObjectReferenceFields(componentContract, report, contract, contractAssetPath, scenePath, objectPath, componentType.FullName, so);
                ValidateStringFields(componentContract, report, contract, contractAssetPath, scenePath, objectPath, componentType.FullName, so);
                ValidateArrayFields(componentContract, report, contract, contractAssetPath, scenePath, objectPath, componentType.FullName, so);
            }
        }

        private static void ValidateObjectReferenceFields(
            WorldRequiredComponentContract componentContract,
            WorldSceneContractValidationReport report,
            WorldSceneContract contract,
            string contractAssetPath,
            string scenePath,
            string objectPath,
            string componentTypeName,
            SerializedObject serializedObject)
        {
            foreach (var fieldNameRaw in componentContract.RequiredNonNullObjectReferenceFields)
            {
                var fieldName = (fieldNameRaw ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(fieldName))
                {
                    continue;
                }

                var property = serializedObject.FindProperty(fieldName);
                if (property == null)
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForField(
                        contract,
                        contractAssetPath,
                        scenePath,
                        objectPath,
                        componentTypeName,
                        fieldName,
                        "Serialized field not found."));
                    continue;
                }

                if (property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForField(
                        contract,
                        contractAssetPath,
                        scenePath,
                        objectPath,
                        componentTypeName,
                        fieldName,
                        "Field is not an object reference."));
                    continue;
                }

                if (property.objectReferenceValue == null)
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForField(
                        contract,
                        contractAssetPath,
                        scenePath,
                        objectPath,
                        componentTypeName,
                        fieldName,
                        "Required object reference is null."));
                }
            }
        }

        private static void ValidateStringFields(
            WorldRequiredComponentContract componentContract,
            WorldSceneContractValidationReport report,
            WorldSceneContract contract,
            string contractAssetPath,
            string scenePath,
            string objectPath,
            string componentTypeName,
            SerializedObject serializedObject)
        {
            foreach (var fieldNameRaw in componentContract.RequiredNonEmptyStringFields)
            {
                var fieldName = (fieldNameRaw ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(fieldName))
                {
                    continue;
                }

                var property = serializedObject.FindProperty(fieldName);
                if (property == null)
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForField(
                        contract,
                        contractAssetPath,
                        scenePath,
                        objectPath,
                        componentTypeName,
                        fieldName,
                        "Serialized field not found."));
                    continue;
                }

                if (property.propertyType != SerializedPropertyType.String)
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForField(
                        contract,
                        contractAssetPath,
                        scenePath,
                        objectPath,
                        componentTypeName,
                        fieldName,
                        "Field is not a string."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(property.stringValue))
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForField(
                        contract,
                        contractAssetPath,
                        scenePath,
                        objectPath,
                        componentTypeName,
                        fieldName,
                        "Required string field is empty."));
                }
            }
        }

        private static void ValidateArrayFields(
            WorldRequiredComponentContract componentContract,
            WorldSceneContractValidationReport report,
            WorldSceneContract contract,
            string contractAssetPath,
            string scenePath,
            string objectPath,
            string componentTypeName,
            SerializedObject serializedObject)
        {
            foreach (var fieldNameRaw in componentContract.RequiredNonEmptyArrayFields)
            {
                var fieldName = (fieldNameRaw ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(fieldName))
                {
                    continue;
                }

                var property = serializedObject.FindProperty(fieldName);
                if (property == null)
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForField(
                        contract,
                        contractAssetPath,
                        scenePath,
                        objectPath,
                        componentTypeName,
                        fieldName,
                        "Serialized field not found."));
                    continue;
                }

                if (!property.isArray || property.propertyType == SerializedPropertyType.String)
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForField(
                        contract,
                        contractAssetPath,
                        scenePath,
                        objectPath,
                        componentTypeName,
                        fieldName,
                        "Field is not an array/list."));
                    continue;
                }

                if (property.arraySize <= 0)
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForField(
                        contract,
                        contractAssetPath,
                        scenePath,
                        objectPath,
                        componentTypeName,
                        fieldName,
                        "Required array/list is empty."));
                }
            }
        }

        private static void ValidateSceneEntryPoints(
            WorldSceneContract contract,
            WorldSceneContractValidationReport report,
            Scene scene,
            string contractAssetPath,
            string scenePath)
        {
            if (!contract.ValidateRequiredSceneEntryPointIds)
            {
                return;
            }

            var entryPoints = Object.FindObjectsByType<SceneEntryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(entry => entry != null && entry.gameObject.scene == scene)
                .ToList();

            var ids = new Dictionary<string, SceneEntryPoint>(StringComparer.Ordinal);
            foreach (var entryPoint in entryPoints)
            {
                var id = (entryPoint.EntryPointId ?? string.Empty).Trim();
                var objectPath = BuildPath(entryPoint.transform);

                if (string.IsNullOrWhiteSpace(id))
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForField(
                        contract,
                        contractAssetPath,
                        scenePath,
                        objectPath,
                        typeof(SceneEntryPoint).FullName,
                        "_entryPointId",
                        "SceneEntryPoint has empty entry point id."));
                    continue;
                }

                if (ids.ContainsKey(id))
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForField(
                        contract,
                        contractAssetPath,
                        scenePath,
                        objectPath,
                        typeof(SceneEntryPoint).FullName,
                        "_entryPointId",
                        $"Duplicate SceneEntryPoint id '{id}' detected."));
                    continue;
                }

                ids[id] = entryPoint;
            }

            foreach (var requiredIdRaw in contract.RequiredSceneEntryPointIds)
            {
                var requiredId = (requiredIdRaw ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(requiredId))
                {
                    continue;
                }

                if (!ids.ContainsKey(requiredId))
                {
                    report.AddIssue(WorldSceneContractValidationIssue.ForField(
                        contract,
                        contractAssetPath,
                        scenePath,
                        "<scene-entry-points>",
                        typeof(SceneEntryPoint).FullName,
                        "_entryPointId",
                        $"Required SceneEntryPoint id '{requiredId}' was not found."));
                }
            }
        }

        private static Type ResolveComponentType(
            WorldRequiredComponentContract contract,
            out string typeLabel,
            out string resolutionError)
        {
            var fallbackTypeName = (contract.ComponentTypeName ?? string.Empty).Trim();
            var monoScript = contract.ComponentScriptAsset as MonoScript;

            if (contract.ComponentScriptAsset != null && monoScript == null)
            {
                typeLabel = !string.IsNullOrWhiteSpace(fallbackTypeName) ? fallbackTypeName : contract.ComponentScriptAsset.name;
                resolutionError = "Component script asset must be a MonoScript.";
                return null;
            }

            if (monoScript != null)
            {
                var fromScript = monoScript.GetClass();
                if (fromScript != null && typeof(Component).IsAssignableFrom(fromScript))
                {
                    typeLabel = fromScript.FullName;
                    resolutionError = string.Empty;
                    return fromScript;
                }

                typeLabel = monoScript.name;
                resolutionError = "MonoScript does not resolve to a Component type.";
                return null;
            }

            if (TryResolveTypeName(fallbackTypeName, out var resolvedType) && typeof(Component).IsAssignableFrom(resolvedType))
            {
                typeLabel = resolvedType.FullName;
                resolutionError = string.Empty;
                return resolvedType;
            }

            typeLabel = fallbackTypeName;
            resolutionError = "Unable to resolve component type from MonoScript or fallback type name.";
            return null;
        }

        private static bool TryResolveTypeName(string typeName, out Type resolvedType)
        {
            resolvedType = null;
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return false;
            }

            resolvedType = Type.GetType(typeName, false);
            if (resolvedType != null)
            {
                return true;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                resolvedType = assemblies[i].GetType(typeName, false);
                if (resolvedType != null)
                {
                    return true;
                }
            }

            for (var i = 0; i < assemblies.Length; i++)
            {
                Type[] types;
                try
                {
                    types = assemblies[i].GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                for (var j = 0; j < types.Length; j++)
                {
                    var type = types[j];
                    if (type == null)
                    {
                        continue;
                    }

                    if (string.Equals(type.FullName, typeName, StringComparison.Ordinal) ||
                        string.Equals(type.Name, typeName, StringComparison.Ordinal))
                    {
                        resolvedType = type;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetOpenScene(string scenePath, out Scene scene)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var candidate = SceneManager.GetSceneAt(i);
                if (candidate.IsValid() && string.Equals(candidate.path, scenePath, StringComparison.Ordinal))
                {
                    scene = candidate;
                    return true;
                }
            }

            scene = default;
            return false;
        }

        private static bool TryFindByPath(Scene scene, string objectPath, out GameObject gameObject)
        {
            gameObject = null;
            if (!scene.IsValid() || string.IsNullOrWhiteSpace(objectPath))
            {
                return false;
            }

            var segments = objectPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                return false;
            }

            Transform current = null;
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (string.Equals(roots[i].name, segments[0], StringComparison.Ordinal))
                {
                    current = roots[i].transform;
                    break;
                }
            }

            if (current == null)
            {
                return false;
            }

            for (var i = 1; i < segments.Length; i++)
            {
                current = current.Find(segments[i]);
                if (current == null)
                {
                    return false;
                }
            }

            gameObject = current.gameObject;
            return true;
        }

        private static string BuildPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            var names = new Stack<string>();
            var current = transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names.ToArray());
        }
    }

    public sealed class WorldSceneContractValidationReport
    {
        private readonly List<WorldSceneContractValidationIssue> _issues = new();

        public int ContractsValidated { get; internal set; }
        public IReadOnlyList<WorldSceneContractValidationIssue> Issues => _issues;
        public bool IsSuccess => _issues.Count == 0;

        internal void AddIssue(WorldSceneContractValidationIssue issue)
        {
            if (issue != null)
            {
                _issues.Add(issue);
            }
        }
    }

    public sealed class WorldSceneContractValidationIssue
    {
        public Object ContractAsset { get; private set; }
        public string ContractAssetPath { get; private set; }
        public string ScenePath { get; private set; }
        public string ObjectPath { get; private set; }
        public string ComponentType { get; private set; }
        public string FieldName { get; private set; }
        public string Message { get; private set; }

        public string ToLogString()
        {
            var context = $"scene='{ScenePath}', objectPath='{ObjectPath}', component='{ComponentType}', field='{FieldName}'";
            return $"World scene contract violation: {Message} ({context}, contract='{ContractAssetPath}').";
        }

        public static WorldSceneContractValidationIssue ForContract(
            Object contractAsset,
            string contractAssetPath,
            string scenePath,
            string message)
        {
            return Create(contractAsset, contractAssetPath, scenePath, "<contract>", "<n/a>", "<n/a>", message);
        }

        public static WorldSceneContractValidationIssue ForObjectPath(
            Object contractAsset,
            string contractAssetPath,
            string scenePath,
            string objectPath,
            string message)
        {
            return Create(contractAsset, contractAssetPath, scenePath, objectPath, "<n/a>", "<n/a>", message);
        }

        public static WorldSceneContractValidationIssue ForComponent(
            Object contractAsset,
            string contractAssetPath,
            string scenePath,
            string objectPath,
            string componentType,
            string message)
        {
            return Create(contractAsset, contractAssetPath, scenePath, objectPath, componentType, "<n/a>", message);
        }

        public static WorldSceneContractValidationIssue ForField(
            Object contractAsset,
            string contractAssetPath,
            string scenePath,
            string objectPath,
            string componentType,
            string fieldName,
            string message)
        {
            return Create(contractAsset, contractAssetPath, scenePath, objectPath, componentType, fieldName, message);
        }

        private static WorldSceneContractValidationIssue Create(
            Object contractAsset,
            string contractAssetPath,
            string scenePath,
            string objectPath,
            string componentType,
            string fieldName,
            string message)
        {
            return new WorldSceneContractValidationIssue
            {
                ContractAsset = contractAsset,
                ContractAssetPath = contractAssetPath ?? string.Empty,
                ScenePath = scenePath ?? string.Empty,
                ObjectPath = objectPath ?? string.Empty,
                ComponentType = componentType ?? string.Empty,
                FieldName = fieldName ?? string.Empty,
                Message = message ?? string.Empty
            };
        }
    }
}
#endif
