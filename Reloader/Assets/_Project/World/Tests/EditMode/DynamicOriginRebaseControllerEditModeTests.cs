using System.Reflection;
using NUnit.Framework;
using Reloader.World.Runtime;
using Reloader.World.Runtime.Origin;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Tests.EditMode
{
    public sealed class DynamicOriginRebaseControllerEditModeTests
    {
        private const float Epsilon = 0.0001f;

        private Scene _scene;
        private GameObject _controllerObject;
        private GameObject _persistentRootObject;
        private GameObject _playerRootObject;
        private GameObject _propObject;
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
            SceneManager.MoveGameObjectToScene(_propObject, _scene);

            _controller.Configure(_persistentRoot, _state, _bridge);
            _controller.ResetState();
        }

        [TearDown]
        public void TearDown()
        {
            _scene = default;
        }

        [Test]
        public void FloatingOriginRuntime_DefinesCanonicalOriginTypesInWorldAssembly()
        {
            AssertFloatingOriginTypeExists("Reloader.World.Runtime.Origin.DynamicOriginRebaseController");
            AssertFloatingOriginTypeExists("Reloader.World.Runtime.Origin.StableWorldCoordinateBridge");
            AssertFloatingOriginTypeExists("Reloader.World.Runtime.Origin.DynamicOriginRebaseState");
            AssertFloatingOriginTypeExists("Reloader.World.Runtime.Origin.IOriginRebaseParticipant");
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

        [Test]
        public void TryRebaseIfNeeded_WhenThresholdCrossed_ShiftsSceneOnceAndReturnsPlayerToBoundedBaselineWindow()
        {
            var participant = _propObject.AddComponent<RecordingParticipant>();
            var startupHorizontalBaseline = new Vector2(_playerRootObject.transform.position.x, _playerRootObject.transform.position.z);
            _playerRootObject.transform.position = new Vector3(630f, 12f, -245f);
            _propObject.transform.position = new Vector3(645f, 2f, -240f);
            var initialOffset = _propObject.transform.position - _playerRootObject.transform.position;
            var canonicalPlayer = _persistentRoot.PlayerRootTransform;

            var rebased = _controller.TryRebaseIfNeeded(10f);

            Assert.That(rebased, Is.True);
            Assert.That(_persistentRoot.PlayerRootTransform, Is.SameAs(canonicalPlayer));
            Assert.That(
                Vector2.Distance(new Vector2(canonicalPlayer.position.x, canonicalPlayer.position.z), startupHorizontalBaseline),
                Is.LessThanOrEqualTo(Epsilon),
                "Canonical rebasing should return the runtime player to its bounded startup baseline window.");
            Assert.That(canonicalPlayer.position.y, Is.EqualTo(12f));
            Assert.That(_propObject.transform.position - canonicalPlayer.position, Is.EqualTo(initialOffset));
            Assert.That(_state.StableOriginOffset, Is.EqualTo(new Vector3(630f, 0f, -245f)));
            Assert.That(_state.LocalOriginOffset, Is.EqualTo(new Vector3(-630f, 0f, 245f)));
            Assert.That(_state.LastRebaseTime, Is.EqualTo(10f));
            Assert.That(_controller.LastRebaseTime, Is.EqualTo(10f));
            Assert.That(participant.BeforeCount, Is.EqualTo(1));
            Assert.That(participant.AfterCount, Is.EqualTo(1));
            Assert.That(participant.BeforeLocalShift, Is.EqualTo(new Vector3(-630f, 0f, 245f)));
            Assert.That(participant.AfterLocalShift, Is.EqualTo(new Vector3(-630f, 0f, 245f)));
            Assert.That(participant.BeforeStableShift, Is.EqualTo(new Vector3(630f, 0f, -245f)));
            Assert.That(participant.AfterStableShift, Is.EqualTo(new Vector3(630f, 0f, -245f)));
        }

        [Test]
        public void TryRebaseIfNeeded_UsesHorizontalDistanceAndHonorsCooldown()
        {
            _playerRootObject.transform.position = new Vector3(0f, 900f, 0f);
            Assert.That(_controller.TryRebaseIfNeeded(1f), Is.False,
                "Vertical displacement alone should not trigger floating-origin rebasing.");

            _playerRootObject.transform.position = new Vector3(501f, 4f, 0f);
            Assert.That(_controller.TryRebaseIfNeeded(5f), Is.True);

            _playerRootObject.transform.position = new Vector3(510f, 4f, 0f);
            Assert.That(_controller.TryRebaseIfNeeded(5.5f), Is.False,
                "Cooldown should prevent immediate retrigger from the canonical controller.");
            Assert.That(_controller.LastRebaseTime, Is.EqualTo(5f));

            _playerRootObject.transform.position = new Vector3(510f, 4f, 0f);
            Assert.That(_controller.TryRebaseIfNeeded(6.1f), Is.True);
            Assert.That(_controller.LastRebaseTime, Is.EqualTo(6.1f));
        }

        private static System.Type AssertFloatingOriginTypeExists(string fullTypeName)
        {
            var resolvedType = System.Type.GetType($"{fullTypeName}, Reloader.World");
            Assert.That(resolvedType, Is.Not.Null, $"Expected floating-origin runtime type '{fullTypeName}' in assembly 'Reloader.World'.");
            return resolvedType;
        }

        private sealed class RecordingParticipant : MonoBehaviour, IOriginRebaseParticipant
        {
            public int BeforeCount { get; private set; }
            public int AfterCount { get; private set; }
            public Vector3 BeforeLocalShift { get; private set; }
            public Vector3 BeforeStableShift { get; private set; }
            public Vector3 AfterLocalShift { get; private set; }
            public Vector3 AfterStableShift { get; private set; }

            public void OnBeforeOriginRebase(Vector3 localShift, Vector3 stableShift)
            {
                BeforeCount++;
                BeforeLocalShift = localShift;
                BeforeStableShift = stableShift;
            }

            public void OnAfterOriginRebase(Vector3 localShift, Vector3 stableShift)
            {
                AfterCount++;
                AfterLocalShift = localShift;
                AfterStableShift = stableShift;
            }
        }
    }
}
