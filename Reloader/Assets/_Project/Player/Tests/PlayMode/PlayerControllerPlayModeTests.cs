using NUnit.Framework;
using Reloader.Player;
using Reloader.Player.Viewmodel;
using Reloader.Core.Events;
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

        [Test]
        public void TestInputSource_ExposesAimHeldProperty()
        {
            var root = new GameObject("InputSourceRoot");
            var input = root.AddComponent<TestInputSource>();
            IPlayerInputSource source = input;
            input.AimHeldValue = true;

            Assert.That(source.AimHeld, Is.True);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void PlayerMover_Tick_AimHeld_AppliesAdsMultiplier()
        {
            var root = new GameObject("PlayerRoot");
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.3f;

            var input = root.AddComponent<TestInputSource>();
            input.Move = Vector2.up;
            input.AimHeldValue = true;
            var settings = new PlayerMovementSettings
            {
                WalkSpeed = 10f,
                SprintSpeed = 14f,
                Acceleration = 100f,
                Gravity = -25f,
                JumpHeight = 1.25f
            };

            var mover = root.AddComponent<PlayerMover>();
            mover.Configure(input, settings);
            mover.Tick(0.1f);

            // With default ADS multiplier target speed should be below base walk speed.
            Assert.That(root.transform.position.z, Is.GreaterThan(0.4f));
            Assert.That(root.transform.position.z, Is.LessThan(1f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void FpsViewmodelAnimatorDriver_NormalizeSpeed_UsesMaxWalkSprintAndClamps()
        {
            var normalizedMid = FpsViewmodelAnimatorDriver.NormalizeSpeed(3f, 4f, 8f);
            var normalizedHigh = FpsViewmodelAnimatorDriver.NormalizeSpeed(100f, 4f, 8f);
            var normalizedZero = FpsViewmodelAnimatorDriver.NormalizeSpeed(3f, 0f, 0f);

            Assert.That(normalizedMid, Is.EqualTo(0.375f).Within(0.0001f));
            Assert.That(normalizedHigh, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(normalizedZero, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ViewmodelAnimationAdapter_MapsWeaponEventsToRuntimeState()
        {
            var root = new GameObject("ViewmodelAdapterRoot");
            var adapter = root.AddComponent<ViewmodelAnimationAdapter>();
            adapter.SetEquippedItemIdForTests("weapon-rifle-01");

            GameEvents.RaiseWeaponReloadStarted("weapon-rifle-01");
            GameEvents.RaiseWeaponFired("weapon-rifle-01", Vector3.zero, Vector3.forward);
            GameEvents.RaiseWeaponAimChanged("weapon-rifle-01", true);
            GameEvents.RaiseWeaponReloadCancelled("weapon-rifle-01", WeaponReloadCancelReason.Sprint);

            Assert.That(adapter.IsReloadingDebug, Is.False);
            Assert.That(adapter.IsAimingDebug, Is.True);
            Assert.That(adapter.AimWeightDebug, Is.EqualTo(1f));
            Assert.That(adapter.FireTriggerCountDebug, Is.EqualTo(1));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ViewmodelBindingResolver_ReportsMissingRequiredBindPointsAsInvalid()
        {
            var root = new GameObject("WeaponRoot");
            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(root.transform);

            var result = ViewmodelBindingResolver.Resolve(root.transform);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorsCount, Is.GreaterThan(0));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ViewmodelProfileResolver_PrefersSpecificThenFamilyThenGlobal()
        {
            var global = ScriptableObject.CreateInstance<AnimationContractProfile>();
            var family = ScriptableObject.CreateInstance<AnimationContractProfile>();
            var specific = ScriptableObject.CreateInstance<AnimationContractProfile>();

            try
            {
                Assert.That(ViewmodelProfileResolver.Resolve(specific, family, global), Is.SameAs(specific));
                Assert.That(ViewmodelProfileResolver.Resolve(null, family, global), Is.SameAs(family));
                Assert.That(ViewmodelProfileResolver.Resolve(null, null, global), Is.SameAs(global));
            }
            finally
            {
                Object.DestroyImmediate(global);
                Object.DestroyImmediate(family);
                Object.DestroyImmediate(specific);
            }
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public Vector2 Move;
            public Vector2 Look;
            public bool SprintHeldValue;
            public bool AimHeldValue;
            public bool JumpPressedThisFrame;
            public bool PickupPressedThisFrame;
            public bool FirePressedThisFrame;
            public bool ReloadPressedThisFrame;
            public int BeltSlotPressed = -1;

            public Vector2 MoveInput => Move;
            public Vector2 LookInput => Look;
            public bool SprintHeld => SprintHeldValue;
            public bool AimHeld => AimHeldValue;
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

            public bool ConsumeMenuTogglePressed()
            {
                return false;
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
