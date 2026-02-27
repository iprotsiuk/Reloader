using System;
using NUnit.Framework;
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

        [Test]
        public void ValidateContracts_ReportsRequiredFieldFailures_WithActionableContext()
        {
            var scene = CreateTempScene();
            try
            {
                var root = new GameObject("Root");
                SceneManager.MoveGameObjectToScene(root, scene);
                var component = root.AddComponent<ContractProbeComponent>();
                component.ResetToInvalidState();

                var contract = ScriptableObject.CreateInstance<WorldSceneContract>();
                contract.ScenePath = TempScenePath;
                contract.SceneRole = WorldSceneRole.ActivityInstance;
                contract.RequiredObjectPaths.Add("Root");
                contract.ValidateRequiredSceneEntryPointIds = false;
                contract.RequiredComponentContracts.Add(new WorldRequiredComponentContract
                {
                    ObjectPath = "Root",
                    ComponentTypeName = typeof(ContractProbeComponent).AssemblyQualifiedName,
                    RequiredNonNullObjectReferenceFields = { "_requiredReference" },
                    RequiredNonEmptyStringFields = { "_requiredString" },
                    RequiredNonEmptyArrayFields = { "_requiredArray" }
                });

                var report = WorldSceneContractValidator.ValidateContracts(new[] { contract });

                Assert.That(report.IsSuccess, Is.False);
                Assert.That(report.Issues.Count, Is.EqualTo(3));
                Assert.That(report.Issues[0].ScenePath, Is.EqualTo(TempScenePath));
                Assert.That(report.Issues[0].ObjectPath, Is.EqualTo("Root"));
                Assert.That(report.Issues[0].ComponentType, Does.Contain(nameof(ContractProbeComponent)));
            }
            finally
            {
                CloseAndDeleteTempScene(scene);
            }
        }

        [Test]
        public void ValidateContracts_PassesWhenAllRequirementsAreSatisfied()
        {
            var scene = CreateTempScene();
            try
            {
                var root = new GameObject("Root");
                SceneManager.MoveGameObjectToScene(root, scene);
                var component = root.AddComponent<ContractProbeComponent>();
                component.ApplyValidState();

                var contract = ScriptableObject.CreateInstance<WorldSceneContract>();
                contract.ScenePath = TempScenePath;
                contract.SceneRole = WorldSceneRole.TownHub;
                contract.RequiredObjectPaths.Add("Root");
                contract.ValidateRequiredSceneEntryPointIds = false;
                contract.RequiredComponentContracts.Add(new WorldRequiredComponentContract
                {
                    ObjectPath = "Root",
                    ComponentTypeName = typeof(ContractProbeComponent).AssemblyQualifiedName,
                    RequiredNonNullObjectReferenceFields = { "_requiredReference" },
                    RequiredNonEmptyStringFields = { "_requiredString" },
                    RequiredNonEmptyArrayFields = { "_requiredArray" }
                });

                var report = WorldSceneContractValidator.ValidateContracts(new[] { contract });
                Assert.That(report.IsSuccess, Is.True);
                Assert.That(report.Issues.Count, Is.EqualTo(0));
            }
            finally
            {
                CloseAndDeleteTempScene(scene);
            }
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
