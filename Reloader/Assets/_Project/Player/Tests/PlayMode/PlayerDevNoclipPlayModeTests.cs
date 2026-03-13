using NUnit.Framework;
using Reloader.Player;
using UnityEngine;

namespace Reloader.Player.Tests.PlayMode
{
    public sealed class PlayerDevNoclipPlayModeTests
    {
        [Test]
        public void Tick_NoclipEnabled_IgnoresGravityAndGroundSnap()
        {
            var root = new GameObject("PlayerNoclipGravityRoot");
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.3f;

            var input = root.AddComponent<TestInputSource>();
            var mover = root.AddComponent<PlayerMover>();
            mover.Configure(input, new PlayerMovementSettings
            {
                Gravity = -25f,
                GroundedSnapVelocity = -2f
            });

            mover.SetDevNoclip(true, 12f);
            mover.Tick(0.1f);

            Assert.That(mover.VerticalVelocity, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(root.transform.position.y, Is.EqualTo(0f).Within(0.0001f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Tick_NoclipEnabled_UsesDevNoclipSpeedInsteadOfWalkSprintSettings()
        {
            var root = new GameObject("PlayerNoclipSpeedRoot");
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.3f;

            var input = root.AddComponent<TestInputSource>();
            input.Move = Vector2.up;

            var mover = root.AddComponent<PlayerMover>();
            mover.Configure(input, new PlayerMovementSettings
            {
                WalkSpeed = 2f,
                SprintSpeed = 3f,
                Acceleration = 100f,
                Gravity = -25f
            });

            mover.SetDevNoclip(true, 12f);
            mover.Tick(0.1f);

            Assert.That(root.transform.position.z, Is.EqualTo(1.2f).Within(0.05f));
            Assert.That(root.transform.position.z, Is.GreaterThan(0.8f));
            Assert.That(mover.VerticalVelocity, Is.EqualTo(0f).Within(0.0001f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Tick_NoclipEnabled_FollowsActiveCameraPitchForFlight()
        {
            var root = new GameObject("PlayerNoclipFlightRoot");
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.3f;

            var input = root.AddComponent<TestInputSource>();
            input.Move = Vector2.up;

            var cameraRoot = new GameObject("PlayerCamera");
            cameraRoot.tag = "MainCamera";
            cameraRoot.transform.SetParent(root.transform, false);
            cameraRoot.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1f, 1f).normalized, Vector3.up);
            cameraRoot.AddComponent<Camera>();

            var mover = root.AddComponent<PlayerMover>();
            mover.Configure(input, new PlayerMovementSettings
            {
                WalkSpeed = 2f,
                SprintSpeed = 3f,
                Acceleration = 100f,
                Gravity = -25f
            });

            mover.SetDevNoclip(true, 10f);
            mover.Tick(0.1f);

            Assert.That(root.transform.position.y, Is.GreaterThan(0.6f));
            Assert.That(root.transform.position.z, Is.GreaterThan(0.6f));
            Assert.That(mover.VerticalVelocity, Is.EqualTo(0f).Within(0.0001f));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(cameraRoot);
        }

        [Test]
        public void Tick_NoclipEnabled_WithoutExplicitSpeed_UsesFiveTimesWalkSpeed()
        {
            var root = new GameObject("PlayerNoclipDefaultSpeedRoot");
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.3f;

            var input = root.AddComponent<TestInputSource>();
            input.Move = Vector2.up;

            var mover = root.AddComponent<PlayerMover>();
            mover.Configure(input, new PlayerMovementSettings
            {
                WalkSpeed = 2f,
                SprintSpeed = 3f,
                Acceleration = 100f,
                Gravity = -25f
            });

            mover.SetDevNoclip(true, 0f);
            mover.Tick(0.1f);

            Assert.That(root.transform.position.z, Is.EqualTo(1f).Within(0.05f));
            Assert.That(root.transform.position.z, Is.GreaterThan(0.8f));
            Assert.That(mover.VerticalVelocity, Is.EqualTo(0f).Within(0.0001f));

            Object.DestroyImmediate(root);
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public Vector2 Move;

            public Vector2 MoveInput => Move;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public bool ConsumePickupPressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeDevConsoleTogglePressed() => false;
            public bool ConsumeAutocompletePressed() => false;
            public int ConsumeSuggestionDelta() => 0;
        }
    }
}
