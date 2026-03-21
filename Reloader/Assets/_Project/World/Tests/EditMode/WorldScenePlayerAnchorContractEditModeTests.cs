using NUnit.Framework;
using Reloader.World.Contracts;
using Reloader.World.Editor;
using Reloader.World.Travel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Tests.EditMode
{
    public sealed class WorldScenePlayerAnchorContractEditModeTests
    {
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string IndoorRangeScenePath = "Assets/_Project/World/Scenes/IndoorRangeInstance.unity";
        private const string MainTownContractPath = "Assets/_Project/World/Data/SceneContracts/MainTownWorldSceneContract.asset";
        private const string IndoorRangeContractPath = "Assets/_Project/World/Data/SceneContracts/IndoorRangeInstanceWorldSceneContract.asset";

        [Test]
        public void MainTownScene_UsesExplicitPlayerSpawnAnchors_AndNoSceneOwnedPlayerRoot()
        {
            AssertSceneUsesAnchorOnlyPlayerContract(
                MainTownScenePath,
                new[]
                {
                    ("MainTownEntry_Spawn", "entry.maintown.spawn", PlayerSpawnAnchorKind.Spawn),
                    ("MainTownEntry_Return", "entry.maintown.return", PlayerSpawnAnchorKind.Return)
                },
                new[]
                {
                    ("MainTownRespawn_Hospital", "entry.maintown.respawn.hospital", PlayerSpawnAnchorKind.HospitalRespawn),
                    ("MainTownRespawn_Police", "entry.maintown.respawn.police", PlayerSpawnAnchorKind.PoliceRespawn)
                });
        }

        [Test]
        public void IndoorRangeScene_UsesExplicitPlayerSpawnAnchors_AndNoSceneOwnedPlayerRoot()
        {
            AssertSceneUsesAnchorOnlyPlayerContract(
                IndoorRangeScenePath,
                new[]
                {
                    ("IndoorRangeEntry_Arrival", "entry.indoor.arrival", PlayerSpawnAnchorKind.Spawn)
                },
                System.Array.Empty<(string objectName, string anchorId, PlayerSpawnAnchorKind kind)>());
        }

        [Test]
        public void DefaultSceneContracts_RequirePlayerAnchors_AndNoPlayerRoot()
        {
            var mainTownContract = AssetDatabase.LoadAssetAtPath<WorldSceneContract>(MainTownContractPath);
            var indoorRangeContract = AssetDatabase.LoadAssetAtPath<WorldSceneContract>(IndoorRangeContractPath);

            Assert.That(mainTownContract, Is.Not.Null);
            Assert.That(indoorRangeContract, Is.Not.Null);

            AssertContractUsesAnchorOnlyPlayerContract(mainTownContract!,
                new[]
                {
                    "MainTownEntry_Spawn",
                    "MainTownEntry_Return"
                },
                "MainTownEntry_Spawn",
                "MainTownEntry_Return",
                "MainTownRespawn_Hospital",
                "MainTownRespawn_Police");
            Assert.That(mainTownContract.RequiredComponentContracts, Has.Some.Matches<WorldRequiredComponentContract>(component =>
                component.ObjectPath == "MainTownEntry_Return" &&
                component.ComponentTypeName.Contains(typeof(SceneEntryPoint).FullName) &&
                component.RequiredNonNullObjectReferenceFields.Contains("_playerSpawnAnchor")));

            AssertContractUsesAnchorOnlyPlayerContract(indoorRangeContract!,
                new[]
                {
                    "IndoorRangeEntry_Arrival"
                },
                "IndoorRangeEntry_Arrival");
        }

        [Test]
        public void Validator_RejectsSceneOwnedPlayerRoot_WhenContractTargetsWorldScene()
        {
            const string tempScenePath = "Assets/_Project/World/Scenes/__PlayerAnchorContractValidatorScene.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var contract = ScriptableObject.CreateInstance<WorldSceneContract>();

            try
            {
                EditorSceneManager.SaveScene(scene, tempScenePath);

                var playerRoot = new GameObject("PlayerRoot");
                SceneManager.MoveGameObjectToScene(playerRoot, scene);

                contract.ScenePath = tempScenePath;
                contract.SceneRole = WorldSceneRole.TownHub;
                contract.ValidateRequiredSceneEntryPointIds = false;

                var report = WorldSceneContractValidator.ValidateContracts(new[] { contract });

                Assert.That(report.IsSuccess, Is.False);
                Assert.That(report.Issues, Has.Some.Matches<WorldSceneContractValidationIssue>(issue =>
                    issue.Message.Contains("PlayerRoot") &&
                    issue.Message.Contains("runtime-owned")));
            }
            finally
            {
                Object.DestroyImmediate(contract);
                if (scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                AssetDatabase.DeleteAsset(tempScenePath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void Validator_RejectsSceneEntryPointWithoutExplicitSpawnAnchor()
        {
            const string tempScenePath = "Assets/_Project/World/Scenes/__PlayerAnchorMissingAnchorValidatorScene.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var contract = ScriptableObject.CreateInstance<WorldSceneContract>();

            try
            {
                EditorSceneManager.SaveScene(scene, tempScenePath);

                var entryObject = new GameObject("MainTownEntry_Spawn");
                SceneManager.MoveGameObjectToScene(entryObject, scene);

                var entryPoint = entryObject.AddComponent<SceneEntryPoint>();
                JsonUtility.FromJsonOverwrite("{\"_entryPointId\":\"entry.maintown.spawn\"}", entryPoint);

                contract.ScenePath = tempScenePath;
                contract.SceneRole = WorldSceneRole.TownHub;
                contract.ValidateRequiredSceneEntryPointIds = false;

                var report = WorldSceneContractValidator.ValidateContracts(new[] { contract });

                Assert.That(report.IsSuccess, Is.False);
                Assert.That(report.Issues, Has.Some.Matches<WorldSceneContractValidationIssue>(issue =>
                    issue.FieldName == "_playerSpawnAnchor" &&
                    issue.Message.Contains("PlayerSpawnAnchor")));
            }
            finally
            {
                Object.DestroyImmediate(contract);
                if (scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                AssetDatabase.DeleteAsset(tempScenePath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void Validator_RejectsSceneEntryPointWhenAnchorKindDoesNotMatch()
        {
            const string tempScenePath = "Assets/_Project/World/Scenes/__PlayerAnchorKindMismatchValidatorScene.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var contract = ScriptableObject.CreateInstance<WorldSceneContract>();

            try
            {
                EditorSceneManager.SaveScene(scene, tempScenePath);

                var entryObject = new GameObject("MainTownEntry_Return");
                SceneManager.MoveGameObjectToScene(entryObject, scene);

                var entryPoint = entryObject.AddComponent<SceneEntryPoint>();
                var anchor = entryObject.AddComponent<PlayerSpawnAnchor>();
                entryPoint.Configure("entry.maintown.return", PlayerSpawnAnchorKind.Return);
                anchor.Configure("entry.maintown.return", PlayerSpawnAnchorKind.Spawn);

                contract.ScenePath = tempScenePath;
                contract.SceneRole = WorldSceneRole.TownHub;
                contract.ValidateRequiredSceneEntryPointIds = false;

                var report = WorldSceneContractValidator.ValidateContracts(new[] { contract });

                Assert.That(report.IsSuccess, Is.False);
                Assert.That(report.Issues, Has.Some.Matches<WorldSceneContractValidationIssue>(issue =>
                    issue.FieldName == "_playerSpawnAnchorKind" &&
                    issue.Message.Contains("does not match")));
            }
            finally
            {
                Object.DestroyImmediate(contract);
                if (scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                AssetDatabase.DeleteAsset(tempScenePath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void Validator_RejectsSceneOwnedPlayerImplementationComponents_WhenPlayerRootIsRenamed()
        {
            const string tempScenePath = "Assets/_Project/World/Scenes/__PlayerAnchorImplementationValidatorScene.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var contract = ScriptableObject.CreateInstance<WorldSceneContract>();

            try
            {
                EditorSceneManager.SaveScene(scene, tempScenePath);

                var playerRig = new GameObject("ScenePlayerRig");
                SceneManager.MoveGameObjectToScene(playerRig, scene);

                var playerCameraDefaultsType = System.Type.GetType("Reloader.Player.PlayerCameraDefaults, Reloader.Player");
                Assert.That(playerCameraDefaultsType, Is.Not.Null, "Expected PlayerCameraDefaults type to exist.");
                playerRig.AddComponent(playerCameraDefaultsType!);

                contract.ScenePath = tempScenePath;
                contract.SceneRole = WorldSceneRole.TownHub;
                contract.ValidateRequiredSceneEntryPointIds = false;

                var report = WorldSceneContractValidator.ValidateContracts(new[] { contract });

                Assert.That(report.IsSuccess, Is.False);
                Assert.That(report.Issues, Has.Some.Matches<WorldSceneContractValidationIssue>(issue =>
                    issue.ComponentType == playerCameraDefaultsType!.FullName &&
                    issue.Message.Contains("runtime player prefab")));
            }
            finally
            {
                Object.DestroyImmediate(contract);
                if (scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                AssetDatabase.DeleteAsset(tempScenePath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void Validator_RejectsCanonicalPlayerImplementationComponent_OmittedFromLegacySubset()
        {
            const string tempScenePath = "Assets/_Project/World/Scenes/__PlayerAnchorCanonicalImplementationValidatorScene.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var contract = ScriptableObject.CreateInstance<WorldSceneContract>();

            try
            {
                EditorSceneManager.SaveScene(scene, tempScenePath);

                var scenePlayerRig = new GameObject("ScenePlayerRig");
                SceneManager.MoveGameObjectToScene(scenePlayerRig, scene);

                var omittedCanonicalComponentType = System.Type.GetType("Reloader.NPCs.World.PlayerShopVendorController, Reloader.NPCs");
                Assert.That(omittedCanonicalComponentType, Is.Not.Null, "Expected PlayerShopVendorController type to exist.");
                scenePlayerRig.AddComponent(omittedCanonicalComponentType!);

                contract.ScenePath = tempScenePath;
                contract.SceneRole = WorldSceneRole.TownHub;
                contract.ValidateRequiredSceneEntryPointIds = false;

                var report = WorldSceneContractValidator.ValidateContracts(new[] { contract });

                Assert.That(report.IsSuccess, Is.False);
                Assert.That(report.Issues, Has.Some.Matches<WorldSceneContractValidationIssue>(issue =>
                    issue.ComponentType == omittedCanonicalComponentType!.FullName &&
                    issue.Message.Contains("runtime player prefab")));
            }
            finally
            {
                Object.DestroyImmediate(contract);
                if (scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                AssetDatabase.DeleteAsset(tempScenePath);
                AssetDatabase.Refresh();
            }
        }

        private static void AssertSceneUsesAnchorOnlyPlayerContract(
            string scenePath,
            (string objectName, string anchorId, PlayerSpawnAnchorKind kind)[] expectedEntryPoints,
            (string objectName, string anchorId, PlayerSpawnAnchorKind kind)[] expectedStandaloneAnchors)
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            try
            {
                Assert.That(FindRoot(scene, "PlayerRoot"), Is.Null,
                    $"Scene '{scenePath}' should expose anchors and local services only. PlayerRoot must be runtime-owned.");

                for (var i = 0; i < expectedEntryPoints.Length; i++)
                {
                    var expected = expectedEntryPoints[i];
                    var anchorObject = FindRoot(scene, expected.objectName);
                    Assert.That(anchorObject, Is.Not.Null, $"Expected anchor root '{expected.objectName}' in scene '{scenePath}'.");

                    var entryPoint = anchorObject!.GetComponent<SceneEntryPoint>();
                    Assert.That(entryPoint, Is.Not.Null, $"Expected SceneEntryPoint on '{expected.objectName}'.");
                    Assert.That(entryPoint!.EntryPointId, Is.EqualTo(expected.anchorId));

                    var anchor = anchorObject!.GetComponent<PlayerSpawnAnchor>();
                    Assert.That(anchor, Is.Not.Null, $"Expected PlayerSpawnAnchor on '{expected.objectName}'.");
                    Assert.That(anchor!.AnchorId, Is.EqualTo(expected.anchorId));
                    Assert.That(anchor.AnchorKind, Is.EqualTo(expected.kind));

                    var serializedEntryPoint = new SerializedObject(entryPoint);
                    Assert.That(serializedEntryPoint.FindProperty("_playerSpawnAnchor")?.objectReferenceValue, Is.SameAs(anchor));
                    Assert.That(serializedEntryPoint.FindProperty("_playerSpawnAnchorKind")?.enumValueIndex, Is.EqualTo((int)expected.kind));
                }

                for (var i = 0; i < expectedStandaloneAnchors.Length; i++)
                {
                    var expected = expectedStandaloneAnchors[i];
                    var anchorObject = FindRoot(scene, expected.objectName);
                    Assert.That(anchorObject, Is.Not.Null, $"Expected anchor root '{expected.objectName}' in scene '{scenePath}'.");

                    Assert.That(anchorObject!.GetComponent<SceneEntryPoint>(), Is.Null,
                        $"Standalone respawn anchor '{expected.objectName}' should not expose a SceneEntryPoint.");

                    var anchor = anchorObject.GetComponent<PlayerSpawnAnchor>();
                    Assert.That(anchor, Is.Not.Null, $"Expected PlayerSpawnAnchor on '{expected.objectName}'.");
                    Assert.That(anchor!.AnchorId, Is.EqualTo(expected.anchorId));
                    Assert.That(anchor.AnchorKind, Is.EqualTo(expected.kind));
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        private static void AssertContractUsesAnchorOnlyPlayerContract(
            WorldSceneContract contract,
            string[] expectedSceneEntryPointObjectPaths,
            params string[] expectedAnchorObjectPaths)
        {
            Assert.That(contract.RequiredObjectPaths, Has.None.EqualTo("PlayerRoot"));
            Assert.That(contract.RequiredObjectPaths, Has.None.EqualTo("PlayerRoot/CameraPivot"));
            Assert.That(contract.RequiredObjectPaths, Has.None.EqualTo("PlayerRoot/CameraPivot/CameraLookTarget"));
            Assert.That(contract.RequiredObjectPaths, Has.None.EqualTo("PlayerRoot/CameraPivot/WeaponMuzzle"));

            for (var i = 0; i < expectedAnchorObjectPaths.Length; i++)
            {
                Assert.That(contract.RequiredObjectPaths, Does.Contain(expectedAnchorObjectPaths[i]));
            }

            Assert.That(contract.RequiredComponentContracts, Has.None.Matches<WorldRequiredComponentContract>(component =>
                component.ObjectPath == "PlayerRoot"));
            for (var i = 0; i < expectedSceneEntryPointObjectPaths.Length; i++)
            {
                var expectedObjectPath = expectedSceneEntryPointObjectPaths[i];
                Assert.That(contract.RequiredComponentContracts, Has.Some.Matches<WorldRequiredComponentContract>(component =>
                    component.ObjectPath == expectedObjectPath &&
                    component.ComponentTypeName.Contains(typeof(SceneEntryPoint).FullName)));
            }

            Assert.That(contract.RequiredComponentContracts, Has.Some.Matches<WorldRequiredComponentContract>(component =>
                component.ComponentTypeName.Contains(typeof(PlayerSpawnAnchor).FullName)));
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == rootName)
                {
                    return roots[i];
                }
            }

            return null;
        }
    }
}
