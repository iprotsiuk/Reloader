using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reloader.Weapons.Animations;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using Reloader.World.Contracts;
using Reloader.World.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Tests.EditMode
{
    public class WorldSceneContractValidatorEditModeTests
    {
        private const string TempScenePath = "Assets/_Project/World/Scenes/__ContractValidatorTestScene.unity";
        private const string RootObjectPath = "Root";

        [Test]
        public void ValidateContracts_ReportsRequiredFieldFailures_WithActionableContext()
        {
            var scene = CreateTempScene();
            WorldSceneContract contract = null;
            try
            {
                var root = new GameObject(RootObjectPath);
                SceneManager.MoveGameObjectToScene(root, scene);
                var component = root.AddComponent<ContractProbeComponent>();
                component.ResetToInvalidState();

                contract = CreateBaseContract(WorldSceneRole.ActivityInstance);

                var report = WorldSceneContractValidator.ValidateContracts(new[] { contract });

                Assert.That(report.IsSuccess, Is.False);
                Assert.That(report.Issues.Count, Is.EqualTo(3));
                Assert.That(report.Issues[0].ScenePath, Is.EqualTo(TempScenePath));
                Assert.That(report.Issues[0].ObjectPath, Is.EqualTo(RootObjectPath));
                Assert.That(report.Issues[0].ComponentType, Does.Contain(nameof(ContractProbeComponent)));
            }
            finally
            {
                if (contract != null)
                {
                    UnityEngine.Object.DestroyImmediate(contract);
                }

                CloseAndDeleteTempScene(scene);
            }
        }

        [Test]
        public void ValidateContracts_PassesWhenAllRequirementsAreSatisfied()
        {
            var scene = CreateTempScene();
            WorldSceneContract contract = null;
            try
            {
                var root = new GameObject(RootObjectPath);
                SceneManager.MoveGameObjectToScene(root, scene);
                var component = root.AddComponent<ContractProbeComponent>();
                component.ApplyValidState();

                contract = CreateBaseContract(WorldSceneRole.TownHub);

                var report = WorldSceneContractValidator.ValidateContracts(new[] { contract });
                Assert.That(report.IsSuccess, Is.True);
                Assert.That(report.Issues.Count, Is.EqualTo(0));
            }
            finally
            {
                if (contract != null)
                {
                    UnityEngine.Object.DestroyImmediate(contract);
                }

                CloseAndDeleteTempScene(scene);
            }
        }

        [Test]
        public void DefaultWorldSceneContracts_IncludeContractPrepAnchors()
        {
            var mainTownContract = AssetDatabase.LoadAssetAtPath<WorldSceneContract>(
                "Assets/_Project/World/Data/SceneContracts/MainTownWorldSceneContract.asset");
            var indoorRangeContract = AssetDatabase.LoadAssetAtPath<WorldSceneContract>(
                "Assets/_Project/World/Data/SceneContracts/IndoorRangeInstanceWorldSceneContract.asset");

            Assert.That(mainTownContract, Is.Not.Null);
            Assert.That(indoorRangeContract, Is.Not.Null);
            Assert.That(mainTownContract!.RequiredObjectPaths, Does.Contain("ReloadingWorkbench"));
            Assert.That(mainTownContract.RequiredObjectPaths, Does.Contain("MainTown_SmokeToIndoor_Trigger"));
            Assert.That(indoorRangeContract!.RequiredObjectPaths, Does.Contain("IndoorRange_SmokeToMainTown_Trigger"));
        }

        [Test]
        public void SupportedWeaponAuthority_UsesKar98kAndCanikTp9Only()
        {
            var starterPistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                "Assets/_Project/Weapons/Data/Weapons/StarterPistol.asset");
            var pistolItem = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                "Assets/_Project/Inventory/Data/Items/Pistol_9x19_Starter.asset");
            var pistolAmmoItem = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                "Assets/_Project/Inventory/Data/Items/Ammo_Factory_9x19_124_FMJ.asset");
            var pistolSpawn = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                "Assets/_Project/Inventory/Data/Spawns/Pistol_9x19_Starter_Spawn.asset");
            var pistolAmmoSpawn = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                "Assets/_Project/Inventory/Data/Spawns/Ammo_Factory_9x19_124_FMJ_Spawn.asset");
            var animationProfile = AssetDatabase.LoadAssetAtPath<WeaponAnimatorOverrideProfile>(
                "Assets/_Project/Weapons/Data/AnimationProfiles/PlayerWeaponAnimatorOverrideProfile.asset");

            Assert.That(starterPistol, Is.Not.Null);
            Assert.That(starterPistol!.ItemId, Is.EqualTo("weapon-canik-tp9"));
            Assert.That(starterPistol.DisplayName, Is.EqualTo("Canik TP9 (9mm)"));

            Assert.That(pistolItem, Is.Not.Null);
            var pistolItemSo = new SerializedObject(pistolItem);
            Assert.That(pistolItemSo.FindProperty("_definitionId")!.stringValue, Is.EqualTo("weapon-canik-tp9"));
            Assert.That(pistolItemSo.FindProperty("_displayName")!.stringValue, Is.EqualTo("Canik TP9 (9mm)"));

            Assert.That(pistolAmmoItem, Is.Not.Null);
            var pistolAmmoSo = new SerializedObject(pistolAmmoItem);
            Assert.That(pistolAmmoSo.FindProperty("_displayName")!.stringValue, Is.EqualTo("Factory 9mm 124gr FMJ"));

            Assert.That(pistolSpawn, Is.Not.Null);
            Assert.That(pistolAmmoSpawn, Is.Not.Null);

            Assert.That(animationProfile, Is.Not.Null);
            var entriesProperty = new SerializedObject(animationProfile).FindProperty("_entries");
            Assert.That(entriesProperty, Is.Not.Null);
            var itemIds = new List<string>();
            for (var i = 0; i < entriesProperty.arraySize; i++)
            {
                itemIds.Add(entriesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("_itemId").stringValue);
            }

            CollectionAssert.AreEquivalent(
                new[] { "weapon-kar98k", "weapon-canik-tp9" },
                itemIds);

            Assert.That(
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Project/Inventory/Data/Items/Rifle_556_Starter.asset"),
                Is.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Project/Inventory/Data/Items/Ammo_Factory_556_55_FMJ.asset"),
                Is.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Project/Inventory/Data/Spawns/Rifle_556_Starter_Spawn.asset"),
                Is.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Project/Inventory/Data/Spawns/Ammo_Factory_556_55_FMJ_Spawn.asset"),
                Is.Null);
        }

        [Test]
        public void ActivityInstanceScaffold_SeedsSupportedWeaponAuthoritySet()
        {
            var scene = CreateTempScene();
            try
            {
                var playerRoot = new GameObject("PlayerRoot");
                var cameraPivot = new GameObject("CameraPivot");
                var camera = new GameObject("Main Camera");

                SceneManager.MoveGameObjectToScene(playerRoot, scene);
                SceneManager.MoveGameObjectToScene(cameraPivot, scene);
                SceneManager.MoveGameObjectToScene(camera, scene);

                cameraPivot.transform.SetParent(playerRoot.transform, false);
                camera.transform.SetParent(cameraPivot.transform, false);
                camera.AddComponent<Camera>();

                EditorSceneManager.SetActiveScene(scene);
                WorldSceneTemplateScaffolds.ApplyActivityInstanceScaffoldToActiveScene();

                var registry = UnityEngine.Object.FindFirstObjectByType<WeaponRegistry>();
                Assert.That(registry, Is.Not.Null);

                var definitions = new SerializedObject(registry).FindProperty("_definitions");
                Assert.That(definitions, Is.Not.Null);
                var ids = new List<string>();
                for (var i = 0; i < definitions.arraySize; i++)
                {
                    if (definitions.GetArrayElementAtIndex(i).objectReferenceValue is WeaponDefinition definition)
                    {
                        ids.Add(definition.ItemId);
                    }
                }

                CollectionAssert.AreEquivalent(
                    new[] { "weapon-kar98k", "weapon-canik-tp9" },
                    ids);
            }
            finally
            {
                CloseAndDeleteTempScene(scene);
            }
        }

        private static WorldSceneContract CreateBaseContract(WorldSceneRole sceneRole)
        {
            var contract = ScriptableObject.CreateInstance<WorldSceneContract>();
            contract.ScenePath = TempScenePath;
            contract.SceneRole = sceneRole;
            contract.RequiredObjectPaths.Add(RootObjectPath);
            contract.ValidateRequiredSceneEntryPointIds = false;
            contract.RequiredComponentContracts.Add(new WorldRequiredComponentContract
            {
                ObjectPath = RootObjectPath,
                ComponentTypeName = typeof(ContractProbeComponent).AssemblyQualifiedName,
                RequiredNonNullObjectReferenceFields = { "_requiredReference" },
                RequiredNonEmptyStringFields = { "_requiredString" },
                RequiredNonEmptyArrayFields = { "_requiredArray" }
            });

            return contract;
        }

        private static Scene CreateTempScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, TempScenePath);
            return scene;
        }

        private static void CloseAndDeleteTempScene(Scene scene)
        {
            if (scene.IsValid())
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            AssetDatabase.DeleteAsset(TempScenePath);
            AssetDatabase.Refresh();
        }

        private sealed class ContractProbeComponent : MonoBehaviour
        {
            [SerializeField] private GameObject _requiredReference;
            [SerializeField] private string _requiredString = string.Empty;
            [SerializeField] private GameObject[] _requiredArray = Array.Empty<GameObject>();

            public void ResetToInvalidState()
            {
                _requiredReference = null;
                _requiredString = string.Empty;
                _requiredArray = Array.Empty<GameObject>();
            }

            public void ApplyValidState()
            {
                _requiredReference = gameObject;
                _requiredString = "ok";
                _requiredArray = new[] { gameObject };
            }
        }
    }
}
