#if UNITY_EDITOR
using System;
using Reloader.Weapons.Runtime;
using Reloader.Weapons.Controllers;
using Reloader.World.Contracts;
using Reloader.World.Travel;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Reloader.World.Editor
{
    public static class WorldSceneContractDefaultsUtility
    {
        private const string ContractFolderPath = "Assets/_Project/World/Data/SceneContracts";
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string IndoorRangeScenePath = "Assets/_Project/World/Scenes/IndoorRangeInstance.unity";

        [MenuItem("Reloader/World/Contracts/Ensure Default World Scene Contracts")]
        public static void EnsureDefaultWorldSceneContracts()
        {
            EnsureContractFolder();

            var mainTownContract = EnsureContractAsset("MainTownWorldSceneContract.asset");
            ConfigureMainTownContract(mainTownContract);

            var indoorRangeContract = EnsureContractAsset("IndoorRangeInstanceWorldSceneContract.asset");
            ConfigureIndoorRangeContract(indoorRangeContract);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Default world scene contract assets ensured.");
        }

        private static void ConfigureMainTownContract(WorldSceneContract contract)
        {
            contract.ScenePath = MainTownScenePath;
            contract.SceneRole = WorldSceneRole.TownHub;
            contract.ValidateRequiredSceneEntryPointIds = true;

            ReplaceWithBaselineObjectPaths(contract);
            ReplaceWithDefaultComponentContracts(contract);

            contract.RequiredSceneEntryPointIds.Clear();
            contract.RequiredSceneEntryPointIds.Add("entry.maintown.spawn");
            contract.RequiredSceneEntryPointIds.Add("entry.maintown.return");

            EditorUtility.SetDirty(contract);
        }

        private static void ConfigureIndoorRangeContract(WorldSceneContract contract)
        {
            contract.ScenePath = IndoorRangeScenePath;
            contract.SceneRole = WorldSceneRole.ActivityInstance;
            contract.ValidateRequiredSceneEntryPointIds = true;

            ReplaceWithBaselineObjectPaths(contract);
            ReplaceWithDefaultComponentContracts(contract);

            contract.RequiredSceneEntryPointIds.Clear();
            contract.RequiredSceneEntryPointIds.Add("entry.indoor.arrival");

            EditorUtility.SetDirty(contract);
        }

        private static void ReplaceWithBaselineObjectPaths(WorldSceneContract contract)
        {
            contract.RequiredObjectPaths.Clear();
            contract.RequiredObjectPaths.Add("PlayerRoot");
            contract.RequiredObjectPaths.Add("PlayerRoot/CameraPivot");
            contract.RequiredObjectPaths.Add("PlayerRoot/CameraPivot/CameraLookTarget");
            contract.RequiredObjectPaths.Add("PlayerRoot/CameraPivot/WeaponMuzzle");
            contract.RequiredObjectPaths.Add("WeaponRegistry");

            if (string.Equals(contract.ScenePath, MainTownScenePath, StringComparison.Ordinal))
            {
                contract.RequiredObjectPaths.Add("ReloadingWorkbench");
                contract.RequiredObjectPaths.Add("MainTown_SmokeToIndoor_Trigger");
                return;
            }

            if (string.Equals(contract.ScenePath, IndoorRangeScenePath, StringComparison.Ordinal))
            {
                contract.RequiredObjectPaths.Add("IndoorRange_SmokeToMainTown_Trigger");
                contract.RequiredObjectPaths.Add("IndoorRange_Geometry");
                contract.RequiredObjectPaths.Add("IndoorRange_Geometry/Range_Floor");
                contract.RequiredObjectPaths.Add("IndoorRange_Geometry/Range_Ceiling");
                contract.RequiredObjectPaths.Add("IndoorRange_Geometry/Range_Wall_Left");
                contract.RequiredObjectPaths.Add("IndoorRange_Geometry/Range_Wall_Right");
                contract.RequiredObjectPaths.Add("IndoorRange_Geometry/Range_Wall_Backstop");
                contract.RequiredObjectPaths.Add("IndoorRange_Geometry/FiringLine");
                contract.RequiredObjectPaths.Add("IndoorRange_Geometry/TargetPlate_1");
                contract.RequiredObjectPaths.Add("IndoorRange_Geometry/TargetPlate_2");
                contract.RequiredObjectPaths.Add("IndoorRange_Geometry/TargetPlate_3");
            }
        }

        private static void ReplaceWithDefaultComponentContracts(WorldSceneContract contract)
        {
            contract.RequiredComponentContracts.Clear();

            contract.RequiredComponentContracts.Add(new WorldRequiredComponentContract
            {
                ObjectPath = "WeaponRegistry",
                ComponentScriptAsset = TryGetMonoScriptForType(typeof(WeaponRegistry)),
                ComponentTypeName = typeof(WeaponRegistry).AssemblyQualifiedName,
                RequiredNonEmptyArrayFields = { "_definitions" }
            });

            contract.RequiredComponentContracts.Add(new WorldRequiredComponentContract
            {
                ObjectPath = "PlayerRoot",
                ComponentScriptAsset = TryGetMonoScriptForType(typeof(PlayerWeaponController)),
                ComponentTypeName = typeof(PlayerWeaponController).AssemblyQualifiedName,
                RequiredNonNullObjectReferenceFields =
                {
                    "_inputSourceBehaviour",
                    "_inventoryController",
                    "_weaponRegistry",
                    "_muzzleTransform",
                    "_cameraDefaults",
                    "_projectilePrefab"
                }
            });

            if (string.Equals(contract.ScenePath, MainTownScenePath, StringComparison.Ordinal))
            {
                contract.RequiredComponentContracts.Add(new WorldRequiredComponentContract
                {
                    ObjectPath = "MainTownEntry_Spawn",
                    ComponentScriptAsset = TryGetMonoScriptForType(typeof(SceneEntryPoint)),
                    ComponentTypeName = typeof(SceneEntryPoint).AssemblyQualifiedName,
                    RequiredNonEmptyStringFields = { "_entryPointId" }
                });
            }
            else if (string.Equals(contract.ScenePath, IndoorRangeScenePath, StringComparison.Ordinal))
            {
                contract.RequiredComponentContracts.Add(new WorldRequiredComponentContract
                {
                    ObjectPath = "IndoorRangeEntry_Arrival",
                    ComponentScriptAsset = TryGetMonoScriptForType(typeof(SceneEntryPoint)),
                    ComponentTypeName = typeof(SceneEntryPoint).AssemblyQualifiedName,
                    RequiredNonEmptyStringFields = { "_entryPointId" }
                });
            }
        }

        private static WorldSceneContract EnsureContractAsset(string fileName)
        {
            var assetPath = $"{ContractFolderPath}/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<WorldSceneContract>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var created = ScriptableObject.CreateInstance<WorldSceneContract>();
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }

        private static void EnsureContractFolder()
        {
            if (AssetDatabase.IsValidFolder(ContractFolderPath))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project/World/Data"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/World", "Data");
            }

            AssetDatabase.CreateFolder("Assets/_Project/World/Data", "SceneContracts");
        }

        private static MonoScript TryGetMonoScriptForType(Type type)
        {
            if (type == null)
            {
                return null;
            }

            if (typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                var probe = new GameObject($"__{type.Name}_ScriptProbe");
                try
                {
                    var component = probe.AddComponent(type) as MonoBehaviour;
                    return component != null ? MonoScript.FromMonoBehaviour(component) : null;
                }
                finally
                {
                    UnityObject.DestroyImmediate(probe);
                }
            }

            if (typeof(ScriptableObject).IsAssignableFrom(type))
            {
                var instance = ScriptableObject.CreateInstance(type) as ScriptableObject;
                try
                {
                    return instance != null ? MonoScript.FromScriptableObject(instance) : null;
                }
                finally
                {
                    if (instance != null)
                    {
                        UnityObject.DestroyImmediate(instance);
                    }
                }
            }

            return null;
        }
    }
}
#endif
