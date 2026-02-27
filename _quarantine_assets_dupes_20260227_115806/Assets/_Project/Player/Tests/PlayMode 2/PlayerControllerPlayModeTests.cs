using NUnit.Framework;
using Reloader.Player;
using UnityEngine;

namespace Reloader.Player.Tests.PlayMode
{
    public class PlayerControllerPlayModeTests
    {
        [Test]
        public void PlayerMover_Tick_MovesRelativeToYaw()
        {
            var root = new GameObject("PlayerRoot");
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.3f;

            var input = root.AddComponent<TestInputSource>();
            input.Move = Vector2.up;

            var settings = new PlayerMovementSettings
            {
                WalkSpeed = 6f,
                SprintSpeed = 9f,
                Acceleration = 100f,
                Gravity = -25f,
                JumpHeight = 1.25f
            };

            var mover = root.AddComponent<PlayerMover>();
            mover.Configure(input, settings);

            root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            mover.Tick(0.1f);

            Assert.That(root.transform.position.x, Is.GreaterThan(0.2f));
            Assert.That(Mathf.Abs(root.transform.position.z), Is.LessThan(0.1f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void PlayerMover_Tick_JumpSetsPositiveVerticalVelocityWhenGrounded()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);

            var root = new GameObject("PlayerRoot");
            root.transform.position = new Vector3(0f, 0.05f, 0f);
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 1f, 0f);

            var input = root.AddComponent<TestInputSource>();
            var mover = root.AddComponent<PlayerMover>();
            mover.Configure(input, new PlayerMovementSettings());

            mover.Tick(0.02f);
            input.JumpPressedThisFrame = true;
            mover.Tick(0.02f);

            Assert.That(mover.VerticalVelocity, Is.GreaterThan(0f));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(ground);
        }

        [Test]
        public void PlayerMover_Tick_JumpBuffer_TriggersJumpWhenGroundedNextFrame()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);

            var root = new GameObject("PlayerRoot");
            root.transform.position = new Vector3(0f, 0.4f, 0f);
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 1f, 0f);

            var input = root.AddComponent<TestInputSource>();
            var settings = new PlayerMovementSettings
            {
                JumpBufferTime = 0.2f,
                Gravity = -25f,
                JumpHeight = 1.4f
            };

            var mover = root.AddComponent<PlayerMover>();
            mover.Configure(input, settings);

            input.JumpPressedThisFrame = true;
            mover.Tick(0.02f); // likely still becoming grounded
            input.JumpPressedThisFrame = false;
            mover.Tick(0.02f); // should consume buffered jump once grounded

            Assert.That(mover.VerticalVelocity, Is.GreaterThan(0f));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(ground);
        }

        [Test]
        public void PlayerLookController_Tick_AppliesYawAndClampsPitch()
        {
            var root = new GameObject("PlayerRoot");
            var cameraPivot = new GameObject("CameraPivot");
            cameraPivot.transform.SetParent(root.transform);

            var input = root.AddComponent<TestInputSource>();
            input.Look = new Vector2(4f, -100f);

            var look = root.AddComponent<PlayerLookController>();
            look.Configure(input, cameraPivot.transform);
            look.PitchClamp = new Vector2(-85f, 85f);
            look.LookSensitivity = new Vector2(1f, 1f);
            look.Tick(1f);

            Assert.That(root.transform.eulerAngles.y, Is.EqualTo(4f).Within(0.05f));
            Assert.That(Mathf.DeltaAngle(0f, cameraPivot.transform.localEulerAngles.x), Is.EqualTo(85f).Within(0.05f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void PlayerInputReader_EnableDisable_DoesNotThrowWithoutActionAsset()
        {
            var go = new GameObject("InputReader");
            var reader = go.AddComponent<PlayerInputReader>();

            Assert.DoesNotThrow(() =>
            {
                reader.enabled = true;
                reader.enabled = false;
            });

            Object.DestroyImmediate(go);
        }

        [Test]
        public void PlayerCursorLockController_LockCursor_LocksAndHidesCursor()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;
            try
            {
                var go = new GameObject("CursorLock");
                var controller = go.AddComponent<PlayerCursorLockController>();

                controller.LockCursor();

                Assert.That(controller.IsCursorLockRequested, Is.True);
                Object.DestroyImmediate(go);
            }
            finally
            {
                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
        }

        [Test]
        public void PlayerCursorLockController_UnlockCursor_UnlocksAndShowsCursor()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;
            try
            {
                var go = new GameObject("CursorLock");
                var controller = go.AddComponent<PlayerCursorLockController>();
                controller.LockCursor();

                controller.UnlockCursor();

                Assert.That(controller.IsCursorLockRequested, Is.False);
                Object.DestroyImmediate(go);
            }
            finally
            {
                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
        }

        [Test]
        public void TestInputSource_ExposesFireAndReloadConsumeMethods()
        {
            var root = new GameObject("InputSourceRoot");
            var input = root.AddComponent<TestInputSource>();
            IPlayerInputSource source = input;

            input.FirePressedThisFrame = true;
            input.ReloadPressedThisFrame = true;

            Assert.That(source.ConsumeFirePressed(), Is.True);
            Assert.That(source.ConsumeReloadPressed(), Is.True);
            Assert.That(source.ConsumeFirePressed(), Is.False);
            Assert.That(source.ConsumeReloadPressed(), Is.False);

            Object.DestroyImmediate(root);
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public Vector2 Move;
            public Vector2 Look;
            public bool SprintHeldValue;
            public bool JumpPressedThisFrame;
            public bool PickupPressedThisFrame;
            public bool FirePressedThisFrame;
            public bool ReloadPressedThisFrame;
            public int BeltSlotPressed = -1;

            public Vector2 MoveInput => Move;
            public Vector2 LookInput => Look;
            public bool SprintHeld => SprintHeldValue;
            public bool ConsumeJumpPressed()
            {
                if (!JumpPressedThisFrame)
                {
                    return false;
                }

                JumpPressedThisFrame = false;
                return true;
            }

            public bool ConsumePickupPressed()
            {
                if (!PickupPressedThisFrame)
                {
                    return false;
                }

                PickupPressedThisFrame = false;
                return true;
            }

            public int ConsumeBeltSelectPressed()
            {
                var pressed = BeltSlotPressed;
                BeltSlotPressed = -1;
                return pressed;
            }

            public bool ConsumeFirePressed()
            {
                if (!FirePressedThisFrame)
                {
                    return false;
                }

                FirePressedThisFrame = false;
                return true;
            }

            public bool ConsumeReloadPressed()
            {
                if (!ReloadPressedThisFrame)
                {
                    return false;
                }

                ReloadPressedThisFrame = false;
                return true;
            }

            private void LateUpdate()
            {
                JumpPressedThisFrame = false;
                PickupPressedThisFrame = false;
                FirePressedThisFrame = false;
                ReloadPressedThisFrame = false;
                BeltSlotPressed = -1;
            }
        }
    }
}
