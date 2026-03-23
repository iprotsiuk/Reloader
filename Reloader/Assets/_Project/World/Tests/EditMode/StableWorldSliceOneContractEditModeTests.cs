using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.World.Runtime;
using Reloader.World.Runtime.Origin;
using Reloader.World.Travel;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Tests.EditMode
{
    public sealed class StableWorldSliceOneContractEditModeTests
    {
        private const float Epsilon = 0.0001f;

        private Scene _scene;
        private GameObject _controllerObject;
        private GameObject _persistentRootObject;
        private GameObject _playerRootObject;
        private GameObject _propObject;
        private GameObject _npcObject;
        private GameObject _anchorObject;
        private DynamicOriginRebaseController _controller;
        private DynamicOriginRebaseState _state;
        private StableWorldCoordinateBridge _bridge;
        private PersistentPlayerRoot _persistentRoot;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(_scene);

            _controllerObject = new GameObject("DynamicOriginController");
            SceneManager.MoveGameObjectToScene(_controllerObject, _scene);

            _state = _controllerObject.AddComponent<DynamicOriginRebaseState>();
            _bridge = _controllerObject.AddComponent<StableWorldCoordinateBridge>();
            _controller = _controllerObject.AddComponent<DynamicOriginRebaseController>();
            _bridge.Initialize(_state);

            _persistentRootObject = new GameObject(nameof(PersistentPlayerRoot));
            SceneManager.MoveGameObjectToScene(_persistentRootObject, _scene);
            _persistentRoot = _persistentRootObject.AddComponent<PersistentPlayerRoot>();

            _playerRootObject = new GameObject("RuntimePlayerRoot");
            SceneManager.MoveGameObjectToScene(_playerRootObject, _scene);
            _persistentRoot.RegisterRuntimePlayerRoot(_playerRootObject.transform);

            _propObject = new GameObject("NearbyProp");
            _npcObject = new GameObject("NearbyNpc");
            _anchorObject = new GameObject("NearbyAnchor");
            SceneManager.MoveGameObjectToScene(_propObject, _scene);
            SceneManager.MoveGameObjectToScene(_npcObject, _scene);
            SceneManager.MoveGameObjectToScene(_anchorObject, _scene);

            _controller.Configure(_persistentRoot, _state, _bridge);
            _controller.ResetState();
        }

        [TearDown]
        public void TearDown()
        {
            _scene = default;
        }

        [Test]
        public void SliceOne_RepeatedRebases_PreserveStablePositionsAndRelativeOffsetsWithoutDrift()
        {
            var startupHorizontalBaseline = new Vector2(_playerRootObject.transform.position.x, _playerRootObject.transform.position.z);
            var nearbyObjects = new[]
            {
                _propObject.transform,
                _npcObject.transform,
                _anchorObject.transform,
            };

            var relativeOffsets = new[]
            {
                new Vector3(4f, 0.5f, 7f),
                new Vector3(-6f, 1.25f, -2f),
                new Vector3(2f, -3f, 5f),
            };

            var farLocalPositions = new[]
            {
                new Vector3(540f, 10f, 32f),
                new Vector3(-612f, 4f, 218f),
                new Vector3(505f, -2f, -499f),
            };

            var rebaseTimes = new[] { 10f, 12f, 14f };
            for (var step = 0; step < farLocalPositions.Length; step++)
            {
                _playerRootObject.transform.position = farLocalPositions[step];

                var stablePositionsBefore = new List<Vector3> { _bridge.LocalToStable(_playerRootObject.transform.position) };
                for (var i = 0; i < nearbyObjects.Length; i++)
                {
                    nearbyObjects[i].position = _playerRootObject.transform.position + relativeOffsets[i];
                    stablePositionsBefore.Add(_bridge.LocalToStable(nearbyObjects[i].position));
                }

                var rebased = _controller.TryRebaseIfNeeded(rebaseTimes[step]);

                Assert.That(rebased, Is.True, $"Expected step {step} to trigger the canonical floating-origin controller.");
                Assert.That(
                    Vector2.Distance(new Vector2(_playerRootObject.transform.position.x, _playerRootObject.transform.position.z), startupHorizontalBaseline),
                    Is.LessThanOrEqualTo(Epsilon),
                    $"Expected step {step} to return the canonical runtime player to its bounded startup baseline window.");
                Assert.That(_bridge.LocalToStable(_playerRootObject.transform.position), Is.EqualTo(stablePositionsBefore[0]));

                for (var i = 0; i < nearbyObjects.Length; i++)
                {
                    Assert.That(nearbyObjects[i].position - _playerRootObject.transform.position, Is.EqualTo(relativeOffsets[i]));
                    Assert.That(
                        _bridge.LocalToStable(nearbyObjects[i].position),
                        Is.EqualTo(stablePositionsBefore[i + 1]),
                        $"Expected step {step} to preserve stable-space truth for nearby object {nearbyObjects[i].name}.");
                }
            }
        }

        [Test]
        public void SliceOne_StableWorldCoordinateBridge_RemainsOnlyApprovedStableLocalConversionSeam()
        {
            var conversionOwners = new[]
            {
                typeof(StableWorldCoordinateBridge),
                typeof(DynamicOriginRebaseController),
                typeof(DynamicOriginRebaseState),
                typeof(BootstrapWorldRoot),
                typeof(PersistentPlayerRoot),
                typeof(WorldTravelCoordinator),
            };

            var conversionMethods = conversionOwners
                .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(method => !method.IsSpecialName && LooksLikeStableLocalConversion(method.Name))
                    .Select(method => $"{type.Name}.{method.Name}"))
                .OrderBy(name => name)
                .ToArray();

            CollectionAssert.AreEquivalent(
                new[]
                {
                    "StableWorldCoordinateBridge.LocalDirectionToStable",
                    "StableWorldCoordinateBridge.LocalToStable",
                    "StableWorldCoordinateBridge.StableToLocal",
                },
                conversionMethods,
                "Stable/local conversion should remain centralized in StableWorldCoordinateBridge instead of leaking alternate conversion seams into controller, bootstrap, persistence, or travel code.");
        }

        [Test]
        public void SliceOne_LocalDirectionToStable_RemainsVerifiedProjectionSeamAcrossRebases()
        {
            var direction = new Vector3(7f, -2f, 3f);
            Assert.That(_bridge.LocalDirectionToStable(direction), Is.EqualTo(direction));

            _state.ApplyRebase(new Vector3(-300f, 0f, 125f), new Vector3(300f, 0f, -125f), 3f);
            _state.ApplyRebase(new Vector3(150f, 0f, -40f), new Vector3(-150f, 0f, 40f), 9f);

            var converted = _bridge.LocalDirectionToStable(direction);
            Assert.That(converted, Is.EqualTo(direction));
            Assert.That(converted.magnitude, Is.EqualTo(direction.magnitude).Within(Epsilon));
        }

        private static bool LooksLikeStableLocalConversion(string methodName)
        {
            return methodName.Contains("LocalToStable")
                || methodName.Contains("StableToLocal")
                || methodName.Contains("LocalDirectionToStable")
                || methodName.Contains("StableDirectionToLocal");
        }
    }
}
